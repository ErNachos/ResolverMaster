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
        /// <summary>
        /// ErNa-006: two separate pipes, north-south over east-west; the two flows never mix. Same in every rotation (never rotated).
        /// </summary>
        Bridge,
    }

    /// <summary>One cell of the plumber grid. <see cref="Rotation"/> = clockwise taps from the base shape (0..3).</summary>
    [Serializable]
    public struct Piece
    {
        public PieceKind Kind;
        public int Rotation;
        /// <summary>Locked pieces (ErNa-006) cannot be rotated: a tap only shakes them and is not a move.</summary>
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

        /// <summary>Straight, curve and T can be turned (unless locked); cross, bridge, source, sink and empty cells cannot (ErNa-006).</summary>
        public bool IsRotatable => !Locked && PieceShapes.IsRotatableKind(Kind);

        /// <summary>A pipe the water can run through (straight, curve, T, cross, bridge).</summary>
        public bool IsPipe => PieceShapes.IsPipeKind(Kind);
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
                case PieceKind.Cross:
                case PieceKind.Bridge: return U | R | D | L;
                case PieceKind.Source:
                case PieceKind.Sink: return U;
                default: return 0;
            }
        }

        /// <summary>
        /// Pieces a tap turns. The cross and the bridge look the same in every rotation (ErNa-006): tapping them only shakes them and
        /// is not a move.
        /// </summary>
        public static bool IsRotatableKind(PieceKind kind) =>
            kind == PieceKind.Straight || kind == PieceKind.Curve || kind == PieceKind.Tee;

        /// <summary>Pieces the water can run through.</summary>
        public static bool IsPipeKind(PieceKind kind) => IsRotatableKind(kind) || kind == PieceKind.Cross || kind == PieceKind.Bridge;

        /// <summary>The two separate channels of a bridge: north-south (Up | Down) and east-west (Right | Left).</summary>
        public const int VerticalChannel = U | D, HorizontalChannel = R | L;

        /// <summary>True for Up and Down (the north-south channel of a bridge).</summary>
        public static bool IsVertical(int dir) => dir == SquareTopology.Up || dir == SquareTopology.Down;

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
