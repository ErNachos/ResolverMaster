using System.Collections;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// Story / Challenge choice for the selected minigame: the illustration of the game on top, then one cream Gemini window per mode
    /// (round icon, name, description). Story opens the chapter list (if the game has Story levels); Challenge still shows a
    /// placeholder message.
    /// </summary>
    public sealed class ModeSelectScreen : UiScreen
    {
        public override string Id => ScreenIds.ModeSelect;

        const float ArtHeight = 380;

        AppServices _ctx;
        LocalizedText _title;
        Image _gameArt, _gameIcon;
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

            ScreenLayout.TopBarButton(ctx, layout.TopBar, "icon_back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            screen._title = ScreenLayout.TopBarTitle(ctx, layout.TopBar, null, s.headerSize).gameObject.AddComponent<LocalizedText>();

            // the game illustration (Home card image), or its icon when the image is missing
            var artRow = ui.Rect("GameArt", layout.Body);
            UiFactory.Layout(artRow, preferredHeight: ArtHeight);
            screen._gameArt = ui.Image("Illustration", artRow, null, Color.white);
            screen._gameArt.preserveAspect = true;
            UiFactory.Stretch(screen._gameArt.rectTransform);
            screen._gameIcon = ui.MaskIcon("Icon", artRow, null, 220, s.icon);
            UiFactory.Place(screen._gameIcon.rectTransform, 0.5f, 0.5f, new Vector2(220, 220), Vector2.zero);

            ScreenLayout.Badge(ctx, layout.Body, "Choose", LocKeys.ModeTitle, s.bodySize, out var chooseBadge);
            UiFactory.Layout(chooseBadge, preferredHeight: 84);

            CreateModeCard(ctx, layout.Body, "story", true, LocKeys.ModeStory, LocKeys.ModeStoryDesc).onClick.AddListener(screen.OpenStory);
            CreateModeCard(ctx, layout.Body, "icon_bolt", false, LocKeys.ModeChallenge, LocKeys.ModeChallengeDesc).onClick.AddListener(screen.ShowPlaceholder);

            // placeholder message, hidden until a mode is tapped
            var msg = ui.Panel("Message", layout.Body);
            UiFactory.Layout(msg, preferredHeight: 200);
            var text = ui.Label("Text", msg.transform, LocKeys.ModePlaceholder, s.bodySize, s.badgeText, TextAnchor.MiddleCenter);
            UiFactory.Stretch(text.rectTransform, 48, 30, 48, 24);
            screen._message = msg.gameObject.AddComponent<CanvasGroup>();
            screen._message.alpha = 0f;
            return screen;
        }

        static Button CreateModeCard(AppServices ctx, Transform parent, string iconName, bool story, string titleKey, string descKey)
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var card = ui.PanelButton("Mode_" + (story ? "story" : "challenge"), parent);
            UiFactory.Layout(card, preferredHeight: s.modeCardHeight);
            const float pad = 44;
            var round = ui.Image("IconBg", card.transform, story ? s.roundYellow : s.roundCream, Color.white);
            round.preserveAspect = true;
            UiFactory.Place(round.rectTransform, 0, 0.5f, new Vector2(s.cardIconSize, s.cardIconSize), new Vector2(pad, 4));
            var icon = story
                ? ui.MaskIcon("Icon", round.transform, iconName, s.cardIconSize * 0.56f, s.icon)
                : ui.Icon("Icon", round.transform, iconName, s.cardIconSize * 0.62f);
            UiFactory.Place(icon.rectTransform, 0.5f, 0.5f, icon.rectTransform.sizeDelta, new Vector2(0, 4));

            float left = pad + s.cardIconSize + 36;
            var title = ui.Label("Title", card.transform, titleKey, s.headerSize, s.text, TextAnchor.LowerLeft, bold: true);
            title.rectTransform.anchorMin = new Vector2(0, 0.52f);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.offsetMin = new Vector2(left, 0);
            title.rectTransform.offsetMax = new Vector2(-pad, -40);
            var desc = ui.Label("Desc", card.transform, descKey, s.smallSize, s.textMuted, TextAnchor.UpperLeft);
            desc.rectTransform.anchorMin = new Vector2(0, 0);
            desc.rectTransform.anchorMax = new Vector2(1, 0.52f);
            desc.rectTransform.offsetMin = new Vector2(left, 44);
            desc.rectTransform.offsetMax = new Vector2(-pad, -8);
            return card;
        }

        public void SetGame(GameInfo game)
        {
            Game = game;
            _title.Bind(_ctx.Loc, game.NameKey);
            var art = _ctx.Cards != null && !string.IsNullOrEmpty(game.CardImage) ? _ctx.Cards.Get(game.CardImage) : null;
            _gameArt.sprite = art;
            _gameArt.gameObject.SetActive(art != null);
            _gameIcon.sprite = _ctx.Icons.Get(game.IconName);
            _gameIcon.gameObject.SetActive(art == null);
        }

        public override void OnShow()
        {
            if (_messageFade != null) StopCoroutine(_messageFade);
            _message.alpha = 0f;
        }

        /// <summary>Story: the chapter list when the minigame has saved Story levels, otherwise the placeholder message.</summary>
        void OpenStory()
        {
            var module = Game != null ? _ctx.Games.FindModule(Game.Id) : null;
            if (module == null || module.StoryLevelCount == 0)
            {
                ShowPlaceholder();
                return;
            }
            _ctx.Screens.Get<ChapterSelectScreen>(ScreenIds.ChapterSelect).Open(module);
            _ctx.Screens.Push(ScreenIds.ChapterSelect);
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
