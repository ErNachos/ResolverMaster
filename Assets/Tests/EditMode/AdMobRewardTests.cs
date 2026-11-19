using System;
using NUnit.Framework;
using ResolverMaster.Core.Ads;

namespace ResolverMaster.Tests
{
    /// <summary>ErNa-013: the rules of the real rewarded ads (<see cref="RewardedAdController"/>) with a fake network.</summary>
    public sealed class AdMobRewardTests
    {
        sealed class FakeProvider : IRewardedAdProvider
        {
            public bool IsReady { get; set; }
            public bool IsLoading { get; private set; }
            public int Loads, Shows;
            Action<bool> _loadDone;
            public Action Earned, Closed, Failed;

            public void Load(Action<bool> done)
            {
                Loads++;
                IsLoading = true;
                _loadDone = done;
            }

            public void FinishLoad(bool ok)
            {
                IsLoading = false;
                IsReady = ok;
                _loadDone?.Invoke(ok);
            }

            public void Show(Action earned, Action closed, Action failed)
            {
                Shows++;
                IsReady = false;
                Earned = earned;
                Closed = closed;
                Failed = failed;
            }
        }

        sealed class FakeHost : IAdHost
        {
            public int Started, Ended, NotAvailable;
            public void AdStarted() => Started++;
            public void AdEnded() => Ended++;
            public void ShowNotAvailable() => NotAvailable++;
        }

        static RewardedAdController Ready(out FakeProvider provider, out FakeHost host)
        {
            provider = new FakeProvider();
            host = new FakeHost();
            var ads = new RewardedAdController(provider, host, watchdogSeconds: 8f, retrySeconds: 30f);
            ads.Start();
            provider.FinishLoad(true);
            return ads;
        }

        [Test]
        public void Reward_OnlyWhenTheNetworkSaysEarned_NeverForAMereClose()
        {
            var ads = Ready(out var provider, out var host);
            Assert.AreEqual(1, provider.Loads, "preloaded once the SDK can request ads");
            int rewards = 0, noReward = 0;

            // watched to the end: earned, then closed -> one reward, after the close only
            ads.Show(() => rewards++, () => noReward++);
            Assert.IsTrue(ads.IsShowing);
            Assert.IsFalse(RewardedAds.CanOffer(ads, true), "one ad at a time");
            provider.Earned();
            ads.Tick(0.016f, hasFocus: false);
            Assert.AreEqual(0, rewards, "nothing while the ad is still open");
            provider.Closed();
            ads.Tick(0.016f, true);
            ads.Tick(0.016f, true);
            Assert.AreEqual(1, rewards);
            Assert.AreEqual(0, noReward);
            Assert.IsFalse(ads.IsShowing);

            // closed early: no reward
            provider.FinishLoad(true);
            ads.Show(() => rewards++, () => noReward++);
            provider.Closed();
            ads.Tick(0.016f, true);
            ads.Tick(0.016f, true);
            Assert.AreEqual(1, rewards, "closed before the end: no reward");
            Assert.AreEqual(1, noReward);

            // "earned" arriving right after the close (same frame) still counts
            provider.FinishLoad(true);
            ads.Show(() => rewards++, () => noReward++);
            provider.Closed();
            provider.Earned();
            ads.Tick(0.016f, true);
            ads.Tick(0.016f, true);
            Assert.AreEqual(2, rewards);
            Assert.AreEqual(1, noReward);
            Assert.AreEqual(3, host.Started);
            Assert.AreEqual(3, host.Ended, "input and sound always given back");
            Assert.AreEqual(0, host.NotAvailable);
        }

        [Test]
        public void NotReadyOrNoConsent_NoReward_MessageShown_NewLoadStarted()
        {
            // no consent / SDK not initialised: nothing is loaded, a tap only shows the message
            var provider = new FakeProvider();
            var host = new FakeHost();
            var ads = new RewardedAdController(provider, host);
            Assert.IsTrue(RewardedAds.CanOffer(ads, true), "the buttons answer the tap even without an ad");
            bool failed = false;
            ads.Show(() => Assert.Fail("no reward"), () => failed = true);
            Assert.IsTrue(failed);
            Assert.AreEqual(1, host.NotAvailable);
            Assert.AreEqual(0, provider.Loads, "no request before the consent allows it");
            Assert.AreEqual(0, host.Started);

            // started, but the first ad did not load: message, no reward, a new load at once
            ads.Start();
            provider.FinishLoad(false);
            failed = false;
            ads.Show(() => Assert.Fail("no reward"), () => failed = true);
            Assert.IsTrue(failed);
            Assert.AreEqual(2, host.NotAvailable);
            Assert.AreEqual(2, provider.Loads, "a tap without an ad starts a new load");
            Assert.AreEqual(0, provider.Shows);

            // a load still running is not started twice
            ads.Show(null, null);
            Assert.AreEqual(2, provider.Loads);
        }

        [Test]
        public void FailedShow_NoReward_Message_AndTheNextAdIsLoaded()
        {
            var ads = Ready(out var provider, out var host);
            int rewards = 0, noReward = 0;
            ads.Show(() => rewards++, () => noReward++);
            provider.Earned();   // even an "earned" never pays for an ad that failed
            provider.Failed();
            ads.Tick(0.016f, true);
            ads.Tick(0.016f, true);
            Assert.AreEqual(0, rewards);
            Assert.AreEqual(1, noReward);
            Assert.AreEqual(1, host.NotAvailable, "failed: the message");
            Assert.AreEqual(1, host.Ended);
            Assert.AreEqual(2, provider.Loads, "the next ad is requested right away");
            Assert.IsTrue(provider.IsLoading);
        }

        [Test]
        public void Reload_AfterEveryAd_RetryAfterAFailedLoad_WatchdogNeverLeavesTheGameBlocked()
        {
            var ads = Ready(out var provider, out var host);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(provider.IsReady);
                ads.Show(null, null);
                provider.Earned();
                provider.Closed();
                ads.Tick(0.016f, true);
                ads.Tick(0.016f, true);
                Assert.AreEqual(2 + i, provider.Loads, "a new load after every ad");
                provider.FinishLoad(true);
            }

            // a failed load is tried again after the retry time
            ads.Show(null, null);
            provider.Closed();
            ads.Tick(0.016f, true);
            ads.Tick(0.016f, true);
            int loads = provider.Loads;
            provider.FinishLoad(false);
            ads.Tick(29f, true);
            Assert.AreEqual(loads, provider.Loads);
            ads.Tick(2f, true);
            Assert.AreEqual(loads + 1, provider.Loads, "retried");
            provider.FinishLoad(true);

            // the close never arrives: while the ad covers the app nothing happens, with the focus back the watchdog ends it
            int rewards = 0, noReward = 0;
            ads.Show(() => rewards++, () => noReward++);
            ads.Tick(60f, hasFocus: false);
            Assert.IsTrue(ads.IsShowing, "the ad is in front: wait");
            ads.Tick(5f, true);
            Assert.IsTrue(ads.IsShowing);
            ads.Tick(5f, true);
            Assert.IsFalse(ads.IsShowing, "the game is never left blocked");
            Assert.AreEqual(0, rewards, "no 'earned': no reward");
            Assert.AreEqual(1, noReward);
            Assert.AreEqual(host.Started, host.Ended);
        }
    }
}
