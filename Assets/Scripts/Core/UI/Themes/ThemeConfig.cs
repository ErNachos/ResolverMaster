using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;
using UnityEngine;

namespace ResolverMaster.Core.UI.Themes
{
    /// <summary>
    /// The single parameter file of the chapter themes (Core, shared by every game): Garden, Farm, City.
    /// Asset: Assets/Resources/ResolverMaster/ThemeConfig.asset (created by Resolver Master > Setup > Ensure Config Assets).
    /// Each game's chapters name one of these ids (e.g. PlumberConfig.chapters[i].theme).
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Theme Config", fileName = "ThemeConfig")]
    public sealed class ThemeConfig : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/ThemeConfig";
        public const string Garden = "garden", Farm = "farm", City = "city";

        /// <summary>Used by a chapter with no theme or an unknown one.</summary>
        public string defaultTheme = Garden;

        public List<VisualTheme> themes = new List<VisualTheme>
        {
            new VisualTheme
            {
                id = Garden, nameKey = "theme.garden.name", accent = C(0x7CC453), seed = 101,
                terrain = TerrainPattern.Grass,
                ground = C(0x6FB04A), groundDetail = C(0x4E8F33),
                slab = C(0xE9E4D6), slabDetail = C(0xC9C2B0), slabEdge = C(0xB3AA95),
                backdrop = BackdropPattern.Meadow,
                skyTop = C(0xD6F0FA), skyBottom = C(0xF4FBF0), landA = C(0x9BD36E), landB = C(0x6EB548), landDetail = C(0xFFF7D6),
            },
            new VisualTheme
            {
                id = Farm, nameKey = "theme.farm.name", accent = C(0xE8C35A), seed = 202,
                terrain = TerrainPattern.Dirt,
                ground = C(0xA8794E), groundDetail = C(0xE3C770),
                slab = C(0xD9B98C), slabDetail = C(0xEBD58E), slabEdge = C(0xA88559),
                backdrop = BackdropPattern.Fields,
                skyTop = C(0xFBEFD2), skyBottom = C(0xFFF8E8), landA = C(0xF0CF6A), landB = C(0xB98A55), landDetail = C(0x8C6239),
            },
            new VisualTheme
            {
                id = City, nameKey = "theme.city.name", accent = C(0xB9C2CC), seed = 303,
                terrain = TerrainPattern.Pavement,
                ground = C(0x9EA3A8), groundDetail = C(0x8A8F94),
                slab = C(0xE4E2DD), slabDetail = C(0xC8C6C0), slabEdge = C(0xA9A7A1),
                backdrop = BackdropPattern.Skyline,
                skyTop = C(0xC9D6E2), skyBottom = C(0xE8EDF2), landA = C(0xA9B6C4), landB = C(0x8C99A8), landDetail = C(0xF3E9C6),
            },
        };

        ThemeCatalog _catalog;

        /// <summary>Theme lookup (rebuilt after edits in the Editor via <see cref="Invalidate"/>).</summary>
        public ThemeCatalog Catalog => _catalog ??= new ThemeCatalog(themes, defaultTheme);

        public VisualTheme ForChapter(StoryChapter chapter) => Catalog.ForChapter(chapter);

        public void Invalidate() => _catalog = null;

        void OnValidate() => _catalog = null;

        public static ThemeConfig LoadOrDefault()
        {
            var cfg = Resources.Load<ThemeConfig>(ResourcesPath);
            if (cfg != null) return cfg;
            Debug.LogWarning("ThemeConfig missing in Resources/" + ResourcesPath + ": default values used.");
            return CreateInstance<ThemeConfig>();
        }

        static Color C(int rgb) => UiKitStyle.Hex(rgb);
    }
}
