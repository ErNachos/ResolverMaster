using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// Keys of the plumber difficulty tiers. cols / rows: grid columns and rows (ErNa-005: rectangular grids); minMoves: solver minimum; path: pipe cells the water crosses in the
    /// optimal solution; tees / crosses: T and cross pieces on the whole board (on the route or as decoys); sinks: fountains;
    /// colors: water colors (1 = every source and sink blue, 2 = one blue and one orange pair).
    /// The layout follows sinks and colors: 1 sink = one route; 2 sinks, 1 color = a route split by a T; 2 colors = two routes.
    /// </summary>
    public static class PlumberDifficulty
    {
        public const string Cols = "cols";
        public const string Rows = "rows";
        public const string MinMoves = "minMoves";
        public const string Path = "path";
        public const string Tees = "tees";
        public const string Crosses = "crosses";
        public const string Sinks = "sinks";
        public const string Colors = "colors";
        /// <summary>ErNa-006: locked pieces on the grid.</summary>
        public const string Locked = "locked";
        /// <summary>ErNa-006: bridges on the grid (on the routes, where two of them cross).</summary>
        public const string Bridges = "bridges";
        /// <summary>ErNa-006: 1 = the level is impossible when its bridges are crosses (a bridge is needed), 0 = no requirement.</summary>
        public const string NeedBridge = "needBridge";

        static DifficultyRange R(string key, int min, int max) => new DifficultyRange(key, min, max);

        /// <summary>Grid of the old square side n (ErNa-004) in ErNa-005: one more row, two more from 5 up (3x4, 4x5, 5x7, 6x8).</summary>
        public static int RowsFor(int cols) => cols >= 5 ? cols + 2 : cols + 1;

        static DifficultyTier T(string id, int level, int cols, int minLo, int minHi, int pathLo, int pathHi, int teeLo, int teeHi, int sinks, int colors) =>
            DifficultyTable.Tier(id, level, level, R(Cols, cols, cols), R(Rows, RowsFor(cols), RowsFor(cols)), R(MinMoves, minLo, minHi), R(Path, pathLo, pathHi),
                R(Tees, teeLo, teeHi), R(Crosses, 0, 0), R(Sinks, sinks, sinks), R(Colors, colors, colors));

        /// <summary>
        /// A tier of chapters 4-9 (ErNa-006): <paramref name="colors"/> separate routes (one fountain each), decoy T and crosses, locked
        /// pieces, bridges (where routes cross) and whether a bridge must be needed.
        /// </summary>
        static DifficultyTier N(string id, int level, int cols, int colors, int minLo, int minHi, int pathLo, int pathHi, int tees, int crossLo, int crossHi,
            int lockLo, int lockHi, int bridgeLo, int bridgeHi, bool needBridge) =>
            DifficultyTable.Tier(id, level, level, R(Cols, cols, cols), R(Rows, RowsFor(cols), RowsFor(cols)), R(MinMoves, minLo, minHi), R(Path, pathLo, pathHi),
                R(Tees, 0, tees), R(Crosses, crossLo, crossHi), R(Sinks, colors, colors), R(Colors, colors, colors),
                R(Locked, lockLo, lockHi), R(Bridges, bridgeLo, bridgeHi), R(NeedBridge, needBridge ? 1 : 0, needBridge ? 1 : 0));

        /// <summary>
        /// Default Story table (copied into PlumberConfig.asset when it is created; the asset is the one that counts), one tier per level.
        /// Saw-tooth: the difficulty grows inside a chapter and drops at the start of the next one, where the novelty arrives.
        /// Grids (columns x rows): 3x4, 4x5, 5x7, 6x8 (the column below; rows from <see cref="RowsFor"/>).
        /// Chapter 1 (1-9): one route, levels 1-3 need 1/2/3 taps, T only as decoys from level 6.
        /// Chapter 2 (10-18): one source, two fountains, a T on the route; 10-11 easy.
        /// Chapter 3 (19-27): blue and orange routes; 19-20 easy, 6x8 from 23.
        /// ErNa-006, one novelty per chapter, each chapter starting easier than the end of the previous one:
        /// Chapter 4 (28-36, factory): 2 colors, a bridge needed in every level, decoy crosses in the last ones; 5x7 then 6x8.
        /// Chapter 5 (37-45, building site): 2 colors, locked pieces; a needed bridge in the last 3.
        /// Chapter 6 (46-54, harbour): 3 colors, no locked pieces, few bridges (never needed); 46-47 airy (short routes).
        /// Chapter 7 (55-63, mountain): mix of 2-3 colors, bridges, locked pieces, decoy crosses; 7x9 in the last 2.
        /// Chapter 8 (64-72, laboratory): 4 colors; 64-65 short separate routes, then bridges; 6x8 then 7x9.
        /// Chapter 9 (73-81, castle): final mix, 3-4 colors, everything, 7x9. Level 81: minimum 21-23, within the chapter 8 maximum (18) + 30%.
        /// </summary>
        public static DifficultyTable Story() => new DifficultyTable
        {
            tiers =
            {
                //  id      lvl cols min      path     tees   sinks colors
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

                //  id      lvl cols col  min      path    tee cross  locked  bridges need
                N("c4-1", 28, 5, 2,  3,  6,  5,  9, 0, 0, 0, 0, 0, 1, 1, true),
                N("c4-2", 29, 5, 2,  4,  7,  6, 10, 0, 0, 0, 0, 0, 1, 1, true),
                N("c4-3", 30, 5, 2,  6,  8,  7, 11, 1, 0, 0, 0, 0, 1, 1, true),
                N("c4-4", 31, 5, 2,  7,  9,  8, 12, 1, 0, 0, 0, 0, 1, 1, true),
                N("c4-5", 32, 6, 2,  8, 10,  9, 14, 1, 0, 0, 0, 0, 1, 2, true),
                N("c4-6", 33, 6, 2,  9, 11, 10, 15, 1, 0, 0, 0, 0, 1, 2, true),
                N("c4-7", 34, 6, 2, 10, 12, 11, 16, 1, 1, 1, 0, 0, 1, 2, true),
                N("c4-8", 35, 6, 2, 11, 13, 12, 17, 2, 1, 1, 0, 0, 1, 2, true),
                N("c4-9", 36, 6, 2, 12, 15, 13, 19, 2, 1, 2, 0, 0, 1, 2, true),

                N("c5-1", 37, 5, 2,  4,  7,  6, 10, 0, 0, 0, 2, 3, 0, 0, false),
                N("c5-2", 38, 5, 2,  5,  8,  7, 11, 0, 0, 0, 2, 3, 0, 0, false),
                N("c5-3", 39, 5, 2,  7,  9,  8, 12, 1, 0, 0, 3, 4, 0, 0, false),
                N("c5-4", 40, 5, 2,  8, 10,  9, 13, 1, 0, 0, 3, 4, 0, 0, false),
                N("c5-5", 41, 6, 2,  9, 11, 10, 15, 1, 0, 0, 3, 5, 0, 0, false),
                N("c5-6", 42, 6, 2, 10, 12, 11, 16, 1, 0, 0, 4, 5, 0, 0, false),
                N("c5-7", 43, 6, 2, 11, 13, 12, 17, 1, 0, 0, 4, 6, 1, 1, true),
                N("c5-8", 44, 6, 2, 12, 14, 13, 18, 2, 0, 0, 4, 6, 1, 1, true),
                N("c5-9", 45, 6, 2, 13, 16, 14, 20, 2, 0, 0, 5, 6, 1, 2, true),

                N("c6-1", 46, 6, 3,  3,  6,  5,  9, 0, 0, 0, 0, 0, 0, 0, false),
                N("c6-2", 47, 6, 3,  5,  7,  6, 10, 0, 0, 0, 0, 0, 0, 0, false),
                N("c6-3", 48, 6, 3,  7,  9,  8, 13, 1, 0, 0, 0, 0, 0, 0, false),
                N("c6-4", 49, 6, 3,  8, 10,  9, 14, 1, 0, 0, 0, 0, 0, 1, false),
                N("c6-5", 50, 6, 3,  9, 11, 10, 15, 1, 0, 0, 0, 0, 0, 0, false),
                N("c6-6", 51, 6, 3, 10, 12, 11, 16, 1, 0, 0, 0, 0, 0, 1, false),
                N("c6-7", 52, 6, 3, 11, 13, 12, 17, 2, 0, 0, 0, 0, 0, 0, false),
                N("c6-8", 53, 6, 3, 12, 14, 13, 18, 2, 0, 0, 0, 0, 0, 1, false),
                N("c6-9", 54, 6, 3, 13, 16, 14, 20, 2, 0, 0, 0, 0, 0, 1, false),

                N("c7-1", 55, 6, 2,  7,  9,  8, 12, 1, 0, 0, 2, 2, 1, 1, true),
                N("c7-2", 56, 6, 3,  8, 10,  9, 14, 1, 1, 1, 0, 0, 0, 0, false),
                N("c7-3", 57, 6, 2, 10, 12, 11, 16, 1, 1, 1, 3, 3, 1, 1, true),
                N("c7-4", 58, 6, 3, 11, 13, 12, 17, 1, 0, 0, 2, 2, 0, 1, false),
                N("c7-5", 59, 6, 2, 12, 14, 13, 18, 2, 1, 1, 0, 0, 1, 2, true),
                N("c7-6", 60, 6, 3, 13, 15, 14, 19, 2, 0, 0, 3, 3, 1, 1, true),
                N("c7-7", 61, 6, 3, 14, 16, 15, 20, 2, 1, 1, 2, 3, 1, 1, true),
                N("c7-8", 62, 7, 3, 15, 17, 16, 22, 2, 1, 1, 3, 3, 1, 1, true),
                N("c7-9", 63, 7, 3, 16, 19, 17, 24, 2, 1, 1, 3, 4, 1, 2, true),

                N("c8-1", 64, 6, 4,  5,  8,  6, 10, 0, 0, 0, 0, 0, 0, 0, false),
                N("c8-2", 65, 6, 4,  7,  9,  8, 12, 0, 0, 0, 0, 0, 0, 0, false),
                N("c8-3", 66, 6, 4,  9, 11, 10, 15, 1, 0, 0, 0, 0, 1, 1, false),
                N("c8-4", 67, 6, 4, 10, 12, 11, 16, 1, 0, 0, 0, 0, 1, 1, true),
                N("c8-5", 68, 7, 4, 12, 14, 13, 18, 1, 0, 0, 0, 0, 1, 1, true),
                N("c8-6", 69, 7, 4, 13, 15, 14, 20, 1, 0, 0, 0, 0, 1, 2, true),
                N("c8-7", 70, 7, 4, 14, 17, 16, 22, 2, 0, 0, 0, 0, 1, 2, true),
                N("c8-8", 71, 7, 4, 16, 19, 18, 24, 2, 0, 0, 0, 0, 1, 2, true),
                N("c8-9", 72, 7, 4, 18, 21, 20, 26, 2, 0, 0, 0, 0, 1, 2, true),

                N("c9-1", 73, 7, 3, 10, 12, 11, 16, 1, 1, 1, 2, 2, 1, 1, true),
                N("c9-2", 74, 7, 4, 11, 13, 12, 17, 1, 0, 0, 0, 0, 1, 1, true),
                N("c9-3", 75, 7, 3, 13, 15, 14, 19, 1, 1, 1, 3, 3, 1, 1, true),
                N("c9-4", 76, 7, 4, 14, 16, 15, 21, 2, 0, 0, 2, 2, 1, 1, true),
                N("c9-5", 77, 7, 3, 16, 18, 17, 23, 2, 1, 1, 3, 3, 1, 2, true),
                N("c9-6", 78, 7, 4, 17, 19, 18, 24, 2, 1, 1, 3, 3, 1, 2, true),
                N("c9-7", 79, 7, 4, 18, 21, 19, 25, 2, 1, 1, 3, 4, 1, 2, true),
                N("c9-8", 80, 7, 4, 20, 23, 21, 27, 2, 1, 1, 3, 4, 1, 2, true),
                N("c9-9", 81, 7, 4, 21, 23, 21, 28, 2, 1, 2, 4, 4, 2, 2, true),
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
            Check(Cols, grid.Width);
            Check(Rows, grid.Height);
            Check(MinMoves, minMoves);
            Check(Path, pathLength);
            Check(Tees, grid.Count(PieceKind.Tee));
            Check(Crosses, grid.Count(PieceKind.Cross));
            Check(Sinks, grid.Count(PieceKind.Sink));
            Check(Colors, grid.ColorCount());
            Check(Locked, grid.CountLocked());
            Check(Bridges, grid.Count(PieceKind.Bridge));
            return problems;
        }
    }
}
