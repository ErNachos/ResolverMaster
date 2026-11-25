using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Project and Android Player Settings applied from code (reproducible, reviewable). Menu Resolver Master > Setup.
    /// Signing is NEVER written here: it is read from environment variables at build time only (<see cref="ReleaseBuild"/>).
    /// </summary>
    public static class ProjectSetup
    {
        public const string CompanyName = "eebbproduction";
        public const string ProductName = "Resolver Master";
        public const string PackageName = "com.eebbproduction.resolvermaster";
        public const string InitialVersion = "1.0.0";
        public const int TargetApi = 36;
        public const AndroidSdkVersions MinSdk = AndroidSdkVersions.AndroidApiLevel26;
        /// <summary>Tallest supported ratio is 9:21 (2.33): a little margin above it.</summary>
        public const float MaxAspectRatio = 2.4f;

        [MenuItem("Resolver Master/Setup/Apply Project Settings")]
        public static void ApplyProjectSettings()
        {
            var log = new StringBuilder("ProjectSetup:");

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, PackageName);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = InitialVersion;
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);
            log.Append($"\n  {ProductName} ({CompanyName}) {PackageName} v{PlayerSettings.bundleVersion} code {PlayerSettings.Android.bundleVersionCode}");

            // portrait only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.maxAspectRatio = MaxAspectRatio;
            PlayerSettings.Android.renderOutsideSafeArea = true;   // backgrounds fill the cutout area, SafeArea keeps content inside
            log.Append("\n  orientation: portrait only; max aspect 2.4 (9:21 supported); render outside safe area");

            // IL2CPP, ARM64, API levels. 16 KB page alignment: the NDK r27c bundled with 6000.0.85f1 aligns native libraries by default.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = MinSdk;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetApi;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;
            log.Append($"\n  IL2CPP, ARM64 only, min API {(int)MinSdk}, target API {TargetApi}");

            // new Input System only (0 = old, 1 = new, 2 = both)
            SetPlayerSettingInt("activeInputHandler", 1);
            log.Append("\n  active input handling: Input System (new) only");

            // no signing data in ProjectSettings.asset
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
            ReleaseBuild.RestoreSigningNames("", "");

            // ErNa-016: Unity audio on, global volume above 0
            AudioProjectSettings.Apply(log);

            // ErNa-011: app icons (every Android slot), no Unity splash screen, Android 12+ splash colour
            AppIconSetup.Apply(log);

            // ErNa-013: AdMob plugin settings (test app id at rest; the build picks the id of its kind) and the AD_ID library
            AdMobBuild.EnsureSettings();
            log.Append("\n  AdMob: plugin settings with the test app id, AD_ID library");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && !Application.isBatchMode)
                log.Append("\n  (active build target is not Android: switch it from Build Profiles when you want)");

            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static void SetPlayerSettingInt(string property, int value)
        {
            var so = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            var p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogWarning("ProjectSetup: Player Settings property not found: " + property);
                return;
            }
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Everything the project needs, in order. Batch: -executeMethod ResolverMaster.Editor.ProjectSetup.ApplyAllBatch</summary>
        [MenuItem("Resolver Master/Setup/Apply All")]
        public static void ApplyAll()
        {
            ApplyProjectSettings();
            UiArtSetup.ImportTmpEssentials();
            UiKitGenerator.RegenerateSprites();
            UiKitAssets.EnsureAssets();
            ConfigAssets.EnsureAll();
            MainSceneBuilder.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("ProjectSetup: Apply All done.");
        }

        public static void ApplyAllBatch()
        {
            try
            {
                ApplyAll();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
