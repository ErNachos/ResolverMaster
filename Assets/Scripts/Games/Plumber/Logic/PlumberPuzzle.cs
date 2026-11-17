using System.Collections.Generic;
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
        /// <summary>ErNa-007: pieces fixed by the light bulb (cell -> rotation); they survive restarts, not a new load.</summary>
        readonly Dictionary<int, int> _pins = new Dictionary<int, int>();
        int _version, _hintVersion = -1;
        int _hintCell = -1, _hintTaps;

        /// <summary>The current state (rotations change with every move).</summary>
        public PipeGrid Grid { get; private set; }

        /// <summary>Cells the player cannot turn (Core, ErNa-006): the locked pieces of the level, and (ErNa-007) the pieces fixed by a hint.</summary>
        public CellLockSet Locks { get; } = new CellLockSet();

        /// <summary>A hint was used since the level was loaded (at most 2 stars).</summary>
        public bool HintUsed { get; private set; }

        /// <summary>
        /// The next hint (ErNa-007): the solver looks for the fewest taps from the current state (locked and fixed pieces stay as they
        /// are); among the pieces it still has to turn, the one the water reaches first (the closest to a source). False when every
        /// piece is already right or nothing can be solved.
        /// </summary>
        public bool TryFindHint(out int cell, out int taps)
        {
            if (_hintVersion != _version)
            {
                _hintVersion = _version;
                _hintCell = -1;
                _hintTaps = 0;
                if (Grid != null && !IsSolved)
                {
                    var sol = PlumberSolver.Solve(Grid);
                    if (sol.Solvable && sol.MinMoves > 0)
                        foreach (int c in sol.PathCells)   // water order: the closest to a source first
                        {
                            int n = 0;
                            foreach (int t in sol.Taps)
                                if (t == c) n++;
                            if (n == 0) continue;
                            _hintCell = c;
                            _hintTaps = n;
                            break;
                        }
                }
            }
            cell = _hintCell;
            taps = _hintTaps;
            return cell >= 0;
        }

        /// <summary>Turns the hinted piece into place and fixes it (not a move, cannot be undone). False if there is no hint.</summary>
        public bool ApplyHint(out int cell, out int taps)
        {
            if (!TryFindHint(out cell, out taps)) return false;
            Grid.Rotate(cell, taps);
            Pin(cell, Grid.Pieces[cell].Rotation);
            HintUsed = true;
            _version++;
            return true;
        }

        void Pin(int cell, int rotation)
        {
            _pins[cell] = rotation;
            Grid.Pieces[cell].Rotation = rotation;
            Grid.Pieces[cell].Locked = true;   // the solver keeps it as it is
            Locks.Pin(cell);
        }

        public override int MinMoves => Level?.minMoves ?? 0;

        /// <summary>Opens the water from every source. Success marks the level solved (stars = moves made so far).</summary>
        public WaterState OpenWater()
        {
            _version++;
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
            Locks.ResetToLevel(Grid.CellCount, c => _start.Pieces[c].Locked);
            foreach (var pin in new List<KeyValuePair<int, int>>(_pins)) Pin(pin.Key, pin.Value);   // fixed pieces stay fixed after a restart
            _version++;
        }

        protected override void OnFreshLoad()
        {
            _pins.Clear();
            HintUsed = false;
        }

        protected override bool TryApply(int cell)
        {
            if (cell < 0 || cell >= Grid.CellCount || !Grid.Pieces[cell].IsRotatable || !Locks.CanChange(cell)) return false;
            Grid.Rotate(cell, 1);
            _version++;
            return true;
        }

        protected override void Revert(int cell)
        {
            _openedOk = false;   // the solved network changed
            _version++;
            if (Locks.IsPinned(cell)) return;   // fixed by a hint before this undo: the move goes away, the piece stays
            Grid.Rotate(cell, -1);
        }

        protected override bool CheckSolved() => _openedOk;
    }
}
