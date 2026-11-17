namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// The water network as nodes (ErNa-006, bridges): every cell is one node (index = cell), except a bridge, which is two separate
    /// nodes: its north-south channel (index = cell) and its east-west channel (index = cell count + cell). Water entering a bridge
    /// from the north leaves only to the south, and never meets the east-west flow. The solver and <see cref="WaterFlow"/> walk these
    /// nodes, so bridges need no special case there. Nodes count = 2 x cells (the upper half is used only by bridges).
    /// </summary>
    public static class PipeNodes
    {
        public static int Count(PipeGrid grid) => grid.CellCount * 2;

        public static int CellOf(PipeGrid grid, int node) => node < grid.CellCount ? node : node - grid.CellCount;

        /// <summary>The node of <paramref name="cell"/> that uses its side <paramref name="dir"/>.</summary>
        public static int NodeAt(PipeGrid grid, int cell, int dir) =>
            grid.Pieces[cell].Kind == PieceKind.Bridge && !PieceShapes.IsVertical(dir) ? grid.CellCount + cell : cell;

        /// <summary>True for the east-west channel of a bridge.</summary>
        public static bool IsHorizontalChannel(PipeGrid grid, int node) => node >= grid.CellCount;

        /// <summary>Openings of a node: the cell openings, or one channel of a bridge (0 for the unused upper nodes).</summary>
        public static int Openings(PipeGrid grid, int node)
        {
            int n = grid.CellCount;
            if (node >= n) return grid.Pieces[node - n].Kind == PieceKind.Bridge ? PieceShapes.HorizontalChannel : 0;
            return grid.Pieces[node].Kind == PieceKind.Bridge ? PieceShapes.VerticalChannel : grid.Openings(node);
        }

        /// <summary>The node reached leaving <paramref name="node"/> through side <paramref name="dir"/>, or -1 at the edge of the grid.</summary>
        public static int Neighbor(PipeGrid grid, int node, int dir)
        {
            int next = grid.Topology.Neighbor(CellOf(grid, node), dir);
            return next < 0 ? -1 : NodeAt(grid, next, grid.Topology.Opposite(dir));
        }
    }
}
