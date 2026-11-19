using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace ResolverMaster.Editor.Validation
{
    /// <summary>
    /// ErNa-011: every Android icon slot of the Player Settings (adaptive background + foreground, round, legacy, every density) has its
    /// file from Assets/Art/AppIcon at the right size; no icon has white corners; the adaptive foreground stays inside the safe zone (circle
    /// of 66 dp on 108 dp); the Play Store icon is 512x512, opaque to the edges, under 1 MB. The Unity splash screen and logo are off.
    /// </summary>
    public sealed class AppIconCheck : IValidationCheck
    {
        public string Name => "AppIcon";

        const float SafeRadius = 33f / 108f;
        const int MaxStoreBytes = 1024 * 1024;

        public void Run(ValidationReport report)
        {
            foreach (var kind in AppIconSetup.Kinds)
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                if (icons.Length == 0) report.Error($"no Android {kind} icon slots");
                foreach (var icon in icons)
                {
                    var files = AppIconSetup.FilesFor(kind, icon.width);
                    var textures = icon.GetTextures();
                    for (int i = 0; i < files.Length; i++)
                    {
                        var tex = textures != null && i < textures.Length ? textures[i] : null;
                        if (tex == null)
                        {
                            report.Error($"{kind} {icon.width}px: layer {i} not set (Setup > Apply Project Settings)");
                            continue;
                        }
                        string path = AssetDatabase.GetAssetPath(tex);
                        if (path != files[i]) report.Error($"{kind} {icon.width}px: layer {i} is '{path}', expected '{files[i]}'");
                        CheckImage(report, path, icon.width, $"{kind} {icon.width}px", opaque: kind == AndroidPlatformIconKind.Legacy,
                            foreground: kind == AndroidPlatformIconKind.Adaptive && i == 1);
                    }
                }
            }

            if (!File.Exists(AppIconSetup.PlayStoreIcon)) report.Error($"Play Store icon missing: {AppIconSetup.PlayStoreIcon} (Tools/Art/make_app_icon.py)");
            else
            {
                if (new FileInfo(AppIconSetup.PlayStoreIcon).Length >= MaxStoreBytes) report.Error("Play Store icon: 1 MB or more");
                CheckImage(report, AppIconSetup.PlayStoreIcon, 512, "Play Store icon", opaque: true, foreground: false);
            }

            if (PlayerSettings.SplashScreen.show) report.Error("the Unity splash screen is on (Setup > Apply Project Settings)");
            if (PlayerSettings.SplashScreen.showUnityLogo) report.Error("the 'Made with Unity' logo is on (Setup > Apply Project Settings)");
        }

        static void CheckImage(ValidationReport report, string path, int size, string what, bool opaque, bool foreground)
        {
            var tex = AppIconSetup.ReadPng(path);
            if (tex == null)
            {
                report.Error($"{what}: cannot read '{path}'");
                return;
            }
            try
            {
                if (tex.width != size || tex.height != size) report.Error($"{what}: {tex.width}x{tex.height}, expected {size}x{size}");
                var px = tex.GetPixels32();
                int w = tex.width, h = tex.height;
                // a corner (3x3 pixels) is white when a visible pixel is almost white
                foreach (var (cx, cy) in new[] { (0, 0), (w - 3, 0), (0, h - 3), (w - 3, h - 3) })
                    for (int y = cy; y < cy + 3; y++)
                    for (int x = cx; x < cx + 3; x++)
                    {
                        var c = px[y * w + x];
                        if (c.a > 128 && c.r >= 236 && c.g >= 236 && c.b >= 236)
                        {
                            report.Error($"{what}: white corner at ({x}, {y})");
                            goto cornersDone;
                        }
                    }
                cornersDone:
                if (opaque)
                    foreach (var c in px)
                        if (c.a < 255)
                        {
                            report.Error($"{what}: not opaque to the edges");
                            break;
                        }
                if (foreground)
                {
                    // the farthest visible pixel from the centre must be inside the safe zone
                    float half = w / 2f, far = 0f;
                    for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (px[y * w + x].a > 8) far = Mathf.Max(far, Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)));
                    if (far > SafeRadius * w) report.Error($"{what}: the drawing reaches {far / w:0.000} of the side, outside the safe zone ({SafeRadius:0.000})");
                }
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }
    }
}
