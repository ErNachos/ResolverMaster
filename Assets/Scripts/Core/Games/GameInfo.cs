using System.Collections.Generic;
using UnityEngine;

namespace ResolverMaster.Core.Games
{
    public enum GameAvailability
    {
        /// <summary>Shown in Home with the "Coming soon" lock; PLAY disabled.</summary>
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
        /// <summary>
        /// Illustration of the Home card: sprite name in the card library (Assets/Art/Cards/&lt;name&gt;.png). Missing image = a placeholder
        /// drawn by code (the game icon), and a validator warning.
        /// </summary>
        public string CardImage;
        /// <summary>Color of the rounded frame of the Home card (not blue or orange: those are the water colors). Transparent = not set (validator error).</summary>
        public Color CardColor = new Color(0, 0, 0, 0);
        /// <summary>
        /// ErNa-010: the soft background of Home while this card is in the centre (and of its chapter list). Transparent = the
        /// UiKitStyle background. Never close to the water colors nor to the target cars (validators).
        /// </summary>
        public Color BackgroundColor = new Color(0, 0, 0, 0);
    }

    /// <summary>Which Home card to show at start-up (pure logic, tested in EditMode).</summary>
    public static class HomeSelection
    {
        /// <summary>Index of the saved game in <paramref name="games"/>; the first card when nothing is saved or the game no longer exists.</summary>
        public static int IndexOf(IReadOnlyList<GameInfo> games, string savedId)
        {
            if (games == null || string.IsNullOrEmpty(savedId)) return 0;
            for (int i = 0; i < games.Count; i++)
                if (games[i].Id == savedId) return i;
            return 0;
        }
    }
}
