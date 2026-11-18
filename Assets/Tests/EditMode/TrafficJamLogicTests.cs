using System.Linq;
using NUnit.Framework;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.Save;
using ResolverMaster.Games.TrafficJam;

namespace ResolverMaster.Tests
{
    /// <summary>TJ-001: Traffic jam rules, exact solver, generator, light bulb, Challenge "out of moves", save, Story levels.</summary>
    public sealed class TrafficJamLogicTests
    {
        // 4x6: red car in column 1 (rows 4-5, exit on top), blue car in row 0 (columns 2-3, exit on the right), a car in row 2 (columns 1-2)
        // with a barrier at its left, a truck in column 0 (rows 3-5) under the barrier.
        const string Small = "4x6 R1.4vU B2.0hR C1.2h T0.3v #0.2";

        static TrafficPuzzle Load(string cells, int minMoves = 0)
        {
            var puzzle = new TrafficPuzzle();
            puzzle.Load(new TrafficLevel { number = 1, cells = cells, minMoves = minMoves });
            return puzzle;
        }

        [Test]
        public void Rules_SlideOnlyAlongTheLine_StopAgainstVehiclesAndBarriers_ReleaseInPlaceIsNoMove_OnlyTargetsLeaveByTheirExit()
        {
            var p = Load(Small);
            const int red = 0, blue = 1, car = 2, truck = 3;
            Assert.AreEqual(VehicleKind.Target, p.Layout.Vehicles[red].Kind);
            Assert.AreEqual(1, p.Layout.Vehicles[blue].Target);

            // every move of a vehicle keeps it on its own row or column
            var buffer = new TrafficMove[p.Layout.MaxMovesPerState];
            int n = p.Layout.Moves(p.State, buffer);
            Assert.Greater(n, 0);
            for (int k = 0; k < n; k++)
            {
                var v = p.Layout.Vehicles[buffer[k].Vehicle];
                if (buffer[k].IsExit) continue;
                for (int c = 0; c < v.Length; c++)
                {
                    int cell = p.Layout.Cell(buffer[k].Vehicle, buffer[k].To + c);
                    Assert.AreEqual(v.Line, v.Horizontal ? cell / 4 : cell % 4, "never off its line");
                }
            }

            // the car stops against the barrier (left) and the edge (right); the truck against the barrier above it; the red car against the car
            p.Range(car, out int lo, out int hi, out bool exit);
            Assert.AreEqual((1, 2, false), (lo, hi, exit));
            p.Range(truck, out lo, out hi, out exit);
            Assert.AreEqual((3, 3, false), (lo, hi, exit));
            p.Range(red, out lo, out hi, out exit);
            Assert.AreEqual((3, 4, false), (lo, hi, exit), "blocked by the car in row 2, no exit at the bottom");
            Assert.IsFalse(p.ApplyMove(new TrafficMove(red, 4, 2)), "cannot jump over the car");
            Assert.IsFalse(p.ApplyMove(new TrafficMove(truck, 3, 2)), "cannot pass the barrier");

            // released where it started: no move
            Assert.IsFalse(p.ApplyMove(new TrafficMove(car, 1, 1)));
            Assert.AreEqual(0, p.MovesMade);

            // only the target cars leave, each only through its own exit
            Assert.IsFalse(p.ApplyMove(new TrafficMove(car, 1, TrafficLayout.Exited)), "an ordinary car never leaves");
            Assert.IsFalse(p.ApplyMove(new TrafficMove(red, 4, TrafficLayout.Exited)), "the red car's way is closed");
            Assert.IsTrue(p.ApplyMove(new TrafficMove(car, 1, 2)), "the car slides right, column 1 is free");
            Assert.IsTrue(p.ApplyMove(new TrafficMove(blue, 2, TrafficLayout.Exited)), "the blue car leaves on the right");
            Assert.IsTrue(p.IsOut(blue));
            Assert.IsFalse(p.IsSolved, "the red car is still in");
            Assert.IsTrue(p.ApplyMove(new TrafficMove(red, 4, TrafficLayout.Exited)), "one push from the bottom row takes the red car out on top");
            Assert.IsTrue(p.IsSolved);
            Assert.AreEqual(3, p.MovesMade, "the last push counts as a move");
            Assert.IsTrue(p.Undo(out var undone) && undone.IsExit && !p.IsOut(red), "undo brings the car back");
        }

