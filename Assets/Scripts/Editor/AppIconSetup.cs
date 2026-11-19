using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// ErNa-011: the app icon and the start of the app. Icons: the PNGs made by Tools/Art/make_app_icon.py in Assets/Art/AppIcon (one
    /// per kind and density) go into every Android slot of the Player Settings (adaptive background + foreground, round, legacy). Start:
    /// no Unity splash screen and no "Made with Unity" logo. The Android 12+ system splash (the app icon on a plain colour) gets the
    /// night blue of the icon background: Unity's own theme sets a transparent icon and background there, so after the Gradle project is
    /// generated the launcher module overrides the two base themes (<see cref="SplashTheme"/>). Applied by
    /// <see cref="ProjectSetup.ApplyProjectSettings"/>; the validator checks the result (<see cref="Validation.AppIconCheck"/>).
    /// </summary>
    public static class AppIconSetup
    {
        public const string Folder = "Assets/Art/AppIcon/";
        public const string ForegroundPrefix = "AppIcon_Foreground_";
        public const string BackgroundPrefix = "AppIcon_Background_";
        public const string RoundPrefix = "AppIcon_Round_";
        public const string LegacyPrefix = "AppIcon_Legacy_";
        public const string PlayStoreIcon = "ArtSource/Icon/Icon_PlayStore_512.png";

        public static readonly PlatformIconKind[] Kinds =
            { AndroidPlatformIconKind.Adaptive, AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Legacy };

        public static string PathFor(string prefix, int size) => $"{Folder}{prefix}{size}.png";

        /// <summary>The files of one Player Settings slot: (background, foreground) for adaptive icons, one image for the others.</summary>
        public static string[] FilesFor(PlatformIconKind kind, int size)
        {
            if (kind == AndroidPlatformIconKind.Adaptive) return new[] { PathFor(BackgroundPrefix, size), PathFor(ForegroundPrefix, size) };
            return new[] { PathFor(kind == AndroidPlatformIconKind.Round ? RoundPrefix : LegacyPrefix, size) };
        }

        /// <summary>Sets every Android icon slot from the files; missing files are reported (and the slot left as it is).</summary>
        public static void Apply(StringBuilder log)
        {
            int set = 0;
            var missing = new List<string>();
            foreach (var kind in Kinds)
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                foreach (var icon in icons)
                {
                    var files = FilesFor(kind, icon.width);
                    var textures = new Texture2D[files.Length];
                    bool ok = true;
                    for (int i = 0; i < files.Length; i++)
                    {
                        textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(files[i]);
                        if (textures[i] == null)
                        {
                            missing.Add(files[i]);
                            ok = false;
                        }
                    }
                    if (!ok) continue;
                    icon.SetTextures(textures);
                    set++;
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
            }
            log.Append($"\n  Android icons: {set} slots (adaptive, round, legacy)");
            if (missing.Count > 0) Debug.LogWarning("AppIconSetup: missing icon files (run Tools/Art/make_app_icon.py): " + string.Join(", ", missing));

            // no Unity splash screen, no "Made with Unity" logo (allowed with every licence since Unity 6)
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            var bg = SplashColor();
            PlayerSettings.SplashScreen.backgroundColor = bg;
            log.Append($"\n  Unity splash screen off; Android 12+ splash background #{ColorUtility.ToHtmlStringRGB(bg)}");
        }

        /// <summary>The night blue of the adaptive background (its file), used for the Android 12+ splash too.</summary>
        public static Color32 SplashColor()
        {
            var tex = ReadPng(PathFor(BackgroundPrefix, 108));
            if (tex == null) return new Color32(2, 29, 100, 255);
            var c = (Color32)tex.GetPixel(0, 0);
            Object.DestroyImmediate(tex);
            c.a = 255;
            return c;
        }

        /// <summary>A PNG read from its file (not through the importer: the exact pixels). Null when missing. Destroy it after use.</summary>
        public static Texture2D ReadPng(string path)
        {
            if (!File.Exists(path)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(File.ReadAllBytes(path))) return tex;
            Object.DestroyImmediate(tex);
            return null;
        }

        /// <summary>
        /// The launcher's override of Unity's two base themes for Android 12+ (values-v31): the app icon on the night blue. App module
        /// resources win over the library ones with the same name.
        /// </summary>
        public static string SplashTheme(Color32 bg)
        {
            string hex = "#" + ColorUtility.ToHtmlStringRGB(bg);
            string Items() =>
                "    <item name=\"android:windowSplashScreenAnimatedIcon\">@mipmap/app_icon</item>\n" +
                "    <item name=\"android:windowSplashScreenBackground\">@color/resolvermaster_splash_background</item>\n" +
                "    <item name=\"android:windowSplashScreenIconBackgroundColor\">@color/resolvermaster_splash_background</item>\n" +
                "    <item name=\"android:windowSplashScreenAnimationDuration\">0</item>\n";
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                   "<!-- Resolver Master (ErNa-011): Android 12+ system splash = app icon on the night blue of the icon background. -->\n" +
                   "<resources>\n" +
                   $"  <color name=\"resolvermaster_splash_background\">{hex}</color>\n" +
                   "  <style name=\"BaseUnityTheme\" parent=\"android:Theme.Holo.Light.NoActionBar.Fullscreen\">\n" + Items() + "  </style>\n" +
                   "  <style name=\"BaseUnityGameActivityTheme\" parent=\"Theme.AppCompat.Light.NoActionBar\">\n" + Items() + "  </style>\n" +
                   "</resources>\n";
        }
    }

    /// <summary>Writes <see cref="AppIconSetup.SplashTheme"/> into the launcher module of every generated Gradle project.</summary>
    public sealed class AndroidSplashThemeWriter : IPostGenerateGradleAndroidProject
    {
        public const string FileName = "resolvermaster_splash.xml";
        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // path = the unityLibrary module; the launcher is next to it
            string launcher = Path.Combine(Path.GetDirectoryName(path.TrimEnd('/', '\\')) ?? path, "launcher");
            if (!Directory.Exists(launcher))
            {
                Debug.LogWarning("AndroidSplashThemeWriter: launcher module not found next to " + path);
                return;
            }
            string dir = Path.Combine(launcher, "src", "main", "res", "values-v31");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, FileName), AppIconSetup.SplashTheme(AppIconSetup.SplashColor()), new UTF8Encoding(false));
        }
    }
}
