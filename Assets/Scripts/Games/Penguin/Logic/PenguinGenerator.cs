using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>
    /// Builds a Pingù level from a seed inside a difficulty tier (PG-001), with the Core <see cref="SeededRandom"/>:
    /// 1. random draft: igloos and penguins, then rocks, snow, water holes, fish and ice blocks on random free cells, and runs of water on
    ///    the border, as many as the tier asks;
    /// 2. every reachable state and the exact minimum (<see cref="PenguinSpace"/>); drafts with more states than the cap, or with no
    ///    solution, are discarded;
    /// 3. while the level is easier than the tier's minimum, one element moves to another free cell and step 2 runs again: the change is
    ///    kept when the level does not get easier and stays within the maximum (hill climbing); after <see cref="Settings.ClimbSteps"/>
    ///    changes a new draft starts;
    /// 4. teaching levels (<see cref="Settings.FishOnPath"/>): the level is found without fish, then the fish are put on cells the
    ///    penguin slides over in its solution, keeping the same minimum: the player takes them without thinking;
    /// 5. the level must use the novelty of its tier and must not have too many useless elements (<see cref="Problems"/>).
    /// Every exploration counts as one attempt; the limit is a number of attempts, never a time. Same seed + same tier = same level.
    /// </summary>
    public static class PenguinGenerator
    {
        public sealed class Settings
        {
            public int Width = 4, Height = 6;
            public DifficultyRange MinMoves = new DifficultyRange(PenguinDifficulty.MinMoves, 1, 30);
            public DifficultyRange Rocks = new DifficultyRange(PenguinDifficulty.Rocks, 2, 4);
            public DifficultyRange Snow = new DifficultyRange(PenguinDifficulty.Snow, 0, 0);
            public DifficultyRange Holes = new DifficultyRange(PenguinDifficulty.Holes, 0, 0);
            public DifficultyRange WaterEdges = new DifficultyRange(PenguinDifficulty.WaterEdges, 0, 0);
            public DifficultyRange Fish = new DifficultyRange(PenguinDifficulty.Fish, 0, 0);
            public DifficultyRange Blocks = new DifficultyRange(PenguinDifficulty.Blocks, 0, 0);
            public int Penguins = 1;
            public bool NeedSnow, NeedPush, NeedFill, FishOnPath;
            /// <summary>States of an optimal solution from which a swipe falls into the water, at least.</summary>
            public int Danger;
            public int MaxUseless = 2;
            /// <summary>Explorations (drafts tried) before giving up.</summary>
            public int MaxAttempts = 300;
            /// <summary>Drafts with more reachable states are discarded.</summary>
            public int MaxStates = 30000;
            public int MaxMinMoves = 30;
            /// <summary>Changes tried on one draft before starting again from a new one.</summary>
            public int ClimbSteps = 40;
            /// <summary>Optional: why the drafts were discarded (Editor tools).</summary>
            public Stats Counters;

            public static Settings FromTier(DifficultyTier tier, int maxAttempts, int maxStates, int maxMinMoves, int climbSteps) => new Settings
            {
                Width = tier.Get(PenguinDifficulty.Cols).min,
                Height = tier.Get(PenguinDifficulty.Rows).min,
                MinMoves = tier.Get(PenguinDifficulty.MinMoves),
                Rocks = tier.GetOr(PenguinDifficulty.Rocks, 0),
                Snow = tier.GetOr(PenguinDifficulty.Snow, 0),
                Holes = tier.GetOr(PenguinDifficulty.Holes, 0),
                WaterEdges = tier.GetOr(PenguinDifficulty.WaterEdges, 0),
                Fish = tier.GetOr(PenguinDifficulty.Fish, 0),
                Blocks = tier.GetOr(PenguinDifficulty.Blocks, 0),
                Penguins = Math.Max(1, Math.Min(2, tier.GetOr(PenguinDifficulty.Penguins, 1).min)),
                NeedSnow = tier.GetOr(PenguinDifficulty.NeedSnow, 0).min > 0,
                Danger = tier.GetOr(PenguinDifficulty.Danger, 0).min,
                NeedPush = tier.GetOr(PenguinDifficulty.NeedPush, 0).min > 0,
                NeedFill = tier.GetOr(PenguinDifficulty.NeedFill, 0).min > 0,
                FishOnPath = tier.GetOr(PenguinDifficulty.FishOnPath, 0).min > 0,
                MaxUseless = tier.GetOr(PenguinDifficulty.Useless, 2).min,
                MaxAttempts = maxAttempts,
                MaxStates = maxStates,
                MaxMinMoves = maxMinMoves,
                ClimbSteps = climbSteps,
            };
        }

        /// <summary>Why explored drafts were discarded (filled only when <see cref="Settings.Counters"/> is set).</summary>
        public sealed class Stats
        {
            public int Explored, Capped, Unsolvable, OutOfRange, Rejected, Misses;
            public long States;
            public readonly Dictionary<string, int> Reasons = new Dictionary<string, int>();

            public override string ToString()
            {
                var sb = new System.Text.StringBuilder($"explored {Explored} (capped {Capped}, unsolvable {Unsolvable}, out of range {OutOfRange}, rejected {Rejected}), misfits {Misses}, states {States}");
                foreach (var kv in Reasons) sb.Append($"; {kv.Key} {kv.Value}");
                return sb.ToString();
            }
        }

        public sealed class Result
        {
            public PenguinLevel Level;
            public PenguinLayout Layout;
            public int States;
            public int Attempts;
        }

        sealed class Eval
        {
            public PenguinLayout Layout;
            public PenguinSpace Space;
            public int Min;
        }

        /// <exception cref="InvalidOperationException">No level within <see cref="Settings.MaxAttempts"/>.</exception>
        public static Result Generate(Settings s, int seed, string tierId = "", int number = 0)
        {
            int lo = Math.Max(1, s.MinMoves.min), hi = Math.Min(s.MinMoves.max, s.MaxMinMoves);
            if (hi < lo) throw new ArgumentException($"minimum moves {lo}..{hi}: empty range");
            var rng = new SeededRandom(seed);
            int attempts = 0, misses = 0, maxMisses = 20 * Math.Max(1, s.MaxAttempts);
            while (attempts < s.MaxAttempts && misses < maxMisses)
            {
                var draft = Draft.Random(rng, s, out int fishWanted);
                if (draft == null)
                {
                    misses++;
                    if (s.Counters != null) s.Counters.Misses++;
                    continue;
                }
                var best = Evaluate(draft, s, ref attempts);
                if (best != null && best.Min > hi) best = null;
                for (int step = 0; step < s.ClimbSteps && (best == null || best.Min < lo) && attempts < s.MaxAttempts; step++)
                {
                    var next = draft.Mutate(rng);
                    if (next == null)
                    {
                        misses++;
                        continue;
                    }
                    var e = Evaluate(next, s, ref attempts);
                    if (e == null || e.Min > hi || (best != null && e.Min < best.Min)) continue;
                    draft = next;
                    best = e;
                }
                if (best == null || best.Min < lo || best.Min > hi)
                {
                    if (s.Counters != null) s.Counters.OutOfRange++;
                    continue;
                }
                if (fishWanted > 0)
                {
                    best = PlaceFishOnPath(draft, best, fishWanted, rng, s, ref attempts);
                    if (best == null)
                    {
                        Count(s, "fish not on the way");
                        continue;
                    }
                }
                var problems = Problems(best.Layout, best.Space, s);
                if (problems.Count > 0)
                {
                    if (s.Counters != null) s.Counters.Rejected++;
                    foreach (var p in problems) Count(s, p.Split(':')[0]);
                    continue;
                }
                return new Result
                {
                    Layout = best.Layout,
                    States = best.Space.Count,
                    Attempts = attempts,
                    Level = new PenguinLevel
                    {
                        number = number, tier = tierId, seed = seed, width = s.Width, height = s.Height, minMoves = best.Min,
                        states = best.Space.Count, cells = best.Layout.Format(),
                    },
                };
            }
            throw new InvalidOperationException($"Pingù: no level for tier '{tierId}' from seed {seed} in {attempts} attempts");
        }

        static void Count(Settings s, string reason)
        {
            if (s.Counters == null) return;
            s.Counters.Reasons.TryGetValue(reason, out int n);
            s.Counters.Reasons[reason] = n + 1;
        }

        static Eval Evaluate(Draft d, Settings s, ref int attempts)
        {
            var layout = d.Build();
            if (layout == null) return null;
            attempts++;
            var space = PenguinSpace.Explore(layout, layout.Start, s.MaxStates);
            if (s.Counters != null)
            {
                s.Counters.Explored++;
                s.Counters.States += space.Count;
            }
            if (space.Capped)
            {
                if (s.Counters != null) s.Counters.Capped++;
                return null;
            }
            if (!space.Solvable)
            {
                if (s.Counters != null) s.Counters.Unsolvable++;
                return null;
            }
            return new Eval { Layout = layout, Space = space, Min = space.MinMoves };
        }

        /// <summary>Teaching: the fish on cells the penguins slide over in the solution found without fish, with the same minimum.</summary>
        static Eval PlaceFishOnPath(Draft d, Eval e, int count, SeededRandom rng, Settings s, ref int attempts)
        {
            var cells = PathCells(e.Layout, e.Space.Solution(e.Layout.Start));
            for (int i = cells.Count - 1; i >= 0; i--)
                if (d.Busy(cells[i])) cells.RemoveAt(i);
            if (cells.Count < count) return null;
            rng.Shuffle(cells);
            var withFish = d.Clone();
            for (int i = 0; i < count; i++) withFish.Fish.Add(cells[i]);
            var f = Evaluate(withFish, s, ref attempts);
            return f != null && f.Min == e.Min ? f : null;
        }

        /// <summary>Cells the penguins slide over (not the cell they start a slide from) when <paramref name="dirs"/> are played from the start.</summary>
        public static List<int> PathCells(PenguinLayout layout, IReadOnlyList<int> dirs)
        {
            var sim = new PenguinSimulator(layout);
            var trace = new PenguinTrace();
            var cells = new List<int>();
            ulong state = layout.Start;
            foreach (int dir in dirs)
            {
                sim.Step(state, dir, out ulong next, trace);
                foreach (var t in trace.Tracks)
                {
                    if (t.Actor >= PenguinLayout.MaxPenguins) continue;
                    int c = t.From;
                    while (c != t.To && c >= 0)
                    {
                        c = layout.Neighbor(c, dir);
                        if (c >= 0 && layout.Terrain(c) == PenguinTerrain.Ice && layout.IglooAt(c) < 0 && !cells.Contains(c)) cells.Add(c);
                    }
                }
                state = next;
            }
            return cells;
        }

        /// <summary>
        /// The checks of a level against its tier (generator, validator): it uses the novelty of its tier, and it has at most
        /// <see cref="Settings.MaxUseless"/> elements never touched in any reachable state. Empty = OK.
        /// </summary>
        public static List<string> Problems(PenguinLayout layout, PenguinSpace space, Settings s)
        {
            var p = new List<string>();
            int min = space.MinMoves;
            if (min <= 0) p.Add("no solution or solved at the start");
            var withUse = PenguinSpace.Explore(layout, layout.Start, Math.Max(s.MaxStates, space.Count + 1), trackUse: true);
            var use = withUse.Use;
            int useless = 0;
            for (int c = 0; c < layout.CellCount; c++)
                switch (layout.Terrain(c))
                {
                    case PenguinTerrain.Rock: if (!use.Rock[c]) useless++; break;
                    case PenguinTerrain.Snow: if (!use.Snow[c]) useless++; break;
                    case PenguinTerrain.Water: if (!use.Water[c]) useless++; break;
                }
            if (layout.BlockCount > 0 && !use.BlockPushed) useless += layout.BlockCount;
            for (int e = 0; e < layout.EdgeCount; e++)
            {
                if (!layout.EdgeWater(e) || (e > 0 && layout.EdgeWater(e - 1) && SameSide(layout, e - 1, e))) continue;
                bool touched = false;
                for (int k = e; k < layout.EdgeCount && layout.EdgeWater(k) && SameSide(layout, e, k); k++) touched |= use.Edge[k];
                if (!touched) useless++;
            }
            if (useless > s.MaxUseless) p.Add($"useless elements: {useless} (at most {s.MaxUseless})");

            if (s.NeedSnow)
            {
                if (layout.Count(PenguinTerrain.Snow) == 0) p.Add("snow not needed: no snow");
                else if (Helps(layout.Variant(snowToIce: true), min, s)) p.Add("snow not needed: as short with ice");
            }
            if (s.Danger > 0)
            {
                int danger = 0;
                ulong state = layout.Start;
                var sim = new PenguinSimulator(layout);
                foreach (int dir in space.Solution(state))
                {
                    if (space.FallMask(state) != 0) danger++;
                    sim.Step(state, dir, out state);
                }
                if (danger < s.Danger) p.Add($"water not dangerous: {danger} risky states on the way (at least {s.Danger})");
            }
            if (s.NeedPush)
            {
                if (layout.BlockCount == 0) p.Add("no push needed: no block");
                else
                {
                    var frozen = PenguinSpace.Explore(layout, layout.Start, s.MaxStates, freezeBlocks: true);
                    if (frozen.Capped || (frozen.Solvable && frozen.MinMoves <= min)) p.Add("no push needed: as short with frozen blocks");
                }
            }
            if (s.NeedFill && layout.BlockCount > 0 && layout.Count(PenguinTerrain.Water) > 0)
            {
                var noFill = PenguinSpace.Explore(layout, layout.Start, s.MaxStates, noFill: true);
                if (noFill.Capped || (noFill.Solvable && noFill.MinMoves <= min)) p.Add("no fill needed: as short without filling");
            }
            if (s.FishOnPath && layout.FishCount > 0)
            {
                var noFish = layout.Variant(noFish: true);
                var v = PenguinSpace.Explore(noFish, noFish.Start, s.MaxStates);
                if (!v.Solvable || v.MinMoves != min) p.Add("fish not on the way: another minimum without fish");
                else
                {
                    var sim = new PenguinSimulator(layout);
                    ulong state = layout.Start;
                    foreach (int dir in v.Solution(noFish.Start)) sim.Step(state, dir, out state);
                    if (!layout.IsSolved(state)) p.Add("fish not on the way: the way without fish misses them");
                }
            }
            return p;
        }

        /// <summary>The variant (a novelty removed) is solvable in at most <paramref name="min"/> moves: the novelty is not needed.</summary>
        static bool Helps(PenguinLayout variant, int min, Settings s)
        {
            var v = PenguinSpace.Explore(variant, variant.Start, s.MaxStates);
            return v.Capped || (v.Solvable && v.MinMoves <= min);
        }

        static bool SameSide(PenguinLayout l, int a, int b) => Side(l, a) == Side(l, b);

        static int Side(PenguinLayout l, int e) => e < l.Width ? 0 : e < l.Width + l.Height ? 1 : e < 2 * l.Width + l.Height ? 2 : 3;

        // ------------------------------------------------------------------ drafts

        sealed class Draft
        {
            public int W, H;
            public PenguinTerrain[] Terrain;
            public List<int> Fish = new List<int>(), Blocks = new List<int>();
            public int[] Pen, Igloo;
            public bool[] Edges;

            public Draft Clone() => new Draft
            {
                W = W, H = H, Terrain = (PenguinTerrain[])Terrain.Clone(), Fish = new List<int>(Fish), Blocks = new List<int>(Blocks),
                Pen = (int[])Pen.Clone(), Igloo = (int[])Igloo.Clone(), Edges = (bool[])Edges.Clone(),
            };

            /// <summary>Not a free ice cell (terrain, or a fish, block, penguin or igloo on it).</summary>
            public bool Busy(int c) =>
                Terrain[c] != PenguinTerrain.Ice || Fish.Contains(c) || Blocks.Contains(c) || Array.IndexOf(Pen, c) >= 0 || Array.IndexOf(Igloo, c) >= 0;

            public static Draft Random(SeededRandom rng, Settings s, out int fishWanted)
            {
                int w = s.Width, h = s.Height, n = w * h;
                var d = new Draft
                {
                    W = w, H = h, Terrain = new PenguinTerrain[n], Pen = new int[s.Penguins], Igloo = new int[s.Penguins],
                    Edges = new bool[2 * (w + h)],
                };
                var cells = new List<int>(n);
                for (int c = 0; c < n; c++) cells.Add(c);
                rng.Shuffle(cells);
                int k = 0;
                int Next() => k < cells.Count ? cells[k++] : -1;
                for (int i = 0; i < s.Penguins; i++)
                {
                    d.Igloo[i] = Next();
                    d.Pen[i] = Next();
                }
                int rocks = Pick(rng, s.Rocks), snow = Pick(rng, s.Snow), holes = Pick(rng, s.Holes), fish = Pick(rng, s.Fish), blocks = Pick(rng, s.Blocks);
                int edges = Pick(rng, s.WaterEdges);
                fishWanted = s.FishOnPath ? fish : 0;
                if (2 * s.Penguins + rocks + snow + holes + fish + blocks > n) return null;
                for (int i = 0; i < rocks; i++) d.Set(Next(), PenguinTerrain.Rock);
                for (int i = 0; i < snow; i++) d.Set(Next(), PenguinTerrain.Snow);
                for (int i = 0; i < holes; i++) d.Set(Next(), PenguinTerrain.Water);
                if (!s.FishOnPath)
                    for (int i = 0; i < fish; i++) d.Fish.Add(Next());
                for (int i = 0; i < blocks; i++) d.Blocks.Add(Next());
                for (int i = 0; i < edges; i++)
                    if (!d.AddEdgeRun(rng)) return null;
                return d;
            }

            static int Pick(SeededRandom rng, DifficultyRange r) => r.max <= r.min ? Math.Max(0, r.min) : rng.Range(Math.Max(0, r.min), r.max);

            void Set(int c, PenguinTerrain t) => Terrain[c] = t;

            int SideStart(int side) => side == 0 ? 0 : side == 1 ? W : side == 2 ? W + H : 2 * W + H;
            int SideLength(int side) => side % 2 == 0 ? W : H;

            /// <summary>A run of 1-3 water sides on the border, not touching another run on the same side.</summary>
            public bool AddEdgeRun(SeededRandom rng)
            {
                for (int tries = 0; tries < 12; tries++)
                {
                    int side = rng.Next(4), len = SideLength(side);
                    int run = rng.Range(1, Math.Max(1, Math.Min(3, len - 1)));
                    int start = rng.Range(0, len - run);
                    int b = SideStart(side);
                    bool free = true;
                    for (int i = start - 1; i <= start + run && free; i++)
                        if (i >= 0 && i < len && Edges[b + i]) free = false;
                    if (!free) continue;
                    for (int i = start; i < start + run; i++) Edges[b + i] = true;
                    return true;
                }
                return false;
            }

            /// <summary>Removes the run of water sides holding border side <paramref name="e"/>.</summary>
            void RemoveEdgeRun(int e)
            {
                int side = e < W ? 0 : e < W + H ? 1 : e < 2 * W + H ? 2 : 3;
                int b = SideStart(side), len = SideLength(side);
                for (int i = e - b; i >= 0 && Edges[b + i]; i--) Edges[b + i] = false;
                for (int i = e - b + 1; i < len && Edges[b + i]; i++) Edges[b + i] = false;
            }

            int FreeCell(SeededRandom rng)
            {
                for (int tries = 0; tries < 24; tries++)
                {
                    int c = rng.Next(W * H);
                    if (!Busy(c)) return c;
                }
                return -1;
            }

            /// <summary>One element (rock, snow, hole, fish, block, penguin, igloo or water run) moved to another place; null if none fits.</summary>
            public Draft Mutate(SeededRandom rng)
            {
                var d = Clone();
                var kinds = new List<int>();
                for (int c = 0; c < Terrain.Length; c++)
                    if (Terrain[c] != PenguinTerrain.Ice) { kinds.Add(0); break; }
                if (Fish.Count > 0) kinds.Add(1);
                if (Blocks.Count > 0) kinds.Add(2);
                kinds.Add(3);
                kinds.Add(4);
                bool anyEdge = Array.IndexOf(Edges, true) >= 0;
                if (anyEdge) kinds.Add(5);
                int kind = kinds[rng.Next(kinds.Count)];
                int to = d.FreeCell(rng);
                switch (kind)
                {
                    case 0:
                    {
                        var terrainCells = new List<int>();
                        for (int c = 0; c < Terrain.Length; c++)
                            if (Terrain[c] != PenguinTerrain.Ice) terrainCells.Add(c);
                        int from = terrainCells[rng.Next(terrainCells.Count)];
                        if (to < 0) return null;
                        d.Terrain[to] = d.Terrain[from];
                        d.Terrain[from] = PenguinTerrain.Ice;
                        break;
                    }
                    case 1:
                        if (to < 0) return null;
                        d.Fish[rng.Next(d.Fish.Count)] = to;
                        break;
                    case 2:
                        if (to < 0) return null;
                        d.Blocks[rng.Next(d.Blocks.Count)] = to;
                        break;
                    case 3:
                        if (to < 0) return null;
                        d.Pen[rng.Next(d.Pen.Length)] = to;
                        break;
                    case 4:
                        if (to < 0) return null;
                        d.Igloo[rng.Next(d.Igloo.Length)] = to;
                        break;
                    default:
                    {
                        var water = new List<int>();
                        for (int e = 0; e < Edges.Length; e++)
                            if (Edges[e]) water.Add(e);
                        d.RemoveEdgeRun(water[rng.Next(water.Count)]);
                        if (!d.AddEdgeRun(rng)) return null;
                        break;
                    }
                }
                return d;
            }

            public PenguinLayout Build()
            {
                try { return new PenguinLayout(W, H, Terrain, Fish, Blocks, Pen, Igloo, Edges); }
                catch (FormatException) { return null; }
            }
        }
    }
}
