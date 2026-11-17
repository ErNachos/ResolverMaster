using System;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>Star thresholds, edited in the Core puzzle config (Resources/ResolverMaster/PuzzleConfig.asset).</summary>
    [Serializable]
    public sealed class StarRules
    {
        /// <summary>2 stars up to minimum + this percentage of the minimum (rounded down)...</summary>
        public int twoStarExtraPercent = 30;
        /// <summary>...but always at least this many extra moves.</summary>
        public int twoStarMinExtra = 2;
        /// <summary>ErNa-007: the most stars of a level solved with the help of a hint (light bulb).</summary>
        public int maxStarsWithHint = 2;
    }

    /// <summary>3 stars at the minimum, 2 stars up to minimum + max(30%, 2), 1 star for any other solution.</summary>
    public static class StarRating
    {
        public const int MaxStars = 3;

        /// <summary>Highest number of moves that still gives 2 stars.</summary>
        public static int TwoStarLimit(int minMoves, StarRules rules) =>
            minMoves + Math.Max(rules.twoStarMinExtra, minMoves * rules.twoStarExtraPercent / 100);

        /// <summary>Stars of a solved level; with a hint used, at most <see cref="StarRules.maxStarsWithHint"/>.</summary>
        public static int Compute(int moves, int minMoves, StarRules rules, bool hintUsed = false)
        {
            int stars = moves <= minMoves ? 3 : moves <= TwoStarLimit(minMoves, rules) ? 2 : 1;
            return hintUsed ? Math.Min(stars, Math.Max(1, rules.maxStarsWithHint)) : stars;
        }
    }
}
