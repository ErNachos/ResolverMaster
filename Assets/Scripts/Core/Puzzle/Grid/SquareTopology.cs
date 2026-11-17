using System;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>
    /// Rectangular grid of square cells. Cell index = y * Width + x, with y = 0 the TOP row (as drawn on screen).
    /// Directions clockwise: 0 Up, 1 Right, 2 Down, 3 Left.
    /// </summary>
    public sealed class SquareTopology : IGridTopology
    {
        public const int Up = 0, Right = 1, Down = 2, Left = 3;

        static readonly int[] s_dx = { 0, 1, 0, -1 };
        static readonly int[] s_dy = { -1, 0, 1, 0 };

        public int Width { get; }
        public int Height { get; }
        public int CellCount => Width * Height;
        public int DirectionCount => 4;
        public int RotationSteps => 4;

        public SquareTopology(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "grid size must be positive");
            Width = width;
            Height = height;
        }

        public int Index(int x, int y) => y * Width + x;
        public int X(int cell) => cell % Width;
        public int Y(int cell) => cell / Width;

        public int Neighbor(int cell, int direction)
        {
            int x = cell % Width + s_dx[direction];
            int y = cell / Width + s_dy[direction];
            return x < 0 || y < 0 || x >= Width || y >= Height ? -1 : y * Width + x;
        }

        public int Opposite(int direction) => (direction + 2) & 3;

        public int RotateMask(int cell, int mask, int steps)
        {
            int s = ((steps % 4) + 4) % 4;
            if (s == 0) return mask & 0xF;
            return ((mask << s) | (mask >> (4 - s))) & 0xF;
        }

        /// <summary>Direction from <paramref name="from"/> to an adjacent <paramref name="to"/>, or -1 if not adjacent.</summary>
        public int DirectionTo(int from, int to)
        {
            for (int d = 0; d < 4; d++)
                if (Neighbor(from, d) == to) return d;
            return -1;
        }
    }
}
