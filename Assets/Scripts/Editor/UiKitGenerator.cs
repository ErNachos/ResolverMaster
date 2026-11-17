using System.IO;
using ResolverMaster.Core.UI;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Generates the only two UI sprites still drawn by code (ErNa-005: everything else is Gemini art in Assets/Art/UI):
    /// the frame of the Home cards (white face, thick black outline, darker band at the bottom; tinted with the game color) and the
    /// pill / circle with a thin outline (dots, badges, the off mark of a toggle).
    /// Output: Assets/Art/UI/Generated/*.png, 9-slice (border = radius, + the band); 1 texture pixel = 1 canvas unit.
    /// </summary>
    public static class UiKitGenerator
    {
        public const string OutDir = "Assets/Art/UI/Generated";
        const float Ink = 0.09f;   // outline grey level (near black)

        [MenuItem("Resolver Master/UI Kit/Regenerate Sprites")]
        public static void RegenerateSprites()
        {
            var s = UiKitAssets.LoadOrCreateStyle();
            Directory.CreateDirectory(OutDir);
            var borders = new System.Collections.Generic.Dictionary<string, Vector4>();
            void Sliced(string name, int radius, int outline, int depth)
            {
                WriteShape(name, radius * 2 + 4, radius * 2 + 4 + depth, radius, outline, depth, s.depthShade);
                borders[name] = new Vector4(radius, radius + depth, radius, radius);   // left, bottom, right, top
            }
            Sliced("card", s.cardRadius, s.outlineThickness, s.cardDepth);
            Sliced("circle", s.circleRadius, s.thinOutline, 0);

            AssetDatabase.Refresh();
            foreach (var kv in borders) ConfigureImporter($"{OutDir}/{kv.Key}.png", kv.Value);
            s.cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutDir}/card.png");
            s.circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutDir}/circle.png");
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            Debug.Log($"UiKitGenerator: card and circle regenerated in {OutDir} (radii {s.cardRadius}/{s.circleRadius}, outline {s.outlineThickness}, card depth {s.cardDepth}).");
        }

        /// <summary>Signed distance to a rounded box (negative inside).</summary>
        static float Box(Vector2 p, float x0, float y0, float x1, float y1, float r)
        {
            r = Mathf.Min(r, (x1 - x0) / 2f, (y1 - y0) / 2f);
            float cx = (x0 + x1) / 2f, cy = (y0 + y1) / 2f;
            float qx = Mathf.Abs(p.x - cx) - ((x1 - x0) / 2f - r);
            float qy = Mathf.Abs(p.y - cy) - ((y1 - y0) / 2f - r);
            return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        /// <summary>
        /// Outer shape = rounded box; face = the same box from y = depth. Inside the face: white; between face and outer edge (the bottom
        /// band): <paramref name="shade"/>; outline (and a thinner seam between face and band): near black.
        /// </summary>
        static void WriteShape(string name, int w, int h, float radius, int outline, int depth, float shade)
        {
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float dO = Box(p, 0, 0, w, h, radius);
                float a = Mathf.Clamp01(0.5f - dO);
                if (a <= 0f)
                {
                    px[y * w + x] = new Color32(255, 255, 255, 0);
                    continue;
                }
                float v;
                if (depth > 0)
                {
                    float dF = Box(p, 0, depth, w, h, radius);
                    v = Mathf.Lerp(shade, 1f, Mathf.Clamp01(0.5f - dF));
                    if (outline > 0 && p.y < depth + (h - depth) / 2f)
                    {
                        float seam = Mathf.Clamp01(0.5f - (Mathf.Abs(dF) - outline * 0.32f));
                        v = Mathf.Lerp(v, Ink, seam);
                    }
                }
                else v = 1f;
                if (outline > 0) v = Mathf.Lerp(v, Ink, Mathf.Clamp01(0.5f + dO + outline));
                byte g = (byte)Mathf.RoundToInt(v * 255);
                px[y * w + x] = new Color32(g, g, g, (byte)Mathf.RoundToInt(a * 255));
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void ConfigureImporter(string path, Vector4 border)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            ArtImportRules.ApplySpriteDefaults(imp);
            imp.spriteBorder = border;
            imp.SaveAndReimport();
        }
    }
}
