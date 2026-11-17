using UnityEditor;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Import rules (first import only: later manual tweaks and the 9-slice borders set by UiKitGenerator are kept):
    /// Assets/Art/Icons and Assets/Art/UI are crisp, uncompressed UI sprites; the illustrations (Assets/Art/Plumber, Assets/Art/Cards,
    /// and the art folder of every future minigame listed in <see cref="IllustrationDirs"/>) are sprites with mipmaps (they are drawn from
    /// 512 px down to ~100 px) and high-quality compression, at most 1024 px.
    /// </summary>
    public sealed class ArtImportRules : AssetPostprocessor
    {
        public static readonly string[] IllustrationDirs = { "Assets/Art/Plumber/", "Assets/Art/Cards/" };

        static bool IsUiArt(string path) => path.StartsWith("Assets/Art/Icons/") || path.StartsWith("Assets/Art/UI/");

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
            if (IsUiArt(assetPath)) ApplySpriteDefaults(imp);
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
