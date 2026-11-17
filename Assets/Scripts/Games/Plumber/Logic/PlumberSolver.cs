using System.Collections.Generic;

namespace ResolverMaster.Games.Plumber
{
    public sealed class PlumberSolution
    {
        public bool Solvable;
        /// <summary>Minimum number of taps over every possible solution.</summary>
        public int MinMoves;
        /// <summary>Pipe cells (not sources or sinks) the water crosses in the optimal solution found.</summary>
        public int PathLength;
        /// <summary>T pieces among those pipe cells.</summary>
        public int TeesOnPath;
        /// <summary>Bridges with water in both channels in the optimal solution found (ErNa-006).</summary>
        public int BridgesCrossed;
        /// <summary>Why the level is not solvable (null when solvable).</summary>
        public string Reason;
        /// <summary>Wet pipe cells of the solution, in water order (propagation step, then cell index).</summary>
        public readonly List<int> PathCells = new List<int>();
        /// <summary>One entry per tap (a cell needing 3 taps appears 3 times), in water order.</summary>
        public readonly List<int> Taps = new List<int>();
        /// <summary>Search nodes visited (all iterations).</summary>
        public int Expanded;
        /// <summary>True when the search stopped at its node limit (the level is treated as not solvable).</summary>
        public bool LimitReached;
    }

    /// <summary>
    /// Bridges (ErNa-006) are two separate nodes (<see cref="PipeNodes"/>), each wet straight through or dry, never turned;
    /// locked pieces and crosses keep their orientation.
    /// Exact minimum-taps solver for the ErNa-003 rules (no leaks, colors apart, every sink fed by its own color).
    /// A solution is a set of wet cells W with orientations such that every opening of a cell in W faces a cell of W opening back
    /// (W is a closed pipe network), each connected part of W holds one color, and each part with a sink also holds a source.
    /// Cells outside W stay as they are (0 taps), so the cost is the sum of the taps of the pipe cells in W
    /// (a cell's cost is the fewest clockwise taps reaching an orientation; symmetric orientations are merged).
    /// Search: sources and sinks start wet; an "open end" is an opening of a wet cell towards an undecided cell, which then has to join W
    /// with a matching orientation. Depth-first over the most constrained open end (every undecided cell is reached in exactly one
    /// way, so all solutions are covered and none twice), inside an iterative deepening on the cost (IDA*): the lower bound adds,
    /// for every cell with open ends, its cheapest orientation that closes them, so the first solution found is a minimum.
    /// Deterministic: same grid, same solution.
    /// </summary>
    public static class PlumberSolver
    {
        public const int MaxCells = 64;
        public const int DefaultMaxExpansions = 4000000;
        const int Inf = int.MaxValue / 4;

        public sealed class Options
        {
            /// <summary>T pieces may not join the water network (chapter 1 check: the T must never be needed).</summary>
            public bool ForbidTees;
            /// <summary>Every bridge is solved as a cross, which mixes (ErNa-006: a level "needs" its bridge when it is then unsolvable).</summary>
            public bool BridgesAsCrosses;
            public int MaxExpansions = DefaultMaxExpansions;
        }

        sealed class Search
        {
            public int N, Dirs;
            public int[] Nb;            // N * Dirs, -1 = edge
            public int[] Opp;           // Dirs
            public int[][] OptMask;     // per cell: distinct orientations (empty = never wet)
            public int[][] OptCost;     // per cell: taps for each orientation (ascending)
            public int[] Mask;          // -1 = undecided
            public int[] Color;
            public int[] Chosen;        // option index (pipes), -1 = fixed (source/sink)
            public bool[] IsSource;
            public int Expanded, MaxExpansions, NextBound;
            public bool Limit;

            // per depth scratch: target cells, their option counts
            public int[] Req, Forb, DemandColor;
        }

