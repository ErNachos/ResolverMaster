using System;
using UnityEngine;

namespace ResolverMaster.Core.Ads
{
    /// <summary>
    /// A rewarded ad (ErNa-007): the player watches it and gets something (e.g. a hint). The game never knows which network is
    /// behind it: the test version today (<see cref="TestRewardedAdService"/>), AdMob later, chosen in one place
    /// (<see cref="RewardedAds.Create"/>).
    /// </summary>
    public interface IRewardedAdService
    {
        /// <summary>An ad can be shown now. Buttons that need an ad are disabled while this is false.</summary>
        bool IsAvailable { get; }

        /// <summary>Shows the ad. Exactly one of the callbacks is called: watched to the end, or failed / closed early.</summary>
        void Show(Action onRewarded, Action onFailedOrClosed);
    }

    /// <summary>No ads at all (release builds until AdMob arrives, or no network): every ad button stays disabled.</summary>
    public sealed class UnavailableRewardedAdService : IRewardedAdService
    {
        public bool IsAvailable => false;

        public void Show(Action onRewarded, Action onFailedOrClosed) => onFailedOrClosed?.Invoke();
    }

    /// <summary>
    /// The test ad: a "Test ad" panel with a short wait and two buttons (watched / closed). Active in the Editor and in the test
    /// builds. The panel is UI, given by the app (<see cref="ITestAdPresenter"/>), so this class stays pure and testable.
    /// </summary>
    public sealed class TestRewardedAdService : IRewardedAdService
    {
        readonly ITestAdPresenter _presenter;

        /// <summary>Tests and previews can switch the test ad off.</summary>
        public bool Enabled = true;

        public TestRewardedAdService(ITestAdPresenter presenter) => _presenter = presenter;

        public bool IsAvailable => Enabled && (_presenter == null || !_presenter.Showing);

        public void Show(Action onRewarded, Action onFailedOrClosed)
        {
            if (!IsAvailable)
            {
                onFailedOrClosed?.Invoke();
                return;
            }
            if (_presenter == null)
            {
                onRewarded?.Invoke();   // no UI (tests): counts as watched
                return;
            }
            _presenter.Present(onRewarded, onFailedOrClosed);
        }
    }

    /// <summary>The UI of the test ad (Core <c>TestAdPanel</c>).</summary>
    public interface ITestAdPresenter
    {
        bool Showing { get; }
        void Present(Action onWatched, Action onClosed);
    }

    /// <summary>The only place that decides which rewarded ads the app uses (AdMob will be added here, nothing else changes).</summary>
    public static class RewardedAds
    {
        /// <summary>Test ads in the Editor and in development (test) builds, when the config allows them; otherwise none (yet).</summary>
        public static IRewardedAdService Create(AdConfig config, ITestAdPresenter testPresenter)
        {
            bool test = Application.isEditor || Debug.isDebugBuild;
            if (test && (config == null || config.testAdsInTestBuilds)) return new TestRewardedAdService(testPresenter);
            return new UnavailableRewardedAdService();
        }

        /// <summary>A button that needs an ad: enabled only if the ad can be shown and the action itself makes sense.</summary>
        public static bool CanOffer(IRewardedAdService ads, bool actionPossible) => actionPossible && ads != null && ads.IsAvailable;
    }
}
