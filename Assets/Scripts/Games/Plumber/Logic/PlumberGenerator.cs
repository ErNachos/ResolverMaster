using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// Builds a level from a seed inside a difficulty tier:
    /// 1. a random self-avoiding route source -> sink of the tier's length; the right piece (straight or curve) on every route cell;
    /// 2. T and cross pieces as the tier asks (on the route or not: a T or a cross can carry any route turn);
    /// 3. every other cell gets a random straight or curve; 4. every rotatable piece gets a random rotation;
    /// 5. the solver measures it: candidates already solved, or with minimum moves / route length outside the tier, are discarded
    /// and the next attempt starts from the same random sequence. Same seed + same tier = same level, on every platform.
    /// </summary>
    public static class PlumberGenerator
    {
        public sealed class Settings
        {
            public int Size;
            public DifficultyRange MinMoves, Path, Tees, Crosses;
            public int MaxAttempts = 20000;

            public static Settings FromTier(DifficultyTier tier, int maxAttempts)
            {
                var size = tier.Get(PlumberDifficulty.Size);
                return new Settings
                {
                    Size = size.min,
                    MinMoves = tier.Get(PlumberDifficulty.MinMoves),
                    Path = tier.Get(PlumberDifficulty.Path),
                    Tees = tier.GetOr(PlumberDifficulty.Tees, 0),
                    Crosses = tier.GetOr(PlumberDifficulty.Crosses, 0),
                    MaxAttempts = maxAttempts,
                };
            }
        }

        public sealed class Result
        {
            public PlumberLevel Level;
            public int Attempts;
        }

        /// <exception cref="InvalidOperationException">No valid level within <see cref="Settings.MaxAttempts"/>.</exception>
        public static Result Generate(Settings settings, int seed, string tierId = "", int number = 0)
        {
            int size = settings.Size;
            if (size < 2) throw new ArgumentException("grid size must be at least 2");
            if (settings.Path.min < 1 || settings.Path.max + 2 > size * size) throw new ArgumentException("route length does not fit the grid");
            var rng = new SeededRandom(seed);
            for (int attempt = 1; attempt <= settings.MaxAttempts; attempt++)
            {
                var grid = TryBuild(settings, rng);
                if (grid == null) continue;
                var sol = PlumberSolver.Solve(grid);
                if (!sol.Solvable || sol.MinMoves == 0) continue;   // MinMoves 0 = already solved at the start
                if (!settings.MinMoves.Contains(sol.MinMoves) || !settings.Path.Contains(sol.PathLength)) continue;
                return new Result
                {
                    Attempts = attempt,
                    Level = new PlumberLevel
                    {
                        number = number,
                        tier = tierId,
                        seed = seed,
                        width = size,
                        height = size,
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
            int size = s.Size;
            var grid = new PipeGrid(size, size);
            var topo = grid.Topology;
            int length = rng.Range(s.Path.min, s.Path.max);

            // 1. self-avoiding random walk of length + 2 cells (source, pipes, sink)
            var route = new List<int> { rng.Next(grid.CellCount) };
            var onRoute = new bool[grid.CellCount];
            onRoute[route[0]] = true;
            var options = new List<int>(4);
            while (route.Count < length + 2)
            {
                int cur = route[route.Count - 1];
                options.Clear();
                for (int d = 0; d < 4; d++)
                {
                    int nb = topo.Neighbor(cur, d);
                    if (nb >= 0 && !onRoute[nb]) options.Add(nb);
                }
                if (options.Count == 0) return null;   // stuck: next attempt
                int next = rng.Pick(options);
                route.Add(next);
                onRoute[next] = true;
            }
            int src = route[0], sink = route[route.Count - 1];
            grid.Pieces[src] = new Piece(PieceKind.Source, topo.DirectionTo(src, route[1]));
            grid.Pieces[sink] = new Piece(PieceKind.Sink, topo.DirectionTo(sink, route[route.Count - 2]));
            for (int i = 1; i < route.Count - 1; i++)
            {
                int entry = topo.DirectionTo(route[i], route[i - 1]);
                int exit = topo.DirectionTo(route[i], route[i + 1]);
                grid.Pieces[route[i]] = new Piece(exit == topo.Opposite(entry) ? PieceKind.Straight : PieceKind.Curve);
            }

            // 2. T and cross pieces anywhere except source and sink
            var free = new List<int>();
            for (int c = 0; c < grid.CellCount; c++)
                if (c != src && c != sink) free.Add(c);
            rng.Shuffle(free);
            int tees = rng.Range(s.Tees.min, s.Tees.max);
            int crosses = rng.Range(s.Crosses.min, s.Crosses.max);
            if (tees + crosses > free.Count) return null;
            var special = new bool[grid.CellCount];
            for (int i = 0; i < tees + crosses; i++)
            {
                grid.Pieces[free[i]] = new Piece(i < tees ? PieceKind.Tee : PieceKind.Cross);
                special[free[i]] = true;
            }

            // 3. fillers, 4. random rotations
            for (int c = 0; c < grid.CellCount; c++)
            {
                if (!onRoute[c] && !special[c]) grid.Pieces[c] = new Piece(rng.Chance(0.5) ? PieceKind.Straight : PieceKind.Curve);
                if (grid.Pieces[c].IsRotatable) grid.Pieces[c].Rotation = rng.Next(topo.RotationSteps);
            }
            return grid;
        }
    }
}
