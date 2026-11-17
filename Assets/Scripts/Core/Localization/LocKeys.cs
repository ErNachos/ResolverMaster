namespace ResolverMaster.Core.Localization
{
    /// <summary>
    /// Every Core text key used by code. The validator checks that each constant here exists in every language,
    /// so new UI text = new constant + one line per language file (Resources/Localization/&lt;code&gt;/core.txt).
    /// </summary>
    public static class LocKeys
    {
        public const string AppTitle = "app.title";
        /// <summary>The big Home button: "GIOCA!" / "PLAY!".</summary>
        public const string HomePlay = "home.play";
        public const string GameComingSoon = "game.status.coming_soon";

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

        // ErNa-007: rewarded ads (test version)
        public const string AdTestTitle = "ad.test.title";
        /// <summary>"Wait {0} s..."</summary>
        public const string AdTestWait = "ad.test.wait";
        public const string AdTestReady = "ad.test.ready";
        public const string AdTestWatched = "ad.test.watched";
        public const string AdTestClosed = "ad.test.closed";

        // ErNa-007: Challenge
        /// <summary>"Challenge · Level {0}".</summary>
        public const string ChallengeLevel = "challenge.level";
        /// <summary>"Moves {0} · min {1} · max {2}".</summary>
        public const string GameMovesMax = "game.moves.max";
        public const string ChallengePause = "challenge.pause";
        /// <summary>"Level {0} · {1} stars" (pause).</summary>
        public const string ChallengeSummary = "challenge.summary";
        public const string ChallengeResume = "challenge.resume";
        public const string ChallengeHome = "challenge.home";
        public const string ChallengeOver = "challenge.over";
        /// <summary>"Stars: {0}".</summary>
        public const string ChallengeStars = "challenge.stars";
        /// <summary>"Levels cleared: {0}".</summary>
        public const string ChallengeLevels = "challenge.levels";
        /// <summary>"Record: {0} stars · {1} levels".</summary>
        public const string ChallengeRecord = "challenge.record";
        public const string ChallengeNewRecord = "challenge.new_record";
        public const string ChallengeNewRun = "challenge.new_run";
        public const string ChallengeConfirmNew = "challenge.confirm_new";
        public const string CommonYes = "common.yes";
        public const string CommonNo = "common.no";
        /// <summary>"Run in progress: level {0} · {1} stars" (mode choice).</summary>
        public const string ModeChallengeRun = "mode.challenge.run";
        /// <summary>Home card: "Challenge" / "Resume".</summary>
        public const string HomeChallenge = "home.challenge";
        public const string HomeChallengeResume = "home.challenge.resume";

        // ErNa-008: lifeline, change level, daily challenge
        /// <summary>"Continue" (end of a run, rewarded ad).</summary>
        public const string ChallengeContinue = "challenge.continue";
        /// <summary>"Daily challenge" (end panel title, Home card).</summary>
        public const string DailyTitle = "daily.title";
        /// <summary>"Daily · Level {0}/{1}" (game screen title).</summary>
        public const string DailyLevel = "daily.level";
        /// <summary>Home card status: "To do".</summary>
        public const string DailyToDo = "daily.todo";
        /// <summary>Home card status: "In progress".</summary>
        public const string DailyInProgress = "daily.in_progress";
        /// <summary>Home card status: "Done · {0} stars".</summary>
        public const string DailyDone = "daily.done";
        /// <summary>End panel: "Completed!".</summary>
        public const string DailyCompleted = "daily.completed";
        /// <summary>End panel: "Not completed".</summary>
        public const string DailyFailed = "daily.failed";
        /// <summary>"Stars: {0}/{1}".</summary>
        public const string DailyStars = "daily.stars";
        /// <summary>"Streak: {0} · best {1}".</summary>
        public const string DailyStreak = "daily.streak";
        /// <summary>"Level {0}/{1} · {2} stars" (pause).</summary>
        public const string DailySummary = "daily.summary";

        /// <summary>Language display name key: "language.it", "language.en".</summary>
        public static string LanguageName(Language language) => "language." + LanguageCodes.ToCode(language);
    }
}
