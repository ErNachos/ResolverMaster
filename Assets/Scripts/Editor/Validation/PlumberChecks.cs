using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Games.Plumber;
using ResolverMaster.Games.Plumber.View;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResolverMaster.Editor.Validation
{
    /// <summary>Core puzzle parameters present and sensible.</summary>
    public sealed class PuzzleConfigCheck : IValidationCheck
    {
        public string Name => "PuzzleConfig";

        public void Run(ValidationReport report)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<PuzzleConfig>(ConfigAssets.PuzzleConfigPath);
            if (cfg == null)
            {
                report.Error($"{ConfigAssets.PuzzleConfigPath} missing: run Resolver Master > Setup > Ensure Config Assets");
                return;
            }
            if (cfg.stars.twoStarExtraPercent < 0) report.Error("stars: twoStarExtraPercent must be >= 0");
            if (cfg.stars.twoStarMinExtra < 1) report.Error("stars: twoStarMinExtra must be >= 1");
        }
    }

    /// <summary>
    /// Water colors (ErNa-006): the palette rules of <see cref="PlumberPalette"/> (at most 4, never red with green, lightness apart),
    /// a sink halo for each, and no UI color (UiKitStyle), theme color (ThemeConfig) or chapter color close to a water color
    /// (delta E below PlumberConfig.minUiWaterDeltaE). Opaque colors only (dim layers and tints are mixed with other colors).
    /// </summary>
    public sealed class PlumberPaletteCheck : IValidationCheck
    {
        public string Name => "PlumberPalette";

        public void Run(ValidationReport report)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<PlumberConfig>(ConfigAssets.PlumberConfigPath);
            if (cfg == null) return;   // reported by the Plumber check
            foreach (var p in PlumberPalette.Problems(cfg.waterColors, cfg.minWaterLightnessGap)) report.Error("waterColors: " + p);
            if (cfg.sinkHaloColors.Count < cfg.waterColors.Count) report.Error("every water color needs a sink halo color in sinkHaloColors");

            var others = new List<(string what, Color color)>();
            var style = AssetDatabase.LoadAssetAtPath<UiKitStyle>(UiKitAssets.StylePath);
            if (style != null)
                foreach (var f in typeof(UiKitStyle).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == typeof(Color)))
                    others.Add(("UiKitStyle." + f.Name, (Color)f.GetValue(style)));
            var themes = AssetDatabase.LoadAssetAtPath<ResolverMaster.Core.UI.Themes.ThemeConfig>(ConfigAssets.ThemeConfigPath);
            if (themes != null)
                foreach (var t in themes.themes)
                foreach (var f in typeof(ResolverMaster.Core.UI.Themes.VisualTheme).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == typeof(Color)))
                    others.Add(($"theme '{t.id}'.{f.Name}", (Color)f.GetValue(t)));
            for (int i = 0; i < cfg.chapters.Count; i++) others.Add(($"chapter {i + 1} color", cfg.chapters[i].color));
            others.Add(("pipeEmpty", cfg.pipeEmpty));
            foreach (var (what, color) in others)
            {
                if (color.a < 0.99f) continue;
                for (int w = 0; w < cfg.waterColors.Count; w++)
                {
                    float d = ColorVision.DeltaE(color, cfg.waterColors[w]);
                    if (d < cfg.minUiWaterDeltaE) report.Error($"{what} #{ColorUtility.ToHtmlStringRGB(color)} is close to water color {w} (delta E {d:0.0} < {cfg.minUiWaterDeltaE:0})");
                }
            }
        }
    }

    /// <summary>
    /// Plumber fountain drawings (ErNa-004): for every variant (PlumberConfig.artVariants, 4) a source with a non-empty mask and a sink,
    /// as files in Assets/Art/Plumber and in the sprite library the game reads (Resources/Games/Plumber/PlumberArt.asset).
    /// </summary>
    public sealed class PlumberArtCheck : IValidationCheck
    {
        public string Name => "PlumberArt";

        public void Run(ValidationReport report)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<PlumberConfig>(ConfigAssets.PlumberConfigPath);
            int variants = cfg != null ? cfg.artVariants : 4;
            if (variants < 1) report.Error("PlumberConfig.artVariants must be at least 1");
            var lib = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.PlumberArtPath);
            if (lib == null) report.Error($"{UiKitAssets.PlumberArtPath} missing: run Resolver Master > UI Kit > Rebuild Sprite Libraries");
            for (int v = 0; v < variants; v++)
            {
                Check(report, lib, "Sources", PlumberArt.SourceName(v), needInk: false);
                Check(report, lib, "Sources", PlumberArt.SourceMaskName(v), needInk: true);
                Check(report, lib, "Targets", PlumberArt.SinkName(v), needInk: false);
            }
        }

        static void Check(ValidationReport report, IconLibrary lib, string folder, string name, bool needInk)
        {
            string path = $"{PlumberArt.AssetDir}/{folder}/{name}.png";
            if (!System.IO.File.Exists(path))
            {
                report.Error($"{path} missing: run python Tools/Art/make_plumber_art.py");
                return;
            }
            if (lib != null && !lib.Has(name)) report.Error($"'{name}' not in the Plumber sprite library: run Resolver Master > UI Kit > Rebuild Sprite Libraries");
            var tex = new Texture2D(2, 2);
            try
            {
                if (!tex.LoadImage(System.IO.File.ReadAllBytes(path)))
                {
                    report.Error($"{path}: not a readable PNG");
                    return;
                }
                int visible = tex.GetPixels32().Count(p => p.a > 16);
                if (visible == 0) report.Error($"{path}: the image is empty");
                else if (needInk && visible < tex.width * tex.height / 100) report.Error($"{path}: mask almost empty ({visible} visible px): magenta not found?");
            }
            finally { Object.DestroyImmediate(tex); }
        }
    }

    /// <summary>
    /// ErNa-006: bridges needed where the tier says so, locked pieces and bridges counted by the tier, last level within the chapter 8
    /// maximum + 30%.
    /// Plumber parameters and Story levels (ErNa-003 rules): water colors with a sink halo each, chapter icons, names, valid themes and texts in every language;
    /// each chapter has exactly 9 levels in order; each level parses, is solvable with no leaks and colors apart, its saved minimum
    /// moves match the solver, opening the water at the start does not solve it, it respects its difficulty tier, in chapter 1 the
    /// solution never uses a T (and the level stays solvable with the same minimum when T pieces are forbidden), and (warning) the
    /// generator still produces it from its seed.
    /// </summary>
    public sealed class PlumberStoryCheck : IValidationCheck
    {
        public string Name => "Plumber";

        public void Run(ValidationReport report)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<PlumberConfig>(ConfigAssets.PlumberConfigPath);
            if (cfg == null)
            {
                report.Error($"{ConfigAssets.PlumberConfigPath} missing: run Resolver Master > Setup > Ensure Config Assets");
                return;
            }
            CheckConfig(cfg, report);

            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigAssets.PlumberStoryPath);
            if (text == null)
            {
                report.Error($"{ConfigAssets.PlumberStoryPath} missing: run Resolver Master > Idraulico > Genera livelli Storia");
                return;
            }
            var set = JsonUtility.FromJson<PlumberLevelSet>(text.text);
            if (set?.levels == null || set.levels.Count == 0)
            {
                report.Error("no Story levels in " + ConfigAssets.PlumberStoryPath);
                return;
            }
            int last = cfg.storyDifficulty.LastLevel;
            if (set.levels.Count != last) report.Error($"{set.levels.Count} Story levels saved, the difficulty table has {last}");

            // chapters: consecutive, exactly 9 levels each, a theme for each
            var book = PlumberModule.BuildBook(set.levels, cfg.chapters);
            if (book.LevelCount != set.levels.Count) report.Error($"chapters cover {book.LevelCount} of {set.levels.Count} levels (chapter numbers out of order?)");
            foreach (var chapter in book.Chapters)
            {
                if (chapter.LevelCount != StoryBook.LevelsPerChapter)
                    report.Error($"chapter {chapter.Number} has {chapter.LevelCount} levels, expected {StoryBook.LevelsPerChapter}");
                if (chapter.Number > cfg.chapters.Count) report.Error($"chapter {chapter.Number} has no name/theme in PlumberConfig.chapters");
            }

            for (int i = 0; i < set.levels.Count; i++)
                CheckLevel(cfg, set.levels[i], i, report);

            // ErNa-006: the last level is demanding but not frustrating: its minimum stays within the chapter 8 maximum + 30%
            const int chapter8 = 8;
            var ch8 = set.levels.Where(l => l.chapter == chapter8).ToList();
            if (ch8.Count > 0 && set.levels.Count > 0)
            {
                int max8 = ch8.Max(l => l.minMoves), final = set.levels[set.levels.Count - 1].minMoves;
                if (final > max8 * cfg.finalLevelMaxRatio)
                    report.Error($"last level: minimum {final} above the chapter 8 maximum {max8} x {cfg.finalLevelMaxRatio:0.00}");
            }
        }

        static void CheckConfig(PlumberConfig cfg, ValidationReport report)
        {
            int last = cfg.storyDifficulty.LastLevel;
            if (last == 0) report.Error("Story difficulty table is empty");
            for (int n = 1; n <= last; n++)
                if (cfg.storyDifficulty.ForLevel(n) == null) report.Error($"no difficulty tier covers level {n}");
            if (cfg.waterColors.Count < 4) report.Error($"waterColors has {cfg.waterColors.Count} colors, the Story uses 4");
            if (cfg.sinkHaloColors.Count < cfg.waterColors.Count) report.Error("every water color needs a sink halo color in sinkHaloColors");

            var icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.IconLibraryPath);
            if (icons != null)
            {
                foreach (var chapter in cfg.chapters)
                    if (!icons.Has(chapter.icon)) report.Error($"chapter icon '{chapter.icon}' missing in the IconLibrary");
                if (!icons.Has("icon_faucet")) report.Error("icon 'icon_faucet' (Open the water button) missing in the IconLibrary");
            }

            // every chapter names a valid theme of the Core ThemeConfig
            var themes = AssetDatabase.LoadAssetAtPath<ResolverMaster.Core.UI.Themes.ThemeConfig>(ConfigAssets.ThemeConfigPath);
            for (int i = 0; i < cfg.chapters.Count; i++)
            {
                var id = cfg.chapters[i].theme;
                if (string.IsNullOrEmpty(id)) report.Error($"chapter {i + 1}: no theme (PlumberConfig.chapters[{i}].theme)");
                else if (themes != null && !themes.Catalog.Has(id)) report.Error($"chapter {i + 1}: theme '{id}' does not exist in ThemeConfig");
            }

            var loc = LocalizationLoader.Load(Language.English);
            var keys = typeof(PlumberKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .Concat(cfg.chapters.Select(c => c.nameKey));
            foreach (var key in keys.Distinct())
            foreach (var lang in LanguageCodes.All)
                if (!loc.Has(lang, key)) report.Error($"key '{key}' used by the Plumber is missing in '{LanguageCodes.ToCode(lang)}'");
        }

        static void CheckLevel(PlumberConfig cfg, PlumberLevel level, int i, ValidationReport report)
        {
            string id = $"level {i + 1}";
            if (level.number != i + 1) report.Error($"{id}: number is {level.number}");
            int chapter = i / StoryBook.LevelsPerChapter + 1, index = i % StoryBook.LevelsPerChapter;
            if (level.chapter != chapter || level.index != index)
                report.Error($"{id}: saved as chapter {level.chapter} index {level.index}, expected chapter {chapter} index {index}");
            PipeGrid grid;
            try { grid = level.CreateGrid(); }
            catch (FormatException e)
            {
                report.Error($"{id}: cells not readable: {e.Message}");
                return;
            }
            if (grid.Width != level.width || grid.Height != level.height)
                report.Error($"{id}: cells are {grid.Width}x{grid.Height}, data says {level.width}x{level.height}");
            if (grid.MaxColor() >= cfg.waterColors.Count) report.Error($"{id}: uses water color {grid.MaxColor()}, only {cfg.waterColors.Count} defined");

            var sol = PlumberSolver.Solve(grid);
            if (!sol.Solvable)
            {
                report.Error($"{id}: not solvable ({sol.Reason})");
                return;
            }
            if (sol.MinMoves != level.minMoves) report.Error($"{id}: saved minimum moves {level.minMoves}, solver finds {sol.MinMoves}");
            if (WaterFlow.Compute(grid).Success) report.Error($"{id}: opening the water at the start already solves it");
            if (sol.PathLength != level.path) report.Warning($"{id}: saved route length {level.path}, solver finds {sol.PathLength}");

            // chapter 1: the T is only a decoy
            if (chapter == 1)
            {
                if (sol.TeesOnPath > 0) report.Error($"{id}: the solution uses {sol.TeesOnPath} T piece(s) (chapter 1: T only as a decoy)");
                var noTee = PlumberSolver.Solve(grid, new PlumberSolver.Options { ForbidTees = true });
                if (!noTee.Solvable || noTee.MinMoves != sol.MinMoves) report.Error($"{id}: without T pieces the minimum changes (chapter 1: T only as a decoy)");
            }

            var tier = cfg.storyDifficulty.ForLevel(level.number);
            if (tier == null) return;
            foreach (var problem in PlumberDifficulty.CheckTier(tier, grid, sol.MinMoves, sol.PathLength))
                report.Error($"{id} (tier {tier.id}): {problem}");
            // ErNa-006: a needed bridge = the level is impossible with crosses (which mix) in place of the bridges
            if (tier.GetOr(PlumberDifficulty.NeedBridge, 0).min > 0 && PlumberSolver.Solve(grid, new PlumberSolver.Options { BridgesAsCrosses = true }).Solvable)
                report.Error($"{id} (tier {tier.id}): the bridge is not needed (solvable with crosses in its place)");

            try
            {
                var regen = PlumberGenerator.Generate(PlumberGenerator.Settings.FromTier(tier, cfg.maxAttempts), level.seed, tier.id, level.number);
                if (regen.Level.cells != level.cells)
                    report.Warning($"{id}: the generator now makes a different level from seed {level.seed} (generator or table changed; regenerate if intended)");
            }
            catch (InvalidOperationException e)
            {
                report.Warning($"{id}: cannot regenerate from seed {level.seed}: {e.Message}");
            }
        }
    }
}
