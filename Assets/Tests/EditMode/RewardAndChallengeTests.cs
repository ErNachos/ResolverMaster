using System;
using NUnit.Framework;
using ResolverMaster.Core.Ads;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.Save;
using ResolverMaster.Games.Plumber;

namespace ResolverMaster.Tests
{
    /// <summary>ErNa-007: rewarded ads, light bulb, Challenge.</summary>
    public sealed class RewardAndChallengeTests
    {
        sealed class FakePresenter : ITestAdPresenter
        {
            public bool Showing { get; set; }
            public Action Watched, Closed;

            public void Present(Action onWatched, Action onClosed)
            {
                Showing = true;
                Watched = () => { Showing = false; onWatched?.Invoke(); };
                Closed = () => { Showing = false; onClosed?.Invoke(); };
            }
        }

        [Test]
        public void Ads_ButtonsDisabledWithoutAnAd_RewardOnlyWhenWatched()
        {
            // no ad service, or no ad available: the buttons that need one are disabled
            Assert.IsFalse(RewardedAds.CanOffer(null, true));
            var none = new UnavailableRewardedAdService();
            Assert.IsFalse(RewardedAds.CanOffer(none, true));
            bool failed = false;
            none.Show(() => Assert.Fail("no reward without an ad"), () => failed = true);
            Assert.IsTrue(failed);

            // the test ad: available, but not while it is already on screen, nor when switched off, nor when the action is pointless
            var presenter = new FakePresenter();
            var test = new TestRewardedAdService(presenter);
            Assert.IsTrue(RewardedAds.CanOffer(test, true));
            Assert.IsFalse(RewardedAds.CanOffer(test, false), "nothing to fix: disabled even with an ad");
            int rewards = 0, closes = 0;
            test.Show(() => rewards++, () => closes++);
            Assert.IsFalse(test.IsAvailable, "one ad at a time");
            presenter.Closed();
            Assert.AreEqual(0, rewards, "closed early: no reward");
            Assert.AreEqual(1, closes);
            test.Show(() => rewards++, () => closes++);
            presenter.Watched();
            Assert.AreEqual(1, rewards);
            Assert.IsTrue(test.IsAvailable);
            test.Enabled = false;
            Assert.IsFalse(RewardedAds.CanOffer(test, true));
        }

        [Test]
        public void Challenge_SameSeedSameLevel_EveryThread_MaxAtLeastMinPlusOne_StaysOnTheLastStep()
        {
            var source = new PlumberChallenge(PlumberDifficulty.Challenge(), new ChallengeSettings(), 4000);
            var queue = new ChallengeQueue(source);
            queue.Reset(777, 0);
            int lastStep = -1;
            for (int n = 1; n <= 24; n++)
            {
                var a = ChallengeRules.Generate(source, 777, n, 0);
                var b = queue.Get(n);   // generated on a worker thread: same result
                Assert.AreEqual(a.data, b.data, $"level {n}");
                Assert.AreEqual(a.minMoves, b.minMoves);
                Assert.AreEqual(a.maxMoves, b.maxMoves);
                Assert.GreaterOrEqual(a.maxMoves, a.minMoves + 1, $"level {n}");
                Assert.GreaterOrEqual(a.step, lastStep, "steps never go back");
                lastStep = a.step;
                var grid = PlumberLevelCodec.Parse(a.data);
                Assert.AreEqual(a.minMoves, PlumberSolver.Solve(grid).MinMoves, "exact minimum");
            }
            // the level seed depends only on (run seed, level, changes): another change gives another level
            Assert.AreEqual(ChallengeRules.LevelSeed(777, 5, 0), ChallengeRules.LevelSeed(777, 5, 0));
            Assert.AreNotEqual(ChallengeRules.LevelSeed(777, 5, 0), ChallengeRules.LevelSeed(777, 5, 1));
            Assert.AreNotEqual(ChallengeRules.Generate(source, 777, 5, 0).data, ChallengeRules.Generate(source, 777, 5, 1).data);

            // the margin goes from 60% to 10% of the minimum, never below +1; after the table the run stays on the last step
            var s = new ChallengeSettings();
            int steps = source.Table.tiers.Count;
            Assert.AreEqual(16, ChallengeRules.MaxMoves(10, 0, steps, s));
            Assert.AreEqual(11, ChallengeRules.MaxMoves(10, steps - 1, steps, s));
            Assert.AreEqual(3, ChallengeRules.MaxMoves(2, steps - 1, steps, s), "never less than +1");
            ChallengeRules.StepFor(source.Table, 10000, out int step);
            Assert.AreEqual(steps - 1, step);
        }

