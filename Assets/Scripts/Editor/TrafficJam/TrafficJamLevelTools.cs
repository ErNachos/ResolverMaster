using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Games.TrafficJam;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ResolverMaster.Editor.TrafficJam
{
    /// <summary>
    /// Traffic jam tools (TJ-001), on the model of the Plumber ones.
    /// Genera livelli Storia: every Story level of the TrafficJamConfig table from its fixed seed (storySeedBase + n; when a seed gives no
    /// level within the attempts, the next fixed seeds storySeedBase + n + 1000·k are tried), in parallel (each level is independent and
    /// deterministic); the exact minimum is measured again by the solver; chapters of 9; saved as game data
    /// (Resources/Games/TrafficJam/trafficjam_story.json) with the table in docs/trafficjam-livelli.md and a check that the difficulty
    /// never drops inside a chapter.
    /// Batch: -executeMethod ResolverMaster.Editor.TrafficJam.TrafficJamLevelTools.GenerateStoryLevelsBatch
    /// </summary>
    public static class TrafficJamLevelTools
    {
        public const string DocPath = "docs/trafficjam-livelli.md";
        public const string ChallengeDocPath = "docs/trafficjam-challenge.md";
        public const int SeedRetries = 8, SeedRetryStep = 1000;
        public const int ChallengeTrialSeed = 20261117;
        public const int TimingSamples = 20, TimingSeedBase = 900000;

        [MenuItem("Resolver Master/Traffic jam/Genera livelli Storia")]
        public static void GenerateStoryLevels() => Debug.Log(Run());

        public static void GenerateStoryLevelsBatch() => Batch(() => Debug.Log(Run()));

        [MenuItem("Resolver Master/Traffic jam/Prova Challenge")]
        public static void TryChallenge() => Debug.Log(ChallengeTrial(ChallengeTrialSeed));

        public static void TryChallengeBatch() => Batch(() => Debug.Log(ChallengeTrial(ChallengeTrialSeed)));

        [MenuItem("Resolver Master/Traffic jam/Misura risolutore")]
        public static void MeasureSolver() => Debug.Log(SolverTiming(LoadConfig()));

        public static void MeasureSolverBatch() => Batch(() => Debug.Log(SolverTiming(LoadConfig())));

        static void Batch(Action action)
        {
            try
            {
                action();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static TrafficJamConfig LoadConfig()
        {
            ConfigAssets.EnsureAll();
            return AssetDatabase.LoadAssetAtPath<TrafficJamConfig>(ConfigAssets.TrafficJamConfigPath);
        }

        sealed class Generated
        {
            public TrafficLevel Level;
            public int Attempts, SeedTries;
            public double Ms;
        }

        /// <summary>One Story level from its fixed seed, or the next fixed ones (pure: runs on any thread).</summary>
        public static TrafficLevel GenerateStoryLevel(TrafficJamConfig cfg, DifficultyTier tier, int n, out int attempts, out int seedTries)
        {
            var settings = TrafficGenerator.Settings.FromTier(tier, cfg.storyMaxAttempts, cfg.storyMaxStates, cfg.maxMinMoves, cfg.climbSteps);
            for (int k = 0; k <= SeedRetries; k++)
            {
                try
                {
                    var r = TrafficGenerator.Generate(settings, cfg.storySeedBase + n + k * SeedRetryStep, tier.id, n);
                    attempts = r.Attempts;
                    seedTries = k + 1;
                    return r.Level;
                }
                catch (InvalidOperationException) { }
            }
            throw new InvalidOperationException($"Traffic jam: no Story level {n} (tier {tier.id}) from {SeedRetries + 1} seeds");
        }

        static string Run()
        {
            var cfg = LoadConfig();
            var table = cfg.storyDifficulty;
            int last = table.LastLevel;
            if (last <= 0) throw new InvalidOperationException("TrafficJamConfig: the Story difficulty table is empty");
            if (last % StoryBook.LevelsPerChapter != 0)
                throw new InvalidOperationException($"TrafficJamConfig: {last} Story levels is not a whole number of {StoryBook.LevelsPerChapter}-level chapters");
            int chapters = last / StoryBook.LevelsPerChapter;
            if (cfg.chapters.Count < chapters) throw new InvalidOperationException($"TrafficJamConfig: {chapters} chapters needed, {cfg.chapters.Count} defined");

            var total = Stopwatch.StartNew();
            var tasks = new List<Task<Generated>>();
            for (int n = 1; n <= last; n++)
            {
                int level = n;
                var tier = table.ForLevel(n) ?? throw new InvalidOperationException($"TrafficJamConfig: no difficulty tier covers level {n}");
                tasks.Add(Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    var l = GenerateStoryLevel(cfg, tier, level, out int attempts, out int tries);
                    sw.Stop();
                    return new Generated { Level = l, Attempts = attempts, SeedTries = tries, Ms = sw.Elapsed.TotalMilliseconds };
                }));
            }
            Task.WaitAll(tasks.ToArray());
            total.Stop();
            var generated = tasks.Select(t => t.Result).OrderBy(g => g.Level.number).ToList();
            var problems = new List<string>();
            foreach (var g in generated)
            {
                var l = g.Level;
                l.chapter = (l.number - 1) / StoryBook.LevelsPerChapter + 1;
                l.index = (l.number - 1) % StoryBook.LevelsPerChapter;
                // the exact minimum measured again from the saved text, by the solver alone
                var layout = l.CreateLayout(out ulong start);
                var space = TrafficSpace.Explore(layout, start, cfg.maxStates);
                if (space.MinMoves != l.minMoves || space.Count != l.states)
                    problems.Add($"level {l.number}: solver finds minimum {space.MinMoves} ({space.Count} positions), generator saved {l.minMoves} ({l.states})");
            }
            // the difficulty rises inside every chapter, and every chapter starts easier than the end of the previous one
            for (int i = 1; i < generated.Count; i++)
            {
                var a = generated[i - 1].Level;
                var b = generated[i].Level;
                if (a.chapter == b.chapter && b.minMoves < a.minMoves) problems.Add($"level {b.number} ({b.minMoves} moves) is easier than level {a.number} ({a.minMoves})");
                if (a.chapter != b.chapter && b.minMoves >= a.minMoves) problems.Add($"chapter {b.chapter} starts at {b.minMoves} moves, not easier than the end of chapter {a.chapter} ({a.minMoves})");
            }

            var set = new TrafficLevelSet { levels = generated.Select(g => g.Level).ToList() };
            Directory.CreateDirectory(ConfigAssets.TrafficJamDir);
            File.WriteAllText(ConfigAssets.TrafficJamStoryPath, JsonUtility.ToJson(set, true) + "\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ConfigAssets.TrafficJamStoryPath);

            string timing = SolverTiming(cfg);
            WriteDoc(cfg, generated, problems, timing, total.Elapsed.TotalSeconds);
            var log = new StringBuilder($"TrafficJamLevelTools: {generated.Count} Story levels in {chapters} chapters saved in {ConfigAssets.TrafficJamStoryPath} " +
                                        $"({total.Elapsed.TotalSeconds:0.0} s in parallel), table in {DocPath}\n");
            foreach (var g in generated)
                log.Append($"  {g.Level.number,2} (c{g.Level.chapter}): {g.Level.width}x{g.Level.height} min {g.Level.minMoves} positions {g.Level.states} " +
                           $"seed {g.Level.seed} ({g.Attempts} attempts, {g.SeedTries} seed(s), {g.Ms:0} ms)\n");
            foreach (var p in problems) log.Append("  PROBLEM ").Append(p).Append('\n');
            log.Append(timing);
            if (problems.Count > 0) Debug.LogWarning("TrafficJamLevelTools: " + problems.Count + " problem(s), see the log");
            return log.ToString();
        }

        /// <summary>
        /// Times on 7x9 (TJ-001): the exact solver alone (every position and the distance table) on the 7x9 Story levels, the light bulb
        /// (table + next move, as on the phone the first time), and the Challenge generator on its 7x9 steps (<see cref="TimingSamples"/> seeds).
        /// </summary>
        public static string SolverTiming(TrafficJamConfig cfg)
        {
            var sb = new StringBuilder("Traffic jam timing (Editor, Mono; exact minimum, no estimate)\n");
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigAssets.TrafficJamStoryPath);
            var levels = text != null ? JsonUtility.FromJson<TrafficLevelSet>(text.text)?.levels : null;
            var big = levels?.Where(l => l.width == 7).ToList() ?? new List<TrafficLevel>();
            if (big.Count > 0)
            {
                var first = big[0].CreateLayout(out ulong s0);
                TrafficSpace.Explore(first, s0, cfg.maxStates);   // warm-up (JIT)
                var solve = new List<double>();
                var hint = new List<double>();
                foreach (var l in big)
                {
                    var layout = l.CreateLayout(out ulong start);
                    var sw = Stopwatch.StartNew();
                    var space = TrafficSpace.Explore(layout, start, cfg.maxStates);
                    sw.Stop();
                    solve.Add(sw.Elapsed.TotalMilliseconds);
                    var puzzle = new TrafficPuzzle { MaxStates = cfg.maxStates };
                    puzzle.Load(l);
                    sw.Restart();
                    puzzle.TryFindHint(out _);
                    sw.Stop();
                    hint.Add(sw.Elapsed.TotalMilliseconds);
                }
                sb.Append($"  solver, {big.Count} Story levels 7x9 ({big.Min(l => l.states)}-{big.Max(l => l.states)} positions): average {solve.Average():0.0} ms, max {solve.Max():0.0} ms\n");
                sb.Append($"  light bulb, first hint of a 7x9 level (table + move): average {hint.Average():0.0} ms, max {hint.Max():0.0} ms\n");
            }
            var source = new TrafficChallenge(cfg.challengeDifficulty, cfg.challenge, cfg.challengeMaxAttempts, cfg.challengeMaxStates, cfg.maxMinMoves, cfg.climbSteps);
            foreach (var tier in cfg.challengeDifficulty.tiers.Where(t => t.Get(TrafficDifficulty.Cols).min == 7))
            {
                var times = new List<double>();
                var mins = new List<int>();
                for (int i = 0; i < TimingSamples; i++)
                {
                    var sw = Stopwatch.StartNew();
                    var level = ChallengeRules.Generate(source, TimingSeedBase + i, tier.firstLevel, 0);
                    sw.Stop();
                    times.Add(sw.Elapsed.TotalMilliseconds);
                    mins.Add(level.minMoves);
                }
                sb.Append($"  Challenge generator {tier.id} (7x9, {tier.GetOr(TrafficDifficulty.Targets, 1).min} target car(s)), {TimingSamples} seeds: average {times.Average():0} ms, " +
                          $"max {times.Max():0} ms, minimum moves {mins.Min()}-{mins.Max()}\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Prova Challenge (TJ-001): the levels 1-70 of a run seed (step, grid, cars, trucks, barriers, target cars, side exits, minimum,
        /// maximum, time), with the average time per step (log + docs/trafficjam-challenge.md).
        /// </summary>
        public static string ChallengeTrial(int runSeed)
        {
            var cfg = LoadConfig();
            var source = new TrafficChallenge(cfg.challengeDifficulty, cfg.challenge, cfg.challengeMaxAttempts, cfg.challengeMaxStates, cfg.maxMinMoves, cfg.climbSteps);
            ChallengeRules.Generate(source, runSeed - 1, 1, 0);   // warm-up (JIT)
            var levels = new List<ChallengeLevel>();
            int lastLevel = cfg.challengeDifficulty.LastLevel;
            for (int n = 1; n <= lastLevel; n++)
            {
                var sw = Stopwatch.StartNew();
                var level = ChallengeRules.Generate(source, runSeed, n, 0);
                sw.Stop();
                level.milliseconds = sw.Elapsed.TotalMilliseconds;
                levels.Add(level);
            }
            var sb = new StringBuilder("# Traffic jam — prova della Challenge\n\n");
            sb.Append("File generato da *Resolver Master > Traffic jam > Prova Challenge* (non modificare a mano). ");
            sb.Append($"Seed della run {runSeed}, livelli 1–{lastLevel}, 0 cambi. Massimo = minimo + margine ");
            sb.Append($"(dal {cfg.challenge.marginStartPercent}% al {cfg.challenge.marginEndPercent}% del minimo, almeno +1). ");
            sb.Append($"Generatore: al massimo {cfg.challengeMaxAttempts} tentativi per seed, {cfg.challengeMaxStates} posizioni per disposizione esplorata.\n\n");
            sb.Append("| Livello | Gradino | Griglia | Auto | Camion | Barriere | Auto obiettivo | Uscite laterali | Minimo | Massimo | Seed usato | Tempo (ms) |\n");
            sb.Append("|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|\n");
            foreach (var l in levels)
            {
                var layout = TrafficLayout.Parse(l.data, out _);
                sb.Append($"| {l.number} | {l.tierId} | {layout.Width}x{layout.Height} | {layout.Count(VehicleKind.Car)} | {layout.Count(VehicleKind.Truck)} | " +
                          $"{layout.ObstacleCount} | {layout.TargetCount} | {layout.SideExitCount} | {l.minMoves} | {l.maxMoves} | {l.seed} | {l.milliseconds:0} |\n");
            }
            sb.Append("\n## Tempo per gradino\n\n| Gradino | Livelli | Griglia | Minimo | Massimo | Media (ms) | Massimo (ms) |\n|---|---:|---|---|---|---:|---:|\n");
            foreach (var g in levels.GroupBy(l => l.tierId))
            {
                var layout = TrafficLayout.Parse(g.First().data, out _);
                sb.Append($"| {g.Key} | {g.First().number}–{g.Last().number} | {layout.Width}x{layout.Height} | {g.Min(l => l.minMoves)}–{g.Max(l => l.minMoves)} | " +
                          $"{g.Min(l => l.maxMoves)}–{g.Max(l => l.maxMoves)} | {g.Average(l => l.milliseconds):0} | {g.Max(l => l.milliseconds):0} |\n");
            }
            sb.Append("\nI livelli successivi si preparano in sottofondo (2 in anticipo, `ChallengeQueue` del Core): il tempo di generazione non blocca il gioco.\n");
            File.WriteAllText(ChallengeDocPath, sb.ToString(), new UTF8Encoding(false));
            return "TrafficJamLevelTools: Challenge trial written to " + ChallengeDocPath + "\n" + sb;
        }

        static void WriteDoc(TrafficJamConfig cfg, List<Generated> levels, List<string> problems, string timing, double seconds)
        {
            var sb = new StringBuilder();
            sb.Append("# Traffic jam — livelli della Storia\n\n");
            sb.Append("File generato da *Resolver Master > Traffic jam > Genera livelli Storia* (non modificare a mano). Regole: `docs/trafficjam-regole.md`.\n");
            sb.Append($"Dati del gioco: `{ConfigAssets.TrafficJamStoryPath}`. Seed del livello n = {cfg.storySeedBase} + n (se quel seed non dà un livello: + {SeedRetryStep}, + {2 * SeedRetryStep}...). ");
            sb.Append($"Generazione: {seconds:0} s in parallelo. Mosse minime esatte (ricerca in ampiezza su tutte le posizioni).\n\n");
            sb.Append("| Cap. | Livelli | Griglie | Mosse minime |\n|---|---|---|---|\n");
            foreach (var chapter in levels.GroupBy(g => g.Level.chapter))
            {
                var theme = chapter.Key - 1 < cfg.chapters.Count ? cfg.chapters[chapter.Key - 1] : null;
                var grids = string.Join(" → ", chapter.Select(g => g.Level.width + "x" + g.Level.height).Distinct());
                sb.Append($"| {chapter.Key}{(theme != null ? " `" + theme.nameKey + "`" : "")} | {chapter.First().Level.number}–{chapter.Last().Level.number} | {grids} | " +
                          $"{string.Join(", ", chapter.Select(g => g.Level.minMoves))} |\n");
            }
            sb.Append(problems.Count == 0 ? "\nControllo: la difficoltà non scende mai dentro un capitolo e ogni capitolo parte più facile della fine del precedente.\n"
                : "\n**Problemi**:\n" + string.Join("\n", problems.Select(p => "- " + p)) + "\n");
            sb.Append("\n## Livelli\n\n| Livello | Cap. | Gradino | Griglia | Auto | Camion | Barriere | Auto obiettivo | Uscite laterali | Mosse minime | Posizioni | Seed | Tentativi |\n");
            sb.Append("|---:|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|\n");
            foreach (var g in levels)
            {
                var l = g.Level;
                var layout = l.CreateLayout(out _);
                sb.Append($"| {l.number} | {l.chapter} | {l.tier} | {l.width}x{l.height} | {layout.Count(VehicleKind.Car)} | {layout.Count(VehicleKind.Truck)} | " +
                          $"{layout.ObstacleCount} | {layout.TargetCount} | {layout.SideExitCount} | {l.minMoves} | {l.states} | {l.seed} | {g.Attempts} |\n");
            }
            sb.Append("\n## Gradini di difficoltà (TrafficJamConfig)\n\n| Gradino | Livello | Valori |\n|---|---|---|\n");
            foreach (var t in cfg.storyDifficulty.tiers)
                sb.Append($"| {t.id} | {t.firstLevel} | {string.Join(", ", t.values.Select(v => v.ToString()))} |\n");
            sb.Append("\n## Posizioni iniziali\n\nFormato di `TrafficLayout`: `LxA`, poi `R`/`B` auto obiettivo rossa/blu, `C` auto, `T` camion + `x.y` (casella in alto a ");
            sb.Append("sinistra) + `h`/`v` (orizzontale/verticale) + lato dell'uscita `U`/`R`/`D`/`L` per le auto obiettivo; `#x.y` barriera.\n\n");
            foreach (var g in levels) sb.Append($"- {g.Level.number}: `{g.Level.cells}`\n");
            sb.Append("\n## Tempi\n\n```\n").Append(timing).Append("```\n");
            Directory.CreateDirectory(Path.GetDirectoryName(DocPath));
            File.WriteAllText(DocPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
