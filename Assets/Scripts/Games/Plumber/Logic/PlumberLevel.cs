using System;
using System.Collections.Generic;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// A saved plumber level (Story levels are generated once in the Editor and shipped as data, never regenerated on the phone).
    /// <see cref="cells"/> uses the <see cref="PlumberLevelCodec"/> text form.
    /// </summary>
    [Serializable]
    public sealed class PlumberLevel
    {
        /// <summary>1-based position in the Story.</summary>
        public int number;
        /// <summary>Difficulty tier id it was generated in.</summary>
        public string tier;
        public int seed;
        public int width;
        public int height;
        /// <summary>Minimum taps, computed by <see cref="PlumberSolver"/> (checked again by the validator).</summary>
        public int minMoves;
        /// <summary>Pipe cells between source and sink in the optimal solution.</summary>
        public int path;
        public string cells;

        public PipeGrid CreateGrid() => PlumberLevelCodec.Parse(cells);
    }

    /// <summary>The JSON file of the Story levels: Assets/Resources/Games/Plumber/plumber_story.json.</summary>
    [Serializable]
    public sealed class PlumberLevelSet
    {
        public List<PlumberLevel> levels = new List<PlumberLevel>();
    }
}
