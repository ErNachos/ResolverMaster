using System;
using NUnit.Framework;
using ResolverMaster.Core.Ads;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
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
