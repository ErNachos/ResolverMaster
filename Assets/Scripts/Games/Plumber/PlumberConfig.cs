using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI.Themes;
using UnityEngine;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// The single parameter file of the Plumber: water colors and sink halos, pipe art and fountain sizes, water speed and animation
    /// times, generator limits, the Story chapters (name, icon, color, theme) and the Story difficulty table.
    /// Asset: Assets/Resources/Games/Plumber/PlumberConfig.asset (created by Resolver Master > Setup > Ensure Config Assets).
    /// Sizes of the pipe art are fractions (or percentages) of a cell, so the look is the same on every grid.
    /// The terrain of the cells comes from the chapter theme (Core ThemeConfig).
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Plumber Config", fileName = "PlumberConfig")]
    public sealed class PlumberConfig : ScriptableObject
    {
        public const string ResourcesPath = "Games/Plumber/PlumberConfig";

        [Header("Colors")]
        /// <summary>Multiplies the theme slab of a locked cell (ErNa-006: the lock plate is drawn on it).</summary>
        public Color lockedTileColor = new Color32(0xC8, 0xC2, 0xB8, 0xFF);
        /// <summary>Every pipe is this grey (the black outline is drawn in the sprite); the water runs inside it.</summary>
        public Color pipeEmpty = new Color32(0xC2, 0xC6, 0xCF, 0xFF);
        /// <summary>Validator: the minimum of the last Story level is at most the highest minimum of chapter 8 times this (not frustrating).</summary>
        public float finalLevelMaxRatio = 1.3f;
        /// <summary>
        /// Water color of each source/sink pair (index = Piece.Color), in order of appearance (ErNa-006): blue, orange, violet, yellow.
        /// Violet and yellow take their hue from the Okabe-Ito palette (reddish purple made darker, yellow): every pair differs in
        /// lightness by at least <see cref="minWaterLightnessGap"/>, never red together with green. Colors only, no symbols.
        /// </summary>
        public List<Color> waterColors = new List<Color>
        {
            new Color32(0x2F, 0x8B, 0xEF, 0xFF), new Color32(0xFF, 0x8A, 0x1F, 0xFF), new Color32(0x8E, 0x3A, 0x80, 0xFF), new Color32(0xF0, 0xE4, 0x42, 0xFF),
        };
        /// <summary>Halo drawn under each sink, per water color (same index as <see cref="waterColors"/>): light blue, orange, violet, yellow.</summary>
        public List<Color> sinkHaloColors = new List<Color>
        {
            new Color32(0x9C, 0xC8, 0xF7, 0xFF), new Color32(0xFF, 0xC9, 0x96, 0xFF), new Color32(0xD9, 0xB0, 0xD2, 0xFF), new Color32(0xF8, 0xF2, 0xA8, 0xFF),
        };
        /// <summary>Validator: smallest difference of lightness (CIE L*) between two water colors (they stay apart also without hue).</summary>
        public float minWaterLightnessGap = 10f;
        /// <summary>Validator: smallest color difference (CIE76 delta E) between a water color and any UI, theme or chapter color.</summary>
        public float minUiWaterDeltaE = 20f;
        /// <summary>ErNa-006: plate under a locked piece (dark, visible on every terrain) and its 4 bolts.</summary>
        public Color lockPlateColor = new Color32(0x3E, 0x42, 0x4C, 0xFF);
        public Color lockBoltColor = new Color32(0xD3, 0xD6, 0xDC, 0xFF);
        /// <summary>ErNa-007: glowing outline of a piece fixed by the light bulb (different from the bolts of a locked piece).</summary>
        public Color hintGlowColor = new Color32(0xFF, 0xFF, 0xF2, 0xFF);
        /// <summary>Failure highlight (mixed colors, wrong or dry fountain).</summary>
        public Color errorColor = new Color32(0xE5, 0x48, 0x4D, 0xFF);

        [Header("Pipe art (fractions of a cell)")]
        /// <summary>Pipe thickness, in percent of the cell side.</summary>
        [Range(10f, 50f)] public float pipeWidthPercent = 34f;
        /// <summary>Black outline of the pipes (ErNa-005: half of the ErNa-004 one).</summary>
        [Range(0f, 0.2f)] public float pipeOutline = 0.022f;
        /// <summary>
        /// Symmetric shading across the pipe (lighter in the middle, darker on both edges: correct in every position, also while
        /// rotating). 0 = off (flat color, the ErNa-005 choice); e.g. 0.25 = edges 25% darker.
        /// </summary>
        [Range(0f, 0.6f)] public float pipeShading = 0f;
        /// <summary>The water inside a pipe: fraction of the inner width of the pipe.</summary>
        [Range(0.3f, 1f)] public float waterWidth = 0.78f;
        [Range(0f, 0.2f)] public float tileMargin = 0.035f;
        /// <summary>ErNa-006: how dark the shadow of the upper pipe of a bridge is on the lower one.</summary>
        [Range(0f, 1f)] public float bridgeShadowAlpha = 0.45f;
        /// <summary>Side of a source or fountain drawing (it never rotates; the pipe stub under it shows the opening).</summary>
        [Range(0.3f, 1f)] public float endpointSize = 0.78f;
        /// <summary>The drawing is moved up by this: its base sits on the pipe, so the stub enters from under the object.</summary>
        [Range(0f, 0.3f)] public float endpointLift = 0.08f;
        /// <summary>Soft shadow under every source and fountain: width and height (fractions of the cell), how dark, and how far below the centre.</summary>
        [Range(0f, 1f)] public float shadowWidth = 0.72f;
        [Range(0f, 0.6f)] public float shadowHeight = 0.24f;
        [Range(0f, 1f)] public float shadowAlpha = 0.42f;
        [Range(-0.5f, 0.5f)] public float shadowOffset = -0.27f;
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
        /// <summary>Texture side of every generated piece sprite (px): at least the biggest cell (about 330 px on a 3x4 grid), never scaled up.</summary>
        public int spriteSize = 384;

        [Header("Fountain drawings (Assets/Art/Plumber, made by Tools/Art/make_plumber_art.py)")]
        /// <summary>Drawings per kind: fonte_1..N (+ fonte_N_mask), arrivo_1..N. The level seed picks one for each piece.</summary>
        public int artVariants = 4;

        [Header("Board")]
        /// <summary>Space between the edge of the cream board window and the cells (canvas units): inside its inner frame line.</summary>
        public float boardInset = 30;
        /// <summary>
        /// Big grids (ErNa-006, 7x9): when the cells would be smaller than this (canvas units), the window frame is drawn thinner
        /// (<see cref="compactFrameScale"/> of its size, inset scaled the same) and may use <see cref="compactOverflow"/> of the screen
        /// margin around the puzzle area, so the cells stay about 145-150 units wide on a 1080-wide screen.
        /// </summary>
        public float compactCellBelow = 150;
        [Range(0.3f, 1f)] public float compactFrameScale = 0.5f;
        public float compactOverflow = 16;

        [Header("Water (continuous, ErNa-005)")]
        /// <summary>Speed of the water front along the pipes, in cells per second (the same everywhere).</summary>
        public float waterSpeed = 4f;
        /// <summary>A gentle acceleration when the water is opened: seconds to full speed, and the starting speed (fraction).</summary>
        public float waterAccelTime = 0.35f;
        [Range(0.05f, 1f)] public float waterStartSpeed = 0.35f;

        [Header("Animation (seconds)")]
        public float rotateDuration = 0.12f;
        /// <summary>ErNa-006: a tap on a piece that cannot turn (cross, bridge, locked) shakes it: seconds, width (fraction of a cell), swings.</summary>
        public float shakeDuration = 0.28f;
        [Range(0f, 0.3f)] public float shakeAmplitude = 0.06f;
        public int shakeCycles = 3;
        /// <summary>A fountain fills up gradually once the water reaches it.</summary>
        public float fountainFill = 0.45f;
        /// <summary>Water splashing out of the fountains at the end of a successful opening.</summary>
        public float winSpray = 0.8f;
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
            // never close to the water colors (blue, orange, violet, yellow: checked by the validator). Themes: Core ThemeConfig ids.
            new ChapterTheme(PlumberKeys.Chapter1, "water_drop", new Color32(0xF2, 0x8D, 0xB2, 0xFF), ThemeConfig.Garden),
            new ChapterTheme(PlumberKeys.Chapter2, "fork", new Color32(0x2E, 0xB8, 0x8A, 0xFF), ThemeConfig.Farm),
            new ChapterTheme(PlumberKeys.Chapter3, "palette", new Color32(0xB0, 0x7C, 0xF0, 0xFF), ThemeConfig.City),
            // ErNa-006: one novelty per chapter
            new ChapterTheme(PlumberKeys.Chapter4, "factory", new Color32(0xB0, 0x70, 0x5E, 0xFF), ThemeConfig.Factory),
            new ChapterTheme(PlumberKeys.Chapter5, "construction", new Color32(0xB8, 0x94, 0x66, 0xFF), ThemeConfig.Site),
            new ChapterTheme(PlumberKeys.Chapter6, "anchor", new Color32(0x5E, 0x9A, 0x90, 0xFF), ThemeConfig.Harbor),
            new ChapterTheme(PlumberKeys.Chapter7, "landscape", new Color32(0x7F, 0xA3, 0x7A, 0xFF), ThemeConfig.Mountain),
            new ChapterTheme(PlumberKeys.Chapter8, "science", new Color32(0xA3, 0xB0, 0xBD, 0xFF), ThemeConfig.Lab),
            new ChapterTheme(PlumberKeys.Chapter9, "castle", new Color32(0xA5, 0x93, 0xC0, 0xFF), ThemeConfig.Castle),
        };

        [Header("Story difficulty (one tier per level, numbered across chapters)")]
        public DifficultyTable storyDifficulty = PlumberDifficulty.Story();

        [Header("Challenge (ErNa-007): steps by level of the run, margins, generator attempts per seed")]
        public DifficultyTable challengeDifficulty = PlumberDifficulty.Challenge();
        /// <summary>Margins, seed retries and (ErNa-008) the 5 steps of the daily challenge, from a medium step to a hard one.</summary>
        public ResolverMaster.Core.Challenge.ChallengeSettings challenge = new ResolverMaster.Core.Challenge.ChallengeSettings
        {
            dailyTiers = { "ch-6", "ch-9", "ch-12", "ch-13", "ch-15" },
        };
        /// <summary>Generator attempts for one Challenge seed (then the next fixed seed is tried): a number, never a time.</summary>
        public int challengeMaxAttempts = 4000;

        public float PipeWidth => pipeWidthPercent / 100f;

        public WaterMotion Motion => new WaterMotion(waterSpeed, waterAccelTime, waterStartSpeed);

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
