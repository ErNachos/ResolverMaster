using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;
using UnityEngine;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// The single parameter file of the Plumber: water colors and sink halos, pipe art and fountain sizes, animation times, generator
    /// limits, the Story chapters (name and theme) and the Story difficulty table.
    /// Asset: Assets/Resources/Games/Plumber/PlumberConfig.asset (created by Resolver Master > Setup > Ensure Config Assets).
    /// Sizes of the pipe art are fractions of a cell, so the look is the same on 3x3 and 6x6 boards.
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Plumber Config", fileName = "PlumberConfig")]
    public sealed class PlumberConfig : ScriptableObject
    {
        public const string ResourcesPath = "Games/Plumber/PlumberConfig";

        [Header("Colors")]
        /// <summary>Board frame (cream panel with the black outline of the UI kit).</summary>
        public Color boardColor = new Color32(0xFF, 0xF6, 0xE3, 0xFF);
        public Color tileColor = new Color32(0xF1, 0xE4, 0xC8, 0xFF);
        public Color lockedTileColor = new Color32(0xD9, 0xCB, 0xAE, 0xFF);
        /// <summary>Every pipe is this grey until the water is opened (the black outline is drawn in the sprite).</summary>
        public Color pipeEmpty = new Color32(0xC2, 0xC6, 0xCF, 0xFF);
        /// <summary>Water color of each source/sink pair (index = Piece.Color): blue, orange.</summary>
        public List<Color> waterColors = new List<Color> { new Color32(0x2F, 0x8B, 0xEF, 0xFF), new Color32(0xFF, 0x8A, 0x1F, 0xFF) };
        /// <summary>Halo drawn under each sink, per water color (same index as <see cref="waterColors"/>): a light blue, a light orange.</summary>
        public List<Color> sinkHaloColors = new List<Color> { new Color32(0x9C, 0xC8, 0xF7, 0xFF), new Color32(0xFF, 0xC9, 0x96, 0xFF) };
        /// <summary>Failure highlight (mixed colors, wrong or dry fountain).</summary>
        public Color errorColor = new Color32(0xE5, 0x48, 0x4D, 0xFF);

        [Header("Pipe art (fractions of a cell)")]
        [Range(0.1f, 0.5f)] public float pipeWidth = 0.34f;
        /// <summary>Black outline of the pipes (cartoon style).</summary>
        [Range(0f, 0.2f)] public float pipeOutline = 0.045f;
        /// <summary>Light reflection along the pipes (untinted white overlay).</summary>
        [Range(0f, 1f)] public float pipeShine = 0.55f;
        [Range(0f, 0.2f)] public float tileMargin = 0.035f;
        [Range(0f, 0.5f)] public float tileRadius = 0.14f;
        /// <summary>Side of a source or fountain drawing (it never rotates; the pipe stub under it shows the opening).</summary>
        [Range(0.3f, 1f)] public float endpointSize = 0.80f;
        /// <summary>Diameter of the colored halo under a sink.</summary>
        [Range(0.3f, 1f)] public float sinkHaloSize = 0.92f;
        /// <summary>Side of one splash drop of a leak.</summary>
        [Range(0.05f, 0.4f)] public float splashDropSize = 0.16f;
        /// <summary>How far the splash drops fly out of the leaking opening.</summary>
        [Range(0.1f, 1.5f)] public float splashDistance = 0.55f;
        /// <summary>Win effect above every fountain: drops, how high they jump, their size.</summary>
        public int winDrops = 7;
        [Range(0.1f, 1.5f)] public float winSplashHeight = 0.6f;
        [Range(0.05f, 0.4f)] public float winDropSize = 0.22f;
        /// <summary>Texture side of every generated piece sprite (px).</summary>
        public int spriteSize = 256;

        [Header("Fountain drawings (Assets/Art/Plumber, made by Tools/Art/make_plumber_art.py)")]
        /// <summary>Drawings per kind: fonte_1..N (+ fonte_N_mask), arrivo_1..N. The level seed picks one for each piece.</summary>
        public int artVariants = 4;

        [Header("Board")]
        /// <summary>Space between the board card edge and the cells (canvas units).</summary>
        public float boardInset = 16;

        [Header("Animation (seconds)")]
        public float rotateDuration = 0.12f;
        /// <summary>Delay between one cell filling and the next.</summary>
        public float waterStep = 0.07f;
        /// <summary>Water splashing out of the fountains at the end of a successful opening.</summary>
        public float sinkFill = 0.8f;
        /// <summary>Running water shimmers: strength (mix towards white), speed (rad/s) and phase step between cells.</summary>
        [Range(0f, 0.5f)] public float waterShimmer = 0.14f;
        public float waterShimmerSpeed = 7f;
        public float waterShimmerPhase = 0.9f;
        public float winPause = 0.25f;
        /// <summary>Failure effect (splash, highlight, blinking fountain); the message appears when it starts.</summary>
        public float failEffect = 1.0f;
        /// <summary>Pause after the failure effect, then the level restarts.</summary>
        public float failPause = 0.45f;
        /// <summary>Blinks of a highlighted cell during the failure effect.</summary>
        public int failBlinks = 3;

        [Header("Generator")]
        public int maxAttempts = 20000;
        /// <summary>Story level n is generated from seed storySeedBase + n.</summary>
        public int storySeedBase = 2611000;

        [Header("Story chapters (9 levels each, in order)")]
        public List<ChapterTheme> chapters = new List<ChapterTheme>
        {
            // not blue or orange: those are the water colors
            new ChapterTheme(PlumberKeys.Chapter1, "water_drop", new Color32(0xFF, 0xCD, 0x3C, 0xFF)),
            new ChapterTheme(PlumberKeys.Chapter2, "fork", new Color32(0x2E, 0xB8, 0x8A, 0xFF)),
            new ChapterTheme(PlumberKeys.Chapter3, "palette", new Color32(0xB0, 0x7C, 0xF0, 0xFF)),
        };

        [Header("Story difficulty (one tier per level, numbered across chapters)")]
        public DifficultyTable storyDifficulty = PlumberDifficulty.Story();

        public Color WaterColor(int color) =>
            waterColors.Count == 0 ? Color.blue : waterColors[Mathf.Clamp(color, 0, waterColors.Count - 1)];

        public Color SinkHaloColor(int color) =>
            sinkHaloColors.Count == 0 ? WaterColor(color) : sinkHaloColors[Mathf.Clamp(color, 0, sinkHaloColors.Count - 1)];

        public static PlumberConfig LoadOrDefault()
        {
            var cfg = Resources.Load<PlumberConfig>(ResourcesPath);
            if (cfg != null) return cfg;
            Debug.LogWarning("PlumberConfig missing in Resources/" + ResourcesPath + ": default values used.");
            return CreateInstance<PlumberConfig>();
        }
    }
}
