using ResolverMaster.Core.App;
using ResolverMaster.Core.UI.Themes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// The common skeleton of every screen: full-screen background (plain color, or the backdrop of a chapter theme), safe area,
    /// top bar and a body column below it.
    /// </summary>
    public sealed class ScreenLayout
    {
        public RectTransform Root;
        public SafeArea SafeArea;
        public RectTransform TopBar;
        public RectTransform Body;
        /// <summary>Theme backdrop, anchored to the bottom and as wide as the screen (hidden on screens without a theme).</summary>
        public Image Backdrop;

        /// <param name="bodyColumn">False: the body is a plain stretched rect (the game screen places its own areas).</param>
        public static ScreenLayout Create<T>(AppServices ctx, Transform parent, string name, bool bodyColumn = true) where T : UiScreen
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var layout = new ScreenLayout();

            layout.Root = ui.Rect(name, parent);
            UiFactory.Stretch(layout.Root);
            layout.Root.gameObject.AddComponent<CanvasGroup>();
            var bg = layout.Root.gameObject.AddComponent<Image>();
            bg.color = s.background;
            bg.raycastTarget = true;   // blocks taps to the screen below during transitions

            layout.Backdrop = ui.Image("Backdrop", layout.Root, null, Color.white);
            var brt = layout.Backdrop.rectTransform;
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var fitter = layout.Backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = (float)ThemeArt.BackdropWidth / ThemeArt.BackdropHeight;
            layout.Backdrop.gameObject.AddComponent<Decoration>();   // background picture: larger than the screen on purpose
            layout.Backdrop.GetComponent<Decoration>().mayLeaveScreen = true;
            layout.Backdrop.gameObject.SetActive(false);

            var safe = ui.Rect("SafeArea", layout.Root);
            UiFactory.Stretch(safe);
            layout.SafeArea = safe.gameObject.AddComponent<SafeArea>();

            layout.TopBar = ui.Rect("TopBar", safe);
            UiFactory.TopStrip(layout.TopBar, s.topBarHeight);

            layout.Body = ui.Rect("Body", safe);
            UiFactory.Stretch(layout.Body, 0, 0, 0, s.topBarHeight);
            if (bodyColumn) ui.Column(layout.Body, s.spacing, new RectOffset((int)s.screenPadding, (int)s.screenPadding, 8, (int)s.screenPadding));
            return layout;
        }

        /// <summary>Shows the backdrop of <paramref name="theme"/> behind the screen (null = the plain background color).</summary>
        public void SetTheme(VisualTheme theme)
        {
            Backdrop.gameObject.SetActive(theme != null);
            if (theme == null) return;
            Backdrop.sprite = ThemeArt.Backdrop(theme);
            var fitter = Backdrop.GetComponent<AspectRatioFitter>();
            var r = Backdrop.sprite.rect;
            fitter.aspectRatio = r.width / Mathf.Max(1f, r.height);
        }

        /// <summary>Round icon button in the top bar, on the left (back) or on the right (settings).</summary>
        public static Button TopBarButton(AppServices ctx, RectTransform topBar, string iconName, bool left)
        {
            var b = ctx.Ui.IconButton(left ? "LeftButton" : "RightButton", topBar, iconName);
            float x = left ? ctx.Style.screenPadding : -ctx.Style.screenPadding;
            UiFactory.Place((RectTransform)b.transform, left ? 0 : 1, 0.5f, new Vector2(ctx.Style.iconButtonSize, ctx.Style.iconButtonSize), new Vector2(x, 0));
            return b;
        }

        /// <summary>Title (light text, dark outline) centred in the top bar, leaving room for the two round buttons.</summary>
        public static TextMeshProUGUI TopBarTitle(AppServices ctx, RectTransform topBar, string key, int size)
        {
            var t = ctx.Ui.Title("Title", topBar, key, size);
            float side = ctx.Style.screenPadding + ctx.Style.iconButtonSize + 16;
            UiFactory.Stretch(t.rectTransform, side, 0, side, 0);
            UiFactory.AutoSize(t, size * 0.6f);
            return t;
        }

        /// <summary>A small cream pill with a dark label (status lines over a theme backdrop).</summary>
        public static TextMeshProUGUI Badge(AppServices ctx, Transform parent, string name, string key, int size, out Image pill)
        {
            pill = ctx.Ui.Image(name, parent, ctx.Style.circleSprite, ctx.Style.surface);
            var label = ctx.Ui.Label("Text", pill.transform, key, size, ctx.Style.text, TextAnchor.MiddleCenter, bold: true);
            UiFactory.Stretch(label.rectTransform, 28, 2, 28, 2);
            UiFactory.AutoSize(label, size * 0.6f);
            return label;
        }
    }
}
