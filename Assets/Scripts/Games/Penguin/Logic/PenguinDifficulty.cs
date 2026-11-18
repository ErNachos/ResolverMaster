using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>
    /// Keys of the Pingù difficulty tiers (PG-001). cols / rows: grid; minMoves: exact minimum; rocks, snow, holes (water cells inside the
    /// grid), waterEdges (runs of water on the border), fish, blocks, penguins (1 or 2). Checks of the novelty (0 or 1, or a count):
    /// needSnow (the level is longer or impossible with the snow turned to ice), danger (states of an optimal solution from which a swipe
    /// falls into the water, at least), needPush (impossible or longer with frozen blocks), needFill (impossible or longer when blocks
    /// cannot fill holes), fishOnPath (teaching: the fish lie on the way of the solution without fish, same minimum), useless (at most this
    /// many rocks, snow cells, holes, water runs or blocks never touched in any reachable state).
    /// </summary>
    public static class PenguinDifficulty
    {
        public const string Cols = "cols";
        public const string Rows = "rows";
        public const string MinMoves = "minMoves";
        public const string Rocks = "rocks";
        public const string Snow = "snow";
        public const string Holes = "holes";
        public const string WaterEdges = "waterEdges";
        public const string Fish = "fish";
        public const string Blocks = "blocks";
        public const string Penguins = "penguins";
        public const string NeedSnow = "needSnow";
        public const string Danger = "danger";
        public const string NeedPush = "needPush";
        public const string NeedFill = "needFill";
        public const string FishOnPath = "fishOnPath";
        public const string Useless = "useless";

        /// <summary>The vertical grids: 4x6, 5x7, 6x8, 7x9 (two more rows than columns).</summary>
        public static int RowsFor(int cols) => cols + 2;

        static DifficultyRange R(string key, int min, int max) => new DifficultyRange(key, min, max);

        /// <summary>What a tier puts in a level (ranges) and asks of it (checks).</summary>
        public struct Spec
        {
            public int Cols, MinLo, MinHi, RockLo, RockHi, SnowLo, SnowHi, HoleLo, HoleHi, EdgeLo, EdgeHi, FishLo, FishHi, BlockLo, BlockHi, Penguins;
            public int NeedSnow, Danger, NeedPush, NeedFill, FishOnPath, Useless;
        }

        static DifficultyTier Tier(string id, int first, int last, Spec s) =>
            DifficultyTable.Tier(id, first, last,
                R(Cols, s.Cols, s.Cols), R(Rows, RowsFor(s.Cols), RowsFor(s.Cols)), R(MinMoves, s.MinLo, s.MinHi),
                R(Rocks, s.RockLo, s.RockHi), R(Snow, s.SnowLo, s.SnowHi), R(Holes, s.HoleLo, s.HoleHi), R(WaterEdges, s.EdgeLo, s.EdgeHi),
                R(Fish, s.FishLo, s.FishHi), R(Blocks, s.BlockLo, s.BlockHi), R(Penguins, s.Penguins, s.Penguins),
                R(NeedSnow, s.NeedSnow, s.NeedSnow), R(Danger, s.Danger, s.Danger), R(NeedPush, s.NeedPush, s.NeedPush),
                R(NeedFill, s.NeedFill, s.NeedFill), R(FishOnPath, s.FishOnPath, s.FishOnPath), R(Useless, s.Useless, s.Useless));

        /// <summary>
        /// Shorthand of a Story tier (one level): grid, minimum, rocks, snow, holes, water runs, fish, blocks, penguins, then the checks.
        /// </summary>
        static DifficultyTier L(string id, int level, int cols, int minLo, int minHi, int rockLo, int rockHi, int snowLo, int snowHi,
            int holeLo, int holeHi, int edgeLo, int edgeHi, int fishLo, int fishHi, int blocks, int penguins,
            int needSnow = 0, int danger = 0, int needPush = 0, int needFill = 0, int fishOnPath = 0, int useless = 2) =>
            Tier(id, level, level, new Spec
            {
                Cols = cols, MinLo = minLo, MinHi = minHi, RockLo = rockLo, RockHi = rockHi, SnowLo = snowLo, SnowHi = snowHi,
                HoleLo = holeLo, HoleHi = holeHi, EdgeLo = edgeLo, EdgeHi = edgeHi, FishLo = fishLo, FishHi = fishHi,
                BlockLo = blocks, BlockHi = blocks, Penguins = penguins,
                NeedSnow = needSnow, Danger = danger, NeedPush = needPush, NeedFill = needFill, FishOnPath = fishOnPath, Useless = useless,
            });

        static DifficultyTier C(string id, int first, int last, int cols, int minLo, int minHi, int rockLo, int rockHi, int snowLo, int snowHi,
            int holeLo, int holeHi, int edgeLo, int edgeHi, int fishLo, int fishHi, int blocks, int penguins,
            int needSnow = 0, int danger = 0, int needPush = 0, int needFill = 0, int useless = 3) =>
            Tier(id, first, last, new Spec
            {
                Cols = cols, MinLo = minLo, MinHi = minHi, RockLo = rockLo, RockHi = rockHi, SnowLo = snowLo, SnowHi = snowHi,
                HoleLo = holeLo, HoleHi = holeHi, EdgeLo = edgeLo, EdgeHi = edgeHi, FishLo = fishLo, FishHi = fishHi,
                BlockLo = blocks, BlockHi = blocks, Penguins = penguins,
                NeedSnow = needSnow, Danger = danger, NeedPush = needPush, NeedFill = needFill, Useless = useless,
            });

        /// <summary>
        /// Default Story table (copied into PenguinConfig.asset when it is created; the asset is the one that counts), one tier per level,
        /// 7 chapters of 9 levels, one novelty per chapter, saw-tooth (the first level of a chapter is easier than the last of the
        /// previous one; inside a chapter the ranges never go down, so the minimum never drops):
        /// 1 frozen lake (ice, rocks, igloo, all wall; 4x6 then 5x7); 2 pack ice (fish, 5x7); 3 blizzard (snow, 5x7 then 6x8);
        /// 4 thaw (water holes and water on the border, 6x8); 5 glacier (ice blocks, also to fill holes, 6x8); 6 pole (two penguins,
        /// 6x8 then 7x9); 7 aurora (everything, 7x9).
        /// </summary>
        public static DifficultyTable Story() => new DifficultyTable
        {
            tiers =
            {
                //  id     lvl cols  min    rocks  snow  holes edges fish  bl pg
                L("c1-1",  1, 4,  1,  2,  2,  3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                L("c1-2",  2, 4,  2,  3,  2,  4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                L("c1-3",  3, 4,  3,  3,  3,  4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                L("c1-4",  4, 4,  3,  4,  3,  5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                L("c1-5",  5, 4,  4,  4,  3,  5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                L("c1-6",  6, 5,  4,  5,  4,  6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                L("c1-7",  7, 5,  5,  5,  4,  6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                L("c1-8",  8, 5,  5,  6,  5,  7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                L("c1-9",  9, 5,  6,  7,  5,  7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),

                L("c2-1", 10, 5,  2,  3,  3,  5, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, fishOnPath: 1),
                L("c2-2", 11, 5,  3,  3,  3,  5, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, fishOnPath: 1),
                L("c2-3", 12, 5,  3,  4,  3,  5, 0, 0, 0, 0, 0, 0, 2, 2, 0, 1, fishOnPath: 1),
                L("c2-4", 13, 5,  4,  5,  4,  6, 0, 0, 0, 0, 0, 0, 1, 2, 0, 1),
                L("c2-5", 14, 5,  5,  5,  4,  6, 0, 0, 0, 0, 0, 0, 2, 2, 0, 1),
                L("c2-6", 15, 5,  5,  6,  4,  6, 0, 0, 0, 0, 0, 0, 2, 2, 0, 1),
                L("c2-7", 16, 5,  6,  7,  4,  7, 0, 0, 0, 0, 0, 0, 2, 3, 0, 1),
                L("c2-8", 17, 5,  7,  8,  5,  7, 0, 0, 0, 0, 0, 0, 2, 3, 0, 1),
                L("c2-9", 18, 5,  8,  9,  5,  7, 0, 0, 0, 0, 0, 0, 3, 3, 0, 1),

                L("c3-1", 19, 5,  3,  4,  3,  5, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, needSnow: 1),
                L("c3-2", 20, 5,  4,  4,  3,  5, 1, 2, 0, 0, 0, 0, 0, 0, 0, 1, needSnow: 1),
                L("c3-3", 21, 5,  4,  5,  3,  5, 2, 2, 0, 0, 0, 0, 0, 1, 0, 1, needSnow: 1),
                L("c3-4", 22, 6,  5,  6,  4,  6, 2, 3, 0, 0, 0, 0, 0, 1, 0, 1, needSnow: 1),
                L("c3-5", 23, 6,  6,  6,  4,  7, 2, 3, 0, 0, 0, 0, 1, 1, 0, 1, needSnow: 1),
                L("c3-6", 24, 6,  6,  7,  5,  7, 2, 3, 0, 0, 0, 0, 1, 2, 0, 1, needSnow: 1),
                L("c3-7", 25, 6,  7,  8,  5,  8, 3, 4, 0, 0, 0, 0, 1, 2, 0, 1, needSnow: 1),
                L("c3-8", 26, 6,  8,  9,  5,  8, 3, 4, 0, 0, 0, 0, 1, 2, 0, 1, needSnow: 1),
                L("c3-9", 27, 6,  9, 10,  6,  8, 3, 4, 0, 0, 0, 0, 2, 2, 0, 1, needSnow: 1),

                L("c4-1", 28, 6,  3,  4,  4,  6, 0, 0, 1, 2, 0, 1, 0, 0, 0, 1, danger: 1),
                L("c4-2", 29, 6,  4,  5,  4,  6, 0, 0, 1, 2, 1, 1, 0, 0, 0, 1, danger: 1),
                L("c4-3", 30, 6,  5,  5,  4,  6, 0, 1, 2, 3, 1, 1, 0, 1, 0, 1, danger: 1),
                L("c4-4", 31, 6,  5,  6,  4,  7, 0, 1, 2, 3, 1, 2, 0, 1, 0, 1, danger: 1),
                L("c4-5", 32, 6,  6,  7,  4,  7, 0, 1, 2, 3, 1, 2, 1, 1, 0, 1, danger: 2),
                L("c4-6", 33, 6,  7,  8,  5,  7, 0, 2, 2, 4, 1, 2, 1, 2, 0, 1, danger: 2),
                L("c4-7", 34, 6,  8,  9,  5,  8, 0, 2, 3, 4, 1, 2, 1, 2, 0, 1, danger: 2),
                L("c4-8", 35, 6,  9, 10,  5,  8, 1, 2, 3, 4, 2, 2, 1, 2, 0, 1, danger: 2),
                L("c4-9", 36, 6, 10, 11,  5,  8, 1, 2, 3, 4, 2, 3, 2, 2, 0, 1, danger: 2),

                L("c5-1", 37, 6,  3,  4,  4,  6, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, needPush: 1),
                L("c5-2", 38, 6,  4,  5,  4,  6, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, needPush: 1),
                L("c5-3", 39, 6,  5,  6,  4,  6, 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, needPush: 1, needFill: 1),
                L("c5-4", 40, 6,  6,  7,  4,  7, 0, 1, 1, 2, 0, 1, 0, 1, 1, 1, needPush: 1, needFill: 1),
                L("c5-5", 41, 6,  7,  8,  4,  7, 0, 1, 1, 2, 0, 1, 0, 1, 1, 1, needPush: 1, needFill: 1),
                L("c5-6", 42, 6,  8,  9,  5,  7, 0, 1, 1, 2, 0, 1, 1, 1, 2, 1, needPush: 1),
                L("c5-7", 43, 6,  9, 10,  5,  8, 0, 2, 1, 2, 0, 1, 1, 2, 2, 1, needPush: 1, needFill: 1),
                L("c5-8", 44, 6, 10, 11,  5,  8, 0, 2, 1, 3, 1, 1, 1, 2, 2, 1, needPush: 1, needFill: 1),
                L("c5-9", 45, 6, 11, 12,  5,  8, 0, 2, 2, 3, 1, 2, 1, 2, 2, 1, needPush: 1, needFill: 1),

                L("c6-1", 46, 6,  3,  4,  4,  6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2),
                L("c6-2", 47, 6,  4,  5,  4,  6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2),
                L("c6-3", 48, 6,  5,  6,  4,  7, 0, 1, 0, 0, 0, 0, 0, 1, 0, 2),
                L("c6-4", 49, 6,  6,  7,  5,  7, 0, 1, 0, 1, 0, 1, 0, 1, 0, 2),
                L("c6-5", 50, 6,  7,  7,  5,  8, 0, 1, 0, 1, 0, 1, 1, 1, 0, 2),
                L("c6-6", 51, 7,  7,  8,  6,  9, 0, 2, 0, 2, 0, 1, 1, 2, 0, 2),
                L("c6-7", 52, 7,  8,  9,  6,  9, 0, 2, 0, 2, 1, 1, 1, 2, 1, 2),
                L("c6-8", 53, 7,  9, 10,  6,  9, 1, 2, 1, 2, 1, 1, 1, 2, 1, 2),
                L("c6-9", 54, 7, 10, 12,  6,  9, 1, 2, 1, 2, 1, 2, 2, 2, 1, 2),

                L("c7-1", 55, 7,  7,  8,  6,  9, 1, 2, 1, 2, 1, 1, 1, 2, 1, 1, danger: 1),
                L("c7-2", 56, 7,  8,  9,  6,  9, 1, 2, 1, 2, 1, 2, 1, 2, 1, 1, danger: 1, needPush: 1),
                L("c7-3", 57, 7,  9,  9,  6,  9, 1, 2, 1, 2, 1, 1, 1, 2, 0, 2, danger: 1),
                L("c7-4", 58, 7,  9, 10,  6,  9, 1, 3, 1, 3, 1, 2, 1, 2, 1, 1, danger: 1, needPush: 1),
                L("c7-5", 59, 7, 10, 11,  6, 10, 1, 3, 1, 3, 1, 2, 1, 2, 1, 2, danger: 1),
                L("c7-6", 60, 7, 11, 12,  6, 10, 1, 3, 1, 3, 1, 2, 2, 2, 2, 1, danger: 1, needPush: 1),
                L("c7-7", 61, 7, 12, 13,  6, 10, 1, 3, 1, 3, 1, 2, 1, 2, 1, 2, danger: 1, needPush: 1),
                L("c7-8", 62, 7, 13, 14,  6, 10, 1, 3, 2, 3, 1, 2, 2, 3, 2, 1, danger: 1, needPush: 1),
                L("c7-9", 63, 7, 14, 16,  6, 10, 1, 3, 2, 3, 1, 2, 2, 3, 1, 2, danger: 1, needPush: 1),
            },
        };

        /// <summary>
        /// Default Challenge table (PG-001, copied into PenguinConfig.asset): steps of 3 levels at the start, then 5; from 4x6 with rocks
        /// only to 7x9 with water, blocks and two penguins. Saw-tooth: the minimum drops when a novelty arrives. After the last step the
        /// run stays on it.
        /// </summary>
        public static DifficultyTable Challenge() => new DifficultyTable
        {
            tiers =
            {
                //  id      levels  cols min    rocks  snow  holes edges fish  bl pg
                C("ch-1",   1,  3, 4, 2, 4,  2, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                C("ch-2",   4,  6, 4, 3, 5,  3, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                C("ch-3",   7,  9, 5, 4, 7,  4, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                C("ch-4",  10, 12, 5, 3, 6,  3, 6, 0, 0, 0, 0, 0, 0, 1, 2, 0, 1),
                C("ch-5",  13, 15, 5, 5, 8,  4, 7, 0, 0, 0, 0, 0, 0, 2, 3, 0, 1),
                C("ch-6",  16, 20, 6, 4, 7,  4, 7, 1, 2, 0, 0, 0, 0, 0, 1, 0, 1, needSnow: 1),
                C("ch-7",  21, 25, 6, 6, 9,  5, 8, 2, 3, 0, 0, 0, 0, 1, 2, 0, 1, needSnow: 1),
                C("ch-8",  26, 30, 6, 5, 8,  4, 7, 0, 1, 1, 2, 1, 1, 0, 1, 0, 1, danger: 1),
                C("ch-9",  31, 35, 6, 7, 10, 5, 8, 0, 2, 2, 3, 1, 2, 1, 2, 0, 1, danger: 1),
                C("ch-10", 36, 40, 6, 5, 8,  4, 7, 0, 1, 0, 1, 0, 1, 0, 1, 1, 1, needPush: 1),
                C("ch-11", 41, 45, 6, 7, 10, 5, 8, 0, 1, 1, 2, 0, 1, 0, 1, 2, 1, needPush: 1),
                C("ch-12", 46, 50, 6, 5, 8,  4, 7, 0, 1, 0, 1, 0, 1, 0, 1, 0, 2),
                C("ch-13", 51, 55, 7, 7, 10, 6, 9, 0, 2, 0, 2, 0, 1, 1, 2, 0, 2),
                C("ch-14", 56, 60, 7, 8, 11, 6, 9, 1, 2, 1, 2, 1, 2, 1, 2, 1, 1, danger: 1),
                C("ch-15", 61, 65, 7, 8, 11, 6, 9, 1, 2, 1, 2, 1, 1, 1, 2, 1, 2),
                C("ch-16", 66, 70, 7, 9, 12, 6, 10, 1, 3, 1, 3, 1, 2, 1, 2, 1, 2),
            },
        };

        /// <summary>The values of a level that are outside its tier (counts only; the checks are <see cref="PenguinGenerator.Problems"/>).</summary>
        public static List<string> CheckTier(DifficultyTier tier, PenguinLayout layout, int minMoves)
        {
            var problems = new List<string>();
            void Check(string key, int value)
            {
                if (tier.TryGet(key, out var r) && !r.Contains(value)) problems.Add($"{key} {value} outside {r.min}..{r.max}");
            }
            Check(Cols, layout.Width);
            Check(Rows, layout.Height);
            Check(MinMoves, minMoves);
            Check(Rocks, layout.Count(PenguinTerrain.Rock));
            Check(Snow, layout.Count(PenguinTerrain.Snow));
            Check(Holes, layout.Count(PenguinTerrain.Water));
            Check(WaterEdges, layout.WaterEdgeSegments);
            Check(Fish, layout.FishCount);
            Check(Blocks, layout.BlockCount);
            Check(Penguins, layout.PenguinCount);
            return problems;
        }
    }
}
