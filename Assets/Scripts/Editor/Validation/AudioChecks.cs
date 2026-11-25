using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ResolverMaster.Core.Audio;
using ResolverMaster.Core.Games;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor.Validation
{
    /// <summary>
    /// ErNa-012: every sound event has its generated file (Assets/Audio/Sfx/&lt;file&gt;.wav, Tools/Audio/make_sfx.py), 16-bit PCM, not
    /// empty or silent, not saturated, without a click at the start or the end, not too long (effects 1 s, jingles 2.5 s), with a small
    /// pitch change only; the <see cref="SfxLibrary"/> the game reads holds every file. Music: <see cref="MusicCheck"/>.
    /// </summary>
    public sealed class AudioCheck : IValidationCheck
    {
        public string Name => "Audio";

        const float MaxEffect = 1.0f, MaxJingle = 2.5f;
        const float MinPeak = 0.05f, MaxPeak = 0.98f, MaxEdge = 0.01f;
        const float MinPitch = 0.8f, MaxPitch = 1.25f;

        public void Run(ValidationReport report)
        {
            var events = typeof(AudioEvents).GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()).ToList();
            foreach (var evt in events)
                if (!AudioCatalog.Sounds.ContainsKey(evt)) report.Error($"event '{evt}' has no sound in AudioCatalog.Sounds");
            foreach (var kv in AudioCatalog.Sounds)
            {
                if (!events.Contains(kv.Key)) report.Error($"AudioCatalog.Sounds: '{kv.Key}' is not an event of AudioEvents");
                if (string.IsNullOrEmpty(kv.Value.File)) report.Error($"event '{kv.Key}': no file");
                if (kv.Value.Pitch < MinPitch || kv.Value.Pitch > MaxPitch)
                    report.Error($"event '{kv.Key}': pitch {kv.Value.Pitch} outside {MinPitch}-{MaxPitch} (a small change only)");
            }

            var lib = AssetDatabase.LoadAssetAtPath<SfxLibrary>(AudioTools.LibraryPath);
            if (lib == null) report.Error($"{AudioTools.LibraryPath} missing (Audio > Importa suoni)");
            foreach (var file in AudioCatalog.Files())
            {
                string path = AudioTools.SfxPath(file);
                if (!File.Exists(path))
                {
                    report.Error($"{path} missing (python Tools/Audio/make_sfx.py)");
                    continue;
                }
                var wav = WavInfo.Read(path, out var error);
                if (wav == null)
                {
                    report.Error($"{path}: {error}");
                    continue;
                }
                float max = file.StartsWith("jingle_", StringComparison.Ordinal) ? MaxJingle : MaxEffect;
                if (wav.Samples == 0 || wav.Peak < MinPeak) report.Error($"{path}: empty or silent (peak {wav.Peak:0.000})");
                if (wav.Peak > MaxPeak || wav.FullScaleSamples > 0)
                    report.Error($"{path}: saturated (peak {wav.Peak:0.000}, {wav.FullScaleSamples} sample(s) at full scale)");
                if (Math.Abs(wav.First) > MaxEdge || Math.Abs(wav.Last) > MaxEdge)
                    report.Error($"{path}: click at the start or the end (first {wav.First:0.000}, last {wav.Last:0.000})");
                if (wav.Seconds > max) report.Error($"{path}: {wav.Seconds:0.00} s (max {max} s)");

                var clip = lib != null ? lib.entries.FirstOrDefault(e => e.name == file).clip : null;
                if (lib != null && clip == null) report.Error($"SfxLibrary: no clip for '{file}' (Audio > Importa suoni)");
                else if (clip != null && AssetDatabase.GetAssetPath(clip) != path)
                    report.Error($"SfxLibrary: '{file}' is {AssetDatabase.GetAssetPath(clip)}, expected {path}");
            }
            if (lib != null)
                foreach (var e in lib.entries)
                    if (!AudioCatalog.Files().Contains(e.name)) report.Error($"SfxLibrary: '{e.name}' is not used by AudioCatalog.Sounds");
        }
    }

    /// <summary>
    /// ErNa-015: the four pieces of music (Tools/Audio/make_music_loops.py) exist in Assets/Resources/Audio/Music, the audio manager
    /// finds each by its name (Resources), every screen (Home and each game) has its own, and they are imported for the phone:
    /// streamed, Vorbis, stereo, not preloaded.
    /// </summary>
    public sealed class MusicCheck : IValidationCheck
    {
        public string Name => "Music";

        const float MinSeconds = 30f;

        public void Run(ValidationReport report)
        {
            // every screen has its music: the Home ("") and each game (its Story and its Challenge alike)
            var screens = new List<string> { "" };
            screens.AddRange(GameRegistry.Discover().Games.Select(g => g.Id));
            var used = new HashSet<string>();
            foreach (var id in screens)
            {
                string key = AudioCatalog.MusicFor(id);
                string who = id == "" ? "Home" : $"game '{id}'";
                if (!AudioCatalog.MusicKeys.Contains(key)) report.Error($"{who}: music '{key}' is not in AudioCatalog.MusicKeys");
                else if (id != "" && key == AudioCatalog.MusicHome) report.Error($"{who}: no music of its own (it plays the Home one)");
                else if (!used.Add(key)) report.Error($"{who}: music '{key}' is already the music of another screen");
            }
            foreach (var key in AudioCatalog.MusicKeys)
                if (!used.Contains(key)) report.Error($"music '{key}': no screen plays it");

            foreach (var key in AudioCatalog.MusicKeys)
            {
                string path = AudioTools.MusicPath(key);
                if (!File.Exists(path))
                {
                    report.Error($"{path} missing (python Tools/Audio/make_music_loops.py, then Audio > Importa suoni)");
                    continue;
                }
                var clip = Resources.Load<AudioClip>(AudioCatalog.MusicResources + key);
                if (clip == null) report.Error($"music '{key}': the audio manager does not find it (Resources/{AudioCatalog.MusicResources}{key})");
                else
                {
                    if (AssetDatabase.GetAssetPath(clip) != path)
                        report.Error($"music '{key}': Resources gives {AssetDatabase.GetAssetPath(clip)}, expected {path} (one file per name)");
                    if (clip.length < MinSeconds) report.Error($"{path}: {clip.length:0.0} s (a loop is at least {MinSeconds} s)");
                    if (clip.channels != 2) report.Error($"{path}: {clip.channels} channel(s), expected stereo");
                }
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                {
                    report.Error($"{path}: not imported as audio");
                    continue;
                }
                var s = importer.defaultSampleSettings;
                if (s.loadType != AudioClipLoadType.Streaming) report.Error($"{path}: Load Type {s.loadType}, expected Streaming");
                if (s.compressionFormat != AudioCompressionFormat.Vorbis)
                    report.Error($"{path}: compression {s.compressionFormat}, expected Vorbis");
                if (s.preloadAudioData) report.Error($"{path}: Preload Audio Data on, expected off");
                if (importer.forceToMono) report.Error($"{path}: Force To Mono on, expected off");
                if (Math.Abs(s.quality - AudioImportRules.MusicQuality) > 0.001f)
                    report.Error($"{path}: Vorbis quality {s.quality:0.00}, expected {AudioImportRules.MusicQuality:0.00}");
            }
        }
    }
}