        [Test]
        public void Solver_ExactMinimumOnHandMadeLevels_SameSeedSameLevel()
        {
            // verified by hand
            Assert.AreEqual(1, Space("4x6 R1.4vU").MinMoves, "free way: one push");
            Assert.AreEqual(2, Space("4x6 R1.4vU C1.2h #0.2").MinMoves, "the car right, then out");
            Assert.AreEqual(3, Space("4x6 R1.4vU C1.2h C3.1v").MinMoves, "the second car up or down, the first right, then out");
            Assert.AreEqual(2, Space("4x6 R1.4vU B2.0hR").MinMoves, "two target cars: two exits");
            Assert.AreEqual(3, Space(Small).MinMoves, "car, blue car, red car");
            Assert.IsFalse(Space("4x6 R1.4vU C0.2h #3.2").Solvable, "the car can never leave column 1");
            // the distance table: every position knows its moves left; the end is 0
            var s = Space("4x6 R1.4vU C1.2h C3.1v");
            for (int i = 0; i < s.Count; i++)
                if (s.Layout.IsSolved(s.StateAt(i))) Assert.AreEqual(0, s.DistanceAt(i));

            // same seed and tier: the same level, every time; inside its tier
            var table = TrafficDifficulty.Story();
            foreach (var tier in new[] { table.ForLevel(5), table.ForLevel(14) })
            {
                var settings = TrafficGenerator.Settings.FromTier(tier, 4000, 20000, 40);
                var a = TrafficGenerator.Generate(settings, 777, tier.id);
                var b = TrafficGenerator.Generate(settings, 777, tier.id);
                Assert.AreEqual(a.Level.cells, b.Level.cells, tier.id);
                Assert.AreEqual(a.Level.minMoves, b.Level.minMoves, tier.id);
                Assert.AreEqual(a.Level.minMoves, Space(a.Level.cells).MinMoves, "saved minimum = solver");
                CollectionAssert.IsEmpty(TrafficDifficulty.CheckTier(tier, a.Layout, a.Level.minMoves), tier.id);
            }
            var source = new TrafficChallenge(TrafficDifficulty.Challenge(), new ChallengeSettings(), 1200, 4000, 40, 60);
            Assert.AreEqual(ChallengeRules.Generate(source, 99, 8, 0).data, ChallengeRules.Generate(source, 99, 8, 0).data);
        }

        [Test]
        public void Hint_EachUseIsOneMoveCloser_NotCounted_TwoStarsAtMost()
        {
            var p = Load("4x6 R1.4vU C1.2h C3.1v", 3);
            Assert.AreEqual(3, p.MovesLeft);
            Assert.IsTrue(p.ApplyMove(new TrafficMove(1, 1, 0)), "a useless move: the car left");
            Assert.AreEqual(3, p.MovesLeft);
            int left = p.MovesLeft;
            while (!p.IsSolved)
            {
                Assert.IsTrue(p.TryFindHint(out var first) && p.TryFindHint(out var again) && first.Equals(again), "always the same move");
                Assert.IsTrue(p.ApplyHint(out _));
                Assert.AreEqual(left - 1, p.IsSolved ? 0 : p.MovesLeft, "one move closer");
                left--;
                Assert.AreEqual(1, p.MovesMade, "the light bulb's moves do not count");
            }
            Assert.IsTrue(p.HintUsed);
            Assert.IsFalse(p.TryFindHint(out _), "nothing to do on a solved level");
            var rules = new StarRules();
            Assert.AreEqual(2, StarRating.Compute(p.MovesMade, 3, rules, p.HintUsed), "fewer moves than the minimum, but a hint: 2 stars");
            Assert.IsTrue(p.Undo(out _) && p.MovesMade == 1, "a hint move can be undone, the counter stays");
            p.Restart();
            Assert.IsTrue(p.HintUsed, "a restart keeps the hint used");
        }

