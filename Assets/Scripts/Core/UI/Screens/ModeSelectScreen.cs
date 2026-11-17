using System.Collections;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// Story / Challenge choice for the selected minigame. In this Update both modes only show a placeholder message;
    /// later Updates open the Story map and the Challenge run from here.
    /// </summary>
    public sealed class ModeSelectScreen : UiScreen
    {
        public override string Id => ScreenIds.ModeSelect;

        AppServices _ctx;
        LocalizedText _title;
        Image _gameIcon;
        CanvasGroup _message;
        Coroutine _messageFade;

        public GameInfo Game { get; private set; }

        public static ModeSelectScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<ModeSelectScreen>(ctx, parent, "ModeSelectScreen");
            var screen = layout.Root.gameObject.AddComponent<ModeSelectScreen>();
            screen._ctx = ctx;
            var ui = ctx.Ui;
            var s = ctx.Style;

            ScreenLayout.TopBarButton(ctx, layout.TopBar, "back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            screen._title = ScreenLayout.TopBarTitle(ctx, layout.TopBar, null, s.headerSize).gameObject.AddComponent<LocalizedText>();

            // big game icon
            var iconRow = ui.Rect("GameIcon", layout.Body);
            UiFactory.Layout(iconRow, preferredHeight: 240);
            var circle = ui.Image("IconBg", iconRow, s.circleSprite, s.accent);
            UiFactory.Place(circle.rectTransform, 0.5f, 0.5f, new Vector2(220, 220), Vector2.zero);
            screen._gameIcon = ui.Icon("Icon", circle.transform, null, 140, s.onPrimary);
            UiFactory.Place(screen._gameIcon.rectTransform, 0.5f, 0.5f, new Vector2(140, 140), Vector2.zero);

            var choose = ui.Label("Choose", layout.Body, LocKeys.ModeTitle, s.bodySize, s.textMuted, TextAnchor.MiddleCenter);
            UiFactory.Layout(choose, preferredHeight: 72);

            CreateModeCard(ctx, layout.Body, "story", s.primary, LocKeys.ModeStory, LocKeys.ModeStoryDesc).onClick.AddListener(screen.ShowPlaceholder);
            CreateModeCard(ctx, layout.Body, "challenge", s.accent, LocKeys.ModeChallenge, LocKeys.ModeChallengeDesc).onClick.AddListener(screen.ShowPlaceholder);

            // placeholder message, hidden until a mode is tapped
            var msg = ui.Image("Message", layout.Body, s.cardSprite, s.badge);
            UiFactory.Layout(msg, preferredHeight: 200);
            var text = ui.Label("Text", msg.transform, LocKeys.ModePlaceholder, s.bodySize, s.badgeText, TextAnchor.MiddleCenter);
            UiFactory.Stretch(text.rectTransform, 40, 16, 40, 16);
            screen._message = msg.gameObject.AddComponent<CanvasGroup>();
            screen._message.alpha = 0f;
            return screen;
        }

        static Button CreateModeCard(AppServices ctx, Transform parent, string iconName, Color iconColor, string titleKey, string descKey)
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var card = ui.Button("Mode_" + iconName, parent, s.cardSprite, s.surface);
            UiFactory.Layout(card, preferredHeight: s.modeCardHeight);
            const float pad = 32;
            var circle = ui.Image("IconBg", card.transform, s.circleSprite, iconColor);
            UiFactory.Place(circle.rectTransform, 0, 0.5f, new Vector2(s.cardIconSize, s.cardIconSize), new Vector2(pad, 0));
            var icon = ui.Icon("Icon", circle.transform, iconName, s.cardIconSize * 0.6f, s.onPrimary);
            UiFactory.Place(icon.rectTransform, 0.5f, 0.5f, icon.rectTransform.sizeDelta, Vector2.zero);

            float left = pad + s.cardIconSize + pad;
            var title = ui.Label("Title", card.transform, titleKey, s.headerSize, s.text, TextAnchor.LowerLeft, bold: true);
            title.rectTransform.anchorMin = new Vector2(0, 0.55f);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.offsetMin = new Vector2(left, 0);
            title.rectTransform.offsetMax = new Vector2(-pad, -24);
            var desc = ui.Label("Desc", card.transform, descKey, s.smallSize, s.textMuted, TextAnchor.UpperLeft);
            desc.rectTransform.anchorMin = new Vector2(0, 0);
            desc.rectTransform.anchorMax = new Vector2(1, 0.55f);
            desc.rectTransform.offsetMin = new Vector2(left, 20);
            desc.rectTransform.offsetMax = new Vector2(-pad, -8);
            return card;
        }

        public void SetGame(GameInfo game)
        {
            Game = game;
            _title.Bind(_ctx.Loc, game.NameKey);
            _gameIcon.sprite = _ctx.Icons.Get(game.IconName);
        }

        public override void OnShow()
        {
            if (_messageFade != null) StopCoroutine(_messageFade);
            _message.alpha = 0f;
        }

        void ShowPlaceholder()
        {
            if (_messageFade != null) StopCoroutine(_messageFade);
            _messageFade = StartCoroutine(FadeIn(_message, _ctx.Style.messageFade));
        }

        static IEnumerator FadeIn(CanvasGroup group, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / seconds;
                yield return null;
            }
            group.alpha = 1f;
        }
    }
}
