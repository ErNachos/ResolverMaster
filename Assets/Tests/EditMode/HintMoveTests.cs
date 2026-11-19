using NUnit.Framework;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Games.Penguin;
using ResolverMaster.Games.Plumber;
using ResolverMaster.Games.TrafficJam;

namespace ResolverMaster.Tests
{
    /// <summary>
    /// ErNa-011: the light bulb's moves count like the player's. For every game, Story levels solved with the light bulb several times
    /// end with moves at least the minimum and at most 2 stars; "Out of moves" works when the light bulb made the last move.
    /// </summary>
    public sealed class HintMoveTests
    {
        static readonly StarRules Rules = new StarRules();
        static readonly int[] Sample = { 0, 4, 8, 13, 22, 31, 40, 50, 62 };

        static void AssertFinal(int moves, int min, bool hintUsed, string what)
        {
            Assert.IsTrue(hintUsed, what);
            Assert.GreaterOrEqual(moves, min, what + ": never below the minimum");
            Assert.LessOrEqual(StarRating.Compute(moves, min, Rules, hintUsed), 2, what + ": at most 2 stars");
        }

        [Test]
        public void Plumber_SolvedWithLightBulb_TurnsCounted_AtLeastMinimum_TwoStarsAtMost()
        {
            var levels = new PlumberModule().StoryLevels;
            foreach (int i in Sample)
            {
                var level = levels[i];
                var p = new PlumberPuzzle();
                p.Load(level);
                // the player turns a piece twice, then the light bulb fixes everything; each hint counts its turns (1 to 3)
                int cell = -1;
                for (int c = 0; c < p.Grid.CellCount && cell < 0; c++)
                    if (p.Grid.Pieces[c].IsRotatable && p.Locks.CanChange(c)) cell = c;
                if (cell >= 0) Assert.IsTrue(p.ApplyMove(cell) && p.ApplyMove(cell));
                int hints = 0;
                while (p.TryFindHint(out _, out _))
                {
                    int before = p.MovesMade;
                    Assert.IsTrue(p.ApplyHint(out _, out int taps));
                    Assert.That(taps, Is.InRange(1, 3), $"level {level.number}");
                    Assert.AreEqual(before + taps, p.MovesMade, $"level {level.number}: the counter goes up by the turns");
                    hints++;
                }
                Assert.GreaterOrEqual(hints, 1);
                Assert.IsTrue(p.OpenWater().Success, $"level {level.number}");
                AssertFinal(p.MovesMade, level.minMoves, p.HintUsed, $"plumber {level.number}");

                // a restart keeps the fixed pieces and their moves; whatever the player does next, the total stays >= the minimum
                p.Load(level);
                Assert.IsTrue(p.ApplyHint(out _, out _));
                p.Restart();
                Assert.Greater(p.MovesMade, 0, "the fixed piece's turns stay counted");
                while (p.ApplyHint(out _, out _)) { }
                Assert.IsTrue(p.OpenWater().Success);
                AssertFinal(p.MovesMade, level.minMoves, p.HintUsed, $"plumber {level.number} after a restart");
            }
        }

        [Test]
        public void TrafficJam_SolvedWithLightBulb_EachMoveCounts_AtLeastMinimum_TwoStarsAtMost()
        {
            var levels = new TrafficJamModule().StoryLevels;
            foreach (int i in Sample)
            {
                var level = levels[i];
                var p = new TrafficPuzzle();
                p.Load(level);
                Assert.IsTrue(p.TryFindHint(out var first) && p.ApplyMove(first), "the player makes the first move");
                while (!p.IsSolved)
                {
                    int before = p.MovesMade;
                    Assert.IsTrue(p.ApplyHint(out _), $"level {level.number}");
                    Assert.AreEqual(before + 1, p.MovesMade, $"level {level.number}: the light bulb's move counts 1");
                }
                Assert.AreEqual(level.minMoves, p.MovesMade, $"level {level.number}: the right moves only");
                AssertFinal(p.MovesMade, level.minMoves, p.HintUsed, $"traffic {level.number}");
            }
        }

        [Test]
        public void Penguin_SolvedWithLightBulb_SwipesAndUndosLikeThePlayers_AtLeastMinimum_TwoStarsAtMost()
        {
            var levels = new PenguinModule().StoryLevels;
            foreach (int i in Sample)
            {
                var level = levels[i];
                var p = new PenguinPuzzle();
                p.Load(level);
                // two swipes of the player that do not fall (they may lead nowhere: the light bulb then undoes out of it)
                for (int dir = 0, made = 0; dir < 4 && made < 2; dir++)
                {
                    if (!p.Swipe(dir)) continue;
                    if (p.Fell || p.IsSolved) Assert.IsTrue(p.Undo(out _));
                    else made++;
                }
                for (int guard = 0; !p.IsSolved; guard++)
                {
                    Assert.Less(guard, 200, $"level {level.number}");
                    int before = p.MovesMade;
                    var r = p.ApplyHint(out _);
                    Assert.AreNotEqual(PenguinHint.None, r, $"level {level.number}");
                    if (r == PenguinHint.Moved) Assert.AreEqual(before + 1, p.MovesMade, $"level {level.number}: a swipe of the light bulb counts 1");
                    else Assert.Less(p.MovesMade, before, $"level {level.number}: the light bulb's undos work like the player's");
                }
                AssertFinal(p.MovesMade, level.minMoves, p.HintUsed, $"penguin {level.number}");
            }
        }

        [Test]
        public void OutOfMoves_WhenTheLightBulbMadeTheLastMove()
        {
            var level = new TrafficJamModule().StoryLevels[20];
            var p = new TrafficPuzzle();
            p.Load(level);
            // a wasted move of the player (one that brings the position no closer to the end)
            bool wasted = false;
            for (int v = 0; v < p.Layout.Vehicles.Count && !wasted; v++)
            {
                if (p.IsOut(v)) continue;
                p.Range(v, out int lo, out int hi, out _);
                int from = p.Pos(v), left = p.MovesLeft;
                for (int to = lo; to <= hi && !wasted; to++)
                {
                    if (to == from || !p.ApplyMove(new TrafficMove(v, from, to))) continue;
                    if (p.MovesLeft >= left) wasted = true;
                    else Assert.IsTrue(p.Undo(out _));
                }
            }
            Assert.IsTrue(wasted, "a level with a move that leads nowhere");
            // the maximum is one move short of the solution: the light bulb makes the moves up to it
            p.MoveLimit = p.MovesMade + p.MovesLeft - 1;
            while (p.ApplyHint(out _)) { }
            Assert.AreEqual(p.MoveLimit, p.MovesMade, "the light bulb made the last move");
            Assert.IsFalse(p.IsSolved);
            Assert.IsFalse(p.ApplyHint(out _), "no hint over the maximum");
            var run = ChallengeRunState.Start(TrafficJamModule.Id, 5);
            run.SetLevel(new ChallengeLevel { number = 1, data = level.cells, minMoves = level.minMoves, maxMoves = p.MoveLimit });
            run.moves = p.MovesMade;
            Assert.IsTrue(run.IsOutOfMoves(p.IsSolved), "\"Out of moves\" also after a light bulb move");
            Assert.IsTrue(p.Undo(out _));
            run.moves = p.MovesMade;
            Assert.IsFalse(run.IsOutOfMoves(p.IsSolved), "undoing the light bulb's move frees it");
        }
    }
}
