using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>What the light bulb did.</summary>
    public enum PenguinHint
    {
        None,
        /// <summary>The next right swipe was made (a free move: it does not count).</summary>
        Moved,
        /// <summary>The player was stuck: the moves were undone back to the last state with a solution.</summary>
        Undone,
    }

    /// <summary>
    /// The Pino rules on the Core puzzle contract (PG-001). A move = one swipe that changes something (an empty swipe is no move).
    /// A swipe that makes a penguin fall is a move that fails the level (<see cref="Fell"/>): the board shows the fall, then the level
    /// starts again (Story) or the run is lost (Challenge). Solved = every penguin in its igloo. Undo puts back the state before the
    /// swipe. The table of every reachable state (<see cref="Space"/>, built the first time it is needed) tells at once whether the
    /// player is stuck (<see cref="IsStuck"/>) and the next right swipe of the light bulb.
    /// </summary>
    public sealed class PenguinPuzzle : PuzzleBase<PenguinLevel, PenguinMove>
    {
        PenguinLevel _parsedOf;
        PenguinSimulator _sim;
        PenguinSpace _space;

        public PenguinLayout Layout { get; private set; }
        /// <summary>The current packed state (<see cref="PenguinLayout"/>).</summary>
        public ulong State { get; private set; }
        /// <summary>The last swipe made a penguin fall: no more moves until restart or undo.</summary>
        public bool Fell { get; private set; }
        public override int MinMoves => Level?.minMoves ?? 0;
        /// <summary>The light bulb was used since the level was loaded (at most 2 stars).</summary>
        public bool HintUsed { get; private set; }
        /// <summary>Cap of the table of states.</summary>
        public int MaxStates { get; set; } = PenguinSpace.DefaultMaxStates;

        /// <summary>The table of every state reachable from the start, with the distances to the end (computed once per level).</summary>
        public PenguinSpace Space => _space ??= PenguinSpace.Explore(Layout, Layout.Start, MaxStates);

        /// <summary>The swipe <paramref name="dir"/> from the current state, without applying it. False = an empty swipe (or nothing to play).</summary>
        public bool TryPlan(int dir, out PenguinMove move, PenguinTrace trace = null)
        {
            move = default;
            if (Layout == null || IsSolved || Fell || dir < 0 || dir > 3) return false;
            var r = _sim.Step(State, dir, out ulong next, trace);
            if (r == PenguinStepResult.None) return false;
            move = new PenguinMove(dir, State, next, r == PenguinStepResult.Fell);
            return true;
        }

        /// <summary>Plans and applies a swipe. False = no move (empty swipe, move limit, solved or failed).</summary>
        public bool Swipe(int dir, PenguinTrace trace = null) => TryPlan(dir, out var move, trace) && ApplyMove(move);

        /// <summary>No sequence of swipes reaches the end any more (shown as "You are stuck").</summary>
        public bool IsStuck => Layout != null && !IsSolved && !Fell && Space.IsStuck(State);

        /// <summary>Moves left from the current state (exact), -1 when stuck or unknown.</summary>
        public int MovesLeft => Fell ? -1 : Space.Distance(State);

        /// <summary>The swipe the light bulb would make now; false when solved, failed or stuck.</summary>
        public bool TryFindHint(out int dir)
        {
            dir = -1;
            return Layout != null && !IsSolved && !Fell && Space.TryNextMove(State, out dir);
        }

        /// <summary>
        /// The light bulb (after the rewarded ad): when stuck, undoes back to the last state with a solution (no undo of the Challenge is
        /// spent: the light bulb is Story only); otherwise makes the next right swipe as a free move. Either way the level gives at most 2 stars.
        /// </summary>
        public PenguinHint ApplyHint(out PenguinMove move, PenguinTrace trace = null)
        {
            move = default;
            if (Layout == null || IsSolved || Fell) return PenguinHint.None;
            if (IsStuck)
            {
                bool any = false;
                while (IsStuck && Undo(out _)) any = true;
                if (!any) return PenguinHint.None;
                HintUsed = true;
                return PenguinHint.Undone;
            }
            if (!Space.TryNextMove(State, out int dir) || !TryPlan(dir, out move, trace) || !ApplyFreeMove(move)) return PenguinHint.None;
            HintUsed = true;
            return PenguinHint.Moved;
        }

        protected override void OnFreshLoad() => HintUsed = false;

        protected override void OnLoad(PenguinLevel level)
        {
            if (!ReferenceEquals(_parsedOf, level) || Layout == null)
            {
                Layout = level.CreateLayout();
                _sim = new PenguinSimulator(Layout);
                _parsedOf = level;
                _space = null;
            }
            State = Layout.Start;
            Fell = false;
        }

        protected override bool TryApply(PenguinMove move)
        {
            if (Layout == null || Fell || move.Before != State) return false;
            State = move.After;
            Fell = move.Fell;
            return true;
        }

        protected override void Revert(PenguinMove move)
        {
            State = move.Before;
            Fell = false;
        }

        protected override bool CheckSolved() => Layout != null && !Fell && Layout.IsSolved(State);

        /// <summary>The packed state in hexadecimal (Challenge save; "!" after a fall).</summary>
        public string CaptureState() => Layout == null ? "" : State.ToString("X") + (Fell ? "!" : "");

        /// <summary>The swipes on the undo stack (Challenge save).</summary>
        public List<int> EncodedHistory()
        {
            var list = new List<int>();
            foreach (var m in History) list.Add(m.Encode());
            return list;
        }

        /// <summary>Replays saved swipes from the start (resume). False if one is not a move of the level.</summary>
        public bool ReplayEncoded(IReadOnlyList<int> history)
        {
            Restart();
            foreach (int dir in history)
                if (!TryPlan(dir, out var move) || !ApplyMove(move)) return false;
            return true;
        }
    }

    /// <summary>
    /// The timing of the play (PG-001), pure logic: a swipe is accepted only when everything is still (swipes during an animation are
    /// ignored); the slide of the accepted swipe lasts <see cref="Seconds"/> at <see cref="CellsPerSecond"/> (real time, so the same on
    /// every frame rate). The board draws the actors from <see cref="Clock"/> and <see cref="Trace"/>.
    /// </summary>
    public sealed class PenguinPlay
    {
        public PenguinPuzzle Puzzle { get; }
        /// <summary>The slides of the move being played.</summary>
        public PenguinTrace Trace { get; } = new PenguinTrace();
        public float CellsPerSecond = 7f;
        /// <summary>Extra time after the last slide (the bump, the fish, the igloo).</summary>
        public float Tail = 0.15f;
        /// <summary>A penguin sliding off the grid goes this far past the edge before it falls (cells).</summary>
        public float FallOffOvershoot = 0.55f;
        /// <summary>Seconds since the move started; -1 = nothing is moving.</summary>
        public float Clock { get; private set; } = -1f;
        public bool Busy => Clock >= 0f;
        /// <summary>The move being played (or the last one).</summary>
        public PenguinMove Move { get; private set; }

        public PenguinPlay(PenguinPuzzle puzzle) => Puzzle = puzzle;

        /// <summary>Length of the move being played (seconds).</summary>
        public float Seconds
        {
            get
            {
                float cells = Trace.Duration;
                foreach (var t in Trace.Tracks)
                    if (t.End == PenguinTrackEnd.FallOff) cells = System.Math.Max(cells, t.T1 + FallOffOvershoot);
                return cells / System.Math.Max(0.1f, CellsPerSecond) + Tail;
            }
        }

        /// <summary>A swipe of the player: ignored while something moves; false = no move (empty swipe, limit, solved, failed).</summary>
        public bool Swipe(int dir)
        {
            if (Busy) return false;
            if (!Puzzle.TryPlan(dir, out var move, Trace) || !Puzzle.ApplyMove(move))
            {
                Trace.Clear();
                return false;
            }
            Move = move;
            Clock = 0f;
            return true;
        }

        /// <summary>The light bulb: the hinted swipe is played like a swipe; an undo back from a stuck state is instant.</summary>
        public PenguinHint Hint()
        {
            if (Busy) return PenguinHint.None;
            var r = Puzzle.ApplyHint(out var move, Trace);
            if (r == PenguinHint.Moved)
            {
                Move = move;
                Clock = 0f;
            }
            else Trace.Clear();
            return r;
        }

        /// <summary>Advances the clock; true on the tick the move ends (then nothing moves).</summary>
        public bool Tick(float dt)
        {
            if (!Busy) return false;
            Clock += dt;
            if (Clock < Seconds) return false;
            Clock = -1f;
            return true;
        }

        /// <summary>Ends the move at once (previews, undo, restart).</summary>
        public void Stop() => Clock = -1f;

        /// <summary>The time unit of the trace reached at the current clock (cells slid by an actor moving since time 0).</summary>
        public float TraceTime => Clock < 0f ? float.MaxValue : Clock * CellsPerSecond;
    }
}
