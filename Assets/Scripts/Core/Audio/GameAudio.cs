using System;
using System.Collections.Generic;
using ResolverMaster.Core.Save;

namespace ResolverMaster.Core.Audio
{
    /// <summary>
    /// ErNa-010: the names of the sound events. Every sound of the app is played by its event name; the file of an event is
    /// Resources/Audio/Sfx/&lt;event&gt; (any audio format Unity reads). A missing file is silence, never an error.
    /// </summary>
    public static class AudioEvents
    {
        // UI
        public const string UiClick = "ui_click";
        public const string UiSwipe = "ui_swipe";
        public const string PanelOpen = "ui_panel_open";
        public const string PanelClose = "ui_panel_close";
        public const string Star = "ui_star";
        public const string ChapterUnlocked = "ui_chapter_unlocked";
        public const string NewRecord = "ui_new_record";
        // Plumber
        public const string PlumberRotate = "plumber_rotate";
        public const string PlumberLocked = "plumber_locked";
        public const string PlumberOpen = "plumber_open";
        public const string PlumberFountain = "plumber_fountain";
        public const string PlumberWin = "plumber_win";
        public const string PlumberFail = "plumber_fail";
        // Traffic jam
        public const string TrafficGrab = "trafficjam_grab";
        public const string TrafficSnap = "trafficjam_snap";
        public const string TrafficBump = "trafficjam_bump";
        public const string TrafficExit = "trafficjam_exit";
        public const string TrafficWin = "trafficjam_win";
        // Pino
        public const string PenguinSlide = "penguin_slide";
        public const string PenguinBump = "penguin_bump";
        public const string PenguinSnow = "penguin_snow";
        public const string PenguinFish = "penguin_fish";
        public const string PenguinIglooOpen = "penguin_igloo_open";
        public const string PenguinEnter = "penguin_enter";
        public const string PenguinSplash = "penguin_splash";
        public const string PenguinPush = "penguin_push";
        public const string PenguinSink = "penguin_sink";
        public const string PenguinWin = "penguin_win";
        // common
        public const string Hint = "common_hint";
        public const string Undo = "common_undo";
        public const string Blocked = "common_blocked";
        public const string ChallengeLost = "common_challenge_lost";
        public const string Continue = "common_continue";
    }

    /// <summary>
    /// ErNa-010: the table event → file. <see cref="KenneyFiles"/>: the file of the Kenney packs (CC0, kenney.nl: Interface Sounds,
    /// Impact Sounds, UI Audio, Music Jingles, saved in ArtSource/Audio/Kenney/) chosen for each event; the Editor tool
    /// (Resolver Master > Audio > Importa suoni) copies it to Resources/Audio/Sfx/&lt;event&gt;. An empty name = no sound (yet).
    /// Music: Resources/Audio/Music/Music_Home, Music_Plumber, Music_TrafficJam, Music_Penguin (from ArtSource/Audio/Music/).
    /// </summary>
    public static class AudioCatalog
    {
        public const string SfxResources = "Audio/Sfx/";
        public const string MusicResources = "Audio/Music/";
        public const string MusicHome = "Music_Home";

