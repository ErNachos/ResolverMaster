using System;
using System.Linq;
using System.Reflection;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Games.Plumber;
using UnityEditor;
using UnityEngine;

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
    /// Plumber parameters and Story levels (ErNa-003 rules): water colors with a sink halo each, chapter icons and names and texts in every language;
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
        }

        static void CheckConfig(PlumberConfig cfg, ValidationReport report)
        {
            int last = cfg.storyDifficulty.LastLevel;
            if (last == 0) report.Error("Story difficulty table is empty");
            for (int n = 1; n <= last; n++)
                if (cfg.storyDifficulty.ForLevel(n) == null) report.Error($"no difficulty tier covers level {n}");
            if (cfg.waterColors.Count < 2) report.Error($"waterColors has {cfg.waterColors.Count} colors, the Story uses 2");
            if (cfg.sinkHaloColors.Count < cfg.waterColors.Count) report.Error("every water color needs a sink halo color in sinkHaloColors");

            var icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.IconLibraryPath);
            if (icons != null)
            {
                foreach (var chapter in cfg.chapters)
                    if (!icons.Has(chapter.icon)) report.Error($"chapter icon '{chapter.icon}' missing in the IconLibrary");
                if (!icons.Has("faucet")) report.Error("icon 'faucet' (Open the water button) missing in the IconLibrary");
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
