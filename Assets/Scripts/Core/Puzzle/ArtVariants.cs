using System;

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
    }
}
