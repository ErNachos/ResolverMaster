using System;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>
    /// PG-001: the direction of a swipe on a grid, pure logic (Core: any game moved by swipes). Directions of
    /// <see cref="SquareTopology"/>: 0 up, 1 right, 2 down, 3 left. A finger movement shorter than the threshold is a tap, not a swipe.
    /// </summary>
    public static class SwipeGesture
    {
        /// <summary>
        /// The direction of a finger movement (<paramref name="dx"/>, <paramref name="dy"/>: canvas units, y up), along its longer axis;
        /// -1 when it is shorter than <paramref name="threshold"/>.
        /// </summary>
        public static int Direction(float dx, float dy, float threshold)
        {
            if (dx * dx + dy * dy < threshold * threshold) return -1;
            if (Math.Abs(dx) >= Math.Abs(dy)) return dx > 0 ? SquareTopology.Right : SquareTopology.Left;
            return dy > 0 ? SquareTopology.Up : SquareTopology.Down;
        }
    }
}
