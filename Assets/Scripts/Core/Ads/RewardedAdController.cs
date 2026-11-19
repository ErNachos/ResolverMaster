using System;

namespace ResolverMaster.Core.Ads
{
    /// <summary>
    /// ErNa-013: one network's rewarded ad (AdMob on the phone, a fake one in the tests). Every callback arrives on the main thread.
    /// </summary>
    public interface IRewardedAdProvider
    {
        /// <summary>An ad is loaded and can be shown now.</summary>
        bool IsReady { get; }
        bool IsLoading { get; }

        /// <summary>Loads the next ad; <paramref name="done"/> says whether it worked.</summary>
        void Load(Action<bool> done);

        /// <summary>
        /// Shows the loaded ad. <paramref name="earned"/>: the network says the user earned the reward (usually before the close).
        /// Then exactly one of <paramref name="closed"/> (the user closed the ad) or <paramref name="failed"/> (it could not be shown).
        /// </summary>
        void Show(Action earned, Action closed, Action failed);
    }

    /// <summary>ErNa-013: what the app does around an ad (Core <c>AdOverlay</c>): block the game's input, pause the sound, show a message.</summary>
    public interface IAdHost
    {
        /// <summary>The ad is about to cover the app: block the input, pause music and effects.</summary>
        void AdStarted();
        /// <summary>The ad is over (closed or failed): input and sound back.</summary>
        void AdEnded();
        /// <summary>"Ad not available. Try again shortly." in the current language.</summary>
        void ShowNotAvailable();
    }

    /// <summary>
    /// ErNa-013: the rules of the real rewarded ads, without any Unity or AdMob type (tested with a fake provider):
    /// - the first ad is loaded once the SDK can request ads (<see cref="Start"/>: consent given and SDK initialised), the next one
    ///   right after every ad (closed or failed); a failed load is retried after a while;
    /// - the reward is given ONLY when the network says it was earned, never for a mere close; closed early, failed or not ready =
    ///   no reward ("not ready" and "failed" also show the "not available" message and start a new load);
    /// - the result is decided one tick after the close (an "earned" that arrives right after the close still counts), and a watchdog
    ///   ends an ad whose close never arrives (the app has the focus again for a while) so the game is never left blocked.
    /// The buttons stay enabled while no ad is on screen: a tap without a ready ad shows the message (the player always gets an answer).
    /// </summary>
    public sealed class RewardedAdController : IRewardedAdService
    {
        readonly IRewardedAdProvider _provider;
        readonly IAdHost _host;
        readonly float _watchdogSeconds, _retrySeconds;

        bool _started, _showing, _earned, _ended, _failed;
        int _endTicks;
        float _focusTime, _retryIn = -1f;
        Action _onRewarded, _onFailedOrClosed;

        public RewardedAdController(IRewardedAdProvider provider, IAdHost host, float watchdogSeconds = 8f, float retrySeconds = 30f)
        {
            _provider = provider;
            _host = host;
            _watchdogSeconds = Math.Max(1f, watchdogSeconds);
            _retrySeconds = Math.Max(1f, retrySeconds);
        }

        /// <summary>The SDK can request ads (consent and initialisation done): ads are loaded from now on.</summary>
        public bool Started => _started;

        public bool IsAvailable => !_showing;

        public bool IsShowing => _showing;

        /// <summary>Called once the SDK is initialised and the consent allows ads: preloads the first ad.</summary>
        public void Start()
        {
            _started = true;
            LoadIfNeeded();
        }

        public void Show(Action onRewarded, Action onFailedOrClosed)
        {
            if (_showing)
            {
                onFailedOrClosed?.Invoke();
                return;
            }
            if (!_started || !_provider.IsReady)
            {
                _host?.ShowNotAvailable();
                LoadIfNeeded();
                onFailedOrClosed?.Invoke();
                return;
            }
            _showing = true;
            _earned = _ended = _failed = false;
            _endTicks = 0;
            _focusTime = 0f;
            _onRewarded = onRewarded;
            _onFailedOrClosed = onFailedOrClosed;
            _host?.AdStarted();
            _provider.Show(OnEarned, OnClosed, OnFailed);
        }

        void OnEarned()
        {
            if (_showing) _earned = true;
        }

        void OnClosed()
        {
            if (_showing) _ended = true;
        }

        void OnFailed()
        {
            if (!_showing) return;
            _ended = true;
            _failed = true;
        }

        /// <summary>Every frame (unscaled time). <paramref name="hasFocus"/>: the app is in front (the ad is a separate screen).</summary>
        public void Tick(float dt, bool hasFocus)
        {
            if (_retryIn >= 0f)
            {
                _retryIn -= dt;
                if (_retryIn < 0f) LoadIfNeeded();
            }
            if (!_showing) return;
            if (_ended)
            {
                if (++_endTicks > 1) Finish();   // one tick of grace for an "earned" after the close
                return;
            }
            _focusTime = hasFocus ? _focusTime + dt : 0f;
            if (_focusTime >= _watchdogSeconds) Finish();   // no close: the ad is gone, never leave the game blocked
        }

        void Finish()
        {
            bool rewarded = _earned && !_failed;
            var onRewarded = _onRewarded;
            var onFailedOrClosed = _onFailedOrClosed;
            _onRewarded = _onFailedOrClosed = null;
            _showing = false;
            _host?.AdEnded();
            if (_failed) _host?.ShowNotAvailable();
            LoadIfNeeded();   // the next ad, right away
            (rewarded ? onRewarded : onFailedOrClosed)?.Invoke();
        }

        void LoadIfNeeded()
        {
            if (!_started || _showing || _provider.IsReady || _provider.IsLoading) return;
            _retryIn = -1f;
            _provider.Load(ok =>
            {
                if (!ok) _retryIn = _retrySeconds;
            });
        }
    }
}
