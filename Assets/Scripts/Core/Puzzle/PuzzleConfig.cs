using UnityEngine;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>
    /// The single parameter file of the Core puzzle rules shared by every minigame (today: star thresholds).
    /// Asset: Assets/Resources/ResolverMaster/PuzzleConfig.asset (created by Resolver Master > Setup > Ensure Config Assets).
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Puzzle Config", fileName = "PuzzleConfig")]
    public sealed class PuzzleConfig : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/PuzzleConfig";

        public StarRules stars = new StarRules();

        /// <summary>The asset, or an in-memory copy with the default values (warning in the log) if it is missing.</summary>
        public static PuzzleConfig LoadOrDefault()
        {
            var cfg = Resources.Load<PuzzleConfig>(ResourcesPath);
            if (cfg != null) return cfg;
            Debug.LogWarning("PuzzleConfig missing in Resources/" + ResourcesPath + ": default values used.");
            return CreateInstance<PuzzleConfig>();
        }
    }
}
