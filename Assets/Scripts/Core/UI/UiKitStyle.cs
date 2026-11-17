using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// The single style file of the UI kit: colors, radii, thicknesses, sizes, text sizes, animation durations, fonts and the
    /// generated 9-slice sprites. All values are in canvas units of the 1080x1920 reference (1 unit = 1 px at 1080 wide).
    /// Asset: Assets/Resources/ResolverMaster/UiKitStyle.asset. After changing a radius, a thickness or a depth run
    /// Resolver Master > UI Kit > Regenerate Sprites.
    /// Look (ErNa-004): cartoon, thick black outlines baked into the sprites, very round corners, flat colors, a darker band at the
    /// bottom of buttons and cards ("press me" depth). Blue and orange are the water colors of the Plumber: never main UI colors.
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/UI Kit Style", fileName = "UiKitStyle")]
    public sealed class UiKitStyle : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/UiKitStyle";

        [Header("Canvas")]
        public Vector2 referenceResolution = new Vector2(1080, 1920);

        [Header("Colors")]
        /// <summary>Screen background (soft lavender).</summary>
        public Color background = Hex(0xE6DCF5);
        /// <summary>Panels, cards and light (secondary) buttons: cream.</summary>
        public Color surface = Hex(0xFFF6E3);
        /// <summary>Main button (PLAY, Open the water, Next): vivid green.</summary>
        public Color primary = Hex(0x5CC93B);
        public Color onPrimary = Hex(0xFFFFFF);
        /// <summary>Second accent (Challenge): raspberry.</summary>
        public Color accent = Hex(0xF25C8A);
        public Color text = Hex(0x2B2238);
        public Color textMuted = Hex(0x6E6480);
        /// <summary>Every UI icon (placeholders until the final icons): one dark color.</summary>
        public Color icon = Hex(0x2B2238);
        public Color badge = Hex(0xEDE2C8);
        public Color badgeText = Hex(0x5E5470);
        public Color toggleOff = Hex(0xDCD3C2);
        /// <summary>Text outline of titles and of labels on colored buttons.</summary>
        public Color outline = Hex(0x1C1726);
        /// <summary>Multiplies a disabled button (muted colors).</summary>
        public Color disabledTint = new Color(0.92f, 0.91f, 0.9f, 1f);
        /// <summary>Alpha of the label/icon of a disabled button.</summary>
        [Range(0f, 1f)] public float disabledContentAlpha = 0.45f;
        /// <summary>How much a disabled button loses its color (0 = none, 1 = grey).</summary>
        [Range(0f, 1f)] public float disabledDesaturate = 0.7f;
        /// <summary>Color of the titles (with the dark outline).</summary>
        public Color title = Hex(0xFFFFFF);

        [Header("Radii (sprite generation)")]
        public int buttonRadius = 48;
        public int cardRadius = 44;
        public int panelRadius = 56;
        /// <summary>Pills and circles (badges, dots, icon circles).</summary>
        public int circleRadius = 112;

        [Header("Thickness and depth (sprite generation)")]
        /// <summary>Black outline of every generated sprite.</summary>
        public int outlineThickness = 6;
        /// <summary>Thin black outline of pills and small circles.</summary>
        public int thinOutline = 5;
        /// <summary>Darker band at the bottom of a button at rest ("press me" depth).</summary>
        public int buttonDepth = 14;
        /// <summary>The same band while pressed: the face goes down by buttonDepth - pressedDepth.</summary>
        public int pressedDepth = 4;
        /// <summary>Darker band at the bottom of cards and panels.</summary>
        public int cardDepth = 12;
        /// <summary>Shade of the bottom band (multiplies the fill color).</summary>
        [Range(0.3f, 1f)] public float depthShade = 0.72f;
        /// <summary>Text outline distance of titles and colored-button labels.</summary>
        public float textOutline = 4f;

        [Header("Sizes and spacing")]
        public float screenPadding = 48;
        public float spacing = 32;
        public float topBarHeight = 176;
        /// <summary>Round icon buttons (back, settings): at least 130 to be easy to tap.</summary>
        public float iconButtonSize = 136;
        public float iconInButton = 68;
        public float cardIconSize = 176;
        public float modeCardHeight = 280;
        public float buttonHeight = 136;
        public float rowHeight = 136;
        public float rowIconSize = 64;
        public float badgeHeight = 64;
        /// <summary>Smallest tappable side (checked by the previews).</summary>
        public float minTouchSize = 130;

        [Header("Home (one big card per minigame)")]
        public float homeTitleHeight = 200;
        public Vector2 homeCardSize = new Vector2(720, 960);
        /// <summary>Distance between the centres of two cards.</summary>
        public float homeCardStep = 700;
        /// <summary>Scale and alpha of a card one step away from the centre.</summary>
        [Range(0.3f, 1f)] public float homeSideScale = 0.8f;
        [Range(0f, 1f)] public float homeSideAlpha = 0.5f;
        /// <summary>Time constant of the snap animation (s): smaller = snappier.</summary>
        public float homeSnapTime = 0.09f;
        /// <summary>A flick moves the target by velocity * this (s), at most one card.</summary>
        public float homeFlickTime = 0.18f;
        public float homeCardInset = 28;
        public float homeCardNameHeight = 150;
        public int homeCardNameSize = 84;
        public float homeDotSize = 30;
        public float homeDotGap = 22;
        public float homeDotsHeight = 64;
        public Vector2 playButtonSize = new Vector2(560, 176);
        public int playTextSize = 84;
        /// <summary>Mix of a "coming soon" card towards the disabled tint.</summary>
        [Range(0f, 1f)] public float comingSoonDim = 0.55f;

        [Header("Puzzle screens (chapters, level grid, game, win panel)")]
        public float bottomBarHeight = 208;
        public float gameButtonSize = 144;
        public float gameIconSize = 76;
        public float gameButtonGap = 72;
        /// <summary>Level grid of a chapter: 3 columns x 3 rows (9 levels per chapter).</summary>
        public int levelColumns = 3;
        public Vector2 levelButtonSize = new Vector2(280, 300);
        public float levelGridSpacing = 40;
        public float levelStarSize = 56;
        public int levelNumberSize = 84;
        public float chapterCardHeight = 264;
        public float chapterIconSize = 168;
        public float chapterStarSize = 48;
        /// <summary>Optional main action button, centred above the bottom bar (e.g. "Open the water").</summary>
        public float mainActionWidth = 760;
        public float mainActionHeight = 152;
        /// <summary>Space between the main action button and the bottom bar (at least 48).</summary>
        public float mainActionGap = 48;
        /// <summary>Space between the puzzle area and the main action button.</summary>
        public float mainActionGapAbove = 24;
        public float mainActionIconSize = 76;
        /// <summary>Short message shown where the main action button is (the button hides meanwhile).</summary>
        public Color toastColor = Hex(0xE9545E);
        public Color toastText = Hex(0xFFFFFF);
        public float winPanelWidth = 900;
        public float winStarSize = 150;
        public Color starOn = Hex(0xFFCD3C);
        public Color starOff = Hex(0xE3D9C6);
        public Color dim = new Color(0.10f, 0.07f, 0.16f, 0.6f);

        [Header("Text sizes")]
        public int titleSize = 96;
        public int headerSize = 60;
        public int bodySize = 44;
        public int buttonTextSize = 48;
        public int smallSize = 36;

        [Header("Animation durations (seconds)")]
        public float screenFade = 0.18f;
        public float buttonPress = 0.06f;
        public float messageFade = 0.25f;
        public float winPanelFade = 0.2f;
        /// <summary>Each earned star pops in (scale 0 -> 1.15 -> 1) in this time, one after the other.</summary>
        public float starPop = 0.28f;
        public float starStagger = 0.16f;
        public float toastFade = 0.15f;
        /// <summary>Time a short message stays fully visible.</summary>
        public float toastHold = 1.2f;

        [Header("Fonts (Assets/Art/Fonts)")]
        public Font fontRegular;
        public Font fontBold;

        [Header("Generated sprites (Assets/Art/UI)")]
        /// <summary>Rectangular button at rest / pressed (9-slice, outline + bottom band).</summary>
        public Sprite buttonSprite;
        public Sprite buttonPressedSprite;
        /// <summary>Tappable card (chapters, levels, modes, Home cards) at rest / pressed.</summary>
        public Sprite cardSprite;
        public Sprite cardPressedSprite;
        /// <summary>Panel that is not tapped (win panel, settings groups, board frame).</summary>
        public Sprite panelSprite;
        /// <summary>Outline ring only.</summary>
        public Sprite outlineSprite;
        /// <summary>Pill / circle with a thin outline (badges, dots, icon circles).</summary>
        public Sprite circleSprite;
        /// <summary>Round icon button at rest / pressed (not sliced: drawn for a square rect).</summary>
        public Sprite roundSprite;
        public Sprite roundPressedSprite;
        /// <summary>Plain rounded rectangle without outline (soft fills, overlays).</summary>
        public Sprite softSprite;

        /// <summary>How far the face of a button goes down when pressed.</summary>
        public float PressDrop => Mathf.Max(0, buttonDepth - pressedDepth);

        public static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
