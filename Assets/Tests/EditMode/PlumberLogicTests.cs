using System.Linq;
using NUnit.Framework;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Games.Plumber;
using UnityEngine;

namespace ResolverMaster.Tests
{
    public sealed class PlumberLogicTests
    {
        const int U = 1, R = 2, D = 4, L = 8;

        [Test]
        public void Rotation_OpeningsAfterOneToFourTurns_ForEveryPiece()
        {
            var expected = new (PieceKind kind, int[] masks)[]
            {
                (PieceKind.Straight, new[] { U | D, R | L, U | D, R | L, U | D }),
                (PieceKind.Curve, new[] { U | R, R | D, D | L, L | U, U | R }),
                (PieceKind.Tee, new[] { U | R | D, R | D | L, D | L | U, L | U | R, U | R | D }),
                (PieceKind.Cross, new[] { U | R | D | L, U | R | D | L, U | R | D | L, U | R | D | L, U | R | D | L }),
                (PieceKind.Source, new[] { U, R, D, L, U }),
                (PieceKind.Sink, new[] { U, R, D, L, U }),
            };
            foreach (var (kind, masks) in expected)
            {
                var grid = new PipeGrid(1, 1);
                grid.Pieces[0] = new Piece(kind);
                for (int turns = 0; turns <= 4; turns++)
                {
                    Assert.AreEqual(masks[turns], grid.Openings(0), $"{kind} after {turns} turn(s)");
                    grid.Rotate(0, 1);
                }
            }
            Assert.AreEqual(2, PieceShapes.DistinctRotations(PieceKind.Straight, new SquareTopology(1, 1), 0));
            Assert.AreEqual(1, PieceShapes.DistinctRotations(PieceKind.Cross, new SquareTopology(1, 1), 0));
        }

        // ErNa-003: updated to the new rules (rotating shows no water; the level is solved only by opening it, not by a move)
        [Test]
        public void Water_HandMadeLevel_ReachesTheRightCells()
        {
            // S> ═ ╗        source opens right; the route turns down the right column and reaches the curve at the bottom right,
            // ║  ╔ ║        which opens left into the vertical straight (index 7): leak there. Sink at bottom-left opens right.
            // E> ║ ╝
            var level = new PlumberLevel { minMoves = 1, cells = "S1 I1 C2/I0 C1 I0/E1 I0 C3" };
            var puzzle = new PlumberPuzzle();
            puzzle.Load(level);

            var water = puzzle.OpenWater();
            CollectionAssert.AreEqual(new[] { 0, 1, 2, -1, -1, 3, -1, -1, 4 }, water.Step);
            Assert.IsFalse(water.Success);
            Assert.IsFalse(puzzle.IsSolved, "a failed opening does not solve");

            Assert.IsTrue(puzzle.ApplyMove(7));
            Assert.IsFalse(puzzle.IsSolved, "a move alone never solves");
            water = puzzle.OpenWater();
            CollectionAssert.AreEqual(new[] { 0, 1, 2, -1, -1, 3, 6, 5, 4 }, water.Step);
            Assert.IsTrue(water.Success);
            Assert.IsTrue(puzzle.IsSolved);
            Assert.AreEqual(1, puzzle.MovesMade, "opening the water is not a move");
            Assert.IsFalse(puzzle.ApplyMove(4), "no moves after the win");

            puzzle.Restart();
            Assert.AreEqual(0, puzzle.MovesMade);
            Assert.IsFalse(puzzle.CanUndo);
            Assert.IsFalse(puzzle.IsSolved);
            Assert.IsFalse(puzzle.ApplyMove(0), "the source cannot be rotated");
        }