        [Test]
        public void Run_MaxBlocksTurns_ThreeUndosTakeTheMoveBack_LostOnError_StarsAndRecord_SaveAndResumeIdentical()
        {
            var source = new PlumberChallenge(PlumberDifficulty.Challenge(), new ChallengeSettings(), 4000);
            var run = ChallengeRunState.Start("plumber", 4242);
            run.SetLevel(ChallengeRules.Generate(source, run.runSeed, 1, 0));
            var level = PlumberChallenge.ToLevel(new ChallengeLevel { number = 1, data = run.levelData, minMoves = run.minMoves, seed = run.levelSeed });
            var puzzle = new PlumberPuzzle { MoveLimit = run.maxMoves };
            puzzle.Load(level);

            // tap the first rotatable piece until the maximum: then pieces no longer turn
            int cell = System.Array.FindIndex(puzzle.Grid.Pieces, p => p.IsRotatable);
            for (int i = 0; i < run.maxMoves; i++)
            {
                Assert.IsTrue(run.CanMove);
                Assert.IsTrue(puzzle.ApplyMove(cell));
                run.OnMove(cell);
            }
            Assert.IsFalse(run.CanMove);
            Assert.IsFalse(puzzle.ApplyMove(cell), "at the maximum the pieces do not turn");
            Assert.AreEqual(run.maxMoves, puzzle.MovesMade);

            // save in the middle of the level and resume: the same level, the same rotations, the same counters
            run.rotations = Rotations(puzzle.Grid);
            var data = new SaveData();
            data.challenge.SetRun(run);
            var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(data)).challenge.ActiveRun("plumber");
            Assert.IsNotNull(loaded);
            Assert.AreEqual(run.levelData, loaded.levelData);
            Assert.AreEqual(run.maxMoves, loaded.maxMoves);
            Assert.AreEqual(run.moves, loaded.moves);
            CollectionAssert.AreEqual(run.undoHistory, loaded.undoHistory);
            var resumed = new PlumberPuzzle { MoveLimit = loaded.maxMoves };
            resumed.Load(PlumberChallenge.ToLevel(new ChallengeLevel { number = loaded.level, data = loaded.levelData, minMoves = loaded.minMoves }));
            Assert.IsTrue(resumed.Replay(loaded.undoHistory));
            Assert.AreEqual(loaded.rotations, Rotations(resumed.Grid));
            Assert.AreEqual(loaded.moves, resumed.MovesMade);

            // 3 undos per run, each takes the move back
            for (int i = 0; i < ChallengeRunState.UndosPerRun; i++)
            {
                int movesBefore = run.moves;
                Assert.IsTrue(run.TryUndo(out int undone));
                Assert.AreEqual(cell, undone);
                Assert.AreEqual(movesBefore - 1, run.moves);
            }
            Assert.IsFalse(run.TryUndo(out _), "no fourth undo in the run");
            Assert.IsTrue(run.CanMove, "undoing frees moves again");

            // stars add up, the next level comes; an error of the water ends the run; the record keeps the best (stars, then levels)
            run.OnSolved(3);
            run.OnSolved(2);
            Assert.AreEqual(5, run.stars);
            Assert.AreEqual(2, run.levelsCleared);
            Assert.AreEqual(3, run.level);
            Assert.IsFalse(run.HasLevel);
            run.OnFailed();
            Assert.IsTrue(run.lost, "ErNa-008: the lifeline is still there, the run waits for Continue");
            Assert.IsFalse(run.CanMove);
            run.Finish();
            Assert.IsTrue(run.over);
            Assert.IsFalse(run.CanMove);
            var block = new ChallengeBlock();
            Assert.IsTrue(block.SubmitScore("plumber", run.stars, run.levelsCleared), "first record");
            Assert.IsFalse(block.SubmitScore("plumber", 5, 1), "same stars, fewer levels: not better");
            Assert.IsTrue(block.SubmitScore("plumber", 5, 3), "same stars, more levels: better");
            Assert.IsFalse(block.SubmitScore("plumber", 4, 9));
            Assert.AreEqual(5, block.Record("plumber").stars);
            Assert.AreEqual(3, block.Record("plumber").levels);
        }

