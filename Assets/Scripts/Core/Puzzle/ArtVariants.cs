using System;
using System.Collections.Generic;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>
    /// Picks which drawing to use for the pieces of a level (e.g. one of the 4 Plumber fountains) from the level seed, so the same
    /// level always looks the same. The k-th piece of a group gets the k-th entry of a seeded shuffle of the variants: pieces of the
    /// same group in one level get different drawings until the variants run out. Pure logic (GUIDELINES 7: own SeededRandom).
    /// </summary>
    public static class ArtVariants
    {
        /// <param name="seed">The level seed.</param>
        /// <param name="group">Separates independent choices (e.g. 1 = sources, 2 = sinks).</param>
        /// <param name="ordinal">0-based position of the piece inside its group (reading order of the grid).</param>
        /// <param name="variants">How many drawings exist.</param>
        public static int Pick(int seed, int group, int ordinal, int variants)
        {
            if (variants <= 0) throw new ArgumentOutOfRangeException(nameof(variants));
            if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal));
            var rng = new SeededRandom(unchecked((ulong)(uint)seed), 0x5A17u + (ulong)(uint)group);
            var order = new int[variants];
            for (int i = 0; i < variants; i++) order[i] = i;
            rng.Shuffle(order);
            return order[ordinal % variants];
        }

        /// <summary>
        /// TJ-002: like <see cref="Pick(int,int,int,int)"/>, among the drawings a theme allows (<see cref="ThemeVariants.Allowed"/>).
        /// </summary>
        public static string Pick(int seed, int group, int ordinal, IReadOnlyList<string> allowed)
        {
            if (allowed == null || allowed.Count == 0) throw new ArgumentException("no drawing allowed", nameof(allowed));
            return allowed[Pick(seed, group, ordinal, allowed.Count)];
        }
    }

    /// <summary>
    /// TJ-002: which drawings of a group a chapter theme allows (e.g. Traffic jam obstacles: road works = barrier and manhole), kept in the
    /// parameter file of the game. A theme that is not listed, no theme (Challenge) or a list with no known drawing allow them all.
    /// </summary>
    [Serializable]
    public sealed class ThemeVariants
    {
        /// <summary>Theme id of ThemeConfig (e.g. "site").</summary>
        public string theme;
        /// <summary>Names of the allowed drawings, in a fixed order (the seed picks among them).</summary>
        public List<string> variants = new List<string>();

        public ThemeVariants() { }

        public ThemeVariants(string theme, params string[] variants)
        {
            this.theme = theme;
            this.variants = new List<string>(variants);
        }

        /// <summary>The drawings of <paramref name="all"/> that <paramref name="themeId"/> allows, in the order of <paramref name="all"/>.</summary>
        public static IReadOnlyList<string> Allowed(IEnumerable<ThemeVariants> table, string themeId, IReadOnlyList<string> all)
        {
            if (table == null || string.IsNullOrEmpty(themeId)) return all;
            foreach (var row in table)
            {
                if (row == null || row.theme != themeId || row.variants == null) continue;
                var list = new List<string>();
                foreach (var name in all)
                    if (row.variants.Contains(name)) list.Add(name);
                return list.Count > 0 ? list : all;
            }
            return all;
        }
    }
}
