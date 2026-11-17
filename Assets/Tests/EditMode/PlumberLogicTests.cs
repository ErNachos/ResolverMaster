using NUnit.Framework;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Games.Plumber;

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

        [Test]
        public void Water_HandMadeLevel_ReachesTheRightCells()
        {
            // S> ═ ╗        source opens right; the route turns down the right column and stops at the vertical straight
            // ║  ╔ ║        bottom middle (index 7); sink at bottom-left opens right
            // E> ║ ╝
            var level = new PlumberLevel { minMoves = 1, cells = "S1 I1 C2/I0 C1 I0/E1 I0 C3" };
            var puzzle = new PlumberPuzzle();
            puzzle.Load(level);

            var water = WaterFlow.Compute(puzzle.Grid);
            int[] steps = { 0, 1, 2, -1, -1, 3, -1, -1, 4 };
            CollectionAssert.AreEqual(steps, water.Step);
            Assert.IsFalse(water.AllSinksReached);
            Assert.IsFalse(puzzle.IsSolved);

            Assert.IsTrue(puzzle.ApplyMove(7));
            water = WaterFlow.Compute(puzzle.Grid);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, -1, -1, 3, 6, 5, 4 }, water.Step);
            Assert.IsTrue(puzzle.IsSolved);
            Assert.AreEqual(1, puzzle.MovesMade);

            Assert.IsTrue(puzzle.Undo(out int undone));
            Assert.AreEqual(7, undone);
            Assert.AreEqual(0, puzzle.MovesMade);
            Assert.IsFalse(puzzle.IsSolved);
            Assert.IsFalse(puzzle.ApplyMove(0), "the source cannot be rotated");
        }

        [Test]
        public void Solver_HandMadeLevels_MinimumMovesWithSymmetries()
        {
            // vertical straight between source and sink: 1 tap (not 3: a straight has 2 positions)
            Assert.AreEqual(1, Solve("S1 I0 E3").MinMoves);
            Assert.AreEqual(1, Solve("S1 I2 E3").MinMoves);
            // a cross never needs taps; the straight after it needs 1
            Assert.AreEqual(1, Solve("S1 X3 I0 E3").MinMoves);
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

        [Test]
        public void Generator_SameSeedSameLevel_AndSolvable()
        {
            var tier = PlumberDifficulty.StoryChapter1().ForLevel(9);
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
            Assert.IsFalse(WaterFlow.Compute(grid).AllSinksReached, "never solved at the start");
            CollectionAssert.IsEmpty(PlumberDifficulty.CheckTier(tier, grid, sol.MinMoves, sol.PathLength));
            // applying the solver taps solves it in exactly MinMoves
            foreach (int cell in sol.Taps) grid.Rotate(cell, 1);
            Assert.AreEqual(sol.MinMoves, sol.Taps.Count);
            Assert.IsTrue(WaterFlow.Compute(grid).AllSinksReached);
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

        static PlumberSolution Solve(string cells) => PlumberSolver.Solve(PlumberLevelCodec.Parse(cells));
    }
}
