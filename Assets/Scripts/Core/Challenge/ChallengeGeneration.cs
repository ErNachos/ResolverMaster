using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Core.Challenge
{
    /// <summary>
    /// What a game gives to have its Challenge (ErNa-007): the step table, the margin rules, and a generator that builds a level of a
    /// step from a seed (with its own exact solver for the minimum). Everything else (seeds, steps, maximum, run, save, screens) is Core.
    /// Implementations must be pure and thread-safe: levels are generated off the main thread.
    /// </summary>
    public interface IChallengeSource
    {
        /// <summary>Steps by level number (firstLevel..lastLevel); after the last step the run stays on it.</summary>
        DifficultyTable Table { get; }

        ChallengeSettings Settings { get; }

        /// <summary>A level of <paramref name="tier"/> from <paramref name="seed"/>, within a fixed number of attempts (never a time limit).</summary>
        bool TryGenerate(DifficultyTier tier, int seed, out string levelData, out int minMoves);
    }

    /// <summary>Challenge parameters stored in the game's parameter file (the margin of moves allowed above the minimum).</summary>
    [Serializable]
    public sealed class ChallengeSettings
    {
        /// <summary>Allowed extra moves on the first step, in percent of the minimum...</summary>
        public int marginStartPercent = 60;
        /// <summary>...going down step by step to this on the last step. Never less than +1 move.</summary>
        public int marginEndPercent = 10;
        /// <summary>When a seed gives no level within its attempts, the next fixed seed (seed + 1) is tried, up to this many times.</summary>
        public int maxSeedRetries = 30;
    }

    /// <summary>A generated Challenge level: the game's level data plus what the Core shows and checks.</summary>
    [Serializable]
    public sealed class ChallengeLevel
    {
        public int number;
        /// <summary>0-based step of the table (the last one again after the table ends).</summary>
        public int step;
        public string tierId;
        /// <summary>The seed that produced the level (the level seed, + retries).</summary>
        public int seed;
        public string data;
        public int minMoves;
        public int maxMoves;
        /// <summary>Generation time (ms), filled by the editor tools only.</summary>
        [NonSerialized] public double milliseconds;
    }

    /// <summary>Seeds and limits of the Challenge, pure logic (no clock, no UnityEngine.Random, no System.Random).</summary>
    public static class ChallengeRules
    {
        /// <summary>Level seed = hash(run seed, level number, number of changes): the same on every phone.</summary>
        public static int LevelSeed(int runSeed, int level, int changes)
        {
            unchecked
            {
                ulong h = 0x9E3779B97F4A7C15UL;
                h = Mix(h ^ (uint)runSeed);
                h = Mix(h ^ ((ulong)(uint)level << 21));
                h = Mix(h ^ ((ulong)(uint)changes << 42));
                return (int)(h & 0x7FFFFFFF);
            }
        }

        /// <summary>SplitMix64 finalizer.</summary>
        static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>The step of a level (0-based) and its tier; after the last step the run stays on the hardest.</summary>
        public static DifficultyTier StepFor(DifficultyTable table, int level, out int step)
        {
            DifficultyTier last = null;
            step = -1;
            int lastIndex = -1;
            for (int i = 0; i < table.tiers.Count; i++)
            {
                var t = table.tiers[i];
                if (t.Covers(level))
                {
                    step = i;
                    return t;
                }
                if (last == null || t.lastLevel > last.lastLevel)
                {
                    last = t;
                    lastIndex = i;
                }
            }
            step = lastIndex;
            return last;
        }

        /// <summary>Margin (percent of the minimum) of a step: from the first to the last value of the settings, evenly.</summary>
        public static float MarginPercent(int step, int steps, ChallengeSettings s)
        {
            if (steps <= 1) return s.marginEndPercent;
            float k = Math.Min(1f, Math.Max(0f, step / (float)(steps - 1)));
            return s.marginStartPercent + (s.marginEndPercent - s.marginStartPercent) * k;
        }

        /// <summary>Maximum moves allowed = minimum + margin, never less than minimum + 1.</summary>
        public static int MaxMoves(int minMoves, int step, int steps, ChallengeSettings s) =>
            minMoves + Math.Max(1, (int)Math.Floor(minMoves * MarginPercent(step, steps, s) / 100f));

        /// <summary>
        /// Generates level <paramref name="level"/> of a run. Deterministic: same run seed, level and changes give the same level everywhere.
        /// If the level seed gives nothing within the generator's attempts, seed + 1, + 2, ... (fixed) are tried.
        /// </summary>
        public static ChallengeLevel Generate(IChallengeSource source, int runSeed, int level, int changes)
        {
            var tier = StepFor(source.Table, level, out int step) ?? throw new InvalidOperationException("empty Challenge table");
            int seed = LevelSeed(runSeed, level, changes);
            var s = source.Settings ?? new ChallengeSettings();
            for (int retry = 0; retry <= Math.Max(0, s.maxSeedRetries); retry++)
            {
                int attempt = unchecked(seed + retry) & 0x7FFFFFFF;
                if (!source.TryGenerate(tier, attempt, out string data, out int min)) continue;
                return new ChallengeLevel
                {
                    number = level, step = step, tierId = tier.id, seed = attempt, data = data, minMoves = min,
                    maxMoves = MaxMoves(min, step, source.Table.tiers.Count, s),
                };
            }
            throw new InvalidOperationException($"Challenge: no level {level} for step '{tier.id}' from seed {seed} after {s.maxSeedRetries} retries");
        }
    }

    /// <summary>
    /// The next levels of a run generated off the main thread (ErNa-007): asking for level n also prepares n+1 and n+2.
    /// ErNa-008: a level is identified by (number, changes): the prepared next levels have 0 changes, and the next change of the
    /// current level is prepared too, so "Change level" is instant.
    /// <see cref="TryGet"/> never blocks; <see cref="Get"/> waits (used when nothing else can be shown).
    /// </summary>
    public sealed class ChallengeQueue
    {
        public const int Ahead = 2;

        readonly IChallengeSource _source;
        readonly Dictionary<long, Task<ChallengeLevel>> _tasks = new Dictionary<long, Task<ChallengeLevel>>();
        int _runSeed;
        bool _started;

        public ChallengeQueue(IChallengeSource source) => _source = source;

        static long Key(int level, int changes) => ((long)level << 32) | (uint)changes;

        /// <summary>A new run: forgets the prepared levels (nothing happens for the same run).</summary>
        public void Reset(int runSeed)
        {
            if (_started && runSeed == _runSeed) return;
            _runSeed = runSeed;
            _started = true;
            _tasks.Clear();
        }

        /// <summary>
        /// Starts level <paramref name="level"/> (with its <paramref name="changes"/>), its next change when
        /// <paramref name="nextChange"/>, and the next <see cref="Ahead"/> levels in the background; drops the older ones.
        /// </summary>
        public void Prefetch(int level, int changes = 0, bool nextChange = false)
        {
            var old = new List<long>();
            foreach (var k in _tasks.Keys)
            {
                int n = (int)(k >> 32), c = (int)(k & 0xFFFFFFFF);
                if (n < level || (n == level && c < changes)) old.Add(k);
            }
            foreach (var k in old) _tasks.Remove(k);
            Start(level, changes);
            if (nextChange) Start(level, changes + 1);
            for (int n = level + 1; n <= level + Ahead; n++) Start(n, 0);
        }

        void Start(int level, int changes)
        {
            long key = Key(level, changes);
            if (_tasks.ContainsKey(key)) return;
            int seed = _runSeed;
            _tasks[key] = Task.Run(() => ChallengeRules.Generate(_source, seed, level, changes));
        }

        public bool TryGet(int level, int changes, out ChallengeLevel result)
        {
            result = null;
            if (!_tasks.TryGetValue(Key(level, changes), out var task) || !task.IsCompleted) return false;
            result = task.Result;
            return true;
        }

        public ChallengeLevel Get(int level, int changes = 0)
        {
            Start(level, changes);
            return _tasks[Key(level, changes)].GetAwaiter().GetResult();
        }
    }
}
