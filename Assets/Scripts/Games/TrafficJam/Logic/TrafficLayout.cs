using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.TrafficJam
{
    public enum VehicleKind
    {
        /// <summary>2 cells.</summary>
        Car,
        /// <summary>3 cells.</summary>
        Truck,
        /// <summary>The car that must leave by its exit (red, or blue in the advanced chapters): 2 cells.</summary>
        Target,
    }

    /// <summary>One vehicle of a level: kind, length, direction and the row or column it can never leave.</summary>
    public sealed class Vehicle
    {
        public VehicleKind Kind;
        public int Length;
        /// <summary>True = moves left/right along its row; false = up/down along its column.</summary>
        public bool Horizontal;
        /// <summary>Its row (horizontal) or column (vertical).</summary>
        public int Line;
        /// <summary>Target cars only: 0 = red, 1 = blue; -1 for the other vehicles.</summary>
        public int Target = -1;
        /// <summary>Target cars only: the side of its exit (<see cref="SquareTopology"/> direction: 0 up, 1 right, 2 down, 3 left).</summary>
        public int ExitSide = SquareTopology.Up;

        public bool IsTarget => Kind == VehicleKind.Target;

        /// <summary>The exit is on the side of the low positions (up or left).</summary>
        public bool ExitsLow => ExitSide == SquareTopology.Up || ExitSide == SquareTopology.Left;
    }

    /// <summary>
    /// A move: one vehicle slid from one position to another along its line, any number of cells, or (target cars) out through its exit.
    /// Positions count along the line: the column of the left cell (horizontal) or the row of the top cell (vertical).
    /// </summary>
    public readonly struct TrafficMove : IEquatable<TrafficMove>
    {
        public readonly int Vehicle;
        public readonly int From;
        /// <summary>The new position, or <see cref="TrafficLayout.Exited"/>.</summary>
        public readonly int To;

        public TrafficMove(int vehicle, int from, int to)
        {
            Vehicle = vehicle;
            From = from;
            To = to;
        }

        public bool IsExit => To == TrafficLayout.Exited;

        /// <summary>One integer (Challenge save): vehicle, from and to in 8 + 4 + 4 bits.</summary>
        public int Encode() => (Vehicle << 8) | (From << 4) | To;

        public static TrafficMove Decode(int code) => new TrafficMove(code >> 8, (code >> 4) & 15, code & 15);

        public bool Equals(TrafficMove o) => Vehicle == o.Vehicle && From == o.From && To == o.To;
        public override bool Equals(object obj) => obj is TrafficMove o && Equals(o);
        public override int GetHashCode() => Encode();
        public override string ToString() => $"v{Vehicle} {From}->{(IsExit ? "out" : To.ToString(CultureInfo.InvariantCulture))}";
    }

    /// <summary>
    /// The fixed part of a level (pure logic): grid size (at most 64 cells), fixed obstacles, the vehicles with their lines and the exits of
    /// the target cars. The moving part is a <b>state</b>: the position of every vehicle packed in one 64-bit number, 4 bits for a target
    /// car (<see cref="Exited"/> = left the grid) and 3 bits for the others (they never leave), at most <see cref="MaxVehicles"/> vehicles.
    /// Target cars come first.
    /// Rules: a vehicle slides only along its line (never turns), stopping against vehicles, obstacles and the edge; a target car may
    /// also slide out through its exit when every cell up to it is free; no other vehicle leaves. Solved = every target car is out.
    /// Text form (<see cref="ToText"/>): "WxH" then tokens: R/B (red/blue target), C (car), T (truck) + "x.y" (top-left cell) + h/v
    /// + exit side U/R/D/L for targets; "#x.y" = obstacle. E.g. "6x8 R2.5vU C0.0h T4.1v #1.3".
    /// </summary>
    public sealed class TrafficLayout
    {
        /// <summary>1 target car (4 bits) + 20 others (3 bits) = 64 bits; with 2 target cars 18 others (62 bits).</summary>
        public const int MaxVehicles = 21;
        public const int Exited = 15;
        public const int MaxCells = 64;

        public readonly int Width, Height;
        public readonly IReadOnlyList<Vehicle> Vehicles;
        /// <summary>One bit per obstacle cell (index y * Width + x).</summary>
        public readonly ulong Obstacles;
        public readonly int TargetCount;

        readonly Vehicle[] _v;
        /// <summary>[vehicle][position] = the cells it covers there.</summary>
        readonly ulong[][] _masks;
        readonly ulong _targetBits;
        /// <summary>Bit offset and value mask (15 or 7) of each vehicle in a packed state.</summary>
        readonly int[] _shift;
        readonly ulong[] _field;

        public TrafficLayout(int width, int height, IList<Vehicle> vehicles, ulong obstacles)
        {
            if (width < 2 || height < 2 || width * height > MaxCells) throw new ArgumentException($"grid {width}x{height}: 2..64 cells");
            if (vehicles.Count == 0 || vehicles.Count > MaxVehicles) throw new ArgumentException($"{vehicles.Count} vehicles: 1..{MaxVehicles}");
            Width = width;
            Height = height;
            Obstacles = obstacles;
            _v = new Vehicle[vehicles.Count];
            vehicles.CopyTo(_v, 0);
            Vehicles = _v;
            _masks = new ulong[_v.Length][];
            _shift = new int[_v.Length];
            _field = new ulong[_v.Length];
            int bits = 0;
            for (int i = 0; i < _v.Length; i++)
            {
                var v = _v[i];
                int lineLength = v.Horizontal ? width : height;
                if (v.Line < 0 || v.Line >= (v.Horizontal ? height : width) || v.Length > lineLength) throw new ArgumentException("vehicle outside the grid");
                int width4 = v.IsTarget ? 4 : 3;
                if (lineLength - v.Length > (v.IsTarget ? Exited - 1 : 7)) throw new ArgumentException("line too long for the packed state");
                _shift[i] = bits;
                _field[i] = v.IsTarget ? 15UL : 7UL;
                bits += width4;
                if (bits > 64) throw new ArgumentException($"{_v.Length} vehicles do not fit a packed state");
                if (v.IsTarget)
                {
                    if (i != TargetCount) throw new ArgumentException("target cars must come first");
                    if (v.Horizontal != (v.ExitSide == SquareTopology.Left || v.ExitSide == SquareTopology.Right))
                        throw new ArgumentException("a target car always faces its exit");
                    TargetCount++;
                    _targetBits |= 15UL << _shift[i];
                }
                _masks[i] = new ulong[lineLength - v.Length + 1];
                for (int p = 0; p < _masks[i].Length; p++)
                for (int k = 0; k < v.Length; k++)
                    _masks[i][p] |= 1UL << Cell(i, p + k);
            }
            if (TargetCount == 0) throw new ArgumentException("no target car");
        }

        public int VehicleCount => _v.Length;
        public int CellCount => Width * Height;

        /// <summary>The highest position of a vehicle (line length - its length).</summary>
        public int MaxPos(int vehicle) => _masks[vehicle].Length - 1;

        /// <summary>The cells covered by a vehicle at a position.</summary>
        public ulong Mask(int vehicle, int pos) => _masks[vehicle][pos];

        /// <summary>The grid cell at <paramref name="along"/> on the line of a vehicle.</summary>
        public int Cell(int vehicle, int along)
        {
            var v = _v[vehicle];
            return v.Horizontal ? v.Line * Width + along : along * Width + v.Line;
        }

        public int Count(VehicleKind kind)
        {
            int n = 0;
            foreach (var v in _v)
                if (v.Kind == kind) n++;
            return n;
        }

        public int ObstacleCount
        {
            get
            {
                int n = 0;
                for (ulong o = Obstacles; o != 0; o &= o - 1) n++;
                return n;
            }
        }

        /// <summary>Target cars whose exit is not on the top edge.</summary>
        public int SideExitCount
        {
            get
            {
                int n = 0;
                foreach (var v in _v)
                    if (v.IsTarget && v.ExitSide != SquareTopology.Up) n++;
                return n;
            }
        }

        // ------------------------------------------------------------------ packed states

        /// <summary>The position of a vehicle in a packed state (<see cref="Exited"/> for a target car that left).</summary>
        public int Pos(ulong state, int vehicle) => (int)((state >> _shift[vehicle]) & _field[vehicle]);

        public ulong With(ulong state, int vehicle, int pos) => (state & ~(_field[vehicle] << _shift[vehicle])) | ((ulong)pos << _shift[vehicle]);

        /// <summary>Every target car is out.</summary>
        public bool IsSolved(ulong state) => (state & _targetBits) == _targetBits;

        /// <summary>No target car is out yet (a possible start).</summary>
        public bool AllTargetsIn(ulong state)
        {
            for (int i = 0; i < TargetCount; i++)
                if (Pos(state, i) == Exited) return false;
            return true;
        }

        /// <summary>Obstacles and the cells of every vehicle still on the grid.</summary>
        public ulong Occupancy(ulong state)
        {
            ulong occ = Obstacles;
            for (int i = 0; i < _v.Length; i++)
            {
                int p = Pos(state, i);
                if (p != Exited) occ |= _masks[i][p];
            }
            return occ;
        }

        /// <summary>
        /// Where vehicle <paramref name="vehicle"/> can slide from its position (lowest and highest free position, its own position
        /// included) and whether it can leave through its exit (target cars only, every cell up to the exit free).
        /// </summary>
        public void Range(ulong state, int vehicle, out int lo, out int hi, out bool canExit) => Range(Occupancy(state), state, vehicle, out lo, out hi, out canExit);

        public void Range(ulong occupancy, ulong state, int vehicle, out int lo, out int hi, out bool canExit)
        {
            int p = Pos(state, vehicle);
            canExit = false;
            if (p == Exited)
            {
                lo = hi = Exited;
                return;
            }
            var m = _masks[vehicle];
            ulong free = ~(occupancy & ~m[p]);
            lo = p;
            while (lo > 0 && (m[lo - 1] & ~free) == 0) lo--;
            hi = p;
            while (hi < m.Length - 1 && (m[hi + 1] & ~free) == 0) hi++;
            var v = _v[vehicle];
            if (v.IsTarget) canExit = v.ExitsLow ? lo == 0 : hi == m.Length - 1;
        }

        /// <summary>
        /// Every legal move from <paramref name="state"/> in a fixed order (vehicles in order, positions ascending, the exit last), written
        /// into <paramref name="buffer"/> (at least <see cref="MaxMovesPerState"/> long). No moves after the level is solved.
        /// </summary>
        public int Moves(ulong state, TrafficMove[] buffer, bool exits = true)
        {
            if (IsSolved(state)) return 0;
            ulong occ = Occupancy(state);
            int n = 0;
            for (int i = 0; i < _v.Length; i++)
            {
                int p = Pos(state, i);
                if (p == Exited) continue;
                Range(occ, state, i, out int lo, out int hi, out bool canExit);
                for (int q = lo; q <= hi; q++)
                    if (q != p) buffer[n++] = new TrafficMove(i, p, q);
                if (canExit && exits) buffer[n++] = new TrafficMove(i, p, Exited);
            }
            return n;
        }

        public int MaxMovesPerState => _v.Length * 9;

        /// <summary>True when <paramref name="move"/> is legal in <paramref name="state"/>.</summary>
        public bool IsLegal(ulong state, TrafficMove move)
        {
            if (move.Vehicle < 0 || move.Vehicle >= _v.Length || IsSolved(state)) return false;
            int p = Pos(state, move.Vehicle);
            if (p == Exited || p != move.From || move.To == move.From) return false;
            Range(state, move.Vehicle, out int lo, out int hi, out bool canExit);
            return move.To == Exited ? canExit : move.To >= lo && move.To <= hi;
        }

        public ulong Apply(ulong state, TrafficMove move) => With(state, move.Vehicle, move.To);

        // ------------------------------------------------------------------ text form

        static readonly char[] s_sides = { 'U', 'R', 'D', 'L' };

        /// <summary>The level text: grid, vehicles at the positions of <paramref name="state"/> (exited ones are dropped), obstacles.</summary>
        public string ToText(ulong state)
        {
            var sb = new StringBuilder();
            sb.Append(Width).Append('x').Append(Height);
            for (int i = 0; i < _v.Length; i++)
            {
                int p = Pos(state, i);
                if (p == Exited) continue;
                var v = _v[i];
                int x = v.Horizontal ? p : v.Line, y = v.Horizontal ? v.Line : p;
                char kind = v.IsTarget ? (v.Target == 1 ? 'B' : 'R') : v.Kind == VehicleKind.Truck ? 'T' : 'C';
                sb.Append(' ').Append(kind).Append(x).Append('.').Append(y).Append(v.Horizontal ? 'h' : 'v');
                if (v.IsTarget) sb.Append(s_sides[v.ExitSide]);
            }
            for (int c = 0; c < CellCount; c++)
                if ((Obstacles >> c & 1) != 0) sb.Append(" #").Append(c % Width).Append('.').Append(c / Width);
            return sb.ToString();
        }

        /// <summary>Reads a level text: the layout and the start state.</summary>
        /// <exception cref="FormatException">Unreadable text.</exception>
        public static TrafficLayout Parse(string text, out ulong state)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("empty level");
            var parts = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var size = parts[0].Split('x');
            if (size.Length != 2 || !int.TryParse(size[0], out int w) || !int.TryParse(size[1], out int h)) throw new FormatException("bad size: " + parts[0]);
            var targets = new List<(Vehicle v, int pos)>();
            var others = new List<(Vehicle v, int pos)>();
            ulong obstacles = 0;
            for (int i = 1; i < parts.Length; i++)
            {
                string t = parts[i];
                char kind = t[0];
                int dot = t.IndexOf('.');
                if (dot < 2) throw new FormatException("bad token: " + t);
                int end = dot + 1;
                while (end < t.Length && char.IsDigit(t[end])) end++;
                if (!int.TryParse(t.Substring(1, dot - 1), out int x) || !int.TryParse(t.Substring(dot + 1, end - dot - 1), out int y))
                    throw new FormatException("bad cell: " + t);
                if (x < 0 || y < 0 || x >= w || y >= h) throw new FormatException("cell outside the grid: " + t);
                if (kind == '#')
                {
                    obstacles |= 1UL << (y * w + x);
                    continue;
                }
                if (end >= t.Length) throw new FormatException("missing direction: " + t);
                bool horizontal = t[end] == 'h';
                if (!horizontal && t[end] != 'v') throw new FormatException("bad direction: " + t);
                var v = new Vehicle { Horizontal = horizontal, Line = horizontal ? y : x };
                switch (kind)
                {
                    case 'R':
                    case 'B':
                        v.Kind = VehicleKind.Target;
                        v.Length = 2;
                        v.Target = kind == 'B' ? 1 : 0;
                        int side = end + 1 < t.Length ? Array.IndexOf(s_sides, t[end + 1]) : -1;
                        if (side < 0) throw new FormatException("missing exit side: " + t);
                        v.ExitSide = side;
                        targets.Add((v, horizontal ? x : y));
                        break;
                    case 'C':
                        v.Kind = VehicleKind.Car;
                        v.Length = 2;
                        others.Add((v, horizontal ? x : y));
                        break;
                    case 'T':
                        v.Kind = VehicleKind.Truck;
                        v.Length = 3;
                        others.Add((v, horizontal ? x : y));
                        break;
                    default: throw new FormatException("unknown vehicle: " + t);
                }
            }
            targets.Sort((a, b) => a.v.Target.CompareTo(b.v.Target));   // red first, then blue
            var all = new List<Vehicle>();
            var pos = new List<int>();
            foreach (var (v, p) in targets) { all.Add(v); pos.Add(p); }
            foreach (var (v, p) in others) { all.Add(v); pos.Add(p); }
            TrafficLayout layout;
            try { layout = new TrafficLayout(w, h, all, obstacles); }
            catch (ArgumentException e) { throw new FormatException(e.Message); }
            state = 0;
            ulong occ = obstacles;
            for (int i = 0; i < all.Count; i++)
            {
                if (pos[i] > layout.MaxPos(i)) throw new FormatException($"vehicle {i} outside the grid");
                ulong m = layout.Mask(i, pos[i]);
                if ((occ & m) != 0) throw new FormatException($"vehicle {i} overlaps another one or an obstacle");
                occ |= m;
                state = layout.With(state, i, pos[i]);
            }
            return layout;
        }
    }
}
