using System;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    public enum PieceKind
    {
        Empty,
        Straight,
        Curve,
        Tee,
        Cross,
        /// <summary>Fixed, one opening: the water starts here.</summary>
        Source,
        /// <summary>Fixed, one opening: must receive water of its color.</summary>
        Sink,
    }

    /// <summary>One cell of the plumber grid. <see cref="Rotation"/> = clockwise taps from the base shape (0..3).</summary>
    [Serializable]
    public struct Piece
    {
        public PieceKind Kind;
        public int Rotation;
        /// <summary>Locked pieces cannot be rotated (future chapters).</summary>
        public bool Locked;
        /// <summary>Water color of a source/sink pair (0 = first). Only one color is used for now.</summary>
        public int Color;

        public Piece(PieceKind kind, int rotation = 0, bool locked = false, int color = 0)
        {
            Kind = kind;
            Rotation = rotation;
            Locked = locked;
            Color = color;
        }

        /// <summary>Straight, curve, T and cross can be tapped (unless locked); source, sink and empty cells cannot.</summary>
        public bool IsRotatable => !Locked && PieceShapes.IsRotatableKind(Kind);
    }

    /// <summary>Base openings of every piece kind (rotation 0) on the square grid: Up = bit 0, Right = 1, Down = 2, Left = 3.</summary>
    public static class PieceShapes
    {
        const int U = 1 << SquareTopology.Up, R = 1 << SquareTopology.Right, D = 1 << SquareTopology.Down, L = 1 << SquareTopology.Left;

        public static int BaseMask(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Straight: return U | D;
                case PieceKind.Curve: return U | R;
                case PieceKind.Tee: return U | R | D;
                case PieceKind.Cross: return U | R | D | L;
                case PieceKind.Source:
                case PieceKind.Sink: return U;
                default: return 0;
            }
        }

        public static bool IsRotatableKind(PieceKind kind) =>
            kind == PieceKind.Straight || kind == PieceKind.Curve || kind == PieceKind.Tee || kind == PieceKind.Cross;

        public static int Openings(Piece piece, IGridTopology topology, int cell) =>
            topology.RotateMask(cell, BaseMask(piece.Kind), piece.Rotation);

        /// <summary>Distinct orientations (symmetry): straight 2, cross 1, curve and T 4.</summary>
        public static int DistinctRotations(PieceKind kind, IGridTopology topology, int cell)
        {
            int mask = BaseMask(kind);
            for (int k = 1; k < topology.RotationSteps; k++)
                if (topology.RotateMask(cell, mask, k) == mask) return k;
            return topology.RotationSteps;
        }
    }
}
