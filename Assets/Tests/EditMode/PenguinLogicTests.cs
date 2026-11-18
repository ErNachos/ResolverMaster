using System.Linq;
using NUnit.Framework;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.Save;
using ResolverMaster.Games.Penguin;

namespace ResolverMaster.Tests
{
    /// <summary>PG-001: Pino rules, exact solver, "You are stuck", light bulb, generator, Challenge save, Story levels.</summary>
    public sealed class PenguinLogicTests
    {
        const int Up = SquareTopology.Up, Right = SquareTopology.Right, Down = SquareTopology.Down, Left = SquareTopology.Left;

        static PenguinPuzzle Load(string cells, int minMoves = 0)
        {
            var puzzle = new PenguinPuzzle();
            puzzle.Load(new PenguinLevel { number = 1, cells = cells, minMoves = minMoves });
            return puzzle;
        }

        static int Pos(PenguinPuzzle p, int penguin = 0) => PenguinLayout.PenguinPos(p.State, penguin);

        static int Cell(PenguinPuzzle p, int x, int y) => p.Layout.Cell(x, y);

        [Test]
        public void Slide_StopsOnRockWallAndInSnow_FallsInWater_EmptySwipeIsNoMove_SwipeDuringAnimationIgnored()
        {
            // 5x4: penguin (2,0), rock (4,0), snow (2,2), water hole (0,3), igloo (4,3)
            var p = Load("5x4:..P.#/...../..s../w...I");
            Assert.IsTrue(p.Swipe(Right));
            Assert.AreEqual(Cell(p, 3, 0), Pos(p), "slides on the ice and stops before the rock");
            Assert.IsFalse(p.Swipe(Up), "the snow wall on top: nothing moves");
            Assert.AreEqual(1, p.MovesMade, "an empty swipe is no move");
            Assert.IsTrue(p.Undo(out _) && Pos(p) == Cell(p, 2, 0));
            Assert.IsTrue(p.Swipe(Down));
            Assert.AreEqual(Cell(p, 2, 2), Pos(p), "stops in the snow");
            Assert.IsTrue(p.Swipe(Down));
            Assert.AreEqual(Cell(p, 2, 3), Pos(p), "leaves the snow, stops against the bottom wall");
            Assert.IsTrue(p.Swipe(Left));
            Assert.IsTrue(p.Fell, "falls into the water hole");
            Assert.IsFalse(p.IsSolved);
            Assert.IsFalse(p.Swipe(Right), "no move after a fall");
            p.Restart();
            Assert.IsFalse(p.Fell);
            Assert.AreEqual(0, p.MovesMade, "the level starts again with 0 moves");
            Assert.IsTrue(p.Swipe(Down) && p.Swipe(Down) && p.Swipe(Right));
            Assert.IsTrue(p.IsSolved, "passes over the open igloo: in");

            // water on the border (left side of the top row): sliding off the grid there is a fall
            var edge = Load("4x2:..P./I...:mmmmmmmmmmwm");
            var trace = new PenguinTrace();
            Assert.IsTrue(edge.TryPlan(Left, out var fall, trace) && fall.Fell);
            Assert.AreEqual(PenguinTrackEnd.FallOff, trace.Tracks.Single().End);
            Assert.IsTrue(edge.Swipe(Down), "the snow wall of the bottom row stops it");
            Assert.IsFalse(edge.Fell);

            // swipes during the slide are ignored
            var play = new PenguinPlay(Load("5x4:..P.#/...../..s../w...I")) { CellsPerSecond = 10f };
            Assert.IsTrue(play.Swipe(Right));
            Assert.IsTrue(play.Busy);
            Assert.IsFalse(play.Swipe(Down), "ignored while sliding");
            Assert.AreEqual(1, play.Puzzle.MovesMade);
            Assert.IsFalse(play.Tick(0.05f), "still sliding");
            Assert.IsTrue(play.Tick(1f), "the slide ends");
            Assert.IsFalse(play.Busy);
            Assert.IsTrue(play.Swipe(Down));
            Assert.AreEqual(2, play.Puzzle.MovesMade);
        }