        public static PlumberSolution Solve(PipeGrid grid, Options options = null)
        {
            options ??= new Options();
            var result = new PlumberSolution();
            if (options.BridgesAsCrosses)
            {
                grid = grid.Clone();
                for (int c = 0; c < grid.CellCount; c++)
                    if (grid.Pieces[c].Kind == PieceKind.Bridge) grid.Pieces[c].Kind = PieceKind.Cross;
            }
            var topo = grid.Topology;
            int n = grid.CellCount;
            int dirs = topo.DirectionCount;
            if (n > MaxCells) return Fail(result, $"grid larger than {MaxCells} cells");
            // the search runs on the nodes of PipeNodes: one per cell, plus the east-west channel of every bridge
            int nodes = PipeNodes.Count(grid);

            var s = new Search
            {
                N = nodes,
                Dirs = dirs,
                Nb = new int[nodes * dirs],
                Opp = new int[dirs],
                OptMask = new int[nodes][],
                OptCost = new int[nodes][],
                Mask = new int[nodes],
                Color = new int[nodes],
                Chosen = new int[nodes],
                IsSource = new bool[nodes],
                MaxExpansions = options.MaxExpansions,
                Req = new int[nodes],
                Forb = new int[nodes],
                DemandColor = new int[nodes],
            };
            for (int d = 0; d < dirs; d++) s.Opp[d] = topo.Opposite(d);
            int sources = 0, sinks = 0;
            for (int c = 0; c < nodes; c++)
            {
                s.Mask[c] = -1;
                s.Chosen[c] = -1;
                s.Color[c] = -1;
                // a bridge channel only links to the neighbours on its own axis; the unused upper nodes link to nothing
                int channel = PipeNodes.Openings(grid, c);
                bool bridgeNode = grid.Pieces[PipeNodes.CellOf(grid, c)].Kind == PieceKind.Bridge;
                for (int d = 0; d < dirs; d++)
                    s.Nb[c * dirs + d] = c >= n && !bridgeNode ? -1 : bridgeNode && (channel & (1 << d)) == 0 ? -1 : PipeNodes.Neighbor(grid, c, d);
                if (bridgeNode)
                {
                    s.OptMask[c] = new[] { channel };   // a bridge never turns: each channel is wet straight through, or dry
                    s.OptCost[c] = new[] { 0 };
                    continue;
                }
                if (c >= n)
                {
                    s.OptMask[c] = s.OptCost[c] = System.Array.Empty<int>();
                    continue;
                }
                var piece = grid.Pieces[c];
                if (piece.Kind == PieceKind.Source || piece.Kind == PieceKind.Sink)
                {
                    s.Mask[c] = grid.Openings(c);
                    s.Color[c] = piece.Color;
                    s.IsSource[c] = piece.Kind == PieceKind.Source;
                    if (s.IsSource[c]) sources++;
                    else sinks++;
                    s.OptMask[c] = s.OptCost[c] = System.Array.Empty<int>();
                    continue;
                }
                var masks = new List<int>();
                var costs = new List<int>();
                bool allowed = PieceShapes.IsPipeKind(piece.Kind) && !(options.ForbidTees && piece.Kind == PieceKind.Tee);
                if (allowed)
                {
                    int choices = piece.IsRotatable ? topo.RotationSteps : 1;   // locked pieces, crosses: as they are
                    int baseMask = PieceShapes.BaseMask(piece.Kind);
                    for (int k = 0; k < choices; k++)
                    {
                        int m = topo.RotateMask(c, baseMask, piece.Rotation + k);
                        if (masks.Contains(m)) continue;   // symmetric orientation: the cheaper one is already listed
                        masks.Add(m);
                        costs.Add(k);
                    }
                }
                s.OptMask[c] = masks.ToArray();
                s.OptCost[c] = costs.ToArray();
            }
            if (sources == 0) return Fail(result, "no source");
            if (sinks == 0) return Fail(result, "no sink");

            // fixed cells facing each other must already agree
            for (int c = 0; c < n; c++)
            {
                if (s.Mask[c] < 0) continue;
                for (int d = 0; d < dirs; d++)
                {
                    if ((s.Mask[c] & (1 << d)) == 0) continue;
                    int nb = s.Nb[c * dirs + d];
                    if (nb < 0) return Fail(result, $"cell {c} opens towards the edge of the grid");
                    if (s.Mask[nb] >= 0 && ((s.Mask[nb] & (1 << s.Opp[d])) == 0 || s.Color[nb] != s.Color[c]))
                        return Fail(result, $"fixed cells {c} and {nb} cannot connect");
                }
            }

            int bound = 0;
            bool found = false;
            while (true)
            {
                s.NextBound = Inf;
                if (Dfs(s, 0, bound))
                {
                    found = true;
                    break;
                }
                if (s.Limit)
                {
                    result.Expanded = s.Expanded;
                    result.LimitReached = true;
                    return Fail(result, $"search limit reached ({s.MaxExpansions} nodes)");
                }
                if (s.NextBound >= Inf) break;
                bound = s.NextBound;
            }
            result.Expanded = s.Expanded;
            if (!found) return Fail(result, "no closed pipe network joins every sink to a source of its color");

            // solution: taps per cell, water order from a real water computation on the solved grid
            var solved = grid.Clone();
            var taps = new int[n];
            for (int c = 0; c < n; c++)
            {
                if (s.Chosen[c] < 0 || grid.Pieces[c].Kind == PieceKind.Bridge) continue;
                taps[c] = s.OptCost[c][s.Chosen[c]];
                solved.Rotate(c, taps[c]);
                result.MinMoves += taps[c];
            }
            var water = WaterFlow.Compute(solved);
            var order = new List<int>();
            for (int c = 0; c < n; c++)
            {
                if (s.Chosen[c] >= 0 || s.Chosen[n + c] >= 0) order.Add(c);
                if (s.Chosen[c] >= 0 && s.Chosen[n + c] >= 0) result.BridgesCrossed++;
            }
            order.Sort((a, b) => water.Step[a] != water.Step[b] ? water.Step[a].CompareTo(water.Step[b]) : a.CompareTo(b));
            foreach (int c in order)
            {
                result.PathCells.Add(c);
                if (grid.Pieces[c].Kind == PieceKind.Tee) result.TeesOnPath++;
                for (int k = 0; k < taps[c]; k++) result.Taps.Add(c);
            }
            result.PathLength = order.Count;
            result.Solvable = true;
            return result;
        }

