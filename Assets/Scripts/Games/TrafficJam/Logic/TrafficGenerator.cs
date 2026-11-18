using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>
    /// Builds a Traffic jam level from a seed inside a difficulty tier (TJ-001), with the Core <see cref="SeededRandom"/>:
    /// 1. random layout: the target cars (red, then blue) on their lines facing their exits (top edge, or another side when the tier asks for
    ///    side exits), then obstacles, trucks and cars at random free places. Nothing else may share the line of a target car in its
    ///    direction, and no obstacle stands on it (they would close the exit for good);
    /// 2. every reachable position and its distance to the end (<see cref="TrafficSpace"/>); layouts with more positions than the cap, or
    ///    with no way out, are discarded;
    /// 3. the start is the position farthest from the end with every target car in, within the tier's maximum (ties: picked by the
    ///    random sequence);
    /// 4. while the start is easier than the tier's minimum, one vehicle (or obstacle) of the layout moves to another random free place
    ///    and steps 2-3 run again: the change is kept when the level does not get easier (hill climbing); after
    ///    <see cref="Settings.ClimbSteps"/> changes a new random layout starts.
    /// Every exploration counts as one attempt; the limit is a number of attempts, never a time. Same seed + same tier = same level on
    /// every platform.
    /// </summary>
    public static class TrafficGenerator
    {
        public sealed class Settings
        {
            public int Width = 4, Height = 6;
            public DifficultyRange MinMoves = new DifficultyRange(TrafficDifficulty.MinMoves, 1, 40);
            public DifficultyRange Cars = new DifficultyRange(TrafficDifficulty.Cars, 2, 2);
            public DifficultyRange Trucks = new DifficultyRange(TrafficDifficulty.Trucks, 0, 0);
            public DifficultyRange Obstacles = new DifficultyRange(TrafficDifficulty.Obstacles, 0, 0);
            /// <summary>Target cars whose exit is not on top.</summary>
            public DifficultyRange SideExits = new DifficultyRange(TrafficDifficulty.SideExits, 0, 0);
            /// <summary>1 (red) or 2 (red and blue).</summary>
            public int Targets = 1;
            /// <summary>Explorations (layouts tried) before giving up.</summary>
            public int MaxAttempts = 400;
            /// <summary>Layouts with more reachable positions are discarded.</summary>
            public int MaxStates = TrafficSpace.DefaultMaxStates;
            /// <summary>The highest minimum moves of any level (about 40).</summary>
            public int MaxMinMoves = 40;
            /// <summary>Changes tried on one random layout before starting again from a new one.</summary>
            public int ClimbSteps = 60;
            /// <summary>Optional: counts why the explored layouts were discarded (Editor tools, measurements).</summary>
            public Stats Counters;

            public static Settings FromTier(DifficultyTier tier, int maxAttempts, int maxStates, int maxMinMoves, int climbSteps = 60) => new Settings
            {
                Width = tier.Get(TrafficDifficulty.Cols).min,
                Height = tier.Get(TrafficDifficulty.Rows).min,
                MinMoves = tier.Get(TrafficDifficulty.MinMoves),
                Cars = tier.GetOr(TrafficDifficulty.Cars, 0),
                Trucks = tier.GetOr(TrafficDifficulty.Trucks, 0),
                Obstacles = tier.GetOr(TrafficDifficulty.Obstacles, 0),
                SideExits = tier.GetOr(TrafficDifficulty.SideExits, 0),
                Targets = Math.Max(1, Math.Min(2, tier.GetOr(TrafficDifficulty.Targets, 1).min)),
                MaxAttempts = maxAttempts,
                MaxStates = maxStates,
                MaxMinMoves = maxMinMoves,
                ClimbSteps = climbSteps,
            };
        }

        /// <summary>Why explored layouts were discarded (filled only when <see cref="Settings.Counters"/> is set).</summary>
        public sealed class Stats
        {
            public int Explored, Capped, NoWayOut, Kept, Misses;
            public long States;

            public override string ToString() => $"explored {Explored} (capped {Capped}, no way out {NoWayOut}, kept {Kept}), misfits {Misses}, positions {States}";
        }

        public sealed class Result
        {
            public TrafficLevel Level;
            public TrafficLayout Layout;
            public ulong Start;
            /// <summary>Reachable positions of the level.</summary>
            public int States;
            public int Attempts;
        }

        /// <exception cref="InvalidOperationException">No level within <see cref="Settings.MaxAttempts"/>.</exception>
        public static Result Generate(Settings s, int seed, string tierId = "", int number = 0)
        {
            int lo = Math.Max(1, s.MinMoves.min), hi = Math.Min(s.MinMoves.max, s.MaxMinMoves);
            if (hi < lo) throw new ArgumentException($"minimum moves {lo}..{hi}: empty range");
            var rng = new SeededRandom(seed);
            // attempts = explorations; layouts or changes that do not fit (cheap, nothing explored) are bounded apart
            int attempts = 0, misses = 0, maxMisses = 20 * Math.Max(1, s.MaxAttempts);
            while (attempts < s.MaxAttempts && misses < maxMisses)
            {
                // a fresh random layout of the tier...
                var draft = Draft.Random(rng, s);
                if (draft == null)
                {
                    misses++;
                    if (s.Counters != null) s.Counters.Misses++;
                    continue;
                }
                var best = Evaluate(draft, s, hi, rng, ref attempts);
                // ...then changed one vehicle at a time: a change is kept when the level does not get easier
                for (int step = 0; step < s.ClimbSteps && (best == null || best.Level.minMoves < lo) && attempts < s.MaxAttempts && misses < maxMisses; step++)
                {
                    var next = draft.Mutate(rng);
                    if (next == null)
                    {
                        misses++;
                        if (s.Counters != null) s.Counters.Misses++;
                        continue;
                    }
                    var r = Evaluate(next, s, hi, rng, ref attempts);
                    if (r == null || (best != null && r.Level.minMoves < best.Level.minMoves)) continue;
                    draft = next;
                    best = r;
                    if (s.Counters != null) s.Counters.Kept++;
                }
                if (best == null || best.Level.minMoves < lo) continue;
                best.Attempts = attempts;
                best.Level.number = number;
                best.Level.tier = tierId;
                best.Level.seed = seed;
                return best;
            }
            throw new InvalidOperationException($"Traffic jam generator: no level {s.Width}x{s.Height} with minimum {lo}..{hi} from seed {seed} in {s.MaxAttempts} attempts");
        }

        /// <summary>
        /// One attempt: every reachable position of the layout; the start = the farthest position from the end with every target car in,
        /// at most <paramref name="hi"/> moves (ties: picked by the random sequence). Null when capped or with no way out.
        /// </summary>
        static Result Evaluate(Draft draft, Settings s, int hi, SeededRandom rng, ref int attempts)
        {
            attempts++;
            var layout = draft.Build(out ulong state);
            var space = TrafficSpace.Explore(layout, state, s.MaxStates);
            var st = s.Counters;
            if (st != null)
            {
                st.Explored++;
                st.States += space.Count;
                if (space.Capped) st.Capped++;
                else if (!space.Solvable) st.NoWayOut++;
            }
            if (space.Capped || !space.Solvable) return null;
            int best = -1, picks = 0;
            for (int i = 0; i < space.Count; i++)
            {
                int d = space.DistanceAt(i);
                if (d > hi || d < best || d <= 0 || !layout.AllTargetsIn(space.StateAt(i))) continue;
                if (d > best)
                {
                    best = d;
                    picks = 0;
                }
                picks++;
            }
            if (best < 0) return null;
            int pick = rng.Next(picks);
            ulong start = 0;
            for (int i = 0; i < space.Count; i++)
                if (space.DistanceAt(i) == best && layout.AllTargetsIn(space.StateAt(i)) && pick-- == 0)
                {
                    start = space.StateAt(i);
                    break;
                }
            return new Result
            {
                Layout = layout, Start = start, States = space.Count,
                Level = new TrafficLevel { width = layout.Width, height = layout.Height, minMoves = best, states = space.Count, cells = layout.ToText(start) },
            };
        }

        static readonly int[] s_sideExits = { SquareTopology.Right, SquareTopology.Down, SquareTopology.Left };

        /// <summary>A layout being built: the vehicles (target cars first) with their positions, and the obstacle cells.</summary>
        sealed class Draft
        {
            public int W, H, Targets;
            public readonly List<Vehicle> Vehicles = new List<Vehicle>();
            public readonly List<int> Positions = new List<int>();
            public readonly List<int> Obstacles = new List<int>();

            public TrafficLayout Build(out ulong state)
            {
                ulong obstacles = 0;
                foreach (int c in Obstacles) obstacles |= 1UL << c;
                var layout = new TrafficLayout(W, H, Vehicles, obstacles);
                state = 0;
                for (int i = 0; i < Positions.Count; i++) state = layout.With(state, i, Positions[i]);
                return layout;
            }

            /// <summary>Lines of the target cars (no other vehicle in their direction there) and the cells where no obstacle may stand.</summary>
            void Locks(out int lockedCols, out int lockedRows, out ulong noObstacle)
            {
                lockedCols = lockedRows = 0;
                noObstacle = 0;
                for (int t = 0; t < Targets; t++)
                {
                    var v = Vehicles[t];
                    if (v.Horizontal)
                    {
                        lockedRows |= 1 << v.Line;
                        for (int x = 0; x < W; x++) noObstacle |= 1UL << (v.Line * W + x);
                    }
                    else
                    {
                        lockedCols |= 1 << v.Line;
                        for (int y = 0; y < H; y++) noObstacle |= 1UL << (y * W + v.Line);
                    }
                }
            }

            ulong CellsOf(int i)
            {
                var v = Vehicles[i];
                ulong m = 0;
                for (int k = 0; k < v.Length; k++) m |= 1UL << (v.Horizontal ? v.Line * W + Positions[i] + k : (Positions[i] + k) * W + v.Line);
                return m;
            }

            /// <summary>The cells taken by everything except vehicle <paramref name="skip"/> and obstacle <paramref name="skipObstacle"/>.</summary>
            ulong Occupied(int skip, int skipObstacle)
            {
                ulong occ = 0;
                for (int i = 0; i < Vehicles.Count; i++)
                    if (i != skip) occ |= CellsOf(i);
                for (int i = 0; i < Obstacles.Count; i++)
                    if (i != skipObstacle) occ |= 1UL << Obstacles[i];
                return occ;
            }

            Draft Copy()
            {
                var d = new Draft { W = W, H = H, Targets = Targets };
                foreach (var v in Vehicles)
                    d.Vehicles.Add(new Vehicle { Kind = v.Kind, Length = v.Length, Horizontal = v.Horizontal, Line = v.Line, Target = v.Target, ExitSide = v.ExitSide });
                d.Positions.AddRange(Positions);
                d.Obstacles.AddRange(Obstacles);
                return d;
            }

            /// <summary>The same layout with one vehicle or obstacle moved to another random free place; null when it did not fit.</summary>
            public Draft Mutate(SeededRandom rng)
            {
                var d = Copy();
                int others = d.Vehicles.Count - d.Targets;
                int pick = rng.Next(others + d.Obstacles.Count + d.Targets);
                if (pick < others)
                {
                    // another vehicle: anywhere, either direction
                    int i = d.Targets + pick;
                    d.Locks(out int lc, out int lr, out _);
                    ulong occ = d.Occupied(i, -1);
                    var v = d.Vehicles[i];
                    v.Horizontal = rng.Chance(0.5);
                    if (!Place(rng, d.W, d.H, v, ref occ, lc, lr, 1, out int pos, 40)) return null;
                    d.Positions[i] = pos;
                    return d;
                }
                pick -= others;
                if (pick < d.Obstacles.Count)
                {
                    d.Locks(out _, out _, out ulong noObstacle);
                    ulong occ = d.Occupied(-1, pick);
                    for (int tries = 0; tries < 40; tries++)
                    {
                        int c = rng.Next(d.W * d.H);
                        if (((occ | noObstacle) >> c & 1) != 0) continue;
                        d.Obstacles[pick] = c;
                        return d;
                    }
                    return null;
                }
                // a target car: another place on its line (its line and its exit stay)
                int t = pick - d.Obstacles.Count;
                var tv = d.Vehicles[t];
                ulong taken = d.Occupied(t, -1);
                int length = tv.Horizontal ? d.W : d.H;
                for (int tries = 0; tries < 20; tries++)
                {
                    int p = rng.Next(length - tv.Length + 1);
                    ulong mask = 0;
                    for (int k = 0; k < tv.Length; k++) mask |= 1UL << (tv.Horizontal ? tv.Line * d.W + p + k : (p + k) * d.W + tv.Line);
                    if ((taken & mask) != 0) continue;
                    d.Positions[t] = p;
                    return d;
                }
                return null;
            }

            /// <summary>A random layout of the tier (null when the vehicles do not fit).</summary>
            public static Draft Random(SeededRandom rng, Settings s)
            {
                int w = s.Width, h = s.Height;
                int targets = s.Targets;
                var d = new Draft { W = w, H = h, Targets = targets };
                ulong occ = 0;
                int lockedCols = 0, lockedRows = 0;

                // 1. target cars: which ones exit on a side, then each on a free line facing its exit
                int sides = rng.Range(Math.Max(0, Math.Min(s.SideExits.min, targets)), Math.Max(0, Math.Min(s.SideExits.max, targets)));
                var order = new List<int>();
                for (int t = 0; t < targets; t++) order.Add(t);
                rng.Shuffle(order);
                var exitSide = new int[targets];
                for (int k = 0; k < targets; k++) exitSide[order[k]] = k < sides ? s_sideExits[rng.Next(s_sideExits.Length)] : SquareTopology.Up;
                for (int t = 0; t < targets; t++)
                {
                    bool horizontal = exitSide[t] == SquareTopology.Left || exitSide[t] == SquareTopology.Right;
                    var v = new Vehicle { Kind = VehicleKind.Target, Length = 2, Horizontal = horizontal, Target = t, ExitSide = exitSide[t] };
                    if (!Place(rng, w, h, v, ref occ, lockedCols, lockedRows, 0, out int pos, 40)) return null;
                    if (horizontal) lockedRows |= 1 << v.Line;
                    else lockedCols |= 1 << v.Line;
                    d.Vehicles.Add(v);
                    d.Positions.Add(pos);
                }
                d.Locks(out _, out _, out ulong noObstacle);

                // 2. obstacles off the target lines
                int obstacles = rng.Range(s.Obstacles.min, s.Obstacles.max);
                for (int i = 0; i < obstacles; i++)
                {
                    bool placed = false;
                    for (int tries = 0; tries < 60 && !placed; tries++)
                    {
                        int c = rng.Next(w * h);
                        ulong bit = 1UL << c;
                        if (((occ | noObstacle) & bit) != 0) continue;
                        occ |= bit;
                        d.Obstacles.Add(c);
                        placed = true;
                    }
                    if (!placed) return null;
                }

                // 3. trucks, then cars
                int trucks = rng.Range(s.Trucks.min, s.Trucks.max), cars = rng.Range(s.Cars.min, s.Cars.max);
                if (4 * targets + 3 * (trucks + cars) > 64) return null;   // the packed state holds 64 bits
                for (int i = 0; i < trucks + cars; i++)
                {
                    var v = new Vehicle { Kind = i < trucks ? VehicleKind.Truck : VehicleKind.Car, Length = i < trucks ? 3 : 2, Horizontal = rng.Chance(0.5) };
                    if (!Place(rng, w, h, v, ref occ, lockedCols, lockedRows, 1, out int pos, 80)) return null;
                    d.Vehicles.Add(v);
                    d.Positions.Add(pos);
                }
                return d;
            }
        }

        /// <summary>A free line and position for <paramref name="v"/> (random; <paramref name="flip"/> 1 = may try the other direction too).</summary>
        static bool Place(SeededRandom rng, int w, int h, Vehicle v, ref ulong occ, int lockedCols, int lockedRows, int flip, out int pos, int tries)
        {
            pos = 0;
            for (int k = 0; k < tries; k++)
            {
                if (flip > 0 && k > 0 && rng.Chance(0.3)) v.Horizontal = !v.Horizontal;
                int lines = v.Horizontal ? h : w, length = v.Horizontal ? w : h;
                if (v.Length > length) continue;
                int line = rng.Next(lines);
                if (v.Horizontal ? (lockedRows >> line & 1) != 0 : (lockedCols >> line & 1) != 0) continue;
                int p = rng.Next(length - v.Length + 1);
                ulong mask = 0;
                for (int c = 0; c < v.Length; c++) mask |= 1UL << (v.Horizontal ? line * w + p + c : (p + c) * w + line);
                if ((occ & mask) != 0) continue;
                occ |= mask;
                v.Line = line;
                pos = p;
                return true;
            }
            return false;
        }
    }
}
