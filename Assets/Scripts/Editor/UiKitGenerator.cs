using System.IO;
using ResolverMaster.Core.UI;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Generates the 9-slice sprites of the UI kit (white rounded rectangles, tinted by the UI) from the radii and thickness of the
    /// <see cref="UiKitStyle"/>. Output: Assets/Art/UI/*.png, imported as Sprite with the border set to the radius.
    /// 1 texture pixel = 1 canvas unit (sprite PPU 100 = canvas reference PPU), so a 48 px radius is 48 units on screen.
    /// </summary>
    public static class UiKitGenerator
    {
        public const string OutDir = "Assets/Art/UI";

        [MenuItem("Resolver Master/UI Kit/Regenerate Sprites")]
        public static void RegenerateSprites()
        {
            var style = UiKitAssets.LoadOrCreateStyle();
            Directory.CreateDirectory(OutDir);
            Write("button", style.buttonRadius, 0);
            Write("card", style.cardRadius, 0);
            Write("panel", style.panelRadius, 0);
            Write("outline", style.cardRadius, style.outlineThickness);
            Write("circle", style.circleRadius, 0);
            AssetDatabase.Refresh();
            foreach (var (name, radius) in new[] { ("button", style.buttonRadius), ("card", style.cardRadius), ("panel", style.panelRadius), ("outline", style.cardRadius), ("circle", style.circleRadius) })
                ConfigureImporter($"{OutDir}/{name}.png", radius);
            style.buttonSprite = Load("button");
            style.cardSprite = Load("card");
            style.panelSprite = Load("panel");
            style.outlineSprite = Load("outline");
            style.circleSprite = Load("circle");
            EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssets();
            Debug.Log($"UiKitGenerator: sprites regenerated in {OutDir} (radii button {style.buttonRadius}, card {style.cardRadius}, panel {style.panelRadius}, circle {style.circleRadius}; outline {style.outlineThickness}).");
        }

        static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{OutDir}/{name}.png");

        /// <summary>Rounded square of side 2*radius+4 (a 4 px stretchable centre); thickness 0 = filled, otherwise an outline.</summary>
        static void Write(string name, int radius, int thickness)
        {
            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // signed distance to a rounded rect filling the texture (negative inside)
                float qx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                float qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
                float d = outside + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                float a = Mathf.Clamp01(0.5f - d);
                if (thickness > 0) a *= Mathf.Clamp01(0.5f + d + thickness);
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
            }
            tex.SetPixels32(px);
            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void ConfigureImporter(string path, int radius)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            ArtImportRules.ApplySpriteDefaults(imp);
            imp.spriteBorder = new Vector4(radius, radius, radius, radius);
            imp.SaveAndReimport();
        }
    }
}
