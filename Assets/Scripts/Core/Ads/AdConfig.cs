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

        /// <summary>The test ad is used in the Editor, in the test APK and in development builds (never in the release AAB).</summary>
        public bool testAdsInTestBuilds = true;
        /// <summary>Seconds before the buttons of the test ad can be pressed.</summary>
        public float testAdSeconds = 1.5f;
        /// <summary>ErNa-010, Editor only: closing the test ad gives no reward (to try the "ad not watched" case).</summary>
        public bool editorAdNotWatched;

        public static AdConfig LoadOrDefault()
        {
            var cfg = Resources.Load<AdConfig>(ResourcesPath);
            if (cfg != null) return cfg;
            Debug.LogWarning("AdConfig missing in Resources/" + ResourcesPath + ": default values used.");
            return CreateInstance<AdConfig>();
        }
    }
}
