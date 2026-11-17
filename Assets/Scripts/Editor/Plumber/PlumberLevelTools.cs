using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ResolverMaster.Games.Plumber;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ResolverMaster.Editor.Plumber
{
    /// <summary>
    /// Menu Resolver Master > Idraulico > Genera livelli Storia: generates every Story level of the PlumberConfig difficulty table from the
    /// fixed seeds (storySeedBase + level), saves them as game data (Resources/Games/Plumber/plumber_story.json) and writes the table in
    /// docs/idraulico-livelli.md, with the average generation time of a 5x5 level.
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
            public int Attempts;
            public double Ms;
        }

        static void Run()
        {
            ConfigAssets.EnsureAll();
            var cfg = AssetDatabase.LoadAssetAtPath<PlumberConfig>(ConfigAssets.PlumberConfigPath);
            int last = cfg.story.LastLevel;
            if (last <= 0) throw new InvalidOperationException("PlumberConfig: the Story difficulty table is empty");

            var generated = new List<Generated>();
            for (int n = 1; n <= last; n++)
            {
                var tier = cfg.story.ForLevel(n) ?? throw new InvalidOperationException($"PlumberConfig: no difficulty tier covers level {n}");
                var sw = Stopwatch.StartNew();
                var r = PlumberGenerator.Generate(PlumberGenerator.Settings.FromTier(tier, cfg.maxAttempts), cfg.storySeedBase + n, tier.id, n);
                sw.Stop();
                generated.Add(new Generated { Level = r.Level, Attempts = r.Attempts, Ms = sw.Elapsed.TotalMilliseconds });
            }

            var set = new PlumberLevelSet { levels = generated.Select(g => g.Level).ToList() };
            Directory.CreateDirectory(ConfigAssets.PlumberDir);
            File.WriteAllText(ConfigAssets.PlumberStoryPath, JsonUtility.ToJson(set, true) + "\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ConfigAssets.PlumberStoryPath);

            string timing = MeasureTiming(cfg, last);
            WriteDoc(cfg, generated, timing);
            var log = new StringBuilder($"PlumberLevelTools: {generated.Count} Story levels saved in {ConfigAssets.PlumberStoryPath}, table in {DocPath}\n");
            foreach (var g in generated)
                log.Append($"  {g.Level.number,2}: {g.Level.width}x{g.Level.height} min {g.Level.minMoves} path {g.Level.path} seed {g.Level.seed} ({g.Attempts} attempts, {g.Ms:0.0} ms)\n");
            log.Append("  ").Append(timing);
            Debug.Log(log.ToString());
        }

        /// <summary>Average time to generate a level of the last tier (5x5 in chapter 1) over <see cref="TimingSamples"/> other seeds.</summary>
        static string MeasureTiming(PlumberConfig cfg, int lastLevel)
        {
            var tier = cfg.story.ForLevel(lastLevel);
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
            return $"generator {settings.Size}x{settings.Size} (tier {tier.id}), {TimingSamples} seeds: average {times.Average():0.0} ms " +
                   $"(min {times.Min():0.0}, max {times.Max():0.0}), average attempts {attempts.Average():0.0}";
        }

        static void WriteDoc(PlumberConfig cfg, List<Generated> levels, string timing)
        {
            var sb = new StringBuilder();
            sb.Append("# Idraulico — livelli della Storia\n\n");
            sb.Append("File generato da *Resolver Master > Idraulico > Genera livelli Storia* (non modificare a mano).\n");
            sb.Append($"Dati del gioco: `{ConfigAssets.PlumberStoryPath}`. Seed del livello n = {cfg.storySeedBase} + n.\n\n");
            sb.Append("## Capitolo 1\n\n");
            sb.Append("| Livello | Gradino | Griglia | Dritti | Curve | T | Croci | Mosse minime | Percorso | Seed | Tentativi |\n");
            sb.Append("|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|\n");
            foreach (var g in levels)
            {
                var l = g.Level;
                var grid = l.CreateGrid();
                sb.Append($"| {l.number} | {l.tier} | {l.width}x{l.height} | {grid.Count(PieceKind.Straight)} | {grid.Count(PieceKind.Curve)} | " +
                          $"{grid.Count(PieceKind.Tee)} | {grid.Count(PieceKind.Cross)} | {l.minMoves} | {l.path} | {l.seed} | {g.Attempts} |\n");
            }
            sb.Append("\n## Gradini di difficoltà (PlumberConfig)\n\n| Gradino | Livelli | Valori |\n|---|---|---|\n");
            foreach (var t in cfg.story.tiers)
                sb.Append($"| {t.id} | {t.firstLevel}–{t.lastLevel} | {string.Join(", ", t.values.Select(v => v.ToString()))} |\n");
            sb.Append("\n## Griglie iniziali\n\nFormato di `PlumberLevelCodec`: righe dall'alto separate da `/`; `I` dritto, `C` curva, `T`, `X` croce, ");
            sb.Append("`S` fonte, `E` arrivo; la cifra è il numero di rotazioni orarie dalla forma base.\n\n");
            foreach (var g in levels) sb.Append($"- {g.Level.number}: `{g.Level.cells}`\n");
            sb.Append("\n## Tempo del generatore\n\n").Append(timing).Append(" (Editor, misurato all'ultima generazione).\n");
            Directory.CreateDirectory(Path.GetDirectoryName(DocPath));
            File.WriteAllText(DocPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
