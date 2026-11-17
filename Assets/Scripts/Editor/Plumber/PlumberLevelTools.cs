using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Games.Plumber;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ResolverMaster.Editor.Plumber
{
    /// <summary>
    /// Menu Resolver Master > Idraulico > Genera livelli Storia: generates every Story level of the PlumberConfig difficulty table from the
    /// fixed seeds (storySeedBase + level), groups them in chapters of 9 (level n is in chapter (n-1)/9+1), saves them as game data
    /// (Resources/Games/Plumber/plumber_story.json) and writes the table in docs/idraulico-livelli.md, with the average generation time
    /// of a level of the last tier (6x8, two colors).
    /// Batch: -executeMethod ResolverMaster.Editor.Plumber.PlumberLevelTools.GenerateStoryLevelsBatch
    /// </summary>
    public static class PlumberLevelTools
    {
        public const string DocPath = "docs/idraulico-livelli.md";
        public const int TimingSamples = 30;
        public const int TimingSeedBase = 900000;

        [MenuItem("Resolver Master/Idraulico/Genera livelli Storia")]
        public static void GenerateStoryLevels() => Run();

        public static void GenerateStoryLevelsBatch()
        {
            try
            {
                Run();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        sealed class Generated
        {
            public PlumberLevel Level;
            public PlumberSolution Solution;
            public int Attempts;
            public double Ms;
        }

        public const string ChallengeDocPath = "docs/idraulico-challenge.md";
        public const int ChallengeTrialSeed = 20261117, ChallengeTrialLevels = 50;

        /// <summary>
        /// Menu Resolver Master > Idraulico > Prova Challenge (ErNa-007): generates the first 50 levels of a run seed and writes step,
        /// grid, sources, minimum, maximum and time of each one (log + docs/idraulico-challenge.md), with the average time per step.
        /// Batch: -executeMethod ResolverMaster.Editor.Plumber.PlumberLevelTools.TryChallengeBatch
        /// </summary>
        [MenuItem("Resolver Master/Idraulico/Prova Challenge")]
        public static void TryChallenge() => Debug.Log(ChallengeTrial(ChallengeTrialSeed));

        public static void TryChallengeBatch()
        {
            try
            {
                Debug.Log(ChallengeTrial(ChallengeTrialSeed));
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static string ChallengeTrial(int runSeed)
        {
            ConfigAssets.EnsureAll();
            var cfg = AssetDatabase.LoadAssetAtPath<PlumberConfig>(ConfigAssets.PlumberConfigPath);
            var source = new PlumberChallenge(cfg.challengeDifficulty, cfg.challenge, cfg.challengeMaxAttempts);
            ResolverMaster.Core.Challenge.ChallengeRules.Generate(source, runSeed - 1, 1, 0);   // warm-up (JIT)
            var levels = new List<ResolverMaster.Core.Challenge.ChallengeLevel>();
            for (int n = 1; n <= ChallengeTrialLevels; n++)
            {
                var sw = Stopwatch.StartNew();
                var level = ResolverMaster.Core.Challenge.ChallengeRules.Generate(source, runSeed, n, 0);
                sw.Stop();
                level.milliseconds = sw.Elapsed.TotalMilliseconds;
                levels.Add(level);
            }
            var sb = new StringBuilder("# Idraulico — prova della Challenge\n\n");
            sb.Append("File generato da *Resolver Master > Idraulico > Prova Challenge* (non modificare a mano). ");
            sb.Append($"Seed della run {runSeed}, primi {ChallengeTrialLevels} livelli, 0 cambi. Massimo = minimo + margine ");
            sb.Append($"(dal {cfg.challenge.marginStartPercent}% al {cfg.challenge.marginEndPercent}% del minimo, almeno +1).\n\n");
            sb.Append("| Livello | Gradino | Griglia | Fonti | Minimo | Massimo | Seed usato | Tempo (ms) |\n|---:|---|---|---:|---:|---:|---:|---:|\n");
            foreach (var l in levels)
            {
                var grid = PlumberLevelCodec.Parse(l.data);
                sb.Append($"| {l.number} | {l.tierId} | {grid.Width}x{grid.Height} | {grid.Count(PieceKind.Source)} | {l.minMoves} | {l.maxMoves} | {l.seed} | {l.milliseconds:0.0} |\n");
            }
            // the steps after level 50 (up to the last one, 7x9 with 4 colors): timed on levels 51-70 of the same run
            var later = new List<ResolverMaster.Core.Challenge.ChallengeLevel>();
            int lastLevel = cfg.challengeDifficulty.LastLevel;
            for (int n = ChallengeTrialLevels + 1; n <= lastLevel; n++)
            {
                var sw = Stopwatch.StartNew();
                var level = ResolverMaster.Core.Challenge.ChallengeRules.Generate(source, runSeed, n, 0);
                sw.Stop();
                level.milliseconds = sw.Elapsed.TotalMilliseconds;
                later.Add(level);
            }
            sb.Append($"\n## Tempo medio per gradino (livelli 1–{lastLevel})\n\n| Gradino | Livelli | Griglia | Fonti | Minimo | Massimo | Media (ms) | Massimo (ms) |\n|---|---:|---|---:|---|---|---:|---:|\n");
            foreach (var g in levels.Concat(later).GroupBy(l => l.tierId))
            {
                var grid = PlumberLevelCodec.Parse(g.First().data);
                sb.Append($"| {g.Key} | {g.First().number}–{g.Last().number} | {grid.Width}x{grid.Height} | {grid.Count(PieceKind.Source)} | " +
                          $"{g.Min(l => l.minMoves)}–{g.Max(l => l.minMoves)} | {g.Min(l => l.maxMoves)}–{g.Max(l => l.maxMoves)} | {g.Average(l => l.milliseconds):0.0} | {g.Max(l => l.milliseconds):0.0} |\n");
            }
            File.WriteAllText(ChallengeDocPath, sb.ToString(), new UTF8Encoding(false));
            return "PlumberLevelTools: Challenge trial written to " + ChallengeDocPath + "\n" + sb;
        }

        /// <summary>
        /// Menu Resolver Master > Idraulico > Misura risolutore (ErNa-006): generates <see cref="TimingSamples"/> random 7x9 levels with
        /// 4 colors (and the other big grids) and times the exact solver alone on each one (average, max, nodes).
        /// Batch: -executeMethod ResolverMaster.Editor.Plumber.PlumberLevelTools.MeasureSolverBatch
        /// </summary>
        [MenuItem("Resolver Master/Idraulico/Misura risolutore")]
        public static void MeasureSolver() => Debug.Log(SolverTiming());

        public static void MeasureSolverBatch()
        {
            try
            {
                Debug.Log(SolverTiming());
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Solver time on random big levels: (columns, rows, colors, route pipes min..max).</summary>
        public static string SolverTiming()
        {
            var cases = new[] { (6, 8, 2, 12, 18), (6, 8, 3, 14, 22), (7, 9, 3, 18, 26), (7, 9, 4, 18, 28), (7, 9, 4, 26, 34) };
            var sb = new StringBuilder("PlumberLevelTools: solver timing (exact minimum, no estimate)\n");
            foreach (var (w, h, colors, pmin, pmax) in cases)
            {
                var settings = new PlumberGenerator.Settings
                {
                    Width = w, Height = h, ColorCount = colors, Layout = PlumberGenerator.Layout.Colors,
                    MinMoves = new DifficultyRange(PlumberDifficulty.MinMoves, 1, 999), Path = new DifficultyRange(PlumberDifficulty.Path, pmin, pmax),
                    Tees = new DifficultyRange(PlumberDifficulty.Tees, 1, 3), Crosses = new DifficultyRange(PlumberDifficulty.Crosses, 0, 1),
                    MaxAttempts = 2000, SolverLimit = PlumberSolver.DefaultMaxExpansions,
                };
                var times = new List<double>();
                var nodes = new List<int>();
                var moves = new List<int>();
                PlumberSolver.Solve(PlumberGenerator.Generate(settings, TimingSeedBase - 1).Level.CreateGrid());   // warm-up (JIT)
                for (int i = 0; i < TimingSamples; i++)
                {
                    var level = PlumberGenerator.Generate(settings, TimingSeedBase + i).Level;
                    var grid = level.CreateGrid();
                    var sw = Stopwatch.StartNew();
                    var sol = PlumberSolver.Solve(grid);
                    sw.Stop();
                    times.Add(sw.Elapsed.TotalMilliseconds);
                    nodes.Add(sol.Expanded);
                    moves.Add(sol.MinMoves);
                }
                sb.Append($"  {w}x{h}, {colors} colors, route pipes {pmin}-{pmax}, {TimingSamples} levels: solver average {times.Average():0.0} ms, max {times.Max():0.0} ms, " +
                          $"average nodes {nodes.Average():0}, max nodes {nodes.Max()}, minimum moves {moves.Min()}-{moves.Max()}\n");
            }
            return sb.ToString();
        }

        static void Run()
        {
            ConfigAssets.EnsureAll();
            var cfg = AssetDatabase.LoadAssetAtPath<PlumberConfig>(ConfigAssets.PlumberConfigPath);
            var table = cfg.storyDifficulty;
            int last = table.LastLevel;
            if (last <= 0) throw new InvalidOperationException("PlumberConfig: the Story difficulty table is empty");
            if (last % StoryBook.LevelsPerChapter != 0)
                throw new InvalidOperationException($"PlumberConfig: {last} Story levels is not a whole number of {StoryBook.LevelsPerChapter}-level chapters");
            int chapters = last / StoryBook.LevelsPerChapter;
            if (cfg.chapters.Count < chapters) throw new InvalidOperationException($"PlumberConfig: {chapters} chapters needed, {cfg.chapters.Count} defined");

            var generated = new List<Generated>();
            for (int n = 1; n <= last; n++)
            {
                var tier = table.ForLevel(n) ?? throw new InvalidOperationException($"PlumberConfig: no difficulty tier covers level {n}");
                var sw = Stopwatch.StartNew();
                var r = PlumberGenerator.Generate(PlumberGenerator.Settings.FromTier(tier, cfg.maxAttempts), cfg.storySeedBase + n, tier.id, n);
                sw.Stop();
                r.Level.chapter = (n - 1) / StoryBook.LevelsPerChapter + 1;
                r.Level.index = (n - 1) % StoryBook.LevelsPerChapter;
                generated.Add(new Generated { Level = r.Level, Solution = r.Solution, Attempts = r.Attempts, Ms = sw.Elapsed.TotalMilliseconds });
            }

            var set = new PlumberLevelSet { levels = generated.Select(g => g.Level).ToList() };
            Directory.CreateDirectory(ConfigAssets.PlumberDir);
            File.WriteAllText(ConfigAssets.PlumberStoryPath, JsonUtility.ToJson(set, true) + "\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ConfigAssets.PlumberStoryPath);

            string timing = MeasureTiming(cfg, last) + "\n" + SolverTiming();
            WriteDoc(cfg, generated, timing);
            var log = new StringBuilder($"PlumberLevelTools: {generated.Count} Story levels in {chapters} chapters saved in {ConfigAssets.PlumberStoryPath}, table in {DocPath}\n");
            foreach (var g in generated)
                log.Append($"  {g.Level.number,2} (c{g.Level.chapter}): {g.Level.width}x{g.Level.height} min {g.Level.minMoves} path {g.Level.path} " +
                           $"seed {g.Level.seed} ({g.Attempts} attempts, {g.Ms:0.0} ms)\n");
            log.Append("  ").Append(timing);
            Debug.Log(log.ToString());
        }

        /// <summary>Average time to generate a level of the last tier (ErNa-006: 7x9, 4 colors, chapter 9) over <see cref="TimingSamples"/> other seeds.</summary>
        static string MeasureTiming(PlumberConfig cfg, int lastLevel)
        {
            var tier = cfg.storyDifficulty.ForLevel(lastLevel);
            var settings = PlumberGenerator.Settings.FromTier(tier, cfg.maxAttempts);
            PlumberGenerator.Generate(settings, TimingSeedBase - 1);   // warm-up (JIT)
            var times = new List<double>();
            var attempts = new List<int>();
            for (int i = 0; i < TimingSamples; i++)
            {
                var sw = Stopwatch.StartNew();
                var r = PlumberGenerator.Generate(settings, TimingSeedBase + i);
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
                attempts.Add(r.Attempts);
            }
            return $"generator {settings.Width}x{settings.Height} {settings.Layout} (tier {tier.id}), {TimingSamples} seeds: average {times.Average():0.00} ms " +
                   $"(min {times.Min():0.00}, max {times.Max():0.00}), average attempts {attempts.Average():0.0}";
        }

        static void WriteDoc(PlumberConfig cfg, List<Generated> levels, string timing)
        {
            var sb = new StringBuilder();
            sb.Append("# Idraulico — livelli della Storia\n\n");
            sb.Append("File generato da *Resolver Master > Idraulico > Genera livelli Storia* (non modificare a mano). Regole del gioco: `docs/idraulico-regole.md`.\n");
            sb.Append($"Dati del gioco: `{ConfigAssets.PlumberStoryPath}`. Seed del livello n = {cfg.storySeedBase} + n. ");
            sb.Append("Id stabile di un livello nel salvataggio: (gioco, capitolo, indice nel capitolo).\n\n");
            sb.Append("Colonne: *Fonti/Arrivi/Colori* sulla griglia; *T* = T sulla griglia (tra parentesi quelle sul percorso della soluzione); ");
            sb.Append("*Ponti* = ponti sulla griglia (tra parentesi quelli con l'acqua nei due canali nella soluzione); *Bloccati* = pezzi che non ruotano; ");
            sb.Append("*Percorso* = tubi attraversati dall'acqua nella soluzione ottima.\n");
            foreach (var chapter in levels.GroupBy(g => g.Level.chapter))
            {
                var theme = chapter.Key - 1 < cfg.chapters.Count ? cfg.chapters[chapter.Key - 1] : null;
                sb.Append($"\n## Capitolo {chapter.Key}{(theme != null ? " — `" + theme.nameKey + "`" : "")}\n\n");
                sb.Append("| Livello | Nel cap. | Gradino | Griglia | Fonti | Arrivi | Colori | Dritti | Curve | T | Croci | Ponti | Bloccati | Mosse minime | Percorso | Seed | Tentativi |\n");
                sb.Append("|---:|---:|---|---|---:|---:|---:|---:|---:|---|---:|---|---:|---:|---:|---:|---:|\n");
                foreach (var g in chapter)
                {
                    var l = g.Level;
                    var grid = l.CreateGrid();
                    sb.Append($"| {l.number} | {l.index + 1} | {l.tier} | {l.width}x{l.height} | {grid.Count(PieceKind.Source)} | {grid.Count(PieceKind.Sink)} | " +
                              $"{grid.ColorCount()} | {grid.Count(PieceKind.Straight)} | {grid.Count(PieceKind.Curve)} | " +
                              $"{grid.Count(PieceKind.Tee)} ({g.Solution.TeesOnPath}) | {grid.Count(PieceKind.Cross)} | {grid.Count(PieceKind.Bridge)} ({g.Solution.BridgesCrossed}) | " +
                              $"{grid.CountLocked()} | {l.minMoves} | {l.path} | {l.seed} | {g.Attempts} |\n");
                }
            }
            sb.Append("\n## Gradini di difficoltà (PlumberConfig)\n\n| Gradino | Livelli | Valori |\n|---|---|---|\n");
            foreach (var t in cfg.storyDifficulty.tiers)
                sb.Append($"| {t.id} | {(t.firstLevel == t.lastLevel ? t.firstLevel.ToString() : t.firstLevel + "–" + t.lastLevel)} | {string.Join(", ", t.values.Select(v => v.ToString()))} |\n");
            sb.Append("\n## Griglie iniziali\n\nFormato di `PlumberLevelCodec`: righe dall'alto separate da `/`; `I` dritto, `C` curva, `T`, `X` croce, ");
            sb.Append("`B` ponte, `S` fonte, `E` arrivo (fontana); la cifra è il numero di rotazioni orarie dalla forma base; `#` = bloccato; ");
            sb.Append("`@1` arancione, `@2` viola, `@3` giallo, senza = colore 0 (blu).\n\n");
            foreach (var g in levels) sb.Append($"- {g.Level.number}: `{g.Level.cells}`\n");
            sb.Append("\n## Tempo del generatore\n\n").Append(timing).Append(" (Editor, misurato all'ultima generazione).\n");
            Directory.CreateDirectory(Path.GetDirectoryName(DocPath));
            File.WriteAllText(DocPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
