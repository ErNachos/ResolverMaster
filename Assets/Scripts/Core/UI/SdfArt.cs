using System;
using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Provisional game art drawn by code (PG-001: moved here from the Traffic jam sprites, shared by every game): signed distance shapes
    /// in cell coordinates (y up), anti-aliased fills with a black cartoon outline, alpha compositing, and the texture of a sprite of
    /// a given number of cells at a given number of pixels per cell (mipmaps, never drawn bigger than that).
    /// </summary>
    public static class SdfArt
    {
        /// <summary>Signed distance to a rounded box (negative inside).</summary>
        public static float RoundBox(Vector2 p, Vector2 centre, Vector2 half, float r)
        {
            float qx = Mathf.Abs(p.x - centre.x) - (half.x - r);
            float qy = Mathf.Abs(p.y - centre.y) - (half.y - r);
            return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        /// <summary>Distance to the segment a-b.</summary>
        public static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return (p - (a + ab * t)).magnitude;
        }

        /// <summary>Signed distance to an ellipse of radii <paramref name="r"/> (approximate, good for drawing).</summary>
        public static float Ellipse(Vector2 p, Vector2 centre, Vector2 r)
        {
            var q = new Vector2((p.x - centre.x) / r.x, (p.y - centre.y) / r.y);
            return (q.magnitude - 1f) * Mathf.Min(r.x, r.y);
        }

        public static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        /// <summary>An anti-aliased shape: black outline of width <paramref name="outline"/> inside its edge, <paramref name="body"/> inside.</summary>
        public static Color Shape(float d, float outline, Color body, int ppc)
        {
            float px = 1f / ppc;
            float alpha = Mathf.Clamp01(0.5f - d / px);
            if (alpha <= 0f) return new Color(0, 0, 0, 0);
            float inner = Mathf.Clamp01(0.5f - (d + outline) / px);
            var c = Color.Lerp(Color.black, body, inner);
            c.a = alpha * body.a;
            return c;
        }

        public static Color Fill(float d, float outline, Color body, int ppc) => Shape(d, outline, body, ppc);

        /// <summary>Plain coverage of a shape in one color (no outline).</summary>
        public static Color Coverage(float d, int ppc, Color color)
        {
            float alpha = Mathf.Clamp01(0.5f - d * ppc) * color.a;
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>Alpha compositing of <paramref name="top"/> over <paramref name="under"/>.</summary>
        public static Color Over(Color under, Color top)
        {
            float a = top.a + under.a * (1f - top.a);
            if (a <= 0f) return new Color(0, 0, 0, 0);
            var c = (top * top.a + under * under.a * (1f - top.a)) / a;
            c.a = a;
            return c;
        }

        /// <summary>A sprite <paramref name="cellsWide"/> x <paramref name="cellsTall"/> cells at <paramref name="ppc"/> pixels per cell; the pixel function gets cell coordinates (y up).</summary>
        public static Sprite Draw(string name, int ppc, float cellsWide, float cellsTall, Func<Vector2, Color> pixel)
        {
            int w = Mathf.Max(1, Mathf.RoundToInt(ppc * cellsWide)), h = Mathf.Max(1, Mathf.RoundToInt(ppc * cellsTall));
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.HideAndDontSave,
            };
            var px = new Color32[w * h];
            float step = 1f / ppc;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = pixel(new Vector2((x + 0.5f) * step, (y + 0.5f) * step));
            tex.SetPixels32(px);
            tex.Apply(true, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
