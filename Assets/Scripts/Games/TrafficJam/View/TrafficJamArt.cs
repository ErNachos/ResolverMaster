using ResolverMaster.Core.UI;
using UnityEngine;

namespace ResolverMaster.Games.TrafficJam.View
{
    /// <summary>
    /// TJ-002: the Traffic jam drawings (Assets/Art/TrafficJam, prepared by Tools/Art/make_trafficjam_art.py from the Gemini originals in
    /// ArtSource/TJ): target cars (worried and happy), cars, trucks (cargo box already lengthened to 3 cells), obstacles. Every vehicle
    /// is drawn with its front up. They are read from a sprite library (<see cref="IconLibrary"/>, Core) in
    /// Resources/Games/TrafficJam/TrafficJamArt.asset, rebuilt by Resolver Master > UI Kit > Rebuild Sprite Libraries. Names and choices:
    /// <see cref="TrafficLook"/>. A missing drawing falls back to the provisional art drawn by code (<see cref="TrafficSprites"/>).
    /// </summary>
    public sealed class TrafficJamArt
    {
        public const string ResourcesPath = "Games/TrafficJam/TrafficJamArt";
        public const string AssetDir = "Assets/Art/TrafficJam";

        readonly IconLibrary _library;

        TrafficJamArt(IconLibrary library) => _library = library;

        public static TrafficJamArt Load()
        {
            var lib = Resources.Load<IconLibrary>(ResourcesPath);
            if (lib == null) Debug.LogWarning("TrafficJamArt: Resources/" + ResourcesPath + " missing: run Resolver Master > UI Kit > Rebuild Sprite Libraries.");
            return new TrafficJamArt(lib);
        }

        /// <summary>The drawing named <paramref name="name"/>, or null (fallback art).</summary>
        public Sprite Get(string name) => _library != null && name != null ? _library.Get(name) : null;
    }
}
