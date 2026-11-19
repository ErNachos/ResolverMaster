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
    /// Shared bookkeeping of <see cref="IPuzzle{TLevel,TMove}"/>: undo stack, moves counter (= what the moves on the stack are worth,
    /// light bulb included, ErNa-011), solved flag, hint used.
    /// A game only implements how to load, apply, revert and check its own rules.
    /// </summary>
    public abstract class PuzzleBase<TLevel, TMove> : IPuzzle<TLevel, TMove>
    {
        readonly UndoStack<TMove> _undo = new UndoStack<TMove>();
        /// <summary>ErNa-011: what each move on the undo stack counts (1 for a player move, the hint's value for a light bulb move).</summary>
        readonly UndoStack<int> _cost = new UndoStack<int>();
        int _stackMoves;
        /// <summary>ErNa-011: moves of hints that are not on the undo stack (e.g. a pipe fixed in place); they survive restarts.</summary>
        int _keptHintMoves;

        public TLevel Level { get; private set; }
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// ErNa-011: every move counts, the light bulb's too (as many as the hint really did). Once solved with a hint the total is
        /// never below the minimum (e.g. a fixed pipe kept after a restart).
        /// </summary>
        public int MovesMade => IsSolved && HintUsed ? System.Math.Max(CountedMoves, MinMoves) : CountedMoves;
        /// <summary>The moves counted so far, without the floor of <see cref="MovesMade"/>.</summary>
        public int CountedMoves => _stackMoves + _keptHintMoves;
        public abstract int MinMoves { get; }
        public bool IsSolved { get; private set; }
        public bool CanUndo => _undo.Count > 0;
        /// <summary>The light bulb was used since the level was loaded (at most <see cref="StarRules.maxStarsWithHint"/> stars; survives restarts).</summary>
        public bool HintUsed { get; private set; }

        /// <summary>ErNa-007, Challenge: moves are refused once this many are made (0 = no limit). Undo still works.</summary>
        public int MoveLimit { get; set; }

        /// <summary>A move worth <paramref name="moves"/> still fits under <see cref="MoveLimit"/>.</summary>
        public bool Fits(int moves) => MoveLimit <= 0 || CountedMoves + moves <= MoveLimit;

        public void Load(TLevel level)
        {
            Level = level;
            IsLoaded = true;
            ClearUndo();
            _keptHintMoves = 0;
            HintUsed = false;
            OnFreshLoad();
            OnLoad(level);
            IsSolved = CheckSolved();
        }

        /// <summary>Moves are refused once the puzzle is solved (the win panel takes over).</summary>
        public bool ApplyMove(TMove move)
        {
            if (!IsLoaded || IsSolved || !Fits(1) || !TryApply(move)) return false;
            Push(move, 1);
            return true;
        }

        /// <summary>
        /// ErNa-011: a move made by the light bulb. It counts like the player's moves (<paramref name="moves"/> of them, at least 1), goes
        /// on the undo stack (undoing it takes its moves back) and respects the move limit. Marks the hint used.
        /// </summary>
        protected bool ApplyHintMove(TMove move, int moves = 1)
        {
            moves = System.Math.Max(1, moves);
            if (!IsLoaded || IsSolved || !Fits(moves) || !TryApply(move)) return false;
            HintUsed = true;
            Push(move, moves);
            return true;
        }

        /// <summary>
        /// ErNa-011: a light bulb change that is not on the undo stack (e.g. a pipe turned into place and fixed): its
        /// <paramref name="moves"/> count and stay counted after a restart (the change stays too). Marks the hint used.
        /// </summary>
        protected void CountHintMoves(int moves)
        {
            _keptHintMoves += System.Math.Max(0, moves);
            HintUsed = true;
        }

        /// <summary>A light bulb that only undoes (PG-001: back to a position with a solution): the undos work as usual.</summary>
        protected void MarkHintUsed() => HintUsed = true;

        void Push(TMove move, int moves)
        {
            _undo.Push(move);
            _cost.Push(moves);
            _stackMoves += moves;
            IsSolved = CheckSolved();
        }

        void ClearUndo()
        {
            _undo.Clear();
            _cost.Clear();
            _stackMoves = 0;
        }

        public bool Undo(out TMove move)
        {
            if (!_undo.TryPop(out move)) return false;
            if (_cost.TryPop(out int moves)) _stackMoves -= moves;
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

        /// <summary>Only on <see cref="Load"/>, not on <see cref="Restart"/>: forget what must survive restarts (e.g. pieces fixed by hints).</summary>
        protected virtual void OnFreshLoad() { }
        protected abstract bool TryApply(TMove move);
        protected abstract void Revert(TMove move);
        protected abstract bool CheckSolved();
    }
}
