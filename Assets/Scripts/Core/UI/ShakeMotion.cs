using System;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// A short side-to-side shake (ErNa-006: a tap on a grid element that cannot change), pure logic: the offset at a time, fading out.
    /// Every game uses it for its locked pieces, so the feedback is the same everywhere.
    /// </summary>
    public static class ShakeMotion
    {
        /// <summary>Horizontal offset <paramref name="t"/> seconds after the tap (0 before and after the shake).</summary>
        public static float Offset(float t, float duration, float amplitude, int cycles)
        {
            if (t <= 0f || duration <= 0f || t >= duration) return 0f;
            float u = t / duration;
            return amplitude * (float)Math.Sin(2.0 * Math.PI * Math.Max(1, cycles) * u) * (1f - u);
        }
    }
}
