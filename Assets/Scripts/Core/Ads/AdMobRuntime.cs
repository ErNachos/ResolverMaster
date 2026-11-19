#if RESOLVERMASTER_ADMOB
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace ResolverMaster.Core.Ads
{
    /// <summary>
    /// ErNa-013: AdMob on the phone (never in the Editor). At start-up: consent update (UMP) and form if required, then, only when
    /// the consent allows it (<c>CanRequestAds</c>), the SDK initialisation and the first rewarded ad (<see cref="RewardedAdController"/>).
    /// A consent that cannot be updated (no network, error) never blocks the game: ads stay unavailable until the consent is obtained
    /// (next start). Every SDK callback is moved to the main thread. Ids from <see cref="AdMobIds"/>, by build kind.
    /// </summary>
    public sealed class AdMobRuntime : MonoBehaviour, IConsentService
    {
        readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();
        RewardedAdController _controller;
        BuildKind _kind;
        AdConfig _config;
        bool _initStarted, _hasFocus = true;

        public event Action Changed;

        public RewardedAdController Controller => _controller;

        /// <summary>Creates the runtime (a child of <paramref name="parent"/>) and returns it; the ads start from its <c>Start</c>.</summary>
        public static AdMobRuntime Create(Transform parent, BuildKind kind, AdConfig config, IAdHost host)
        {
            var go = new GameObject("AdMob");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<AdMobRuntime>();
            rt._kind = kind;
            rt._config = config;
            var provider = new AdMobRewardedProvider(AdMobIds.RewardedUnitId(kind), rt.Post);
            rt._controller = new RewardedAdController(provider, host, config.adWatchdogSeconds, config.loadRetrySeconds);
            return rt;
        }

        /// <summary>Runs <paramref name="action"/> on the main thread (next Update).</summary>
        void Post(Action action)
        {
            if (action != null) _mainThread.Enqueue(action);
        }

        void Start()
        {
            // before anything else of the SDK; obsolete in plugin 11.x but still honoured. Every callback is also moved to the main
            // thread by Post, so the ads work either way.
#pragma warning disable CS0618
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
#pragma warning restore CS0618
            var request = new ConsentRequestParameters();
            if (_kind != BuildKind.Release && _config.consentDebugEea)
                request.ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugGeography.EEA,
                    TestDeviceHashedIds = new List<string>(_config.consentTestDeviceIds ?? new string[0]),
                };
            ConsentInformation.Update(request, error => Post(() => OnConsentUpdated(error)));
            // consent of an earlier start still valid: no need to wait for the update
            if (ConsentInformation.CanRequestAds()) InitializeAds();
        }

        void OnConsentUpdated(FormError error)
        {
            if (error != null)
            {
                Debug.LogWarning($"AdMob: consent update failed ({error.ErrorCode}): {error.Message}. Ads only with an earlier consent.");
                if (ConsentInformation.CanRequestAds()) InitializeAds();
                Changed?.Invoke();
                return;
            }
            ConsentForm.LoadAndShowConsentFormIfRequired(formError => Post(() =>
            {
                if (formError != null) Debug.LogWarning($"AdMob: consent form not shown ({formError.ErrorCode}): {formError.Message}");
                if (ConsentInformation.CanRequestAds()) InitializeAds();
                Changed?.Invoke();
            }));
        }

        void InitializeAds()
        {
            if (_initStarted) return;
            _initStarted = true;
            MobileAds.Initialize(_ => Post(() => _controller.Start()));
        }

        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public void ShowPrivacyOptions(Action done)
        {
            ConsentForm.ShowPrivacyOptionsForm(error => Post(() =>
            {
                if (error != null) Debug.LogWarning($"AdMob: privacy options form ({error.ErrorCode}): {error.Message}");
                if (ConsentInformation.CanRequestAds()) InitializeAds();
                Changed?.Invoke();
                done?.Invoke();
            }));
        }

        void OnApplicationFocus(bool focus) => _hasFocus = focus;

        void OnApplicationPause(bool paused)
        {
            if (paused) _hasFocus = false;
        }

        void Update()
        {
            while (_mainThread.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
            _controller.Tick(Time.unscaledDeltaTime, _hasFocus);
        }
    }

    /// <summary>ErNa-013: the AdMob rewarded ad behind <see cref="IRewardedAdProvider"/> (one unit id, chosen by build kind).</summary>
    sealed class AdMobRewardedProvider : IRewardedAdProvider
    {
        readonly string _unitId;
        readonly Action<Action> _post;
        RewardedAd _ad;

        public AdMobRewardedProvider(string unitId, Action<Action> post)
        {
            _unitId = unitId;
            _post = post;
        }

        public bool IsReady => _ad != null && _ad.CanShowAd();

        public bool IsLoading { get; private set; }

        public void Load(Action<bool> done)
        {
            IsLoading = true;
            RewardedAd.Load(_unitId, new AdRequest(), (ad, error) => _post(() =>
            {
                IsLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("AdMob: rewarded ad not loaded: " + (error != null ? error.GetMessage() : "no ad"));
                    done?.Invoke(false);
                    return;
                }
                _ad?.Destroy();
                _ad = ad;
                done?.Invoke(true);
            }));
        }

        public void Show(Action earned, Action closed, Action failed)
        {
            var ad = _ad;
            _ad = null;   // one show per ad: the next one is loaded after it
            if (ad == null || !ad.CanShowAd())
            {
                ad?.Destroy();
                failed?.Invoke();
                return;
            }
            ad.OnAdFullScreenContentClosed += () => _post(() =>
            {
                closed?.Invoke();
                ad.Destroy();
            });
            ad.OnAdFullScreenContentFailed += error => _post(() =>
            {
                Debug.LogWarning("AdMob: rewarded ad not shown: " + error?.GetMessage());
                failed?.Invoke();
                ad.Destroy();
            });
            ad.Show(_ => _post(earned));
        }
    }
}
#endif
