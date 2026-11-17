using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// Keys of the plumber difficulty tiers. size: grid side; minMoves: solver minimum; path: pipe cells of the optimal route;
    /// tees / crosses: how many T and cross pieces are on the board (0..0 = none).
    /// </summary>
    public static class PlumberDifficulty
    {
        public const string Size = "size";
        public const string MinMoves = "minMoves";
        public const string Path = "path";
        public const string Tees = "tees";
        public const string Crosses = "crosses";

        static DifficultyRange R(string key, int min, int max) => new DifficultyRange(key, min, max);

        /// <summary>
        /// Default Story chapter 1 (copied into PlumberConfig.asset when it is created; the asset is the one that counts).
        /// 1-3: 3x3, straights and curves, 1/2/3 moves. 4-7: 4x4, slow growth. 8-10: 5x5, T pieces appear, longer winding route.
        /// </summary>
        public static DifficultyTable StoryChapter1() => new DifficultyTable
        {
            tiers =
            {
                DifficultyTable.Tier("c1-1", 1, 1, R(Size, 3, 3), R(MinMoves, 1, 1), R(Path, 1, 3), R(Tees, 0, 0), R(Crosses, 0, 0)),
                DifficultyTable.Tier("c1-2", 2, 2, R(Size, 3, 3), R(MinMoves, 2, 2), R(Path, 2, 4), R(Tees, 0, 0), R(Crosses, 0, 0)),
                DifficultyTable.Tier("c1-3", 3, 3, R(Size, 3, 3), R(MinMoves, 3, 3), R(Path, 3, 5), R(Tees, 0, 0), R(Crosses, 0, 0)),
                DifficultyTable.Tier("c1-4", 4, 5, R(Size, 4, 4), R(MinMoves, 3, 4), R(Path, 4, 7), R(Tees, 0, 0), R(Crosses, 0, 0)),
                DifficultyTable.Tier("c1-5", 6, 7, R(Size, 4, 4), R(MinMoves, 5, 6), R(Path, 5, 9), R(Tees, 0, 0), R(Crosses, 0, 0)),
                DifficultyTable.Tier("c1-6", 8, 8, R(Size, 5, 5), R(MinMoves, 7, 8), R(Path, 9, 15), R(Tees, 1, 3), R(Crosses, 0, 0)),
                DifficultyTable.Tier("c1-7", 9, 9, R(Size, 5, 5), R(MinMoves, 8, 9), R(Path, 9, 15), R(Tees, 1, 3), R(Crosses, 0, 0)),
                DifficultyTable.Tier("c1-8", 10, 10, R(Size, 5, 5), R(MinMoves, 9, 10), R(Path, 10, 15), R(Tees, 1, 3), R(Crosses, 0, 0)),
            },
        };

        /// <summary>The values of <paramref name="level"/> that are outside its tier (empty list = OK).</summary>
        public static System.Collections.Generic.List<string> CheckTier(DifficultyTier tier, PipeGrid grid, int minMoves, int pathLength)
        {
            var problems = new System.Collections.Generic.List<string>();
            void Check(string key, int value)
            {
                if (tier.TryGet(key, out var r) && !r.Contains(value)) problems.Add($"{key} {value} outside {r.min}..{r.max}");
            }
            if (grid.Width != grid.Height) problems.Add($"grid {grid.Width}x{grid.Height} is not square");
            Check(Size, grid.Width);
            Check(MinMoves, minMoves);
            Check(Path, pathLength);
            Check(Tees, grid.Count(PieceKind.Tee));
            Check(Crosses, grid.Count(PieceKind.Cross));
            return problems;
        }
    }
}
