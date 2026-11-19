using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ResolverMaster.Core.Ads;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// ErNa-013: the AdMob app id of every Android build, chosen automatically (never by hand): the real id only for the release AAB
    /// of <see cref="ReleaseBuild"/>, Google's test id for every other build (test APK, development, a build made from Build Settings).
    /// The plugin writes the id of its settings asset into the manifest (its ManifestProcessor); before it runs, <see cref="AdMobPreBuild"/>
    /// puts the right id in the settings (in memory, never saved), and after the build the test id is back. The generated Gradle project
    /// is then checked (<see cref="AdMobGradleCheck"/>): app id of the build kind, AD_ID permission; otherwise the build stops.
    /// </summary>
    public static class AdMobBuild
    {
        /// <summary>The settings asset of the Google Mobile Ads plugin (created by <see cref="EnsureSettings"/>).</summary>
        public const string SettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        const string SettingsTypeName = "GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor";
        const string AppIdField = "adMobAndroidAppId";

        public const string AppIdMetaData = "com.google.android.gms.ads.APPLICATION_ID";
        public const string AdIdPermission = "com.google.android.gms.permission.AD_ID";

        /// <summary>Our Android library: declares the AD_ID permission explicitly (it also comes from the SDK).</summary>
        public const string AdsLibraryManifest = "Assets/Plugins/Android/ResolverMasterAds.androidlib/AndroidManifest.xml";

        static BuildKind? _releaseBuildKind;

        /// <summary>The kind of the Android build being made: set by ReleaseBuild, otherwise a development (test ids) build.</summary>
        public static BuildKind CurrentBuildKind => _releaseBuildKind ?? BuildKind.Development;

        /// <summary>ReleaseBuild: the build that starts now is a release AAB or a test APK.</summary>
        public static void BeginReleaseBuild(BuildKind kind) => _releaseBuildKind = kind;

        /// <summary>After every build (also a failed one): back to the test id, no build kind pending.</summary>
        public static void EndBuild()
        {
            _releaseBuildKind = null;
            SetSettingsAppId(AdMobIds.TestAppId);
        }

        public static ScriptableObject LoadSettings() => AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsPath);

        /// <summary>The app id at rest in the plugin settings (always the test id in the repository).</summary>
        public static string SettingsAppId()
        {
            var settings = LoadSettings();
            if (settings == null) return null;
            return new SerializedObject(settings).FindProperty(AppIdField)?.stringValue;
        }

        /// <summary>Writes the Android app id into the plugin settings, in memory (the plugin reads the loaded instance).</summary>
        public static bool SetSettingsAppId(string appId, bool save = false)
        {
            var settings = LoadSettings();
            if (settings == null) return false;
            var so = new SerializedObject(settings);
            var prop = so.FindProperty(AppIdField);
            if (prop == null) return false;
            if (prop.stringValue != appId)
            {
                prop.stringValue = appId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (save)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }
            return true;
        }

        /// <summary>Setup: the plugin settings asset with the test app id (created when missing) and our ads library manifest.</summary>
        public static void EnsureSettings()
        {
            if (LoadSettings() == null)
            {
                var type = Type.GetType(SettingsTypeName);
                if (type == null)
                {
                    Debug.LogError("AdMobBuild: Google Mobile Ads plugin not found (Packages/manifest.json: com.google.ads.mobile).");
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath) ?? "Assets");
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance(type), SettingsPath);
            }
            SetSettingsAppId(AdMobIds.TestAppId, save: true);
            EnsureAdsLibrary();
        }

        /// <summary>The manifest of our Android library: only the AD_ID permission.</summary>
        public static string AdsLibraryManifestText() =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<!-- Resolver Master (ErNa-013): rewarded ads. AD_ID is declared here as well, so the permission never depends on the SDK. -->\n" +
            "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" package=\"com.eebbproduction.resolvermaster.ads\">\n" +
            $"  <uses-permission android:name=\"{AdIdPermission}\"/>\n" +
            "</manifest>\n";

        static void EnsureAdsLibrary()
        {
            string dir = Path.GetDirectoryName(AdsLibraryManifest) ?? "";
            Directory.CreateDirectory(dir);
            string props = Path.Combine(dir, "project.properties");
            if (!File.Exists(props)) File.WriteAllText(props, "android.library=true\n", new UTF8Encoding(false));
            if (!File.Exists(AdsLibraryManifest) || File.ReadAllText(AdsLibraryManifest) != AdsLibraryManifestText())
                File.WriteAllText(AdsLibraryManifest, AdsLibraryManifestText(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ manifests

        static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";

        /// <summary>The values of the app id meta-data in a manifest (normally zero or one).</summary>
        public static IEnumerable<string> AppIdsIn(XDocument manifest) =>
            manifest.Descendants("meta-data")
                .Where(e => (string)e.Attribute(Android + "name") == AppIdMetaData)
                .Select(e => (string)e.Attribute(Android + "value"));

        public static bool HasAdIdPermission(XDocument manifest) =>
            manifest.Descendants("uses-permission").Any(e => (string)e.Attribute(Android + "name") == AdIdPermission);

        /// <summary>
        /// Checks the manifests of a generated Gradle project: exactly the app id of <paramref name="kind"/>, never the other one, and
        /// the AD_ID permission. Returns the problems (empty = OK).
        /// </summary>
        public static List<string> CheckGradleProject(string gradleRoot, BuildKind kind)
        {
            var problems = new List<string>();
            string expected = AdMobIds.AppId(kind);
            var ids = new List<string>();
            bool adId = false;
            foreach (var file in Directory.GetFiles(gradleRoot, "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                string rel = file.Substring(gradleRoot.Length).Replace('\\', '/');
                if (rel.Contains("/build/") || rel.Contains("/.gradle/")) continue;
                XDocument doc;
                try { doc = XDocument.Load(file); }
                catch (Exception e)
                {
                    problems.Add($"{rel}: unreadable ({e.Message})");
                    continue;
                }
                ids.AddRange(AppIdsIn(doc));
                adId |= HasAdIdPermission(doc);
            }
            if (ids.Count == 0) problems.Add($"no {AppIdMetaData} meta-data in the Gradle project");
            foreach (var id in ids.Distinct())
                if (id != expected) problems.Add($"{AppIdMetaData} is {id}, the {kind} build needs {expected}");
            if (!adId) problems.Add($"permission {AdIdPermission} missing");
            return problems;
        }
    }

    /// <summary>ErNa-013: before the plugin's ManifestProcessor (order 0) and its Gradle pre-processor (-1): the app id of this build.</summary>
    public sealed class AdMobPreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            var kind = AdMobBuild.CurrentBuildKind;
            if (!AdMobBuild.SetSettingsAppId(AdMobIds.AppId(kind)))
                throw new BuildFailedException("AdMob: plugin settings missing: run Resolver Master > Setup > Apply Project Settings.");
            Debug.Log($"AdMob: {kind} build, app id {(kind == BuildKind.Release ? "real" : "test")}, rewarded unit {(kind == BuildKind.Release ? "real" : "test")}.");
        }
    }

    /// <summary>ErNa-013: after a successful build the plugin settings go back to the test id (a failed build: ReleaseBuild does it).</summary>
    public sealed class AdMobPostBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1000;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android) AdMobBuild.SetSettingsAppId(AdMobIds.TestAppId);
        }
    }

    /// <summary>ErNa-013: the generated Gradle project has the app id of the build kind and AD_ID, otherwise the build stops.</summary>
    public sealed class AdMobGradleCheck : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // path = the unityLibrary module; the plugin's library and ours are inside it or next to it
            string root = Path.GetDirectoryName(path.TrimEnd('/', '\\')) ?? path;
            var kind = AdMobBuild.CurrentBuildKind;
            var problems = AdMobBuild.CheckGradleProject(root, kind);
            if (problems.Count > 0) throw new BuildFailedException("AdMob: " + string.Join("; ", problems));
            Debug.Log($"AdMob: Gradle project OK ({kind}: app id {AdMobIds.AppId(kind)}, AD_ID present).");
        }
    }
}