        [Test]
        public void Leak_WetPipeOpenTowardsTheEdge_FailsAtThatCell()
        {
            // S> ═ ╗
            // E> ═ ╚>       the bottom-right curve opens up and right: the water reaches it and leaks out of the grid
            var grid = PlumberLevelCodec.Parse("S1 I1 C2/E1 I1 C0");
            var water = WaterFlow.Compute(grid);
            Assert.IsFalse(water.Success);
            var leak = water.FirstProblem;
            Assert.AreEqual(WaterProblemKind.Leak, leak.Kind);
            Assert.AreEqual(5, leak.Cell);
            Assert.AreEqual(SquareTopology.Right, leak.Direction);
            Assert.AreEqual(3, leak.Step);
            Assert.AreEqual(3, water.StopStep, "the animation stops at the leak");
            Assert.IsTrue(water.Problems.Any(p => p.Kind == WaterProblemKind.DrySink && p.Cell == 3), "the sink stays dry too");

            grid.Rotate(5, 3);   // up + left: the route closes into the sink
            Assert.IsTrue(WaterFlow.Compute(grid).Success);
        }

        [Test]
        public void Colors_FlowsTouchingOrWrongFountain_Fail()
        {
            // a blue and an orange source both pour into the same straight
            var mix = WaterFlow.Compute(PlumberLevelCodec.Parse("S1 I1 S3@1"));
            Assert.IsFalse(mix.Success);
            Assert.AreEqual(WaterProblemKind.ColorMix, mix.FirstProblem.Kind);
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, new[] { mix.FirstProblem.Cell, mix.FirstProblem.OtherCell });

            // blue water reaches the orange fountain
            var wrong = WaterFlow.Compute(PlumberLevelCodec.Parse("S1 I1 E3@1"));
            Assert.IsFalse(wrong.Success);
            Assert.AreEqual(WaterProblemKind.WrongColor, wrong.FirstProblem.Kind);
            Assert.AreEqual(2, wrong.FirstProblem.Cell);

            // the same route into a fountain of its own color is fine; two separate colors side by side are fine
            Assert.IsTrue(WaterFlow.Compute(PlumberLevelCodec.Parse("S1 I1 E3")).Success);
            Assert.IsTrue(WaterFlow.Compute(PlumberLevelCodec.Parse("S1 I1 E3/S1@1 I1 E3@1")).Success);
        }

        [Test]
        public void Tee_OneSourceTwoFountains_Succeeds()
        {
            //    v          source on top pours down into a T that splits left and right into two blue fountains
            // E> T <E
            var grid = PlumberLevelCodec.Parse(". S2 ./E1 T1 E3/. . .");
            var start = WaterFlow.Compute(grid);
            Assert.IsFalse(start.Success, "T1 is closed on top: the source leaks into it");
            Assert.AreEqual(WaterProblemKind.Leak, start.FirstProblem.Kind);

            var sol = PlumberSolver.Solve(grid);
            Assert.IsTrue(sol.Solvable);
            Assert.AreEqual(2, sol.MinMoves);
            Assert.AreEqual(1, sol.TeesOnPath);
            foreach (int cell in sol.Taps) grid.Rotate(cell, 1);
            var water = WaterFlow.Compute(grid);
            Assert.IsTrue(water.Success);
            Assert.IsTrue(water.IsWet(3) && water.IsWet(5), "both fountains are reached");
        }

        [Test]
        public void Solver_TwoColors_HandMadeLevel_MinimumMoves()
        {
            // blue on top, orange at the bottom: each straight needs 1 tap; the middle straight stays out of the water
            var grid = PlumberLevelCodec.Parse("S1 I0 E3/. I0 ./S1@1 I0 E3@1");
            var sol = PlumberSolver.Solve(grid);
            Assert.IsTrue(sol.Solvable);
            Assert.AreEqual(2, sol.MinMoves);
            CollectionAssert.AreEquivalent(new[] { 1, 7 }, sol.PathCells);
            foreach (int cell in sol.Taps) grid.Rotate(cell, 1);
            Assert.IsTrue(WaterFlow.Compute(grid).Success);

            // with a T on the blue route its third opening must go down, through the middle, into the orange route: impossible
            Assert.IsFalse(PlumberSolver.Solve(PlumberLevelCodec.Parse("S1 T0 E3/. I0 ./S1@1 I0 E3@1")).Solvable);
        }