        [Test]
        public void Challenge_OutOfMoves_UndoFreesMoves_LostWithoutUndos_SaveAndResumeIdentical()
        {
            var run = ChallengeRunState.Start(TrafficJamModule.Id, 7);
            run.SetLevel(new ChallengeLevel { number = 1, data = Small, minMoves = 3, maxMoves = 4, tierId = "t" });
            var p = new TrafficPuzzle { MoveLimit = run.maxMoves };
            p.Load(TrafficChallenge.ToLevel(1, "t", 0, run.levelData, run.minMoves));
            // the car back and forth, then the blue car out: the maximum without the solution (the red car is still in)
            var moves = new[] { new TrafficMove(2, 1, 2), new TrafficMove(2, 2, 1), new TrafficMove(2, 1, 2), new TrafficMove(1, 2, TrafficLayout.Exited) };
            foreach (var m in moves)
            {
                Assert.IsTrue(p.ApplyMove(m));
                run.OnMove(m.Encode());
            }
            Assert.IsTrue(run.IsOutOfMoves(p.IsSolved), "out of moves");
            Assert.IsFalse(run.OutOfMovesIsLost(p.IsSolved), "undos left: the player may undo");
            Assert.IsFalse(p.ApplyMove(new TrafficMove(0, 4, TrafficLayout.Exited)), "the vehicles are locked at the maximum");

            // save here and resume: same positions (the blue car is out), same counters
            run.rotations = p.CaptureState();
            Assert.AreEqual('x', run.rotations[1], "the blue car is saved as out");
            var data = new SaveData();
            data.challenge.SetRun(run);
            var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(data)).challenge.ActiveRun(TrafficJamModule.Id);
            var resumed = new TrafficPuzzle { MoveLimit = loaded.maxMoves };
            resumed.Load(TrafficChallenge.ToLevel(loaded.level, loaded.tierId, loaded.levelSeed, loaded.levelData, loaded.minMoves));
            Assert.IsTrue(resumed.ReplayEncoded(loaded.undoHistory));
            Assert.AreEqual(loaded.rotations, resumed.CaptureState());
            Assert.AreEqual(p.State, resumed.State);
            Assert.AreEqual(loaded.moves, resumed.MovesMade);
            Assert.IsTrue(loaded.IsOutOfMoves(resumed.IsSolved));

            // an undo frees a move (the blue car comes back): no longer out of moves, the red car can leave
            Assert.IsTrue(run.TryUndo(out int code) && p.Undo(out var undone) && undone.Encode() == code && !p.IsOut(1));
            Assert.IsFalse(run.IsOutOfMoves(p.IsSolved));
            Assert.IsTrue(p.ApplyMove(new TrafficMove(0, 4, TrafficLayout.Exited)));
            run.OnMove(new TrafficMove(0, 4, TrafficLayout.Exited).Encode());
            Assert.IsTrue(run.IsOutOfMoves(p.IsSolved), "the blue car is still in: out of moves again");
            Assert.IsFalse(run.IsOutOfMoves(true), "a solution with the last move is no loss");

            // out of moves with no undo left: the run is lost (then Continue, as after any loss)
            var lostRun = ChallengeRunState.Start(TrafficJamModule.Id, 8);
            lostRun.SetLevel(new ChallengeLevel { number = 1, data = Small, minMoves = 3, maxMoves = 4 });
            lostRun.undosLeft = 0;
            foreach (var m in moves) lostRun.OnMove(m.Encode());
            Assert.IsTrue(lostRun.OutOfMovesIsLost(false));
            lostRun.OnFailed();
            Assert.IsTrue(lostRun.lost && lostRun.CanContinue);
            Assert.IsTrue(lostRun.Continue());
            Assert.AreEqual(0, lostRun.moves);
            Assert.AreEqual(ChallengeRunState.UndosPerRun, lostRun.undosLeft);
            Assert.IsFalse(lostRun.IsOutOfMoves(false));
        }

        [Test]
        public void Story_63LevelsInSevenChapters_NumberedInOrder_SavedMinimumIsExact()
        {
            var module = new TrafficJamModule();
            var levels = module.StoryLevels;
            Assert.AreEqual(63, levels.Count);
            var book = module.Story;
            Assert.AreEqual(7, book.Chapters.Count);
            Assert.IsTrue(book.Chapters.All(c => c.LevelCount == StoryBook.LevelsPerChapter));
            Assert.LessOrEqual(levels[0].minMoves, 2, "level 1: one or two moves");
            for (int i = 0; i < levels.Count; i++)
            {
                var l = levels[i];
                Assert.AreEqual(i + 1, l.number);
                Assert.AreEqual(i / 9 + 1, l.chapter, $"level {l.number}");
                Assert.AreEqual(i % 9, l.index, $"level {l.number}");
                Assert.AreEqual(l.minMoves, Space(l.cells).MinMoves, $"level {l.number}");
            }
            for (int c = 1; c < 7; c++)
                Assert.Less(levels[c * 9].minMoves, levels[c * 9 - 1].minMoves, $"chapter {c + 1} starts easier");
            Assert.IsTrue(levels.Any(l => l.CreateLayout(out _).TargetCount == 2), "red and blue target cars");
            Assert.IsTrue(levels.Any(l => l.CreateLayout(out _).SideExitCount > 0), "exits on other sides");
        }

        static TrafficSpace Space(string cells) => TrafficSpace.Explore(TrafficLayout.Parse(cells, out ulong start), start);
    }
}
