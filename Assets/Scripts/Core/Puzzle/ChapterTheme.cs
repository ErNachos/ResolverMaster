using System;
using UnityEngine;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>Name and theme of a Story chapter, edited in each game's parameter file (one entry per chapter, in order).</summary>
    [Serializable]
    public sealed class ChapterTheme
    {
        /// <summary>Localization key of the chapter name (in the game's text file).</summary>
        public string nameKey;
        /// <summary>IconLibrary sprite shown on the chapter card.</summary>
        public string icon;
        /// <summary>Card accent color.</summary>
        public Color color = new Color32(0x3D, 0x6F, 0xE0, 0xFF);
        /// <summary>Id of the visual theme (terrain, background) in the Core ThemeConfig, e.g. "garden". Empty = the default theme.</summary>
        public string theme;

        public ChapterTheme() { }

        public ChapterTheme(string nameKey, string icon, Color color, string theme = null)
        {
            this.nameKey = nameKey;
            this.icon = icon;
            this.color = color;
            this.theme = theme;
        }
    }
}
