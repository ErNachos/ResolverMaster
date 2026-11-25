using System.Collections.Generic;
using ResolverMaster.Core.Save;
using UnityEngine;

namespace ResolverMaster.Core.Audio
{
    /// <summary>
    /// ErNa-010: the sound of the app: two channels. Music: one piece at a time, looping, a fade of about 1 s when it changes, paused
    /// while the app is in the background. Effects: several at once (a pool of sources), the same one limited
    /// (<see cref="VoiceLimiter"/>). Volumes and vibration read the saved settings every time (changes apply at once). Effects: the
    /// sound of an event (<see cref="AudioCatalog.Sounds"/>, file and pitch) from the <see cref="SfxLibrary"/> (ErNa-012); music from
    /// Resources the first time. A missing clip is silence.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour, IAudioPlayer
    {
        SettingsBlock _settings;
        AudioConfig _cfg;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> _effects = new List<AudioSource>();
        AudioSource _musicA, _musicB;
        string _musicKey;
        float _fade = -1f;
        VoiceLimiter _limiter;
        SfxLibrary _sfx;

        public static AudioManager Create(Transform parent, SettingsBlock settings, AudioConfig cfg)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            // ErNa-016: Unity mixes nothing without an AudioListener (the Main scene never had one: a phone build played the effects
            // by chance, and went silent once the streamed music started). The listener lives with the sound, exactly one.
            if (Object.FindAnyObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
            // ErNa-016: the sound never starts paused or at volume 0 (only an ad pauses it, AdOverlay gives it back)
            AudioListener.pause = false;
            if (AudioListener.volume <= 0f) AudioListener.volume = 1f;
            var m = go.AddComponent<AudioManager>();
            m._settings = settings;
            m._cfg = cfg != null ? cfg : ScriptableObject.CreateInstance<AudioConfig>();
            m._limiter = new VoiceLimiter(m._cfg.maxSameEffect, m._cfg.sameEffectWindow);
            m._sfx = Resources.Load<SfxLibrary>(SfxLibrary.ResourcesPath);
            m._musicA = m.Source(true);
            m._musicB = m.Source(true);
            for (int i = 0; i < Mathf.Max(1, m._cfg.effectVoices); i++) m._effects.Add(m.Source(false));
            GameAudio.Player = m;
            return m;
        }

        AudioSource Source(bool music)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = music;
            s.spatialBlend = 0f;
            return s;
        }

        AudioClip Clip(string path)
        {
            if (_clips.TryGetValue(path, out var c)) return c;
            try
            {
                c = Resources.Load<AudioClip>(path);
            }
            catch (System.Exception e)
            {
                // ErNa-016: a piece that cannot be loaded is silence; the effects and the other pieces go on
                Debug.LogError($"AudioManager: music '{path}' not loaded: {e.Message}");
                c = null;
            }
            _clips[path] = c;   // a missing file is remembered as null: silence, no error
            return c;
        }

        public void Play(string evt, float pitch = 1f)
        {
            float volume = AudioVolumes.Effects(_settings, _cfg);
            if (volume <= 0f || _sfx == null || !AudioCatalog.TryResolve(evt, pitch, out var file, out float finalPitch)) return;
            var clip = _sfx.Get(file);
            if (clip == null || !_limiter.TryStart(evt, Time.unscaledTime)) return;
            AudioSource free = null;
            foreach (var s in _effects)
                if (!s.isPlaying) { free = s; break; }
            if (free == null) return;   // every voice busy: this sound is skipped
            free.pitch = finalPitch;
            free.PlayOneShot(clip, volume);
        }

        public void PlayMusic(string key)
        {
            if (key == _musicKey) return;
            _musicKey = key;
            var clip = string.IsNullOrEmpty(key) ? null : Clip(AudioCatalog.MusicResources + key);
            // the new piece fades in on B while A fades out, then they swap
            (_musicA, _musicB) = (_musicB, _musicA);
            _musicA.volume = 0f;
            try
            {
                _musicA.clip = clip;
                if (clip != null) _musicA.Play();
                else _musicA.Stop();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AudioManager: music '{key}' not played: {e.Message}");
                _musicA.clip = null;
            }
            _fade = 0f;
        }

        public void Vibrate()
        {
            if (!AudioVolumes.Vibration(_settings)) return;
#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor) Handheld.Vibrate();
#endif
        }

        void Update()
        {
            float target = AudioVolumes.Music(_settings, _cfg);
            if (_fade >= 0f)
            {
                _fade += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(_fade / Mathf.Max(0.01f, _cfg.musicFade));
                _musicA.volume = target * u;
                _musicB.volume = _musicB.isPlaying ? target * (1f - u) : 0f;
                if (u >= 1f)
                {
                    _fade = -1f;
                    _musicB.Stop();
                }
            }
            else _musicA.volume = target;   // the settings switch the music on and off at once
        }

        /// <summary>The music stops while the app is in the background and goes on when it comes back.</summary>
        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _musicA.Pause();
                _musicB.Pause();
            }
            else
            {
                _musicA.UnPause();
                if (_fade >= 0f) _musicB.UnPause();
            }
        }

        void OnDestroy()
        {
            if (ReferenceEquals(GameAudio.Player, this)) GameAudio.Player = null;
        }
    }
}
