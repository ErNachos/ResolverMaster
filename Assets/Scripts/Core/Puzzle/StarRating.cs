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
    }

    /// <summary>3 stars at the minimum, 2 stars up to minimum + max(30%, 2), 1 star for any other solution.</summary>
    public static class StarRating
    {
        public const int MaxStars = 3;

        /// <summary>Highest number of moves that still gives 2 stars.</summary>
        public static int TwoStarLimit(int minMoves, StarRules rules) =>
            minMoves + Math.Max(rules.twoStarMinExtra, minMoves * rules.twoStarExtraPercent / 100);

        /// <summary>Stars of a solved level.</summary>
        public static int Compute(int moves, int minMoves, StarRules rules)
        {
            if (moves <= minMoves) return 3;
            return moves <= TwoStarLimit(minMoves, rules) ? 2 : 1;
        }
    }
}
