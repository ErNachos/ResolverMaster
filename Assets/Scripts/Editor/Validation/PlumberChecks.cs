using System;
using ResolverMaster.Core.Puzzle;
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
    /// Plumber Story levels: each level parses, is solvable, its saved minimum moves match the solver, it is not solved at the start,
    /// it respects the values of its difficulty tier, and (warning) the generator still produces it from its seed.
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
            int last = cfg.story.LastLevel;
            if (last == 0) report.Error("Story difficulty table is empty");
            for (int n = 1; n <= last; n++)
                if (cfg.story.ForLevel(n) == null) report.Error($"no difficulty tier covers level {n}");
            if (cfg.waterColors.Count == 0) report.Error("waterColors is empty");

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
            if (set.levels.Count != last) report.Error($"{set.levels.Count} Story levels saved, the difficulty table has {last}");

            for (int i = 0; i < set.levels.Count; i++)
            {
                var level = set.levels[i];
                string id = $"level {i + 1}";
                if (level.number != i + 1) report.Error($"{id}: number is {level.number}");
                PipeGrid grid;
                try { grid = level.CreateGrid(); }
                catch (FormatException e)
                {
                    report.Error($"{id}: cells not readable: {e.Message}");
                    continue;
                }
                if (grid.Width != level.width || grid.Height != level.height)
                    report.Error($"{id}: cells are {grid.Width}x{grid.Height}, data says {level.width}x{level.height}");
                if (grid.MaxColor() >= cfg.waterColors.Count) report.Error($"{id}: uses water color {grid.MaxColor()}, only {cfg.waterColors.Count} defined");

                var sol = PlumberSolver.Solve(grid);
                if (!sol.Solvable)
                {
                    report.Error($"{id}: not solvable ({sol.Reason})");
                    continue;
                }
                if (sol.MinMoves != level.minMoves) report.Error($"{id}: saved minimum moves {level.minMoves}, solver finds {sol.MinMoves}");
                if (WaterFlow.Compute(grid).AllSinksReached) report.Error($"{id}: already solved at the start");
                if (sol.PathLength != level.path) report.Warning($"{id}: saved route length {level.path}, solver finds {sol.PathLength}");

                var tier = cfg.story.ForLevel(level.number);
                if (tier == null) continue;
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
}
