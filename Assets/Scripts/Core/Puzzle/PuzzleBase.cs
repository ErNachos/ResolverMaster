namespace ResolverMaster.Core.Puzzle
{
    /// <summary>What the common game screen needs to know about a running puzzle (moves counter, buttons, win).</summary>
    public interface IPuzzleSession
    {
        int MovesMade { get; }
        int MinMoves { get; }
        bool IsSolved { get; }
        bool CanUndo { get; }
    }

    /// <summary>The contract of every puzzle: load a level, apply a move, check the win, undo, restart.</summary>
    public interface IPuzzle<TLevel, TMove> : IPuzzleSession
    {
        void Load(TLevel level);
        /// <returns>False if the move is not allowed (nothing changes, no move counted).</returns>
        bool ApplyMove(TMove move);
        bool Undo(out TMove move);
        void Restart();
    }

    /// <summary>
    /// Shared bookkeeping of <see cref="IPuzzle{TLevel,TMove}"/>: undo stack, moves counter (= moves on the stack), solved flag.
    /// A game only implements how to load, apply, revert and check its own rules.
    /// </summary>
    public abstract class PuzzleBase<TLevel, TMove> : IPuzzle<TLevel, TMove>
    {
        readonly UndoStack<TMove> _undo = new UndoStack<TMove>();
        /// <summary>TJ-001: one flag per move on the undo stack, true = a free move (made by a hint: it does not count).</summary>
        readonly UndoStack<bool> _free = new UndoStack<bool>();
        int _freeCount;

        public TLevel Level { get; private set; }
        public bool IsLoaded { get; private set; }
        /// <summary>Moves on the undo stack, without the free ones (TJ-001: a move made by the light bulb does not count).</summary>
        public int MovesMade => _undo.Count - _freeCount;
        public abstract int MinMoves { get; }
        public bool IsSolved { get; private set; }
        public bool CanUndo => _undo.Count > 0;

        /// <summary>ErNa-007, Challenge: moves are refused once this many are made (0 = no limit). Undo still works.</summary>
        public int MoveLimit { get; set; }

        public void Load(TLevel level)
        {
            Level = level;
            IsLoaded = true;
            ClearUndo();
            OnFreshLoad();
            OnLoad(level);
            IsSolved = CheckSolved();
        }

        /// <summary>Moves are refused once the puzzle is solved (the win panel takes over).</summary>
        public bool ApplyMove(TMove move)
        {
            if (!IsLoaded || IsSolved || (MoveLimit > 0 && MovesMade >= MoveLimit) || !TryApply(move)) return false;
            _undo.Push(move);
            _free.Push(false);
            IsSolved = CheckSolved();
            return true;
        }

        /// <summary>
        /// TJ-001: a move that does not count (e.g. made by the light bulb): it goes on the undo stack, so undo stays consistent, but not
        /// in <see cref="MovesMade"/>; the move limit does not apply.
        /// </summary>
        protected bool ApplyFreeMove(TMove move)
        {
            if (!IsLoaded || IsSolved || !TryApply(move)) return false;
            _undo.Push(move);
            _free.Push(true);
            _freeCount++;
            IsSolved = CheckSolved();
            return true;
        }

        /// <summary>The move on top of the undo stack is a free one (<see cref="ApplyFreeMove"/>).</summary>
        public bool LastMoveIsFree => _free.TryPeek(out bool f) && f;

        void ClearUndo()
        {
            _undo.Clear();
            _free.Clear();
            _freeCount = 0;
        }

        public bool Undo(out TMove move)
        {
            if (!_undo.TryPop(out move)) return false;
            if (_free.TryPop(out bool free) && free) _freeCount--;
            Revert(move);
            IsSolved = CheckSolved();
            return true;
        }

        /// <summary>The moves on the undo stack, oldest first (Challenge save, ErNa-007).</summary>
        public System.Collections.Generic.IReadOnlyList<TMove> History => _undo.Items;

        /// <summary>Restarts and applies <paramref name="moves"/> in order (a saved play resumed). False if one is refused.</summary>
        public bool Replay(System.Collections.Generic.IEnumerable<TMove> moves)
        {
            Restart();
            foreach (var m in moves)
                if (!ApplyMove(m)) return false;
            return true;
        }

        public void Restart()
        {
            if (!IsLoaded) return;
            ClearUndo();
            OnLoad(Level);
            IsSolved = CheckSolved();
        }

        /// <summary>For games whose win comes from an action that is not a move (e.g. opening the water): re-reads <see cref="CheckSolved"/>.</summary>
        protected void RefreshSolved() => IsSolved = CheckSolved();

        /// <summary>Resets the state to the level start (also used by <see cref="Restart"/>).</summary>
        protected abstract void OnLoad(TLevel level);

        /// <summary>Only on <see cref="Load"/>, not on <see cref="Restart"/>: forget what must survive restarts (e.g. hints, ErNa-007).</summary>
        protected virtual void OnFreshLoad() { }
        protected abstract bool TryApply(TMove move);
        protected abstract void Revert(TMove move);
        protected abstract bool CheckSolved();
    }
}
