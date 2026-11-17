using ResolverMaster.Core.App;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>Home: app title, Settings button, one card per registered minigame (scrollable list).</summary>
    public sealed class HomeScreen : UiScreen
    {
        public override string Id => ScreenIds.Home;

        public static HomeScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<HomeScreen>(ctx, parent, "HomeScreen");
            var screen = layout.Root.gameObject.AddComponent<HomeScreen>();
            var ui = ctx.Ui;
            var s = ctx.Style;

            ScreenLayout.TopBarTitle(ctx, layout.TopBar, LocKeys.AppTitle, s.titleSize);
            ScreenLayout.TopBarButton(ctx, layout.TopBar, "settings", left: false)
                .onClick.AddListener(() => ctx.Screens.Push(ScreenIds.Settings));

            var choose = ui.Label("Choose", layout.Body, LocKeys.HomeChoose, s.bodySize, s.textMuted, TextAnchor.MiddleLeft);
            UiFactory.Layout(choose, preferredHeight: 72);

            // scrollable list of game cards
            var viewport = ui.Rect("List", layout.Body);
            UiFactory.Layout(viewport, flexibleHeight: 1);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;   // lets a drag anywhere in the list scroll it
            var content = ui.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            ui.Column(content, s.spacing);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            foreach (var game in ctx.Games.Games)
                CreateCard(ctx, content, game);
            return screen;
        }

        static void CreateCard(AppServices ctx, Transform parent, GameInfo game)
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var card = ui.Button("Card_" + game.Id, parent, s.cardSprite, s.surface);
            UiFactory.Layout(card, preferredHeight: s.cardHeight);
            card.onClick.AddListener(() =>
            {
                ctx.Screens.Get<ModeSelectScreen>(ScreenIds.ModeSelect).SetGame(game);
                ctx.Screens.Push(ScreenIds.ModeSelect);
            });

            const float pad = 32;
            var circle = ui.Image("IconBg", card.transform, s.circleSprite, game.Availability == GameAvailability.Available ? s.primary : s.accent);
            UiFactory.Place(circle.rectTransform, 0, 0.5f, new Vector2(s.cardIconSize, s.cardIconSize), new Vector2(pad, 0));
            var icon = ui.Icon("Icon", circle.transform, game.IconName, s.cardIconSize * 0.62f, s.onPrimary);
            UiFactory.Place(icon.rectTransform, 0.5f, 0.5f, icon.rectTransform.sizeDelta, Vector2.zero);

            float textLeft = pad + s.cardIconSize + pad;
            var name = ui.Label("Name", card.transform, game.NameKey, s.headerSize, s.text, TextAnchor.LowerLeft, bold: true);
            name.rectTransform.anchorMin = new Vector2(0, 0.5f);
            name.rectTransform.anchorMax = new Vector2(1, 1);
            name.rectTransform.offsetMin = new Vector2(textLeft, 4);
            name.rectTransform.offsetMax = new Vector2(-pad, -16);

            // status badge: [lock] Coming soon
            var badge = ui.Image("Badge", card.transform, s.circleSprite, s.badge);
            var brt = badge.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0, 1);
            brt.anchoredPosition = new Vector2(textLeft, -12);
            var row = ui.Row(brt, 12, new RectOffset(24, 28, 10, 10));
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            // fixed height: otherwise the sliced Image reports its sprite borders (128 px) as preferred height and the badge spills out
            UiFactory.Layout(badge, preferredHeight: s.badgeHeight);
            var fit = badge.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            bool soon = game.Availability == GameAvailability.ComingSoon;
            if (soon)
            {
                var lockIcon = ui.Icon("Lock", badge.transform, "lock", 36, s.badgeText);
                UiFactory.Layout(lockIcon, preferredHeight: 40, preferredWidth: 36);
            }
            var status = ui.Label("Status", badge.transform, soon ? LocKeys.GameComingSoon : LocKeys.GameAvailable, s.smallSize, s.badgeText, TextAnchor.MiddleLeft, bold: true);
            status.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.Layout(status, preferredHeight: 44);
        }
    }
}
