namespace ResolverMaster.Core.Localization
{
    /// <summary>
    /// Every Core text key used by code. The validator checks that each constant here exists in every language,
    /// so new UI text = new constant + one line per language file (Resources/Localization/&lt;code&gt;/core.txt).
    /// </summary>
    public static class LocKeys
    {
        public const string AppTitle = "app.title";
        public const string HomeChoose = "home.choose";
        public const string GameComingSoon = "game.status.coming_soon";
        public const string GameAvailable = "game.status.available";

        public const string ModeTitle = "mode.title";
        public const string ModeStory = "mode.story";
        public const string ModeStoryDesc = "mode.story.desc";
        public const string ModeChallenge = "mode.challenge";
        public const string ModeChallengeDesc = "mode.challenge.desc";
        public const string ModePlaceholder = "mode.placeholder";

        public const string SettingsTitle = "settings.title";
        public const string SettingsLanguage = "settings.language";
        public const string SettingsSound = "settings.sound";
        public const string SettingsVibration = "settings.vibration";
        public const string CommonOn = "common.on";
        public const string CommonOff = "common.off";

        /// <summary>Language display name key: "language.it", "language.en".</summary>
        public static string LanguageName(Language language) => "language." + LanguageCodes.ToCode(language);
    }
}
