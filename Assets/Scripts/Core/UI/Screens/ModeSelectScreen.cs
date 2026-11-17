using System.Collections;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// Story / Challenge choice for the selected minigame: the illustration of the game on top, then one cream Gemini window per mode
    /// (round icon, name, description). Story opens the chapter list (if the game has Story levels); Challenge (ErNa-007) resumes the
    /// run in progress or starts one (placeholder for games without a Challenge); under it the run or the record, and "New run".
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
        // ErNa-007: the run in progress or the record, "New run" (with a confirmation)
        GameObject _challengeInfoRow;
        TMPro.TextMeshProUGUI _challengeInfo;
        Button _newRun;
        DialogPanel _confirm;

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
            CreateModeCard(ctx, layout.Body, "icon_bolt", false, LocKeys.ModeChallenge, LocKeys.ModeChallengeDesc).onClick.AddListener(screen.OpenChallenge);

            screen._challengeInfo = ScreenLayout.Badge(ctx, layout.Body, "ChallengeInfo", null, s.smallSize, out var infoPill);
            UiFactory.Layout(infoPill, preferredHeight: 84);
            screen._challengeInfoRow = infoPill.gameObject;
            screen._newRun = ui.SecondaryButton("NewRun", layout.Body, LocKeys.ChallengeNewRun, out _);
            UiFactory.Layout(screen._newRun, preferredHeight: s.buttonHeight);
            screen._newRun.onClick.AddListener(screen.AskNewRun);
            screen._confirm = new DialogPanel(ctx, layout.Root, "ConfirmNewRun", LocKeys.ChallengeNewRun, 1, LocKeys.CommonNo, LocKeys.CommonYes);

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
            RefreshChallenge();
        }

        public override void OnShow()
        {
            if (_messageFade != null) StopCoroutine(_messageFade);
            _message.alpha = 0f;
            _confirm.Hide();
            RefreshChallenge();
        }

        /// <summary>Under the Challenge card: the run in progress (and "New run") or the record; nothing for games without a Challenge.</summary>
        void RefreshChallenge()
        {
            var module = Game != null ? _ctx.Games.FindModule(Game.Id) : null;
            bool has = ChallengeLauncher.HasChallenge(module);
            var run = has ? ChallengeLauncher.ActiveRun(_ctx, module) : null;
            var record = has ? _ctx.Data.challenge.Record(module.Info.Id) : null;
            if (run != null) _challengeInfo.text = string.Format(_ctx.Loc.Get(LocKeys.ModeChallengeRun), run.level, run.stars);
            else if (record != null) _challengeInfo.text = string.Format(_ctx.Loc.Get(LocKeys.ChallengeRecord), record.stars, record.levels);
            _challengeInfoRow.SetActive(run != null || record != null);
            _newRun.gameObject.SetActive(run != null);
        }

        /// <summary>Challenge: resumes the run in progress, or starts one; the placeholder for games without a Challenge.</summary>
        void OpenChallenge()
        {
            var module = Game != null ? _ctx.Games.FindModule(Game.Id) : null;
            if (!ChallengeLauncher.HasChallenge(module))
            {
                ShowPlaceholder();
                return;
            }
            ChallengeLauncher.Open(_ctx, module, forceNew: false);
        }

        /// <summary>One active run per game: a new one replaces it, after a confirmation.</summary>
        void AskNewRun()
        {
            var module = Game != null ? _ctx.Games.FindModule(Game.Id) : null;
            if (!ChallengeLauncher.HasChallenge(module)) return;
            _confirm.SetLine(0, _ctx.Loc.Get(LocKeys.ChallengeConfirmNew));
            _confirm.Show(() =>
            {
                _confirm.Hide();
                ChallengeLauncher.Open(_ctx, module, forceNew: true);
            }, () => _confirm.Hide());
        }

        /// <summary>Previews: the confirmation of a new run.</summary>
        public void ShowConfirmForPreview() => AskNewRun();

        public override bool HandleBack()
        {
            if (!_confirm.Visible) return false;
            _confirm.Hide();
            return true;
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
