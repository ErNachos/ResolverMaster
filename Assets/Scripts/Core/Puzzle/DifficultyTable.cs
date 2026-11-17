using System;
using System.Collections.Generic;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>An inclusive integer range for one named value of a tier (e.g. "minMoves" 3..5).</summary>
    [Serializable]
    public struct DifficultyRange
    {
        public string key;
        public int min;
        public int max;

        public DifficultyRange(string key, int min, int max)
        {
            this.key = key;
            this.min = min;
            this.max = max;
        }

        public bool Contains(int value) => value >= min && value <= max;

        public override string ToString() => min == max ? $"{key} {min}" : $"{key} {min}-{max}";
    }

    /// <summary>A difficulty step: the levels it covers and the min/max of each value. The keys are chosen by each game.</summary>
    [Serializable]
    public sealed class DifficultyTier
    {
        public string id;
        public int firstLevel;
        public int lastLevel;
        public List<DifficultyRange> values = new List<DifficultyRange>();

        public bool Covers(int level) => level >= firstLevel && level <= lastLevel;

        public bool TryGet(string key, out DifficultyRange range)
        {
            foreach (var v in values)
                if (v.key == key)
                {
                    range = v;
                    return true;
                }
            range = default;
            return false;
        }

        public DifficultyRange Get(string key) =>
            TryGet(key, out var r) ? r : throw new KeyNotFoundException($"difficulty tier '{id}' has no value '{key}'");

        /// <summary>A range, or [fallback, fallback] when the tier does not define the key.</summary>
        public DifficultyRange GetOr(string key, int fallback) => TryGet(key, out var r) ? r : new DifficultyRange(key, fallback, fallback);
    }

    /// <summary>
    /// Generic difficulty table (one per game and mode, stored in the game's parameter file). Story: tiers by level number;
    /// Challenge (later): the same tiers by run step, with levels generated from a seed inside the tier values.
    /// </summary>
    [Serializable]
    public sealed class DifficultyTable
    {
        public List<DifficultyTier> tiers = new List<DifficultyTier>();

        /// <summary>The tier covering <paramref name="level"/> (1-based), or null.</summary>
        public DifficultyTier ForLevel(int level)
        {
            foreach (var t in tiers)
                if (t.Covers(level)) return t;
            return null;
        }

        public int LastLevel
        {
            get
            {
                int last = 0;
                foreach (var t in tiers) last = Math.Max(last, t.lastLevel);
                return last;
            }
        }

        public static DifficultyTier Tier(string id, int firstLevel, int lastLevel, params DifficultyRange[] values) =>
            new DifficultyTier { id = id, firstLevel = firstLevel, lastLevel = lastLevel, values = new List<DifficultyRange>(values) };
    }
}
