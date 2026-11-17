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

        /// <summary>"Story · Chapters" (chapter list subtitle).</summary>
        public const string ChaptersSubtitle = "chapters.subtitle";
        /// <summary>"{0}/{1}" stars of a chapter.</summary>
        public const string ChaptersStars = "chapters.stars";
        /// <summary>"Chapter {0}".</summary>
        public const string LevelsChapter = "levels.chapter";
        /// <summary>"Chapter {0} · stars {1}/{2}" (level grid subtitle).</summary>
        public const string LevelsSubtitle = "levels.subtitle";
        /// <summary>"Level {0}".</summary>
        public const string GameLevel = "game.level";
        /// <summary>"Moves {0} · min {1}".</summary>
        public const string GameMoves = "game.moves";
        public const string WinTitle = "win.title";
        /// <summary>"Moves: {0}   Minimum: {1}".</summary>
        public const string WinMoves = "win.moves";
        public const string WinRetry = "win.retry";
        public const string WinNext = "win.next";
        public const string WinLevels = "win.levels";

        /// <summary>Language display name key: "language.it", "language.en".</summary>
        public static string LanguageName(Language language) => "language." + LanguageCodes.ToCode(language);
    }
}