        [Test]
        public void FishOpenTheIgloo_PassingOverEnters_BlocksSlideStopAndFillHoles()
        {
            // the igloo is closed until the fish is taken: the penguin slides over it
            var p = Load("5x3:P.I.f/...../.....");
            var trace = new PenguinTrace();
            Assert.IsTrue(p.Swipe(Right, trace));
            Assert.AreEqual(Cell(p, 4, 0), Pos(p), "over the closed igloo, the fish taken, stops at the wall");
            Assert.IsTrue(p.Layout.IglooOpen(p.State));
            Assert.IsTrue(trace.Events.Any(e => e.Kind == PenguinTrace.EventKind.Fish && e.Time == 4));
            Assert.IsTrue(trace.Events.Any(e => e.Kind == PenguinTrace.EventKind.IglooOpen && e.Time == 4));
            Assert.IsTrue(p.Swipe(Left));
            Assert.IsTrue(p.IsSolved, "in as soon as it passes over the open igloo");
            Assert.AreEqual(2, p.Space.MinMoves);

            // a block: the penguin stops before it, the block slides on and sinks into the hole, which becomes ice
            var b = Load("6x3:P.c..w/....../.....I");
            Assert.IsTrue(b.Swipe(Right, trace));
            Assert.AreEqual(Cell(b, 1, 0), Pos(b));
            Assert.AreEqual(PenguinLayout.Gone, PenguinLayout.BlockPos(b.State, 0), "sunk");
            Assert.IsFalse(b.Layout.IsWater(b.State, Cell(b, 5, 0)), "the hole is ice now");
            Assert.IsTrue(trace.Tracks.Any(t => t.End == PenguinTrackEnd.Sink) && trace.Tracks.Any(t => t.End == PenguinTrackEnd.Push));
            Assert.IsTrue(b.Swipe(Right));
            Assert.IsFalse(b.Fell, "slides over the filled hole");
            Assert.AreEqual(Cell(b, 5, 0), Pos(b));
            Assert.AreEqual(2, b.Space.MinMoves, "down, right (hand-verified)");

            // a block that cannot move is a rock; a block stops in the snow
            var stuck = Load("4x2:P.c#/I...");
            Assert.IsTrue(stuck.Swipe(Right));
            Assert.AreEqual(Cell(stuck, 1, 0), Pos(stuck));
            Assert.AreEqual(Cell(stuck, 2, 0), PenguinLayout.BlockPos(stuck.State, 0), "the block did not move");
            Assert.IsFalse(stuck.Swipe(Right), "nothing can move: no move");
            var snow = Load("6x2:Pc..s./I.....");
            Assert.IsTrue(snow.Swipe(Right));
            Assert.AreEqual(Cell(snow, 0, 0), Pos(snow), "next to the block: the penguin stays");
            Assert.AreEqual(Cell(snow, 4, 0), PenguinLayout.BlockPos(snow.State, 0), "the block stops in the snow");
        }

        [Test]
        public void TwoPenguins_MoveTogether_StopAgainstEachOther_EnterSeparately()
        {
            var p = Load("5x2:P.Q../I...J");
            Assert.IsTrue(p.Swipe(Right));
            Assert.AreEqual(Cell(p, 4, 0), Pos(p, 1), "the front penguin stops at the wall");
            Assert.AreEqual(Cell(p, 3, 0), Pos(p, 0), "the other one against it");
            Assert.IsTrue(p.Swipe(Down));
            Assert.IsTrue(p.Layout.IsIn(p.State, 1), "the second penguin is in its igloo");
            Assert.IsFalse(p.Layout.IsIn(p.State, 0));
            Assert.IsFalse(p.IsSolved);
            Assert.IsTrue(p.Swipe(Left));
            Assert.IsTrue(p.IsSolved, "both in");

            // the first one in disappears: the other one slides on over the cell
            var q = Load("6x2:P.Q..J/I.....");
            Assert.IsTrue(q.Swipe(Right));
            Assert.IsTrue(q.Layout.IsIn(q.State, 1));
            Assert.AreEqual(Cell(q, 5, 0), Pos(q, 0), "over the other igloo, like ice");
            Assert.IsTrue(q.Swipe(Left) && q.Swipe(Down) && q.IsSolved);
            Assert.AreEqual(3, q.MovesMade);
        }

        [Test]
        public void Stuck_DetectedExactly_LightBulbUndoesOutOfIt_ThenMakesTheNextMove_NotCounted_TwoStars()
        {
            // 3x3: rock (1,1), snow (2,1), igloo (0,2), rock (2,2); water on the border above (2,0) and right of (2,1)
            var p = Load("3x3:P../.#s/I.#:mmwmwmmmmmmm", 1);
            Assert.AreEqual(1, p.Space.MinMoves, "down: straight into the igloo (hand-verified)");
            Assert.IsTrue(p.Swipe(Right));
            Assert.IsFalse(p.IsStuck, "from (2,0) the penguin can still go back");
            Assert.IsTrue(p.Swipe(Down));
            Assert.AreEqual(Cell(p, 2, 1), Pos(p), "in the snow");
            Assert.IsTrue(p.IsStuck, "every swipe from the snow falls or does nothing");
            var alone = PenguinSpace.Explore(p.Layout, p.State);
            Assert.IsFalse(alone.Solvable, "a search from that state alone agrees");
            Assert.IsTrue(p.Space.TryNextMove(p.Layout.Start, out int first) && first == Down);

            // the light bulb when stuck: back to the last state with a solution
            Assert.AreEqual(PenguinHint.Undone, p.ApplyHint(out _));
            Assert.AreEqual(Cell(p, 2, 0), Pos(p));
            Assert.IsFalse(p.IsStuck);
            Assert.AreEqual(1, p.MovesMade);
            // then the next right move, which does not count
            Assert.AreEqual(PenguinHint.Moved, p.ApplyHint(out var hinted));
            Assert.AreEqual(Left, hinted.Dir);
            Assert.AreEqual(1, p.MovesMade, "the light bulb's move does not count");
            Assert.IsTrue(p.Swipe(Down) && p.IsSolved);
            Assert.IsTrue(p.HintUsed);
            Assert.AreEqual(2, StarRating.Compute(p.MovesMade, 1, new StarRules(), p.HintUsed), "at most 2 stars with the light bulb");

            // Challenge: stuck with undos left = "You are stuck" (undo or end); with none = lost
            var run = ChallengeRunState.Start(PenguinModule.Id, 3);
            run.SetLevel(new ChallengeLevel { number = 1, data = "3x3:P../.#s/I.#:mmwmwmmmmmmm", minMoves = 1, maxMoves = 4 });
            Assert.IsFalse(run.StuckIsLost(true));
            run.undosLeft = 0;
            Assert.IsTrue(run.StuckIsLost(true));
            Assert.IsFalse(run.StuckIsLost(false));
        }

