using UnityEngine;

namespace ResolverMaster.Core.Audio
{
    /// <summary>The single parameter file of the sound (ErNa-010): Assets/Resources/ResolverMaster/AudioConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Resolver Master/Audio Config", fileName = "AudioConfig")]
    public sealed class AudioConfig : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/AudioConfig";

        /// <summary>
        /// ErNa-015: music about 11 dB under the effects (taps, wins and errors stay clear): the loops are at -18 LUFS, about -24 dB
        /// through a phone speaker, the effects at -19 dB (Tools/Audio/make_music_loops.py prints the value for its loops).
        /// </summary>
        public const float DefaultMusicVolume = 0.5f;

        [Range(0f, 1f)] public float musicVolume = DefaultMusicVolume;
        [Range(0f, 1f)] public float effectsVolume = 1f;
        /// <summary>Fade between two pieces of music (seconds).</summary>
        public float musicFade = 1f;
        /// <summary>Effects playing together at most, and the same effect at most this many times within the window (seconds).</summary>
        public int effectVoices = 10;
        public int maxSameEffect = 3;
        public float sameEffectWindow = 0.12f;

        public static AudioConfig LoadOrDefault()
        {
            var cfg = Resources.Load<AudioConfig>(ResourcesPath);
            return cfg != null ? cfg : CreateInstance<AudioConfig>();
        }
    }
}
