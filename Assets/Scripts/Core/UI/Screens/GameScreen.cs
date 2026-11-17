using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// The common game screen of every minigame: top bar (back, level number, moves made / minimum), the puzzle area fitted to the
    /// screen and the safe area, the optional main action button chosen by the game (short messages appear in its place),
    /// bottom bar (undo, restart, help - disabled for now) and the win panel.
    /// The puzzle itself is a <see cref="PuzzleBoard"/> created once per minigame by its <see cref="MinigameModule"/>.
    /// </summary>
    public sealed class GameScreen : UiScreen
    {
        public override string Id => ScreenIds.Game;

        AppServices _ctx;
        RectTransform _topBar, _area, _bottomBar;
        Text _title, _moves;
        Button _undo, _restart, _help;
        readonly Dictionary<string, PuzzleBoard> _boards = new Dictionary<string, PuzzleBoard>();
        WinPanel _win;
        Button _action;
        Image _actionIcon;
        LocalizedText _actionLabel;
        CanvasGroup _toast;
        Text _toastText;
        float _toastTime = -1f;
        bool _lastBusy;

        public MinigameModule Module { get; private set; }
        public PuzzleBoard Board { get; private set; }
        /// <summary>0-based index in the whole Story (levels are numbered across chapters).</summary>
        public int LevelIndex { get; private set; }
        public LevelId Level => Module.Story.IdOf(LevelIndex);
        public RectTransform TopBarRect => _topBar;
        public RectTransform BottomBarRect => _bottomBar;
        public RectTransform PuzzleArea => _area;
        /// <summary>The main action button, null when the current board has none or a message is shown in its place.</summary>
        public RectTransform MainActionRect => _action.gameObject.activeSelf ? (RectTransform)_action.transform : null;
        /// <summary>The short message shown in place of the main action, null when hidden.</summary>
        public RectTransform MessageRect => _toast.gameObject.activeSelf ? (RectTransform)_toast.transform : null;
        public bool WinVisible => _win.Visible;

        public static GameScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<GameScreen>(ctx, parent, "GameScreen", bodyColumn: false);
            var screen = layout.Root.gameObject.AddComponent<GameScreen>();
            screen._ctx = ctx;
            var ui = ctx.Ui;
            var s = ctx.Style;

            // ---- top bar: back, "Level N", "Moves x · min y" ----
            screen._topBar = layout.TopBar;
            ScreenLayout.TopBarButton(ctx, layout.TopBar, "back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            float side = s.screenPadding + s.iconButtonSize + 16;
            screen._title = ui.Label("Title", layout.TopBar, null, s.headerSize, s.text, TextAnchor.LowerCenter, bold: true);
            var trt = screen._title.rectTransform;
            trt.anchorMin = new Vector2(0, 0.45f);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(side, 0);
            trt.offsetMax = new Vector2(-side, -8);
            screen._moves = ui.Label("Moves", layout.TopBar, null, s.smallSize, s.textMuted, TextAnchor.UpperCenter, bold: true);
            var mrt = screen._moves.rectTransform;
            mrt.anchorMin = new Vector2(0, 0);
            mrt.anchorMax = new Vector2(1, 0.45f);
            mrt.offsetMin = new Vector2(side, 8);
            mrt.offsetMax = new Vector2(-side, -4);

            // ---- puzzle area between the bars (and above the main action, when the board has one) ----
            screen._area = ui.Rect("PuzzleArea", layout.Body);
            UiFactory.Stretch(screen._area, s.screenPadding, s.bottomBarHeight, s.screenPadding, 0);

            // ---- bottom bar: undo, restart, help ----
            screen._bottomBar = ui.Rect("BottomBar", layout.Body);
            screen._bottomBar.anchorMin = new Vector2(0, 0);
            screen._bottomBar.anchorMax = new Vector2(1, 0);
            screen._bottomBar.pivot = new Vector2(0.5f, 0);
            screen._bottomBar.sizeDelta = new Vector2(0, s.bottomBarHeight);
            screen._bottomBar.anchoredPosition = Vector2.zero;
            float dx = s.gameButtonSize + s.gameButtonGap;
            screen._undo = screen.BarButton("Undo", "undo", -dx);
            screen._restart = screen.BarButton("Restart", "restart", 0);
            screen._help = screen.BarButton("Help", "help", dx);
            screen._undo.onClick.AddListener(() => { if (screen.Board != null && !screen.Board.IsBusy) screen.Board.Undo(); });
            screen._restart.onClick.AddListener(() => { if (screen.Board != null && !screen.Board.IsBusy) screen.Board.Restart(); });
            SetEnabled(screen._help, false);   // hints come in a later Update

            // ---- optional main action, centred above the bottom bar; the short messages appear in its place ----
            var slot = new Vector2(s.mainActionWidth, s.mainActionHeight);
            var slotPos = new Vector2(0, s.bottomBarHeight + s.mainActionGap);
            screen._action = ui.Button("MainAction", layout.Body, s.buttonSprite, s.primary);
            UiFactory.Place((RectTransform)screen._action.transform, 0.5f, 0, slot, slotPos);
            screen._action.onClick.AddListener(screen.RunMainAction);
            screen._actionIcon = ui.Icon("Icon", screen._action.transform, null, s.mainActionIconSize, s.onPrimary);
            UiFactory.Place(screen._actionIcon.rectTransform, 0, 0.5f, new Vector2(s.mainActionIconSize, s.mainActionIconSize), new Vector2(56, 0));
            var label = ui.Label("Label", screen._action.transform, null, s.headerSize, s.onPrimary, TextAnchor.MiddleCenter, bold: true);
            UiFactory.Stretch(label.rectTransform, 56 + s.mainActionIconSize + 16, 8, 56, 8);
            screen._actionLabel = label.gameObject.AddComponent<LocalizedText>();
            screen._action.gameObject.SetActive(false);

            var toast = ui.Image("Message", layout.Body, s.buttonSprite, s.toastColor);
            UiFactory.Place(toast.rectTransform, 0.5f, 0, slot, slotPos);
            screen._toastText = ui.Label("Text", toast.transform, null, s.headerSize, s.toastText, TextAnchor.MiddleCenter, bold: true);
            UiFactory.Stretch(screen._toastText.rectTransform, 32, 8, 32, 8);
            screen._toast = toast.gameObject.AddComponent<CanvasGroup>();
            screen._toast.blocksRaycasts = false;
            toast.gameObject.SetActive(false);

            screen._win = new WinPanel(ctx, layout.Root, screen.Retry, screen.Next);
            return screen;
        }

        Button BarButton(string name, string icon, float x)
        {
            var s = _ctx.Style;
            var b = _ctx.Ui.IconButton(name, _bottomBar, icon);
            UiFactory.Place((RectTransform)b.transform, 0.5f, 0.5f, new Vector2(s.gameButtonSize, s.gameButtonSize), new Vector2(x, 0));
            var iconImg = b.transform.Find("Icon").GetComponent<Image>();
            iconImg.rectTransform.sizeDelta = new Vector2(s.gameIconSize, s.gameIconSize);
            return b;
        }

        static void SetEnabled(Button b, bool on)
        {
            b.interactable = on;
            var icon = b.transform.Find("Icon").GetComponent<Image>();
            var c = icon.color;
            c.a = on ? 1f : 0.35f;
            icon.color = c;
        }

        /// <summary>Loads Story level <paramref name="index"/> (0-based, whole Story) of <paramref name="module"/>; creates its board the first time.</summary>
        public void StartLevel(MinigameModule module, int index)
        {
            if (Module != module)
            {
                if (Board != null) Board.gameObject.SetActive(false);
                Module = module;
                Board = GetBoard(module);
                Board.gameObject.SetActive(true);
                SetupMainAction();
            }
            LevelIndex = index;
            _win.Hide();
            HideMessage();
            Board.Load(new LevelRequest(GameMode.Story, index));
            Refresh();
        }

        PuzzleBoard GetBoard(MinigameModule module)
        {
            if (_boards.TryGetValue(module.Info.Id, out var board)) return board;
            board = module.CreateBoard(_ctx, _area);
            if (board == null) throw new System.InvalidOperationException($"minigame '{module.Info.Id}' has no board");
            board.Changed += () => { if (board == Board) Refresh(); };
            board.Completed += () => { if (board == Board) OnCompleted(); };
            board.MessageRequested += key => { if (board == Board) ShowMessage(_ctx.Loc.Get(key)); };
            _boards[module.Info.Id] = board;
            return board;
        }

        /// <summary>Shows the main action button only for boards that have one, and gives the puzzle area the room it leaves.</summary>
        void SetupMainAction()
        {
            var s = _ctx.Style;
            var info = Board.MainAction;
            _action.gameObject.SetActive(info != null);
            float bottom = s.bottomBarHeight + (info != null ? s.mainActionHeight + 2 * s.mainActionGap : 0);
            UiFactory.Stretch(_area, s.screenPadding, bottom, s.screenPadding, 0);
            if (info == null) return;
            _actionLabel.Bind(_ctx.Loc, info.LabelKey);
            _actionIcon.sprite = _ctx.Icons.Get(info.IconName);
        }

        void RunMainAction()
        {
            if (Board == null || Board.IsBusy || Board.Session.IsSolved) return;
            Board.RunMainAction();
            Refresh();
        }

        void Refresh()
        {
            if (Board == null) return;
            var session = Board.Session;
            _title.text = string.Format(_ctx.Loc.Get(LocKeys.GameLevel), LevelIndex + 1);
            _moves.text = string.Format(_ctx.Loc.Get(LocKeys.GameMoves), session.MovesMade, session.MinMoves);
            bool busy = Board.IsBusy;
            bool playing = !session.IsSolved && !busy;
            SetEnabled(_undo, playing && session.CanUndo);
            SetEnabled(_restart, playing && session.MovesMade > 0);
            _action.interactable = playing;
            _actionIcon.color = new Color(1, 1, 1, playing ? 1f : 0.6f);
            _lastBusy = busy;
        }

        /// <summary>Short message in place of the main action button (shown at once, held, then faded out).</summary>
        public void ShowMessage(string text)
        {
            _toastText.text = text;
            _toast.gameObject.SetActive(true);
            _toast.transform.SetAsLastSibling();
            _toast.alpha = 1f;
            _toastTime = 0f;
            _action.gameObject.SetActive(false);
        }

        void HideMessage()
        {
            _toast.gameObject.SetActive(false);
            _toastTime = -1f;
            if (Board != null) _action.gameObject.SetActive(Board.MainAction != null);
        }

        void TickMessage(float dt)
        {
            if (_toastTime < 0f) return;
            var s = _ctx.Style;
            _toastTime += dt;
            float fade = Mathf.Max(0.01f, s.toastFade);
            float t = _toastTime - s.toastHold;
            _toast.alpha = t <= 0 ? 1f : Mathf.Clamp01(1f - t / fade);
            if (t >= fade) HideMessage();
        }

        void OnCompleted() => ShowWin(animate: true);

        /// <summary>Saves the result and opens the win panel. <paramref name="animate"/> false: final state at once (previews).</summary>
        public void ShowWin(bool animate)
        {
            var session = Board.Session;
            int stars = StarRating.Compute(session.MovesMade, session.MinMoves, _ctx.Puzzle.stars);
            if (_ctx.Data.progress.SetBest(StoryProgress.Mode, Level, stars)) _ctx.Save();
            Refresh();
            _win.Show(stars, session.MovesMade, session.MinMoves, HasNext(), animate);
        }

        /// <summary>The next numbered level exists and is unlocked (the next chapter only once this one is complete).</summary>
        bool HasNext() =>
            LevelIndex + 1 < Module.StoryLevelCount && StoryProgress.IsLevelUnlocked(_ctx.Data.progress, Module.Story, LevelIndex + 1);

        void Retry()
        {
            _win.Hide();
            HideMessage();
            Board.Load(new LevelRequest(GameMode.Story, LevelIndex));
            Refresh();
        }

        void Next()
        {
            if (!HasNext())
            {
                _ctx.Screens.Back();
                return;
            }
            var chapter = Module.Story.ChapterOf(LevelIndex + 1);
            if (!chapter.Contains(LevelIndex))   // moving into the next chapter: Back then returns to its grid
                _ctx.Screens.Get<LevelSelectScreen>(ScreenIds.LevelSelect)?.Open(Module, chapter.Number);
            StartLevel(Module, LevelIndex + 1);
        }

        public override void OnShow() => Refresh();

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _win.Tick(dt);
            TickMessage(dt);
            if (Board != null && Board.IsBusy != _lastBusy) Refresh();   // animations start and end without a move
        }

        // ------------------------------------------------------------------ win panel

        /// <summary>Dim layer + card with the title, the stars (popping in one after the other), moves and the Retry / Next buttons.</summary>
        sealed class WinPanel
        {
            readonly AppServices _ctx;
            readonly CanvasGroup _group;
            readonly Image[] _stars = new Image[StarRating.MaxStars];
            readonly Text _moves;
            readonly LocalizedText _nextLabel;
            int _earned;
            float _time = -1f;

            public bool Visible => _group.gameObject.activeSelf;

            public WinPanel(AppServices ctx, RectTransform screenRoot, System.Action retry, System.Action next)
            {
                _ctx = ctx;
                var ui = ctx.Ui;
                var s = ctx.Style;

                var dim = ui.Image("WinOverlay", screenRoot, null, s.dim);
                dim.raycastTarget = true;   // blocks the board while the panel is open
                UiFactory.Stretch(dim.rectTransform);
                dim.gameObject.AddComponent<ModalOverlay>();
                _group = dim.gameObject.AddComponent<CanvasGroup>();

                const float pad = 56, titleH = 84, starsH = 180, movesH = 64, gap = 32;
                float height = pad + titleH + gap + starsH + gap + movesH + gap + 8 + s.buttonHeight + pad;
                var panel = ui.Image("WinPanel", dim.transform, s.panelSprite, s.surface);
                UiFactory.Place(panel.rectTransform, 0.5f, 0.5f, new Vector2(s.winPanelWidth, height), Vector2.zero);

                var title = ui.Label("Title", panel.transform, LocKeys.WinTitle, s.headerSize, s.text, TextAnchor.MiddleCenter, bold: true);
                Top(title.rectTransform, pad, titleH, pad);

                var starsRow = ui.Rect("Stars", panel.transform);
                Top(starsRow, pad + titleH + gap, starsH, pad);
                float step = s.winStarSize + 24;
                for (int i = 0; i < _stars.Length; i++)
                {
                    _stars[i] = ui.Icon("Star" + (i + 1), starsRow, "star", s.winStarSize, s.starOff);
                    // the middle star sits a little higher
                    UiFactory.Place(_stars[i].rectTransform, 0.5f, 0.5f, new Vector2(s.winStarSize, s.winStarSize),
                        new Vector2((i - 1) * step, i == 1 ? 14 : -6));
                }

                _moves = ui.Label("Moves", panel.transform, null, s.bodySize, s.textMuted, TextAnchor.MiddleCenter);
                Top(_moves.rectTransform, pad + titleH + gap + starsH + gap, movesH, pad);

                var buttons = ui.Rect("Buttons", panel.transform);
                buttons.anchorMin = new Vector2(0, 0);
                buttons.anchorMax = new Vector2(1, 0);
                buttons.pivot = new Vector2(0.5f, 0);
                buttons.offsetMin = new Vector2(pad, pad);
                buttons.offsetMax = new Vector2(-pad, pad + s.buttonHeight);
                ui.Row(buttons, 32);
                ui.TextButton("Retry", buttons, LocKeys.WinRetry, s.badge, s.text, out _).onClick.AddListener(() => retry());
                ui.TextButton("Next", buttons, LocKeys.WinNext, s.primary, s.onPrimary, out var nextText).onClick.AddListener(() => next());
                _nextLabel = nextText.GetComponent<LocalizedText>();

                dim.gameObject.SetActive(false);
            }

            static void Top(RectTransform rt, float top, float height, float sidePad)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.offsetMin = new Vector2(sidePad, -top - height);
                rt.offsetMax = new Vector2(-sidePad, -top);
            }

            public void Show(int stars, int moves, int minMoves, bool hasNext, bool animate)
            {
                var s = _ctx.Style;
                _earned = stars;
                _moves.text = string.Format(_ctx.Loc.Get(LocKeys.WinMoves), moves, minMoves);
                _nextLabel.Bind(_ctx.Loc, hasNext ? LocKeys.WinNext : LocKeys.WinLevels);
                for (int i = 0; i < _stars.Length; i++)
                {
                    _stars[i].color = i < stars ? s.starOn : s.starOff;
                    _stars[i].rectTransform.localScale = animate && i < stars ? Vector3.zero : Vector3.one;
                }
                _group.gameObject.SetActive(true);
                _group.transform.SetAsLastSibling();
                _group.alpha = animate ? 0f : 1f;
                _time = animate ? 0f : -1f;
            }

            public void Hide()
            {
                _group.gameObject.SetActive(false);
                _time = -1f;
            }

            /// <summary>Fade-in, then each earned star scales 0 -> 1.15 -> 1, one after the other.</summary>
            public void Tick(float dt)
            {
                if (_time < 0f || !Visible) return;
                var s = _ctx.Style;
                _time += dt;
                _group.alpha = Mathf.Clamp01(_time / Mathf.Max(0.01f, s.winPanelFade));
                bool done = _time >= s.winPanelFade;
                for (int i = 0; i < _earned; i++)
                {
                    float t = (_time - s.winPanelFade - i * s.starStagger) / Mathf.Max(0.01f, s.starPop);
                    float scale = t <= 0 ? 0 : t >= 1 ? 1 : (t < 0.6f ? Mathf.Lerp(0, 1.15f, t / 0.6f) : Mathf.Lerp(1.15f, 1f, (t - 0.6f) / 0.4f));
                    _stars[i].rectTransform.localScale = new Vector3(scale, scale, 1);
                    if (t < 1) done = false;
                }
                if (done) _time = -1f;
            }
        }
    }
}
