using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.App
{
    /// <summary>Builds the whole UI (screen manager + every screen) under a canvas. Shared by the app start-up and the editor previews.</summary>
    public static class AppUi
    {
        /// <summary>Creates the screen-space canvas used on the phone (Canvas Scaler on the 1080x1920 reference, Expand).</summary>
        public static Canvas CreateCanvas(UiKitStyle style)
        {
            var go = new GameObject("Canvas", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = style.referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Builds every screen under <paramref name="root"/> (a full-canvas RectTransform) and shows Home.</summary>
        public static ScreenManager Build(AppServices ctx, RectTransform root)
        {
            ctx.Ui = new UiFactory(ctx.Style, ctx.Icons, ctx.Loc);
            var manager = root.gameObject.AddComponent<ScreenManager>();
            manager.Init(ctx.Style.screenFade);
            ctx.Screens = manager;
            manager.Register(HomeScreen.Create(ctx, root));
            manager.Register(ModeSelectScreen.Create(ctx, root));
            manager.Register(SettingsScreen.Create(ctx, root));
            manager.Register(ChapterSelectScreen.Create(ctx, root));
            manager.Register(LevelSelectScreen.Create(ctx, root));
            manager.Register(GameScreen.Create(ctx, root));
            manager.ShowRoot(ScreenIds.Home);
            return manager;
        }
    }
}
