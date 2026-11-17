using System.IO;
using ResolverMaster.Core.UI;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Generates the sprites of the UI kit from the radii, outline thickness and depths of the <see cref="UiKitStyle"/>:
    /// a white face (tinted by the UI), a thick black outline (stays black whatever the tint), and for buttons, cards and panels a
    /// darker band at the bottom (the "press me" depth; tinted it becomes a darker shade of the fill). Pressed variants have a
    /// smaller band and a transparent strip on top, so in the same rect the face goes down.
    /// Output: Assets/Art/UI/*.png. Rectangles are 9-slice (border = radius, + the bands); 1 texture pixel = 1 canvas unit.
    /// The round button is not sliced: it is drawn for a square rect at 256 px.
    /// </summary>
    public static class UiKitGenerator
    {
        public const string OutDir = "Assets/Art/UI";
        const int RoundTexture = 256;
        const float Ink = 0.09f;   // outline grey level (near black)

        [MenuItem("Resolver Master/UI Kit/Regenerate Sprites")]
        public static void RegenerateSprites()
        {
            var s = UiKitAssets.LoadOrCreateStyle();
            Directory.CreateDirectory(OutDir);
            int t = s.outlineThickness, drop = Mathf.RoundToInt(s.PressDrop);

            var borders = new System.Collections.Generic.Dictionary<string, Vector4>();
            void Sliced(string name, int radius, int outline, int depth, int top)
            {
                WriteShape(name, radius * 2 + 4, radius * 2 + 4 + depth + top, radius, outline, depth, top, s.depthShade);
                borders[name] = new Vector4(radius, radius + depth, radius, radius + top);   // left, bottom, right, top
            }
            Sliced("button", s.buttonRadius, t, s.buttonDepth, 0);
            Sliced("button_pressed", s.buttonRadius, t, s.pressedDepth, drop);
            Sliced("card", s.cardRadius, t, s.cardDepth, 0);
            Sliced("card_pressed", s.cardRadius, t, Mathf.Min(s.pressedDepth, s.cardDepth), Mathf.Max(0, s.cardDepth - s.pressedDepth));
            Sliced("panel", s.panelRadius, t, s.cardDepth, 0);
            Sliced("circle", s.circleRadius, s.thinOutline, 0, 0);
            Sliced("soft", s.cardRadius, 0, 0, 0);
            WriteRing("outline", s.cardRadius, t);
            borders["outline"] = new Vector4(s.cardRadius, s.cardRadius, s.cardRadius, s.cardRadius);

            // round button: the face is a circle, the band below it; drawn at 256 px for a square of iconButtonSize units
            float k = RoundTexture / Mathf.Max(1f, s.iconButtonSize);
            int rd = Mathf.RoundToInt(s.buttonDepth * k), rp = Mathf.RoundToInt(s.pressedDepth * k), rt = Mathf.RoundToInt(t * k);
            int face = RoundTexture - rd;
            WriteShape("round", RoundTexture, RoundTexture, face / 2f, rt, rd, 0, s.depthShade, faceWidth: face);
            WriteShape("round_pressed", RoundTexture, RoundTexture, face / 2f, rt, rp, rd - rp, s.depthShade, faceWidth: face);
            borders["round"] = borders["round_pressed"] = Vector4.zero;

            AssetDatabase.Refresh();
            foreach (var kv in borders) ConfigureImporter($"{OutDir}/{kv.Key}.png", kv.Value);
            s.buttonSprite = Load("button");
            s.buttonPressedSprite = Load("button_pressed");
            s.cardSprite = Load("card");
            s.cardPressedSprite = Load("card_pressed");
            s.panelSprite = Load("panel");
            s.outlineSprite = Load("outline");
            s.circleSprite = Load("circle");
            s.roundSprite = Load("round");
            s.roundPressedSprite = Load("round_pressed");
            s.softSprite = Load("soft");
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            Debug.Log($"UiKitGenerator: sprites regenerated in {OutDir} (radii button {s.buttonRadius}, card {s.cardRadius}, panel {s.panelRadius}, " +
                      $"circle {s.circleRadius}; outline {t}, depth {s.buttonDepth}/{s.pressedDepth}, card depth {s.cardDepth}).");
        }

        static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{OutDir}/{name}.png");

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
        /// Outer shape = rounded box from y = 0 to h - top; face = the same box from y = depth. Inside the face: white; between face
        /// and outer edge (the bottom band): <paramref name="shade"/>; outline (and a thinner seam between face and band): near black.
        /// </summary>
        static void WriteShape(string name, int w, int h, float radius, int outline, int depth, int top, float shade, float faceWidth = -1)
        {
            float fx0 = 0, fx1 = w;
            if (faceWidth > 0)
            {
                fx0 = (w - faceWidth) / 2f;
                fx1 = fx0 + faceWidth;
            }
            float yTop = h - top;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float dO = Box(p, fx0, 0, fx1, yTop, radius);
                float a = Mathf.Clamp01(0.5f - dO);
                if (a <= 0f)
                {
                    px[y * w + x] = new Color32(255, 255, 255, 0);
                    continue;
                }
                float v;
                if (depth > 0)
                {
                    float dF = Box(p, fx0, depth, fx1, yTop, radius);
                    v = Mathf.Lerp(shade, 1f, Mathf.Clamp01(0.5f - dF));
                    // seam between the face and the band, only on the lower half of the face
                    if (outline > 0 && p.y < depth + (yTop - depth) / 2f)
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
            Save(name, w, h, px);
        }

        /// <summary>Outline ring only (white, tinted by the UI).</summary>
        static void WriteRing(string name, int radius, int thickness)
        {
            int size = radius * 2 + 4;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Box(new Vector2(x + 0.5f, y + 0.5f), 0, 0, size, size, radius);
                float a = Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(0.5f + d + thickness);
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
            }
            Save(name, size, size, px);
        }

        static void Save(string name, int w, int h, Color32[] px)
        {
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
