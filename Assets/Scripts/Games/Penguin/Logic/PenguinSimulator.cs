using System.Collections.Generic;

namespace ResolverMaster.Games.Penguin
{
    public enum PenguinStepResult
    {
        /// <summary>Nothing can move that way: no move (an empty swipe).</summary>
        None,
        Moved,
        /// <summary>A penguin fell into the water: the level is failed.</summary>
        Fell,
    }

    /// <summary>How the slide of one actor (penguin or ice block) ends.</summary>
    public enum PenguinTrackEnd : byte
    {
        /// <summary>Stopped against a rock, the wall, a block, the other penguin, a fish or an igloo (blocks).</summary>
        Bump,
        /// <summary>A penguin stopped because it pushed a block (zero length when it was already next to it).</summary>
        Push,
        /// <summary>Entered a snow cell and stopped there.</summary>
        Snow,
        /// <summary>A penguin went into its open igloo.</summary>
        Enter,
        /// <summary>A penguin fell into a water hole (<see cref="PenguinTrace.Track.To"/>).</summary>
        Fall,
        /// <summary>A penguin slid off the grid into the water of the border, beyond <see cref="PenguinTrace.Track.To"/>.</summary>
        FallOff,
        /// <summary>A block sank into a water hole and filled it (<see cref="PenguinTrace.Track.To"/> becomes ice).</summary>
        Sink,
    }

    /// <summary>
    /// What happened during one move, with its times (pure logic, for the animations): every actor slides at the same constant speed,
    /// one cell per time unit, from <c>From</c> at <c>T0</c> to <c>To</c> at <c>T1</c>; fish taken and the igloo opening at their time.
    /// Actors: 0-1 the penguins, 2-4 the ice blocks in the order of the packed state at the start of the move.
    /// </summary>
    public sealed class PenguinTrace
    {
        public struct Track
        {
            public int Actor;
            public int From, To;
            public int T0, T1;
            public PenguinTrackEnd End;
        }

        public enum EventKind
        {
            Fish,
            IglooOpen,
        }

        public struct Event
        {
            public EventKind Kind;
            /// <summary>The fish (for <see cref="EventKind.Fish"/>).</summary>
            public int Index;
            public int Time;
        }

        public readonly List<Track> Tracks = new List<Track>();
        public readonly List<Event> Events = new List<Event>();
        public int Dir;
        /// <summary>Time units until the last actor stops.</summary>
        public int Duration;
        public bool Fell;

        public void Clear()
        {
            Tracks.Clear();
            Events.Clear();
            Duration = 0;
            Fell = false;
        }
    }

    /// <summary>Which elements of a level were ever used (generator: levels with too many useless elements are discarded).</summary>
    public sealed class PenguinUse
    {
        public readonly bool[] Rock, Snow, Water, Edge;
        public bool BlockPushed;

        public PenguinUse(PenguinLayout layout)
        {
            Rock = new bool[layout.CellCount];
            Snow = new bool[layout.CellCount];
            Water = new bool[layout.CellCount];
            Edge = new bool[layout.EdgeCount];
        }
    }

    /// <summary>
    /// The rules of one swipe (PG-001), pure logic: every penguin slides in the swipe direction at the same time and the same speed, one
    /// cell per tick; in each tick the actors in front move first (so two penguins in a line keep their gap and stop one against the
    /// other). A penguin stops against a rock, the snow wall of the border, a block that cannot move, the other penguin; it stops IN a snow
    /// cell; it falls into water (a hole or the water of the border); it takes the fish it passes over; it enters its igloo as soon as it
    /// passes over it with every fish taken (before: it slides over it like ice), and disappears. A penguin that hits a block stops before
    /// it and the block slides on in the same direction (penguins only push blocks, never a block another block), until a rock, the border
    /// (wall or water), another block, a penguin, a fish not yet taken or an igloo stops it; it stops in snow; in a water hole it sinks and
    /// the hole becomes ice. Not thread-safe: one simulator per search (buffers reused, no allocation per step).
    /// </summary>
    public sealed class PenguinSimulator
    {
        const int Actors = 5, FirstBlock = 2;

        readonly PenguinLayout _l;
        readonly int[] _occ;
        readonly int[] _pos = new int[Actors];
        readonly bool[] _moving = new bool[Actors];
        readonly bool[] _done = new bool[Actors];
        readonly int[] _from = new int[Actors];
        readonly int[] _t0 = new int[Actors];
        readonly int[] _order = new int[Actors];
        int _fish, _holes, _time, _dir;
        bool _fell;
        PenguinTrace _trace;
        PenguinUse _use;

        /// <summary>Generator check: blocks never move (they act as rocks).</summary>
        public bool FreezeBlocks;
        /// <summary>Generator check: blocks never sink (a water hole stops them like a wall).</summary>
        public bool NoFill;

