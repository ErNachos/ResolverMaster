using System;
using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>Color vision deficiencies simulated by <see cref="ColorVision.Simulate(Color, ColorDeficiency)"/>.</summary>
    public enum ColorDeficiency
    {
        None,
        Deuteranopia,
        Protanopia,
        Tritanopia,
    }

    /// <summary>
    /// Color math shared by the validator, the previews and the tests (ErNa-006), pure logic: CIE L*a*b* (D65), the CIE76 color
    /// difference, a coarse hue family (to forbid red together with green) and the simulation of the three dichromacies with the
    /// Machado, Oliveira and Fernandes (2009) matrices at full severity, applied in linear RGB.
    /// </summary>
    public static class ColorVision
    {
        public static readonly ColorDeficiency[] Deficiencies = { ColorDeficiency.Deuteranopia, ColorDeficiency.Protanopia, ColorDeficiency.Tritanopia };

        static readonly float[][] s_deutan =
        {
            new[] { 0.367322f, 0.860646f, -0.227968f }, new[] { 0.280085f, 0.672501f, 0.047413f }, new[] { -0.011820f, 0.042940f, 0.968881f },
        };
        static readonly float[][] s_protan =
        {
            new[] { 0.152286f, 1.052583f, -0.204868f }, new[] { 0.114503f, 0.786281f, 0.099216f }, new[] { -0.003882f, -0.048116f, 1.051998f },
        };
        static readonly float[][] s_tritan =
        {
            new[] { 1.255528f, -0.076749f, -0.178779f }, new[] { -0.078411f, 0.930809f, 0.147602f }, new[] { 0.004733f, 0.691367f, 0.303900f },
        };

        public static string Code(ColorDeficiency d)
        {
            switch (d)
            {
                case ColorDeficiency.Deuteranopia: return "deut";
                case ColorDeficiency.Protanopia: return "prot";
                case ColorDeficiency.Tritanopia: return "trit";
                default: return "normal";
            }
        }

        static float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);

        static float ToSrgb(float c)
        {
            c = Mathf.Clamp01(c);
            return c <= 0.0031308f ? 12.92f * c : 1.055f * Mathf.Pow(c, 1f / 2.4f) - 0.055f;
        }

        /// <summary>CIE L*a*b* of an sRGB color (D65 white).</summary>
        public static Vector3 ToLab(Color c)
        {
            float r = ToLinear(c.r), g = ToLinear(c.g), b = ToLinear(c.b);
            float x = (0.4124f * r + 0.3576f * g + 0.1805f * b) / 0.95047f;
            float y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            float z = (0.0193f * r + 0.1192f * g + 0.9505f * b) / 1.08883f;
            float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
            return new Vector3(116f * F(y) - 16f, 500f * (F(x) - F(y)), 200f * (F(y) - F(z)));
        }

        /// <summary>Perceived lightness L* (0 black .. 100 white).</summary>
        public static float Lightness(Color c) => ToLab(c).x;

        /// <summary>CIE76 difference (about 2.3 = just noticeable; above 20 = clearly different colors).</summary>
        public static float DeltaE(Color a, Color b) => Vector3.Distance(ToLab(a), ToLab(b));

        /// <summary>The color as seen with the deficiency (alpha kept).</summary>
        public static Color Simulate(Color c, ColorDeficiency d)
        {
            float[][] m;
            switch (d)
            {
                case ColorDeficiency.Deuteranopia: m = s_deutan; break;
                case ColorDeficiency.Protanopia: m = s_protan; break;
                case ColorDeficiency.Tritanopia: m = s_tritan; break;
                default: return c;
            }
            float r = ToLinear(c.r), g = ToLinear(c.g), b = ToLinear(c.b);
            return new Color(
                ToSrgb(m[0][0] * r + m[0][1] * g + m[0][2] * b),
                ToSrgb(m[1][0] * r + m[1][1] * g + m[1][2] * b),
                ToSrgb(m[2][0] * r + m[2][1] * g + m[2][2] * b),
                c.a);
        }

        /// <summary>Fast version for whole images: a 32x32x32 lookup table of the simulation.</summary>
        public static Color32[] SimulateImage(Color32[] pixels, ColorDeficiency d)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            var lut = new Color32[32 * 32 * 32];
            for (int r = 0; r < 32; r++)
            for (int g = 0; g < 32; g++)
            for (int b = 0; b < 32; b++)
                lut[(r * 32 + g) * 32 + b] = Simulate(new Color((r * 8 + 4) / 255f, (g * 8 + 4) / 255f, (b * 8 + 4) / 255f), d);
            var result = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                var s = lut[((p.r >> 3) * 32 + (p.g >> 3)) * 32 + (p.b >> 3)];
                s.a = p.a;
                result[i] = s;
            }
            return result;
        }

        /// <summary>Strong red: hue 345..15 degrees, saturated, not dark.</summary>
        public static bool IsRed(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            float deg = h * 360f;
            return (deg >= 345f || deg <= 15f) && s > 0.4f && v > 0.35f;
        }

        /// <summary>Strong green: hue 75..165 degrees, saturated, not dark.</summary>
        public static bool IsGreen(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            float deg = h * 360f;
            return deg >= 75f && deg <= 165f && s > 0.4f && v > 0.35f;
        }
    }
}
