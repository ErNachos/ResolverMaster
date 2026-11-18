using System.Collections.Generic;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI.Themes;
using UnityEngine;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>
    /// The single parameter file of Pingù (PG-001): colors of the provisional art, board, swipe, animation times, solver and generator
    /// limits, the Story chapters (name, icon, color, theme), the Story difficulty table and the Challenge steps and margins.
    /// Asset: Assets/Resources/Games/Penguin/PenguinConfig.asset (Setup > Ensure Config Assets). Sizes are fractions of a cell.
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Penguin Config", fileName = "PenguinConfig")]
    public sealed class PenguinConfig : ScriptableObject
    {
        public const string ResourcesPath = "Games/Penguin/PenguinConfig";

        [Header("Colors of the provisional art")]
        /// <summary>
        /// Scarf of each penguin and the flag of its igloo: orange (plain) for the first, violet (striped) for the second. The pattern
        /// always differs too (never the color alone).
        /// </summary>
        public List<Color> scarfColors = new List<Color> { new Color32(0xF2, 0x8C, 0x28, 0xFF), new Color32(0x8E, 0x5B, 0xB5, 0xFF) };
        public Color penguinBody = new Color32(0x2B, 0x2F, 0x3A, 0xFF);
        public Color penguinBelly = new Color32(0xFA, 0xFA, 0xF5, 0xFF);
        public Color penguinBeak = new Color32(0xF6, 0xB2, 0x3C, 0xFF);
        public Color fishColor = new Color32(0xF2, 0x8F, 0x7A, 0xFF);
        public Color fishFin = new Color32(0xE0, 0x6A, 0x58, 0xFF);
        public Color rockColor = new Color32(0x8A, 0x8F, 0x96, 0xFF);
        public Color rockShade = new Color32(0x6B, 0x70, 0x78, 0xFF);
        public Color snowColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public Color snowShade = new Color32(0xD8, 0xE4, 0xEC, 0xFF);
        public Color waterColor = new Color32(0x3E, 0x8E, 0xC9, 0xFF);
        public Color waterDeep = new Color32(0x2C, 0x6C, 0xA6, 0xFF);
        public Color blockColor = new Color32(0xBF, 0xE8, 0xF4, 0xFF);
        public Color blockShade = new Color32(0x8C, 0xC9, 0xDE, 0xFF);
        public Color iglooColor = new Color32(0xF7, 0xFA, 0xFC, 0xFF);
        public Color iglooShade = new Color32(0xC9, 0xD9, 0xE4, 0xFF);
        public Color iglooDoor = new Color32(0x2B, 0x33, 0x40, 0xFF);
        /// <summary>The ice plate that closes the door of an igloo while fish are missing.</summary>
        public Color iglooClosed = new Color32(0xA9, 0xD4, 0xE6, 0xFF);
        public Color hintArrowColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        [Header("Art (fractions of a cell)")]
        [Range(0f, 0.1f)] public float outline = 0.035f;
        /// <summary>Texture pixels per cell of the generated sprites (at least the biggest cell, about 230 units on a 4x6 grid: never scaled up).</summary>
        public int spritePixelsPerCell = 256;
        /// <summary>Room kept around the cells for the border (snow wall or water), in cells.</summary>
        [Range(0f, 0.5f)] public float borderDepth = 0.24f;

        [Header("Board")]
        public float boardInset = 30;
        public float compactCellBelow = 150;
        [Range(0.3f, 1f)] public float compactFrameScale = 0.5f;
        public float compactOverflow = 16;
        /// <summary>A swipe counts from this distance (fraction of a cell, at least <see cref="swipeMinUnits"/> canvas units): a tap is not a swipe.</summary>
        [Range(0.05f, 1f)] public float swipeThreshold = 0.3f;
        public float swipeMinUnits = 40f;

        [Header("Animation (seconds, real time)")]
        /// <summary>Slide speed: cells per second, the same for penguins and blocks.</summary>
        public float slideSpeed = 7f;
        /// <summary>The squash of a stop against something, and the dizzy face after it.</summary>
        public float bumpDuration = 0.18f;
        public float dizzyTime = 0.6f;
        /// <summary>A fish taken: it pops and fades; the igloo opening; a penguin going in.</summary>
        public float fishPop = 0.3f;
        public float iglooOpen = 0.35f;
        public float enterDuration = 0.3f;
        /// <summary>A penguin falling into the water (scared, it shrinks and the water splashes), then the pause before the restart.</summary>
        public float fallDuration = 0.55f;
        public float failPause = 0.6f;
        /// <summary>A block sinking into a hole (the hole becomes ice at the end).</summary>
        public float sinkDuration = 0.35f;
        /// <summary>Pause after the last penguin is in, then the win panel.</summary>
        public float winPause = 0.35f;
        /// <summary>The light bulb: the arrow shows, then the swipe is made.</summary>
        public float hintArrowTime = 0.55f;

        [Header("Solver and generator")]
        /// <summary>Most reachable states of a level (solver, light bulb, "You are stuck", validator).</summary>
        public int maxStates = PenguinSpace.DefaultMaxStates;
        /// <summary>Story generator (Editor): states per explored draft (bigger ones are discarded), explorations per level.</summary>
        public int storyMaxStates = 40000;
        public int storyMaxAttempts = 3000;
        /// <summary>Changes tried on one draft before a new one (hill climbing).</summary>
        public int climbSteps = 40;
        /// <summary>The highest minimum moves of any level.</summary>
        public int maxMinMoves = 30;
        /// <summary>Story level n is generated from seed storySeedBase + n.</summary>
        public int storySeedBase = 5101000;

        [Header("Story chapters (9 levels each, in order)")]
        public List<ChapterTheme> chapters = new List<ChapterTheme>
        {
            new ChapterTheme(PenguinKeys.Chapter1, "ice_skating", new Color32(0xA9, 0xC9, 0xD2, 0xFF), ThemeConfig.Lake),
            new ChapterTheme(PenguinKeys.Chapter2, "set_meal", new Color32(0x9F, 0xC3, 0xBC, 0xFF), ThemeConfig.PackIce),
            new ChapterTheme(PenguinKeys.Chapter3, "weather_snowy", new Color32(0xB8, 0xC2, 0xCA, 0xFF), ThemeConfig.Blizzard),
            new ChapterTheme(PenguinKeys.Chapter4, "waves", new Color32(0xA8, 0xC3, 0xA0, 0xFF), ThemeConfig.Thaw),
            new ChapterTheme(PenguinKeys.Chapter5, "terrain", new Color32(0x9D, 0xB7, 0xC4, 0xFF), ThemeConfig.Glacier),
            new ChapterTheme(PenguinKeys.Chapter6, "diversity_1", new Color32(0xC4, 0xB8, 0xB0, 0xFF), ThemeConfig.Pole),
            new ChapterTheme(PenguinKeys.Chapter7, "auto_awesome", new Color32(0x8F, 0xCB, 0xB0, 0xFF), ThemeConfig.NorthernLights),
        };

        [Header("Story difficulty (one tier per level, numbered across chapters)")]
        public DifficultyTable storyDifficulty = PenguinDifficulty.Story();

        [Header("Challenge: steps by level of the run, margins, generator limits per seed")]
        public DifficultyTable challengeDifficulty = PenguinDifficulty.Challenge();
        /// <summary>Margins (minimum + 60% on the first step down to + 10% on the last, at least +1), seed retries, the 5 daily steps.</summary>
        public ChallengeSettings challenge = new ChallengeSettings
        {
            dailyTiers = { "ch-6", "ch-8", "ch-10", "ch-12", "ch-14" },
        };
        /// <summary>Explorations for one Challenge seed (then the next fixed seed): a number, never a time.</summary>
        public int challengeMaxAttempts = 600;
        /// <summary>States per explored draft in the Challenge (small: generation on the phone stays fast).</summary>
        public int challengeMaxStates = 15000;

        public Color ScarfColor(int penguin) =>
            scarfColors.Count == 0 ? Color.white : scarfColors[Mathf.Clamp(penguin, 0, scarfColors.Count - 1)];

        public PenguinChallenge CreateChallenge() =>
            new PenguinChallenge(challengeDifficulty, challenge, challengeMaxAttempts, challengeMaxStates, maxMinMoves, climbSteps);

        public static PenguinConfig LoadOrDefault()
        {
            var cfg = Resources.Load<PenguinConfig>(ResourcesPath);
            if (cfg != null) return cfg;
            Debug.LogWarning("PenguinConfig missing in Resources/" + ResourcesPath + ": default values used.");
            return CreateInstance<PenguinConfig>();
        }
    }
}
