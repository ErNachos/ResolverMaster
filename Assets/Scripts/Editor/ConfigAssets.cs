using System;
using System.IO;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Themes;
using ResolverMaster.Games.Penguin;
using ResolverMaster.Games.Plumber;
using ResolverMaster.Games.TrafficJam;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Creates the parameter files that do not exist yet, with the default values written in their classes
    /// (Core <see cref="PuzzleConfig"/> and <see cref="ThemeConfig"/>, Plumber <see cref="PlumberConfig"/>, Traffic jam
    /// <see cref="TrafficJamConfig"/>, PG-001 Pino <see cref="PenguinConfig"/>). Existing files are never overwritten; TJ-001: themes added to the classes later are appended to
    /// the existing ThemeConfig (the themes already there are not touched).
    /// Batch: -executeMethod ResolverMaster.Editor.ConfigAssets.EnsureAllBatch
    /// </summary>
    public static class ConfigAssets
    {
        public const string PuzzleConfigPath = UiKitAssets.ResourcesDir + "/PuzzleConfig.asset";
        public const string ThemeConfigPath = UiKitAssets.ResourcesDir + "/ThemeConfig.asset";
        public const string AdConfigPath = UiKitAssets.ResourcesDir + "/AdConfig.asset";
        public const string AudioConfigPath = UiKitAssets.ResourcesDir + "/AudioConfig.asset";
        public const string PlumberDir = "Assets/Resources/Games/Plumber";
        public const string PlumberConfigPath = PlumberDir + "/PlumberConfig.asset";
        public const string PlumberStoryPath = PlumberDir + "/plumber_story.json";
        public const string TrafficJamDir = "Assets/Resources/Games/TrafficJam";
        public const string TrafficJamConfigPath = TrafficJamDir + "/TrafficJamConfig.asset";
        public const string TrafficJamStoryPath = TrafficJamDir + "/trafficjam_story.json";
        public const string PenguinDir = "Assets/Resources/Games/Penguin";
        public const string PenguinConfigPath = PenguinDir + "/PenguinConfig.asset";
        public const string PenguinStoryPath = PenguinDir + "/penguin_story.json";

        [MenuItem("Resolver Master/Setup/Ensure Config Assets")]
        public static void EnsureAll()
        {
            Ensure<PuzzleConfig>(PuzzleConfigPath);
            Ensure<ResolverMaster.Core.Ads.AdConfig>(AdConfigPath);
            Ensure<ThemeConfig>(ThemeConfigPath);
            Ensure<PlumberConfig>(PlumberConfigPath);
            Ensure<TrafficJamConfig>(TrafficJamConfigPath);
            Ensure<PenguinConfig>(PenguinConfigPath);
            Ensure<ResolverMaster.Core.Audio.AudioConfig>(AudioConfigPath);
            AddMissingThemes();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// TJ-001: appends to ThemeConfig.asset every theme of the class defaults whose id it does not have yet (new games bring new
        /// themes); the themes already in the asset, edited or not, stay as they are.
        /// </summary>
        public static void AddMissingThemes()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ThemeConfig>(ThemeConfigPath);
            if (asset == null) return;
            var defaults = ScriptableObject.CreateInstance<ThemeConfig>();
            int added = 0;
            foreach (var t in defaults.themes)
            {
                if (asset.themes.Exists(x => x != null && x.id == t.id)) continue;
                asset.themes.Add(t);
                added++;
            }
            Object.DestroyImmediate(defaults);
            if (added == 0) return;
            asset.Invalidate();
            EditorUtility.SetDirty(asset);
            Debug.Log($"ConfigAssets: {added} new theme(s) added to {ThemeConfigPath}");
        }

        public static void EnsureAllBatch()
        {
            try
            {
                EnsureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Overwrites the UiKitStyle, the ThemeConfig and the PlumberConfig with the default values written in their classes (used when a
        /// new style changes many defaults at once, e.g. ErNa-004, ErNa-005), then regenerates the sprites and the libraries (fonts,
        /// materials and sprites are filled again).
        /// Manual edits of those two files are lost: check them with git first.
        /// Batch only: -executeMethod ResolverMaster.Editor.ConfigAssets.ResetVisualDefaultsBatch
        /// </summary>
        public static void ResetVisualDefaultsBatch()
        {
            try
            {
                ResetToDefaults<UiKitStyle>(UiKitAssets.StylePath);
                ResetToDefaults<PlumberConfig>(PlumberConfigPath);
                ResetToDefaults<ThemeConfig>(ThemeConfigPath);
                UiKitGenerator.RegenerateSprites();
                UiKitAssets.EnsureAssets();
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Overwrites only the game parameter files (PlumberConfig, ThemeConfig) with the default values of their classes (ErNa-006: new
        /// water colors, chapters 4-9, new themes and difficulty table). Check with git first that they had no manual edits.
        /// Batch only: -executeMethod ResolverMaster.Editor.ConfigAssets.ResetGameDefaultsBatch
        /// </summary>
        public static void ResetGameDefaultsBatch()
        {
            try
            {
                ResetToDefaults<PlumberConfig>(PlumberConfigPath);
                ResetToDefaults<ThemeConfig>(ThemeConfigPath);
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void ResetToDefaults<T>(string path) where T : ScriptableObject
        {
            var asset = Ensure<T>(path);
            var fresh = ScriptableObject.CreateInstance<T>();
            string name = asset.name;
            EditorUtility.CopySerialized(fresh, asset);
            asset.name = name;   // CopySerialized copies the (empty) name of the fresh instance too
            Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(asset);
            Debug.Log($"ConfigAssets: {path} reset to the default values of {typeof(T).Name}");
        }

        public static T Ensure<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"ConfigAssets: created {path} with default values");
            return asset;
        }
    }
}