        // ErNa-003: updated to the new rules (no leaks: a cross in a single row always leaks; routes are measured in water order)
        [Test]
        public void Solver_HandMadeLevels_MinimumMovesWithSymmetries()
        {
            // vertical straight between source and sink: 1 tap (not 3: a straight has 2 positions)
            Assert.AreEqual(1, Solve("S1 I0 E3").MinMoves);
            Assert.AreEqual(1, Solve("S1 I2 E3").MinMoves);
            // a cross with nothing above and below always leaks
            Assert.IsFalse(Solve("S1 X3 I0 E3").Solvable);
            // curves turn only clockwise: C0 -> C2 is 2 taps, C3 -> C2 is 3 taps; C1 -> C3 is 2 taps
            Assert.AreEqual(4, Solve("S1 C0/E1 C1").MinMoves);
            Assert.AreEqual(5, Solve("S1 C3/E1 C1").MinMoves);
            // a winding route already right: 0 taps, 5 pipe cells (down, right, up, right, down)
            var winding = Solve("S2 C1 C2/C0 C3 I0/. . E0");
            Assert.AreEqual(0, winding.MinMoves);
            Assert.AreEqual(5, winding.PathLength);
            CollectionAssert.AreEqual(new[] { 3, 4, 1, 2, 5 }, winding.PathCells);
            // a curve can never join left and right: no solution
            var none = Solve("S1 C1 E3");
            Assert.IsFalse(none.Solvable);
            Assert.IsNotNull(none.Reason);
        }

        // ErNa-003: updated to the new table and rules
        [Test]
        public void Generator_SameSeedSameLevel_AndSolvable()
        {
            foreach (int number in new[] { 9, 18, 27 })   // one route, fork, two colors
            {
                var tier = PlumberDifficulty.Story().ForLevel(number);
                var settings = PlumberGenerator.Settings.FromTier(tier, 20000);
                var a = PlumberGenerator.Generate(settings, 12345).Level;
                var b = PlumberGenerator.Generate(settings, 12345).Level;
                var c = PlumberGenerator.Generate(settings, 54321).Level;
                Assert.AreEqual(a.cells, b.cells);
                Assert.AreNotEqual(a.cells, c.cells);

                var grid = a.CreateGrid();
                var sol = PlumberSolver.Solve(grid);
                Assert.IsTrue(sol.Solvable);
                Assert.AreEqual(a.minMoves, sol.MinMoves);
                Assert.IsFalse(WaterFlow.Compute(grid).Success, "never solved at the start");
                CollectionAssert.IsEmpty(PlumberDifficulty.CheckTier(tier, grid, sol.MinMoves, sol.PathLength));
                // applying the solver taps solves it in exactly MinMoves
                foreach (int cell in sol.Taps) grid.Rotate(cell, 1);
                Assert.AreEqual(sol.MinMoves, sol.Taps.Count);
                Assert.IsTrue(WaterFlow.Compute(grid).Success);
            }
        }

        [Test]
        public void Stars_ThreeTwoOne_AtTheLimits()
        {
            var rules = new StarRules { twoStarExtraPercent = 30, twoStarMinExtra = 2 };
            // min 5: 30% = 1 (rounded down) < 2, so 2 stars up to 7
            Assert.AreEqual(3, StarRating.Compute(5, 5, rules));
            Assert.AreEqual(2, StarRating.Compute(6, 5, rules));
            Assert.AreEqual(2, StarRating.Compute(7, 5, rules));
            Assert.AreEqual(1, StarRating.Compute(8, 5, rules));
            // min 10: 30% = 3, so 2 stars up to 13
            Assert.AreEqual(3, StarRating.Compute(10, 10, rules));
            Assert.AreEqual(2, StarRating.Compute(13, 10, rules));
            Assert.AreEqual(1, StarRating.Compute(14, 10, rules));
            // min 1: 2 stars up to 3
            Assert.AreEqual(2, StarRating.Compute(3, 1, rules));
            Assert.AreEqual(1, StarRating.Compute(4, 1, rules));
        }

