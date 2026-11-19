using System.Collections.Generic;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI.Themes;
using UnityEngine;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>
    /// The single parameter file of Pino (PG-001): sizes of the drawings (PG-002), board, swipe, animation times, solver and generator
    /// limits, the Story chapters (name, icon, color, theme), the Story difficulty table and the Challenge steps and margins.
    /// Asset: Assets/Resources/Games/Penguin/PenguinConfig.asset (Setup > Ensure Config Assets). Sizes are fractions of a cell.
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/Penguin Config", fileName = "PenguinConfig")]
    public sealed class PenguinConfig : ScriptableObject
    {
        public const string ResourcesPath = "Games/Penguin/PenguinConfig";

        [Header("Drawings (PG-002: Assets/Art/Penguin; sizes in cells)")]
        /// <summary>
        /// Scarf of each penguin and the flag of its igloo, as painted in the drawings: orange (plain) for the first, purple (striped) for
        /// the second. The pattern always differs too (never the color alone); the validator keeps the two colors far apart.
        /// </summary>
        public List<Color> scarfColors = new List<Color> { new Color32(0xE4, 0x7D, 0x49, 0xFF), new Color32(0x8A, 0x35, 0x75, 0xFF) };
        /// <summary>Height of the drawings of the penguins seen from above (one canvas for still and sliding: the still body is ~80% of it).</summary>
        [Range(0.5f, 1.5f)] public float topPenguinHeight = 1.08f;
        /// <summary>Height of the drawings of the penguins seen from the front (one canvas: the body is ~85% of it; the canvas is the one of PG-002, room on top included).</summary>
        [Range(0.5f, 2f)] public float frontPenguinHeight = 1.3f;
        /// <summary>The feet of a penguin seen from the front stand this far above the bottom of its cell (it may stick out upwards).</summary>
        [Range(0f, 0.5f)] public float frontPenguinFeet = 0.08f;
        /// <summary>Width of an igloo (its base on the bottom of the cell; the dome and the flag stick out upwards).</summary>
        [Range(0.5f, 1.2f)] public float iglooWidth = 0.94f;
        [Range(0.2f, 1f)] public float fishSize = 0.7f;
        [Range(0.3f, 1f)] public float blockSize = 0.86f;
        [Range(0.3f, 1f)] public float rockSize = 0.94f;
        /// <summary>Outline of the effects still drawn by code (light bulb arrow, splash, sparkle).</summary>
        [Range(0f, 0.1f)] public float outline = 0.035f;
        /// <summary>Texture pixels per cell of the effects drawn by code (at least the biggest cell: never scaled up).</summary>
        public int spritePixelsPerCell = 256;
        /// <summary>
        /// Room kept around the cells for the border, in cells: a row of square pieces (snow wall or water tile), 1 / borderDepth of them
        /// along each cell side.
        /// </summary>
        [Range(0.1f, 0.5f)] public float borderDepth = 0.3334f;
        public Color hintArrowColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

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
        /// <summary>The squash of a stop against something.</summary>
        public float bumpDuration = 0.18f;
        /// <summary>A penguin "turns towards us" (from above to the front, or back): a horizontal squash that swaps the drawing.</summary>
        public float turnDuration = 0.16f;
        /// <summary>A fish taken: it jumps (<see cref="fishJump"/> cells) and vanishes; the igloo opening.</summary>
        public float fishPop = 0.4f;
        [Range(0f, 1.5f)] public float fishJump = 0.45f;
        public float iglooOpen = 0.35f;
        /// <summary>A penguin falling into the water: scared (after the turn) for <see cref="scaredHold"/>, then it shrinks and the water splashes; then the pause before the restart.</summary>
        public float scaredHold = 0.25f;
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
