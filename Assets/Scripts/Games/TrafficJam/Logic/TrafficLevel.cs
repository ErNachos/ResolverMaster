using System;
using System.Collections.Generic;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>
    /// A saved Traffic jam level (Story levels are generated once in the Editor and shipped as data, never regenerated on the phone).
    /// <see cref="cells"/> is the <see cref="TrafficLayout"/> text form of the start.
    /// </summary>
    [Serializable]
    public sealed class TrafficLevel
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
        /// <summary>Minimum moves, exact (<see cref="TrafficSpace"/>; checked again by the validator).</summary>
        public int minMoves;
        /// <summary>Positions reachable from the start (size of the light bulb's distance table).</summary>
        public int states;
        public string cells;

        public TrafficLayout CreateLayout(out ulong start) => TrafficLayout.Parse(cells, out start);
    }

    /// <summary>The JSON file of the Story levels: Assets/Resources/Games/TrafficJam/trafficjam_story.json.</summary>
    [Serializable]
    public sealed class TrafficLevelSet
    {
        public List<TrafficLevel> levels = new List<TrafficLevel>();
    }
}
