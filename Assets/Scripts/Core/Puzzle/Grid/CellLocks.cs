using System;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>Why a grid element cannot be changed by the player (ErNa-006).</summary>
    public enum CellLock
    {
        /// <summary>Free: taps change it.</summary>
        None,
        /// <summary>Locked by the level design (drawn with a plate and bolts): part of the puzzle, also for the solver.</summary>
        Locked,
        /// <summary>Fixed during play by a hint (drawn with a glowing outline): stays fixed after a restart, cannot be undone.</summary>
        Pinned,
    }

    /// <summary>
    /// The locked elements of a grid, for every game (ErNa-006, GUIDELINES 1): which cells the player cannot change and why. A tap on
    /// a cell that cannot change is not a move: the board only shakes it (<see cref="ResolverMaster.Core.UI.ShakeMotion"/>).
    /// Level locks come from the level data; pins are added during play and survive <see cref="ResetToLevel"/>-style restarts
    /// when the game keeps them. Pure logic.
    /// </summary>
    public sealed class CellLockSet
    {
        CellLock[] _state = Array.Empty<CellLock>();

        public int Count => _state.Length;

        /// <summary>Starts from the level: <paramref name="lockedByLevel"/> tells which cells the level locks; no pins.</summary>
        public void ResetToLevel(int cells, Func<int, bool> lockedByLevel)
        {
            if (_state.Length != cells) _state = new CellLock[cells];
            for (int i = 0; i < cells; i++) _state[i] = lockedByLevel != null && lockedByLevel(i) ? CellLock.Locked : CellLock.None;
        }

        public CellLock Get(int cell) => cell >= 0 && cell < _state.Length ? _state[cell] : CellLock.None;

        public bool CanChange(int cell) => Get(cell) == CellLock.None;

        public bool IsPinned(int cell) => Get(cell) == CellLock.Pinned;

        /// <summary>Pins a free cell (a level lock stays a level lock). False if the cell was not free.</summary>
        public bool Pin(int cell)
        {
            if (Get(cell) != CellLock.None || cell < 0 || cell >= _state.Length) return false;
            _state[cell] = CellLock.Pinned;
            return true;
        }

        public int PinnedCount
        {
            get
            {
                int n = 0;
                foreach (var s in _state)
                    if (s == CellLock.Pinned) n++;
                return n;
            }
        }
    }
}