        // ErNa-005
        [Test]
        public void Generator_Grid5x7_SameSeedSameLevel_AndSolvable()
        {
            var tier = PlumberDifficulty.Story().ForLevel(14);   // chapter 2, 5 columns x 7 rows
            var settings = PlumberGenerator.Settings.FromTier(tier, 20000);
            Assert.AreEqual(5, settings.Width);
            Assert.AreEqual(7, settings.Height);
            var a = PlumberGenerator.Generate(settings, 777).Level;
            var b = PlumberGenerator.Generate(settings, 777).Level;
            Assert.AreEqual(a.cells, b.cells, "same seed, same level");
            Assert.AreEqual(5, a.width);
            Assert.AreEqual(7, a.height);
            var grid = a.CreateGrid();
            Assert.AreEqual(5, grid.Width);
            Assert.AreEqual(7, grid.Height);
            var sol = PlumberSolver.Solve(grid);
            Assert.IsTrue(sol.Solvable);
            Assert.AreEqual(a.minMoves, sol.MinMoves);
            CollectionAssert.IsEmpty(PlumberDifficulty.CheckTier(tier, grid, sol.MinMoves, sol.PathLength));
            foreach (int cell in sol.Taps) grid.Rotate(cell, 1);
            Assert.IsTrue(WaterFlow.Compute(grid).Success);
        }

        // ErNa-005
        [Test]
        public void WaterTiming_TeeBranchesTogether_FartherCellsLaterInProportion()
        {
            //   . v . .     the source pours down into a T that splits left and right;
            //   E>T = <E    the right branch is one straight longer than the left one
            var grid = PlumberLevelCodec.Parse(". S2 . ./E1 T3 I1 E3");
            var state = WaterFlow.Compute(grid);
            Assert.IsTrue(state.Success);
            var t = WaterTiming.Compute(grid, state);
            const int tee = 5, straight = 6, leftSink = 4, rightSink = 7;
            const int up = SquareTopology.Up, right = SquareTopology.Right, left = SquareTopology.Left;

            // both branches leave the T at the same moment (same distance from the source)
            float leftExit = t.NodeDistance(t.PortNode(tee, left)), rightExit = t.NodeDistance(t.PortNode(tee, right));
            Assert.AreEqual(leftExit, rightExit, 1e-5f);
            Assert.AreEqual(1.5f, leftExit, 1e-5f, "source arm 0.5 + into the T 0.5 + out of it 0.5");

            // arrival = distance along the pipes: the T at 0.5, the straight at 1.5, the far fountain at 2.5 (3x, 5x the T)
            Assert.AreEqual(0.5f, t.CellArrival(tee), 1e-5f);
            Assert.AreEqual(t.NodeDistance(t.PortNode(tee, up)), t.CellArrival(tee), 1e-5f);
            Assert.AreEqual(3f * t.CellArrival(tee), t.CellArrival(straight), 1e-5f);
            Assert.AreEqual(5f * t.CellArrival(tee), t.CellArrival(rightSink), 1e-5f);
            Assert.AreEqual(t.CellArrival(leftSink) + 1f, t.CellArrival(rightSink), 1e-5f, "one cell farther = one cell later");

            // at a constant speed the times keep the same proportions; the gentle start only delays the beginning
            var motion = new WaterMotion(4f, 0f, 1f);
            Assert.AreEqual(3f * motion.TimeAt(t.CellArrival(tee)), motion.TimeAt(t.CellArrival(straight)), 1e-4f);
            var eased = new WaterMotion(4f, 0.35f, 0.35f);
            for (float d = 0.1f; d < 5f; d += 0.37f) Assert.AreEqual(d, eased.DistanceAt(eased.TimeAt(d)), 1e-3f, "time and distance agree");
            Assert.AreEqual(t.EndDistance, t.StopDistance, 1e-5f, "success: the water stops when everything is full");
        }

