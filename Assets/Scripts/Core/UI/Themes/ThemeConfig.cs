using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;
using UnityEngine;

namespace ResolverMaster.Core.UI.Themes
{
    /// <summary>
    /// The single parameter file of the chapter themes (Core, shared by every game): Garden, Farm, City; ErNa-006: Factory, Building site,
    /// Harbour, Mountain, Laboratory, Castle; TJ-001: Parking, Market, Airport, Crossroads, Rush hour; ErNa-009: Spa, Station, Mine, Palace; PG-001: Lake, Pack ice,
    /// Blizzard, Thaw, Glacier, Pole, Aurora.
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
        /// <summary>ErNa-009 (Plumber chapters 10-13).</summary>
        public const string Spa = "spa", Station = "station", Mine = "mine", Palace = "palace";
        /// <summary>PG-001 (Pingù chapters): frozen lake, pack ice, blizzard, thaw, glacier, pole, northern lights.</summary>
        public const string Lake = "lake", PackIce = "packice", Blizzard = "blizzard", Thaw = "thaw", Glacier = "glacier", Pole = "pole",
            NorthernLights = "aurora";

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
            // ErNa-009: never close to the water colors nor to the Traffic jam target cars (checked by the validators)
            new VisualTheme
            {
                id = Spa, nameKey = "theme.spa.name", accent = C(0x8FB8A8), seed = 1606,
                terrain = TerrainPattern.Pavement,
                ground = C(0xA59E8F), groundDetail = C(0x8E877A),
                slab = C(0xE6E1D6), slabDetail = C(0xCEC7B8), slabEdge = C(0xABA391),
                backdrop = BackdropPattern.Mountains,
                skyTop = C(0xDDE5E0), skyBottom = C(0xF3F5F1), landA = C(0x9FB8A6), landB = C(0x7A9585), landDetail = C(0xF4F1EA),
            },
            new VisualTheme
            {
                id = Station, nameKey = "theme.station.name", accent = C(0xA88F7A), seed = 1707,
                terrain = TerrainPattern.Pavement,
                ground = C(0x6E6A66), groundDetail = C(0x5C5854),
                slab = C(0xD9D4CC), slabDetail = C(0xC0BAB0), slabEdge = C(0x9E978C),
                backdrop = BackdropPattern.Skyline,
                skyTop = C(0xE0DBD3), skyBottom = C(0xF4F1EC), landA = C(0x9A8F84), landB = C(0x766C63), landDetail = C(0xEADFC0),
            },
            new VisualTheme
            {
                id = Mine, nameKey = "theme.mine.name", accent = C(0x8C7A66), seed = 1808,
                terrain = TerrainPattern.Dirt,
                ground = C(0x5E4B3C), groundDetail = C(0x7A6450),
                slab = C(0xB8A58C), slabDetail = C(0xA08D74), slabEdge = C(0x7D6B55),
                backdrop = BackdropPattern.Mountains,
                skyTop = C(0xD8D2C8), skyBottom = C(0xEFEBE4), landA = C(0x8A7D6E), landB = C(0x5E4B3C), landDetail = C(0xD9CDB5),
            },
            new VisualTheme
            {
                id = Palace, nameKey = "theme.palace.name", accent = C(0xC9A9B0), seed = 1909,
                terrain = TerrainPattern.Grass,
                ground = C(0x5F7F4E), groundDetail = C(0x4A6A3C),
                slab = C(0xE8E0D0), slabDetail = C(0xD2C7B2), slabEdge = C(0xAE9F86),
                backdrop = BackdropPattern.Castle,
                skyTop = C(0xE5DDE2), skyBottom = C(0xF6F2F2), landA = C(0xC4B49C), landB = C(0x5F7F4E), landDetail = C(0x8E7F6B),
            },
            // PG-001, Pingù: pale ice and snow, never close to the water colors nor to the Traffic jam target cars (checked by the validators)
            Ice(Lake, 2001, Icy(0xF2F5F7, 0xD9E4EA, 0xE8F1F3, 0xBFD3DA, 0xA9C6CF), BackdropPattern.Icefield, 0xCFE3EC, 0xF2F7F8, 0xE9F0F2, 0xBFD3DA, 0x8FA9B3, 0xA9C9D2),
            Ice(PackIce, 2002, Icy(0xEEF3F2, 0xD3E1DF, 0xE6EFEF, 0xBCD3D4, 0x9FBEC2), BackdropPattern.Icefield, 0xD7E6E8, 0xF3F7F6, 0xEEF3F2, 0x9FC3C2, 0x7FA3A3, 0x9FC3BC),
            Ice(Blizzard, 2003, Icy(0xE9ECEF, 0xCDD4DA, 0xE3EAEE, 0xC2CFD6, 0xA3B1BA), BackdropPattern.Icefield, 0xB8C0C8, 0xDDE2E6, 0xE6EAED, 0xC9D3DA, 0x9AA6AF, 0xB8C2CA),
            Ice(Thaw, 2004, Icy(0xEEF1EC, 0xD2DCCF, 0xE6EFF0, 0xBCD3D4, 0x9FBEC2), BackdropPattern.Icefield, 0xDCE6E0, 0xF3F6F1, 0xE8EEE6, 0xA9C2B4, 0x7E9A86, 0xA8C3A0),
            Ice(Glacier, 2005, Icy(0xEEF2F4, 0xD2DDE3, 0xE4EEF2, 0xB7CCD6, 0x98B2BF), BackdropPattern.Mountains, 0xD3E2EA, 0xF1F5F7, 0xC9D6DC, 0xE8EEF0, 0xF7F9FA, 0x9DB7C4),
            Ice(Pole, 2006, Icy(0xF4F3F1, 0xDCDDDF, 0xEDF2F3, 0xC8D8DD, 0xA6BCC4), BackdropPattern.Icefield, 0xE8E1DA, 0xF6F3EF, 0xF1F2F2, 0xC9D8DE, 0x9DAAB0, 0xC4B8B0),
            Ice(NorthernLights, 2007, Icy(0xE6ECF0, 0xC8D3DB, 0xDDE8EE, 0xAFC4CF, 0x8DA5B2), BackdropPattern.Aurora, 0x1C2433, 0x2E3B52, 0xDCE4EA, 0xAEBFCB, 0x7FD8A6, 0x8FCBB0),
        };

        /// <summary>PG-001: the snow ground and the ice tiles of a Pingù theme (ground, ground detail, slab, slab detail, slab rim).</summary>
        static int[] Icy(int ground, int groundDetail, int slab, int slabDetail, int slabEdge) => new[] { ground, groundDetail, slab, slabDetail, slabEdge };

        /// <summary>PG-001: a Pingù theme (snow terrain; backdrop colors: sky top and bottom, land A and B, detail; accent).</summary>
        static VisualTheme Ice(string id, int seed, int[] cells, BackdropPattern backdrop, int skyTop, int skyBottom, int landA, int landB, int landDetail, int accent) =>
            new VisualTheme
            {
                id = id, nameKey = "theme." + id + ".name", accent = C(accent), seed = seed,
                terrain = TerrainPattern.Snow,
                ground = C(cells[0]), groundDetail = C(cells[1]),
                slab = C(cells[2]), slabDetail = C(cells[3]), slabEdge = C(cells[4]),
                backdrop = backdrop,
                skyTop = C(skyTop), skyBottom = C(skyBottom), landA = C(landA), landB = C(landB), landDetail = C(landDetail),
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
