using System;
using System.IO;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Games.Plumber;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Creates the parameter files that do not exist yet, with the default values written in their classes
    /// (Core <see cref="PuzzleConfig"/>, Plumber <see cref="PlumberConfig"/>). Existing files are never overwritten.
    /// Batch: -executeMethod ResolverMaster.Editor.ConfigAssets.EnsureAllBatch
    /// </summary>
    public static class ConfigAssets
    {
        public const string PuzzleConfigPath = UiKitAssets.ResourcesDir + "/PuzzleConfig.asset";
        public const string PlumberDir = "Assets/Resources/Games/Plumber";
        public const string PlumberConfigPath = PlumberDir + "/PlumberConfig.asset";
        public const string PlumberStoryPath = PlumberDir + "/plumber_story.json";

        [MenuItem("Resolver Master/Setup/Ensure Config Assets")]
        public static void EnsureAll()
        {
            Ensure<PuzzleConfig>(PuzzleConfigPath);
            Ensure<PlumberConfig>(PlumberConfigPath);
            AssetDatabase.SaveAssets();
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
        /// Overwrites the UiKitStyle and the PlumberConfig with the default values written in their classes (used when a new style changes
        /// many defaults at once, e.g. ErNa-004), then regenerates the sprites and the libraries (fonts and sprites are filled again).
        /// Manual edits of those two files are lost: check them with git first.
        /// Batch only: -executeMethod ResolverMaster.Editor.ConfigAssets.ResetVisualDefaultsBatch
        /// </summary>
        public static void ResetVisualDefaultsBatch()
        {
            try
            {
                ResetToDefaults<UiKitStyle>(UiKitAssets.StylePath);
                ResetToDefaults<PlumberConfig>(PlumberConfigPath);
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
