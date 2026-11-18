using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>
    /// Keys of the Traffic jam difficulty tiers (TJ-001). cols / rows: grid; minMoves: exact minimum; cars / trucks: the other vehicles
    /// (2 and 3 cells); obstacles: fixed barriers; targets: 1 (red) or 2 (red and blue); sideExits: target cars whose exit is not on top.
    /// </summary>
    public static class TrafficDifficulty
    {
        public const string Cols = "cols";
        public const string Rows = "rows";
        public const string MinMoves = "minMoves";
        public const string Cars = "cars";
        public const string Trucks = "trucks";
        public const string Obstacles = "obstacles";
        public const string Targets = "targets";
        public const string SideExits = "sideExits";

        static DifficultyRange R(string key, int min, int max) => new DifficultyRange(key, min, max);

        /// <summary>The vertical grids: 4x6, 5x7, 6x8, 7x9 (two more rows than columns).</summary>
        public static int RowsFor(int cols) => cols + 2;

        static DifficultyTier T(string id, int first, int last, int cols, int minLo, int minHi, int carLo, int carHi, int truckLo, int truckHi,
            int obsLo, int obsHi, int targets, int sideLo, int sideHi) =>
            DifficultyTable.Tier(id, first, last, R(Cols, cols, cols), R(Rows, RowsFor(cols), RowsFor(cols)), R(MinMoves, minLo, minHi),
                R(Cars, carLo, carHi), R(Trucks, truckLo, truckHi), R(Obstacles, obsLo, obsHi), R(Targets, targets, targets), R(SideExits, sideLo, sideHi));

        static DifficultyTier L(string id, int level, int cols, int minLo, int minHi, int carLo, int carHi, int truckLo, int truckHi,
            int obsLo, int obsHi, int targets, int sideLo, int sideHi) =>
            T(id, level, level, cols, minLo, minHi, carLo, carHi, truckLo, truckHi, obsLo, obsHi, targets, sideLo, sideHi);

        /// <summary>
        /// Default Story table (copied into TrafficJamConfig.asset when it is created; the asset is the one that counts), one tier per level,
        /// 7 chapters of 9 levels, one novelty per chapter, saw-tooth (the first level of a chapter is easier than the last of the previous):
        /// 1 parking (cars only, exit on top, 4x6 then 5x7); 2 market (trucks, 5x7); 3 city centre (6x8); 4 road works (obstacles, 6x8);
        /// 5 airport (red and blue target cars, 6x8); 6 crossroads (exits on other sides, 6x8 then 7x9); 7 rush hour (everything, 7x9).
        /// </summary>
        public static DifficultyTable Story() => new DifficultyTable
        {
            tiers =
            {
                //  id     lvl cols  min      cars    trucks  obst  tg side
                L("c1-1",  1, 4,  1,  2,  2,  2, 0, 0, 0, 0, 1, 0, 0),
                L("c1-2",  2, 4,  2,  3,  3,  3, 0, 0, 0, 0, 1, 0, 0),
                L("c1-3",  3, 4,  3,  4,  4,  4, 0, 0, 0, 0, 1, 0, 0),
                L("c1-4",  4, 4,  4,  5,  4,  5, 0, 0, 0, 0, 1, 0, 0),
                L("c1-5",  5, 4,  5,  6,  5,  6, 0, 0, 0, 0, 1, 0, 0),
                L("c1-6",  6, 5,  5,  6,  6,  7, 0, 0, 0, 0, 1, 0, 0),
                L("c1-7",  7, 5,  6,  7,  7,  8, 0, 0, 0, 0, 1, 0, 0),
                L("c1-8",  8, 5,  7,  8,  8,  9, 0, 0, 0, 0, 1, 0, 0),
                L("c1-9",  9, 5,  8, 10,  9, 10, 0, 0, 0, 0, 1, 0, 0),

                L("c2-1", 10, 5,  4,  5,  4,  6, 1, 1, 0, 0, 1, 0, 0),
                L("c2-2", 11, 5,  5,  6,  5,  6, 1, 1, 0, 0, 1, 0, 0),
                L("c2-3", 12, 5,  6,  7,  5,  7, 1, 2, 0, 0, 1, 0, 0),
                L("c2-4", 13, 5,  7,  8,  6,  7, 1, 2, 0, 0, 1, 0, 0),
                L("c2-5", 14, 5,  8,  9,  6,  8, 2, 2, 0, 0, 1, 0, 0),
                L("c2-6", 15, 5,  9, 10,  6,  8, 2, 2, 0, 0, 1, 0, 0),
                L("c2-7", 16, 5, 10, 11,  7,  8, 2, 3, 0, 0, 1, 0, 0),
                L("c2-8", 17, 5, 11, 12,  7,  9, 2, 3, 0, 0, 1, 0, 0),
                L("c2-9", 18, 5, 12, 14,  7,  9, 2, 3, 0, 0, 1, 0, 0),

                L("c3-1", 19, 6,  7,  8,  8, 10, 2, 3, 0, 0, 1, 0, 0),
                L("c3-2", 20, 6,  8,  9,  8, 10, 2, 3, 0, 0, 1, 0, 0),
                L("c3-3", 21, 6,  9, 10,  9, 11, 2, 3, 0, 0, 1, 0, 0),
                L("c3-4", 22, 6, 10, 11,  9, 11, 3, 3, 0, 0, 1, 0, 0),
                L("c3-5", 23, 6, 11, 12, 10, 12, 3, 3, 0, 0, 1, 0, 0),
                L("c3-6", 24, 6, 12, 13, 10, 12, 3, 4, 0, 0, 1, 0, 0),
                L("c3-7", 25, 6, 13, 14, 11, 12, 3, 4, 0, 0, 1, 0, 0),
                L("c3-8", 26, 6, 14, 15, 11, 13, 3, 4, 0, 0, 1, 0, 0),
                L("c3-9", 27, 6, 15, 17, 11, 13, 3, 4, 0, 0, 1, 0, 0),

                L("c4-1", 28, 6,  9, 10,  8, 10, 2, 3, 2, 2, 1, 0, 0),
                L("c4-2", 29, 6, 10, 11,  8, 10, 2, 3, 2, 3, 1, 0, 0),
                L("c4-3", 30, 6, 11, 12,  9, 11, 2, 3, 2, 3, 1, 0, 0),
                L("c4-4", 31, 6, 12, 13,  9, 11, 3, 3, 3, 4, 1, 0, 0),
                L("c4-5", 32, 6, 13, 14,  9, 11, 3, 3, 3, 4, 1, 0, 0),
                L("c4-6", 33, 6, 14, 15, 10, 12, 3, 4, 3, 4, 1, 0, 0),
                L("c4-7", 34, 6, 15, 16, 10, 12, 3, 4, 3, 5, 1, 0, 0),
                L("c4-8", 35, 6, 16, 17, 10, 12, 3, 4, 4, 5, 1, 0, 0),
                L("c4-9", 36, 6, 17, 19, 10, 12, 3, 4, 4, 5, 1, 0, 0),

                L("c5-1", 37, 6, 10, 11,  8, 10, 2, 3, 0, 1, 2, 0, 0),
                L("c5-2", 38, 6, 11, 12,  8, 10, 2, 3, 0, 1, 2, 0, 0),
                L("c5-3", 39, 6, 12, 13,  9, 11, 2, 3, 0, 2, 2, 0, 0),
                L("c5-4", 40, 6, 13, 14,  9, 11, 3, 3, 0, 2, 2, 0, 0),
                L("c5-5", 41, 6, 14, 15,  9, 11, 3, 3, 1, 2, 2, 0, 0),
                L("c5-6", 42, 6, 15, 16, 10, 12, 3, 4, 1, 2, 2, 0, 0),
                L("c5-7", 43, 6, 16, 17, 10, 12, 3, 4, 1, 3, 2, 0, 0),
                L("c5-8", 44, 6, 17, 18, 10, 12, 3, 4, 1, 3, 2, 0, 0),
                L("c5-9", 45, 6, 18, 20, 10, 12, 3, 4, 2, 3, 2, 0, 0),

                L("c6-1", 46, 6, 11, 12,  8, 10, 2, 3, 0, 1, 1, 1, 1),
                L("c6-2", 47, 6, 12, 13,  8, 10, 2, 3, 0, 1, 1, 1, 1),
                L("c6-3", 48, 6, 13, 14,  9, 11, 2, 3, 0, 2, 2, 1, 1),
                L("c6-4", 49, 6, 14, 15,  9, 11, 3, 3, 0, 2, 2, 1, 2),
                L("c6-5", 50, 6, 15, 16, 10, 12, 3, 4, 0, 2, 2, 1, 2),
                L("c6-6", 51, 7, 15, 16, 12, 13, 4, 5, 4, 6, 1, 1, 1),
                L("c6-7", 52, 7, 16, 17, 10, 11, 5, 6, 5, 7, 2, 1, 2),
                L("c6-8", 53, 7, 17, 18, 10, 11, 5, 6, 5, 7, 2, 1, 2),
                L("c6-9", 54, 7, 18, 20, 10, 11, 5, 6, 6, 8, 2, 2, 2),

                L("c7-1", 55, 7, 13, 14, 12, 13, 4, 5, 5, 7, 1, 0, 1),
                L("c7-2", 56, 7, 14, 15, 10, 11, 5, 6, 5, 7, 2, 0, 1),
                L("c7-3", 57, 7, 15, 16, 12, 13, 4, 5, 5, 7, 1, 1, 1),
                L("c7-4", 58, 7, 16, 17, 10, 11, 5, 6, 6, 8, 2, 1, 2),
                L("c7-5", 59, 7, 17, 18, 10, 11, 5, 6, 6, 8, 2, 0, 2),
                L("c7-6", 60, 7, 18, 19, 12, 13, 5, 5, 6, 8, 1, 1, 1),
                L("c7-7", 61, 7, 19, 20, 10, 11, 5, 6, 6, 8, 2, 1, 2),
                L("c7-8", 62, 7, 20, 21, 10, 11, 5, 6, 6, 8, 2, 1, 2),
                L("c7-9", 63, 7, 21, 23, 10, 11, 5, 6, 6, 8, 2, 2, 2),
            },
        };

        /// <summary>
        /// Default Challenge table (TJ-001, copied into TrafficJamConfig.asset): steps of 3 levels at the start, then 5; from 4x6 with a few
        /// cars to 7x9 with trucks, obstacles, two target cars and side exits. Saw-tooth: the minimum drops when a novelty arrives.
        /// After the last step the run stays on it.
        /// </summary>
        public static DifficultyTable Challenge() => new DifficultyTable
        {
            tiers =
            {
                //  id      levels  cols  min     cars    trucks  obst  tg side
                T("ch-1",   1,  3, 4,  2,  4,  3,  4, 0, 0, 0, 0, 1, 0, 0),
                T("ch-2",   4,  6, 4,  4,  6,  4,  6, 0, 0, 0, 0, 1, 0, 0),
                T("ch-3",   7,  9, 5,  5,  8,  6,  8, 0, 0, 0, 0, 1, 0, 0),
                T("ch-4",  10, 12, 5,  5,  8,  5,  7, 1, 2, 0, 0, 1, 0, 0),
                T("ch-5",  13, 15, 5,  8, 11,  6,  8, 2, 3, 0, 0, 1, 0, 0),
                T("ch-6",  16, 20, 6,  7, 10, 11, 12, 3, 4, 0, 0, 1, 0, 0),
                T("ch-7",  21, 25, 6, 10, 13, 12, 12, 4, 4, 0, 0, 1, 0, 0),
                T("ch-8",  26, 30, 6,  8, 11, 10, 11, 3, 4, 2, 3, 1, 0, 0),
                T("ch-9",  31, 35, 6, 11, 14, 11, 11, 4, 4, 3, 4, 1, 0, 0),
                T("ch-10", 36, 40, 6,  9, 12, 10, 10, 4, 4, 2, 3, 2, 0, 0),
                T("ch-11", 41, 45, 6, 10, 13, 10, 10, 4, 4, 3, 3, 2, 0, 0),
                T("ch-12", 46, 50, 6, 10, 13, 11, 11, 4, 4, 1, 2, 1, 1, 1),
                T("ch-13", 51, 55, 7, 10, 13, 13, 13, 5, 5, 7, 7, 1, 1, 1),
                T("ch-14", 56, 60, 7,  9, 12, 10, 10, 6, 6, 8, 8, 2, 1, 1),
                T("ch-15", 61, 65, 7,  9, 12, 10, 10, 6, 6, 8, 8, 2, 1, 2),
                T("ch-16", 66, 70, 7, 10, 13, 10, 10, 6, 6, 8, 8, 2, 2, 2),
            },
        };

        /// <summary>The values of a level that are outside its tier (empty = OK).</summary>
        public static List<string> CheckTier(DifficultyTier tier, TrafficLayout layout, int minMoves)
        {
            var problems = new List<string>();
            void Check(string key, int value)
            {
                if (tier.TryGet(key, out var r) && !r.Contains(value)) problems.Add($"{key} {value} outside {r.min}..{r.max}");
            }
            Check(Cols, layout.Width);
            Check(Rows, layout.Height);
            Check(MinMoves, minMoves);
            Check(Cars, layout.Count(VehicleKind.Car));
            Check(Trucks, layout.Count(VehicleKind.Truck));
            Check(Obstacles, layout.ObstacleCount);
            Check(Targets, layout.TargetCount);
            Check(SideExits, layout.SideExitCount);
            return problems;
        }
    }
}
