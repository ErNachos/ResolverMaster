using System;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>
    /// Every position reachable from a start and how many moves each one is from the end (TJ-001, exact, pure logic).
    /// 1. Breadth-first search forward from the start (a move = one vehicle slid any number of cells, or a target car out through its exit);
    ///    solved positions are not expanded (nothing moves after the win). Stops when more than the cap of positions are found.
    /// 2. Breadth-first search backwards from every solved position: the distance table (moves left to the end) of every reachable position.
    ///    Slides can always be slid back, so a position's slide neighbours are also its predecessors; a position where a target car is out
    ///    comes from every position where that car stands on its line with a free way to its exit.
    /// Every slide can be undone, so from any reachable position the end stays reachable when the start can reach it.
    /// </summary>
    public sealed class TrafficSpace
    {
        public const int DefaultMaxStates = 2000000;

        readonly TrafficLayout _layout;
        readonly StateIndex _index;
        int[] _dist;

        public TrafficLayout Layout => _layout;
        public ulong Start { get; }
        /// <summary>Reachable positions found (all of them unless <see cref="Capped"/>).</summary>
        public int Count => _index.Count;
        /// <summary>More positions than the cap: the search stopped, the distances are not computed (the generator discards such levels).</summary>
        public bool Capped { get; private set; }
        /// <summary>Minimum moves from the start; -1 when the end cannot be reached (or the search was capped).</summary>
        public int MinMoves => Distance(Start);
        public bool Solvable => MinMoves >= 0;
        /// <summary>The highest distance of a reachable position.</summary>
        public int MaxDistance { get; private set; } = -1;

        TrafficSpace(TrafficLayout layout, ulong start)
        {
            _layout = layout;
            Start = start;
            _index = new StateIndex(4096);
        }

        /// <summary>Explores every position reachable from <paramref name="start"/> (at most <paramref name="maxStates"/>) and their distances to the end.</summary>
        public static TrafficSpace Explore(TrafficLayout layout, ulong start, int maxStates = DefaultMaxStates)
        {
            var space = new TrafficSpace(layout, start);
            space.Forward(maxStates);
            if (!space.Capped) space.Backward();
            return space;
        }

        /// <summary>Moves left from <paramref name="state"/> to the end; -1 when not reachable from the start or no way out.</summary>
        public int Distance(ulong state)
        {
            if (_dist == null) return -1;
            int i = _index.IndexOf(state);
            return i < 0 ? -1 : _dist[i];
        }

        /// <summary>Reachable position number <paramref name="i"/> (0..Count-1, in the order the forward search found them).</summary>
        public ulong StateAt(int i) => _index.KeyAt(i);

        public int DistanceAt(int i) => _dist == null ? -1 : _dist[i];

        /// <summary>
        /// A move that brings <paramref name="state"/> one move closer to the end (the light bulb). Several good moves: always the first in
        /// the fixed order of <see cref="TrafficLayout.Moves"/> (vehicles in order, positions ascending, the exit last). False when solved,
        /// unknown or without a way out.
        /// </summary>
        public bool TryNextMove(ulong state, out TrafficMove move)
        {
            move = default;
            int d = Distance(state);
            if (d <= 0) return false;
            var buffer = new TrafficMove[_layout.MaxMovesPerState];
            int n = _layout.Moves(state, buffer);
            for (int k = 0; k < n; k++)
            {
                if (Distance(_layout.Apply(state, buffer[k])) != d - 1) continue;
                move = buffer[k];
                return true;
            }
            return false;
        }

        /// <summary>Up to this many positions the forward search keeps every slide and exit it finds, so the backward search reuses them.</summary>
        const int RecordLimit = 300000;

        // the moves found by the forward search (while recording): slides of position i in _adj[_adjStart[i].._adjStart[i+1]];
        // exits as pairs (from -> to)
        int[] _adj = new int[1024], _adjStart = new int[1024], _exitFrom = new int[64], _exitTo = new int[64];
        int _adjCount, _exitCount;
        bool _recorded;

        void Forward(int maxStates)
        {
            var buffer = new TrafficMove[_layout.MaxMovesPerState];
            _index.Add(Start, out _);
            bool record = true;
            for (int i = 0; i < _index.Count; i++)
            {
                ulong s = _index.KeyAt(i);
                if (record)
                {
                    if (i + 1 >= _adjStart.Length) Array.Resize(ref _adjStart, _adjStart.Length * 2);
                    _adjStart[i] = _adjCount;
                }
                int n = _layout.Moves(s, buffer);   // none from a solved position
                for (int k = 0; k < n; k++)
                {
                    int j = _index.Add(_layout.Apply(s, buffer[k]), out bool added);
                    if (added && _index.Count > maxStates)
                    {
                        Capped = true;
                        return;
                    }
                    if (!record) continue;
                    if (buffer[k].IsExit)
                    {
                        if (_exitCount == _exitFrom.Length)
                        {
                            Array.Resize(ref _exitFrom, _exitCount * 2);
                            Array.Resize(ref _exitTo, _exitCount * 2);
                        }
                        _exitFrom[_exitCount] = i;
                        _exitTo[_exitCount++] = j;
                    }
                    else
                    {
                        if (_adjCount == _adj.Length) Array.Resize(ref _adj, _adjCount * 2);
                        _adj[_adjCount++] = j;
                    }
                }
                if (record && _index.Count > RecordLimit)
                {
                    record = false;   // too big to keep: the backward search generates the moves again
                    _adj = _adjStart = _exitFrom = _exitTo = null;
                }
            }
            if (record)
            {
                _adjStart[_index.Count] = _adjCount;
                _recorded = true;
            }
        }

        void Backward()
        {
            int count = _index.Count;
            _dist = new int[count];
            var queue = new int[count];
            int head = 0, tail = 0;
            for (int i = 0; i < count; i++)
            {
                if (_layout.IsSolved(_index.KeyAt(i)))
                {
                    _dist[i] = 0;
                    queue[tail++] = i;
                }
                else _dist[i] = -1;
            }
            if (_recorded) BackwardRecorded(queue, head, tail);
            else BackwardGenerated(queue, head, tail);
            _adj = _adjStart = _exitFrom = _exitTo = null;
        }

        /// <summary>Backward search over the moves kept by the forward search (slides are symmetric; exits reversed).</summary>
        void BackwardRecorded(int[] queue, int head, int tail)
        {
            var exitHead = new int[_index.Count];
            for (int i = 0; i < exitHead.Length; i++) exitHead[i] = -1;
            var exitNext = new int[_exitCount];
            for (int e = 0; e < _exitCount; e++)
            {
                exitNext[e] = exitHead[_exitTo[e]];
                exitHead[_exitTo[e]] = e;
            }
            while (head < tail)
            {
                int i = queue[head++];
                int d = _dist[i];
                if (d > MaxDistance) MaxDistance = d;
                for (int e = _adjStart[i]; e < _adjStart[i + 1]; e++)
                {
                    int j = _adj[e];
                    if (_dist[j] >= 0) continue;
                    _dist[j] = d + 1;
                    queue[tail++] = j;
                }
                for (int e = exitHead[i]; e >= 0; e = exitNext[e])
                {
                    int j = _exitFrom[e];
                    if (_dist[j] >= 0) continue;
                    _dist[j] = d + 1;
                    queue[tail++] = j;
                }
            }
        }

        /// <summary>Backward search generating the predecessors again (big spaces, nothing kept).</summary>
        void BackwardGenerated(int[] queue, int head, int tail)
        {
            var buffer = new TrafficMove[_layout.MaxMovesPerState];
            var vehicles = _layout.Vehicles;
            while (head < tail)
            {
                int i = queue[head++];
                ulong s = _index.KeyAt(i);
                int d = _dist[i];
                if (d > MaxDistance) MaxDistance = d;
                // slides: their reverse is a slide too (a solved position is only reached by an exit: no slides into it)
                if (!_layout.IsSolved(s))
                {
                    int n = _layout.Moves(s, buffer, exits: false);
                    for (int k = 0; k < n; k++) Visit(_layout.Apply(s, buffer[k]), d, queue, ref tail);
                }
                // exits: the target car back on any cell of its line with a free way to its exit
                ulong occ = _layout.Occupancy(s);
                for (int t = 0; t < _layout.TargetCount; t++)
                {
                    if (_layout.Pos(s, t) != TrafficLayout.Exited) continue;
                    int max = _layout.MaxPos(t);
                    if (vehicles[t].ExitsLow)
                        for (int q = 0; q <= max && (_layout.Mask(t, q) & occ) == 0; q++) Visit(_layout.With(s, t, q), d, queue, ref tail);
                    else
                        for (int q = max; q >= 0 && (_layout.Mask(t, q) & occ) == 0; q--) Visit(_layout.With(s, t, q), d, queue, ref tail);
                }
            }
        }

        void Visit(ulong state, int d, int[] queue, ref int tail)
        {
            int j = _index.IndexOf(state);
            if (j < 0 || _dist[j] >= 0) return;
            _dist[j] = d + 1;
            queue[tail++] = j;
        }
    }
}
