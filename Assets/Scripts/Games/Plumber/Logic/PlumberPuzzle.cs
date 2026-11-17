using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>The plumber rules on the Core puzzle contract. A move = the index of the tapped cell (one 90-degree clockwise turn).</summary>
    public sealed class PlumberPuzzle : PuzzleBase<PlumberLevel, int>
    {
        PipeGrid _start;
        PlumberLevel _startOf;

        /// <summary>The current state (rotations change with every move).</summary>
        public PipeGrid Grid { get; private set; }

        public override int MinMoves => Level?.minMoves ?? 0;

        /// <summary>Parses the level once; Restart only copies the start state back.</summary>
        protected override void OnLoad(PlumberLevel level)
        {
            if (!ReferenceEquals(_startOf, level) || _start == null)
            {
                _start = level.CreateGrid();
                _startOf = level;
                Grid = _start.Clone();
            }
            else Grid.CopyFrom(_start);
        }

        protected override bool TryApply(int cell)
        {
            if (cell < 0 || cell >= Grid.CellCount || !Grid.Pieces[cell].IsRotatable) return false;
            Grid.Rotate(cell, 1);
            return true;
        }

        protected override void Revert(int cell) => Grid.Rotate(cell, -1);

        protected override bool CheckSolved() => WaterFlow.Compute(Grid).AllSinksReached;
    }
}
