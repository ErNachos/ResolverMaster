using System.IO;
using System.Linq;
using ResolverMaster.Core.UI;
using ResolverMaster.Games.Plumber.View;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Creates and fills the UI kit assets: the UiKitStyle (fonts, sprites) and the sprite libraries (<see cref="IconLibrary"/>):
    /// icons (Assets/Art/Icons), Home card illustrations (Assets/Art/Cards) and the art of the minigames (Plumber fountains).
    /// </summary>
    public static class UiKitAssets
    {
        public const string ResourcesDir = "Assets/Resources/ResolverMaster";
        public const string StylePath = ResourcesDir + "/UiKitStyle.asset";
        public const string IconLibraryPath = ResourcesDir + "/IconLibrary.asset";
        public const string CardLibraryPath = ResourcesDir + "/CardLibrary.asset";
        public const string IconsDir = "Assets/Art/Icons";
        public const string CardsDir = "Assets/Art/Cards";
        public const string PlumberArtPath = "Assets/Resources/" + PlumberArt.ResourcesPath + ".asset";
        public const string FontRegularPath = "Assets/Art/Fonts/Fredoka-Regular.ttf";
        public const string FontBoldPath = "Assets/Art/Fonts/Fredoka-SemiBold.ttf";

        /// <summary>Every sprite library: (art folder, library asset). A new minigame with drawings adds one line here.</summary>
        public static readonly (string dir, string asset)[] Libraries =
        {
            (IconsDir, IconLibraryPath),
            (CardsDir, CardLibraryPath),
            (PlumberArt.AssetDir, PlumberArtPath),
        };

        public static UiKitStyle LoadOrCreateStyle()
        {
            var style = AssetDatabase.LoadAssetAtPath<UiKitStyle>(StylePath);
            if (style != null) return style;
            Directory.CreateDirectory(ResourcesDir);
            style = ScriptableObject.CreateInstance<UiKitStyle>();
            AssetDatabase.CreateAsset(style, StylePath);
            return style;
        }

        public static void EnsureAssets()
        {
            var style = LoadOrCreateStyle();
            style.fontRegular = AssetDatabase.LoadAssetAtPath<Font>(FontRegularPath);
            style.fontBold = AssetDatabase.LoadAssetAtPath<Font>(FontBoldPath);
            EditorUtility.SetDirty(style);
            RebuildSpriteLibraries();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Resolver Master/UI Kit/Rebuild Sprite Libraries")]
        public static void RebuildSpriteLibraries()
        {
            foreach (var (dir, asset) in Libraries) RebuildLibrary(dir, asset);
        }

        public static void RebuildSpriteLibrariesBatch()
        {
            RebuildSpriteLibraries();
            EditorApplication.Exit(0);
        }

        /// <summary>Fills the library at <paramref name="assetPath"/> with every sprite under <paramref name="dir"/> (name = file name).</summary>
        public static IconLibrary RebuildLibrary(string dir, string assetPath)
        {
            var lib = AssetDatabase.LoadAssetAtPath<IconLibrary>(assetPath);
            if (lib == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                lib = ScriptableObject.CreateInstance<IconLibrary>();
                AssetDatabase.CreateAsset(lib, assetPath);
            }
            lib.entries = AssetDatabase.IsValidFolder(dir)
                ? AssetDatabase.FindAssets("t:Sprite", new[] { dir })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Distinct()
                    .OrderBy(p => p)
                    .Select(p => new IconLibrary.Entry { name = Path.GetFileNameWithoutExtension(p), sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p) })
                    .Where(e => e.sprite != null)
                    .ToList()
                : new System.Collections.Generic.List<IconLibrary.Entry>();
            lib.InvalidateCache();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"UiKitAssets: {assetPath} has {lib.entries.Count} sprites from {dir}: {string.Join(", ", lib.entries.Select(e => e.name))}");
            return lib;
        }
    }
}
