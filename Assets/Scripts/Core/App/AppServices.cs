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
        /// <summary>The panel of the test ad (null when the app uses no test ads).</summary>
        public TestAdPanel TestAds;

        public void Save()
        {
            Store?.Save(Data);
        }
    }
}
