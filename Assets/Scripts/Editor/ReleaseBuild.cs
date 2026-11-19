using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using ResolverMaster.Core.Ads;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Release builds: Resolver Master > Build Release AAB (the file for Google Play) and > Build Release APK (test) (same release
    /// configuration, to install with adb). Output in Builds/Release (ignored by git).
    ///
    /// Signing comes ONLY from environment variables read at build time: RESOLVERMASTER_KEYSTORE (path of the keystore, outside the
    /// project), RESOLVERMASTER_KEYSTORE_PASS, RESOLVERMASTER_KEY_ALIAS, RESOLVERMASTER_KEY_PASS. If one is missing the build stops and
    /// names it (values are never printed). After the build the signing fields are emptied again, so nothing ends up in ProjectSettings.asset.
    /// </summary>
    public static class ReleaseBuild
    {
        public const string OutDir = "Builds/Release";
        public const string EnvKeystore = "RESOLVERMASTER_KEYSTORE";
        public const string EnvKeystorePass = "RESOLVERMASTER_KEYSTORE_PASS";
        public const string EnvKeyAlias = "RESOLVERMASTER_KEY_ALIAS";
        public const string EnvKeyPass = "RESOLVERMASTER_KEY_PASS";
        public static readonly string[] EnvNames = { EnvKeystore, EnvKeystorePass, EnvKeyAlias, EnvKeyPass };

        [MenuItem("Resolver Master/Build Release AAB")]
        public static void BuildAabMenu() => BuildSigned(appBundle: true);

        [MenuItem("Resolver Master/Build Release APK (test)")]
        public static void BuildApkMenu() => BuildSigned(appBundle: false);

        /// <summary>Batch: exit 0 on success, 6 on failure (missing signing included).</summary>
        public static void BuildAabBatch() => EditorApplication.Exit(BuildSigned(appBundle: true) ? 0 : 6);

        public static void BuildApkBatch() => EditorApplication.Exit(BuildSigned(appBundle: false) ? 0 : 6);

        /// <summary>
        /// ErNa-010/013: scripting symbols added to one build only: the test APK is a test build (AdMob test ids), the AAB the only
        /// release build (real ids).
        /// </summary>
        public static string[] ExtraDefines(bool appBundle) => new[] { appBundle ? RewardedAds.ReleaseBuildDefine : RewardedAds.TestBuildDefine };

        /// <summary>ErNa-013: the kind of build each menu makes (decides the AdMob ids).</summary>
        public static BuildKind KindOf(bool appBundle) => appBundle ? BuildKind.Release : BuildKind.TestApk;

        /// <summary>Names of the signing variables that are not set (empty = all present).</summary>
        public static string[] MissingSigningVariables() =>
            EnvNames.Where(n => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(n))).ToArray();

        public static bool BuildSigned(bool appBundle)
        {
            var missing = MissingSigningVariables();
            if (missing.Length > 0)
            {
                Debug.LogError("ReleaseBuild: build stopped, signing environment variables missing: " + string.Join(", ", missing)
                    + ". Set them (see GUIDELINES.md, Build) and restart Unity Hub/Unity so it sees them.");
                return false;
            }
            string keystore = Environment.GetEnvironmentVariable(EnvKeystore);
            if (!File.Exists(keystore))
            {
                Debug.LogError($"ReleaseBuild: build stopped, the file named by {EnvKeystore} does not exist.");
                return false;
            }
            if (Path.GetFullPath(keystore).StartsWith(Path.GetFullPath("."), StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("ReleaseBuild: build stopped, the keystore is inside the project folder: move it outside the repository.");
                return false;
            }
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != ProjectSetup.PackageName)
            {
                Debug.LogError("ReleaseBuild: build stopped, package name differs from ProjectSetup: run Resolver Master > Setup > Apply Project Settings.");
                return false;
            }

            bool prevBundle = EditorUserBuildSettings.buildAppBundle, prevDev = EditorUserBuildSettings.development;
            bool prevExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            string ext = appBundle ? ".aab" : ".apk";
            string path = $"{OutDir}/ResolverMaster-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}{(appBundle ? "" : "-test")}{ext}";
            Directory.CreateDirectory(OutDir);
            var watch = Stopwatch.StartNew();
            try
            {
                EditorUserBuildSettings.buildAppBundle = appBundle;
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;   // ErNa-014: always a real .aab/.apk, never a Gradle folder
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable(EnvKeystorePass);
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable(EnvKeyAlias);
                PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable(EnvKeyPass);
                AdMobBuild.BeginReleaseBuild(KindOf(appBundle));   // ErNa-013: real AdMob ids only in the AAB

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    locationPathName = path,
                    options = BuildOptions.None,
                    extraScriptingDefines = ExtraDefines(appBundle),   // ErNa-013: test APK = AdMob test ids, AAB = real ids
                });
                bool ok = report.summary.result == BuildResult.Succeeded;
                long bytes = ok && File.Exists(path) ? new FileInfo(path).Length : 0;
                Debug.Log($"ReleaseBuild: {(ok ? "OK" : "FAILED")} {path} {bytes / 1048576f:0.0} MB in {watch.Elapsed.TotalMinutes:0.0} min, "
                    + $"version {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}), errors {report.summary.totalErrors}, warnings {report.summary.totalWarnings}");
                return ok;
            }
            finally
            {
                AdMobBuild.EndBuild();
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                PlayerSettings.Android.useCustomKeystore = false;
                RestoreSigningNames("", "");
                EditorUserBuildSettings.buildAppBundle = prevBundle;
                EditorUserBuildSettings.development = prevDev;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = prevExport;
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Writes keystore path and alias through the serialized Player Settings: the keystoreName setter turns "" into "{inproject}: ".
        /// </summary>
        public static void RestoreSigningNames(string store, string alias)
        {
            var so = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            var s = so.FindProperty("AndroidKeystoreName");
            var a = so.FindProperty("AndroidKeyaliasName");
            if (s != null) s.stringValue = store ?? "";
            if (a != null) a.stringValue = alias ?? "";
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