        public PenguinLayout Layout => _l;

        public PenguinSimulator(PenguinLayout layout)
        {
            _l = layout;
            _occ = new int[layout.CellCount];
            for (int i = 0; i < _occ.Length; i++) _occ[i] = -1;
        }

        /// <summary>
        /// Applies one swipe to <paramref name="state"/>. <paramref name="trace"/> (optional) receives the slides and their times;
        /// <paramref name="use"/> (optional) marks the elements touched.
        /// </summary>
        public PenguinStepResult Step(ulong state, int dir, out ulong next, PenguinTrace trace = null, PenguinUse use = null)
        {
            _trace = trace;
            _use = use;
            _dir = dir;
            trace?.Clear();
            if (trace != null) trace.Dir = dir;
            _fish = PenguinLayout.FishMask(state);
            _holes = PenguinLayout.HoleMask(state);
            _time = 0;
            _fell = false;
            for (int a = 0; a < Actors; a++)
            {
                _pos[a] = -1;
                _moving[a] = false;
            }
            for (int i = 0; i < _l.PenguinCount; i++)
            {
                int p = PenguinLayout.PenguinPos(state, i);
                if (p == PenguinLayout.Gone) continue;
                _pos[i] = p;
                _moving[i] = true;
                _from[i] = p;
                _t0[i] = 0;
            }
            for (int j = 0; j < _l.BlockCount; j++)
            {
                int b = PenguinLayout.BlockPos(state, j);
                if (b != PenguinLayout.Gone) _pos[FirstBlock + j] = b;
            }
            for (int a = 0; a < Actors; a++)
                if (_pos[a] >= 0) _occ[_pos[a]] = a;

            while (true)
            {
                int count = 0;
                for (int a = 0; a < Actors; a++)
                    if (_moving[a]) _order[count++] = a;
                if (count == 0) break;
                _time++;
                // the actors in front move first (insertion sort, at most 5)
                for (int i = 1; i < count; i++)
                {
                    int a = _order[i], k = i - 1;
                    while (k >= 0 && Ahead(a, _order[k])) { _order[k + 1] = _order[k]; k--; }
                    _order[k + 1] = a;
                }
                for (int a = 0; a < Actors; a++) _done[a] = false;
                for (int i = 0; i < count; i++)
                {
                    int a = _order[i];
                    if (_moving[a] && !_done[a]) Advance(a);
                }
            }
            // a penguin standing on its igloo when the last fish was taken goes in
            if (_fish == _l.AllFishMask)
                for (int i = 0; i < _l.PenguinCount; i++)
                    if (_pos[i] >= 0 && _l.Igloo(i) == _pos[i])
                    {
                        _from[i] = _pos[i];
                        _t0[i] = _time;
                        Stop(i, PenguinTrackEnd.Enter, _pos[i], _time);
                        _occ[_pos[i]] = -1;
                        _pos[i] = -1;
                    }

            for (int a = 0; a < Actors; a++)
                if (_pos[a] >= 0) _occ[_pos[a]] = -1;
            ulong s = 0;
            for (int i = 0; i < PenguinLayout.MaxPenguins; i++)
                s |= (ulong)(i < _l.PenguinCount && _pos[i] >= 0 ? _pos[i] : PenguinLayout.Gone) << (PenguinLayout.PenguinShift + 6 * i);
            int b0 = Block(0), b1 = Block(1), b2 = Block(2);
            if (b1 < b0) (b0, b1) = (b1, b0);
            if (b2 < b1) (b1, b2) = (b2, b1);
            if (b1 < b0) (b0, b1) = (b1, b0);
            s |= (ulong)b0 << PenguinLayout.BlockShift;
            s |= (ulong)b1 << (PenguinLayout.BlockShift + 6);
            s |= (ulong)b2 << (PenguinLayout.BlockShift + 12);
            s |= (ulong)(uint)_fish << PenguinLayout.FishShift;
            s |= (ulong)(uint)_holes << PenguinLayout.HoleShift;
            next = s;
            if (trace != null)
            {
                trace.Fell = _fell;
                int d = 0;
                foreach (var t in trace.Tracks) d = t.T1 > d ? t.T1 : d;
                trace.Duration = d;
            }
            _trace = null;
            _use = null;
            if (_fell) return PenguinStepResult.Fell;
            return next == state ? PenguinStepResult.None : PenguinStepResult.Moved;
        }

        int Block(int j) => j < _l.BlockCount && _pos[FirstBlock + j] >= 0 ? _pos[FirstBlock + j] : PenguinLayout.Gone;

        /// <summary>Actor <paramref name="a"/> is further in the swipe direction than <paramref name="b"/> (ties: lower index first).</summary>
        bool Ahead(int a, int b)
        {
            int da = Along(_pos[a]), db = Along(_pos[b]);
            return da > db || (da == db && a < b);
        }