        [Test]
        public void Generator_SameSeedSameLevel_UsesTheNovelty_SaveAndResumeIdentical()
        {
            var table = PenguinDifficulty.Story();
            foreach (int n in new[] { 10, 22, 32, 41, 52 })
            {
                var tier = table.ForLevel(n);
                var settings = PenguinGenerator.Settings.FromTier(tier, 3000, 40000, 30, 40);
                var a = PenguinGenerator.Generate(settings, 4321 + n, tier.id);
                var b = PenguinGenerator.Generate(settings, 4321 + n, tier.id);
                Assert.AreEqual(a.Level.cells, b.Level.cells, tier.id);
                var layout = PenguinLayout.Parse(a.Level.cells);
                var space = PenguinSpace.Explore(layout, layout.Start);
                Assert.AreEqual(a.Level.minMoves, space.MinMoves, "saved minimum = solver");
                CollectionAssert.IsEmpty(PenguinDifficulty.CheckTier(tier, layout, space.MinMoves), tier.id);
                CollectionAssert.IsEmpty(PenguinGenerator.Problems(layout, space, settings), tier.id + ": uses its novelty");
            }

            // Challenge: same run seed = same level; save in the middle of a level and resume: the same state
            var cfg = UnityEngine.ScriptableObject.CreateInstance<PenguinConfig>();
            var source = cfg.CreateChallenge();
            var level = ChallengeRules.Generate(source, 99, 47, 0);
            Assert.AreEqual(level.data, ChallengeRules.Generate(source, 99, 47, 0).data);
            Assert.AreEqual(2, PenguinLayout.Parse(level.data).PenguinCount, "step ch-12: two penguins");
            var run = ChallengeRunState.Start(PenguinModule.Id, 99);
            run.SetLevel(level);
            var p = new PenguinPuzzle { MoveLimit = run.maxMoves };
            p.Load(PenguinChallenge.ToLevel(run.level, run.tierId, run.levelSeed, run.levelData, run.minMoves));
            var dirs = p.Space.Solution(p.State);
            for (int i = 0; i < dirs.Count - 1; i++)
            {
                Assert.IsTrue(p.Swipe(dirs[i]));
                run.OnMove(dirs[i]);
            }
            run.rotations = p.CaptureState();
            var data = new SaveData();
            data.challenge.SetRun(run);
            var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(data)).challenge.ActiveRun(PenguinModule.Id);
            var resumed = new PenguinPuzzle { MoveLimit = loaded.maxMoves };
            resumed.Load(PenguinChallenge.ToLevel(loaded.level, loaded.tierId, loaded.levelSeed, loaded.levelData, loaded.minMoves));
            Assert.IsTrue(resumed.ReplayEncoded(loaded.undoHistory));
            Assert.AreEqual(p.State, resumed.State, "penguins, fish, blocks, holes and penguins in: the same");
            Assert.AreEqual(loaded.rotations, resumed.CaptureState());
            Assert.AreEqual(loaded.moves, resumed.MovesMade);
            Assert.IsTrue(resumed.Swipe(dirs[dirs.Count - 1]) && resumed.IsSolved, "one move from the end, as before");
            UnityEngine.Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Story_63LevelsInSevenChapters_NumberedInOrder_SavedMinimumIsExact()
        {
            var module = new PenguinModule();
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
                var layout = l.CreateLayout();
                Assert.AreEqual(l.minMoves, PenguinSpace.Explore(layout, layout.Start).MinMoves, $"level {l.number}");
            }
            for (int c = 1; c < 7; c++)
                Assert.Less(levels[c * 9].minMoves, levels[c * 9 - 1].minMoves, $"chapter {c + 1} starts easier");
            Assert.IsTrue(levels.Skip(9).Take(9).All(l => l.CreateLayout().FishCount > 0), "chapter 2: fish");
            Assert.IsTrue(levels.Skip(36).Take(9).All(l => l.CreateLayout().BlockCount > 0), "chapter 5: ice blocks");
            Assert.IsTrue(levels.Skip(45).Take(9).All(l => l.CreateLayout().PenguinCount == 2), "chapter 6: two penguins");
        }
    }
}
