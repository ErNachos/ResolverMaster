using System.Collections.Generic;
using ResolverMaster.Core.Ads;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// The common game screen of every minigame, over the backdrop of the chapter theme: top bar (back, level number, moves made /
    /// minimum), the puzzle area between the top bar and the optional main action button chosen by the game (constant margins; short
    /// messages appear in place of the button), bottom bar (undo, restart, help - disabled for now) and the win panel.
    /// The puzzle itself is a <see cref="PuzzleBoard"/> created once per minigame by its <see cref="MinigameModule"/>.
    /// ErNa-007, Challenge mode (<see cref="StartChallenge"/>): moves made / minimum / maximum (the counter turns red with 2 moves
    /// left), pieces stop turning at the maximum, 3 undos per run (the undo takes the move back), no restart and no light bulb, a
    /// failure ends the run (end panel), Back opens the pause; the run is saved at every move and at every level change.
    /// ErNa-008: on the end panel "Continue" (rewarded ad, once per run) plays the same level again from the start with 3 undos;
    /// "Change level" (rewarded ad, 0 moves); the daily challenge (5 fixed levels, no change, its own end panel).
    /// </summary>
    public sealed class GameScreen : UiScreen
    {
        public override string Id => ScreenIds.Game;

        /// <summary>ErNa-008: the placeholder icon of the buttons that show a rewarded ad.</summary>
        public const string AdIcon = "ad_video";

        AppServices _ctx;
        ScreenLayout _layout;
        RectTransform _topBar, _area, _bottomBar;
        TextMeshProUGUI _title, _moves;
        Button _undo, _restart, _help, _change;
        readonly Dictionary<string, PuzzleBoard> _boards = new Dictionary<string, PuzzleBoard>();
        WinPanel _win;
        Button _action;
        Image _actionIcon;
        LocalizedText _actionLabel;
        CanvasGroup _toast;
        TextMeshProUGUI _toastText;
        float _toastTime = -1f;
        bool _lastBusy, _lastAds;
        GameMode _mode = GameMode.Story;
        ChallengeRunState _run;
        readonly Dictionary<string, ChallengeQueue> _queues = new Dictionary<string, ChallengeQueue>();
        DialogPanel _pause, _end, _dailyEnd, _outOfMoves;
        Image _undoBadge;
        TextMeshProUGUI _undoCount;
        bool _lastNewRecord;

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
        public GameMode Mode => _mode;
        /// <summary>The Challenge run being played (null in Story).</summary>
        public ChallengeRunState Run => _run;
        public bool PauseVisible => _pause.Visible;
        public bool EndVisible => _end.Visible || _dailyEnd.Visible;
        /// <summary>TJ-001: the "Out of moves" panel (undo or end the run) is open.</summary>
        public bool OutOfMovesVisible => _outOfMoves.Visible;

        public static GameScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<GameScreen>(ctx, parent, "GameScreen", bodyColumn: false);
            var screen = layout.Root.gameObject.AddComponent<GameScreen>();
            screen._ctx = ctx;
            screen._layout = layout;
            var ui = ctx.Ui;
            var s = ctx.Style;

            // ---- top bar: back, "Level N", moves made / minimum (in a cream pill: readable on every backdrop) ----
            screen._topBar = layout.TopBar;
            ScreenLayout.TopBarButton(ctx, layout.TopBar, "icon_back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            float side = s.screenPadding + s.iconButtonSize + 16;
            screen._title = ui.Title("Title", layout.TopBar, null, s.headerSize, TextAnchor.LowerCenter);
            var trt = screen._title.rectTransform;
            trt.anchorMin = new Vector2(0, 0.42f);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(side, 0);
            trt.offsetMax = new Vector2(-side, -6);
            UiFactory.AutoSize(screen._title, s.bodySize);
            screen._moves = ScreenLayout.Badge(ctx, layout.TopBar, "Moves", null, s.smallSize, out var movesPill);
            UiFactory.Place(movesPill.rectTransform, 0.5f, 0, new Vector2(440, 60), new Vector2(0, 10));

            // ---- puzzle area between the top bar and the main action (or the bottom bar), constant margins ----
            screen._area = ui.Rect("PuzzleArea", layout.Body);
            UiFactory.Stretch(screen._area, s.puzzleMargin, s.bottomBarHeight + s.puzzleMargin, s.puzzleMargin, s.puzzleMargin);

            // ---- bottom bar: undo, restart, help ----
            screen._bottomBar = ui.Rect("BottomBar", layout.Body);
            screen._bottomBar.anchorMin = new Vector2(0, 0);
            screen._bottomBar.anchorMax = new Vector2(1, 0);
            screen._bottomBar.pivot = new Vector2(0.5f, 0);
            screen._bottomBar.sizeDelta = new Vector2(0, s.bottomBarHeight);
            screen._bottomBar.anchoredPosition = Vector2.zero;
            float dx = s.gameButtonSize + s.gameButtonGap;
            screen._undo = screen.BarButton("Undo", "icon_undo", -dx);
            screen._restart = screen.BarButton("Restart", "icon_restart", 0);
            screen._help = screen.BarButton("Help", "icon_help", dx);
            screen._undo.onClick.AddListener(screen.UndoPressed);
            // Challenge: undos left in the run, a small badge on the undo button
            screen._undoBadge = ui.Image("UndoBadge", screen._undo.transform, s.circleSprite, s.primary);
            UiFactory.Place(screen._undoBadge.rectTransform, 1, 1, new Vector2(64, 64), new Vector2(-8, -8));
            screen._undoBadge.gameObject.AddComponent<Decoration>();   // on the corner of the button on purpose
            screen._undoCount = ui.Title("Count", screen._undoBadge.transform, null, s.smallSize);
            UiFactory.Stretch(screen._undoCount.rectTransform, 0, 4, 0, 0);
            screen._undoBadge.gameObject.SetActive(false);
            screen._restart.onClick.AddListener(() => { if (screen.Board != null && !screen.Board.IsBusy) screen.Board.Restart(); });
            screen._help.onClick.AddListener(screen.RequestHint);   // ErNa-007: the light bulb (rewarded ad, then a hint)
            // ErNa-008, Challenge: "Change level" in the place of the light bulb (placeholder icon), with the ad badge on its corner
            screen._change = screen.BarButton("ChangeLevel", "change_level", dx);
            var changeIcon = screen._change.transform.Find("Icon")?.GetComponent<Image>();
            if (changeIcon != null)
            {
                changeIcon.color = s.icon;   // a white mask: tinted like the other placeholder icons
                changeIcon.rectTransform.sizeDelta *= 1.2f;   // the thin Material glyph next to the full Gemini drawings
            }
            var adBadge = ui.Image("AdBadge", screen._change.transform, s.circleSprite, s.primary);
            UiFactory.Place(adBadge.rectTransform, 1, 1, new Vector2(64, 64), new Vector2(-8, -8));
            adBadge.gameObject.AddComponent<Decoration>();   // on the corner of the button on purpose
            var adIcon = ui.MaskIcon("Icon", adBadge.transform, AdIcon, 40, s.onPrimary);
            UiFactory.Place(adIcon.rectTransform, 0.5f, 0.5f, new Vector2(40, 40), new Vector2(0, 2));
            screen._change.onClick.AddListener(screen.RequestChangeLevel);
            screen._change.gameObject.SetActive(false);

            // ---- optional main action, centred above the bottom bar; the short messages appear in its place ----
            var slot = new Vector2(s.mainActionWidth, s.mainActionHeight);
            var slotPos = new Vector2(0, s.bottomBarHeight + s.mainActionGap);
            screen._action = ui.PrimaryButton("MainAction", layout.Body, null, out var label);
            UiFactory.Place((RectTransform)screen._action.transform, 0.5f, 0, slot, slotPos);
            screen._action.onClick.AddListener(screen.RunMainAction);
            screen._actionIcon = ui.Icon("Icon", screen._action.transform, null, s.mainActionIconSize);
            UiFactory.Place(screen._actionIcon.rectTransform, 0, 0.5f, new Vector2(s.mainActionIconSize, s.mainActionIconSize), new Vector2(64, 6));
            label.fontSize = label.fontSizeMax = s.headerSize;
            UiFactory.Stretch(label.rectTransform, 64 + s.mainActionIconSize + 12, 14, 64, 4);
            screen._actionLabel = label.gameObject.AddComponent<LocalizedText>();
            screen._action.gameObject.SetActive(false);

            // the short (error) messages: same pill as the buttons, tinted red, white outlined text
            var toast = ui.Image("Message", layout.Body, s.buttonCream, s.toastColor, pill: true);
            UiFactory.Place(toast.rectTransform, 0.5f, 0, slot, slotPos);
            screen._toastText = ui.Title("Text", toast.transform, null, s.headerSize);
            screen._toastText.color = s.toastText;
            UiFactory.Stretch(screen._toastText.rectTransform, 40, 14, 40, 4);
            UiFactory.AutoSize(screen._toastText, s.bodySize);
            screen._toast = toast.gameObject.AddComponent<CanvasGroup>();
            screen._toast.blocksRaycasts = false;
            toast.gameObject.SetActive(false);

            screen._win = new WinPanel(ctx, layout.Root, screen.Retry, screen.Next);
            // ErNa-007: Challenge pause and end of the run; ErNa-008: "Continue" with the ad icon on the end panel
            screen._pause = new DialogPanel(ctx, layout.Root, "Pause", LocKeys.ChallengePause, 1, LocKeys.ChallengeHome, LocKeys.ChallengeResume);
            screen._end = new DialogPanel(ctx, layout.Root, "RunOver", LocKeys.ChallengeOver, 3, LocKeys.ChallengeHome, LocKeys.ChallengeNewRun,
                extraKey: LocKeys.ChallengeContinue, extraIcon: AdIcon);
            // ErNa-008: end of the daily challenge (result, stars out of 15, streak), "Continue" while the lifeline is unused
            screen._dailyEnd = new DialogPanel(ctx, layout.Root, "DailyOver", LocKeys.DailyTitle, 3, null, LocKeys.ChallengeHome,
                extraKey: LocKeys.ChallengeContinue, extraIcon: AdIcon);
            // TJ-001: out of moves in a game with no final check (Traffic jam): undo to free moves, or end the run
            screen._outOfMoves = new DialogPanel(ctx, layout.Root, "OutOfMoves", LocKeys.ChallengeOutOfMoves, 1, LocKeys.ChallengeEnd, LocKeys.ChallengeUndo);
            return screen;
        }

        Button BarButton(string name, string icon, float x)
        {
            var s = _ctx.Style;
            var b = _ctx.Ui.IconButton(name, _bottomBar, icon, s.gameButtonSize, s.gameIconSize);
            UiFactory.Place((RectTransform)b.transform, 0.5f, 0.5f, new Vector2(s.gameButtonSize, s.gameButtonSize), new Vector2(x, 0));
            return b;
        }

        void UseModule(MinigameModule module)
        {
            if (Module == module) return;
            if (Board != null) Board.gameObject.SetActive(false);
            Module = module;
            Board = GetBoard(module);
            Board.gameObject.SetActive(true);
            SetupMainAction();
        }

        /// <summary>Story or Challenge: the buttons each mode has (Challenge: no restart, no light bulb, undos left on the undo button).</summary>
        void SetMode(GameMode mode)
        {
            _mode = mode;
            bool challenge = mode == GameMode.Challenge;
            _restart.gameObject.SetActive(!challenge);
            _help.gameObject.SetActive(!challenge);
            _change.gameObject.SetActive(false);   // shown by StartChallenge for the runs that allow it
            _undoBadge.gameObject.SetActive(challenge);
            Board.RestartOnFailure = !challenge;
            if (!challenge)
            {
                _run = null;
                Board.SetMoveLimit(0);
            }
            _pause.Hide();
            _end.Hide();
            _dailyEnd.Hide();
            _outOfMoves.Hide();
        }

        /// <summary>Loads Story level <paramref name="index"/> (0-based, whole Story) of <paramref name="module"/>; creates its board the first time.</summary>
        public void StartLevel(MinigameModule module, int index)
        {
            UseModule(module);
            SetMode(GameMode.Story);
            LevelIndex = index;
            _win.Hide();
            HideMessage();
            var theme = _ctx.Themes != null ? _ctx.Themes.ForChapter(module.Story.ChapterOf(index)) : null;
            _layout.SetTheme(theme);
            Board.Theme = theme;
            Board.Load(new LevelRequest(GameMode.Story, index));
            Refresh();
        }

        PuzzleBoard GetBoard(MinigameModule module)
        {
            if (_boards.TryGetValue(module.Info.Id, out var board)) return board;
            board = module.CreateBoard(_ctx, _area);
            if (board == null) throw new System.InvalidOperationException($"minigame '{module.Info.Id}' has no board");
            board.Changed += () =>
            {
                if (board != Board) return;
                if (_mode == GameMode.Challenge) SyncRun();
                Refresh();
            };
            board.Completed += () => { if (board == Board) OnCompleted(); };
            board.Failed += () => { if (board == Board && _mode == GameMode.Challenge) OnRunLost(); };
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
            float bottom = s.bottomBarHeight + (info != null ? s.mainActionGap + s.mainActionHeight : 0) + s.puzzleMargin;
            UiFactory.Stretch(_area, s.puzzleMargin, bottom, s.puzzleMargin, s.puzzleMargin);
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
            bool busy = Board.IsBusy;
            bool playing = !session.IsSolved && !busy;
            if (_mode == GameMode.Challenge && _run != null)
            {
                _title.text = _run.IsDaily
                    ? string.Format(_ctx.Loc.Get(LocKeys.DailyLevel), Mathf.Min(_run.level, _run.LevelCount), _run.LevelCount)
                    : string.Format(_ctx.Loc.Get(LocKeys.ChallengeLevel), _run.level);
                _moves.text = string.Format(_ctx.Loc.Get(LocKeys.GameMovesMax), session.MovesMade, session.MinMoves, _run.maxMoves);
                // 2 moves left or fewer: the counter turns red (never a water color)
                _moves.color = _run.maxMoves - session.MovesMade <= 2 ? _ctx.Style.offMark : _ctx.Style.text;
                _undoCount.text = _run.undosLeft.ToString();
                playing &= !_run.over;
                UiFactory.SetInteractable(_undo, playing && session.CanUndo && _run.undosLeft > 0);
                UiFactory.SetInteractable(_change, RewardedAds.CanOffer(_ctx.Ads, playing && _run.CanChangeLevel && session.MovesMade == 0));
                UiFactory.SetInteractable(_action, playing);
                _end.SetExtraInteractable(RewardedAds.CanOffer(_ctx.Ads, _run.CanContinue));
                _dailyEnd.SetExtraInteractable(RewardedAds.CanOffer(_ctx.Ads, _run.CanContinue));
                _lastBusy = busy;
                _lastAds = _ctx.Ads != null && _ctx.Ads.IsAvailable;
                CheckOutOfMoves(session, busy);
                return;
            }
            _moves.color = _ctx.Style.text;
            _title.text = string.Format(_ctx.Loc.Get(LocKeys.GameLevel), LevelIndex + 1);
            _moves.text = string.Format(_ctx.Loc.Get(LocKeys.GameMoves), session.MovesMade, session.MinMoves);
            UiFactory.SetInteractable(_undo, playing && session.CanUndo);
            UiFactory.SetInteractable(_restart, playing && session.MovesMade > 0);
            UiFactory.SetInteractable(_action, playing);
            UiFactory.SetInteractable(_help, RewardedAds.CanOffer(_ctx.Ads, playing && Board.CanHint));
            _lastBusy = busy;
            _lastAds = _ctx.Ads != null && _ctx.Ads.IsAvailable;
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
            if (_mode == GameMode.Challenge && _run != null)
            {
                // Challenge: the stars add to the run, the next level comes (saved now: closing the app here keeps the progress)
                int earned = StarRating.Compute(session.MovesMade, session.MinMoves, _ctx.Puzzle.stars);
                int moves = session.MovesMade, min = session.MinMoves;
                _run.OnSolved(earned);
                if (_run.IsComplete) FinishRun();   // ErNa-008: the last level of the daily challenge: result and streak saved now
                else
                {
                    _ctx.Save();
                    QueueFor(Module, _run.IsDaily).Prefetch(_run.level, 0, _run.allowChange);
                }
                Refresh();
                _win.Show(earned, moves, min, true, animate, showRetry: false);
                return;
            }
            int stars = StarRating.Compute(session.MovesMade, session.MinMoves, _ctx.Puzzle.stars, Board.HintUsed);
            if (_ctx.Data.progress.SetBest(StoryProgress.Mode, Level, stars)) _ctx.Save();
            Refresh();
            _win.Show(stars, session.MovesMade, session.MinMoves, HasNext(), animate);
        }

        /// <summary>The next numbered level exists and is unlocked (the next chapter only once this one is complete).</summary>
        bool HasNext() =>
            LevelIndex + 1 < Module.StoryLevelCount && StoryProgress.IsLevelUnlocked(_ctx.Data.progress, Module.Story, LevelIndex + 1);

        // ------------------------------------------------------------------ Challenge (ErNa-007)

        /// <summary>The level queue of a game: one for the endless Challenge and one for the daily challenge.</summary>
        ChallengeQueue QueueFor(MinigameModule module, bool daily)
        {
            string key = module.Info.Id + (daily ? "#daily" : "");
            if (!_queues.TryGetValue(key, out var q)) _queues[key] = q = new ChallengeQueue(module.Challenge);
            return q;
        }

        /// <summary>Plays <paramref name="run"/> of <paramref name="module"/> from where it was saved (its level, moves and undos).</summary>
        public void StartChallenge(MinigameModule module, ChallengeRunState run)
        {
            UseModule(module);
            SetMode(GameMode.Challenge);
            _run = run;
            QueueFor(module, run.IsDaily).Reset(run.runSeed, run.IsDaily ? run.fixedSteps : null);
            _change.gameObject.SetActive(run.allowChange);
            LoadChallengeLevel(resume: true);
            if (run.lost) ShowEnd(run);   // ErNa-008: closed on the end panel: the choice (Continue or not) is still open
        }

        /// <summary>The current level of the run (generated and saved first if new), with its saved moves when resuming.</summary>
        void LoadChallengeLevel(bool resume)
        {
            var queue = QueueFor(Module, _run.IsDaily);
            if (!_run.HasLevel)
            {
                _run.SetLevel(queue.Get(_run.level, _run.changes));
                _ctx.Save();
            }
            queue.Prefetch(_run.level, _run.changes, _run.allowChange);   // this level, its next change and the next 2, off the main thread
            _win.Hide();
            HideMessage();
            var themes = _ctx.Themes != null ? _ctx.Themes.Catalog.Themes : null;
            var theme = themes != null && themes.Count > 0 ? themes[Mathf.Max(0, _run.step) % themes.Count] : null;   // a new look every step
            _layout.SetTheme(theme);
            Board.Theme = theme;
            Board.SetMoveLimit(_run.maxMoves);
            var history = new List<int>(_run.undoHistory);
            Board.Load(new LevelRequest(GameMode.Challenge, _run.level - 1, _run.levelSeed, _run.levelData, _run.minMoves));
            if (resume && history.Count > 0 && !Board.RestoreMoves(history))
                Debug.LogWarning("GameScreen: the saved Challenge moves do not fit the level; the level starts again.");
            SyncRun();
            Refresh();
        }

        /// <summary>The run follows the board (moves, undo stack, rotations) and is saved: at every move, undo and level load.</summary>
        void SyncRun()
        {
            if (_run == null || _run.over || Board == null) return;
            _run.moves = Board.Session.MovesMade;
            _run.undoHistory.Clear();
            _run.undoHistory.AddRange(Board.MoveHistory);
            _run.rotations = Board.CaptureState();
            _ctx.Save();
        }

        /// <summary>
        /// TJ-001, a game with no final check (<see cref="PuzzleBoard.LosesWhenOutOfMoves"/>): at the maximum without the solution the
        /// pieces are locked and "Out of moves" opens (undo, or end the run); with no undo left the run is lost at once.
        /// </summary>
        void CheckOutOfMoves(IPuzzleSession session, bool busy)
        {
            bool stuck = Board.LosesWhenOutOfMoves && _run.IsOutOfMoves(session.IsSolved) && session.MovesMade >= _run.maxMoves;
            if (!stuck)
            {
                _outOfMoves.Hide();
                return;
            }
            if (busy || _pause.Visible || EndVisible || _win.Visible) return;
            if (_run.OutOfMovesIsLost(session.IsSolved))
            {
                _outOfMoves.Hide();
                OnRunLost();
                return;
            }
            if (_outOfMoves.Visible) return;
            _outOfMoves.SetLine(0, string.Format(_ctx.Loc.Get(LocKeys.ChallengeOutOfMovesHint), _run.undosLeft));
            _outOfMoves.Show(() =>
            {
                // "Undo" (the green button): one move back, the run goes on
                _outOfMoves.Hide();
                UndoPressed();
            }, () =>
            {
                // "End": the run is lost as after any failure (the end panel offers "Continue" while the lifeline is unused)
                _outOfMoves.Hide();
                OnRunLost();
            });
        }

        void UndoPressed()
        {
            if (Board == null || Board.IsBusy) return;
            if (_mode == GameMode.Challenge)
            {
                if (_run == null || !_run.TryUndo(out _)) return;   // 3 per run; the move goes off the counter
            }
            Board.Undo();
            Refresh();
        }

        /// <summary>
        /// A failed opening stops the run. ErNa-008: with the lifeline unused the run stays saved and the end panel offers "Continue";
        /// otherwise the run is over at once (score to the record, run removed).
        /// </summary>
        void OnRunLost()
        {
            if (_run == null || _run.over || _run.lost) return;
            _run.OnFailed();
            _lastNewRecord = false;
            if (_run.over) FinishRun();
            else _ctx.Save();
            ShowEnd(_run);
        }

        /// <summary>
        /// The run is over for good: the score goes to the record and the run is removed from the save. ErNa-008: a daily challenge
        /// goes to today's result, best score and streak instead (not to the Challenge record).
        /// </summary>
        void FinishRun()
        {
            if (_run == null) return;
            var block = _ctx.Data.challenge;
            if (_run.IsDaily) DailyChallenge.Finish(block, _run);
            else
            {
                _run.Finish();
                _lastNewRecord = block.SubmitScore(Module.Info.Id, _run.stars, _run.levelsCleared);
                block.EndRun(Module.Info.Id);
            }
            _ctx.Save();
        }

        /// <summary>The end panel of the kind of run (endless Challenge or daily challenge).</summary>
        void ShowEnd(ChallengeRunState run)
        {
            if (run.IsDaily) ShowDailyOver(run);
            else ShowRunOver(run, _lastNewRecord);
        }

        /// <summary>
        /// ErNa-008, end of the daily challenge: completed or not, stars out of 15, streak and best streak; Home; "Continue" (rewarded
        /// ad) while the lifeline is unused.
        /// </summary>
        public void ShowDailyOver(ChallengeRunState run)
        {
            var loc = _ctx.Loc;
            var stats = _ctx.Data.challenge.Daily(Module.Info.Id);
            bool completed = run.IsComplete;
            _dailyEnd.SetLine(0, loc.Get(completed ? LocKeys.DailyCompleted : LocKeys.DailyFailed), completed ? _ctx.Style.primary : (Color?)null);
            _dailyEnd.SetLine(1, string.Format(loc.Get(LocKeys.DailyStars), run.stars, run.LevelCount * StarRating.MaxStars));
            _dailyEnd.SetLine(2, string.Format(loc.Get(LocKeys.DailyStreak), DailyChallenge.CurrentStreak(stats, _ctx.Today), stats?.bestStreak ?? 0));
            _dailyEnd.SetExtra(run.CanContinue, RewardedAds.CanOffer(_ctx.Ads, run.CanContinue), ContinueRun);
            Refresh();
            _dailyEnd.Show(GoHome, null);
        }

        /// <summary>
        /// End of the run: stars, levels cleared, record (highlighted when new), New run / Home; ErNa-008: "Continue" (rewarded ad)
        /// while the lifeline is unused (disabled without an ad). Leaving the panel without continuing ends the run.
        /// </summary>
        public void ShowRunOver(ChallengeRunState run, bool newRecord)
        {
            var loc = _ctx.Loc;
            var record = _ctx.Data.challenge.Record(Module.Info.Id);
            _end.SetLine(0, string.Format(loc.Get(LocKeys.ChallengeStars), run.stars));
            _end.SetLine(1, string.Format(loc.Get(LocKeys.ChallengeLevels), run.levelsCleared));
            _end.SetLine(2, newRecord ? loc.Get(LocKeys.ChallengeNewRecord)
                : string.Format(loc.Get(LocKeys.ChallengeRecord), record?.stars ?? 0, record?.levels ?? 0), newRecord ? _ctx.Style.primary : (Color?)null);
            _end.SetExtra(run.CanContinue, RewardedAds.CanOffer(_ctx.Ads, run.CanContinue), ContinueRun);
            Refresh();
            _end.Show(() =>
            {
                if (_run != null && !_run.over) FinishRun();
                ChallengeLauncher.Open(_ctx, Module, forceNew: true);
            }, GoHome);
        }

        /// <summary>
        /// ErNa-008, "Change level": a rewarded ad, then (if watched) another level of the same step (seed with one more change);
        /// only with 0 moves, as many times as wanted, never in the daily challenge.
        /// </summary>
        void RequestChangeLevel()
        {
            if (_run == null || Board == null || Board.IsBusy || Board.Session.MovesMade != 0) return;
            if (!RewardedAds.CanOffer(_ctx.Ads, _run.CanChangeLevel)) return;
            var run = _run;
            _ctx.Ads.Show(() =>
            {
                if (run != _run || Board.IsBusy || Board.Session.MovesMade != 0 || !run.ChangeLevel()) return;
                _ctx.Save();
                LoadChallengeLevel(resume: false);
            }, Refresh);
        }

        /// <summary>ErNa-008, the lifeline: a rewarded ad, then (if watched) the same level from the start, 0 moves, 3 undos.</summary>
        void ContinueRun()
        {
            if (_run == null || !RewardedAds.CanOffer(_ctx.Ads, _run.CanContinue)) return;
            var run = _run;
            _ctx.Ads.Show(() =>
            {
                if (run != _run || !run.Continue()) return;
                _ctx.Save();
                _end.Hide();
                _dailyEnd.Hide();
                LoadChallengeLevel(resume: false);
            }, Refresh);
        }

        void ShowPause()
        {
            if (_run != null)
                _pause.SetLine(0, _run.IsDaily
                    ? string.Format(_ctx.Loc.Get(LocKeys.DailySummary), _run.level, _run.LevelCount, _run.stars)
                    : string.Format(_ctx.Loc.Get(LocKeys.ChallengeSummary), _run.level, _run.stars));
            _pause.Show(() => _pause.Hide(), GoHome);
        }

        /// <summary>Back to Home (the run is already saved; a lost run left without "Continue" is over).</summary>
        void GoHome()
        {
            if ((_end.Visible || _dailyEnd.Visible) && _run != null && _run.lost && !_run.over) FinishRun();
            _pause.Hide();
            _end.Hide();
            _dailyEnd.Hide();
            _outOfMoves.Hide();
            _ctx.Save();
            _ctx.Screens.ShowRoot(ScreenIds.Home);
        }

        /// <summary>Challenge: Back opens (or closes) the pause; after the end of the run it goes Home.</summary>
        public override bool HandleBack()
        {
            if (_ctx.TestAds != null && _ctx.TestAds.Showing) return true;   // the ad answers with its own buttons
            if (_mode != GameMode.Challenge) return false;
            if (_end.Visible || _dailyEnd.Visible) GoHome();
            else if (_pause.Visible) _pause.Hide();
            else ShowPause();
            return true;
        }

        /// <summary>Previews: the pause panel.</summary>
        public void ShowPauseForPreview() => ShowPause();

        void Retry()
        {
            _win.Hide();
            HideMessage();
            Board.Load(new LevelRequest(GameMode.Story, LevelIndex));
            Refresh();
        }

        void Next()
        {
            if (_mode == GameMode.Challenge)
            {
                if (_run != null && _run.IsComplete)
                {
                    _win.Hide();
                    ShowDailyOver(_run);   // ErNa-008: the 5th daily level solved: the end panel
                }
                else LoadChallengeLevel(resume: false);
                return;
            }
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

        /// <summary>The light bulb: a rewarded ad, then (if watched) one hint. Disabled without an ad or without anything to fix.</summary>
        void RequestHint()
        {
            if (Board == null || Board.IsBusy || Board.Session.IsSolved || !RewardedAds.CanOffer(_ctx.Ads, Board.CanHint)) return;
            var board = Board;
            _ctx.Ads.Show(() =>
            {
                if (board == Board && !board.IsBusy && !board.Session.IsSolved) board.ApplyHint();
                Refresh();
            }, Refresh);
        }

        public override void OnShow() => Refresh();

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _win.Tick(dt);
            TickMessage(dt);
            if (Board != null && (Board.IsBusy != _lastBusy || (_ctx.Ads != null && _ctx.Ads.IsAvailable) != _lastAds)) Refresh();   // animations and ads start and end without a move
        }

        // ------------------------------------------------------------------ win panel

        /// <summary>Dim layer + card with the title, the stars (popping in one after the other), moves and the Retry / Next buttons.</summary>
        sealed class WinPanel
        {
            readonly AppServices _ctx;
            readonly CanvasGroup _group;
            readonly Image[] _stars = new Image[StarRating.MaxStars];
            readonly TextMeshProUGUI _moves;
            readonly LocalizedText _nextLabel;
            readonly Button _retry;
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

                // the green ribbon (never stretched: drawn at its own size) sits across the top edge of the window
                const float pad = 64, ribbonOver = 70, starsH = 200, movesH = 64, gap = 28, bottomPad = 72;
                var ribbonSize = s.ribbon != null ? s.ribbon.rect.size : new Vector2(462, 152);
                float top = ribbonSize.y - ribbonOver + gap;
                float height = top + starsH + gap + movesH + gap + 16 + s.buttonHeight + bottomPad;
                var panel = ui.Panel("WinPanel", dim.transform);
                UiFactory.Place(panel.rectTransform, 0.5f, 0.5f, new Vector2(s.winPanelWidth, height), new Vector2(0, -ribbonOver / 2f));

                var ribbon = ui.Picture("Ribbon", dim.transform, s.ribbon, 0);
                UiFactory.Place(ribbon.rectTransform, 0.5f, 0.5f, ribbonSize, new Vector2(0, height / 2f - ribbonOver / 2f + ribbonSize.y / 2f - ribbonOver));
                var title = ui.Title("Title", ribbon.transform, LocKeys.WinTitle, s.headerSize);
                // on the flat front band of the ribbon (between the folds)
                UiFactory.Stretch(title.rectTransform, ribbonSize.x * 0.17f, ribbonSize.y * 0.36f, ribbonSize.x * 0.17f, ribbonSize.y * 0.12f);
                UiFactory.AutoSize(title, s.smallSize);

                var starsRow = ui.Rect("Stars", panel.transform);
                Top(starsRow, top, starsH, pad);
                float step = s.winStarSize + 24;
                for (int i = 0; i < _stars.Length; i++)
                {
                    _stars[i] = ui.Picture("Star" + (i + 1), starsRow, s.starEmpty, s.winStarSize);
                    // the middle star sits a little higher
                    UiFactory.Place(_stars[i].rectTransform, 0.5f, 0.5f, new Vector2(s.winStarSize, s.winStarSize),
                        new Vector2((i - 1) * step, i == 1 ? 14 : -6));
                }

                _moves = ui.Label("Moves", panel.transform, null, s.bodySize, s.text, TextAnchor.MiddleCenter, bold: true);
                Top(_moves.rectTransform, top + starsH + gap, movesH, pad);

                var buttons = ui.Rect("Buttons", panel.transform);
                buttons.anchorMin = new Vector2(0, 0);
                buttons.anchorMax = new Vector2(1, 0);
                buttons.pivot = new Vector2(0.5f, 0);
                buttons.offsetMin = new Vector2(pad, bottomPad);
                buttons.offsetMax = new Vector2(-pad, bottomPad + s.buttonHeight);
                ui.Row(buttons, 32);
                _retry = ui.SecondaryButton("Retry", buttons, LocKeys.WinRetry, out _);
                _retry.onClick.AddListener(() => retry());
                ui.PrimaryButton("Next", buttons, LocKeys.WinNext, out var nextText).onClick.AddListener(() => next());
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

            public void Show(int stars, int moves, int minMoves, bool hasNext, bool animate, bool showRetry = true)
            {
                _retry.gameObject.SetActive(showRetry);
                var s = _ctx.Style;
                _earned = stars;
                _moves.text = string.Format(_ctx.Loc.Get(LocKeys.WinMoves), moves, minMoves);
                _nextLabel.Bind(_ctx.Loc, hasNext ? LocKeys.WinNext : LocKeys.WinLevels);
                for (int i = 0; i < _stars.Length; i++)
                {
                    _stars[i].sprite = i < stars ? s.starFull : s.starEmpty;
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
