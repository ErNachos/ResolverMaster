using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// Home: the title on top, one big card per registered minigame (colored rounded frame, illustration, translated name) that
    /// scrolls with the finger and stops centred, the side cards peeking in smaller and fainter, the position dots, the big PLAY
    /// button (opens the mode choice of the centred game; disabled for a "coming soon" game) and the round Settings button bottom left.
    /// The last card chosen is saved (save block <c>home</c>) and shown again at start-up. Scroll and snap logic: <see cref="CardCarousel"/>.
    /// </summary>
    public sealed class HomeScreen : UiScreen
    {
        public override string Id => ScreenIds.Home;

        sealed class Card
        {
            public GameInfo Game;
            public RectTransform Rect;
            public CanvasGroup Group;
            public Decoration Peek;
            public Button Button;
            // ErNa-007: Challenge access and record on the card (games with a Challenge only)
            public Button Challenge;
            public LocalizedText ChallengeLabel;
            public TMPro.TextMeshProUGUI Record;
            // ErNa-008: the daily challenge (state of today and current streak)
            public Button Daily;
            public TMPro.TextMeshProUGUI DailyState, DailyStreak;
        }

        AppServices _ctx;
        RectTransform _carouselRect;
        CardCarousel _carousel;
        readonly List<Card> _cards = new List<Card>();
        readonly List<Image> _dots = new List<Image>();
        Button _play;
        int _shownCentered = -1;
        float _velocity;
        Vector2 _dragStartLocal;
        float _lastDragX;
        float _cardHeight;

        /// <summary>The card in the centre (or the one the carousel is moving to).</summary>
        public int SelectedIndex => _carousel.Target;
        public GameInfo SelectedGame => _cards.Count > 0 ? _cards[_carousel.Target].Game : null;
        public RectTransform CarouselRect => _carouselRect;

        public static HomeScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<HomeScreen>(ctx, parent, "HomeScreen", bodyColumn: false);
            var screen = layout.Root.gameObject.AddComponent<HomeScreen>();
            screen._ctx = ctx;
            var ui = ctx.Ui;
            var s = ctx.Style;

            var title = ui.Title("Title", layout.TopBar, LocKeys.AppTitle, s.titleSize);
            UiFactory.Stretch(title.rectTransform, s.screenPadding, 0, s.screenPadding, 0);

            // ---- bottom strip: Settings (round yellow, gear, bottom left) and PLAY (centre) ----
            float strip = s.screenPadding + Mathf.Max(s.playButtonSize.y, s.homeSettingsSize);
            var settings = ui.IconButton("SettingsButton", layout.Body, "icon_settings", s.homeSettingsSize, 0, yellow: true);
            UiFactory.Place((RectTransform)settings.transform, 0, 0, new Vector2(s.homeSettingsSize, s.homeSettingsSize),
                new Vector2(s.screenPadding, s.screenPadding + (s.playButtonSize.y - s.homeSettingsSize) / 2f));
            settings.onClick.AddListener(() => ctx.Screens.Push(ScreenIds.Settings));

            screen._play = ui.PrimaryButton("PlayButton", layout.Body, LocKeys.HomePlay, out var playLabel);
            playLabel.fontSize = playLabel.fontSizeMax = s.playTextSize;
            UiFactory.Place((RectTransform)screen._play.transform, 0.5f, 0, s.playButtonSize, new Vector2(0, s.screenPadding));
            screen._play.onClick.AddListener(screen.Play);

            // ---- dots ----
            var dots = ui.Rect("Dots", layout.Body);
            dots.anchorMin = new Vector2(0, 0);
            dots.anchorMax = new Vector2(1, 0);
            dots.pivot = new Vector2(0.5f, 0);
            dots.sizeDelta = new Vector2(0, s.homeDotsHeight);
            dots.anchoredPosition = new Vector2(0, strip + s.spacing);

            // ---- carousel: takes the drags; cards are children ----
            screen._carouselRect = ui.Rect("Carousel", layout.Body);
            UiFactory.Stretch(screen._carouselRect, 0, strip + s.spacing + s.homeDotsHeight + 8, 0, 0);
            var hit = screen._carouselRect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;   // a drag anywhere in the band moves the cards
            screen._carouselRect.gameObject.AddComponent<CarouselDragRelay>().Target = screen;

            var games = ctx.Games.Games;
            screen._carousel = new CardCarousel(games.Count, s.homeCardStep);
            for (int i = 0; i < games.Count; i++)
            {
                screen._cards.Add(screen.CreateCard(games[i], i));
                var dot = ui.Image("Dot" + (i + 1), dots, s.circleSprite, s.surface);
                screen._dots.Add(dot);
            }
            screen._carousel.JumpTo(HomeSelection.IndexOf(games, ctx.Data.home.selectedGame));
            screen.RefreshChallenge();
            screen.Refit();
            return screen;
        }

        Card CreateCard(GameInfo game, int index)
        {
            var ui = _ctx.Ui;
            var s = _ctx.Style;
            bool soon = game.Availability == GameAvailability.ComingSoon;
            var c = new Card { Game = game };
            var frameColor = soon ? Color.Lerp(game.CardColor, s.disabledTint, s.comingSoonDim) : game.CardColor;
            c.Button = ui.CardButton("Card_" + game.Id, _carouselRect, frameColor);
            c.Rect = (RectTransform)c.Button.transform;
            UiFactory.Place(c.Rect, 0.5f, 0.5f, s.homeCardSize, Vector2.zero);
            c.Group = c.Button.gameObject.AddComponent<CanvasGroup>();
            c.Peek = c.Button.gameObject.AddComponent<Decoration>();
            c.Peek.mayLeaveScreen = true;
            c.Button.onClick.AddListener(() => OnCardTapped(index));

            // illustration area: a cream Gemini window with the image of the game, or a placeholder (the game icon)
            float inset = s.homeCardInset;
            var art = ui.Panel("Art", c.Rect);
            if (soon) art.color = s.disabledTint;
            UiFactory.Stretch(art.rectTransform, inset, s.homeCardNameHeight + s.cardDepth, inset, inset);
            var sprite = _ctx.Cards != null && !string.IsNullOrEmpty(game.CardImage) ? _ctx.Cards.Get(game.CardImage) : null;
            if (sprite != null)
            {
                var img = ui.Image("Illustration", art.transform, sprite, soon ? s.disabledTint : Color.white);
                img.preserveAspect = true;
                // inside the inner frame line of the window (a "coming soon" card, or one with a Challenge, keeps the bottom free)
                UiFactory.Stretch(img.rectTransform, 40, soon ? 210 : HasChallenge(game) ? DailyBottom + s.buttonHeight + 24 : 44, 40, 40);
            }
            else
            {
                // placeholder: the game icon (in the upper half when the lock and the badge take the lower one)
                var icon = ui.MaskIcon("Placeholder", art.transform, game.IconName, 300, s.icon);
                icon.color = new Color(s.icon.r, s.icon.g, s.icon.b, soon ? 0.35f : 0.8f);
                UiFactory.Place(icon.rectTransform, 0.5f, 0.5f, new Vector2(300, 300), new Vector2(0, soon ? 190 : s.cardDepth / 2f));
            }

            var name = ui.Title("Name", c.Rect, game.NameKey, s.homeCardNameSize);
            UiFactory.Stretch(name.rectTransform, inset, s.cardDepth + 8, inset, 0);
            name.rectTransform.anchorMax = new Vector2(1, 0);
            name.rectTransform.offsetMax = new Vector2(-inset, s.cardDepth + s.homeCardNameHeight);

            if (!soon && HasChallenge(game))
            {
                // ErNa-007: the Challenge record (trophy + stars) and the Challenge button ("Resume" when a run is in progress)
                var pill = ui.Image("Record", art.transform, s.circleSprite, s.surface);
                UiFactory.Place(pill.rectTransform, 0.5f, 0, new Vector2(220, 104), new Vector2(-170, 68));
                var trophy = ui.Icon("Trophy", pill.transform, "icon_trophy", 76);
                UiFactory.Place(trophy.rectTransform, 0, 0.5f, new Vector2(76, 76), new Vector2(22, 2));
                c.Record = ui.Label("Stars", pill.transform, null, s.bodySize, s.text, TextAnchor.MiddleCenter, bold: true);
                UiFactory.Stretch(c.Record.rectTransform, 22 + 76 + 6, 4, 18, 4);
                UiFactory.AutoSize(c.Record, s.smallSize);
                c.Challenge = ui.PrimaryButton("Challenge", art.transform, LocKeys.HomeChallenge, out var label);
                UiFactory.Place((RectTransform)c.Challenge.transform, 0.5f, 0, new Vector2(320, s.buttonHeight), new Vector2(120, 52));
                c.ChallengeLabel = label.GetComponent<LocalizedText>();
                UiFactory.AutoSize(label, s.smallSize);
                c.Challenge.onClick.AddListener(() => OpenChallenge(index));
                CreateDaily(c, art.transform, index);
            }

            if (soon)
            {
                // the "Coming soon" badge with the Gemini lock, under the illustration
                var badge = ui.Image("Badge", art.transform, s.circleSprite, s.surface);
                UiFactory.Place(badge.rectTransform, 0.5f, 0, new Vector2(540, 116), new Vector2(0, 66));
                var lockIcon = ui.Icon("Lock", badge.transform, "icon_lock", 84);
                UiFactory.Place(lockIcon.rectTransform, 0, 0.5f, new Vector2(84, 84), new Vector2(30, 0));
                var text = ui.Label("Status", badge.transform, LocKeys.GameComingSoon, s.bodySize, s.text, TextAnchor.MiddleCenter, bold: true);
                UiFactory.Stretch(text.rectTransform, 30 + 84 + 12, 4, 30, 4);
                UiFactory.AutoSize(text, s.smallSize);
            }
            return c;
        }

        /// <summary>Bottom of the daily challenge row in the illustration window (above the Challenge row).</summary>
        const float DailyBottom = 52 + 136 + 20;

        /// <summary>
        /// ErNa-008: the daily challenge row of a card: a cream pill with the calendar, "Daily challenge" and today's state (to do /
        /// in progress / done with N stars), the current streak on the right (placeholder flame). Disabled when today's is over.
        /// </summary>
        void CreateDaily(Card c, Transform art, int index)
        {
            var ui = _ctx.Ui;
            var s = _ctx.Style;
            // right end of the pill, from the edge: pad | count | flame | gap; the texts stop before it
            const float width = 560, iconSize = 84, pad = 26, countWidth = 60, flameSize = 56, gap = 12;
            const float streakWidth = pad + countWidth + flameSize + gap;
            c.Daily = ui.Pressable("Daily", art, s.buttonCream, Color.white, pill: true);
            UiFactory.Place((RectTransform)c.Daily.transform, 0.5f, 0, new Vector2(width, s.buttonHeight), new Vector2(0, DailyBottom));
            c.Daily.onClick.AddListener(() => OpenDaily(index));
            var calendar = ui.Icon("Calendar", c.Daily.transform, "icon_calendar", iconSize);
            UiFactory.Place(calendar.rectTransform, 0, 0.5f, new Vector2(iconSize, iconSize), new Vector2(pad, 5));
            float left = pad + iconSize + 12;
            var title = ui.Label("Title", c.Daily.transform, LocKeys.DailyTitle, s.smallSize, s.text, TextAnchor.LowerLeft, bold: true);
            title.rectTransform.anchorMin = new Vector2(0, 0.5f);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.offsetMin = new Vector2(left, 0);
            title.rectTransform.offsetMax = new Vector2(-streakWidth, -12);
            UiFactory.AutoSize(title, s.smallSize * 0.8f);
            c.DailyState = ui.Label("State", c.Daily.transform, null, s.smallSize, s.textMuted, TextAnchor.UpperLeft);
            c.DailyState.rectTransform.anchorMin = new Vector2(0, 0);
            c.DailyState.rectTransform.anchorMax = new Vector2(1, 0.5f);
            c.DailyState.rectTransform.offsetMin = new Vector2(left, 18);
            c.DailyState.rectTransform.offsetMax = new Vector2(-streakWidth, 0);
            UiFactory.AutoSize(c.DailyState, s.smallSize * 0.8f);
            // the streak: flame + number, on the right end of the pill
            var flame = ui.MaskIcon("Streak", c.Daily.transform, "streak", flameSize, s.offMark);
            UiFactory.Place(flame.rectTransform, 1, 0.5f, new Vector2(flameSize, flameSize), new Vector2(-(pad + countWidth), 5));
            c.DailyStreak = ui.Label("StreakCount", c.Daily.transform, null, s.bodySize, s.text, TextAnchor.MiddleLeft, bold: true);
            var srt = c.DailyStreak.rectTransform;
            srt.anchorMin = new Vector2(1, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(1, 0.5f);
            srt.offsetMin = new Vector2(-(pad + countWidth) + 4, 14);
            srt.offsetMax = new Vector2(-pad, -4);
            UiFactory.AutoSize(c.DailyStreak, s.smallSize);
        }

        // ------------------------------------------------------------------ input

        /// <summary>On the carousel band: drags that start on a card bubble up to it (cards are buttons, which take only taps).</summary>
        sealed class CarouselDragRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public HomeScreen Target;
            public void OnBeginDrag(PointerEventData e) => Target.OnBeginDrag(e);
            public void OnDrag(PointerEventData e) => Target.OnDrag(e);
            public void OnEndDrag(PointerEventData e) => Target.OnEndDrag(e);
        }

        bool LocalPoint(PointerEventData e, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_carouselRect, e.position, e.pressEventCamera, out local);

        void OnBeginDrag(PointerEventData e)
        {
            if (!LocalPoint(e, out _dragStartLocal)) return;
            _lastDragX = _dragStartLocal.x;
            _velocity = 0f;
            _carousel.BeginDrag();
        }

        void OnDrag(PointerEventData e)
        {
            if (!_carousel.Dragging || !LocalPoint(e, out var p)) return;
            float dt = Mathf.Max(0.001f, Time.unscaledDeltaTime);
            _velocity = Mathf.Lerp(_velocity, (p.x - _lastDragX) / dt, 0.5f);
            _lastDragX = p.x;
            _carousel.Drag(p.x - _dragStartLocal.x);
        }

        void OnEndDrag(PointerEventData e)
        {
            if (!_carousel.Dragging) return;
            _carousel.EndDrag(_velocity, _ctx.Style.homeFlickTime);
        }

        void OnCardTapped(int index)
        {
            if (index != _carousel.Target) _carousel.MoveTo(index);
            else Play();
        }

        bool HasChallenge(GameInfo game) => ChallengeLauncher.HasChallenge(_ctx.Games.FindModule(game.Id));

        /// <summary>The Challenge button of a card: the centred card opens its Challenge, a side card first comes to the centre.</summary>
        void OpenChallenge(int index)
        {
            if (index != _carousel.Target || !_carousel.Resting)
            {
                _carousel.MoveTo(index);
                return;
            }
            ChallengeLauncher.Open(_ctx, _ctx.Games.FindModule(_cards[index].Game.Id), forceNew: false);
        }

        /// <summary>ErNa-008: the daily challenge of a card (a side card first comes to the centre).</summary>
        void OpenDaily(int index)
        {
            if (index != _carousel.Target || !_carousel.Resting)
            {
                _carousel.MoveTo(index);
                return;
            }
            ChallengeLauncher.OpenDaily(_ctx, _ctx.Games.FindModule(_cards[index].Game.Id));
        }

        /// <summary>Record and "Resume" of every card with a Challenge (after a run they change).</summary>
        void RefreshChallenge()
        {
            foreach (var c in _cards)
            {
                if (c.Challenge == null) continue;
                var record = _ctx.Data.challenge.Record(c.Game.Id);
                c.Record.text = (record?.stars ?? 0).ToString();
                bool running = _ctx.Data.challenge.ActiveRun(c.Game.Id) != null;
                c.ChallengeLabel.Bind(_ctx.Loc, running ? LocKeys.HomeChallengeResume : LocKeys.HomeChallenge);
                // ErNa-008: today's daily challenge and the current streak
                var module = _ctx.Games.FindModule(c.Game.Id);
                var state = ChallengeLauncher.DailyStatusOf(_ctx, module, out int stars);
                c.DailyState.text = state == DailyStatus.ToDo ? _ctx.Loc.Get(LocKeys.DailyToDo)
                    : state == DailyStatus.InProgress ? _ctx.Loc.Get(LocKeys.DailyInProgress)
                    : string.Format(_ctx.Loc.Get(LocKeys.DailyDone), stars);
                c.DailyStreak.text = DailyChallenge.CurrentStreak(_ctx.Data.challenge.Daily(c.Game.Id), _ctx.Today).ToString();
                UiFactory.SetInteractable(c.Daily, state != DailyStatus.Done);
            }
        }

        void Play()
        {
            var game = SelectedGame;
            if (game == null || game.Availability != GameAvailability.Available || !_carousel.Resting) return;
            _ctx.Screens.Get<ModeSelectScreen>(ScreenIds.ModeSelect).SetGame(game);
            _ctx.Screens.Push(ScreenIds.ModeSelect);
        }

        // ------------------------------------------------------------------ layout

        /// <summary>Shows card <paramref name="index"/> centred at once (previews).</summary>
        public void ShowCard(int index)
        {
            _carousel.JumpTo(index);
            Refit();
        }

        /// <summary>Card height from the carousel band (taller phones get taller cards), then every card placed.</summary>
        public void Refit()
        {
            var s = _ctx.Style;
            float band = _carouselRect.rect.height;
            float h = band > 0 ? Mathf.Clamp(band - 2 * s.spacing, s.homeCardSize.y * 0.8f, s.homeCardSize.y * 1.25f) : s.homeCardSize.y;
            if (!Mathf.Approximately(h, _cardHeight))
            {
                _cardHeight = h;
                foreach (var c in _cards) c.Rect.sizeDelta = new Vector2(s.homeCardSize.x, h);
            }
            ApplyPositions();
        }

        void ApplyPositions()
        {
            var s = _ctx.Style;
            int centered = _carousel.Centered;
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                float d = _carousel.Distance(i);
                c.Rect.anchoredPosition = new Vector2(_carousel.Offset(i), 0);
                float scale = Mathf.Lerp(1f, s.homeSideScale, d);
                c.Rect.localScale = new Vector3(scale, scale, 1f);
                c.Group.alpha = Mathf.Lerp(1f, s.homeSideAlpha, d);
                c.Peek.enabled = d > 0.01f;
            }
            if (_cards.Count > 0) _cards[centered].Rect.SetAsLastSibling();
            if (centered != _shownCentered) ShowSelection(centered);
        }

        /// <summary>Dots and PLAY follow the centred card.</summary>
        void ShowSelection(int centered)
        {
            var s = _ctx.Style;
            _shownCentered = centered;
            float total = 0;
            for (int i = 0; i < _dots.Count; i++) total += (i == centered ? s.homeDotSize * 2f : s.homeDotSize) + (i > 0 ? s.homeDotGap : 0);
            float x = -total / 2f;
            for (int i = 0; i < _dots.Count; i++)
            {
                float w = i == centered ? s.homeDotSize * 2f : s.homeDotSize;
                UiFactory.Place(_dots[i].rectTransform, 0.5f, 0.5f, new Vector2(w, s.homeDotSize), new Vector2(x + w / 2f, 0));
                _dots[i].color = i == centered ? s.primary : s.surface;
                x += w + s.homeDotGap;
            }
            bool available = _cards.Count > 0 && _cards[centered].Game.Availability == GameAvailability.Available;
            UiFactory.SetInteractable(_play, available);
        }

        void SaveSelection()
        {
            var game = SelectedGame;
            if (game == null || _ctx.Data.home.selectedGame == game.Id) return;
            _ctx.Data.home.selectedGame = game.Id;
            _ctx.Save();
        }

        public override void OnShow()
        {
            RefreshChallenge();
            Refit();
        }

        void Update()
        {
            bool moving = _carousel.Tick(Time.unscaledDeltaTime, _ctx.Style.homeSnapTime);
            if (moving || _carousel.Dragging) ApplyPositions();
            if (_carousel.Resting) SaveSelection();
        }

        void LateUpdate()
        {
            if (!Mathf.Approximately(_carouselRect.rect.height, _lastBand))
            {
                _lastBand = _carouselRect.rect.height;
                Refit();
            }
        }

        float _lastBand = -1f;
    }
}
