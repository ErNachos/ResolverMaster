namespace ResolverMaster.Core.Puzzle
{
    /// <summary>
    /// Abstract grid shape. Cells are indices 0..CellCount-1; directions are 0..DirectionCount-1 in clockwise order.
    /// Puzzles only talk to this interface, so hexagons or triangles can be added later as new implementations
    /// (a triangle grid rotates differently in up/down cells: that is why rotation takes the cell).
    /// </summary>
    public interface IGridTopology
    {
        int CellCount { get; }

        /// <summary>Sides of a cell (square 4, hexagon 6, triangle 3).</summary>
        int DirectionCount { get; }

        /// <summary>Clockwise steps of a full turn of a piece (square 4: one tap = 90 degrees).</summary>
        int RotationSteps { get; }

        /// <summary>The cell across side <paramref name="direction"/>, or -1 outside the grid.</summary>
        int Neighbor(int cell, int direction);

        /// <summary>The side of the neighbor that faces back (Up &lt;-&gt; Down...).</summary>
        int Opposite(int direction);

        /// <summary>Rotates a <see cref="DirectionMask"/> clockwise by <paramref name="steps"/> (negative = counter-clockwise).</summary>
        int RotateMask(int cell, int mask, int steps);
    }

    /// <summary>A set of directions as bits: bit d set = side d open.</summary>
    public static class DirectionMask
    {
        public static int Bit(int direction) => 1 << direction;

        public static bool Has(int mask, int direction) => (mask & (1 << direction)) != 0;

        public static int Count(int mask)
        {
            int n = 0;
            for (; mask != 0; mask &= mask - 1) n++;
            return n;
        }
    }
}
