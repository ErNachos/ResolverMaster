using System;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// The Plumber's Challenge (ErNa-007): its step table and margins (PlumberConfig) and its generator + exact solver. Pure and
    /// thread-safe (the Core generates the next levels off the main thread): it only reads the copied table and settings.
    /// </summary>
    public sealed class PlumberChallenge : IChallengeSource
    {
        readonly int _maxAttempts;

        public DifficultyTable Table { get; }
        public ChallengeSettings Settings { get; }

        public PlumberChallenge(DifficultyTable table, ChallengeSettings settings, int maxAttempts)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Settings = settings ?? new ChallengeSettings();
            _maxAttempts = Math.Max(1, maxAttempts);
        }

        public bool TryGenerate(DifficultyTier tier, int seed, out string levelData, out int minMoves)
        {
            try
            {
                var r = PlumberGenerator.Generate(PlumberGenerator.Settings.FromTier(tier, _maxAttempts), seed, tier.id);
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
        public static PlumberLevel ToLevel(ChallengeLevel c)
        {
            var grid = PlumberLevelCodec.Parse(c.data);
            return new PlumberLevel
            {
                number = c.number, chapter = 0, index = c.number - 1, tier = c.tierId, seed = c.seed,
                width = grid.Width, height = grid.Height, minMoves = c.minMoves, cells = c.data,
            };
        }
    }
}
