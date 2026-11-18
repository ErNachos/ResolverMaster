using System;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>
    /// The Traffic jam Challenge (TJ-001): only its step table and margins (TrafficJamConfig) and its generator with the exact solver;
    /// run, score, record, pause, save, Continue, Change level and the daily challenge are Core. Pure and thread-safe (the Core generates
    /// the next levels off the main thread): it only reads the copied table and settings.
    /// </summary>
    public sealed class TrafficChallenge : IChallengeSource
    {
        readonly int _maxAttempts, _maxStates, _maxMinMoves, _climbSteps;

        public DifficultyTable Table { get; }
        public ChallengeSettings Settings { get; }

        public TrafficChallenge(DifficultyTable table, ChallengeSettings settings, int maxAttempts, int maxStates, int maxMinMoves, int climbSteps)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Settings = settings ?? new ChallengeSettings();
            _maxAttempts = Math.Max(1, maxAttempts);
            _maxStates = Math.Max(1000, maxStates);
            _maxMinMoves = Math.Max(1, maxMinMoves);
            _climbSteps = Math.Max(0, climbSteps);
        }

        public bool TryGenerate(DifficultyTier tier, int seed, out string levelData, out int minMoves)
        {
            try
            {
                var r = TrafficGenerator.Generate(TrafficGenerator.Settings.FromTier(tier, _maxAttempts, _maxStates, _maxMinMoves, _climbSteps), seed, tier.id);
                levelData = r.Level.cells;
                minMoves = r.Level.minMoves;
                return true;
            }
            catch (InvalidOperationException)
            {
                levelData = null;
                minMoves = 0;
                return false;
            }
        }

        /// <summary>A playable level from a generated Challenge level.</summary>
        public static TrafficLevel ToLevel(ChallengeLevel c) => ToLevel(c.number, c.tierId, c.seed, c.data, c.minMoves);

        public static TrafficLevel ToLevel(int number, string tier, int seed, string data, int minMoves)
        {
            var layout = TrafficLayout.Parse(data, out _);
            return new TrafficLevel
            {
                number = number, chapter = 0, index = number - 1, tier = tier, seed = seed,
                width = layout.Width, height = layout.Height, minMoves = minMoves, cells = data,
            };
        }
    }
}
