using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>
    /// The Traffic jam rules on the Core puzzle contract (TJ-001). A move = one vehicle slid along its line by any number of cells, or a
    /// target car pushed out through its exit; a vehicle put back where it was is no move (refused). Solved = every target car is out.
    /// The light bulb (<see cref="ApplyHint"/>): from the distance table of the level (built once, the first time) the first move that
    /// brings the position one move closer to the end; it counts as 1 move (ErNa-011), can be undone, and the level gives at most 2 stars.
    /// </summary>
    public sealed class TrafficPuzzle : PuzzleBase<TrafficLevel, TrafficMove>
    {
        TrafficLevel _parsedOf;
        ulong _start;
        TrafficSpace _space;

        public TrafficLayout Layout { get; private set; }
        /// <summary>The current position (packed, <see cref="TrafficLayout"/>).</summary>
        public ulong State { get; private set; }
        public override int MinMoves => Level?.minMoves ?? 0;
        /// <summary>Cap of the hint's distance table.</summary>
        public int MaxStates { get; set; } = TrafficSpace.DefaultMaxStates;

        public int Pos(int vehicle) => Layout.Pos(State, vehicle);

        public bool IsOut(int vehicle) => Pos(vehicle) == TrafficLayout.Exited;

        /// <summary>Where a vehicle can be dragged now (its own position included) and whether it can leave through its exit.</summary>
        public void Range(int vehicle, out int lo, out int hi, out bool canExit) => Layout.Range(State, vehicle, out lo, out hi, out canExit);

        /// <summary>The level's distance table (computed the first time it is needed, then kept for this level).</summary>
        public TrafficSpace Space => _space ??= TrafficSpace.Explore(Layout, _start, MaxStates);

        /// <summary>Moves left from the current position (exact), -1 if unknown.</summary>
        public int MovesLeft => Space.Distance(State);

        /// <summary>The move the light bulb would make now; false when solved.</summary>
        public bool TryFindHint(out TrafficMove move)
        {
            move = default;
            return Layout != null && !IsSolved && Space.TryNextMove(State, out move);
        }

        /// <summary>Makes the hinted move (ErNa-011: it counts 1 move, like the player's). False when there is none.</summary>
        public bool ApplyHint(out TrafficMove move) => TryFindHint(out move) && ApplyHintMove(move, 1);

        protected override void OnLoad(TrafficLevel level)
        {
            if (!ReferenceEquals(_parsedOf, level) || Layout == null)
            {
                Layout = level.CreateLayout(out _start);
                _parsedOf = level;
                _space = null;
            }
            State = _start;
        }

        protected override bool TryApply(TrafficMove move)
        {
            if (Layout == null || !Layout.IsLegal(State, move)) return false;
            State = Layout.Apply(State, move);
            return true;
        }

        protected override void Revert(TrafficMove move) => State = Layout.With(State, move.Vehicle, move.From);

        protected override bool CheckSolved() => Layout != null && Layout.IsSolved(State);

        /// <summary>One character per vehicle: its position (0-8) or 'x' when out (Challenge save).</summary>
        public string CaptureState()
        {
            if (Layout == null) return "";
            var chars = new char[Layout.VehicleCount];
            for (int i = 0; i < chars.Length; i++)
            {
                int p = Pos(i);
                chars[i] = p == TrafficLayout.Exited ? 'x' : (char)('0' + p);
            }
            return new string(chars);
        }

        /// <summary>The moves on the undo stack as integers (Challenge save).</summary>
        public List<int> EncodedHistory()
        {
            var list = new List<int>();
            foreach (var m in History) list.Add(m.Encode());
            return list;
        }

        /// <summary>Replays saved moves from the start (resume). False if one does not fit the level.</summary>
        public bool ReplayEncoded(IReadOnlyList<int> history)
        {
            var moves = new List<TrafficMove>();
            foreach (int code in history) moves.Add(TrafficMove.Decode(code));
            return Replay(moves);
        }
    }
}
