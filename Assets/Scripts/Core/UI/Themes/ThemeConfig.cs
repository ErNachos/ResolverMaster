using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;
using UnityEngine;

namespace ResolverMaster.Core.UI.Themes
{
    /// <summary>
    /// The single parameter file of the chapter themes (Core, shared by every game): Garden, Farm, City; ErNa-006: Factory, Building site,
    /// Harbour, Mountain, Laboratory, Castle; TJ-001: Parking, Market, Airport, Crossroads, Rush hour.
    /// Asset: Assets/Resources/ResolverMaster/ThemeConfig.asset (created by Resolver Master > Setup > Ensure Config Assets).
    /// Each game's chapters name one of these ids (e.g. PlumberConfig.chapters[i].theme).
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Theme Config", fileName = "ThemeConfig")]
    public sealed class ThemeConfig : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/ThemeConfig";
        public const string Garden = "garden", Farm = "farm", City = "city";
        /// <summary>ErNa-006 (Plumber chapters 4-9).</summary>
        public const string Factory = "factory", Site = "site", Harbor = "harbor", Mountain = "mountain", Lab = "lab", Castle = "castle";
        /// <summary>TJ-001 (Traffic jam chapters; city and site are shared).</summary>
        public const string Parking = "parking", Market = "market", Airport = "airport", Crossroads = "crossroads", RushHour = "rushhour";

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
            // ErNa-006: never close to the water colors (blue, orange, violet, yellow): checked by the validator
            new VisualTheme
            {
                id = Factory, nameKey = "theme.factory.name", accent = C(0xB0705E), seed = 404,
                terrain = TerrainPattern.Pavement,
                ground = C(0x8F5446), groundDetail = C(0x7A463A),
                slab = C(0xD5D2CE), slabDetail = C(0xBAB6B0), slabEdge = C(0x9A958F),
                backdrop = BackdropPattern.Skyline,
                skyTop = C(0xCCCDD0), skyBottom = C(0xE9E8E6), landA = C(0x9A6A5C), landB = C(0x6F6B69), landDetail = C(0xE6DCC0),
            },
            new VisualTheme
            {
                id = Site, nameKey = "theme.site.name", accent = C(0xB89466), seed = 505,
                terrain = TerrainPattern.Dirt,
                ground = C(0x8A6E50), groundDetail = C(0xB59C74),
                slab = C(0xCDBB98), slabDetail = C(0xB8A37C), slabEdge = C(0x927B58),
                backdrop = BackdropPattern.Fields,
                skyTop = C(0xEDE4D3), skyBottom = C(0xF8F3EA), landA = C(0xC2AA80), landB = C(0x957A57), landDetail = C(0x6B553A),
            },
            new VisualTheme
            {
                id = Harbor, nameKey = "theme.harbor.name", accent = C(0x5E9A90), seed = 606,
                terrain = TerrainPattern.Pavement,
                ground = C(0x8D8A83), groundDetail = C(0x77746E),
                slab = C(0xC7A47E), slabDetail = C(0xA98761), slabEdge = C(0x876847),
                backdrop = BackdropPattern.Harbor,
                skyTop = C(0xD3DCDA), skyBottom = C(0xEEF1EE), landA = C(0x4E7D74), landB = C(0x8B8174), landDetail = C(0x4A4541),
            },
            new VisualTheme
            {
                id = Mountain, nameKey = "theme.mountain.name", accent = C(0x7FA37A), seed = 707,
                terrain = TerrainPattern.Grass,
                ground = C(0x5C7D49), groundDetail = C(0x46633A),
                slab = C(0xD8D5CD), slabDetail = C(0xBCB7AC), slabEdge = C(0x9C978B),
                backdrop = BackdropPattern.Mountains,
                skyTop = C(0xD6E2E4), skyBottom = C(0xF0F4F1), landA = C(0x9A9F99), landB = C(0x5C7D49), landDetail = C(0xF7F7F3),
            },
            new VisualTheme
            {
                id = Lab, nameKey = "theme.lab.name", accent = C(0xA3B0BD), seed = 808,
                terrain = TerrainPattern.Pavement,
                ground = C(0xC5C9CE), groundDetail = C(0xB1B6BC),
                slab = C(0xEEF0F2), slabDetail = C(0xD6DADF), slabEdge = C(0xAEB4BB),
                backdrop = BackdropPattern.Skyline,
                skyTop = C(0xDDE1E5), skyBottom = C(0xF2F3F5), landA = C(0xB9BFC6), landB = C(0x9CA3AB), landDetail = C(0xEFE8CF),
            },
            new VisualTheme
            {
                id = Castle, nameKey = "theme.castle.name", accent = C(0xA593C0), seed = 909,
                terrain = TerrainPattern.Grass,
                ground = C(0x4D6A3B), groundDetail = C(0x3B5430),
                slab = C(0xD3C8B9), slabDetail = C(0xBAAE9D), slabEdge = C(0x988B79),
                backdrop = BackdropPattern.Castle,
                skyTop = C(0xDAD5E3), skyBottom = C(0xF1EFF3), landA = C(0xA69D90), landB = C(0x4D6A3B), landDetail = C(0x5E554B),
            },
            // TJ-001: never close to the water colors nor to the red and blue of the Traffic jam target cars (checked by the validator)
            new VisualTheme
            {
                id = Parking, nameKey = "theme.parking.name", accent = C(0x8FB39A), seed = 1101,
                terrain = TerrainPattern.Pavement,
                ground = C(0x747C76), groundDetail = C(0x636B65),
                slab = C(0xDCDDD8), slabDetail = C(0xC4C6C0), slabEdge = C(0xA3A69F),
                backdrop = BackdropPattern.Meadow,
                skyTop = C(0xDCEBE6), skyBottom = C(0xF3F7F2), landA = C(0xA9C9A0), landB = C(0x7FA77A), landDetail = C(0xF3EBD2),
            },
            new VisualTheme
            {
                id = Market, nameKey = "theme.market.name", accent = C(0xC79A6B), seed = 1202,
                terrain = TerrainPattern.Dirt,
                ground = C(0x9B7B5C), groundDetail = C(0xC9AE84),
                slab = C(0xE3D3B8), slabDetail = C(0xCDBB9B), slabEdge = C(0xA8916F),
                backdrop = BackdropPattern.Fields,
                skyTop = C(0xF5E9D8), skyBottom = C(0xFBF6EE), landA = C(0xD9B98A), landB = C(0xA37E58), landDetail = C(0x6F5A44),
            },
            new VisualTheme
            {
                id = Airport, nameKey = "theme.airport.name", accent = C(0x9DB0A3), seed = 1303,
                terrain = TerrainPattern.Pavement,
                ground = C(0x8D9396), groundDetail = C(0x7B8184),
                slab = C(0xECEDEE), slabDetail = C(0xD5D8DB), slabEdge = C(0xAEB3B8),
                backdrop = BackdropPattern.Meadow,
                skyTop = C(0xD9E6EC), skyBottom = C(0xF2F6F4), landA = C(0xA8C98A), landB = C(0x84AD6B), landDetail = C(0xF4F1E6),
            },
            new VisualTheme
            {
                id = Crossroads, nameKey = "theme.crossroads.name", accent = C(0xA89C8A), seed = 1404,
                terrain = TerrainPattern.Pavement,
                ground = C(0x5F6366), groundDetail = C(0x6F7377),
                slab = C(0xCFCFCB), slabDetail = C(0xB8B8B3), slabEdge = C(0x979792),
                backdrop = BackdropPattern.Skyline,
                skyTop = C(0xE3DCCF), skyBottom = C(0xF5F1EA), landA = C(0xA79F93), landB = C(0x857D72), landDetail = C(0xF1E3B9),
            },
            new VisualTheme
            {
                id = RushHour, nameKey = "theme.rushhour.name", accent = C(0x9C93AE), seed = 1505,
                terrain = TerrainPattern.Pavement,
                ground = C(0x55585E), groundDetail = C(0x6A6E74),
                slab = C(0xD3D0CC), slabDetail = C(0xBCB8B3), slabEdge = C(0x9A958F),
                backdrop = BackdropPattern.Skyline,
                skyTop = C(0xD9CFE0), skyBottom = C(0xF2E9E4), landA = C(0x8E8A9A), landB = C(0x6E6A7A), landDetail = C(0xF6DFA6),
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
