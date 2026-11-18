using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>The plumber board state: a square topology and one piece per cell.</summary>
    public sealed class PipeGrid
    {
        public readonly SquareTopology Topology;
        public readonly Piece[] Pieces;

        public int Width => Topology.Width;
        public int Height => Topology.Height;
        public int CellCount => Pieces.Length;

        public PipeGrid(int width, int height)
        {
            Topology = new SquareTopology(width, height);
            Pieces = new Piece[width * height];
        }

        public PipeGrid Clone()
        {
            var copy = new PipeGrid(Width, Height);
            Array.Copy(Pieces, copy.Pieces, Pieces.Length);
            return copy;
        }

        public void CopyFrom(PipeGrid other)
        {
            if (other.Width != Width || other.Height != Height) throw new ArgumentException("grid size differs");
            Array.Copy(other.Pieces, Pieces, Pieces.Length);
        }

        public int Openings(int cell) => PieceShapes.Openings(Pieces[cell], Topology, cell);

        /// <summary>Rotates by <paramref name="steps"/> clockwise taps (negative = back). Rotation stays in 0..RotationSteps-1.</summary>
        public void Rotate(int cell, int steps)
        {
            int n = Topology.RotationSteps;
            Pieces[cell].Rotation = (((Pieces[cell].Rotation + steps) % n) + n) % n;
        }

        public List<int> CellsOf(PieceKind kind)
        {
            var list = new List<int>();
            for (int i = 0; i < Pieces.Length; i++)
                if (Pieces[i].Kind == kind) list.Add(i);
            return list;
        }

        public int Count(PieceKind kind)
        {
            int n = 0;
            foreach (var p in Pieces)
                if (p.Kind == kind) n++;
            return n;
        }

        /// <summary>Locked pieces (ErNa-006).</summary>
        public int CountLocked()
        {
            int n = 0;
            foreach (var p in Pieces)
                if (p.Locked && p.Kind != PieceKind.Empty) n++;
            return n;
        }

        /// <summary>Number of distinct water colors used by sources and sinks.</summary>
        public int ColorCount()
        {
            var seen = new HashSet<int>();
            foreach (var p in Pieces)
                if (p.Kind == PieceKind.Source || p.Kind == PieceKind.Sink) seen.Add(p.Color);
            return seen.Count;
        }

        /// <summary>ErNa-009: fountains (sinks) of water color <paramref name="color"/>.</summary>
        public int CountSinks(int color)
        {
            int n = 0;
            foreach (var p in Pieces)
                if (p.Kind == PieceKind.Sink && p.Color == color) n++;
            return n;
        }

        /// <summary>Highest water color used by a source or sink (-1 if none).</summary>
        public int MaxColor()
        {
            int max = -1;
            foreach (var p in Pieces)
                if (p.Kind == PieceKind.Source || p.Kind == PieceKind.Sink) max = Math.Max(max, p.Color);
            return max;
        }
    }
}
