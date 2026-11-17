namespace ResolverMaster.Core.Puzzle
{
    public enum GameMode
    {
        Story,
        Challenge,
    }

    public static class GameModeCodes
    {
        /// <summary>Stable code used in the save file. Never change it after release.</summary>
        public static string ToCode(GameMode mode) => mode == GameMode.Story ? "story" : "challenge";
    }

    /// <summary>
    /// Which level a board must load. Story: <see cref="Index"/> is the 0-based level of the saved list.
    /// Challenge (later): the level is generated from <see cref="Seed"/> inside the difficulty tier of step <see cref="Index"/>.
    /// </summary>
    public readonly struct LevelRequest
    {
        public readonly GameMode Mode;
        public readonly int Index;
        public readonly int Seed;
        /// <summary>Challenge (ErNa-007): the generated level in the game's format and its exact minimum.</summary>
        public readonly string Data;
        public readonly int MinMoves;

        public LevelRequest(GameMode mode, int index, int seed = 0, string data = null, int minMoves = 0)
        {
            Mode = mode;
            Index = index;
            Seed = seed;
            Data = data;
            MinMoves = minMoves;
        }
    }
}
