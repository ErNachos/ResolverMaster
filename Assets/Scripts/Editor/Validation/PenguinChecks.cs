using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Themes;
using ResolverMaster.Editor.Penguin;
using ResolverMaster.Games.Penguin;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor.Validation
{
    /// <summary>
    /// PG-001, Pino parameters and Story levels: chapters with icon, name, valid theme and texts in every language; tiers on the vertical
    /// grids; scarf colors far apart; 9 levels per chapter, numbered in order; each level readable, not solved at the start, its saved
    /// minimum = the exact solver, states under the cap, inside its difficulty tier, using the novelty of its tier and without too many
    /// useless elements (<see cref="PenguinGenerator.Problems"/>); the difficulty never drops inside a chapter and every chapter starts
    /// easier than the end of the previous one; level 1 in at most 2 moves; (warning) the generator still makes it from its seed.
    /// </summary>
    public sealed class PenguinStoryCheck : IValidationCheck
    {
        public string Name => "Penguin";

        public void Run(ValidationReport report)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<PenguinConfig>(ConfigAssets.PenguinConfigPath);
            if (cfg == null)
            {
                report.Error($"{ConfigAssets.PenguinConfigPath} missing: run Resolver Master > Setup > Ensure Config Assets");
                return;
            }
            CheckConfig(cfg, report);
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigAssets.PenguinStoryPath);
            if (text == null)
            {
                report.Error($"{ConfigAssets.PenguinStoryPath} missing: run Resolver Master > Pino > Genera livelli Storia");
                return;
            }
            var set = JsonUtility.FromJson<PenguinLevelSet>(text.text);
            if (set?.levels == null || set.levels.Count == 0)
            {
                report.Error("no Story levels in " + ConfigAssets.PenguinStoryPath);
                return;
            }
            int last = cfg.storyDifficulty.LastLevel;
            if (set.levels.Count != last) report.Error($"{set.levels.Count} Story levels saved, the difficulty table has {last}");
            var book = PenguinModule.BuildBook(set.levels, cfg.chapters);
            if (book.LevelCount != set.levels.Count) report.Error($"chapters cover {book.LevelCount} of {set.levels.Count} levels (chapter numbers out of order?)");
            foreach (var chapter in book.Chapters)
            {
                if (chapter.LevelCount != StoryBook.LevelsPerChapter)
                    report.Error($"chapter {chapter.Number} has {chapter.LevelCount} levels, expected {StoryBook.LevelsPerChapter}");
                if (chapter.Number > cfg.chapters.Count) report.Error($"chapter {chapter.Number} has no name/theme in PenguinConfig.chapters");
            }

            // every level (in parallel: each one runs a few exhaustive searches)
            var checks = set.levels.Select((l, i) => Task.Run(() => CheckLevel(cfg, l, i))).ToArray();
            Task.WaitAll(checks);
            foreach (var t in checks)
            foreach (var (error, message) in t.Result)
                if (error) report.Error(message);
                else report.Warning(message);

            for (int i = 1; i < set.levels.Count; i++)
            {
                var a = set.levels[i - 1];
                var b = set.levels[i];
                if (a.chapter == b.chapter && b.minMoves < a.minMoves) report.Error($"level {b.number}: {b.minMoves} moves, easier than level {a.number} ({a.minMoves}) in the same chapter");
                if (a.chapter != b.chapter && b.minMoves >= a.minMoves) report.Error($"chapter {b.chapter} starts at {b.minMoves} moves: not easier than the end of chapter {a.chapter} ({a.minMoves})");
            }
            if (set.levels[0].minMoves > 2) report.Error($"level 1 needs {set.levels[0].minMoves} moves (at most 2)");
        }

        static void CheckConfig(PenguinConfig cfg, ValidationReport report)
        {
            int last = cfg.storyDifficulty.LastLevel;
            if (last == 0) report.Error("Story difficulty table is empty");
            for (int n = 1; n <= last; n++)
                if (cfg.storyDifficulty.ForLevel(n) == null) report.Error($"no difficulty tier covers level {n}");
            foreach (var tier in cfg.storyDifficulty.tiers.Concat(cfg.challengeDifficulty.tiers))
            {
                int cols = tier.GetOr(PenguinDifficulty.Cols, 0).min, rows = tier.GetOr(PenguinDifficulty.Rows, 0).min;
                if (cols < 4 || cols > 7 || rows != cols + 2) report.Error($"tier {tier.id}: grid {cols}x{rows} (vertical grids 4x6, 5x7, 6x8, 7x9)");
                if (tier.GetOr(PenguinDifficulty.MinMoves, 1).max > cfg.maxMinMoves) report.Error($"tier {tier.id}: minimum above the cap of {cfg.maxMinMoves} moves");
                int penguins = tier.GetOr(PenguinDifficulty.Penguins, 1).min;
                if (penguins < 1 || penguins > PenguinLayout.MaxPenguins) report.Error($"tier {tier.id}: {penguins} penguins (1 or 2)");
                if (tier.GetOr(PenguinDifficulty.Blocks, 0).max > PenguinLayout.MaxBlocks) report.Error($"tier {tier.id}: more than {PenguinLayout.MaxBlocks} ice blocks");
                if (tier.GetOr(PenguinDifficulty.Fish, 0).max > PenguinLayout.MaxFish) report.Error($"tier {tier.id}: more than {PenguinLayout.MaxFish} fish");
            }
            if (cfg.scarfColors.Count < 2) report.Error("scarfColors needs 2 colors (one per penguin)");
            else if (ColorVision.DeltaE(cfg.scarfColors[0], cfg.scarfColors[1]) < 30f) report.Error("the two scarf colors are too close (delta E < 30)");
            var icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.IconLibraryPath);
            if (icons != null)
                foreach (var chapter in cfg.chapters)
                    if (!icons.Has(chapter.icon)) report.Error($"chapter icon '{chapter.icon}' missing in the IconLibrary");
            var themes = AssetDatabase.LoadAssetAtPath<ThemeConfig>(ConfigAssets.ThemeConfigPath);
            for (int i = 0; i < cfg.chapters.Count; i++)
            {
                var id = cfg.chapters[i].theme;
                if (string.IsNullOrEmpty(id)) report.Error($"chapter {i + 1}: no theme (PenguinConfig.chapters[{i}].theme)");
                else if (themes != null && !themes.Catalog.Has(id)) report.Error($"chapter {i + 1}: theme '{id}' does not exist in ThemeConfig");
            }
            var loc = LocalizationLoader.Load(Language.English);
            var keys = typeof(PenguinKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .Concat(cfg.chapters.Select(c => c.nameKey));
            foreach (var key in keys.Distinct())
            foreach (var lang in LanguageCodes.All)
                if (!loc.Has(lang, key)) report.Error($"key '{key}' used by Pino is missing in '{LanguageCodes.ToCode(lang)}'");
        }

        /// <summary>The problems of one level: (true = error, false = warning, message). Pure: runs on any thread.</summary>
        static System.Collections.Generic.List<(bool, string)> CheckLevel(PenguinConfig cfg, PenguinLevel level, int i)
        {
            var issues = new System.Collections.Generic.List<(bool, string)>();
            string id = $"level {i + 1}";
            if (level.number != i + 1) issues.Add((true, $"{id}: number is {level.number}"));
            int chapter = i / StoryBook.LevelsPerChapter + 1, index = i % StoryBook.LevelsPerChapter;
            if (level.chapter != chapter || level.index != index)
                issues.Add((true, $"{id}: saved as chapter {level.chapter} index {level.index}, expected chapter {chapter} index {index}"));
            PenguinLayout layout;
            try { layout = level.CreateLayout(); }
            catch (FormatException e)
            {
                issues.Add((true, $"{id}: cells not readable: {e.Message}"));
                return issues;
            }
            if (layout.Width != level.width || layout.Height != level.height)
                issues.Add((true, $"{id}: cells are {layout.Width}x{layout.Height}, data says {level.width}x{level.height}"));
            if (layout.IsSolved(layout.Start)) issues.Add((true, $"{id}: solved at the start"));
            var space = PenguinSpace.Explore(layout, layout.Start, cfg.maxStates);
            if (space.Capped)
            {
                issues.Add((true, $"{id}: more than {cfg.maxStates} reachable states"));
                return issues;
            }
            if (!space.Solvable)
            {
                issues.Add((true, $"{id}: no solution"));
                return issues;
            }
            if (space.MinMoves != level.minMoves) issues.Add((true, $"{id}: saved minimum moves {level.minMoves}, solver finds {space.MinMoves}"));
            if (space.Count != level.states) issues.Add((false, $"{id}: saved {level.states} states, solver finds {space.Count}"));
            var tier = cfg.storyDifficulty.ForLevel(level.number);
            if (tier == null) return issues;
            foreach (var problem in PenguinDifficulty.CheckTier(tier, layout, space.MinMoves)) issues.Add((true, $"{id} (tier {tier.id}): {problem}"));
            var settings = PenguinLevelTools.StorySettings(cfg, tier);
            foreach (var problem in PenguinGenerator.Problems(layout, space, settings)) issues.Add((true, $"{id} (tier {tier.id}): {problem}"));
            try
            {
                var r = PenguinGenerator.Generate(settings, level.seed, tier.id, level.number);
                if (r.Level.cells != level.cells)
                    issues.Add((false, $"{id}: the generator now makes a different level from seed {level.seed} (generator or table changed; regenerate if intended)"));
            }
            catch (InvalidOperationException e)
            {
                issues.Add((false, $"{id}: cannot regenerate from seed {level.seed}: {e.Message}"));
            }
            return issues;
        }
    }
}
