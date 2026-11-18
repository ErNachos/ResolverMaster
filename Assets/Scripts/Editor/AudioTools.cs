using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using ResolverMaster.Core.Audio;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// ErNa-010, the sound files. Resolver Master > Audio > Importa suoni: for every event of <see cref="AudioCatalog.KenneyFiles"/> the
    /// chosen file is looked for under ArtSource/Audio (folders, or inside the downloaded .zip packs) and copied to
    /// Assets/Resources/Audio/Sfx/&lt;event&gt;.&lt;ext&gt;; the music ArtSource/Audio/Music/Music_*.(ogg|mp3|wav) goes to
    /// Assets/Resources/Audio/Music/. ArtSource is only read. The report lists the events left silent.
    /// Batch: -executeMethod ResolverMaster.Editor.AudioTools.ImportBatch
    /// </summary>
    public static class AudioTools
    {
        public const string SourceDir = "ArtSource/Audio";
        public const string SfxDir = "Assets/Resources/Audio/Sfx";
        public const string MusicDir = "Assets/Resources/Audio/Music";
        static readonly string[] MusicExtensions = { ".ogg", ".mp3", ".wav" };
        static readonly string[] MusicKeys = { AudioCatalog.MusicHome, "Music_Plumber", "Music_TrafficJam", "Music_Penguin" };

        [MenuItem("Resolver Master/Audio/Importa suoni")]
        public static void Import() => Debug.Log(Run());

        public static void ImportBatch()
        {
            try
            {
                Debug.Log(Run());
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static string Run()
        {
            var log = new StringBuilder("AudioTools:\n");
            Directory.CreateDirectory(SfxDir);
            Directory.CreateDirectory(MusicDir);
            var files = Directory.Exists(SourceDir) ? Directory.GetFiles(SourceDir, "*", SearchOption.AllDirectories) : Array.Empty<string>();
            var silent = new List<string>();
            foreach (var kv in AudioCatalog.KenneyFiles)
            {
                string evt = kv.Key, wanted = kv.Value;
                if (string.IsNullOrEmpty(wanted) || !CopySound(files, wanted, Path.Combine(SfxDir, evt + Path.GetExtension(wanted)), log)) silent.Add(evt);
            }
            foreach (var key in MusicKeys)
            {
                var src = MusicExtensions.Select(e => Path.Combine(SourceDir, "Music", key + e)).FirstOrDefault(File.Exists);
                if (src == null)
                {
                    log.Append($"  music {key}: missing in {SourceDir}/Music (silence)\n");
                    continue;
                }
                File.Copy(src, Path.Combine(MusicDir, key + Path.GetExtension(src)), true);
                log.Append($"  music {key} <- {src}\n");
            }
            AssetDatabase.Refresh();
            log.Append(silent.Count == 0 ? "  every event has its sound\n" : $"  {silent.Count} event(s) without a sound (silence): {string.Join(", ", silent)}\n");
            return log.ToString();
        }

        /// <summary>The file named <paramref name="wanted"/> in a folder or in a .zip under ArtSource/Audio, copied to <paramref name="dest"/>.</summary>
        static bool CopySound(string[] files, string wanted, string dest, StringBuilder log)
        {
            var direct = files.FirstOrDefault(f => string.Equals(Path.GetFileName(f), wanted, StringComparison.OrdinalIgnoreCase));
            if (direct != null)
            {
                File.Copy(direct, dest, true);
                log.Append($"  {Path.GetFileName(dest)} <- {direct}\n");
                return true;
            }
            foreach (var zip in files.Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
            {
                using (var archive = ZipFile.OpenRead(zip))
                {
                    var entry = archive.Entries.FirstOrDefault(e => string.Equals(e.Name, wanted, StringComparison.OrdinalIgnoreCase));
                    if (entry == null) continue;
                    using (var input = entry.Open())
                    using (var output = File.Create(dest))
                        input.CopyTo(output);
                    log.Append($"  {Path.GetFileName(dest)} <- {zip}:{entry.FullName}\n");
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// ErNa-010: import settings of the sounds for the phone: short effects decompressed on load (no delay when played, Vorbis, mono),
    /// music streamed (Vorbis, stereo kept).
    /// </summary>
    public sealed class AudioImportRules : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            bool music = assetPath.StartsWith(AudioTools.MusicDir + "/", StringComparison.Ordinal);
            bool sfx = assetPath.StartsWith(AudioTools.SfxDir + "/", StringComparison.Ordinal);
            if (!music && !sfx) return;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.quality = music ? 0.5f : 0.6f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = sfx;
            importer.loadInBackground = music;
        }
    }
}
