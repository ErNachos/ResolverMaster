using System.Collections.Generic;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI.Themes;
using UnityEngine;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>
    /// The single parameter file of Traffic jam (TJ-001): colors of the target cars and of the other vehicles, vehicle and barrier art,
    /// board, animation times, solver and generator limits, the Story chapters (name, icon, color, theme), the Story difficulty table and
    /// the Challenge steps and margins. Asset: Assets/Resources/Games/TrafficJam/TrafficJamConfig.asset (Setup > Ensure Config Assets).
    /// Sizes are fractions of a cell, so the look is the same on every grid. The terrain of the cells comes from the chapter theme.
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Traffic Jam Config", fileName = "TrafficJamConfig")]
    public sealed class TrafficJamConfig : ScriptableObject
    {
        public const string ResourcesPath = "Games/TrafficJam/TrafficJamConfig";

        [Header("Colors")]
        /// <summary>
        /// The target cars and their exits: red (index 0) and blue (index 1). Reserved: no other vehicle, UI, theme or chapter color may
        /// be within <see cref="minTargetDeltaE"/> of them (validator).
        /// </summary>
        public List<Color> targetColors = new List<Color> { new Color32(0xE0, 0x2B, 0x20, 0xFF), new Color32(0x2F, 0x7F, 0xE8, 0xFF) };
        /// <summary>The other vehicles, picked from the level seed: yellow, white, beige, grey, light violet (never red, blue or green).</summary>
        public List<Color> vehicleColors = new List<Color>
        {
            new Color32(0xF4, 0xC4, 0x30, 0xFF), new Color32(0xF4, 0xF1, 0xEA, 0xFF), new Color32(0xD9, 0xC2, 0x9A, 0xFF),
            new Color32(0x9A, 0xA0, 0xA8, 0xFF), new Color32(0xB9, 0xA3, 0xE3, 0xFF),
        };
        /// <summary>Validator: smallest color difference (CIE76 delta E) between a target color and any other color of the game, UI, themes, chapters.</summary>
        public float minTargetDeltaE = 20f;
        /// <summary>Barrier stripes (road works): amber and charcoal.</summary>
        public Color barrierColorA = new Color32(0xF5, 0xB8, 0x3D, 0xFF);
        public Color barrierColorB = new Color32(0x33, 0x36, 0x3D, 0xFF);
        /// <summary>Windows and wheels of every vehicle; the stripes of the target cars.</summary>
        public Color glassColor = new Color32(0x3B, 0x4A, 0x5A, 0xFF);
        public Color wheelColor = new Color32(0x22, 0x22, 0x26, 0xFF);
        public Color stripeColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        /// <summary>The arrow of the light bulb.</summary>
        public Color hintArrowColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        [Header("Vehicle art (fractions of a cell)")]
        /// <summary>Empty space between a vehicle and the cell borders.</summary>
        [Range(0f, 0.2f)] public float vehicleInset = 0.07f;
        /// <summary>Corner radius of a vehicle body.</summary>
        [Range(0f, 0.5f)] public float vehicleCorner = 0.22f;
        /// <summary>Black outline of vehicles and barriers.</summary>
        [Range(0f, 0.1f)] public float outline = 0.035f;
        /// <summary>Texture pixels per cell of the generated sprites (at least the biggest cell, about 240 units on a 4x6 grid: never scaled up).</summary>
        public int spritePixelsPerCell = 256;
        /// <summary>The exit gate: how far it reaches outside the cells (the board keeps room for it), and its width.</summary>
        [Range(0.05f, 0.5f)] public float exitDepth = 0.26f;
        [Range(0.3f, 1f)] public float exitWidth = 0.86f;

        [Header("Board")]
        public float boardInset = 30;
        public float compactCellBelow = 150;
        [Range(0.3f, 1f)] public float compactFrameScale = 0.5f;
        public float compactOverflow = 16;

        [Header("Animation (seconds, real time)")]
        /// <summary>A released vehicle slides to the nearest cell.</summary>
        public float snapDuration = 0.09f;
        /// <summary>A target car drives out through its exit and fades.</summary>
        public float exitDuration = 0.38f;
        /// <summary>Pause after the last target car is out, then the win panel.</summary>
        public float winPause = 0.25f;
        /// <summary>The light bulb: the arrow shows, then the vehicle slides.</summary>
        public float hintArrowTime = 0.55f;
        public float hintMoveDuration = 0.28f;
        /// <summary>A drag on a vehicle that cannot move (Challenge, out of moves): the Core shake.</summary>
        public float shakeDuration = 0.28f;
        [Range(0f, 0.3f)] public float shakeAmplitude = 0.06f;
        public int shakeCycles = 3;
        /// <summary>Released past the last cell by more than this (fraction of a cell), a target car with a free way leaves.</summary>
        [Range(0.1f, 1f)] public float exitThreshold = 0.45f;

        [Header("Solver and generator")]
        /// <summary>Most reachable positions of a level (solver, light bulb, validator). Levels with more are discarded by the generator.</summary>
        public int maxStates = TrafficSpace.DefaultMaxStates;
        /// <summary>Story generator (Editor): positions per explored layout (bigger ones are discarded quickly), explorations per level.</summary>
        public int storyMaxStates = 20000;
        public int storyMaxAttempts = 4000;
        /// <summary>Changes tried on one random layout before a new one (hill climbing).</summary>
        public int climbSteps = 60;
        /// <summary>The highest minimum moves of any level.</summary>
        public int maxMinMoves = 40;
        /// <summary>Story level n is generated from seed storySeedBase + n.</summary>
        public int storySeedBase = 3611000;

        [Header("Story chapters (9 levels each, in order)")]
        public List<ChapterTheme> chapters = new List<ChapterTheme>
        {
            // never close to the red and blue of the target cars (checked by the validator). Themes: Core ThemeConfig ids.
            new ChapterTheme(TrafficJamKeys.Chapter1, "parking", new Color32(0x8F, 0xB3, 0x9A, 0xFF), ThemeConfig.Parking),
            new ChapterTheme(TrafficJamKeys.Chapter2, "storefront", new Color32(0xC7, 0x9A, 0x6B, 0xFF), ThemeConfig.Market),
            new ChapterTheme(TrafficJamKeys.Chapter3, "location_city", new Color32(0xB9, 0xC2, 0xCC, 0xFF), ThemeConfig.City),
            new ChapterTheme(TrafficJamKeys.Chapter4, "construction", new Color32(0xB8, 0x94, 0x66, 0xFF), ThemeConfig.Site),
            new ChapterTheme(TrafficJamKeys.Chapter5, "flight", new Color32(0x9D, 0xB0, 0xA3, 0xFF), ThemeConfig.Airport),
            new ChapterTheme(TrafficJamKeys.Chapter6, "traffic", new Color32(0xA8, 0x9C, 0x8A, 0xFF), ThemeConfig.Crossroads),
            new ChapterTheme(TrafficJamKeys.Chapter7, "schedule", new Color32(0x9C, 0x93, 0xAE, 0xFF), ThemeConfig.RushHour),
        };

        [Header("Story difficulty (one tier per level, numbered across chapters)")]
        public DifficultyTable storyDifficulty = TrafficDifficulty.Story();

        [Header("Challenge: steps by level of the run, margins, generator limits per seed")]
        public DifficultyTable challengeDifficulty = TrafficDifficulty.Challenge();
        /// <summary>Margins (minimum + 60% on the first step down to + 10% on the last, at least +1), seed retries, the 5 daily steps.</summary>
        public ChallengeSettings challenge = new ChallengeSettings
        {
            dailyTiers = { "ch-6", "ch-8", "ch-10", "ch-12", "ch-14" },
        };
        /// <summary>Explorations for one Challenge seed (then the next fixed seed): a number, never a time.</summary>
        public int challengeMaxAttempts = 1200;
        /// <summary>Positions per explored layout in the Challenge (small: generation on the phone stays fast).</summary>
        public int challengeMaxStates = 4000;

        public Color TargetColor(int target) =>
            targetColors.Count == 0 ? Color.red : targetColors[Mathf.Clamp(target, 0, targetColors.Count - 1)];

        public Color VehicleColor(int index) =>
            vehicleColors.Count == 0 ? Color.gray : vehicleColors[((index % vehicleColors.Count) + vehicleColors.Count) % vehicleColors.Count];

        public static TrafficJamConfig LoadOrDefault()
        {
            var cfg = Resources.Load<TrafficJamConfig>(ResourcesPath);
            if (cfg != null) return cfg;
            Debug.LogWarning("TrafficJamConfig missing in Resources/" + ResourcesPath + ": default values used.");
            return CreateInstance<TrafficJamConfig>();
        }
    }
}
