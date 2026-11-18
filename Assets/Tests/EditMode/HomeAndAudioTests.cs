using NUnit.Framework;
using ResolverMaster.Core.Audio;
using ResolverMaster.Core.Save;
using ResolverMaster.Core.UI;
using UnityEngine;

namespace ResolverMaster.Tests
{
    /// <summary>ErNa-010: the Home background and arrows, the audio table, music and volumes.</summary>
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
            // every event: its file, or silence (a missing file is no error)
            foreach (var evt in AudioCatalog.Events)
            {
                Assert.IsFalse(string.IsNullOrEmpty(evt));
                Assert.DoesNotThrow(() => Resources.Load<AudioClip>(AudioCatalog.SfxResources + evt), evt);
            }
            Assert.IsNull(Resources.Load<AudioClip>(AudioCatalog.SfxResources + "no_such_event"));
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
    }
}
