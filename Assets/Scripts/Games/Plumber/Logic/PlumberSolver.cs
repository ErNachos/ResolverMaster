using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    public sealed class PlumberSolution
    {
        public bool Solvable;
        /// <summary>Minimum number of taps over every possible solution.</summary>
        public int MinMoves;
        /// <summary>Distinct pipe cells between source and sink in the optimal route found.</summary>
        public int PathLength;
        /// <summary>Why the level is not solvable / not supported (null when solvable).</summary>
        public string Reason;
        /// <summary>Pipe cells of the optimal route, from the source to the sink.</summary>
        public readonly List<int> PathCells = new List<int>();
        /// <summary>One entry per tap (a cell needing 3 taps appears 3 times), in route order.</summary>
        public readonly List<int> Taps = new List<int>();
        public int Expanded;
    }

    /// <summary>
    /// Exact minimum-taps solver.
    /// Why a route search is exact: leaks and unconnected pieces are allowed, so a solution only needs one water route from the source
    /// to the sink; every other piece can stay as it is (0 taps). The cheapest solution is therefore the cheapest route, where each cell
    /// on it costs the fewest clockwise taps that open both its entry and its exit side (symmetry included: a straight needs at most 1,
    /// a cross 0). A rotatable piece with several distinct orientations can be crossed only once (it has one orientation), while
    /// crosses and locked pieces have a single orientation and may be crossed again.
    /// Search: A* over (cell, entry side, cells used), with a heuristic from the same problem without the "used once" rule (admissible
    /// and consistent), so the first route reaching the sink is optimal. Deterministic: same grid, same route.
    /// Supports one source/sink pair (the multi-color rules come with their levels).
    /// </summary>
    public static class PlumberSolver
    {
        public const int MaxCells = 64;
        const int Inf = int.MaxValue / 4;

        struct Node
        {
            public int Cell;   // -1 = reached the sink
            public int Entry;
            public ulong Used;
            public int G;
            public int Parent;
            public int PassCost;   // taps spent on the parent's cell to come here
        }

        public static PlumberSolution Solve(PipeGrid grid, int maxExpansions = 2000000)
        {
            var result = new PlumberSolution();
            var topo = grid.Topology;
            int n = grid.CellCount;
            int dirs = topo.DirectionCount;
            if (n > MaxCells) return Fail(result, $"grid larger than {MaxCells} cells");

            var sources = grid.CellsOf(PieceKind.Source);
            var sinks = grid.CellsOf(PieceKind.Sink);
            if (sources.Count != 1 || sinks.Count != 1)
                return Fail(result, $"{sources.Count} sources and {sinks.Count} sinks: the solver supports exactly one pair for now");
            int src = sources[0], sink = sinks[0];
            if (grid.Pieces[src].Color != grid.Pieces[sink].Color) return Fail(result, "source and sink have different colors");
            int srcOut = SingleDirection(grid.Openings(src));
            int sinkIn = SingleDirection(grid.Openings(sink));

            // cheapest taps to cross each cell entering from side e and leaving from side x
            var pass = new int[n * dirs * dirs];
            var tracked = new bool[n];
            for (int i = 0; i < pass.Length; i++) pass[i] = Inf;
            for (int c = 0; c < n; c++)
            {
                var piece = grid.Pieces[c];
                if (!PieceShapes.IsRotatableKind(piece.Kind)) continue;   // empty, source, sink: never crossed
                int baseMask = PieceShapes.BaseMask(piece.Kind);
                int choices = piece.Locked ? 1 : topo.RotationSteps;
                for (int k = 0; k < choices; k++)
                {
                    int m = topo.RotateMask(c, baseMask, piece.Rotation + k);
                    for (int e = 0; e < dirs; e++)
                    for (int x = 0; x < dirs; x++)
                    {
                        if (e == x || (m & (1 << e)) == 0 || (m & (1 << x)) == 0) continue;
                        int idx = (c * dirs + e) * dirs + x;
                        if (pass[idx] == Inf) pass[idx] = k;   // k grows: the first match is the cheapest
                    }
                }
                tracked[c] = !piece.Locked && PieceShapes.DistinctRotations(piece.Kind, topo, c) > 1;
            }

            int first = topo.Neighbor(src, srcOut);
            int firstEntry = topo.Opposite(srcOut);
            if (first < 0) return Fail(result, "the source faces the edge of the grid");
            if (first == sink)
            {
                if (firstEntry != sinkIn) return Fail(result, "the source touches the sink from a closed side");
                result.Solvable = true;
                return result;
            }

            // heuristic: cheapest cost to the sink ignoring the "used once" rule (relaxation until stable; tiny graphs)
            var h = new int[n * dirs];
            for (int i = 0; i < h.Length; i++) h[i] = Inf;
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int c = 0; c < n; c++)
                for (int e = 0; e < dirs; e++)
                for (int x = 0; x < dirs; x++)
                {
                    int pc = pass[(c * dirs + e) * dirs + x];
                    if (pc == Inf) continue;
                    int nb = topo.Neighbor(c, x);
                    if (nb < 0) continue;
                    int cand;
                    if (nb == sink)
                    {
                        if (topo.Opposite(x) != sinkIn) continue;
                        cand = pc;
                    }
                    else
                    {
                        int hv = h[nb * dirs + topo.Opposite(x)];
                        if (hv >= Inf) continue;
                        cand = pc + hv;
                    }
                    if (cand < h[c * dirs + e])
                    {
                        h[c * dirs + e] = cand;
                        changed = true;
                    }
                }
            }
            int startH = h[first * dirs + firstEntry];
            if (startH >= Inf) return Fail(result, "no pipe route can connect the source to the sink");

            // A*
            var nodes = new List<Node>();
            var best = new Dictionary<(int, ulong), int>();
            var open = new BucketQueue<int>();
            ulong startUsed = tracked[first] ? 1UL << first : 0UL;
            nodes.Add(new Node { Cell = first, Entry = firstEntry, Used = startUsed, G = 0, Parent = -1 });
            best[(first * dirs + firstEntry, startUsed)] = 0;
            open.Push(0, startH);
            int goal = -1;
            while (open.TryPop(out int id, out _))
            {
                var node = nodes[id];
                if (node.Cell < 0)
                {
                    goal = id;
                    break;
                }
                if (best.TryGetValue((node.Cell * dirs + node.Entry, node.Used), out int bestG) && bestG < node.G) continue;   // stale
                if (++result.Expanded > maxExpansions) return Fail(result, $"search limit reached ({maxExpansions} states)");
                for (int x = 0; x < dirs; x++)
                {
                    if (x == node.Entry) continue;
                    int pc = pass[(node.Cell * dirs + node.Entry) * dirs + x];
                    if (pc == Inf) continue;
                    int nb = topo.Neighbor(node.Cell, x);
                    if (nb < 0) continue;
                    int g = node.G + pc;
                    if (nb == sink)
                    {
                        if (topo.Opposite(x) != sinkIn) continue;
                        nodes.Add(new Node { Cell = -1, G = g, Parent = id, PassCost = pc });
                        open.Push(nodes.Count - 1, g);
                        continue;
                    }
                    int ne = topo.Opposite(x);
                    int hv = h[nb * dirs + ne];
                    if (hv >= Inf) continue;
                    ulong used = node.Used;
                    if (tracked[nb])
                    {
                        ulong bit = 1UL << nb;
                        if ((used & bit) != 0) continue;
                        used |= bit;
                    }
                    var key = (nb * dirs + ne, used);
                    if (best.TryGetValue(key, out int og) && og <= g) continue;
                    best[key] = g;
                    nodes.Add(new Node { Cell = nb, Entry = ne, Used = used, G = g, Parent = id, PassCost = pc });
                    open.Push(nodes.Count - 1, g + hv);
                }
            }
            if (goal < 0) return Fail(result, "no route reaches the sink");

            // rebuild the route: each node's PassCost belongs to its parent's cell
            var cells = new List<int>();
            var costs = new List<int>();
            for (int cur = goal; nodes[cur].Parent >= 0; cur = nodes[cur].Parent)
            {
                cells.Add(nodes[nodes[cur].Parent].Cell);
                costs.Add(nodes[cur].PassCost);
            }
            cells.Reverse();
            costs.Reverse();
            var distinct = new HashSet<int>();
            for (int i = 0; i < cells.Count; i++)
            {
                if (distinct.Add(cells[i])) result.PathCells.Add(cells[i]);
                for (int k = 0; k < costs[i]; k++) result.Taps.Add(cells[i]);
            }
            result.Solvable = true;
            result.MinMoves = nodes[goal].G;
            result.PathLength = distinct.Count;
            return result;
        }

        static int SingleDirection(int mask)
        {
            for (int d = 0; d < 32; d++)
                if (mask == 1 << d) return d;
            return -1;
        }

        static PlumberSolution Fail(PlumberSolution r, string reason)
        {
            r.Solvable = false;
            r.Reason = reason;
            return r;
        }
    }
}
