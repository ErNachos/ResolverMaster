using ResolverMaster.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Builds uGUI elements from the <see cref="UiKitStyle"/>: every screen is assembled from these helpers, so the look lives in one place.
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

        public Image Image(string name, Transform parent, Sprite sprite, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public Image Icon(string name, Transform parent, string iconName, float size, Color color)
        {
            var sprite = Icons != null && iconName != null ? Icons.Get(iconName) : null;
            if (sprite == null && iconName != null) Debug.LogWarning($"UiFactory: icon '{iconName}' not found in the IconLibrary");
            var img = Image(name, parent, sprite, color);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        /// <summary>A localized label: the text comes from <paramref name="key"/> and follows language changes.</summary>
        public Text Label(string name, Transform parent, string key, int size, Color color, TextAnchor align, bool bold = false)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = bold ? Style.fontBold : Style.fontRegular;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.lineSpacing = 1f;
            t.raycastTarget = false;
            if (!string.IsNullOrEmpty(key)) rt.gameObject.AddComponent<LocalizedText>().Bind(Loc, key);
            return t;
        }

        /// <summary>A title: light text with the dark outline of the style.</summary>
        public Text Title(string name, Transform parent, string key, int size, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Label(name, parent, key, size, Style.title, align, bold: true);
            AddOutline(t);
            return t;
        }

        /// <summary>Dark outline around a text (titles, labels on colored buttons).</summary>
        public Outline AddOutline(Text text)
        {
            var o = text.gameObject.GetComponent<Outline>();
            if (o == null) o = text.gameObject.AddComponent<Outline>();
            o.effectColor = Style.outline;
            o.effectDistance = new Vector2(Style.textOutline, -Style.textOutline);
            o.useGraphicAlpha = true;
            return o;
        }

        /// <summary>Cream panel with the black outline (not tappable).</summary>
        public Image Panel(string name, Transform parent, Color color) => Image(name, parent, Style.panelSprite, color);

        // ---------- controls ----------

        /// <summary>A plain button (no press animation): tinted sprite + Button with the muted disabled tint.</summary>
        public Button Button(string name, Transform parent, Sprite sprite, Color color)
        {
            var img = Image(name, parent, sprite, color);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.disabledColor = Style.disabledTint;
            colors.fadeDuration = Style.buttonPress;
            b.colors = colors;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            return b;
        }

        /// <summary>A button that goes down when pressed (<see cref="PressableButton"/>): <paramref name="pressed"/> is the sprite while held.</summary>
        public Button Pressable(string name, Transform parent, Sprite normal, Sprite pressed, Color color, float drop)
        {
            var b = Button(name, parent, normal, color);
            b.gameObject.AddComponent<PressableButton>().Init((Image)b.targetGraphic, normal, pressed, drop, Style.disabledContentAlpha, Style.disabledDesaturate);
            return b;
        }

        /// <summary>Tappable card (chapters, levels, modes, Home): cream or colored, outline and bottom band.</summary>
        public Button CardButton(string name, Transform parent, Color color) =>
            Pressable(name, parent, Style.cardSprite, Style.cardPressedSprite, color, Mathf.Max(0, Style.cardDepth - Style.pressedDepth));

        /// <summary>Round button with a centred dark icon (back, settings, undo, restart, help). <paramref name="size"/> 0 = style size.</summary>
        public Button IconButton(string name, Transform parent, string iconName, float size = 0, float iconSize = 0)
        {
            if (size <= 0) size = Style.iconButtonSize;
            if (iconSize <= 0) iconSize = Style.iconInButton;
            float k = size / Mathf.Max(1f, Style.iconButtonSize);   // the round sprite scales with the rect: so do its depth and drop
            var b = Pressable(name, parent, Style.roundSprite, Style.roundPressedSprite, Style.surface, Style.PressDrop * k);
            ((RectTransform)b.transform).sizeDelta = new Vector2(size, size);
            var icon = Icon("Icon", b.transform, iconName, iconSize, Style.icon);
            Place(icon.rectTransform, 0.5f, 0.5f, new Vector2(iconSize, iconSize), new Vector2(0, Style.buttonDepth * k / 2f));
            return b;
        }

        /// <summary>Rectangular button with a centred localized label (label on the face, above the bottom band).</summary>
        public Button TextButton(string name, Transform parent, string key, Color background, Color textColor, out Text label, bool outlined = false)
        {
            var b = Pressable(name, parent, Style.buttonSprite, Style.buttonPressedSprite, background, Style.PressDrop);
            label = Label("Label", b.transform, key, Style.buttonTextSize, textColor, TextAnchor.MiddleCenter, bold: true);
            Stretch(label.rectTransform, 20, 6 + Style.buttonDepth, 20, 6);
            if (outlined) AddOutline(label);
            return b;
        }

        /// <summary>Main button (PLAY, Open the water, Next): vivid green, white outlined text.</summary>
        public Button PrimaryButton(string name, Transform parent, string key, out Text label) =>
            TextButton(name, parent, key, Style.primary, Style.onPrimary, out label, outlined: true);

        /// <summary>Secondary button: cream, dark text.</summary>
        public Button SecondaryButton(string name, Transform parent, string key, out Text label) =>
            TextButton(name, parent, key, Style.surface, Style.text, out label);

        /// <summary>Enables/disables a button: muted background (Button tint) and, for pressable buttons, muted label and icon.</summary>
        public static void SetInteractable(Button b, bool on)
        {
            var p = b.GetComponent<PressableButton>();
            if (p != null) p.SetInteractable(on);
            else b.interactable = on;
        }
    }
}
