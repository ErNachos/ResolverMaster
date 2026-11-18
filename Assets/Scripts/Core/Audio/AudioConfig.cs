using UnityEngine;

namespace ResolverMaster.Core.Audio
{
    /// <summary>The single parameter file of the sound (ErNa-010): Assets/Resources/ResolverMaster/AudioConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Resolver Master/Audio Config", fileName = "AudioConfig")]
    public sealed class AudioConfig : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/AudioConfig";

        /// <summary>Music lower than the effects.</summary>
        [Range(0f, 1f)] public float musicVolume = 0.6f;
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
