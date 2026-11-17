using System;
using System.Collections.Generic;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>
    /// Deterministic random generator (PCG32, O'Neill 2014) with only integer arithmetic: the same seed gives the same sequence on every
    /// platform and .NET runtime (unlike System.Random, whose algorithm is not guaranteed, and UnityEngine.Random, which is global).
    /// Every generator receives its own instance (GUIDELINES 7).
    /// </summary>
    public sealed class SeededRandom
    {
        const ulong Multiplier = 6364136223846793005UL;
        const ulong DefaultStream = 0xDA3E39CB94B95BDBUL;

        ulong _state;
        readonly ulong _increment;

        public SeededRandom(int seed) : this(unchecked((ulong)(uint)seed), DefaultStream) { }

        public SeededRandom(ulong seed, ulong stream)
        {
            _increment = (stream << 1) | 1UL;
            _state = 0;
            NextUInt();
            unchecked { _state += seed; }
            NextUInt();
        }

        public uint NextUInt()
        {
            unchecked
            {
                ulong old = _state;
                _state = old * Multiplier + _increment;
                uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
                int rot = (int)(old >> 59);
                return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
            }
        }

        /// <summary>Uniform integer in [0, maxExclusive) without modulo bias.</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            uint bound = (uint)maxExclusive;
            uint threshold = unchecked(0u - bound) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return (int)(r % bound);
            }
        }

        /// <summary>Uniform integer in [minInclusive, maxInclusive].</summary>
        public int Range(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive) throw new ArgumentException("max < min");
            return minInclusive + Next(maxInclusive - minInclusive + 1);
        }

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble() => NextUInt() * (1.0 / 4294967296.0);

        public bool Chance(double probability) => NextDouble() < probability;

        public T Pick<T>(IReadOnlyList<T> items) => items[Next(items.Count)];

        /// <summary>Fisher-Yates shuffle in place.</summary>
        public void Shuffle<T>(IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
