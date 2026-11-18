using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Themes;
using ResolverMaster.Games.TrafficJam;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor.Validation
{
    /// <summary>
    /// TJ-001, Traffic jam colors: two target colors (red, blue) far apart; no other color of the game (vehicles, barriers), of the UI
    /// (UiKitStyle), of the themes (ThemeConfig) or of the Traffic jam chapters within TrafficJamConfig.minTargetDeltaE (CIE76) of them.
    /// Opaque colors only.
    /// </summary>
    public sealed class TrafficJamPaletteCheck : IValidationCheck
    {
        public string Name => "TrafficJamPalette";

        public void Run(ValidationReport report)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<TrafficJamConfig>(ConfigAssets.TrafficJamConfigPath);
            if (cfg == null) return;   // reported by the Traffic jam check
            if (cfg.targetColors.Count < 2)
            {
                report.Error("targetColors needs 2 colors (red and blue target cars)");
                return;
            }
            if (ColorVision.DeltaE(cfg.targetColors[0], cfg.targetColors[1]) < 40f) report.Error("the two target colors are too close (delta E < 40)");
            if (cfg.vehicleColors.Count < 3) report.Error("vehicleColors needs at least 3 colors");
            var others = new List<(string what, Color color)>();
            var style = AssetDatabase.LoadAssetAtPath<UiKitStyle>(UiKitAssets.StylePath);
            if (style != null)
                foreach (var f in typeof(UiKitStyle).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == typeof(Color)))
                    others.Add(("UiKitStyle." + f.Name, (Color)f.GetValue(style)));
            var themes = AssetDatabase.LoadAssetAtPath<ThemeConfig>(ConfigAssets.ThemeConfigPath);
            if (themes != null)
                foreach (var t in themes.themes)
                foreach (var f in typeof(VisualTheme).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == typeof(Color)))
                    others.Add(($"theme '{t.id}'.{f.Name}", (Color)f.GetValue(t)));
            for (int i = 0; i < cfg.chapters.Count; i++) others.Add(($"Traffic jam chapter {i + 1} color", cfg.chapters[i].color));
            for (int i = 0; i < cfg.vehicleColors.Count; i++) others.Add(($"vehicleColors[{i}]", cfg.vehicleColors[i]));
            // ErNa-010: the Home background of every game (base, gradient ends and pattern)
            foreach (var g in ResolverMaster.Core.Games.GameRegistry.Discover().Games)
            {
                if (g.BackgroundColor.a <= 0f) continue;
                others.Add(($"{g.Id} BackgroundColor", g.BackgroundColor));
                others.Add(($"{g.Id} background top", ResolverMaster.Core.UI.BackgroundColors.Top(g.BackgroundColor)));
                others.Add(($"{g.Id} background bottom", ResolverMaster.Core.UI.BackgroundColors.Bottom(g.BackgroundColor)));
                others.Add(($"{g.Id} background pattern", ResolverMaster.Core.UI.BackgroundColors.Pattern(g.BackgroundColor)));
            }
            others.Add(("barrierColorA", cfg.barrierColorA));
            others.Add(("barrierColorB", cfg.barrierColorB));
            foreach (var (what, color) in others)
            {
                if (color.a < 0.99f) continue;
                for (int t = 0; t < 2; t++)
                {
                    float d = ColorVision.DeltaE(color, cfg.targetColors[t]);
                    if (d < cfg.minTargetDeltaE)
                        report.Error($"{what} #{ColorUtility.ToHtmlStringRGB(color)} is close to the {(t == 0 ? "red" : "blue")} target car (delta E {d:0.0} < {cfg.minTargetDeltaE:0})");
                }
            }
        }
    }

    /// <summary>
    /// TJ-002, Traffic jam drawings: every vehicle and obstacle drawing (<see cref="TrafficLook.AllNames"/>) as a non-empty file in
    /// Assets/Art/TrafficJam and in the sprite library the game reads (Resources/Games/TrafficJam/TrafficJamArt.asset); the obstacle table
    /// of the themes names known themes and known obstacles only.
    /// </summary>
    public sealed class TrafficJamArtCheck : IValidationCheck
    {
        public string Name => "TrafficJamArt";

        public void Run(ValidationReport report)
        {
            var lib = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.TrafficJamArtPath);
            if (lib == null) report.Error($"{UiKitAssets.TrafficJamArtPath} missing: run Resolver Master > UI Kit > Rebuild Sprite Libraries");
            foreach (var name in TrafficLook.AllNames())
            {
                string path = $"{ResolverMaster.Games.TrafficJam.View.TrafficJamArt.AssetDir}/{name}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    report.Error($"{path} missing or not a sprite: run python Tools/Art/make_trafficjam_art.py");
                    continue;
                }
                if (lib != null && !lib.Has(name)) report.Error($"'{name}' not in the Traffic jam sprite library: run Resolver Master > UI Kit > Rebuild Sprite Libraries");
                if (sprite.rect.width < 64 || sprite.rect.height < 64) report.Error($"{path}: too small ({sprite.rect.width}x{sprite.rect.height})");
            }
            var cfg = AssetDatabase.LoadAssetAtPath<TrafficJamConfig>(ConfigAssets.TrafficJamConfigPath);
            if (cfg == null) return;
            var themes = AssetDatabase.LoadAssetAtPath<ThemeConfig>(ConfigAssets.ThemeConfigPath);
            foreach (var row in cfg.obstaclesByTheme)
            {
                if (row == null) continue;
                if (themes != null && !themes.themes.Any(t => t.id == row.theme)) report.Error($"obstaclesByTheme: unknown theme '{row.theme}'");
                if (row.variants == null || row.variants.Count == 0) report.Error($"obstaclesByTheme '{row.theme}': no obstacle");
                else
                    foreach (var v in row.variants)
                        if (!TrafficLook.ObstacleNames.Contains(v)) report.Error($"obstaclesByTheme '{row.theme}': unknown obstacle '{v}'");
            }
        }
    }

    /// <summary>
    /// TJ-001, Traffic jam parameters and Story levels: chapters with icon, name, valid theme and texts in every language; 9 levels per
    /// chapter, numbered in order; each level readable, its saved minimum = the exact solver, positions under the cap, not solved at the
    /// start, inside its difficulty tier; the difficulty never drops inside a chapter and every chapter starts easier than the end of the
    /// previous one; (warning) the generator still makes it from its seed (checked in parallel).
    /// </summary>
    public sealed class TrafficJamStoryCheck : IValidationCheck
    {
        public string Name => "TrafficJam";

        public void Run(ValidationReport report)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<TrafficJamConfig>(ConfigAssets.TrafficJamConfigPath);
            if (cfg == null)
            {
                report.Error($"{ConfigAssets.TrafficJamConfigPath} missing: run Resolver Master > Setup > Ensure Config Assets");
                return;
            }
            CheckConfig(cfg, report);
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigAssets.TrafficJamStoryPath);
            if (text == null)
            {
                report.Error($"{ConfigAssets.TrafficJamStoryPath} missing: run Resolver Master > Traffic jam > Genera livelli Storia");
                return;
            }
            var set = JsonUtility.FromJson<TrafficLevelSet>(text.text);
            if (set?.levels == null || set.levels.Count == 0)
            {
                report.Error("no Story levels in " + ConfigAssets.TrafficJamStoryPath);
                return;
            }
            int last = cfg.storyDifficulty.LastLevel;
            if (set.levels.Count != last) report.Error($"{set.levels.Count} Story levels saved, the difficulty table has {last}");
            var book = TrafficJamModule.BuildBook(set.levels, cfg.chapters);
            if (book.LevelCount != set.levels.Count) report.Error($"chapters cover {book.LevelCount} of {set.levels.Count} levels (chapter numbers out of order?)");
            foreach (var chapter in book.Chapters)
            {
                if (chapter.LevelCount != StoryBook.LevelsPerChapter)
                    report.Error($"chapter {chapter.Number} has {chapter.LevelCount} levels, expected {StoryBook.LevelsPerChapter}");
                if (chapter.Number > cfg.chapters.Count) report.Error($"chapter {chapter.Number} has no name/theme in TrafficJamConfig.chapters");
            }

            for (int i = 0; i < set.levels.Count; i++) CheckLevel(cfg, set.levels[i], i, report);
            for (int i = 1; i < set.levels.Count; i++)
            {
                var a = set.levels[i - 1];
                var b = set.levels[i];
                if (a.chapter == b.chapter && b.minMoves < a.minMoves) report.Error($"level {b.number}: {b.minMoves} moves, easier than level {a.number} ({a.minMoves}) in the same chapter");
                if (a.chapter != b.chapter && b.minMoves >= a.minMoves) report.Error($"chapter {b.chapter} starts at {b.minMoves} moves: not easier than the end of chapter {a.chapter} ({a.minMoves})");
            }
            if (set.levels[0].minMoves > 2) report.Error($"level 1 needs {set.levels[0].minMoves} moves (at most 2)");

            // the generator still makes every level from its seed (the slow part, in parallel)
            var regen = set.levels.Select(l => Task.Run(() =>
            {
                var tier = cfg.storyDifficulty.ForLevel(l.number);
                if (tier == null) return null;
                try
                {
                    var settings = TrafficGenerator.Settings.FromTier(tier, cfg.storyMaxAttempts, cfg.storyMaxStates, cfg.maxMinMoves, cfg.climbSteps);
                    var r = TrafficGenerator.Generate(settings, l.seed, tier.id, l.number);
                    return r.Level.cells == l.cells ? null : $"level {l.number}: the generator now makes a different level from seed {l.seed} (generator or table changed; regenerate if intended)";
                }
                catch (InvalidOperationException e)
                {
                    return $"level {l.number}: cannot regenerate from seed {l.seed}: {e.Message}";
                }
            })).ToArray();
            Task.WaitAll(regen);
            foreach (var t in regen)
                if (t.Result != null) report.Warning(t.Result);
        }

        static void CheckConfig(TrafficJamConfig cfg, ValidationReport report)
        {
            int last = cfg.storyDifficulty.LastLevel;
            if (last == 0) report.Error("Story difficulty table is empty");
            for (int n = 1; n <= last; n++)
                if (cfg.storyDifficulty.ForLevel(n) == null) report.Error($"no difficulty tier covers level {n}");
            foreach (var tier in cfg.storyDifficulty.tiers.Concat(cfg.challengeDifficulty.tiers))
            {
                int cols = tier.GetOr(TrafficDifficulty.Cols, 0).min, rows = tier.GetOr(TrafficDifficulty.Rows, 0).min;
                if (cols < 4 || cols > 7 || rows != cols + 2) report.Error($"tier {tier.id}: grid {cols}x{rows} (vertical grids 4x6, 5x7, 6x8, 7x9)");
                if (tier.GetOr(TrafficDifficulty.MinMoves, 1).max > cfg.maxMinMoves) report.Error($"tier {tier.id}: minimum above the cap of {cfg.maxMinMoves} moves");
            }
            var icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.IconLibraryPath);
            if (icons != null)
                foreach (var chapter in cfg.chapters)
                    if (!icons.Has(chapter.icon)) report.Error($"chapter icon '{chapter.icon}' missing in the IconLibrary");
            var themes = AssetDatabase.LoadAssetAtPath<ThemeConfig>(ConfigAssets.ThemeConfigPath);
            for (int i = 0; i < cfg.chapters.Count; i++)
            {
                var id = cfg.chapters[i].theme;
                if (string.IsNullOrEmpty(id)) report.Error($"chapter {i + 1}: no theme (TrafficJamConfig.chapters[{i}].theme)");
                else if (themes != null && !themes.Catalog.Has(id)) report.Error($"chapter {i + 1}: theme '{id}' does not exist in ThemeConfig");
            }
            var loc = LocalizationLoader.Load(Language.English);
            var keys = typeof(TrafficJamKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .Concat(cfg.chapters.Select(c => c.nameKey));
            foreach (var key in keys.Distinct())
            foreach (var lang in LanguageCodes.All)
                if (!loc.Has(lang, key)) report.Error($"key '{key}' used by Traffic jam is missing in '{LanguageCodes.ToCode(lang)}'");
        }

        static void CheckLevel(TrafficJamConfig cfg, TrafficLevel level, int i, ValidationReport report)
        {
            string id = $"level {i + 1}";
            if (level.number != i + 1) report.Error($"{id}: number is {level.number}");
            int chapter = i / StoryBook.LevelsPerChapter + 1, index = i % StoryBook.LevelsPerChapter;
            if (level.chapter != chapter || level.index != index)
                report.Error($"{id}: saved as chapter {level.chapter} index {level.index}, expected chapter {chapter} index {index}");
            TrafficLayout layout;
            ulong start;
            try { layout = level.CreateLayout(out start); }
            catch (FormatException e)
            {
                report.Error($"{id}: cells not readable: {e.Message}");
                return;
            }
            if (layout.Width != level.width || layout.Height != level.height)
                report.Error($"{id}: cells are {layout.Width}x{layout.Height}, data says {level.width}x{level.height}");
            if (layout.IsSolved(start) || !layout.AllTargetsIn(start)) report.Error($"{id}: a target car is already out at the start");
            var space = TrafficSpace.Explore(layout, start, cfg.maxStates);
            if (space.Capped)
            {
                report.Error($"{id}: more than {cfg.maxStates} reachable positions");
                return;
            }
            if (!space.Solvable)
            {
                report.Error($"{id}: no way out");
                return;
            }
            if (space.MinMoves != level.minMoves) report.Error($"{id}: saved minimum moves {level.minMoves}, solver finds {space.MinMoves}");
            if (space.Count != level.states) report.Warning($"{id}: saved {level.states} positions, solver finds {space.Count}");
            var tier = cfg.storyDifficulty.ForLevel(level.number);
            if (tier == null) return;
            foreach (var problem in TrafficDifficulty.CheckTier(tier, layout, space.MinMoves)) report.Error($"{id} (tier {tier.id}): {problem}");
        }
    }
}