        /// <summary>Event → Kenney file name (without folder; soft and cheerful sounds; the jingles for the wins).</summary>
        public static readonly IReadOnlyDictionary<string, string> KenneyFiles = new Dictionary<string, string>
        {
            [AudioEvents.UiClick] = "click_001.ogg",
            [AudioEvents.UiSwipe] = "scroll_001.ogg",
            [AudioEvents.PanelOpen] = "maximize_001.ogg",
            [AudioEvents.PanelClose] = "minimize_001.ogg",
            [AudioEvents.Star] = "confirmation_001.ogg",
            [AudioEvents.ChapterUnlocked] = "jingles_PIZZI00.ogg",
            [AudioEvents.NewRecord] = "jingles_PIZZI01.ogg",
            [AudioEvents.PlumberRotate] = "switch_002.ogg",
            [AudioEvents.PlumberLocked] = "error_004.ogg",
            [AudioEvents.PlumberOpen] = "drop_002.ogg",
            [AudioEvents.PlumberFountain] = "drop_003.ogg",
            [AudioEvents.PlumberWin] = "jingles_STEEL00.ogg",
            [AudioEvents.PlumberFail] = "error_006.ogg",
            [AudioEvents.TrafficGrab] = "select_001.ogg",
            [AudioEvents.TrafficSnap] = "impactPlate_light_000.ogg",
            [AudioEvents.TrafficBump] = "impactSoft_medium_000.ogg",
            [AudioEvents.TrafficExit] = "confirmation_002.ogg",
            [AudioEvents.TrafficWin] = "jingles_NES00.ogg",
            [AudioEvents.PenguinSlide] = "footstep_snow_000.ogg",
            [AudioEvents.PenguinBump] = "impactSoft_heavy_000.ogg",
            [AudioEvents.PenguinSnow] = "footstep_snow_002.ogg",
            [AudioEvents.PenguinFish] = "pluck_001.ogg",
            [AudioEvents.PenguinIglooOpen] = "open_001.ogg",
            [AudioEvents.PenguinEnter] = "confirmation_003.ogg",
            [AudioEvents.PenguinSplash] = "impactGlass_light_000.ogg",
            [AudioEvents.PenguinPush] = "impactWood_light_000.ogg",
            [AudioEvents.PenguinSink] = "drop_004.ogg",
            [AudioEvents.PenguinWin] = "jingles_SAX00.ogg",
            [AudioEvents.Hint] = "glass_001.ogg",
            [AudioEvents.Undo] = "back_001.ogg",
            [AudioEvents.Blocked] = "error_002.ogg",
            [AudioEvents.ChallengeLost] = "jingles_HIT00.ogg",
            [AudioEvents.Continue] = "confirmation_004.ogg",
        };

        /// <summary>Every event of the table.</summary>
        public static IEnumerable<string> Events => KenneyFiles.Keys;

        /// <summary>The win jingle of a game (<c>GameInfo.Id</c>); unknown games: the Plumber one.</summary>
        public static string WinFor(string gameId) =>
            gameId == "trafficjam" ? AudioEvents.TrafficWin : gameId == "penguin" ? AudioEvents.PenguinWin : AudioEvents.PlumberWin;

        /// <summary>
        /// The music of a screen: "" = Home (also the screens of no game), otherwise the game's own (its Story and its Challenge alike).
        /// </summary>
        public static string MusicFor(string gameId)
        {
            switch (gameId)
            {
                case "plumber": return "Music_Plumber";
                case "trafficjam": return "Music_TrafficJam";
                case "penguin": return "Music_Penguin";
                default: return MusicHome;
            }
        }
    }

    /// <summary>ErNa-010: volumes from the saved settings (pure logic).</summary>
    public static class AudioVolumes
    {
        public static float Music(SettingsBlock s, AudioConfig cfg) => s != null && s.music ? Clamp(cfg != null ? cfg.musicVolume : 0.6f) : 0f;

        public static float Effects(SettingsBlock s, AudioConfig cfg) => s != null && s.sound ? Clamp(cfg != null ? cfg.effectsVolume : 1f) : 0f;

        public static bool Vibration(SettingsBlock s) => s != null && s.vibration;

        static float Clamp(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>ErNa-010: the same sound may not start more than <c>max</c> times within <c>window</c> seconds (pure logic).</summary>
    public sealed class VoiceLimiter
    {
        readonly Dictionary<string, List<float>> _starts = new Dictionary<string, List<float>>();
        readonly int _max;
        readonly float _window;

        public VoiceLimiter(int max, float window)
        {
            _max = Math.Max(1, max);
            _window = Math.Max(0f, window);
        }

        public bool TryStart(string evt, float now)
        {
            if (!_starts.TryGetValue(evt, out var list)) _starts[evt] = list = new List<float>();
            list.RemoveAll(t => now - t > _window);
            if (list.Count >= _max) return false;
            list.Add(now);
            return true;
        }
    }

    /// <summary>What plays sounds and music (the Core <c>AudioManager</c>; nothing in tests and previews).</summary>
    public interface IAudioPlayer
    {
        void Play(string evt, float pitch = 1f);
        void PlayMusic(string key);
        void Vibrate();
    }

    /// <summary>
    /// ErNa-010: the one door of the games and screens to the sound: <c>GameAudio.Play(AudioEvents.X)</c>. Without a player (tests,
    /// previews, Editor tools) nothing happens.
    /// </summary>
    public static class GameAudio
    {
        public static IAudioPlayer Player;

        public static void Play(string evt, float pitch = 1f) => Player?.Play(evt, pitch);

        public static void PlayMusic(string key) => Player?.PlayMusic(key);

        /// <summary>A short vibration on important events (win, error, loss), only when it is on in the settings.</summary>
        public static void Vibrate() => Player?.Vibrate();
    }
}