        /// <summary>Depth-first search below the cost bound. True = a solution is now stored in Mask/Chosen.</summary>
        static bool Dfs(Search s, int g, int bound)
        {
            if (++s.Expanded > s.MaxExpansions)
            {
                s.Limit = true;
                return false;
            }
            int n = s.N, dirs = s.Dirs;
            // collect the open ends: required / forbidden sides and demand color of every undecided cell next to a wet one
            var req = s.Req;
            var forb = s.Forb;
            var dcol = s.DemandColor;
            for (int c = 0; c < n; c++)
            {
                req[c] = 0;
                forb[c] = 0;
                dcol[c] = -1;
            }
            bool any = false;
            for (int c = 0; c < n; c++)
            {
                int m = s.Mask[c];
                if (m < 0) continue;
                for (int d = 0; d < dirs; d++)
                {
                    int t = s.Nb[c * dirs + d];
                    if (t < 0 || s.Mask[t] >= 0) continue;
                    int side = 1 << s.Opp[d];
                    if ((m & (1 << d)) != 0)
                    {
                        if (dcol[t] >= 0 && dcol[t] != s.Color[c]) return false;   // two colors would meet in t
                        dcol[t] = s.Color[c];
                        req[t] |= side;
                        any = true;
                    }
                    else forb[t] |= side;
                }
            }
            if (!any) return CheckComponents(s);

            // lower bound and the most constrained target
            int h = 0, best = -1, bestCount = int.MaxValue;
            for (int t = 0; t < n; t++)
            {
                if (req[t] == 0) continue;
                int f = forb[t] | EdgeMask(s, t);
                int count = 0, minCost = Inf;
                var masks = s.OptMask[t];
                for (int i = 0; i < masks.Length; i++)
                {
                    int m = masks[i];
                    if ((m & req[t]) != req[t] || (m & f) != 0) continue;
                    count++;
                    if (s.OptCost[t][i] < minCost) minCost = s.OptCost[t][i];
                }
                if (count == 0) return false;
                h += minCost;
                if (count < bestCount)
                {
                    bestCount = count;
                    best = t;
                }
            }
            if (g + h > bound)
            {
                if (g + h < s.NextBound) s.NextBound = g + h;
                return false;
            }

            int target = best;
            int need = req[target], color = dcol[target];
            int forbidden = forb[target] | EdgeMask(s, target);
            var opts = s.OptMask[target];
            for (int i = 0; i < opts.Length; i++)
            {
                int m = opts[i];
                if ((m & need) != need || (m & forbidden) != 0) continue;
                int cost = s.OptCost[target][i];
                s.Mask[target] = m;
                s.Color[target] = color;
                s.Chosen[target] = i;
                if (Dfs(s, g + cost, bound)) return true;
                s.Mask[target] = -1;
                s.Color[target] = -1;
                s.Chosen[target] = -1;
                if (s.Limit) return false;
            }
            return false;
        }

        static int EdgeMask(Search s, int cell)
        {
            int mask = 0;
            for (int d = 0; d < s.Dirs; d++)
                if (s.Nb[cell * s.Dirs + d] < 0) mask |= 1 << d;
            return mask;
        }

        /// <summary>The network is closed: every connected part holding a sink must also hold a source (colors already agree).</summary>
        static bool CheckComponents(Search s)
        {
            int n = s.N, dirs = s.Dirs;
            var seen = new bool[n];
            var stack = new Stack<int>();
            for (int start = 0; start < n; start++)
            {
                if (s.Mask[start] < 0 || seen[start]) continue;
                bool hasSource = false, hasSink = false;
                seen[start] = true;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int c = stack.Pop();
                    if (s.IsSource[c]) hasSource = true;
                    else if (s.Chosen[c] < 0) hasSink = true;
                    for (int d = 0; d < dirs; d++)
                    {
                        if ((s.Mask[c] & (1 << d)) == 0) continue;
                        int nb = s.Nb[c * dirs + d];
                        if (nb >= 0 && !seen[nb] && s.Mask[nb] >= 0)
                        {
                            seen[nb] = true;
                            stack.Push(nb);
                        }
                    }
                }
                if (hasSink && !hasSource) return false;
            }
            return true;
        }

        static PlumberSolution Fail(PlumberSolution r, string reason)
        {
            r.Solvable = false;
            r.Reason = reason;
            return r;
        }
    }
}