        int Along(int cell) => _l.X(cell) * PenguinLayout.Dx(_dir) + _l.Y(cell) * PenguinLayout.Dy(_dir);

        /// <summary>One tick of actor <paramref name="a"/>: one cell forward, or the end of its slide.</summary>
        void Advance(int a)
        {
            _done[a] = true;
            bool penguin = a < FirstBlock;
            int cur = _pos[a];
            int n = _l.Neighbor(cur, _dir);
            if (n < 0)
            {
                if (penguin && _l.EdgeWaterAt(cur, _dir))
                {
                    if (_use != null) _use.Edge[_l.EdgeIndex(cur, _dir)] = true;
                    _fell = true;
                    Stop(a, PenguinTrackEnd.FallOff, cur, _time - 1);
                    _occ[cur] = -1;
                    _pos[a] = -1;
                }
                else Stop(a, PenguinTrackEnd.Bump, cur, _time - 1);
                return;
            }
            var terrain = _l.Terrain(n);
            if (terrain == PenguinTerrain.Rock)
            {
                if (_use != null) _use.Rock[n] = true;
                Stop(a, PenguinTrackEnd.Bump, cur, _time - 1);
                return;
            }
            int other = _occ[n];
            if (other >= 0)
            {
                if (penguin && other >= FirstBlock && !_moving[other] && !FreezeBlocks)
                {
                    // the penguin stops and the block slides on in the same direction (or stays, like a rock)
                    _moving[other] = true;
                    _from[other] = n;
                    _t0[other] = _time - 1;
                    Advance(other);
                    bool pushed = _pos[other] != n;
                    if (pushed && _use != null) _use.BlockPushed = true;
                    Stop(a, pushed ? PenguinTrackEnd.Push : PenguinTrackEnd.Bump, cur, _time - 1);
                }
                else Stop(a, PenguinTrackEnd.Bump, cur, _time - 1);
                return;
            }
            if (!penguin && ((_l.FishAt(n) >= 0 && (_fish & (1 << _l.FishAt(n))) == 0) || _l.IglooAt(n) >= 0))
            {
                Stop(a, PenguinTrackEnd.Bump, cur, _time - 1);
                return;
            }
            if (terrain == PenguinTerrain.Water)
            {
                int hole = _l.HoleAt(n);
                if (hole < 0 || (_holes & (1 << hole)) == 0)
                {
                    if (_use != null) _use.Water[n] = true;
                    if (penguin)
                    {
                        _fell = true;
                        _occ[cur] = -1;
                        _pos[a] = n;
                        Stop(a, PenguinTrackEnd.Fall, n, _time);
                        _pos[a] = -1;
                    }
                    else if (NoFill || hole < 0) Stop(a, PenguinTrackEnd.Bump, cur, _time - 1);
                    else
                    {
                        _holes |= 1 << hole;
                        _occ[cur] = -1;
                        _pos[a] = n;
                        Stop(a, PenguinTrackEnd.Sink, n, _time);
                        _pos[a] = -1;
                    }
                    return;
                }
            }
            _occ[cur] = -1;
            _occ[n] = a;
            _pos[a] = n;
            if (penguin)
            {
                int fish = _l.FishAt(n);
                if (fish >= 0 && (_fish & (1 << fish)) == 0)
                {
                    _fish |= 1 << fish;
                    _trace?.Events.Add(new PenguinTrace.Event { Kind = PenguinTrace.EventKind.Fish, Index = fish, Time = _time });
                    if (_fish == _l.AllFishMask) _trace?.Events.Add(new PenguinTrace.Event { Kind = PenguinTrace.EventKind.IglooOpen, Time = _time });
                }
                if (_l.Igloo(a) == n && _fish == _l.AllFishMask)
                {
                    Stop(a, PenguinTrackEnd.Enter, n, _time);
                    _occ[n] = -1;
                    _pos[a] = -1;
                    return;
                }
            }
            if (terrain == PenguinTerrain.Snow)
            {
                if (_use != null) _use.Snow[n] = true;
                Stop(a, PenguinTrackEnd.Snow, n, _time);
            }
        }

        /// <summary>Ends the slide of <paramref name="a"/> at <paramref name="cell"/> at <paramref name="time"/> (a block that did not move leaves no track).</summary>
        void Stop(int a, PenguinTrackEnd end, int cell, int time)
        {
            _moving[a] = false;
            if (_trace == null) return;
            bool moved = _from[a] != cell || time > _t0[a];
            if (!moved && a >= FirstBlock) return;
            if (!moved && end == PenguinTrackEnd.Bump) return;   // a penguin that could not move at all
            _trace.Tracks.Add(new PenguinTrace.Track { Actor = a, From = _from[a], To = cell, T0 = _t0[a], T1 = time, End = end });
        }
    }
}
