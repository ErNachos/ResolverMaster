using System;
using System.IO;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Games.Plumber;
using UnityEditor;
using UnityEngine;

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
