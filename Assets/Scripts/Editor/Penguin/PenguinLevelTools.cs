using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Games.Penguin;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ResolverMaster.Editor.Penguin
{
    /// <summary>
    /// Pingù tools (PG-001), on the model of the other games.
    /// Genera livelli Storia: every Story level of the PenguinConfig table from its fixed seed (storySeedBase + n; when a seed gives no
    /// level within the attempts, the next fixed seeds storySeedBase + n + 1000·k are tried), in parallel (each level is independent and
    /// deterministic); the exact minimum is measured again by the solver; chapters of 9; saved as game data
    /// (Resources/Games/Penguin/penguin_story.json) with the table in docs/penguin-livelli.md. Nothing is saved when a level is missing or
    /// the difficulty drops inside a chapter.
    /// Batch: -executeMethod ResolverMaster.Editor.Penguin.PenguinLevelTools.GenerateStoryLevelsBatch
    /// </summary>
    public static class PenguinLevelTools
    {
        public const string DocPath = "docs/penguin-livelli.md";
        public const string ChallengeDocPath = "docs/penguin-challenge.md";
        public const int SeedRetries = 8, SeedRetryStep = 1000;
        public const int ChallengeTrialSeed = 20261118;
        public const int TimingSamples = 20, TimingSeedBase = 700000;

        [MenuItem("Resolver Master/Pingù/Genera livelli Storia")]
        public static void GenerateStoryLevels() => Debug.Log(Run());

        public static void GenerateStoryLevelsBatch() => Batch(() => Debug.Log(Run()));

        [MenuItem("Resolver Master/Pingù/Prova Challenge")]
        public static void TryChallenge() => Debug.Log(ChallengeTrial(ChallengeTrialSeed));

        public static void TryChallengeBatch() => Batch(() => Debug.Log(ChallengeTrial(ChallengeTrialSeed)));

        [MenuItem("Resolver Master/Pingù/Misura risolutore")]
        public static void MeasureSolver() => Debug.Log(Timing(LoadConfig()));

        public static void MeasureSolverBatch() => Batch(() => Debug.Log(Timing(LoadConfig())));

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

        static PenguinConfig LoadConfig()
        {
            ConfigAssets.EnsureAll();
            return AssetDatabase.LoadAssetAtPath<PenguinConfig>(ConfigAssets.PenguinConfigPath);
        }

        sealed class Generated
        {
            public int Number;
            public PenguinLevel Level;
            public int Attempts, SeedTries;
            public double Ms;
            public string Error;
            public string Stats;
        }

        public static PenguinGenerator.Settings StorySettings(PenguinConfig cfg, DifficultyTier tier) =>
            PenguinGenerator.Settings.FromTier(tier, cfg.storyMaxAttempts, cfg.storyMaxStates, cfg.maxMinMoves, cfg.climbSteps);

        /// <summary>One Story level from its fixed seed, or the next fixed ones (pure: runs on any thread).</summary>
        public static PenguinLevel GenerateStoryLevel(PenguinConfig cfg, DifficultyTier tier, int n, out int attempts, out int seedTries,
            PenguinGenerator.Stats stats = null)
        {
            var settings = StorySettings(cfg, tier);
            settings.Counters = stats;
            attempts = 0;
            for (int k = 0; k <= SeedRetries; k++)
            {
                try
                {
                    var r = PenguinGenerator.Generate(settings, cfg.storySeedBase + n + k * SeedRetryStep, tier.id, n);
                    attempts += r.Attempts;
                    seedTries = k + 1;
                    return r.Level;
                }
                catch (InvalidOperationException) { attempts += settings.MaxAttempts; }
            }
            throw new InvalidOperationException($"Pingù: no Story level {n} (tier {tier.id}) from {SeedRetries + 1} seeds");
        }

        static string Run()
        {
            var cfg = LoadConfig();
            var table = cfg.storyDifficulty;
            int last = table.LastLevel;
            if (last <= 0) throw new InvalidOperationException("PenguinConfig: the Story difficulty table is empty");
            if (last % StoryBook.LevelsPerChapter != 0)
                throw new InvalidOperationException($"PenguinConfig: {last} Story levels is not a whole number of {StoryBook.LevelsPerChapter}-level chapters");
            int chapters = last / StoryBook.LevelsPerChapter;
            if (cfg.chapters.Count < chapters) throw new InvalidOperationException($"PenguinConfig: {chapters} chapters needed, {cfg.chapters.Count} defined");

            var total = Stopwatch.StartNew();
            var tasks = new List<Task<Generated>>();
            for (int n = 1; n <= last; n++)
            {
                int level = n;
                var tier = table.ForLevel(n) ?? throw new InvalidOperationException($"PenguinConfig: no difficulty tier covers level {n}");
                tasks.Add(Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    var stats = new PenguinGenerator.Stats();
                    try
                    {
                        var l = GenerateStoryLevel(cfg, tier, level, out int attempts, out int tries, stats);
                        return new Generated { Number = level, Level = l, Attempts = attempts, SeedTries = tries, Ms = sw.Elapsed.TotalMilliseconds, Stats = stats.ToString() };
                    }
                    catch (InvalidOperationException e)
                    {
                        return new Generated { Number = level, Error = e.Message, Ms = sw.Elapsed.TotalMilliseconds, Stats = stats.ToString() };
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
            total.Stop();
            var generated = tasks.Select(t => t.Result).OrderBy(g => g.Number).ToList();
            var problems = new List<string>();
            foreach (var g in generated.Where(g => g.Error != null)) problems.Add($"level {g.Number}: {g.Error} ({g.Stats})");
            var ok = generated.Where(g => g.Level != null).ToList();
            foreach (var g in ok)
            {
                var l = g.Level;
                l.chapter = (l.number - 1) / StoryBook.LevelsPerChapter + 1;
                l.index = (l.number - 1) % StoryBook.LevelsPerChapter;
                var layout = l.CreateLayout();
                var space = PenguinSpace.Explore(layout, layout.Start, cfg.maxStates);
                if (space.MinMoves != l.minMoves || space.Count != l.states)
                    problems.Add($"level {l.number}: solver finds minimum {space.MinMoves} ({space.Count} states), generator saved {l.minMoves} ({l.states})");
            }
            for (int i = 1; i < ok.Count; i++)
            {
                var a = ok[i - 1].Level;
                var b = ok[i].Level;
                if (b.number != a.number + 1) continue;
                if (a.chapter == b.chapter && b.minMoves < a.minMoves) problems.Add($"level {b.number} ({b.minMoves} moves) is easier than level {a.number} ({a.minMoves})");
                if (a.chapter != b.chapter && b.minMoves >= a.minMoves) problems.Add($"chapter {b.chapter} starts at {b.minMoves} moves, not easier than the end of chapter {a.chapter} ({a.minMoves})");
            }

            var log = new StringBuilder($"PenguinLevelTools: {ok.Count} of {last} Story levels in {total.Elapsed.TotalSeconds:0.0} s (parallel)\n");
            foreach (var g in generated)
            {
                if (g.Level == null)
                {
                    log.Append($"  {g.Number,2}: FAILED {g.Ms:0} ms - {g.Stats}\n");
                    continue;
                }
                log.Append($"  {g.Level.number,2} (c{g.Level.chapter}): {g.Level.width}x{g.Level.height} min {g.Level.minMoves} states {g.Level.states} " +
                           $"seed {g.Level.seed} ({g.Attempts} attempts, {g.SeedTries} seed(s), {g.Ms:0} ms) {g.Level.cells}\n    {g.Stats}\n");
            }
            foreach (var p in problems) log.Append("  PROBLEM ").Append(p).Append('\n');
            if (ok.Count != last || problems.Count > 0)
            {
                Debug.LogWarning("PenguinLevelTools: " + problems.Count + " problem(s): nothing saved, see the log");
                return log.ToString();
            }

            var set = new PenguinLevelSet { levels = ok.Select(g => g.Level).ToList() };
            Directory.CreateDirectory(ConfigAssets.PenguinDir);
            File.WriteAllText(ConfigAssets.PenguinStoryPath, JsonUtility.ToJson(set, true) + "\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ConfigAssets.PenguinStoryPath);
            string timing = Timing(cfg);
            WriteDoc(cfg, ok, timing, total.Elapsed.TotalSeconds);
            log.Append($"saved in {ConfigAssets.PenguinStoryPath}, table in {DocPath}\n").Append(timing);
            return log.ToString();
        }

        /// <summary>
        /// Times on 7x9 (PG-001): the exact solver (every state and the distance table, which also answers "You are stuck" and the light
        /// bulb) on the 7x9 Story levels; the check after a move and the light bulb once the table is there; the Challenge generator on
        /// its 7x9 steps (<see cref="TimingSamples"/> seeds).
        /// </summary>
        public static string Timing(PenguinConfig cfg)
        {
            var sb = new StringBuilder("Pingù timing (Editor, Mono; exact minimum, no estimate)\n");
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigAssets.PenguinStoryPath);
            var levels = text != null ? JsonUtility.FromJson<PenguinLevelSet>(text.text)?.levels : null;
            var big = levels?.Where(l => l.width == 7).ToList() ?? new List<PenguinLevel>();
            if (big.Count > 0)
            {
                var first = big[0].CreateLayout();
                PenguinSpace.Explore(first, first.Start, cfg.maxStates);   // warm-up (JIT)
                var solve = new List<double>();
                var check = new List<double>();
                foreach (var l in big)
                {
                    var puzzle = new PenguinPuzzle { MaxStates = cfg.maxStates };
                    puzzle.Load(l);
                    var sw = Stopwatch.StartNew();
                    bool stuck = puzzle.IsStuck;   // builds the table (once per level)
                    sw.Stop();
                    solve.Add(sw.Elapsed.TotalMilliseconds);
                    sw.Restart();
                    for (int i = 0; i < 100; i++)
                    {
                        stuck |= puzzle.IsStuck;
                        puzzle.TryFindHint(out _);
                    }
                    sw.Stop();
                    check.Add(sw.Elapsed.TotalMilliseconds / 100.0);
                    if (stuck) sb.Append($"  level {l.number}: stuck at the start?!\n");
                }
                sb.Append($"  solver (table of every state), {big.Count} Story levels 7x9 ({big.Min(l => l.states)}-{big.Max(l => l.states)} states): " +
                          $"average {solve.Average():0.0} ms, max {solve.Max():0.0} ms\n");
                sb.Append($"  \"You are stuck\" + light bulb with the table: average {check.Average() * 1000:0.0} µs, max {check.Max() * 1000:0.0} µs\n");
            }
            var source = cfg.CreateChallenge();
            foreach (var tier in cfg.challengeDifficulty.tiers.Where(t => t.Get(PenguinDifficulty.Cols).min == 7))
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
                sb.Append($"  Challenge generator {tier.id} (7x9, {tier.GetOr(PenguinDifficulty.Penguins, 1).min} penguin(s), {tier.GetOr(PenguinDifficulty.Blocks, 0)}), " +
                          $"{TimingSamples} seeds: average {times.Average():0} ms, max {times.Max():0} ms, minimum moves {mins.Min()}-{mins.Max()}\n");
            }
            return sb.ToString();
        }

        /// <summary>Prova Challenge (PG-001): the levels 1-70 of a run seed with the average time per step (log + docs/penguin-challenge.md).</summary>
        public static string ChallengeTrial(int runSeed)
        {
            var cfg = LoadConfig();
            var source = cfg.CreateChallenge();
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
            var sb = new StringBuilder("# Pingù — prova della Challenge\n\n");
            sb.Append("File generato da *Resolver Master > Pingù > Prova Challenge* (non modificare a mano). ");
            sb.Append($"Seed della run {runSeed}, livelli 1–{lastLevel}, 0 cambi. Massimo = minimo + margine ");
            sb.Append($"(dal {cfg.challenge.marginStartPercent}% al {cfg.challenge.marginEndPercent}% del minimo, almeno +1). ");
            sb.Append($"Generatore: al massimo {cfg.challengeMaxAttempts} tentativi per seed, {cfg.challengeMaxStates} stati per bozza esplorata.\n\n");
            sb.Append("| Livello | Gradino | Griglia | Rocce | Neve | Buchi | Tratti d'acqua | Pesci | Blocchi | Pinguini | Minimo | Massimo | Seed usato | Tempo (ms) |\n");
            sb.Append("|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|\n");
            foreach (var l in levels)
            {
                var layout = PenguinLayout.Parse(l.data);
                sb.Append($"| {l.number} | {l.tierId} | {layout.Width}x{layout.Height} | {layout.Count(PenguinTerrain.Rock)} | {layout.Count(PenguinTerrain.Snow)} | " +
                          $"{layout.Count(PenguinTerrain.Water)} | {layout.WaterEdgeSegments} | {layout.FishCount} | {layout.BlockCount} | {layout.PenguinCount} | " +
                          $"{l.minMoves} | {l.maxMoves} | {l.seed} | {l.milliseconds:0} |\n");
            }
            sb.Append("\n## Tempo per gradino\n\n| Gradino | Livelli | Griglia | Minimo | Massimo | Media (ms) | Massimo (ms) |\n|---|---:|---|---|---|---:|---:|\n");
            foreach (var g in levels.GroupBy(l => l.tierId))
            {
                var layout = PenguinLayout.Parse(g.First().data);
                sb.Append($"| {g.Key} | {g.First().number}–{g.Last().number} | {layout.Width}x{layout.Height} | {g.Min(l => l.minMoves)}–{g.Max(l => l.minMoves)} | " +
                          $"{g.Min(l => l.maxMoves)}–{g.Max(l => l.maxMoves)} | {g.Average(l => l.milliseconds):0} | {g.Max(l => l.milliseconds):0} |\n");
            }
            sb.Append("\nI livelli successivi si preparano in sottofondo (2 in anticipo, `ChallengeQueue` del Core): il tempo di generazione non blocca il gioco.\n");
            Directory.CreateDirectory(Path.GetDirectoryName(ChallengeDocPath));
            File.WriteAllText(ChallengeDocPath, sb.ToString(), new UTF8Encoding(false));
            return "PenguinLevelTools: Challenge trial written to " + ChallengeDocPath + "\n" + sb;
        }

        static void WriteDoc(PenguinConfig cfg, List<Generated> levels, string timing, double seconds)
        {
            var sb = new StringBuilder();
            sb.Append("# Pingù — livelli della Storia\n\n");
            sb.Append("File generato da *Resolver Master > Pingù > Genera livelli Storia* (non modificare a mano). Regole: `docs/penguin-regole.md`.\n");
            sb.Append($"Dati del gioco: `{ConfigAssets.PenguinStoryPath}`. Seed del livello n = {cfg.storySeedBase} + n (se quel seed non dà un livello: + {SeedRetryStep}, + {2 * SeedRetryStep}...). ");
            sb.Append($"Generazione: {seconds:0} s in parallelo. Mosse minime esatte (ricerca in ampiezza su tutti gli stati).\n\n");
            sb.Append("| Cap. | Livelli | Griglie | Mosse minime |\n|---|---|---|---|\n");
            foreach (var chapter in levels.GroupBy(g => g.Level.chapter))
            {
                var theme = chapter.Key - 1 < cfg.chapters.Count ? cfg.chapters[chapter.Key - 1] : null;
                var grids = string.Join(" → ", chapter.Select(g => g.Level.width + "x" + g.Level.height).Distinct());
                sb.Append($"| {chapter.Key}{(theme != null ? " `" + theme.nameKey + "`" : "")} | {chapter.First().Level.number}–{chapter.Last().Level.number} | {grids} | " +
                          $"{string.Join(", ", chapter.Select(g => g.Level.minMoves))} |\n");
            }
            sb.Append("\nControllo: la difficoltà non scende mai dentro un capitolo e ogni capitolo parte più facile della fine del precedente.\n");
            sb.Append("\n## Livelli\n\n| Livello | Cap. | Gradino | Griglia | Rocce | Neve | Buchi | Tratti d'acqua | Pesci | Blocchi | Pinguini | Mosse minime | Stati | Seed | Tentativi |\n");
            sb.Append("|---:|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|\n");
            foreach (var g in levels)
            {
                var l = g.Level;
                var layout = l.CreateLayout();
                sb.Append($"| {l.number} | {l.chapter} | {l.tier} | {l.width}x{l.height} | {layout.Count(PenguinTerrain.Rock)} | {layout.Count(PenguinTerrain.Snow)} | " +
                          $"{layout.Count(PenguinTerrain.Water)} | {layout.WaterEdgeSegments} | {layout.FishCount} | {layout.BlockCount} | {layout.PenguinCount} | " +
                          $"{l.minMoves} | {l.states} | {l.seed} | {g.Attempts} |\n");
            }
            sb.Append("\n## Gradini di difficoltà (PenguinConfig)\n\n| Gradino | Livello | Valori |\n|---|---|---|\n");
            foreach (var t in cfg.storyDifficulty.tiers)
                sb.Append($"| {t.id} | {t.firstLevel} | {string.Join(", ", t.values.Select(v => v.ToString()))} |\n");
            sb.Append("\n## Posizioni iniziali\n\nFormato di `PenguinLayout`: `LxA:righe[:bordo]`; `.` ghiaccio, `#` roccia, `s` neve, `w` acqua, `f` pesce, ");
            sb.Append("`c` blocco di ghiaccio, `P`/`Q` pinguini, `I`/`J` i loro igloo; bordo (se c'è acqua): un carattere per lato di casella, ");
            sb.Append("`m` muro di neve, `w` acqua (in alto da sinistra, a destra dall'alto, in basso da sinistra, a sinistra dall'alto).\n\n");
            foreach (var g in levels) sb.Append($"- {g.Level.number}: `{g.Level.cells}`\n");
            sb.Append("\n## Tempi\n\n```\n").Append(timing).Append("```\n");
            Directory.CreateDirectory(Path.GetDirectoryName(DocPath));
            File.WriteAllText(DocPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
