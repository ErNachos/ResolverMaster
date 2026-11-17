using System.Collections.Generic;
using ResolverMaster.Core.UI;
using UnityEngine;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// Rules of the water colors (ErNa-006), pure logic used by the validator and the tests: up to 4 colors, never red together with
    /// green, and every pair at least a given lightness apart (L*), so the colors stay distinguishable also for color-blind players.
    /// </summary>
    public static class PlumberPalette
    {
        public const int MaxColors = 4;

        /// <summary>Problems of a water palette (empty = valid).</summary>
        public static List<string> Problems(IReadOnlyList<Color> water, float minLightnessGap)
        {
            var problems = new List<string>();
            if (water == null || water.Count == 0)
            {
                problems.Add("no water colors");
                return problems;
            }
            if (water.Count > MaxColors) problems.Add($"{water.Count} water colors, at most {MaxColors}");
            int red = -1, green = -1;
            for (int i = 0; i < water.Count; i++)
            {
                if (ColorVision.IsRed(water[i])) red = i;
                if (ColorVision.IsGreen(water[i])) green = i;
            }
            if (red >= 0 && green >= 0) problems.Add($"red (color {red}) and green (color {green}) together");
            for (int i = 0; i < water.Count; i++)
            for (int j = i + 1; j < water.Count; j++)
            {
                float gap = Mathf.Abs(ColorVision.Lightness(water[i]) - ColorVision.Lightness(water[j]));
                if (gap < minLightnessGap) problems.Add($"colors {i} and {j}: lightness differs by {gap:0.0} (minimum {minLightnessGap:0.0})");
            }
            return problems;
        }

        /// <summary>Smallest color difference (delta E) between two water colors as seen with the deficiency.</summary>
        public static float MinDeltaE(IReadOnlyList<Color> water, ColorDeficiency deficiency, out int a, out int b)
        {
            float best = float.MaxValue;
            a = b = -1;
            for (int i = 0; i < water.Count; i++)
            for (int j = i + 1; j < water.Count; j++)
            {
                float d = ColorVision.DeltaE(ColorVision.Simulate(water[i], deficiency), ColorVision.Simulate(water[j], deficiency));
                if (d < best)
                {
                    best = d;
                    a = i;
                    b = j;
                }
            }
            return best;
        }
    }
}
