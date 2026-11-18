using ResolverMaster.Core.UI;
using UnityEngine;

namespace ResolverMaster.Games.Penguin.View
{
    /// <summary>
    /// PG-002: the Pino drawings (Assets/Art/Penguin, prepared by Tools/Art/make_penguin_art.py from the originals in ArtSource/PG):
    /// penguins seen from the front (4 expressions) and from above (still, sliding), igloos (closed, open), fish, ice block, rock, tiles
    /// (ice, snow, water, sunken block, snow wall). The sprites of one family share one canvas (one scale, one anchor). They are read
    /// from a sprite library (<see cref="IconLibrary"/>, Core) in Resources/Games/Penguin/PenguinArt.asset, rebuilt by Resolver Master >
    /// UI Kit > Rebuild Sprite Libraries. Names: <see cref="PenguinLook"/>.
    /// </summary>
    public sealed class PenguinArt
    {
        public const string ResourcesPath = "Games/Penguin/PenguinArt";
        public const string AssetDir = "Assets/Art/Penguin";

        readonly IconLibrary _library;

        PenguinArt(IconLibrary library) => _library = library;

        public static PenguinArt Load()
        {
            var lib = Resources.Load<IconLibrary>(ResourcesPath);
            if (lib == null) Debug.LogWarning("PenguinArt: Resources/" + ResourcesPath + " missing: run Resolver Master > UI Kit > Rebuild Sprite Libraries.");
            return new PenguinArt(lib);
        }

        /// <summary>The drawing named <paramref name="name"/>, or null (the image is then hidden).</summary>
        public Sprite Get(string name) => _library != null && name != null ? _library.Get(name) : null;

        /// <summary>Width / height of a drawing (1 when missing).</summary>
        public float Aspect(string name)
        {
            var s = Get(name);
            return s != null && s.rect.height > 0 ? s.rect.width / s.rect.height : 1f;
        }
    }
}
