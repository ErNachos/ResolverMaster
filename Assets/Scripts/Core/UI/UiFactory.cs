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

        // ---------- controls ----------

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
            colors.pressedColor = new Color(0.82f, 0.82f, 0.86f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            colors.fadeDuration = Style.buttonPress;
            b.colors = colors;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            return b;
        }

        /// <summary>Round button with a centred icon (back, settings...).</summary>
        public Button IconButton(string name, Transform parent, string iconName)
        {
            var b = Button(name, parent, Style.circleSprite, Style.surface);
            ((RectTransform)b.transform).sizeDelta = new Vector2(Style.iconButtonSize, Style.iconButtonSize);
            var icon = Icon("Icon", b.transform, iconName, Style.iconInButton, Style.primary);
            Place(icon.rectTransform, 0.5f, 0.5f, new Vector2(Style.iconInButton, Style.iconInButton), Vector2.zero);
            return b;
        }

        /// <summary>Rectangular button with a centred localized label.</summary>
        public Button TextButton(string name, Transform parent, string key, Color background, Color textColor, out Text label)
        {
            var b = Button(name, parent, Style.buttonSprite, background);
            label = Label("Label", b.transform, key, Style.buttonTextSize, textColor, TextAnchor.MiddleCenter, bold: true);
            Stretch(label.rectTransform, 16, 8, 16, 8);
            return b;
        }
    }
}
