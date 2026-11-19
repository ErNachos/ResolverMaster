using UnityEditor;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Import rules (first import only: later manual tweaks and the 9-slice borders set by the setup are kept):
    /// Assets/Art/Icons (placeholder masks) are crisp, uncompressed UI sprites; Assets/Art/UI (Gemini interface art, drawn smaller than
    /// its size) and Assets/Art/UI/Generated are uncompressed sprites with mipmaps; the illustrations (Assets/Art/Plumber, Assets/Art/Cards,
    /// and the art folder of every future minigame listed in <see cref="IllustrationDirs"/>) are sprites with mipmaps (they are drawn from
    /// 512 px down to ~100 px) and high-quality compression, at most 1024 px. PG-002: the drawings in <see cref="CrispIllustrationDirs"/>
    /// (Pino: a little blurry already) are imported like the Gemini UI art: mipmaps, no compression (clean outlines). ErNa-011: the app
    /// icons (<see cref="AppIconSetup.Folder"/>) are plain textures, uncompressed, never resized.
    /// </summary>
    public sealed class ArtImportRules : AssetPostprocessor
    {
        public static readonly string[] IllustrationDirs = { "Assets/Art/Plumber/", "Assets/Art/Cards/", "Assets/Art/TrafficJam/" };
        public static readonly string[] CrispIllustrationDirs = { "Assets/Art/Penguin/" };

        static bool IsIcon(string path) => path.StartsWith("Assets/Art/Icons/");
        static bool IsAppIcon(string path) => path.StartsWith(AppIconSetup.Folder);

        static bool IsUiArt(string path)
        {
            if (path.StartsWith("Assets/Art/UI/")) return true;
            foreach (var dir in CrispIllustrationDirs)
                if (path.StartsWith(dir)) return true;
            return false;
        }

        static bool IsIllustration(string path)
        {
            foreach (var dir in IllustrationDirs)
                if (path.StartsWith(dir)) return true;
            return false;
        }

        void OnPreprocessTexture()
        {
            var imp = (TextureImporter)assetImporter;
            if (!imp.importSettingsMissing) return;
            if (IsAppIcon(assetPath)) ApplyAppIconDefaults(imp);
            else if (IsIcon(assetPath)) ApplySpriteDefaults(imp);
            else if (IsUiArt(assetPath)) ApplyUiArtDefaults(imp);
            else if (IsIllustration(assetPath)) ApplyIllustrationDefaults(imp);
        }

        public static void ApplySpriteDefaults(TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            imp.filterMode = UnityEngine.FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
        }

        /// <summary>ErNa-011: app icons (Player Settings): exact pixels, one file per density: no scaling, mipmaps or compression.</summary>
        public static void ApplyAppIconDefaults(TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Default;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            imp.filterMode = UnityEngine.FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
        }

        /// <summary>Gemini UI art: never scaled up, often drawn at half its size: mipmaps keep it smooth; no compression (sharp outlines).</summary>
        public static void ApplyUiArtDefaults(TextureImporter imp)
        {
            ApplySpriteDefaults(imp);
            imp.mipmapEnabled = true;
            imp.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            imp.filterMode = UnityEngine.FilterMode.Trilinear;
        }

        public static void ApplyIllustrationDefaults(TextureImporter imp)
        {
            ApplySpriteDefaults(imp);
            imp.mipmapEnabled = true;
            imp.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            imp.filterMode = UnityEngine.FilterMode.Trilinear;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.maxTextureSize = 1024;
        }
    }
}
