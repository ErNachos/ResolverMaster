using ResolverMaster.Core.Ads;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.Save;
using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Themes;

namespace ResolverMaster.Core.App
{
    /// <summary>The app-wide services handed to every screen (no static singletons, so previews can build a private copy).</summary>
    public sealed class AppServices
    {
        public Localizer Loc;
        public SaveData Data;
        /// <summary>Null in previews: nothing is written.</summary>
        public SaveStore Store;
        public GameRegistry Games;
        public UiKitStyle Style;
        public IconLibrary Icons;
        /// <summary>Illustrations of the Home cards (Assets/Art/Cards); may miss images: Home draws a placeholder.</summary>
        public IconLibrary Cards;
        public PuzzleConfig Puzzle;
        /// <summary>Chapter themes (terrain, backdrop), shared by every game.</summary>
        public ThemeConfig Themes;
        public UiFactory Ui;
        public ScreenManager Screens;
        /// <summary>Rewarded ads (ErNa-007): chosen by <see cref="RewardedAds.Create"/> when the UI is built, unless already set (tests, previews).</summary>
        public IRewardedAdService Ads;
        /// <summary>The panel of the test ad (Editor only, ErNa-013).</summary>
        public TestAdPanel TestAds;
        /// <summary>ErNa-013: input shield, sound pause and "not available" message around a real ad.</summary>
        public AdOverlay AdOverlay;
        /// <summary>ErNa-013: the privacy consent (UMP); null in the Editor and in the previews ("Privacy options" hidden).</summary>
        public IConsentService Consent;

        /// <summary>An ad (test panel or real) is on screen: Back and taps are for the ad.</summary>
        public bool AdShowing => (Ads != null && Ads.IsShowing) || (TestAds != null && TestAds.Showing);
        /// <summary>ErNa-008: the phone's local date and time (the daily challenge); previews set a fixed date.</summary>
        public System.Func<System.DateTime> Clock = () => System.DateTime.Now;

        /// <summary>Today as YYYYMMDD (local date).</summary>
        public int Today => ResolverMaster.Core.Challenge.DailyChallenge.DayKey(Clock());

        public void Save()
        {
            Store?.Save(Data);
        }
    }
}
