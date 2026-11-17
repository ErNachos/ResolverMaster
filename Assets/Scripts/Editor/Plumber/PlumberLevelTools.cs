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
    /// of a level of the last tier (6x6, two colors).
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

            string timing = MeasureTiming(cfg, last);
            WriteDoc(cfg, generated, timing);
            var log = new StringBuilder($"PlumberLevelTools: {generated.Count} Story levels in {chapters} chapters saved in {ConfigAssets.PlumberStoryPath}, table in {DocPath}\n");
            foreach (var g in generated)
                log.Append($"  {g.Level.number,2} (c{g.Level.chapter}): {g.Level.width}x{g.Level.height} min {g.Level.minMoves} path {g.Level.path} " +
                           $"seed {g.Level.seed} ({g.Attempts} attempts, {g.Ms:0.0} ms)\n");
            log.Append("  ").Append(timing);
            Debug.Log(log.ToString());
        }

        /// <summary>Average time to generate a level of the last tier (6x6 two colors in chapter 3) over <see cref="TimingSamples"/> other seeds.</summary>
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
            return $"generator {settings.Size}x{settings.Size} {settings.Layout} (tier {tier.id}), {TimingSamples} seeds: average {times.Average():0.00} ms " +
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
            sb.Append("*Percorso* = tubi attraversati dall'acqua nella soluzione ottima.\n");
            foreach (var chapter in levels.GroupBy(g => g.Level.chapter))
            {
                var theme = chapter.Key - 1 < cfg.chapters.Count ? cfg.chapters[chapter.Key - 1] : null;
                sb.Append($"\n## Capitolo {chapter.Key}{(theme != null ? " — `" + theme.nameKey + "`" : "")}\n\n");
                sb.Append("| Livello | Nel cap. | Gradino | Griglia | Fonti | Arrivi | Colori | Dritti | Curve | T | Croci | Mosse minime | Percorso | Seed | Tentativi |\n");
                sb.Append("|---:|---:|---|---|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|\n");
                foreach (var g in chapter)
                {
                    var l = g.Level;
                    var grid = l.CreateGrid();
                    sb.Append($"| {l.number} | {l.index + 1} | {l.tier} | {l.width}x{l.height} | {grid.Count(PieceKind.Source)} | {grid.Count(PieceKind.Sink)} | " +
                              $"{grid.ColorCount()} | {grid.Count(PieceKind.Straight)} | {grid.Count(PieceKind.Curve)} | " +
                              $"{grid.Count(PieceKind.Tee)} ({g.Solution.TeesOnPath}) | {grid.Count(PieceKind.Cross)} | {l.minMoves} | {l.path} | {l.seed} | {g.Attempts} |\n");
                }
            }
            sb.Append("\n## Gradini di difficoltà (PlumberConfig)\n\n| Gradino | Livelli | Valori |\n|---|---|---|\n");
            foreach (var t in cfg.storyDifficulty.tiers)
                sb.Append($"| {t.id} | {(t.firstLevel == t.lastLevel ? t.firstLevel.ToString() : t.firstLevel + "–" + t.lastLevel)} | {string.Join(", ", t.values.Select(v => v.ToString()))} |\n");
            sb.Append("\n## Griglie iniziali\n\nFormato di `PlumberLevelCodec`: righe dall'alto separate da `/`; `I` dritto, `C` curva, `T`, `X` croce, ");
            sb.Append("`S` fonte, `E` arrivo (fontana); la cifra è il numero di rotazioni orarie dalla forma base; `@1` = colore 1 (arancione), senza = colore 0 (blu).\n\n");
            foreach (var g in levels) sb.Append($"- {g.Level.number}: `{g.Level.cells}`\n");
            sb.Append("\n## Tempo del generatore\n\n").Append(timing).Append(" (Editor, misurato all'ultima generazione).\n");
            Directory.CreateDirectory(Path.GetDirectoryName(DocPath));
            File.WriteAllText(DocPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
