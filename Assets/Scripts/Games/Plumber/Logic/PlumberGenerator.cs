using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// Builds a level from a seed inside a difficulty tier:
    /// 1. the water routes, by self-avoiding random walks: one source -> one sink (chapter 1); one source -> a T -> two sinks (chapter 2);
    ///    one separate route per color, 2-4 colors (chapters 3, 6, 8). Each route cell gets the piece its openings ask for (straight, curve, T);
    /// 2. decoy T and cross pieces off the routes, as many as the tier asks; 3. random straights and curves everywhere else;
    /// 4. (ErNa-006) locked pieces: on a route already right (a constraint) or off the routes (obstacles, decoys);
    ///    a random rotation for every other rotatable piece;
    /// 5. the solver measures it: candidates that are already solved, not solvable, too hard for the solver, outside the tier, or
    ///    (one sink) whose best solution uses a T, are discarded and the next attempt continues the same random sequence.
    /// Same seed + same tier = same level, on every platform.
    /// </summary>
    public static class PlumberGenerator
    {
        public enum Layout
        {
            /// <summary>One source, one sink.</summary>
            Single,
            /// <summary>One source, a T, two sinks of the same color.</summary>
            Fork,
            /// <summary>One separate route per color (2-4 colors, ErNa-006): a source and a sink of each color.</summary>
            Colors,
        }

        public sealed class Settings
        {
            /// <summary>Grid columns and rows (they can differ).</summary>
            public int Width, Height;
            public DifficultyRange MinMoves, Path, Tees, Crosses;
            /// <summary>ErNa-006: locked pieces on the grid, some on the route (already right: a constraint), some off it (obstacles).</summary>
            public DifficultyRange Locked;
            public Layout Layout;
            /// <summary>Water colors (= routes) of the <see cref="Layout.Colors"/> layout.</summary>
            public int ColorCount = 1;
            public int MaxAttempts = 20000;
            /// <summary>Solver node limit per candidate: harder candidates are skipped.</summary>
            public int SolverLimit = 400000;

            public static Settings FromTier(DifficultyTier tier, int maxAttempts)
            {
                int sinks = tier.GetOr(PlumberDifficulty.Sinks, 1).min;
                int colors = tier.GetOr(PlumberDifficulty.Colors, 1).min;
                return new Settings
                {
                    Width = tier.Get(PlumberDifficulty.Cols).min,
                    Height = tier.Get(PlumberDifficulty.Rows).min,
                    MinMoves = tier.Get(PlumberDifficulty.MinMoves),
                    Path = tier.Get(PlumberDifficulty.Path),
                    Tees = tier.GetOr(PlumberDifficulty.Tees, 0),
                    Crosses = tier.GetOr(PlumberDifficulty.Crosses, 0),
                    Locked = tier.GetOr(PlumberDifficulty.Locked, 0),
                    Layout = colors >= 2 ? Layout.Colors : sinks >= 2 ? Layout.Fork : Layout.Single,
                    ColorCount = Math.Max(1, colors),
                    MaxAttempts = maxAttempts,
                };
            }
        }

        public sealed class Result
        {
            public PlumberLevel Level;
            public PlumberSolution Solution;
            public int Attempts;
        }

        /// <exception cref="InvalidOperationException">No valid level within <see cref="Settings.MaxAttempts"/>.</exception>
        public static Result Generate(Settings settings, int seed, string tierId = "", int number = 0)
        {
            if (settings.Width < 2 || settings.Height < 2) throw new ArgumentException("grid columns and rows must be at least 2");
            int routes = settings.Layout == Layout.Colors ? settings.ColorCount : 1;
            int endpoints = settings.Layout == Layout.Single ? 2 : settings.Layout == Layout.Fork ? 3 : 2 * routes;
            int minPath = settings.Layout == Layout.Single ? 1 : settings.Layout == Layout.Fork ? 2 : routes;
            if (settings.ColorCount > PlumberPalette.MaxColors) throw new ArgumentException($"at most {PlumberPalette.MaxColors} colors");
            if (settings.Path.min < minPath || settings.Path.max + endpoints > settings.Width * settings.Height) throw new ArgumentException("route length does not fit the grid");
            var rng = new SeededRandom(seed);
            var options = new PlumberSolver.Options { MaxExpansions = settings.SolverLimit };
            for (int attempt = 1; attempt <= settings.MaxAttempts; attempt++)
            {
                var grid = TryBuild(settings, rng);
                if (grid == null) continue;
                var sol = PlumberSolver.Solve(grid, options);
                if (!sol.Solvable || sol.MinMoves == 0) continue;   // MinMoves 0 = already solved at the start
                if (!settings.MinMoves.Contains(sol.MinMoves) || !settings.Path.Contains(sol.PathLength)) continue;
                if (settings.Layout == Layout.Single && sol.TeesOnPath > 0) continue;   // chapter 1: a T is only a decoy
                if (settings.Layout == Layout.Fork && sol.TeesOnPath == 0) continue;
                return new Result
                {
                    Attempts = attempt,
                    Solution = sol,
                    Level = new PlumberLevel
                    {
                        number = number,
                        tier = tierId,
                        seed = seed,
                        width = settings.Width,
                        height = settings.Height,
                        minMoves = sol.MinMoves,
                        path = sol.PathLength,
                        cells = PlumberLevelCodec.Format(grid),
                    },
                };
            }
            throw new InvalidOperationException($"plumber generator: no level for tier '{tierId}' (seed {seed}) in {settings.MaxAttempts} attempts");
        }

        static PipeGrid TryBuild(Settings s, SeededRandom rng)
        {
            var grid = new PipeGrid(s.Width, s.Height);
            var topo = grid.Topology;
            int n = grid.CellCount;
            var open = new int[n];       // route openings per cell
            var onRoute = new bool[n];
            int length = rng.Range(s.Path.min, s.Path.max);
            int routeTees = 0;

            switch (s.Layout)
            {
                case Layout.Single:
                {
                    var route = Walk(topo, rng, rng.Next(n), length + 1, onRoute);
                    if (route == null) return null;
                    Mark(topo, grid, open, route, PieceKind.Source, PieceKind.Sink, 0);
                    break;
                }
                case Layout.Fork:
                {
                    // length = trunk pipes + the T + the pipes of the two branches
                    int rest = length - 1;
                    int trunk = rng.Range(1, Math.Max(1, rest / 2));
                    int b1 = rng.Range(0, rest - trunk);
                    int b2 = rest - trunk - b1;
                    var trunkRoute = Walk(topo, rng, rng.Next(n), trunk + 1, onRoute);   // source + trunk pipes + the T cell
                    if (trunkRoute == null) return null;
                    int tee = trunkRoute[trunkRoute.Count - 1];
                    var branch1 = Walk(topo, rng, tee, b1 + 1, onRoute, startOnRoute: true);
                    if (branch1 == null) return null;
                    var branch2 = Walk(topo, rng, tee, b2 + 1, onRoute, startOnRoute: true);
                    if (branch2 == null) return null;
                    LinkRoute(topo, open, trunkRoute);
                    LinkRoute(topo, open, branch1);
                    LinkRoute(topo, open, branch2);
                    grid.Pieces[trunkRoute[0]] = Fixed(topo, PieceKind.Source, trunkRoute[0], open, 0);
                    grid.Pieces[branch1[branch1.Count - 1]] = Fixed(topo, PieceKind.Sink, branch1[branch1.Count - 1], open, 0);
                    grid.Pieces[branch2[branch2.Count - 1]] = Fixed(topo, PieceKind.Sink, branch2[branch2.Count - 1], open, 0);
                    routeTees = 1;
                    break;
                }
                default:
                {
                    if (s.ColorCount != 2)
                    {
                        if (!BuildRoutes(s, rng, topo, grid, open, onRoute, length)) return null;
                        break;
                    }
                    // two colors: the ErNa-003 sequence of random numbers, so the saved chapter 3 levels stay the same
                    int lenA = rng.Range(1, length - 1);
                    int lenB = length - lenA;
                    int colorA = rng.Next(2);
                    var a = Walk(topo, rng, rng.Next(n), lenA + 1, onRoute);
                    if (a == null) return null;
                    int startB = PickFree(rng, onRoute);
                    if (startB < 0) return null;
                    var b = Walk(topo, rng, startB, lenB + 1, onRoute);
                    if (b == null) return null;
                    Mark(topo, grid, open, a, PieceKind.Source, PieceKind.Sink, colorA);
                    Mark(topo, grid, open, b, PieceKind.Source, PieceKind.Sink, 1 - colorA);
                    break;
                }
            }

            // route pipes: the piece their openings ask for (the rotation is randomized below anyway)
            for (int c = 0; c < n; c++)
            {
                if (!onRoute[c] || grid.Pieces[c].Kind == PieceKind.Source || grid.Pieces[c].Kind == PieceKind.Sink) continue;
                grid.Pieces[c] = new Piece(PieceFor(topo, c, open[c]));
            }

            // decoys off the routes, then fillers
            var free = new List<int>();
            for (int c = 0; c < n; c++)
                if (!onRoute[c]) free.Add(c);
            rng.Shuffle(free);
            int decoyTees = Math.Max(0, rng.Range(s.Tees.min, s.Tees.max) - routeTees);
            int crosses = rng.Range(s.Crosses.min, s.Crosses.max);
            if (decoyTees + crosses > free.Count) return null;
            for (int i = 0; i < free.Count; i++)
            {
                PieceKind kind = i < decoyTees ? PieceKind.Tee
                    : i < decoyTees + crosses ? PieceKind.Cross
                    : rng.Chance(0.5) ? PieceKind.Straight : PieceKind.Curve;
                grid.Pieces[free[i]] = new Piece(kind);
            }
            // locked pieces (ErNa-006): on the route already right, or off the route as obstacles. Only when the tier asks for them,
            // so the random sequence of the older tiers (and their saved levels) does not change.
            if (s.Locked.max > 0)
            {
                var routePipes = new List<int>();
                for (int c = 0; c < n; c++)
                    if (onRoute[c] && grid.Pieces[c].IsRotatable) routePipes.Add(c);
                var offRoute = new List<int>();
                for (int c = 0; c < n; c++)
                    if (!onRoute[c] && grid.Pieces[c].IsRotatable) offRoute.Add(c);
                rng.Shuffle(routePipes);
                rng.Shuffle(offRoute);
                int locks = rng.Range(s.Locked.min, s.Locked.max);
                for (int i = 0; i < locks; i++)
                {
                    bool onPath = routePipes.Count > 0 && (offRoute.Count == 0 || rng.Chance(0.5));
                    var list = onPath ? routePipes : offRoute;
                    if (list.Count == 0) return null;
                    int c = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    grid.Pieces[c].Rotation = onPath ? RotationFor(topo, c, PieceShapes.BaseMask(grid.Pieces[c].Kind), open[c]) : rng.Next(topo.RotationSteps);
                    grid.Pieces[c].Locked = true;
                }
            }
            for (int c = 0; c < n; c++)
                if (grid.Pieces[c].IsRotatable) grid.Pieces[c].Rotation = rng.Next(topo.RotationSteps);
            return grid;
        }

        /// <summary>
        /// <see cref="Settings.ColorCount"/> separate routes (3-4 colors, ErNa-006): the pipes are split at random among the routes
        /// (at least one each), the colors are shuffled, each route starts on a free cell.
        /// </summary>
        static bool BuildRoutes(Settings s, SeededRandom rng, SquareTopology topo, PipeGrid grid, int[] open, bool[] onRoute, int length)
        {
            int k = s.ColorCount;
            if (length < k) return false;
            var lens = new int[k];
            for (int i = 0; i < k; i++) lens[i] = 1;
            for (int i = k; i < length; i++) lens[rng.Next(k)]++;
            var colors = new int[k];
            for (int i = 0; i < k; i++) colors[i] = i;
            rng.Shuffle(colors);
            for (int r = 0; r < k; r++)
            {
                int start = r == 0 ? rng.Next(grid.CellCount) : PickFree(rng, onRoute);
                if (start < 0) return false;
                var route = Walk(topo, rng, start, lens[r] + 1, onRoute);
                if (route == null) return false;
                Mark(topo, grid, open, route, PieceKind.Source, PieceKind.Sink, colors[r]);
            }
            return true;
        }

        /// <summary>
        /// Self-avoiding random walk of <paramref name="steps"/> cells after <paramref name="start"/> (returned list includes the start).
        /// Marks the cells as route. <paramref name="startOnRoute"/>: the start is already a route cell (a branch leaving a T).
        /// </summary>
        static List<int> Walk(SquareTopology topo, SeededRandom rng, int start, int steps, bool[] onRoute, bool startOnRoute = false)
        {
            if (!startOnRoute && onRoute[start]) return null;
            var route = new List<int> { start };
            onRoute[start] = true;
            var options = new List<int>(4);
            for (int i = 0; i < steps; i++)
            {
                int cur = route[route.Count - 1];
                options.Clear();
                for (int d = 0; d < topo.DirectionCount; d++)
                {
                    int nb = topo.Neighbor(cur, d);
                    if (nb >= 0 && !onRoute[nb]) options.Add(nb);
                }
                if (options.Count == 0) return null;   // stuck: next attempt
                int next = rng.Pick(options);
                route.Add(next);
                onRoute[next] = true;
            }
            return route;
        }

        static int PickFree(SeededRandom rng, bool[] onRoute)
        {
            var free = new List<int>();
            for (int c = 0; c < onRoute.Length; c++)
                if (!onRoute[c]) free.Add(c);
            return free.Count == 0 ? -1 : rng.Pick(free);
        }

        static void LinkRoute(SquareTopology topo, int[] open, List<int> route)
        {
            for (int i = 0; i + 1 < route.Count; i++)
            {
                int d = topo.DirectionTo(route[i], route[i + 1]);
                open[route[i]] |= 1 << d;
                open[route[i + 1]] |= 1 << topo.Opposite(d);
            }
        }

        /// <summary>A whole route: first cell = source, last = sink, both of <paramref name="color"/>.</summary>
        static void Mark(SquareTopology topo, PipeGrid grid, int[] open, List<int> route, PieceKind first, PieceKind last, int color)
        {
            LinkRoute(topo, open, route);
            grid.Pieces[route[0]] = Fixed(topo, first, route[0], open, color);
            grid.Pieces[route[route.Count - 1]] = Fixed(topo, last, route[route.Count - 1], open, color);
        }

        static Piece Fixed(SquareTopology topo, PieceKind kind, int cell, int[] open, int color) =>
            new Piece(kind, RotationFor(topo, cell, PieceShapes.BaseMask(kind), open[cell]), color: color);

        static PieceKind PieceFor(SquareTopology topo, int cell, int mask)
        {
            int bits = 0;
            for (int d = 0; d < topo.DirectionCount; d++)
                if ((mask & (1 << d)) != 0) bits++;
            if (bits >= 3) return PieceKind.Tee;
            return RotationFor(topo, cell, PieceShapes.BaseMask(PieceKind.Straight), mask) >= 0 ? PieceKind.Straight : PieceKind.Curve;
        }

        static int RotationFor(SquareTopology topo, int cell, int baseMask, int wanted)
        {
            for (int k = 0; k < topo.RotationSteps; k++)
                if (topo.RotateMask(cell, baseMask, k) == wanted) return k;
            return -1;
        }
    }
}
