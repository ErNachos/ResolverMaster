using System.Linq;
using NUnit.Framework;
using ResolverMaster.Core.Audio;
using ResolverMaster.Core.Save;
using ResolverMaster.Core.UI;
using UnityEngine;

namespace ResolverMaster.Tests
{
    /// <summary>ErNa-010: the Home background and arrows, the audio table, music and volumes; ErNa-012: the table of the generated sounds.</summary>
    public sealed class HomeAndAudioTests
    {
        [Test]
        public void HomeBackground_FollowsTheCard_BlendsWhileScrolling_ArrowsOffAtTheEnds()
        {
            var colors = new[] { new Color(0.9f, 0.86f, 0.96f), new Color(0.96f, 0.93f, 0.82f), new Color(0.87f, 0.93f, 0.96f) };
            Assert.AreEqual(colors[0], BackgroundColors.At(colors, 0f));
            Assert.AreEqual(colors[2], BackgroundColors.At(colors, 2f));
            Assert.AreEqual(colors[2], BackgroundColors.At(colors, 3.5f), "past the last card: its color");
            var half = BackgroundColors.At(colors, 0.5f);
            Assert.AreEqual(Color.Lerp(colors[0], colors[1], 0.5f), half, "half way: the mix of the two cards");
            // gradient and pattern stay close to the base: lighter on top, darker at the bottom, the pattern just darker
            var c = colors[1];
            Assert.Greater(BackgroundColors.Top(c).grayscale, c.grayscale);
            Assert.Less(BackgroundColors.Bottom(c).grayscale, c.grayscale);
            Assert.Less(BackgroundColors.Pattern(c).grayscale, c.grayscale);
            Assert.Greater(BackgroundColors.Pattern(c).grayscale, c.grayscale * 0.85f, "only a little darker");

            Assert.AreEqual((false, true), CarouselArrows.Shown(0, 3), "first card: no left arrow");
            Assert.AreEqual((true, true), CarouselArrows.Shown(1, 3));
            Assert.AreEqual((true, false), CarouselArrows.Shown(2, 3), "last card: no right arrow");
            Assert.AreEqual((false, false), CarouselArrows.Shown(0, 1), "one card: no arrow");
        }

        [Test]
        public void Audio_EveryEventHasAFileOrIsSilentWithoutErrors_MusicOfEachScreen_VolumesFromTheSettings()
        {
            // an unknown event is silence, never an error
            Assert.IsFalse(AudioCatalog.TryResolve("no_such_event", 1f, out _, out _));
            GameAudio.Player = null;
            Assert.DoesNotThrow(() => GameAudio.Play(AudioEvents.UiClick), "no player (tests, previews): nothing happens");

            // music: Home, then each game in its Story and its Challenge
            Assert.AreEqual("Music_Home", AudioCatalog.MusicFor(""));
            Assert.AreEqual("Music_Plumber", AudioCatalog.MusicFor("plumber"));
            Assert.AreEqual("Music_TrafficJam", AudioCatalog.MusicFor("trafficjam"));
            Assert.AreEqual("Music_Penguin", AudioCatalog.MusicFor("penguin"));
            Assert.AreEqual(AudioEvents.PenguinWin, AudioCatalog.WinFor("penguin"));

            // volumes from the saved settings: music lower than the effects, each switched off on its own
            var cfg = ScriptableObject.CreateInstance<AudioConfig>();
            var s = new SettingsBlock();
            Assert.AreEqual(0.6f, AudioVolumes.Music(s, cfg), 1e-4);
            Assert.AreEqual(1f, AudioVolumes.Effects(s, cfg), 1e-4);
            s.music = false;
            Assert.AreEqual(0f, AudioVolumes.Music(s, cfg));
            Assert.AreEqual(1f, AudioVolumes.Effects(s, cfg), 1e-4, "the effects stay on");
            s.sound = false;
            Assert.AreEqual(0f, AudioVolumes.Effects(s, cfg));
            Assert.IsTrue(AudioVolumes.Vibration(s));
            var old = SaveSerializer.FromJson("{\"version\":1,\"settings\":{\"language\":\"it\",\"sound\":false,\"vibration\":true}}");
            Assert.IsTrue(old.settings.music, "older files: the music is on");
            Object.DestroyImmediate(cfg);

            // the same sound never piles up
            var limiter = new VoiceLimiter(2, 0.1f);
            Assert.IsTrue(limiter.TryStart("a", 0f));
            Assert.IsTrue(limiter.TryStart("a", 0.01f));
            Assert.IsFalse(limiter.TryStart("a", 0.02f), "a third time at once: skipped");
            Assert.IsTrue(limiter.TryStart("b", 0.02f), "another sound plays");
            Assert.IsTrue(limiter.TryStart("a", 0.5f), "later again");
        }

        [Test]
        public void SoundTable_EveryEventHasAGeneratedSound_SharedFilesChangePitchALittle()
        {
            // ErNa-012: every event declared in AudioEvents has its sound, and the table names no other event
            var events = typeof(AudioEvents).GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()).ToList();
            CollectionAssert.AreEquivalent(events, AudioCatalog.Events.ToList());

            foreach (var kv in AudioCatalog.Sounds)
            {
                Assert.IsFalse(string.IsNullOrEmpty(kv.Value.File), kv.Key);
                Assert.That(kv.Value.Pitch, Is.InRange(0.8f, 1.25f), kv.Key + ": only a small change of pitch");
            }
            // events sharing a file sound different: never the same file at the same pitch
            foreach (var group in AudioCatalog.Sounds.GroupBy(kv => kv.Value.File))
                Assert.AreEqual(group.Count(), group.Select(kv => kv.Value.Pitch).Distinct().Count(), group.Key + " is shared at the same pitch");
            Assert.Less(AudioCatalog.Files().Count(), AudioCatalog.Sounds.Count, "similar events share their sound");
            Assert.AreEqual(AudioCatalog.Files().Count(), AudioCatalog.Files().Distinct().Count());

            // the caller's pitch (the rising stars) multiplies the pitch of the table
            Assert.IsTrue(AudioCatalog.TryResolve(AudioEvents.Star, 1.24f, out var file, out float pitch));
            Assert.AreEqual("star", file);
            Assert.AreEqual(1.24f, pitch, 1e-5);
            Assert.IsTrue(AudioCatalog.TryResolve(AudioEvents.PenguinPush, 1f, out file, out pitch));
            Assert.AreEqual(AudioCatalog.Sounds[AudioEvents.PenguinSlide].File, file, "the block sliding uses the slide on ice, lower");
            Assert.Less(pitch, 1f);
            Assert.IsFalse(AudioCatalog.TryResolve("", 1f, out _, out _));
            Assert.IsFalse(AudioCatalog.TryResolve(null, 1f, out _, out _));
        }
    }
}
