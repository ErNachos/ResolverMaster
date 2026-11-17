using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// Keys of the plumber difficulty tiers. size: grid side; minMoves: solver minimum; path: pipe cells the water crosses in the
    /// optimal solution; tees / crosses: T and cross pieces on the whole board (on the route or as decoys); sinks: fountains;
    /// colors: water colors (1 = every source and sink blue, 2 = one blue and one orange pair).
    /// The layout follows sinks and colors: 1 sink = one route; 2 sinks, 1 color = a route split by a T; 2 colors = two routes.
    /// </summary>
    public static class PlumberDifficulty
    {
        public const string Size = "size";
        public const string MinMoves = "minMoves";
        public const string Path = "path";
        public const string Tees = "tees";
        public const string Crosses = "crosses";
        public const string Sinks = "sinks";
        public const string Colors = "colors";

        static DifficultyRange R(string key, int min, int max) => new DifficultyRange(key, min, max);

        static DifficultyTier T(string id, int level, int size, int minLo, int minHi, int pathLo, int pathHi, int teeLo, int teeHi, int sinks, int colors) =>
            DifficultyTable.Tier(id, level, level, R(Size, size, size), R(MinMoves, minLo, minHi), R(Path, pathLo, pathHi),
                R(Tees, teeLo, teeHi), R(Crosses, 0, 0), R(Sinks, sinks, sinks), R(Colors, colors, colors));

        /// <summary>
        /// Default Story table (copied into PlumberConfig.asset when it is created; the asset is the one that counts), one tier per level.
        /// Saw-tooth: the difficulty grows inside a chapter and drops at the start of the next one, where the novelty arrives.
        /// Chapter 1 (1-9): one route, levels 1-3 need 1/2/3 taps, T only as decoys from level 6.
        /// Chapter 2 (10-18): one source, two fountains, a T on the route; 10-11 easy.
        /// Chapter 3 (19-27): blue and orange routes; 19-20 easy, 6x6 from 23.
        /// </summary>
        public static DifficultyTable Story() => new DifficultyTable
        {
            tiers =
            {
                //  id      lvl size min      path     tees   sinks colors
                T("c1-1",  1, 3,  1,  1,  1,  3, 0, 0, 1, 1),
                T("c1-2",  2, 3,  2,  2,  2,  4, 0, 0, 1, 1),
                T("c1-3",  3, 3,  3,  3,  3,  5, 0, 0, 1, 1),
                T("c1-4",  4, 4,  3,  4,  4,  7, 0, 0, 1, 1),
                T("c1-5",  5, 4,  4,  5,  5,  8, 0, 0, 1, 1),
                T("c1-6",  6, 4,  5,  6,  6,  9, 0, 1, 1, 1),
                T("c1-7",  7, 5,  6,  7,  7, 12, 1, 1, 1, 1),
                T("c1-8",  8, 5,  7,  8,  8, 13, 1, 2, 1, 1),
                T("c1-9",  9, 5,  8, 10,  9, 15, 1, 2, 1, 1),

                T("c2-1", 10, 4,  2,  3,  3,  6, 1, 1, 2, 1),
                T("c2-2", 11, 4,  3,  4,  4,  7, 1, 1, 2, 1),
                T("c2-3", 12, 4,  4,  5,  5,  8, 1, 1, 2, 1),
                T("c2-4", 13, 4,  5,  6,  6,  9, 1, 2, 2, 1),
                T("c2-5", 14, 5,  6,  7,  7, 11, 1, 2, 2, 1),
                T("c2-6", 15, 5,  7,  8,  8, 12, 1, 2, 2, 1),
                T("c2-7", 16, 5,  8,  9,  9, 13, 1, 2, 2, 1),
                T("c2-8", 17, 5,  9, 10, 10, 14, 1, 2, 2, 1),
                T("c2-9", 18, 5, 10, 12, 11, 16, 1, 3, 2, 1),

                T("c3-1", 19, 5,  2,  3,  2,  6, 0, 0, 2, 2),
                T("c3-2", 20, 5,  3,  4,  3,  7, 0, 0, 2, 2),
                T("c3-3", 21, 5,  5,  6,  6, 10, 0, 1, 2, 2),
                T("c3-4", 22, 5,  6,  7,  7, 11, 0, 1, 2, 2),
                T("c3-5", 23, 6,  7,  8,  8, 13, 0, 1, 2, 2),
                T("c3-6", 24, 6,  8,  9,  9, 14, 0, 1, 2, 2),
                T("c3-7", 25, 6,  9, 10, 10, 15, 1, 2, 2, 2),
                T("c3-8", 26, 6, 10, 11, 11, 16, 1, 2, 2, 2),
                T("c3-9", 27, 6, 11, 13, 12, 18, 1, 2, 2, 2),
            },
        };

        /// <summary>The values of a level that are outside its tier (empty list = OK).</summary>
        public static List<string> CheckTier(DifficultyTier tier, PipeGrid grid, int minMoves, int pathLength)
        {
            var problems = new List<string>();
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
            Check(Sinks, grid.Count(PieceKind.Sink));
            Check(Colors, grid.ColorCount());
            return problems;
        }
    }
}