        [Test]
        public void Continue_OncePerRun_SameLevelFromTheStart_UndosBack_StarsKept_SavedWithTheRun_NoAdNoButton()
        {
            var source = new PlumberChallenge(PlumberDifficulty.Challenge(), new ChallengeSettings(), 4000);
            var run = ChallengeRunState.Start("plumber", 99);
            run.OnSolved(3);
            run.OnSolved(2);
            run.SetLevel(ChallengeRules.Generate(source, run.runSeed, run.level, run.changes));
            string levelData = run.levelData;
            run.OnMove(0);
            run.OnMove(1);
            Assert.IsTrue(run.TryUndo(out _));
            Assert.IsTrue(run.TryUndo(out _));
            Assert.AreEqual(1, run.undosLeft);
            Assert.IsFalse(run.CanContinue, "nothing lost yet");

            // lost: the run waits; without an ad the Continue button is disabled
            run.OnFailed();
            Assert.IsTrue(run.CanContinue);
            Assert.IsFalse(run.over);
            Assert.IsFalse(RewardedAds.CanOffer(new UnavailableRewardedAdService(), run.CanContinue));
            Assert.IsTrue(RewardedAds.CanOffer(new TestRewardedAdService(null), run.CanContinue));

            // the choice survives a save (closing the app on the end panel)
            var data = new SaveData();
            data.challenge.SetRun(run);
            run = SaveSerializer.FromJson(SaveSerializer.ToJson(data)).challenge.ActiveRun("plumber");
            Assert.IsNotNull(run, "a lost run waiting for Continue is still the active run");
            Assert.IsTrue(run.CanContinue);

            // Continue: the same level from the start, 0 moves, 3 undos; stars and levels cleared stay
            run.OnMove(5);   // ignored by the rules below: a lost run takes no moves in the game, but the state must reset anyway
            Assert.IsTrue(run.Continue());
            Assert.AreEqual(3, run.level);
            Assert.AreEqual(levelData, run.levelData, "the same level");
            Assert.AreEqual(0, run.moves);
            Assert.AreEqual(0, run.undoHistory.Count);
            Assert.AreEqual(ChallengeRunState.UndosPerRun, run.undosLeft);
            Assert.AreEqual(5, run.stars);
            Assert.AreEqual(2, run.levelsCleared);
            Assert.IsTrue(run.lifelineUsed);
            Assert.IsTrue(run.CanMove);
            var reloaded = SaveSerializer.FromJson(SaveSerializer.ToJson(data)).challenge;
            reloaded.SetRun(run);
            Assert.IsTrue(SaveSerializer.FromJson(SaveSerializer.ToJson(new SaveData { challenge = reloaded })).challenge.ActiveRun("plumber").lifelineUsed);

            // only once: the next failure ends the run
            run.OnFailed();
            Assert.IsFalse(run.CanContinue);
            Assert.IsFalse(run.Continue());
            Assert.IsTrue(run.over);
            Assert.IsFalse(RewardedAds.CanOffer(new TestRewardedAdService(null), run.CanContinue), "no second Continue");
        }

        static string Rotations(PipeGrid grid)
        {
            var chars = new char[grid.CellCount];
            for (int c = 0; c < chars.Length; c++) chars[c] = (char)('0' + grid.Pieces[c].Rotation);
            return new string(chars);
        }

        [Test]
        public void Hint_FixesThePieceClosestToASource_NotAMove_TwoStarsAtMost_SurvivesRestart()
        {
            // S> | | <E : both straights need one tap; the one next to the source is hinted first
            var level = new PlumberLevel { minMoves = 2, cells = "S1 I0 I0 E3" };
            var puzzle = new PlumberPuzzle();
            puzzle.Load(level);
            Assert.IsTrue(puzzle.ApplyMove(1));
            Assert.IsTrue(puzzle.ApplyMove(1), "turned twice: wrong again");
            Assert.IsTrue(puzzle.TryFindHint(out int cell, out int taps));
            Assert.AreEqual(1, cell, "the piece the water reaches first");
            Assert.AreEqual(1, taps);

            Assert.IsTrue(puzzle.ApplyHint(out cell, out _));
            Assert.AreEqual(2, puzzle.MovesMade, "the hint is not a move");
            Assert.IsTrue(puzzle.HintUsed);
            Assert.IsTrue(puzzle.Locks.IsPinned(1));
            int fixedRotation = puzzle.Grid.Pieces[1].Rotation;
            Assert.IsFalse(puzzle.ApplyMove(1), "a fixed piece no longer turns");
            Assert.IsTrue(puzzle.Undo(out int undone));
            Assert.AreEqual(1, undone);
            Assert.AreEqual(fixedRotation, puzzle.Grid.Pieces[1].Rotation, "undo removes the move, the fixed piece stays");
            Assert.AreEqual(1, puzzle.MovesMade);

            // the next hint is the other straight; after it nothing is left to fix
            Assert.IsTrue(puzzle.TryFindHint(out cell, out _));
            Assert.AreEqual(2, cell);
            Assert.IsTrue(puzzle.ApplyHint(out _, out _));
            Assert.IsFalse(puzzle.TryFindHint(out _, out _), "every piece right: the light bulb is disabled");
            Assert.IsTrue(puzzle.OpenWater().Success);
            var rules = new StarRules();
            Assert.AreEqual(3, StarRating.Compute(1, 2, rules));
            Assert.AreEqual(2, StarRating.Compute(1, 2, rules, hintUsed: true), "with a hint: at most 2 stars");

            // a restart (e.g. after a failed opening) keeps the fixed pieces and the 2-star limit; a new load forgets them
            puzzle.Load(level);
            Assert.IsTrue(puzzle.ApplyHint(out _, out _));
            fixedRotation = puzzle.Grid.Pieces[1].Rotation;
            Assert.IsFalse(puzzle.OpenWater().Success, "the second straight is still wrong");
            puzzle.Restart();
            Assert.AreEqual(0, puzzle.MovesMade);
            Assert.IsTrue(puzzle.Locks.IsPinned(1));
            Assert.AreEqual(fixedRotation, puzzle.Grid.Pieces[1].Rotation);
            Assert.IsTrue(puzzle.HintUsed);
            puzzle.Load(level);
            Assert.IsFalse(puzzle.Locks.IsPinned(1));
            Assert.IsFalse(puzzle.HintUsed);
        }
    }
}
