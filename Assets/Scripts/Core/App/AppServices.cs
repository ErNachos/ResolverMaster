using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.Save;
using ResolverMaster.Core.UI;

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
        public UiFactory Ui;
        public ScreenManager Screens;

        public void Save()
        {
            Store?.Save(Data);
        }
    }
}
