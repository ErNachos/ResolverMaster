using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// The plumber rules on the Core puzzle contract. A move = the index of the tapped cell (one 90-degree clockwise turn).
    /// Rotating never shows water: the level is solved only by <see cref="OpenWater"/> (not a move). A failed opening leaves the state
    /// as it is for the failure animation; the board then calls <see cref="PuzzleBase{TLevel,TMove}.Restart"/> (moves 0, undo empty).
    /// </summary>
    public sealed class PlumberPuzzle : PuzzleBase<PlumberLevel, int>
    {
        PipeGrid _start;
        PlumberLevel _startOf;
        bool _openedOk;

        /// <summary>The current state (rotations change with every move).</summary>
        public PipeGrid Grid { get; private set; }

        /// <summary>Cells the player cannot turn (Core, ErNa-006): the locked pieces of the level.</summary>
        public CellLockSet Locks { get; } = new CellLockSet();

        public override int MinMoves => Level?.minMoves ?? 0;

        /// <summary>Opens the water from every source. Success marks the level solved (stars = moves made so far).</summary>
        public WaterState OpenWater()
        {
            var water = WaterFlow.Compute(Grid);
            if (!IsSolved && water.Success)
            {
                _openedOk = true;
                RefreshSolved();
            }
            return water;
        }

        /// <summary>Parses the level once; Restart only copies the start state back.</summary>
        protected override void OnLoad(PlumberLevel level)
        {
            _openedOk = false;
            if (!ReferenceEquals(_startOf, level) || _start == null)
            {
                _start = level.CreateGrid();
                _startOf = level;
                Grid = _start.Clone();
            }
            else Grid.CopyFrom(_start);
            Locks.ResetToLevel(Grid.CellCount, c => Grid.Pieces[c].Locked);
        }

        protected override bool TryApply(int cell)
        {
            if (cell < 0 || cell >= Grid.CellCount || !Grid.Pieces[cell].IsRotatable || !Locks.CanChange(cell)) return false;
            Grid.Rotate(cell, 1);
            return true;
        }

        protected override void Revert(int cell)
        {
            _openedOk = false;   // the solved network changed
            Grid.Rotate(cell, -1);
        }

        protected override bool CheckSolved() => _openedOk;
    }
}
