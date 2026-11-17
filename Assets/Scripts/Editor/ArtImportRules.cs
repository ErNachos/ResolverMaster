using UnityEditor;

namespace ResolverMaster.Editor
{
    /// <summary>Import rules for the generated UI art: everything under Assets/Art/Icons and Assets/Art/UI is a crisp, uncompressed UI sprite.</summary>
    public sealed class ArtImportRules : AssetPostprocessor
    {
        static bool IsUiArt(string path) => path.StartsWith("Assets/Art/Icons/") || path.StartsWith("Assets/Art/UI/");

        void OnPreprocessTexture()
        {
            if (!IsUiArt(assetPath)) return;
            var imp = (TextureImporter)assetImporter;
            // only on first import: later manual tweaks (and the 9-slice borders set by UiKitGenerator) are kept
            if (!imp.importSettingsMissing) return;
            ApplySpriteDefaults(imp);
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
    }
}
