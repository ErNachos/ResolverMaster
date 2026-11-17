namespace ResolverMaster.Core.Games
{
    public enum GameAvailability
    {
        /// <summary>Shown in Home with the "Coming soon" badge.</summary>
        ComingSoon,
        Available,
    }

    /// <summary>What Home needs to know about a minigame. Built by each minigame's <see cref="MinigameModule"/>.</summary>
    public sealed class GameInfo
    {
        /// <summary>Stable id (lowercase, no spaces): used in save blocks and file names. Never change it after release.</summary>
        public string Id;
        /// <summary>Localization key of the name, e.g. "game.plumber.name".</summary>
        public string NameKey;
        /// <summary>Sprite name in the IconLibrary (Assets/Art/Icons/&lt;name&gt;.png).</summary>
        public string IconName;
        public GameAvailability Availability;
        /// <summary>Position in Home (ascending).</summary>
        public int Order;
    }
}
