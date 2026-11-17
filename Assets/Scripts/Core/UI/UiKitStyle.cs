using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// The single style file of the UI kit: colors, radii, thicknesses, sizes, text sizes, animation durations, fonts and the
    /// generated 9-slice sprites. All values are in canvas units of the 1080x1920 reference (1 unit = 1 px at 1080 wide).
    /// Asset: Assets/Resources/ResolverMaster/UiKitStyle.asset. After changing a radius or a thickness run
    /// Resolver Master > UI Kit > Regenerate Sprites.
    /// </summary>
    [CreateAssetMenu(menuName = "Resolver Master/UI Kit Style", fileName = "UiKitStyle")]
    public sealed class UiKitStyle : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/UiKitStyle";

        [Header("Canvas")]
        public Vector2 referenceResolution = new Vector2(1080, 1920);

        [Header("Colors")]
        public Color background = Hex(0xEEF2F7);
        public Color surface = Hex(0xFFFFFF);
        public Color primary = Hex(0x3D6FE0);
        public Color onPrimary = Hex(0xFFFFFF);
        public Color accent = Hex(0xFF9F43);
        public Color text = Hex(0x1F2A44);
        public Color textMuted = Hex(0x6B7896);
        public Color badge = Hex(0xE3E8F2);
        public Color badgeText = Hex(0x55627F);
        public Color toggleOff = Hex(0xC9D1E0);
        public Color outline = Hex(0xD5DCE8);

        [Header("Radii (sprite generation)")]
        public int buttonRadius = 48;
        public int cardRadius = 40;
        public int panelRadius = 56;
        public int circleRadius = 64;

        [Header("Thickness")]
        public int outlineThickness = 4;

        [Header("Sizes and spacing")]
        public float screenPadding = 48;
        public float spacing = 32;
        public float topBarHeight = 176;
        public float iconButtonSize = 128;
        public float iconInButton = 72;
        public float cardHeight = 240;
        public float cardIconSize = 176;
        public float modeCardHeight = 280;
        public float buttonHeight = 128;
        public float rowHeight = 128;
        public float rowIconSize = 64;
        public float badgeHeight = 64;

        [Header("Text sizes")]
        public int titleSize = 84;
        public int headerSize = 60;
        public int bodySize = 44;
        public int buttonTextSize = 46;
        public int smallSize = 36;

        [Header("Animation durations (seconds)")]
        public float screenFade = 0.18f;
        public float buttonPress = 0.08f;
        public float messageFade = 0.25f;

        [Header("Fonts (Assets/Art/Fonts)")]
        public Font fontRegular;
        public Font fontBold;

        [Header("Generated 9-slice sprites (Assets/Art/UI)")]
        public Sprite buttonSprite;
        public Sprite cardSprite;
        public Sprite panelSprite;
        public Sprite outlineSprite;
        public Sprite circleSprite;

        public static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
