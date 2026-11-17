using System;
using NUnit.Framework;
using ResolverMaster.Core.Ads;

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
    }
}
