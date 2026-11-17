using System;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>Menu Resolver Master > Bump Version Code: +1 to the Android version code (needed for every Google Play upload).</summary>
    public static class VersionTools
    {
        [MenuItem("Resolver Master/Bump Version Code")]
        public static void BumpVersionCode()
        {
            int code = Bump(() => PlayerSettings.Android.bundleVersionCode, v => PlayerSettings.Android.bundleVersionCode = v);
            AssetDatabase.SaveAssets();
            Debug.Log($"VersionTools: version code is now {code} (version {PlayerSettings.bundleVersion}). Commit ProjectSettings/ProjectSettings.asset.");
        }

        /// <summary>Batch: -executeMethod ResolverMaster.Editor.VersionTools.BumpVersionCodeBatch</summary>
        public static void BumpVersionCodeBatch()
        {
            BumpVersionCode();
            EditorApplication.Exit(0);
        }

        /// <summary>The rule, separated from Player Settings so it can be tested: reads the code, writes code + 1, returns it.</summary>
        public static int Bump(Func<int> read, Action<int> write)
        {
            int next = checked(read() + 1);
            write(next);
            return next;
        }
    }
}
