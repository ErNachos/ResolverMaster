using System.Collections.Generic;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>Result of a water computation: for every cell the propagation step (-1 = dry) and the water color.</summary>
    public sealed class WaterState
    {
        /// <summary>0 at the sources, +1 per cell crossed (used for the cell-by-cell animation).</summary>
        public int[] Step;
        public int[] Color;
        /// <summary>Every sink receives water of its own color.</summary>
        public bool AllSinksReached;

        public bool IsWet(int cell) => Step[cell] >= 0;
    }

    /// <summary>
    /// Water spreads from each source into every neighbor whose opening faces back (openings must match on both sides).
    /// Leaks (openings towards the edge or a closed side) are allowed. Breadth-first, so steps grow with the distance.
    /// </summary>
    public static class WaterFlow
    {
        public static WaterState Compute(PipeGrid grid)
        {
            int n = grid.CellCount;
            var topo = grid.Topology;
            var state = new WaterState { Step = new int[n], Color = new int[n] };
            for (int i = 0; i < n; i++)
            {
                state.Step[i] = -1;
                state.Color[i] = -1;
            }
            var queue = new Queue<int>();
            for (int i = 0; i < n; i++)
            {
                if (grid.Pieces[i].Kind != PieceKind.Source) continue;
                state.Step[i] = 0;
                state.Color[i] = grid.Pieces[i].Color;
                queue.Enqueue(i);
            }
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                int open = grid.Openings(cell);
                for (int d = 0; d < topo.DirectionCount; d++)
                {
                    if ((open & (1 << d)) == 0) continue;
                    int next = topo.Neighbor(cell, d);
                    if (next < 0 || state.Step[next] >= 0) continue;
                    if ((grid.Openings(next) & (1 << topo.Opposite(d))) == 0) continue;
                    state.Step[next] = state.Step[cell] + 1;
                    state.Color[next] = state.Color[cell];
                    queue.Enqueue(next);
                }
            }
            bool all = true;
            for (int i = 0; i < n; i++)
                if (grid.Pieces[i].Kind == PieceKind.Sink && (state.Step[i] < 0 || state.Color[i] != grid.Pieces[i].Color)) all = false;
            state.AllSinksReached = all;
            return state;
        }
    }
}
