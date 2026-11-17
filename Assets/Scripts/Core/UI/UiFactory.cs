using ResolverMaster.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Builds uGUI elements from the <see cref="UiKitStyle"/>: every screen is assembled from these helpers, so the look lives in one place.
    /// Texts are TextMeshPro (Fredoka SDF; outlined texts use the SDF outline material of the style).
    /// Sprites are the Gemini UI art (buttons, panels, tiles, stars, icons); icons named <c>icon_*</c> are full-color drawings
    /// (<see cref="Icon"/>), the few remaining placeholder icons are white masks tinted by code (<see cref="MaskIcon"/>).
    /// Works in edit mode too (previews): nothing here depends on Awake/Start.
    /// </summary>
    public sealed class UiFactory
    {
        public const int UiLayer = 5;

        public readonly UiKitStyle Style;
        public readonly IconLibrary Icons;
        public readonly Localizer Loc;

        public UiFactory(UiKitStyle style, IconLibrary icons, Localizer loc)
        {
            Style = style;
            Icons = icons;
            Loc = loc;
        }

        // ---------- rects ----------

        public RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Full-width strip glued to the top, <paramref name="height"/> tall.</summary>
        public static void TopStrip(RectTransform rt, float height, float top = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = new Vector2(0, -top);
        }

        /// <summary>Fixed-size rect anchored at (ax, ay) of the parent, pivot at the same point.</summary>
        public static void Place(RectTransform rt, float ax, float ay, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ax, ay);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        /// <summary>
        /// Fixes the size of an element inside a layout group. flexibleHeight defaults to 0 (not -1) on purpose: a nested layout group with
        /// childForceExpand reports flexible height 1, and the parent column would then stretch the element to fill the screen.
        /// </summary>
        public static LayoutElement Layout(Component c, float preferredHeight = -1, float preferredWidth = -1, float flexibleWidth = -1, float flexibleHeight = 0)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.minWidth = preferredWidth;
            le.flexibleWidth = flexibleWidth;
            le.flexibleHeight = flexibleHeight;
            return le;
        }

        /// <summary>
        /// A vertical scroll list filling the rest of a column (ErNa-006: 9 chapters do not fit a screen): a masked viewport (flexible
        /// height) and a <paramref name="content"/> column that grows with its children. Drags anywhere in the list scroll it.
        /// </summary>
        public ScrollRect ScrollList(string name, Transform parent, float spacing, out RectTransform content, float bottomPadding = 0)
        {
            var viewport = Rect(name, parent);
            Layout(viewport, flexibleHeight: 1);
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();   // transparent: catches drags between the items
            catcher.color = new Color(1, 1, 1, 0);
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            Column(content, spacing, new RectOffset(0, 0, 0, (int)bottomPadding));
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            return scroll;
        }

        public VerticalLayoutGroup Column(RectTransform rt, float spacing, RectOffset padding = null)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset();
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public HorizontalLayoutGroup Row(RectTransform rt, float spacing, RectOffset padding = null)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding ?? new RectOffset();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            return h;
        }

        // ---------- graphics ----------

        /// <summary>An image; sprites with a border are drawn 9-sliced. <paramref name="pill"/>: the slices keep the height (<see cref="SlicedFit"/>).</summary>
        public Image Image(string name, Transform parent, Sprite sprite, Color color, bool pill = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            if (pill) rt.gameObject.AddComponent<SlicedFit>();
            return img;
        }

        /// <summary>A full-color icon drawn by Gemini (icon_*, stars): never tinted, aspect kept.</summary>
        public Image Icon(string name, Transform parent, string iconName, float size)
        {
            var sprite = Icons != null && iconName != null ? Icons.Get(iconName) : null;
            if (sprite == null && iconName != null) Debug.LogWarning($"UiFactory: icon '{iconName}' not found in the IconLibrary");
            var img = Image(name, parent, sprite, Color.white);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        /// <summary>A sprite drawn whole (stars, ribbon...), aspect kept.</summary>
        public Image Picture(string name, Transform parent, Sprite sprite, float size)
        {
            var img = Image(name, parent, sprite, Color.white);
            img.type = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        /// <summary>A placeholder icon (white mask) tinted with <paramref name="color"/> (chapter themes, game icons).</summary>
        public Image MaskIcon(string name, Transform parent, string iconName, float size, Color color)
        {
            var img = Icon(name, parent, iconName, size);
            img.color = color;
            return img;
        }

        /// <summary>A localized label: the text comes from <paramref name="key"/> and follows language changes.</summary>
        public TextMeshProUGUI Label(string name, Transform parent, string key, int size, Color color, TextAnchor align, bool bold = false)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = bold ? Style.fontBold : Style.fontRegular;
            t.fontSize = size;
            t.color = color;
            t.alignment = ToTmp(align);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = false;
            if (!string.IsNullOrEmpty(key)) rt.gameObject.AddComponent<LocalizedText>().Bind(Loc, key);
            return t;
        }

        /// <summary>A title: light text with the dark SDF outline of the style.</summary>
        public TextMeshProUGUI Title(string name, Transform parent, string key, int size, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Label(name, parent, key, size, Style.title, align, bold: true);
            SetOutlined(t, true);
            return t;
        }

        /// <summary>Dark SDF outline on a bold text (titles, labels on colored buttons), or back to the plain font material.</summary>
        public void SetOutlined(TMP_Text text, bool on)
        {
            if (on && Style.fontBoldOutlined != null)
            {
                text.font = Style.fontBold;
                text.fontSharedMaterial = Style.fontBoldOutlined;
            }
            else if (text.font != null) text.fontSharedMaterial = text.font.material;
        }

        /// <summary>Shrinks the text down to <paramref name="minSize"/> when it does not fit (one line).</summary>
        public static void AutoSize(TMP_Text t, float minSize)
        {
            t.enableAutoSizing = true;
            t.fontSizeMax = t.fontSize;
            t.fontSizeMin = minSize;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        public static TextAlignmentOptions ToTmp(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        /// <summary>Cream Gemini window (not tappable).</summary>
        public Image Panel(string name, Transform parent) => Image(name, parent, Style.panel, Color.white);

        // ---------- controls ----------

        /// <summary>A plain button (no press animation, no color transition): sprite + Button.</summary>
        public Button Button(string name, Transform parent, Sprite sprite, Color color, bool pill = false)
        {
            var img = Image(name, parent, sprite, color, pill);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            return b;
        }

        /// <summary>A button that goes down and gets darker when pressed, desaturated when disabled (<see cref="PressableButton"/>).</summary>
        public Button Pressable(string name, Transform parent, Sprite sprite, Color color, bool pill = false)
        {
            var b = Button(name, parent, sprite, color, pill);
            b.gameObject.AddComponent<PressableButton>().Init((Image)b.targetGraphic, Style.pressDrop, Style.pressDarken, Style.disabledMaterial, Style.disabledContentAlpha);
            return b;
        }

        /// <summary>Home card: the generated frame tinted with the game color (outline and bottom band baked in the sprite).</summary>
        public Button CardButton(string name, Transform parent, Color color) => Pressable(name, parent, Style.cardSprite, color);

        /// <summary>A tappable cream Gemini window (mode cards).</summary>
        public Button PanelButton(string name, Transform parent) => Pressable(name, parent, Style.panel, Color.white);

        /// <summary>Round Gemini button (cream, or yellow) with a centred full-color icon. <paramref name="size"/> 0 = style size.</summary>
        public Button IconButton(string name, Transform parent, string iconName, float size = 0, float iconSize = 0, bool yellow = false)
        {
            if (size <= 0) size = Style.iconButtonSize;
            if (iconSize <= 0) iconSize = Style.iconInButton * size / Style.iconButtonSize;
            var b = Pressable(name, parent, yellow ? Style.roundYellow : Style.roundCream, Color.white);
            ((Image)b.targetGraphic).preserveAspect = true;
            ((RectTransform)b.transform).sizeDelta = new Vector2(size, size);
            var icon = Icon("Icon", b.transform, iconName, iconSize);
            // the round face sits above the darker band at the bottom of the sprite: centre the icon on the face
            Place(icon.rectTransform, 0.5f, 0.5f, new Vector2(iconSize, iconSize), new Vector2(0, size * 0.025f));
            return b;
        }

        /// <summary>Pill button with a centred localized label (the label sits on the face, above the bottom band of the sprite).</summary>
        public Button TextButton(string name, Transform parent, string key, bool green, out TextMeshProUGUI label)
        {
            var b = Pressable(name, parent, green ? Style.buttonGreen : Style.buttonCream, Color.white, pill: true);
            label = Label("Label", b.transform, key, Style.buttonTextSize, green ? Style.onPrimary : Style.text, TextAnchor.MiddleCenter, bold: true);
            Stretch(label.rectTransform, 28, 14, 28, 4);
            AutoSize(label, Style.smallSize);
            if (green) SetOutlined(label, true);
            return b;
        }

        /// <summary>Main button (PLAY, Open the water, Next): green Gemini pill, white outlined text.</summary>
        public Button PrimaryButton(string name, Transform parent, string key, out TextMeshProUGUI label) => TextButton(name, parent, key, true, out label);

        /// <summary>Secondary button: cream Gemini pill, dark text.</summary>
        public Button SecondaryButton(string name, Transform parent, string key, out TextMeshProUGUI label) => TextButton(name, parent, key, false, out label);

        /// <summary>Switches a pill between green (selected / on, white outlined text) and cream (dark text).</summary>
        public void SetGreen(Button pill, TMP_Text label, bool green)
        {
            ((Image)pill.targetGraphic).sprite = green ? Style.buttonGreen : Style.buttonCream;
            if (label == null) return;
            label.color = green ? Style.onPrimary : Style.text;
            SetOutlined(label, green);
            if (!green) label.font = Style.fontBold;
        }

        /// <summary>Enables/disables a button: pressable buttons get desaturated sprites and faded texts.</summary>
        public static void SetInteractable(Button b, bool on)
        {
            var p = b.GetComponent<PressableButton>();
            if (p != null) p.SetInteractable(on);
            else b.interactable = on;
        }
    }
}