        // ErNa-006
        [Test]
        public void Palette_FourWaterColors_Valid_RedWithGreenOrSameLightnessRejected()
        {
            var cfg = UnityEngine.ScriptableObject.CreateInstance<PlumberConfig>();
            try
            {
                Assert.AreEqual(4, cfg.waterColors.Count, "blue, orange, violet, yellow");
                Assert.AreEqual(cfg.waterColors.Count, cfg.sinkHaloColors.Count);
                CollectionAssert.IsEmpty(PlumberPalette.Problems(cfg.waterColors, cfg.minWaterLightnessGap));
                // blue and orange unchanged from ErNa-003
                Assert.AreEqual(new Color32(0x2F, 0x8B, 0xEF, 0xFF), (Color32)cfg.waterColors[0]);
                Assert.AreEqual(new Color32(0xFF, 0x8A, 0x1F, 0xFF), (Color32)cfg.waterColors[1]);
            }
            finally { UnityEngine.Object.DestroyImmediate(cfg); }

            var redGreen = new[] { new Color(0.85f, 0.1f, 0.1f), new Color(0.2f, 0.75f, 0.2f) };
            Assert.IsTrue(PlumberPalette.Problems(redGreen, 0f).Any(p => p.Contains("red")));
            var sameLightness = new[] { new Color(0.5f, 0.5f, 0.5f), new Color(0.52f, 0.5f, 0.48f) };
            Assert.IsTrue(PlumberPalette.Problems(sameLightness, 10f).Any(p => p.Contains("lightness")));
        }

        // ErNa-006
        [Test]
        public void Solver_ThreeAndFourColors_SolvableAndMinimumEqualsExhaustiveSearch()
        {
            // small 3- and 4-color levels: the solver minimum equals the cheapest of every rotation of every pipe
            foreach (var (w, h, colors) in new[] { (3, 4, 3), (4, 3, 3), (4, 4, 4) })
            for (int seed = 1; seed <= 6; seed++)
            {
                var settings = new PlumberGenerator.Settings
                {
                    Width = w, Height = h, ColorCount = colors, Layout = PlumberGenerator.Layout.Colors,
                    MinMoves = new DifficultyRange(PlumberDifficulty.MinMoves, 1, 99), Path = new DifficultyRange(PlumberDifficulty.Path, colors, w * h - 2 * colors),
                    Tees = new DifficultyRange(PlumberDifficulty.Tees, 0, 0), Crosses = new DifficultyRange(PlumberDifficulty.Crosses, 0, 0), MaxAttempts = 5000,
                };
                var level = PlumberGenerator.Generate(settings, seed * 101).Level;
                var grid = level.CreateGrid();
                Assert.AreEqual(colors, grid.ColorCount());
                var sol = PlumberSolver.Solve(grid);
                Assert.IsTrue(sol.Solvable, level.cells);
                Assert.AreEqual(Exhaustive(grid), sol.MinMoves, level.cells);
                foreach (int cell in sol.Taps) grid.Rotate(cell, 1);
                Assert.IsTrue(WaterFlow.Compute(grid).Success, level.cells);
            }

            // a 7x9 level with 4 colors from the generator: same seed, same level, solved by the solver taps
            var big = new PlumberGenerator.Settings
            {
                Width = 7, Height = 9, ColorCount = 4, Layout = PlumberGenerator.Layout.Colors,
                MinMoves = new DifficultyRange(PlumberDifficulty.MinMoves, 1, 99), Path = new DifficultyRange(PlumberDifficulty.Path, 20, 30),
                Tees = new DifficultyRange(PlumberDifficulty.Tees, 1, 2), Crosses = new DifficultyRange(PlumberDifficulty.Crosses, 0, 1),
            };
            var a = PlumberGenerator.Generate(big, 4242).Level;
            Assert.AreEqual(a.cells, PlumberGenerator.Generate(big, 4242).Level.cells);
            var g = a.CreateGrid();
            Assert.AreEqual(4, g.ColorCount());
            var s = PlumberSolver.Solve(g);
            Assert.AreEqual(a.minMoves, s.MinMoves);
            foreach (int cell in s.Taps) g.Rotate(cell, 1);
            Assert.IsTrue(WaterFlow.Compute(g).Success);
        }

