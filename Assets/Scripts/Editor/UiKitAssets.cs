using System.IO;
using System.Linq;
using ResolverMaster.Core.UI;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>Creates and fills the UI kit assets: the UiKitStyle (fonts, sprites) and the IconLibrary (every PNG in Assets/Art/Icons).</summary>
    public static class UiKitAssets
    {
        public const string ResourcesDir = "Assets/Resources/ResolverMaster";
        public const string StylePath = ResourcesDir + "/UiKitStyle.asset";
        public const string IconLibraryPath = ResourcesDir + "/IconLibrary.asset";
        public const string IconsDir = "Assets/Art/Icons";
        public const string FontRegularPath = "Assets/Art/Fonts/Fredoka-Regular.ttf";
        public const string FontBoldPath = "Assets/Art/Fonts/Fredoka-SemiBold.ttf";

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
            RebuildIconLibrary();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Resolver Master/UI Kit/Rebuild Icon Library")]
        public static void RebuildIconLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<IconLibrary>(IconLibraryPath);
            if (lib == null)
            {
                Directory.CreateDirectory(ResourcesDir);
                lib = ScriptableObject.CreateInstance<IconLibrary>();
                AssetDatabase.CreateAsset(lib, IconLibraryPath);
            }
            lib.entries = AssetDatabase.FindAssets("t:Sprite", new[] { IconsDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(p => new IconLibrary.Entry { name = Path.GetFileNameWithoutExtension(p), sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p) })
                .Where(e => e.sprite != null)
                .ToList();
            lib.InvalidateCache();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"UiKitAssets: IconLibrary has {lib.entries.Count} icons: {string.Join(", ", lib.entries.Select(e => e.name))}");
        }
    }
}
