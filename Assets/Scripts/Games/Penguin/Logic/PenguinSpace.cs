using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>
    /// The exact solver of Pino (PG-001): breadth-first search over every state reachable from the start (positions of the penguins,
    /// fish taken, positions of the blocks, holes filled, penguins already in), with the 4 swipes of each state; then the distance of every
    /// state to the end, by a backward search from the solved states. Moves that make a penguin fall are dead ends.
    /// <see cref="MinMoves"/> is exact. The same table answers, at once, "is there still a solution?" (<see cref="IsStuck"/>) and "which
    /// move now?" (<see cref="TryNextMove"/>, the light bulb). Capped at a number of states (parameter file).
    /// </summary>
    public sealed class PenguinSpace
    {
        public const int DefaultMaxStates = 150000;

        readonly StateIndex _index;
        int[] _succ;
        int[] _dist;
        byte[] _falls;

        public PenguinLayout Layout { get; }
        public int Count => _index.Count;
        /// <summary>More states than the cap: the search stopped (distances unknown).</summary>
        public bool Capped { get; private set; }
        /// <summary>Exact minimum moves from the start; -1 = no solution (or capped).</summary>
        public int MinMoves { get; private set; } = -1;
        public bool Solvable => MinMoves >= 0;
        /// <summary>Elements touched in some reachable state (only with <c>trackUse</c>).</summary>
        public PenguinUse Use { get; private set; }

        PenguinSpace(PenguinLayout layout, int capacity)
        {
            Layout = layout;
            _index = new StateIndex(capacity);
        }

        /// <param name="freezeBlocks">Blocks never move (generator check: is a push needed?).</param>
        /// <param name="noFill">Blocks never sink (generator check: is filling a hole needed?).</param>
        public static PenguinSpace Explore(PenguinLayout layout, ulong start, int maxStates = DefaultMaxStates, bool trackUse = false,
            bool freezeBlocks = false, bool noFill = false)
        {
            maxStates = Math.Max(16, maxStates);
            var space = new PenguinSpace(layout, Math.Min(maxStates, 4096));
            var sim = new PenguinSimulator(layout) { FreezeBlocks = freezeBlocks, NoFill = noFill };
            var use = trackUse ? new PenguinUse(layout) : null;
            space.Use = use;
            var succ = new List<int>(4096 * 4);
            var falls = new List<byte>(4096);
            space._index.Add(start, out _);
            for (int i = 0; i < space._index.Count; i++)
            {
                ulong s = space._index.KeyAt(i);
                byte fall = 0;
                if (layout.IsSolved(s))
                {
                    for (int d = 0; d < 4; d++) succ.Add(-1);
                    falls.Add(0);
                    continue;
                }
                for (int d = 0; d < 4; d++)
                {
                    var r = sim.Step(s, d, out ulong next, null, use);
                    if (r == PenguinStepResult.Moved)
                    {
                        int j = space._index.IndexOf(next);
                        if (j < 0)
                        {
                            if (space._index.Count >= maxStates)
                            {
                                space.Capped = true;
                                space._succ = null;
                                return space;
                            }
                            j = space._index.Add(next, out _);
                        }
                        succ.Add(j);
                    }
                    else
                    {
                        succ.Add(-1);
                        if (r == PenguinStepResult.Fell) fall |= (byte)(1 << d);
                    }
                }
                falls.Add(fall);
            }
            space._succ = succ.ToArray();
            space._falls = falls.ToArray();
            space.ComputeDistances();
            return space;
        }

        /// <summary>Backward breadth-first search from every solved state over the reversed moves.</summary>
        void ComputeDistances()
        {
            int n = _index.Count;
            var start = new int[n + 1];
            for (int k = 0; k < _succ.Length; k++)
                if (_succ[k] >= 0) start[_succ[k] + 1]++;
            for (int i = 0; i < n; i++) start[i + 1] += start[i];
            var fill = (int[])start.Clone();
            var preds = new int[start[n]];
            for (int k = 0; k < _succ.Length; k++)
                if (_succ[k] >= 0) preds[fill[_succ[k]]++] = k >> 2;
            _dist = new int[n];
            var queue = new int[n];
            int head = 0, tail = 0;
            for (int i = 0; i < n; i++)
            {
                if (Layout.IsSolved(_index.KeyAt(i)))
                {
                    _dist[i] = 0;
                    queue[tail++] = i;
                }
                else _dist[i] = -1;
            }
            while (head < tail)
            {
                int j = queue[head++];
                for (int k = start[j]; k < start[j + 1]; k++)
                {
                    int p = preds[k];
                    if (_dist[p] >= 0) continue;
                    _dist[p] = _dist[j] + 1;
                    queue[tail++] = p;
                }
            }
            MinMoves = _dist[0];
        }

        /// <summary>Moves left from a state (exact); -1 = no way to the end from there, or a state not reachable from the start.</summary>
        public int Distance(ulong state)
        {
            if (_dist == null) return -1;
            int i = _index.IndexOf(state);
            return i < 0 ? -1 : _dist[i];
        }

        /// <summary>A state reachable from the start, not solved, from which no sequence of moves reaches the end.</summary>
        public bool IsStuck(ulong state)
        {
            if (_dist == null || Layout.IsSolved(state)) return false;
            int i = _index.IndexOf(state);
            return i >= 0 && _dist[i] < 0;
        }

        /// <summary>The first swipe (up, right, down, left order) that brings the state one move closer to the end.</summary>
        public bool TryNextMove(ulong state, out int dir)
        {
            dir = -1;
            if (_dist == null) return false;
            int i = _index.IndexOf(state);
            if (i < 0 || _dist[i] <= 0) return false;
            for (int d = 0; d < 4; d++)
            {
                int j = _succ[i * 4 + d];
                if (j >= 0 && _dist[j] == _dist[i] - 1)
                {
                    dir = d;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Swipes from a state that make a penguin fall (bit per direction).</summary>
        public int FallMask(ulong state)
        {
            if (_falls == null) return 0;
            int i = _index.IndexOf(state);
            return i < 0 ? 0 : _falls[i];
        }

        /// <summary>Optimal swipes of one way to the end from <paramref name="state"/> (empty when there is none).</summary>
        public List<int> Solution(ulong state)
        {
            var dirs = new List<int>();
            if (_dist == null) return dirs;
            int i = _index.IndexOf(state);
            if (i < 0 || _dist[i] < 0) return dirs;
            while (_dist[i] > 0)
            {
                for (int d = 0; d < 4; d++)
                {
                    int j = _succ[i * 4 + d];
                    if (j < 0 || _dist[j] != _dist[i] - 1) continue;
                    dirs.Add(d);
                    i = j;
                    break;
                }
            }
            return dirs;
        }

        /// <summary>The shortest swipes from the start to a state with no solution left ("You are stuck"); empty when there is none.</summary>
        public List<int> PathToStuck()
        {
            var dirs = new List<int>();
            if (_dist == null || Count == 0) return dirs;
            var parent = new int[Count];
            var how = new byte[Count];
            for (int i = 0; i < Count; i++) parent[i] = -1;
            parent[0] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                if (_dist[i] < 0 && !Layout.IsSolved(_index.KeyAt(i)))
                {
                    for (int k = i; k != 0; k = parent[k]) dirs.Add(how[k]);
                    dirs.Reverse();
                    return dirs;
                }
                for (int d = 0; d < 4; d++)
                {
                    int j = _succ[i * 4 + d];
                    if (j < 0 || parent[j] >= 0) continue;
                    parent[j] = i;
                    how[j] = (byte)d;
                    queue.Enqueue(j);
                }
            }
            return dirs;
        }

        /// <summary>Number of different first swipes that start an optimal way to the end.</summary>
        public int OptimalFirstMoves(ulong state)
        {
            if (_dist == null) return 0;
            int i = _index.IndexOf(state), k = 0;
            if (i < 0 || _dist[i] <= 0) return 0;
            for (int d = 0; d < 4; d++)
            {
                int j = _succ[i * 4 + d];
                if (j >= 0 && _dist[j] == _dist[i] - 1) k++;
            }
            return k;
        }
    }
}
