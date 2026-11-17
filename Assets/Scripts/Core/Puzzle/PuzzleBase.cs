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

        public TLevel Level { get; private set; }
        public bool IsLoaded { get; private set; }
        public int MovesMade => _undo.Count;
        public abstract int MinMoves { get; }
        public bool IsSolved { get; private set; }
        public bool CanUndo => _undo.Count > 0;

        public void Load(TLevel level)
        {
            Level = level;
            IsLoaded = true;
            _undo.Clear();
            OnLoad(level);
            IsSolved = CheckSolved();
        }

        /// <summary>Moves are refused once the puzzle is solved (the win panel takes over).</summary>
        public bool ApplyMove(TMove move)
        {
            if (!IsLoaded || IsSolved || !TryApply(move)) return false;
            _undo.Push(move);
            IsSolved = CheckSolved();
            return true;
        }

        public bool Undo(out TMove move)
        {
            if (!_undo.TryPop(out move)) return false;
            Revert(move);
            IsSolved = CheckSolved();
            return true;
        }

        public void Restart()
        {
            if (!IsLoaded) return;
            _undo.Clear();
            OnLoad(Level);
            IsSolved = CheckSolved();
        }

        /// <summary>For games whose win comes from an action that is not a move (e.g. opening the water): re-reads <see cref="CheckSolved"/>.</summary>
        protected void RefreshSolved() => IsSolved = CheckSolved();

        /// <summary>Resets the state to the level start (also used by <see cref="Restart"/>).</summary>
        protected abstract void OnLoad(TLevel level);
        protected abstract bool TryApply(TMove move);
        protected abstract void Revert(TMove move);
        protected abstract bool CheckSolved();
    }
}
