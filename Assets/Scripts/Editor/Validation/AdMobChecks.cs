using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ResolverMaster.Core.Ads;
using UnityEditor;
using UnityEditor.Build;

namespace ResolverMaster.Editor.Validation
{
    /// <summary>
    /// ErNa-013: AdMob. The real app id and rewarded unit are present and well formed, test ids are Google's; the release build uses only
    /// the real ids and every other build only the test ids (ids, build symbols, plugin settings at rest); the real ids are written only in
    /// <see cref="AdMobIds"/>; the manifest gets the AD_ID permission (our library) and the app id meta-data (the plugin's
    /// ManifestProcessor, from the settings asset); the Gradle check of the build is in place.
    /// </summary>
    public sealed class AdMobCheck : IValidationCheck
    {
        public string Name => "AdMob";

        static readonly BuildKind[] TestKinds = { BuildKind.Editor, BuildKind.TestApk, BuildKind.Development };

        public void Run(ValidationReport report)
        {
            // ---- ids: present, format, real vs test ----
            if (!AdMobIds.IsValidAppId(AdMobIds.RealAppId)) report.Error($"real app id '{AdMobIds.RealAppId}' is not ca-app-pub-<16 digits>~<10 digits>");
            if (!AdMobIds.IsValidUnitId(AdMobIds.RealRewardedUnitId)) report.Error($"real rewarded unit '{AdMobIds.RealRewardedUnitId}' is not ca-app-pub-<16 digits>/<10 digits>");
            if (!AdMobIds.IsValidAppId(AdMobIds.TestAppId) || !AdMobIds.IsValidUnitId(AdMobIds.TestRewardedUnitId)) report.Error("test ids malformed");
            if (AdMobIds.IsTestId(AdMobIds.RealAppId) || AdMobIds.IsTestId(AdMobIds.RealRewardedUnitId)) report.Error("the real ids are Google's test ids");
            if (!AdMobIds.IsTestId(AdMobIds.TestAppId) || !AdMobIds.IsTestId(AdMobIds.TestRewardedUnitId)) report.Error("the test ids are not Google's");
            string realPublisher = AdMobIds.RealAppId.Split('~')[0];
            if (!AdMobIds.RealRewardedUnitId.StartsWith(realPublisher + "/")) report.Error("real app id and rewarded unit belong to different publishers");

            // ---- release = only real ids; test builds = only test ids ----
            if (AdMobIds.AppId(BuildKind.Release) != AdMobIds.RealAppId || AdMobIds.RewardedUnitId(BuildKind.Release) != AdMobIds.RealRewardedUnitId)
                report.Error("the release build does not use the real ids");
            if (AdMobIds.IsTestId(AdMobIds.AppId(BuildKind.Release)) || AdMobIds.IsTestId(AdMobIds.RewardedUnitId(BuildKind.Release)))
                report.Error("a test id in the release build");
            foreach (var kind in TestKinds)
                if (!AdMobIds.IsTestId(AdMobIds.AppId(kind)) || !AdMobIds.IsTestId(AdMobIds.RewardedUnitId(kind)))
                    report.Error($"the real id in a {kind} build");
            if (ReleaseBuild.KindOf(appBundle: true) != BuildKind.Release || ReleaseBuild.KindOf(appBundle: false) != BuildKind.TestApk)
                report.Error("ReleaseBuild: the AAB must be the release kind, the APK the test kind");
            var aab = ReleaseBuild.ExtraDefines(appBundle: true);
            var apk = ReleaseBuild.ExtraDefines(appBundle: false);
            if (!aab.Contains(RewardedAds.ReleaseBuildDefine) || aab.Contains(RewardedAds.TestBuildDefine)) report.Error("AAB symbols: release only");
            if (!apk.Contains(RewardedAds.TestBuildDefine) || apk.Contains(RewardedAds.ReleaseBuildDefine)) report.Error("test APK symbols: test only");
            if (AdMobBuild.CurrentBuildKind != BuildKind.Development) report.Error("a build kind is still pending outside a build");
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            if (defines.Contains(RewardedAds.ReleaseBuildDefine) || defines.Contains(RewardedAds.TestBuildDefine))
                report.Error("the build symbols are in the Player Settings: only ReleaseBuild adds them, per build");

            // ---- real ids only in AdMobIds ----
            string idsFile = Path.GetFullPath("Assets/Scripts/Core/Ads/AdMobIds.cs");
            foreach (var file in Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".cs" && ext != ".asset" && ext != ".xml" && ext != ".txt" && ext != ".json") continue;
                if (string.Equals(Path.GetFullPath(file), idsFile, StringComparison.OrdinalIgnoreCase)) continue;
                string text = File.ReadAllText(file);
                if (text.Contains(AdMobIds.RealAppId) || text.Contains(AdMobIds.RealRewardedUnitId))
                    report.Error($"a real AdMob id written in {file.Replace('\\', '/')} (only AdMobIds, the build picks it)");
            }

            // ---- plugin settings at rest: the test app id ----
            if (AdMobBuild.LoadSettings() == null) report.Error($"plugin settings missing: {AdMobBuild.SettingsPath} (Setup > Apply Project Settings)");
            else
            {
                string id = AdMobBuild.SettingsAppId();
                if (id != AdMobIds.TestAppId) report.Error($"plugin settings: Android app id '{id}', expected the test id at rest (the release build sets the real one)");
            }
            if (Type.GetType("GoogleMobileAds.Editor.ManifestProcessor, GoogleMobileAds.Editor") == null)
                report.Error("the plugin's ManifestProcessor is missing: no app id meta-data in the manifest");
            var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.google.ads.mobile");
            if (pkg == null) report.Error("package com.google.ads.mobile not installed");
            else
            {
                string pluginManifest = Path.Combine(pkg.resolvedPath, "Plugins/Android/GoogleMobileAdsPlugin.androidlib/AndroidManifest.xml");
                if (!File.Exists(pluginManifest)) report.Error("the plugin's Android library manifest is missing (the app id meta-data goes there)");
            }

            // ---- manifest: AD_ID ----
            if (!File.Exists(AdMobBuild.AdsLibraryManifest)) report.Error($"{AdMobBuild.AdsLibraryManifest} missing (Setup > Apply Project Settings)");
            else
            {
                try
                {
                    if (!AdMobBuild.HasAdIdPermission(XDocument.Load(AdMobBuild.AdsLibraryManifest))) report.Error($"{AdMobBuild.AdIdPermission} not in {AdMobBuild.AdsLibraryManifest}");
                }
                catch (Exception e) { report.Error($"{AdMobBuild.AdsLibraryManifest} unreadable: {e.Message}"); }
            }

            // ---- the check of the generated project works (a fake Gradle project per kind) ----
            string tmp = Path.Combine(Path.GetTempPath(), "rm_admob_check_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(tmp, "lib"));
                File.WriteAllText(Path.Combine(tmp, "lib", "AndroidManifest.xml"),
                    "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"><uses-permission android:name=\"" + AdMobBuild.AdIdPermission + "\"/>"
                    + "<application><meta-data android:name=\"" + AdMobBuild.AppIdMetaData + "\" android:value=\"" + AdMobIds.TestAppId + "\"/></application></manifest>");
                if (AdMobBuild.CheckGradleProject(tmp, BuildKind.TestApk).Count != 0) report.Error("Gradle check: rejects a correct test project");
                if (AdMobBuild.CheckGradleProject(tmp, BuildKind.Release).Count == 0) report.Error("Gradle check: accepts the test id in a release project");
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch (IOException) { }
            }
        }
    }
}
