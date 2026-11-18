using System;
using System.Collections.Generic;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>
    /// A saved Pino level (Story levels are generated once in the Editor and shipped as data, never regenerated on the phone).
    /// <see cref="cells"/> is the <see cref="PenguinLayout"/> text form.
    /// </summary>
    [Serializable]
    public sealed class PenguinLevel
    {
        /// <summary>1-based position in the Story (numbered across chapters: 1..63).</summary>
        public int number;
        /// <summary>1-based chapter.</summary>
        public int chapter;
        /// <summary>0-based position inside the chapter. (game, chapter, index) is the stable id used by the save.</summary>
        public int index;
        /// <summary>Difficulty tier id it was generated in.</summary>
        public string tier;
        public int seed;
        public int width;
        public int height;
        /// <summary>Minimum moves, exact (<see cref="PenguinSpace"/>; checked again by the validator).</summary>
        public int minMoves;
        /// <summary>States reachable from the start (size of the table of the light bulb and of "You are stuck").</summary>
        public int states;
        public string cells;

        public PenguinLayout CreateLayout() => PenguinLayout.Parse(cells);
    }

    /// <summary>The JSON file of the Story levels: Assets/Resources/Games/Penguin/penguin_story.json.</summary>
    [Serializable]
    public sealed class PenguinLevelSet
    {
        public List<PenguinLevel> levels = new List<PenguinLevel>();
    }

    /// <summary>One swipe with the states before and after it (undo puts the state before back: slides cannot always be undone by a swipe).</summary>
    public readonly struct PenguinMove
    {
        public readonly int Dir;
        public readonly ulong Before, After;
        /// <summary>A penguin falls into the water with this swipe.</summary>
        public readonly bool Fell;

        public PenguinMove(int dir, ulong before, ulong after, bool fell)
        {
            Dir = dir;
            Before = before;
            After = after;
            Fell = fell;
        }

        /// <summary>Challenge save: the direction (the states follow from the start).</summary>
        public int Encode() => Dir;
    }
}