        // ErNa-006
        [Test]
        public void Bridge_TwoColorsCrossWithoutMixing_ACrossInItsPlaceMixes()
        {
            //     v          blue from the top runs down through the bridge into the blue fountain below;
            // o>  B  <o      orange runs east-west through the same bridge into the orange fountain
            //     ^
            var grid = PlumberLevelCodec.Parse(". S2 ./S1@1 B0 E3@1/. E0 .");
            var water = WaterFlow.Compute(grid);
            Assert.IsTrue(water.Success, string.Join(", ", water.Problems));
            const int bridge = 4;
            Assert.AreEqual(0, water.NodeColor[bridge], "north-south channel: blue");
            Assert.AreEqual(1, water.NodeColor[grid.CellCount + bridge], "east-west channel: orange");
            Assert.AreEqual(0, water.Color[7], "the blue fountain gets blue");

            // the solver uses both channels; solved as a cross the level is impossible (the bridge is needed)
            var sol = PlumberSolver.Solve(grid);
            Assert.IsTrue(sol.Solvable);
            Assert.AreEqual(0, sol.MinMoves);
            Assert.AreEqual(1, sol.BridgesCrossed);
            Assert.IsFalse(PlumberSolver.Solve(grid, new PlumberSolver.Options { BridgesAsCrosses = true }).Solvable);

            // a cross mixes the two colors
            var cross = WaterFlow.Compute(PlumberLevelCodec.Parse(". S2 ./S1@1 X0 E3@1/. E0 ."));
            Assert.IsFalse(cross.Success);
            Assert.AreEqual(WaterProblemKind.ColorMix, cross.FirstProblem.Kind);

            // the timing: the two channels are separate parts that never share a node
            var t = WaterTiming.Compute(grid, water);
            var parts = t.Parts(bridge);
            Assert.AreEqual(2, parts.Count);
            Assert.IsTrue(parts.All(p => p.Through));
            Assert.AreNotEqual(t.NodeColor(parts[0].NodeA), t.NodeColor(parts[1].NodeA));
            Assert.AreEqual(t.EndDistance, t.StopDistance, 1e-5f);

            // a bridge never leaks sideways when only one channel is used, and a single channel cannot turn
            Assert.IsTrue(WaterFlow.Compute(PlumberLevelCodec.Parse("S2/B0/E0")).Success);
            Assert.IsFalse(PlumberSolver.Solve(PlumberLevelCodec.Parse("S1 B0/. E0")).Solvable, "water entering from the west leaves to the east only");
        }

