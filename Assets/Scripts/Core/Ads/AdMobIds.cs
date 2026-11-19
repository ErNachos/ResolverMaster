using System.Text.RegularExpressions;

namespace ResolverMaster.Core.Ads
{
    /// <summary>
    /// ErNa-013: the only place of the AdMob ids. The real ids go ONLY into the release AAB (<see cref="BuildKind.Release"/>);
    /// every other build (test APK, development build) uses Google's test ids, never the real ones (a real ad shown in a test build
    /// can get the account suspended). The Editor uses no AdMob at all (the test panel).
    /// </summary>
    public static class AdMobIds
    {
        /// <summary>The real AdMob app id (Android).</summary>
        public const string RealAppId = "ca-app-pub-6347940602066252~5954105532";
        /// <summary>The real rewarded ad unit (one for every use: Continue, Change level, light bulb).</summary>
        public const string RealRewardedUnitId = "ca-app-pub-6347940602066252/9823574496";

        /// <summary>Google's sample app id for tests (Android).</summary>
        public const string TestAppId = "ca-app-pub-3940256099942544~3347511713";
        /// <summary>Google's sample rewarded unit for tests (Android).</summary>
        public const string TestRewardedUnitId = "ca-app-pub-3940256099942544/5224354917";

        static readonly Regex AppIdFormat = new Regex(@"^ca-app-pub-\d{16}~\d{10}$");
        static readonly Regex UnitIdFormat = new Regex(@"^ca-app-pub-\d{16}/\d{10}$");

        public static bool IsValidAppId(string id) => id != null && AppIdFormat.IsMatch(id);
        public static bool IsValidUnitId(string id) => id != null && UnitIdFormat.IsMatch(id);

        /// <summary>The app id written in the manifest of a build of this kind.</summary>
        public static string AppId(BuildKind kind) => kind == BuildKind.Release ? RealAppId : TestAppId;

        /// <summary>The rewarded unit requested by a build of this kind.</summary>
        public static string RewardedUnitId(BuildKind kind) => kind == BuildKind.Release ? RealRewardedUnitId : TestRewardedUnitId;

        /// <summary>A test id of Google (publisher 3940256099942544).</summary>
        public static bool IsTestId(string id) => id != null && id.StartsWith("ca-app-pub-3940256099942544");
    }
}
