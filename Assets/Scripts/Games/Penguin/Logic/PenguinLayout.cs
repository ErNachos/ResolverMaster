using System;
using System.Collections.Generic;
using System.Text;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>What a cell of the ice field is made of (the fixed part of a level).</summary>
    public enum PenguinTerrain : byte
    {
        Ice,
        /// <summary>Stops penguins and blocks in the cell before it.</summary>
        Rock,
        /// <summary>A penguin or a block entering it stops there.</summary>
        Snow,
        /// <summary>A hole in the ice: a penguin falls in (level failed); a block sinks and fills it (it becomes ice).</summary>
        Water,
    }

    /// <summary>
    /// A Pino level (PG-001): the fixed board (terrain, border, fish, igloos) and the packed start state. Pure logic, immutable after
    /// creation, thread-safe to read. Rules: <c>docs/penguin-regole.md</c>.
    /// Cells: index = y * Width + x, y = 0 the top row. Directions: the Core <see cref="SquareTopology"/> ones (0 up, 1 right, 2 down, 3 left).
    /// The border of the grid is made of segments, one per cell side along the edge: snow wall (stops like a rock) or water (a penguin
    /// falls in; a block stops as against a wall).
    /// Packed state (64 bits): penguin i at bits 6i..6i+5 (63 = in its igloo, or no such penguin); block j at 12+6j (63 = sunk; blocks
    /// are the same, kept sorted so equal positions pack the same); collected fish mask at bits 30..37; filled holes mask at 38..53.
    /// Text form: <c>WxH:row/row/...[:border]</c>, rows from the top; <c>.</c> ice, <c>#</c> rock, <c>s</c> snow, <c>w</c> water,
    /// <c>f</c> fish, <c>c</c> ice block, <c>P</c>/<c>Q</c> first/second penguin, <c>I</c>/<c>J</c> their igloos (all on ice); border:
    /// one character per edge side (<c>m</c> wall, <c>w</c> water) along the top (left to right), the right side (top to bottom), the
    /// bottom (left to right), the left side (top to bottom); missing = all wall.
    /// </summary>
    public sealed class PenguinLayout
    {
        public const int MaxPenguins = 2, MaxBlocks = 3, MaxFish = 8, MaxHoles = 16, MaxCells = 63;
        /// <summary>Packed position of a penguin in its igloo (or absent) and of a sunk block.</summary>
        public const int Gone = 63;
        public const int PenguinShift = 0, BlockShift = 12, FishShift = 30, HoleShift = 38;

        static readonly int[] s_dx = { 0, 1, 0, -1 };
        static readonly int[] s_dy = { -1, 0, 1, 0 };

        readonly PenguinTerrain[] _terrain;
        readonly int[] _fishCells, _fishAt, _igloos, _iglooAt, _holeCells, _holeAt;
        readonly bool[] _edgeWater;

        public int Width { get; }
        public int Height { get; }
        public int CellCount => Width * Height;
        public int PenguinCount { get; }
        public int BlockCount { get; }
        public int FishCount => _fishCells.Length;
        /// <summary>Water holes that blocks can fill (only tracked when the level has blocks).</summary>
        public int HoleCount => _holeCells.Length;
        public ulong Start { get; }
        public int AllFishMask => (1 << FishCount) - 1;
        /// <summary>The penguin bits of a solved state (every penguin in its igloo).</summary>
        public ulong SolvedPenguins { get; }

        public PenguinLayout(int width, int height, PenguinTerrain[] terrain, IReadOnlyList<int> fish, IReadOnlyList<int> blocks,
            IReadOnlyList<int> penguins, IReadOnlyList<int> igloos, bool[] edgeWater = null)
        {
            if (width < 2 || height < 2 || width * height > MaxCells) throw new FormatException($"grid {width}x{height}: 2..63 cells");
            Width = width;
            Height = height;
            int n = width * height;
            if (terrain == null || terrain.Length != n) throw new FormatException("terrain size");
            _terrain = (PenguinTerrain[])terrain.Clone();
            if (penguins == null || igloos == null || penguins.Count < 1 || penguins.Count > MaxPenguins || igloos.Count != penguins.Count)
                throw new FormatException("1 or 2 penguins, each with its igloo");
            if (fish.Count > MaxFish) throw new FormatException($"at most {MaxFish} fish");
            if (blocks.Count > MaxBlocks) throw new FormatException($"at most {MaxBlocks} ice blocks");
            PenguinCount = penguins.Count;
            BlockCount = blocks.Count;
            _edgeWater = new bool[2 * (width + height)];
            if (edgeWater != null)
            {
                if (edgeWater.Length != _edgeWater.Length) throw new FormatException("border length");
                Array.Copy(edgeWater, _edgeWater, _edgeWater.Length);
            }

            var used = new bool[n];
            void Claim(int cell, string what)
            {
                if (cell < 0 || cell >= n) throw new FormatException(what + " outside the grid");
                if (used[cell] || _terrain[cell] != PenguinTerrain.Ice) throw new FormatException(what + " must be alone on an ice cell");
                used[cell] = true;
            }

            _fishCells = new int[fish.Count];
            _fishAt = Filled(n, -1);
            for (int i = 0; i < fish.Count; i++)
            {
                Claim(fish[i], "fish");
                _fishCells[i] = fish[i];
                _fishAt[fish[i]] = i;
            }
            _igloos = new int[PenguinCount];
            _iglooAt = Filled(n, -1);
            for (int i = 0; i < PenguinCount; i++)
            {
                Claim(igloos[i], "igloo");
                _igloos[i] = igloos[i];
                _iglooAt[igloos[i]] = i;
            }
            var holes = new List<int>();
            _holeAt = Filled(n, -1);
            if (BlockCount > 0)
                for (int c = 0; c < n; c++)
                    if (_terrain[c] == PenguinTerrain.Water)
                    {
                        _holeAt[c] = holes.Count;
                        holes.Add(c);
                    }
            if (holes.Count > MaxHoles) throw new FormatException($"at most {MaxHoles} water holes in a level with ice blocks");
            _holeCells = holes.ToArray();

            ulong start = 0, solved = 0;
            for (int i = 0; i < MaxPenguins; i++)
            {
                ulong p = Gone;
                if (i < PenguinCount)
                {
                    Claim(penguins[i], "penguin");
                    p = (ulong)penguins[i];
                }
                start |= p << (PenguinShift + 6 * i);
                solved |= (ulong)Gone << (PenguinShift + 6 * i);
            }
            var sorted = new List<int>(blocks);
            foreach (int b in sorted) Claim(b, "ice block");
            sorted.Sort();
            for (int j = 0; j < MaxBlocks; j++)
                start |= (ulong)(j < sorted.Count ? sorted[j] : Gone) << (BlockShift + 6 * j);
            Start = start;
            SolvedPenguins = solved;
        }

        static int[] Filled(int n, int value)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = value;
            return a;
        }

        // ------------------------------------------------------------------ board

        public PenguinTerrain Terrain(int cell) => _terrain[cell];
        public int FishCell(int fish) => _fishCells[fish];
        /// <summary>The fish on a cell (-1 = none).</summary>
        public int FishAt(int cell) => _fishAt[cell];
        public int Igloo(int penguin) => _igloos[penguin];
        /// <summary>Whose igloo is on a cell (-1 = none).</summary>
        public int IglooAt(int cell) => _iglooAt[cell];
        public int HoleCell(int hole) => _holeCells[hole];
        /// <summary>The tracked hole on a cell (-1 = none or not tracked).</summary>
        public int HoleAt(int cell) => _holeAt[cell];

        public int X(int cell) => cell % Width;
        public int Y(int cell) => cell / Width;
        public int Cell(int x, int y) => y * Width + x;

        /// <summary>The next cell in a direction, -1 outside the grid.</summary>
        public int Neighbor(int cell, int dir)
        {
            int x = cell % Width + s_dx[dir], y = cell / Width + s_dy[dir];
            return x < 0 || y < 0 || x >= Width || y >= Height ? -1 : y * Width + x;
        }

        public static int Dx(int dir) => s_dx[dir];
        public static int Dy(int dir) => s_dy[dir];

        /// <summary>Index of the border segment on side <paramref name="dir"/> of an edge cell (see the class summary).</summary>
        public int EdgeIndex(int cell, int dir)
        {
            int x = X(cell), y = Y(cell);
            switch (dir)
            {
                case SquareTopology.Up: return x;
                case SquareTopology.Right: return Width + y;
                case SquareTopology.Down: return Width + Height + x;
                default: return 2 * Width + Height + y;
            }
        }

        public int EdgeCount => _edgeWater.Length;
        public bool EdgeWater(int edge) => _edgeWater[edge];
        /// <summary>The border beyond side <paramref name="dir"/> of an edge cell is water.</summary>
        public bool EdgeWaterAt(int cell, int dir) => _edgeWater[EdgeIndex(cell, dir)];

        /// <summary>The edge cell and its outer side of border segment <paramref name="edge"/>.</summary>
        public void EdgeCell(int edge, out int cell, out int dir)
        {
            int w = Width, h = Height;
            if (edge < w) { cell = Cell(edge, 0); dir = SquareTopology.Up; }
            else if (edge < w + h) { cell = Cell(w - 1, edge - w); dir = SquareTopology.Right; }
            else if (edge < 2 * w + h) { cell = Cell(edge - w - h, h - 1); dir = SquareTopology.Down; }
            else { cell = Cell(0, edge - 2 * w - h); dir = SquareTopology.Left; }
        }

        public int Count(PenguinTerrain t)
        {
            int k = 0;
            foreach (var x in _terrain)
                if (x == t) k++;
            return k;
        }

        public int WaterEdgeCount
        {
            get
            {
                int k = 0;
                foreach (bool w in _edgeWater)
                    if (w) k++;
                return k;
            }
        }

        /// <summary>Runs of consecutive water border sides on the same side of the grid.</summary>
        public int WaterEdgeSegments
        {
            get
            {
                int k = 0;
                for (int e = 0; e < _edgeWater.Length; e++)
                {
                    if (!_edgeWater[e]) continue;
                    bool continues = e > 0 && _edgeWater[e - 1] && SideOf(e - 1) == SideOf(e);
                    if (!continues) k++;
                }
                return k;
            }
        }

        int SideOf(int edge) => edge < Width ? 0 : edge < Width + Height ? 1 : edge < 2 * Width + Height ? 2 : 3;

        // ------------------------------------------------------------------ packed state

        public static int PenguinPos(ulong state, int penguin) => (int)((state >> (PenguinShift + 6 * penguin)) & 63);
        public static int BlockPos(ulong state, int block) => (int)((state >> (BlockShift + 6 * block)) & 63);
        public static int FishMask(ulong state) => (int)((state >> FishShift) & 0xFF);
        public static int HoleMask(ulong state) => (int)((state >> HoleShift) & 0xFFFF);

        public bool IsIn(ulong state, int penguin) => PenguinPos(state, penguin) == Gone;
        public bool IsSolved(ulong state) => (state & 0xFFFUL) == SolvedPenguins;
        public bool FishTaken(ulong state, int fish) => (FishMask(state) & (1 << fish)) != 0;
        public bool HoleFilled(ulong state, int hole) => (HoleMask(state) & (1 << hole)) != 0;
        public bool IglooOpen(ulong state) => FishMask(state) == AllFishMask;

        /// <summary>Water now on a cell (a filled hole is ice).</summary>
        public bool IsWater(ulong state, int cell)
        {
            if (_terrain[cell] != PenguinTerrain.Water) return false;
            int h = _holeAt[cell];
            return h < 0 || (HoleMask(state) & (1 << h)) == 0;
        }

        /// <summary>The block on a cell (-1 = none).</summary>
        public int BlockAt(ulong state, int cell)
        {
            for (int j = 0; j < BlockCount; j++)
                if (BlockPos(state, j) == cell) return j;
            return -1;
        }

        public static ulong Pack(int[] penguins, int[] blocks, int fishMask, int holeMask)
        {
            ulong s = 0;
            for (int i = 0; i < MaxPenguins; i++) s |= (ulong)(i < penguins.Length ? penguins[i] & 63 : Gone) << (PenguinShift + 6 * i);
            // blocks sorted (insertion sort: at most 3)
            int b0 = blocks.Length > 0 ? blocks[0] : Gone, b1 = blocks.Length > 1 ? blocks[1] : Gone, b2 = blocks.Length > 2 ? blocks[2] : Gone;
            if (b1 < b0) (b0, b1) = (b1, b0);
            if (b2 < b1) (b1, b2) = (b2, b1);
            if (b1 < b0) (b0, b1) = (b1, b0);
            s |= (ulong)(b0 & 63) << BlockShift;
            s |= (ulong)(b1 & 63) << (BlockShift + 6);
            s |= (ulong)(b2 & 63) << (BlockShift + 12);
            s |= (ulong)(fishMask & 0xFF) << FishShift;
            s |= (ulong)(holeMask & 0xFFFF) << HoleShift;
            return s;
        }

        // ------------------------------------------------------------------ text form

        public static PenguinLayout Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new FormatException("empty level");
            var parts = text.Trim().Split(':');
            if (parts.Length < 2) throw new FormatException("expected WxH:rows[:border]");
            var size = parts[0].Split('x');
            if (size.Length != 2 || !int.TryParse(size[0], out int w) || !int.TryParse(size[1], out int h)) throw new FormatException("bad size " + parts[0]);
            var rows = parts[1].Split('/');
            if (rows.Length != h) throw new FormatException($"{rows.Length} rows, expected {h}");
            var terrain = new PenguinTerrain[w * h];
            var fish = new List<int>();
            var blocks = new List<int>();
            int[] penguins = { -1, -1 }, igloos = { -1, -1 };
            for (int y = 0; y < h; y++)
            {
                if (rows[y].Length != w) throw new FormatException($"row {y}: {rows[y].Length} cells, expected {w}");
                for (int x = 0; x < w; x++)
                {
                    int c = y * w + x;
                    switch (rows[y][x])
                    {
                        case '.': break;
                        case '#': terrain[c] = PenguinTerrain.Rock; break;
                        case 's': terrain[c] = PenguinTerrain.Snow; break;
                        case 'w': terrain[c] = PenguinTerrain.Water; break;
                        case 'f': fish.Add(c); break;
                        case 'c': blocks.Add(c); break;
                        case 'P': penguins[0] = c; break;
                        case 'Q': penguins[1] = c; break;
                        case 'I': igloos[0] = c; break;
                        case 'J': igloos[1] = c; break;
                        default: throw new FormatException($"unknown cell '{rows[y][x]}'");
                    }
                }
            }
            if (penguins[0] < 0 || igloos[0] < 0) throw new FormatException("no penguin P or no igloo I");
            if ((penguins[1] < 0) != (igloos[1] < 0)) throw new FormatException("the second penguin Q needs its igloo J");
            int count = penguins[1] >= 0 ? 2 : 1;
            bool[] edges = null;
            if (parts.Length > 2 && parts[2].Length > 0)
            {
                if (parts[2].Length != 2 * (w + h)) throw new FormatException($"border: {parts[2].Length} sides, expected {2 * (w + h)}");
                edges = new bool[parts[2].Length];
                for (int e = 0; e < edges.Length; e++)
                {
                    char ch = parts[2][e];
                    if (ch != 'm' && ch != 'w') throw new FormatException($"border side '{ch}' (m wall, w water)");
                    edges[e] = ch == 'w';
                }
            }
            return new PenguinLayout(w, h, terrain, fish, blocks, new ArraySegment<int>(penguins, 0, count), new ArraySegment<int>(igloos, 0, count), edges);
        }

        /// <summary>The text form of the board with the pieces of <paramref name="state"/> (the start when omitted).</summary>
        public string Format(ulong? state = null)
        {
            ulong s = state ?? Start;
            var sb = new StringBuilder();
            sb.Append(Width).Append('x').Append(Height).Append(':');
            for (int y = 0; y < Height; y++)
            {
                if (y > 0) sb.Append('/');
                for (int x = 0; x < Width; x++)
                {
                    int c = Cell(x, y);
                    char ch = '.';
                    switch (_terrain[c])
                    {
                        case PenguinTerrain.Rock: ch = '#'; break;
                        case PenguinTerrain.Snow: ch = 's'; break;
                        case PenguinTerrain.Water: ch = IsWater(s, c) ? 'w' : '.'; break;
                    }
                    if (_fishAt[c] >= 0 && !FishTaken(s, _fishAt[c])) ch = 'f';
                    if (_iglooAt[c] >= 0) ch = _iglooAt[c] == 0 ? 'I' : 'J';
                    if (BlockAt(s, c) >= 0) ch = 'c';
                    for (int i = 0; i < PenguinCount; i++)
                        if (PenguinPos(s, i) == c) ch = i == 0 ? 'P' : 'Q';
                    sb.Append(ch);
                }
            }
            if (WaterEdgeCount > 0)
            {
                sb.Append(':');
                foreach (bool e in _edgeWater) sb.Append(e ? 'w' : 'm');
            }
            return sb.ToString();
        }

        /// <summary>A copy with some kinds of cells replaced (the generator's checks that a novelty is needed).</summary>
        public PenguinLayout Variant(bool snowToIce = false, bool waterToIce = false, bool edgesToWall = false, bool rockToIce = false, bool noFish = false)
        {
            var t = (PenguinTerrain[])_terrain.Clone();
            for (int c = 0; c < t.Length; c++)
            {
                if (snowToIce && t[c] == PenguinTerrain.Snow) t[c] = PenguinTerrain.Ice;
                if (waterToIce && t[c] == PenguinTerrain.Water) t[c] = PenguinTerrain.Ice;
                if (rockToIce && t[c] == PenguinTerrain.Rock) t[c] = PenguinTerrain.Ice;
            }
            var pens = new int[PenguinCount];
            for (int i = 0; i < PenguinCount; i++) pens[i] = PenguinPos(Start, i);
            var blocks = new List<int>();
            for (int j = 0; j < BlockCount; j++) blocks.Add(BlockPos(Start, j));
            return new PenguinLayout(Width, Height, t, noFish ? Array.Empty<int>() : _fishCells, blocks, pens, _igloos, edgesToWall ? null : _edgeWater);
        }
    }
}
