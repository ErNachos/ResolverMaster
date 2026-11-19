using UnityEngine;

namespace ResolverMaster.Core.Ads
{
    /// <summary>
    /// The single parameter file of the ads (Core): Assets/Resources/ResolverMaster/AdConfig.asset (Setup > Ensure Config Assets).
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Ad Config", fileName = "AdConfig")]
    public sealed class AdConfig : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/AdConfig";

        /// <summary>Seconds before the buttons of the test ad (Editor only, ErNa-013) can be pressed.</summary>
        public float testAdSeconds = 1.5f;
        /// <summary>ErNa-010, Editor only: closing the test ad gives no reward (to try the "ad not watched" case).</summary>
        public bool editorAdNotWatched;

        /// <summary>AdMob (ErNa-013, ids in AdMobIds): an ad whose close never arrives ends after the app has had the focus again for this many seconds.</summary>
        [Header("AdMob (ErNa-013): ids in AdMobIds")]
        public float adWatchdogSeconds = 8f;
        /// <summary>Seconds before a failed load is tried again.</summary>
        public float loadRetrySeconds = 30f;
        /// <summary>Seconds the "Ad not available" message stays before fading.</summary>
        public float notAvailableSeconds = 2.5f;
        /// <summary>Test APK and development builds only (never the release AAB): the consent form behaves as in the EEA.</summary>
        public bool consentDebugEea = true;
        /// <summary>
        /// Hashed ids of the test phones (logcat of the first start: "Use new ConsentDebugSettings.Builder().addTestDeviceHashedId(...)"):
        /// Google applies the debug geography only to these phones. Never used by the release AAB.
        /// </summary>
        public string[] consentTestDeviceIds = new string[0];

        public static AdConfig LoadOrDefault()
        {
            var cfg = Resources.Load<AdConfig>(ResourcesPath);
            if (cfg != null) return cfg;
            Debug.LogWarning("AdConfig missing in Resources/" + ResourcesPath + ": default values used.");
            return CreateInstance<AdConfig>();
        }
    }
}