        // ErNa-006
        [Test]
        public void Taps_CrossBridgeAndLockedPieces_NeverCountAsMoves_OldLevelsStillLoad()
        {
            var puzzle = new PlumberPuzzle();
            puzzle.Load(new PlumberLevel { minMoves = 1, cells = "S1 I0 E3/X0 B0 C1#" });
            foreach (int cell in new[] { 3, 4, 5 })
            {
                int rotation = puzzle.Grid.Pieces[cell].Rotation;
                Assert.IsFalse(puzzle.ApplyMove(cell), $"cell {cell} ({puzzle.Grid.Pieces[cell].Kind})");
                Assert.AreEqual(rotation, puzzle.Grid.Pieces[cell].Rotation);
            }
            Assert.AreEqual(0, puzzle.MovesMade);
            Assert.IsFalse(puzzle.CanUndo);
            Assert.IsTrue(puzzle.ApplyMove(1), "a normal straight still turns");
            Assert.AreEqual(1, puzzle.MovesMade);
            Assert.IsTrue(puzzle.Grid.Pieces[5].Locked);
            Assert.AreEqual(CellLock.Locked, puzzle.Locks.Get(5), "the Core lock set mirrors the level");
            Assert.IsTrue(puzzle.Locks.CanChange(1));
            Assert.AreEqual(PieceKind.Bridge, puzzle.Grid.Pieces[4].Kind);

            // the solver keeps locked pieces as they are: a wrong locked straight blocks the only route, a right one costs nothing
            Assert.IsFalse(Solve("S1 I0# E3").Solvable);
            Assert.AreEqual(1, Solve("S1 I1# I0 E3").MinMoves);

            // the generator: locked pieces on the route are already right, the level stays exact and solvable
            var settings = new PlumberGenerator.Settings
            {
                Width = 5, Height = 7, ColorCount = 2, Layout = PlumberGenerator.Layout.Colors,
                MinMoves = new DifficultyRange(PlumberDifficulty.MinMoves, 4, 30), Path = new DifficultyRange(PlumberDifficulty.Path, 8, 14),
                Tees = new DifficultyRange(PlumberDifficulty.Tees, 0, 1), Crosses = new DifficultyRange(PlumberDifficulty.Crosses, 0, 0),
                Locked = new DifficultyRange(PlumberDifficulty.Locked, 3, 4),
            };
            var level = PlumberGenerator.Generate(settings, 31337).Level;
            var lockedGrid = level.CreateGrid();
            Assert.That(lockedGrid.CountLocked(), Is.InRange(3, 4));
            var lockedSol = PlumberSolver.Solve(lockedGrid);
            Assert.AreEqual(level.minMoves, lockedSol.MinMoves);
            Assert.IsFalse(lockedSol.Taps.Any(c => lockedGrid.Pieces[c].Locked), "no tap on a locked piece");

            // the format round trip keeps bridges and locks; an ErNa-005 level (no bridges, no locks) reads the same as before
            const string withNew = "S1 I0 E3/X0 B0 C1#";
            Assert.AreEqual(withNew, PlumberLevelCodec.Format(PlumberLevelCodec.Parse(withNew)));
            const string old = "S1 I1 C2/I0 C1 I0/E1 I0 C3";
            var oldGrid = PlumberLevelCodec.Parse(old);
            Assert.AreEqual(old, PlumberLevelCodec.Format(oldGrid));
            Assert.IsFalse(oldGrid.Pieces.Any(p => p.Locked || p.Kind == PieceKind.Bridge));
            Assert.AreEqual(1, PlumberSolver.Solve(oldGrid).MinMoves);
        }

        /// <summary>Cheapest successful opening over every rotation of every rotatable piece (small grids only).</summary>
        static int Exhaustive(PipeGrid start)
        {
            var cells = Enumerable.Range(0, start.CellCount).Where(c => start.Pieces[c].IsRotatable).ToArray();
            Assert.LessOrEqual(cells.Length, 10, "too many pieces for the exhaustive search");
            int best = int.MaxValue;
            var grid = start.Clone();
            int combos = 1 << (2 * cells.Length);
            for (int k = 0; k < combos; k++)
            {
                int cost = 0;
                for (int i = 0; i < cells.Length; i++)
                {
                    int taps = (k >> (2 * i)) & 3;
                    grid.Pieces[cells[i]].Rotation = (start.Pieces[cells[i]].Rotation + taps) % 4;
                    cost += taps;
                }
                if (cost < best && WaterFlow.Compute(grid).Success) best = cost;
            }
            return best;
        }

        static PlumberSolution Solve(string cells) => PlumberSolver.Solve(PlumberLevelCodec.Parse(cells));
    }
}
