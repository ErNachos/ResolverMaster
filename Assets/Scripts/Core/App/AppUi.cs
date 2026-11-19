using ResolverMaster.Core.Ads;
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
            // rewarded ads (ErNa-007): the test panel above every screen (Editor), AdMob on the phone (ErNa-013), chosen in one place
            var adConfig = AdConfig.LoadOrDefault();
            ctx.TestAds = TestAdPanel.Create(ctx, root, adConfig.testAdSeconds);
            ctx.AdOverlay = AdOverlay.Create(ctx, root, adConfig.notAvailableSeconds);
            ctx.Ads ??= RewardedAds.Create(RewardedAds.CurrentKind, adConfig, ctx.TestAds, kind => DeviceAds(ctx, root, kind, adConfig));
            manager.ShowRoot(ScreenIds.Home);
            return manager;
        }

        /// <summary>ErNa-013: AdMob with the UMP consent (phone builds only); null without the plugin (no ads).</summary>
        static IRewardedAdService DeviceAds(AppServices ctx, RectTransform root, BuildKind kind, AdConfig config)
        {
#if RESOLVERMASTER_ADMOB
            var runtime = AdMobRuntime.Create(root.parent != null ? root.parent : root, kind, config, ctx.AdOverlay);
            ctx.Consent = runtime;
            return runtime.Controller;
#else
            Debug.LogWarning("AppUi: Google Mobile Ads plugin missing: no rewarded ads.");
            return null;
#endif
        }
    }
}
