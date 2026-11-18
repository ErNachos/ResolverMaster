using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ResolverMaster.Core.UI;
using ResolverMaster.Games.Penguin.View;
using ResolverMaster.Games.Plumber.View;
using ResolverMaster.Games.TrafficJam.View;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Creates and fills the UI kit assets: the UiKitStyle (TMP fonts, materials, Gemini and generated sprites) and the sprite libraries
    /// (<see cref="IconLibrary"/>): icons (Gemini icon_* drawings in Assets/Art/UI + placeholder masks in Assets/Art/Icons), Home card
    /// illustrations (Assets/Art/Cards) and the art of the minigames (Plumber fountains, Traffic jam vehicles and obstacles, Pino).
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
        public const string TrafficJamArtPath = "Assets/Resources/" + TrafficJamArt.ResourcesPath + ".asset";
        public const string PenguinArtPath = "Assets/Resources/" + PenguinArt.ResourcesPath + ".asset";
        public const string FontRegularPath = "Assets/Art/Fonts/Fredoka-Regular.ttf";
        public const string FontBoldPath = "Assets/Art/Fonts/Fredoka-SemiBold.ttf";

        /// <summary>A sprite library: the folders it reads (with an optional file-name filter) and its asset.</summary>
        public sealed class Library
        {
            public string[] Dirs;
            public Func<string, bool> Accept;
            public string Asset;
        }

        /// <summary>Every sprite library. A new minigame with drawings adds one line here.</summary>
        public static readonly Library[] Libraries =
        {
            new Library { Dirs = new[] { UiArtSetup.UiArtDir, IconsDir }, Accept = n => !n.Contains("/UI/") || Path.GetFileName(n).StartsWith("icon_"), Asset = IconLibraryPath },
            new Library { Dirs = new[] { CardsDir }, Asset = CardLibraryPath },
            new Library { Dirs = new[] { PlumberArt.AssetDir }, Asset = PlumberArtPath },
            new Library { Dirs = new[] { TrafficJamArt.AssetDir }, Asset = TrafficJamArtPath },
            new Library { Dirs = new[] { PenguinArt.AssetDir }, Asset = PenguinArtPath },
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

        /// <summary>Fonts, Gemini sprite import settings, materials, style and libraries (TMP Essentials must be imported already).</summary>
        public static void EnsureAssets()
        {
            var style = LoadOrCreateStyle();
            UiArtSetup.EnsureFonts();
            int configured = UiArtSetup.ConfigureUiArt();
            UiArtSetup.FillStyle(style);
            RebuildSpriteLibraries();
            AssetDatabase.SaveAssets();
            Debug.Log($"UiKitAssets: style filled, {configured} Gemini UI sprites configured.");
        }

        [MenuItem("Resolver Master/UI Kit/Rebuild Sprite Libraries")]
        public static void RebuildSpriteLibraries()
        {
            foreach (var lib in Libraries) RebuildLibrary(lib);
        }

        public static void RebuildSpriteLibrariesBatch()
        {
            RebuildSpriteLibraries();
            EditorApplication.Exit(0);
        }

        /// <summary>Fills the library with every sprite of its folders (name = file name; the first folder wins on equal names).</summary>
        public static IconLibrary RebuildLibrary(Library library)
        {
            var lib = AssetDatabase.LoadAssetAtPath<IconLibrary>(library.Asset);
            if (lib == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(library.Asset));
                lib = ScriptableObject.CreateInstance<IconLibrary>();
                AssetDatabase.CreateAsset(lib, library.Asset);
            }
            var entries = new List<IconLibrary.Entry>();
            var seen = new HashSet<string>();
            foreach (var dir in library.Dirs)
            {
                if (!AssetDatabase.IsValidFolder(dir)) continue;
                foreach (var path in AssetDatabase.FindAssets("t:Sprite", new[] { dir }).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p))
                {
                    if (library.Accept != null && !library.Accept(path)) continue;
                    var name = Path.GetFileNameWithoutExtension(path);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null || !seen.Add(name)) continue;
                    entries.Add(new IconLibrary.Entry { name = name, sprite = sprite });
                }
            }
            lib.entries = entries.OrderBy(e => e.name).ToList();
            lib.InvalidateCache();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"UiKitAssets: {library.Asset} has {lib.entries.Count} sprites from {string.Join(", ", library.Dirs)}: {string.Join(", ", lib.entries.Select(e => e.name))}");
            return lib;
        }
    }
}
