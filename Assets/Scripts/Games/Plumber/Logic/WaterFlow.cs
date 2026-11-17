using System.Collections.Generic;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>Why an opening of the water failed.</summary>
    public enum WaterProblemKind
    {
        /// <summary>Water leaves a wet cell through an opening towards the grid edge or a side that does not match.</summary>
        Leak,
        /// <summary>Two different colors meet in connected pipes.</summary>
        ColorMix,
        /// <summary>A sink receives water of another color.</summary>
        WrongColor,
        /// <summary>A sink receives no water at all.</summary>
        DrySink,
    }

    /// <summary>One failure of an opening, with the place (for the animation) and the propagation step when it happens.</summary>
    public readonly struct WaterProblem
    {
        public readonly WaterProblemKind Kind;
        /// <summary>Leak: the wet cell that leaks. ColorMix: one of the two cells. WrongColor / DrySink: the sink.</summary>
        public readonly int Cell;
        /// <summary>Leak: the side the water leaves from (-1 otherwise).</summary>
        public readonly int Direction;
        /// <summary>ColorMix: the other cell (-1 otherwise).</summary>
        public readonly int OtherCell;
        /// <summary>The water animation runs up to this step, then the failure effect plays.</summary>
        public readonly int Step;

        public WaterProblem(WaterProblemKind kind, int cell, int direction, int otherCell, int step)
        {
            Kind = kind;
            Cell = cell;
            Direction = direction;
            OtherCell = otherCell;
            Step = step;
        }

        public override string ToString() => $"{Kind} at cell {Cell} (dir {Direction}, other {OtherCell}, step {Step})";
    }

    /// <summary>Result of opening the water: for every cell the propagation step (-1 = dry) and the water color, plus every problem.</summary>
    public sealed class WaterState
    {
        /// <summary>0 at the sources, +1 per cell crossed (used for the cell-by-cell animation). A bridge: its first wet channel.</summary>
        public int[] Step;
        public int[] Color;
        /// <summary>Per <see cref="PipeNodes"/> node (ErNa-006): both channels of a bridge, each with its own step and color.</summary>
        public int[] NodeStep, NodeColor;
        /// <summary>Every problem, ordered by step (then kind, then cell): <see cref="FirstProblem"/> is the one the player sees.</summary>
        public readonly List<WaterProblem> Problems = new List<WaterProblem>();
        /// <summary>Highest step reached by the water.</summary>
        public int LastStep;

        /// <summary>No leak, no mixed colors, every sink reached by its own color.</summary>
        public bool Success => Problems.Count == 0;
        public WaterProblem FirstProblem => Problems[0];
        /// <summary>The animation stops here: the end of the water on success, the first problem otherwise.</summary>
        public int StopStep => Success ? LastStep : FirstProblem.Step;

        public bool IsWet(int cell) => Step[cell] >= 0;
    }

    /// <summary>
    /// Opening the water (ErNa-003 rules): water starts from every source at the same time and spreads breadth-first into every neighbor
    /// whose opening faces back (a bridge carries two separate flows, <see cref="PipeNodes"/>). The attempt succeeds only when no wet opening leads to the edge or to a non-matching side, no two colors
    /// touch, and every sink receives water of its own color. Cells the water never reaches do not count.
    /// </summary>
    public static class WaterFlow
    {
        public static WaterState Compute(PipeGrid grid)
        {
            int n = grid.CellCount, nodes = PipeNodes.Count(grid);
            var topo = grid.Topology;
            var state = new WaterState { Step = new int[n], Color = new int[n], NodeStep = new int[nodes], NodeColor = new int[nodes] };
            for (int i = 0; i < nodes; i++)
            {
                state.NodeStep[i] = -1;
                state.NodeColor[i] = -1;
            }
            var queue = new Queue<int>();
            for (int i = 0; i < n; i++)
            {
                if (grid.Pieces[i].Kind != PieceKind.Source) continue;
                state.NodeStep[i] = 0;
                state.NodeColor[i] = grid.Pieces[i].Color;
                queue.Enqueue(i);
            }
            var mixes = new HashSet<long>();
            while (queue.Count > 0)
            {
                int node = queue.Dequeue();
                int cell = PipeNodes.CellOf(grid, node);
                int step = state.NodeStep[node];
                int color = state.NodeColor[node];
                if (step > state.LastStep) state.LastStep = step;
                int open = PipeNodes.Openings(grid, node);
                for (int d = 0; d < topo.DirectionCount; d++)
                {
                    if ((open & (1 << d)) == 0) continue;
                    int next = PipeNodes.Neighbor(grid, node, d);
                    if (next < 0 || (PipeNodes.Openings(grid, next) & (1 << topo.Opposite(d))) == 0)
                    {
                        state.Problems.Add(new WaterProblem(WaterProblemKind.Leak, cell, d, -1, step));
                        continue;
                    }
                    int nextCell = PipeNodes.CellOf(grid, next);
                    if (state.NodeStep[next] >= 0)
                    {
                        if (state.NodeColor[next] != color)
                        {
                            long key = node < next ? ((long)node << 32) | (uint)next : ((long)next << 32) | (uint)node;
                            if (mixes.Add(key))
                                state.Problems.Add(new WaterProblem(WaterProblemKind.ColorMix, cell, -1, nextCell, System.Math.Max(step, state.NodeStep[next])));
                        }
                        continue;
                    }
                    state.NodeStep[next] = step + 1;
                    state.NodeColor[next] = color;
                    var piece = grid.Pieces[nextCell];
                    if (piece.Kind == PieceKind.Sink && piece.Color != color)
                        state.Problems.Add(new WaterProblem(WaterProblemKind.WrongColor, nextCell, -1, cell, step + 1));
                    queue.Enqueue(next);
                }
            }
            // per cell: the node itself, or the first wet channel of a bridge
            for (int i = 0; i < n; i++)
            {
                int a = state.NodeStep[i], b = state.NodeStep[n + i];
                bool useB = b >= 0 && (a < 0 || b < a);
                state.Step[i] = useB ? b : a;
                state.Color[i] = useB ? state.NodeColor[n + i] : state.NodeColor[i];
            }
            for (int i = 0; i < n; i++)
                if (grid.Pieces[i].Kind == PieceKind.Sink && state.Step[i] < 0)
                    state.Problems.Add(new WaterProblem(WaterProblemKind.DrySink, i, -1, -1, state.LastStep + 1));
            state.Problems.Sort((a, b) =>
            {
                int c = a.Step.CompareTo(b.Step);
                if (c == 0) c = a.Kind.CompareTo(b.Kind);
                if (c == 0) c = a.Cell.CompareTo(b.Cell);
                return c != 0 ? c : a.Direction.CompareTo(b.Direction);
            });
            return state;
        }
    }
}
