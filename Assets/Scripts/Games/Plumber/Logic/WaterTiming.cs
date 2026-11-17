using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// One stretch of pipe inside a cell that the water runs along (ErNa-005, continuous water):
    /// an arm from the centre of the cell to one of its openings (straight, T, cross, source, sink: two arms make a straight line),
    /// or the quarter arc of a curve between its two openings. Both ends are nodes of the <see cref="WaterTiming"/> graph.
    /// </summary>
    public readonly struct WaterPart
    {
        public readonly int Cell;
        /// <summary>True: the quarter arc of a curve from opening <see cref="DirA"/> to <see cref="DirB"/>. False: an arm from the centre (end A) to opening <see cref="DirB"/>.</summary>
        public readonly bool Arc;
        /// <summary>Arc only: the first opening (-1 for an arm, whose end A is the centre).</summary>
        public readonly int DirA;
        public readonly int DirB;
        public readonly int NodeA, NodeB;
        /// <summary>In cells: an arm 0.5, a curve a quarter of a circle of radius 0.5 (pi/4).</summary>
        public readonly float Length;

        public WaterPart(int cell, bool arc, int dirA, int dirB, int nodeA, int nodeB, float length)
        {
            Cell = cell;
            Arc = arc;
            DirA = dirA;
            DirB = dirB;
            NodeA = nodeA;
            NodeB = nodeB;
            Length = length;
        }
    }

    /// <summary>
    /// Speed of the water front: constant (cells per second) after a short, gentle acceleration when the water is opened.
    /// Pure logic: the view asks how far the water has gone at a time, and when it reaches a distance.
    /// </summary>
    public readonly struct WaterMotion
    {
        /// <summary>Cells per second at full speed.</summary>
        public readonly float Speed;
        /// <summary>Seconds to reach full speed (0 = full speed at once).</summary>
        public readonly float AccelTime;
        /// <summary>Speed at the opening, as a fraction of <see cref="Speed"/>.</summary>
        public readonly float StartFraction;

        public WaterMotion(float speed, float accelTime, float startFraction)
        {
            Speed = Math.Max(0.01f, speed);
            AccelTime = Math.Max(0f, accelTime);
            StartFraction = Math.Min(1f, Math.Max(0.01f, startFraction));
        }

        /// <summary>Distance covered (cells along the pipes) <paramref name="t"/> seconds after the opening.</summary>
        public float DistanceAt(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= AccelTime) return Speed * (AccelTime * (1f + StartFraction) / 2f + (t - AccelTime));
            return Speed * (StartFraction * t + (1f - StartFraction) * t * t / (2f * AccelTime));
        }

        /// <summary>Seconds after the opening at which the water has covered <paramref name="distance"/> cells.</summary>
        public float TimeAt(float distance)
        {
            if (distance <= 0f) return 0f;
            float accelDistance = Speed * AccelTime * (1f + StartFraction) / 2f;
            if (distance >= accelDistance) return AccelTime + (distance - accelDistance) / Speed;
            float a = (1f - StartFraction) / (2f * AccelTime), b = StartFraction, c = -distance / Speed;
            return (float)((-b + Math.Sqrt(b * b - 4 * a * c)) / (2 * a));
        }
    }

    /// <summary>
    /// When the water reaches every point of the pipes (ErNa-005): the pipes become a graph whose nodes are the centres of the cells and
    /// the middles of their open sides (shared by two cells whose openings face each other), linked by <see cref="WaterPart"/>s.
    /// The water starts from the centre of every source at the same time and flows at the same speed everywhere, so the distance from
    /// the nearest source (cells along the pipes) is the arrival time in "cells": after a T both branches are reached together.
    /// The rules (success, problems) stay those of <see cref="WaterFlow"/>; this class only adds where along the pipes each problem
    /// happens, so the animation can run smoothly up to it. Pure logic, tested in EditMode.
    /// </summary>
    public sealed class WaterTiming
    {
        public const float ArmLength = 0.5f;
        public static readonly float ArcLength = (float)(Math.PI / 4.0);

        const float Unreached = float.PositiveInfinity;

        readonly int _cells;
        readonly int[] _port;              // (cell * 4 + dir) -> node id, -1 when the side is closed
        readonly float[] _dist;
        readonly int[] _color;
        readonly List<WaterPart>[] _parts;
        readonly float[] _problemDistance;

        /// <summary>Distance at which the last reached point gets wet.</summary>
        public float EndDistance { get; private set; }
        /// <summary>Index in <see cref="WaterState.Problems"/> of the problem that happens first along the pipes (-1 = success).</summary>
        public int FirstProblemIndex { get; private set; } = -1;
        /// <summary>The animation stops here: the end of the water on success, the first problem otherwise.</summary>
        public float StopDistance { get; private set; }

        WaterTiming(int cells)
        {
            _cells = cells;
            _port = new int[cells * 4];
            _dist = new float[cells * 5];
            _color = new int[cells * 5];
            _parts = new List<WaterPart>[cells];
            for (int i = 0; i < _parts.Length; i++) _parts[i] = new List<WaterPart>(4);
            _problemDistance = Array.Empty<float>();
        }

        WaterTiming(int cells, int problems) : this(cells) => _problemDistance = new float[problems];

        /// <summary>The node in the middle of a cell (pieces with a centre: every kind but the curve and the empty cell).</summary>
        public int CenterNode(int cell) => cell;

        /// <summary>The node of an open side (shared with the neighbour when the openings face each other), -1 for a closed side.</summary>
        public int PortNode(int cell, int dir) => _port[cell * 4 + dir];

        /// <summary>Distance (cells along the pipes) at which the water reaches the node; +infinity if it never does.</summary>
        public float NodeDistance(int node) => node < 0 ? Unreached : _dist[node];

        /// <summary>Water color reaching the node first (-1 if never reached).</summary>
        public int NodeColor(int node) => node < 0 ? -1 : _color[node];

        public bool Reached(int node) => node >= 0 && !float.IsInfinity(_dist[node]);

        public IReadOnlyList<WaterPart> Parts(int cell) => _parts[cell];

        /// <summary>When the water first enters the cell (+infinity if never).</summary>
        public float CellArrival(int cell)
        {
            float best = Unreached;
            foreach (var p in _parts[cell]) best = Math.Min(best, Math.Min(_dist[p.NodeA], _dist[p.NodeB]));
            return best;
        }

        /// <summary>Distance along the pipes at which problem <paramref name="index"/> of the <see cref="WaterState"/> happens.</summary>
        public float ProblemDistance(int index) => _problemDistance[index];

        public static WaterTiming Compute(PipeGrid grid, WaterState state)
        {
            var topo = grid.Topology;
            int n = grid.CellCount;
            var t = new WaterTiming(n, state.Problems.Count);
            for (int i = 0; i < t._dist.Length; i++)
            {
                t._dist[i] = Unreached;
                t._color[i] = -1;
            }

            // ports: one node per open side; two facing openings share the node of the lower index
            for (int c = 0; c < n; c++)
            {
                int open = grid.Openings(c);
                for (int d = 0; d < 4; d++)
                {
                    t._port[c * 4 + d] = -1;
                    if ((open & (1 << d)) == 0) continue;
                    t._port[c * 4 + d] = n + c * 4 + d;
                    int nb = topo.Neighbor(c, d);
                    int back = topo.Opposite(d);
                    if (nb >= 0 && nb < c && (grid.Openings(nb) & (1 << back)) != 0) t._port[c * 4 + d] = t._port[nb * 4 + back];
                }
            }

            // parts: arms from the centre, or the arc of a curve
            for (int c = 0; c < n; c++)
            {
                var kind = grid.Pieces[c].Kind;
                if (kind == PieceKind.Empty) continue;
                int open = grid.Openings(c);
                if (kind == PieceKind.Curve)
                {
                    int a = -1, b = -1;
                    for (int d = 0; d < 4; d++)
                        if ((open & (1 << d)) != 0)
                        {
                            if (a < 0) a = d;
                            else b = d;
                        }
                    if (a >= 0 && b >= 0) t._parts[c].Add(new WaterPart(c, true, a, b, t._port[c * 4 + a], t._port[c * 4 + b], ArcLength));
                    continue;
                }
                for (int d = 0; d < 4; d++)
                    if ((open & (1 << d)) != 0) t._parts[c].Add(new WaterPart(c, false, -1, d, c, t._port[c * 4 + d], ArmLength));
            }

            // the nearest source of every node (all sources start together, same speed everywhere)
            var adjacency = new List<(int to, float len)>[t._dist.Length];
            for (int c = 0; c < n; c++)
                foreach (var p in t._parts[c])
                {
                    (adjacency[p.NodeA] ??= new List<(int, float)>()).Add((p.NodeB, p.Length));
                    (adjacency[p.NodeB] ??= new List<(int, float)>()).Add((p.NodeA, p.Length));
                }
            var done = new bool[t._dist.Length];
            for (int c = 0; c < n; c++)
                if (grid.Pieces[c].Kind == PieceKind.Source)
                {
                    t._dist[c] = 0f;
                    t._color[c] = grid.Pieces[c].Color;
                }
            while (true)
            {
                int u = -1;
                for (int i = 0; i < t._dist.Length; i++)
                    if (!done[i] && !float.IsInfinity(t._dist[i]) && (u < 0 || t._dist[i] < t._dist[u] || (t._dist[i] == t._dist[u] && t._color[i] < t._color[u]))) u = i;
                if (u < 0) break;
                done[u] = true;
                t.EndDistance = Math.Max(t.EndDistance, t._dist[u]);
                if (adjacency[u] == null) continue;
                foreach (var (to, len) in adjacency[u])
                {
                    float d = t._dist[u] + len;
                    if (d < t._dist[to] - 1e-6f)
                    {
                        t._dist[to] = d;
                        t._color[to] = t._color[u];
                    }
                }
            }

            // where along the pipes each problem of the rules happens
            float first = Unreached;
            for (int i = 0; i < state.Problems.Count; i++)
            {
                var pr = state.Problems[i];
                float at;
                switch (pr.Kind)
                {
                    case WaterProblemKind.Leak:
                        at = t.NodeDistance(t.PortNode(pr.Cell, pr.Direction));
                        break;
                    case WaterProblemKind.ColorMix:
                        at = t.MeetingDistance(pr.Cell, pr.OtherCell);
                        break;
                    case WaterProblemKind.WrongColor:
                        at = t.NodeDistance(t.CenterNode(pr.Cell));
                        break;
                    default:
                        at = t.EndDistance;
                        break;
                }
                if (float.IsInfinity(at)) at = t.EndDistance;
                t._problemDistance[i] = at;
                if (at < first - 1e-5f)
                {
                    first = at;
                    t.FirstProblemIndex = i;
                }
            }
            t.StopDistance = t.FirstProblemIndex >= 0 ? first : t.EndDistance;
            return t;
        }

        /// <summary>Where two colors meet on the parts of two neighbouring cells: halfway between the fronts on the part they share.</summary>
        float MeetingDistance(int cellA, int cellB)
        {
            float best = Unreached;
            foreach (int cell in new[] { cellA, cellB })
            {
                if (cell < 0) continue;
                foreach (var p in _parts[cell])
                {
                    if (!Reached(p.NodeA) || !Reached(p.NodeB) || _color[p.NodeA] == _color[p.NodeB]) continue;
                    best = Math.Min(best, (_dist[p.NodeA] + _dist[p.NodeB] + p.Length) / 2f);
                }
            }
            if (float.IsInfinity(best)) best = Math.Max(CellArrival(cellA), cellB >= 0 ? CellArrival(cellB) : 0f);
            return best;
        }
    }
}
