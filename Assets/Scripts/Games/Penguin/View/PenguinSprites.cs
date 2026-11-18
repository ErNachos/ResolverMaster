using System.Collections.Generic;
using UnityEngine;
using static ResolverMaster.Core.UI.SdfArt;

namespace ResolverMaster.Games.Penguin.View
{
    /// <summary>
    /// The few Pino effects still drawn by code (PG-001; since PG-002 everything else is a drawing, <see cref="PenguinArt"/>): the light
    /// bulb arrow, the splash ring of a fall and the sparkle of a fish. Every texture has <see cref="PenguinConfig.spritePixelsPerCell"/>
    /// pixels per cell. Cached per set of config values for the whole run.
    /// </summary>
    public sealed class PenguinSprites
    {
        static readonly Dictionary<string, PenguinSprites> s_cache = new Dictionary<string, PenguinSprites>();

        public Sprite Arrow, Splash, Sparkle;

        public static PenguinSprites For(PenguinConfig cfg)
        {
            string key = string.Join("|", cfg.spritePixelsPerCell, cfg.outline);
            if (s_cache.TryGetValue(key, out var cached) && cached.IsAlive) return cached;
            var s = new PenguinSprites();
            s.Generate(cfg);
            s_cache[key] = s;
            return s;
        }

        bool IsAlive => Arrow != null && Splash != null && Sparkle != null;

        void Generate(PenguinConfig cfg)
        {
            int ppc = Mathf.Clamp(cfg.spritePixelsPerCell, 32, 512);
            float ol = cfg.outline;

            // ---- the light bulb arrow (points up), a splash ring, a sparkle
            Arrow = Draw("pg_hint_arrow", ppc, 1, 1, p =>
            {
                float head = Mathf.Max(Mathf.Abs(p.x - 0.5f) * 1.1f + (p.y - 0.86f) * 0.62f, 0.5f - p.y);
                float shaft = RoundBox(p, new Vector2(0.5f, 0.32f), new Vector2(0.11f, 0.2f), 0.05f);
                return Fill(Mathf.Min(head, shaft), ol * 1.1f, Color.white, ppc);
            });
            Splash = Draw("pg_splash", ppc, 1, 1, p =>
            {
                var c = Coverage(Mathf.Abs((p - new Vector2(0.5f, 0.5f)).magnitude - 0.34f) - 0.035f, ppc, new Color(1, 1, 1, 0.9f));
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f + 0.3f;
                    var s = new Vector2(0.5f + Mathf.Cos(a) * 0.43f, 0.5f + Mathf.Sin(a) * 0.43f);
                    c = Over(c, Shape((p - s).magnitude - 0.035f, ol * 0.35f, new Color(0.8f, 0.93f, 1f, 1f), ppc));
                }
                return c;
            });
            Sparkle = Draw("pg_sparkle", ppc, 1, 1, p => Shape(Star(p, new Vector2(0.5f, 0.5f), 0.3f), ol * 0.6f, new Color(1f, 0.93f, 0.5f, 1f), ppc));
        }

        /// <summary>A five-pointed star of radius <paramref name="r"/> (approximate distance).</summary>
        static float Star(Vector2 p, Vector2 c, float r)
        {
            var q = p - c;
            float a = Mathf.Atan2(q.y, q.x) + Mathf.PI / 2f;
            float k = Mathf.Repeat(a, Mathf.PI * 2f / 5f) / (Mathf.PI * 2f / 5f);
            float edge = Mathf.Lerp(r * 0.45f, r, Mathf.Abs(k - 0.5f) * 2f);
            return q.magnitude - edge;
        }
    }
}
