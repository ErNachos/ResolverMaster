using System;
using UnityEngine;

namespace ResolverMaster.Core.Ads
{
    /// <summary>
    /// A rewarded ad (ErNa-007): the player watches it and gets something (e.g. a hint). The game never knows which network is
    /// behind it: the test panel in the Editor (<see cref="TestRewardedAdService"/>), AdMob on the phone (ErNa-013,
    /// <see cref="RewardedAdController"/>), chosen in one place (<see cref="RewardedAds.Create"/>).
    /// </summary>
    public interface IRewardedAdService
    {
        /// <summary>An ad can be shown now. Buttons that need an ad are disabled while this is false.</summary>
        bool IsAvailable { get; }

        /// <summary>Shows the ad. Exactly one of the callbacks is called: watched to the end, or failed / closed early.</summary>
        void Show(Action onRewarded, Action onFailedOrClosed);

        /// <summary>ErNa-013: an ad is on screen (the game ignores Back and taps until it ends).</summary>
        bool IsShowing { get; }
    }

    /// <summary>No ads at all (a phone build without the AdMob plugin): every ad button stays disabled.</summary>
    public sealed class UnavailableRewardedAdService : IRewardedAdService
    {
        public bool IsAvailable => false;

        public bool IsShowing => false;

        public void Show(Action onRewarded, Action onFailedOrClosed) => onFailedOrClosed?.Invoke();
    }

    /// <summary>
    /// The test ad: a "Test ad" panel with a short wait and one "Close" button. ErNa-010: closing it always gives the reward (the
    /// player sees the hint, Continue or Change level at once); only in the Editor an option of AdConfig simulates an ad not watched
    /// (<see cref="RewardOnClose"/> false). ErNa-013: only in the Editor (and the automatic tests); the phone always uses AdMob. The panel is UI, given by the app
    /// (<see cref="ITestAdPresenter"/>), so this class stays pure and testable.
    /// </summary>
    public sealed class TestRewardedAdService : IRewardedAdService
    {
        readonly ITestAdPresenter _presenter;

        /// <summary>Tests and previews can switch the test ad off.</summary>
        public bool Enabled = true;

        /// <summary>Closing the panel gives the reward (always, except the "not watched" option of the Editor).</summary>
        public bool RewardOnClose = true;

        public TestRewardedAdService(ITestAdPresenter presenter) => _presenter = presenter;

        public bool IsAvailable => Enabled && (_presenter == null || !_presenter.Showing);

        public bool IsShowing => _presenter != null && _presenter.Showing;

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
            bool reward = RewardOnClose;
            _presenter.Present(() => (reward ? onRewarded : onFailedOrClosed)?.Invoke());
        }
    }

    /// <summary>The UI of the test ad (Core <c>TestAdPanel</c>).</summary>
    public interface ITestAdPresenter
    {
        bool Showing { get; }
        /// <summary>Shows the panel; <paramref name="onClosed"/> runs when the player closes it (after the short wait).</summary>
        void Present(Action onClosed);
    }

    /// <summary>ErNa-010: what kind of app is running (decides the ads, never a manual switch).</summary>
    public enum BuildKind
    {
        Editor,
        /// <summary>The APK of Resolver Master > Build Release APK (test): built with <see cref="RewardedAds.TestBuildDefine"/>.</summary>
        TestApk,
        /// <summary>A development build (Unity's "Development Build").</summary>
        Development,
        /// <summary>The AAB for Google Play (built with <see cref="RewardedAds.ReleaseBuildDefine"/>): the only kind with the real AdMob ids.</summary>
        Release,
    }

    /// <summary>The only place that decides which rewarded ads the app uses.</summary>
    public static class RewardedAds
    {
        /// <summary>ErNa-010: scripting symbol added by ReleaseBuild only to the test APK (never to the AAB).</summary>
        public const string TestBuildDefine = "RESOLVERMASTER_TEST_BUILD";

        /// <summary>
        /// ErNa-013: scripting symbol added by ReleaseBuild only to the release AAB. Without it a phone build is never
        /// <see cref="BuildKind.Release"/> (a build made by hand gets the test ids, never the real ones).
        /// </summary>
        public const string ReleaseBuildDefine = "RESOLVERMASTER_RELEASE_BUILD";

        /// <summary>The kind of the running app.</summary>
        public static BuildKind CurrentKind
        {
            get
            {
                if (Application.isEditor) return BuildKind.Editor;
#if RESOLVERMASTER_TEST_BUILD
                return BuildKind.TestApk;
#elif RESOLVERMASTER_RELEASE_BUILD
                return Debug.isDebugBuild ? BuildKind.Development : BuildKind.Release;
#else
                return BuildKind.Development;
#endif
            }
        }

        /// <summary>ErNa-013: the test panel only in the Editor (and the tests); every phone build uses AdMob.</summary>
        public static bool UsesTestPanel(BuildKind kind) => kind == BuildKind.Editor;

        /// <summary>Closing the test ad gives the reward, except in the Editor with the "not watched" option.</summary>
        public static bool RewardOnClose(BuildKind kind, AdConfig config) => !(kind == BuildKind.Editor && config != null && config.editorAdNotWatched);

        /// <summary>
        /// The ads of this app: the test panel in the Editor, AdMob on the phone (<paramref name="deviceAds"/>, given by the app:
        /// null when the plugin is missing = no ads).
        /// </summary>
        public static IRewardedAdService Create(BuildKind kind, AdConfig config, ITestAdPresenter testPresenter,
            Func<BuildKind, IRewardedAdService> deviceAds = null)
        {
            if (UsesTestPanel(kind)) return new TestRewardedAdService(testPresenter) { RewardOnClose = RewardOnClose(kind, config) };
            return deviceAds?.Invoke(kind) ?? new UnavailableRewardedAdService();
        }

        /// <summary>A button that needs an ad: enabled only if the ad can be shown and the action itself makes sense.</summary>
        public static bool CanOffer(IRewardedAdService ads, bool actionPossible) => actionPossible && ads != null && ads.IsAvailable;
    }
}
