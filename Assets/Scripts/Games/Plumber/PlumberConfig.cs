using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;
using UnityEngine;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// The single parameter file of the Plumber: colors, pipe thickness, animation times, generator limits and the Story difficulty table.
    /// Asset: Assets/Resources/Games/Plumber/PlumberConfig.asset (created by Resolver Master > Setup > Ensure Config Assets).
    /// Sizes of the pipe art are fractions of a cell, so the look is the same on 3x3 and 5x5 boards.
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Plumber Config", fileName = "PlumberConfig")]
    public sealed class PlumberConfig : ScriptableObject
    {
        public const string ResourcesPath = "Games/Plumber/PlumberConfig";

        [Header("Colors")]
        public Color boardColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public Color tileColor = new Color32(0xE6, 0xEB, 0xF3, 0xFF);
        public Color lockedTileColor = new Color32(0xC9, 0xD1, 0xE0, 0xFF);
        public Color pipeEmpty = new Color32(0xA2, 0xAC, 0xBE, 0xFF);
        /// <summary>Water color of each source/sink pair (index = Piece.Color). One color for now.</summary>
        public List<Color> waterColors = new List<Color> { new Color32(0x2F, 0x8B, 0xEF, 0xFF) };

        [Header("Pipe art (fractions of a cell)")]
        [Range(0.1f, 0.5f)] public float pipeWidth = 0.30f;
        [Range(0f, 0.2f)] public float pipeOutline = 0.05f;
        [Range(0.1f, 0.45f)] public float tankRadius = 0.30f;
        [Range(0.1f, 0.45f)] public float sinkRingRadius = 0.28f;
        [Range(0.02f, 0.2f)] public float sinkRingWidth = 0.11f;
        [Range(0f, 0.2f)] public float tileMargin = 0.035f;
        [Range(0f, 0.5f)] public float tileRadius = 0.14f;
        /// <summary>Texture side of every generated piece sprite (px).</summary>
        public int spriteSize = 256;

        [Header("Board")]
        /// <summary>Space between the board card edge and the cells (canvas units).</summary>
        public float boardInset = 16;

        [Header("Animation (seconds)")]
        public float rotateDuration = 0.12f;
        /// <summary>Delay between one cell filling and the next.</summary>
        public float waterStep = 0.06f;
        public float sinkFill = 0.35f;
        public float winPause = 0.3f;

        [Header("Generator")]
        public int maxAttempts = 20000;
        /// <summary>Story level n is generated from seed storySeedBase + n.</summary>
        public int storySeedBase = 2611000;

        [Header("Story difficulty (chapter 1)")]
        public DifficultyTable story = PlumberDifficulty.StoryChapter1();

        public Color WaterColor(int color) =>
            waterColors.Count == 0 ? Color.blue : waterColors[Mathf.Clamp(color, 0, waterColors.Count - 1)];

        public static PlumberConfig LoadOrDefault()
        {
            var cfg = Resources.Load<PlumberConfig>(ResourcesPath);
            if (cfg != null) return cfg;
            Debug.LogWarning("PlumberConfig missing in Resources/" + ResourcesPath + ": default values used.");
            return CreateInstance<PlumberConfig>();
        }
    }
}
