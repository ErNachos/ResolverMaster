using ResolverMaster.Core.App;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>The common skeleton of every screen: full-screen background, safe area, top bar and a body column below it.</summary>
    public sealed class ScreenLayout
    {
        public RectTransform Root;
        public SafeArea SafeArea;
        public RectTransform TopBar;
        public RectTransform Body;

        public static ScreenLayout Create<T>(AppServices ctx, Transform parent, string name) where T : UiScreen
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

            var safe = ui.Rect("SafeArea", layout.Root);
            UiFactory.Stretch(safe);
            layout.SafeArea = safe.gameObject.AddComponent<SafeArea>();

            layout.TopBar = ui.Rect("TopBar", safe);
            UiFactory.TopStrip(layout.TopBar, s.topBarHeight);

            layout.Body = ui.Rect("Body", safe);
            UiFactory.Stretch(layout.Body, 0, 0, 0, s.topBarHeight);
            ui.Column(layout.Body, s.spacing, new RectOffset((int)s.screenPadding, (int)s.screenPadding, 8, (int)s.screenPadding));
            return layout;
        }

        /// <summary>Round icon button in the top bar, on the left (back) or on the right (settings).</summary>
        public static Button TopBarButton(AppServices ctx, RectTransform topBar, string iconName, bool left)
        {
            var b = ctx.Ui.IconButton(left ? "LeftButton" : "RightButton", topBar, iconName);
            float x = left ? ctx.Style.screenPadding : -ctx.Style.screenPadding;
            UiFactory.Place((RectTransform)b.transform, left ? 0 : 1, 0.5f, new Vector2(ctx.Style.iconButtonSize, ctx.Style.iconButtonSize), new Vector2(x, 0));
            return b;
        }

        /// <summary>Title centred in the top bar, leaving room for the two round buttons.</summary>
        public static Text TopBarTitle(AppServices ctx, RectTransform topBar, string key, int size)
        {
            var t = ctx.Ui.Label("Title", topBar, key, size, ctx.Style.text, TextAnchor.MiddleCenter, bold: true);
            float side = ctx.Style.screenPadding + ctx.Style.iconButtonSize + 16;
            UiFactory.Stretch(t.rectTransform, side, 0, side, 0);
            return t;
        }
    }
}
