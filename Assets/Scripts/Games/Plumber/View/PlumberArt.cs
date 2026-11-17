using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using UnityEngine;

namespace ResolverMaster.Games.Plumber.View
{
    /// <summary>
    /// The fountain drawings of the Plumber (Assets/Art/Plumber, prepared by Tools/Art/make_plumber_art.py from the Gemini originals):
    /// fonte_N + fonte_N_mask (sources: the mask is tinted with the water color) and arrivo_N (sinks). They are read from a sprite
    /// library (<see cref="IconLibrary"/>, Core) in Resources/Games/Plumber/PlumberArt.asset, rebuilt by
    /// Resolver Master > UI Kit > Rebuild Sprite Libraries. The variant of each piece comes from the level seed (<see cref="ArtVariants"/>).
    /// </summary>
    public sealed class PlumberArt
    {
        public const string ResourcesPath = "Games/Plumber/PlumberArt";
        public const string AssetDir = "Assets/Art/Plumber";
        /// <summary><see cref="ArtVariants"/> groups: sources and sinks are chosen independently.</summary>
        public const int SourceGroup = 1, SinkGroup = 2;

        public static string SourceName(int variant) => "fonte_" + (variant + 1);
        public static string SourceMaskName(int variant) => SourceName(variant) + "_mask";
        public static string SinkName(int variant) => "arrivo_" + (variant + 1);

        readonly IconLibrary _library;

        PlumberArt(IconLibrary library) => _library = library;

        public bool Loaded => _library != null;

        public static PlumberArt Load()
        {
            var lib = Resources.Load<IconLibrary>(ResourcesPath);
            if (lib == null) Debug.LogWarning("PlumberArt: Resources/" + ResourcesPath + " missing: run Resolver Master > UI Kit > Rebuild Sprite Libraries.");
            return new PlumberArt(lib);
        }

        public Sprite Source(int variant) => _library != null ? _library.Get(SourceName(variant)) : null;
        public Sprite SourceMask(int variant) => _library != null ? _library.Get(SourceMaskName(variant)) : null;
        public Sprite Sink(int variant) => _library != null ? _library.Get(SinkName(variant)) : null;
    }
}
