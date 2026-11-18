using ResolverMaster.Core.Audio;
using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// The levels of the Story chapters, one page per chapter: a 3x3 grid of Gemini tiles over the backdrop of the chapter theme
    /// (cream = to play, green = solved with its stars, grey with the lock = locked; inside a chapter a level unlocks when the previous one
    /// is solved, <see cref="StoryProgress.IsLevelUnlocked"/>). ErNa-009: a swipe left or right moves to the next or previous chapter, like
    /// the Home cards (same <see cref="CardCarousel"/> logic: smooth drag, snap to a page, at most one page per flick), with a dot per
    /// chapter; locked chapters can be seen (their tiles are locked). Title and backdrop follow the chapter in the centre. Starting a Story
    /// level moves the pages to its chapter (<see cref="FollowLevel"/>), so Back from a level, also after "Next" into another chapter,
    /// lands on the right one. The same screen for every game (Core).
    /// </summary>
    public sealed class LevelSelectScreen : UiScreen
    {
        public override string Id => ScreenIds.LevelSelect;

        sealed class LevelButton
        {
            public Button Button;
            public Image Background;
            public TextMeshProUGUI Number;
            public Image Lock;
            public Image[] Stars;
        }

        sealed class Page
        {
            public RectTransform Rect;
            public TextMeshProUGUI Subtitle;
            public RectTransform Grid;
            public LayoutElement GridLayout;
            public readonly List<LevelButton> Buttons = new List<LevelButton>();
            public StoryChapter Chapter;
        }

        AppServices _ctx;
        ScreenLayout _layout;
        LocalizedText _title;
        RectTransform _pager;
        CarouselArrows _arrows;
        readonly List<Page> _pages = new List<Page>();
        CardCarousel _carousel = new CardCarousel(0, 1f);
        int _shownChapter = -1;
        bool _wasMoving;
        float _pageWidth;

        // drag in progress
        Vector2 _dragStartLocal;
        float _lastDragX, _velocity;

        public MinigameModule Module { get; private set; }
        /// <summary>The chapter in the centre (the one the page shows when at rest).</summary>
        public StoryChapter Chapter => Module != null && _pages.Count > 0 ? Module.Story.Chapter(_carousel.Target + 1) : null;
        /// <summary>The page logic (previews, tests of the view).</summary>
        public CardCarousel Pager => _carousel;
        public override string MusicGame => Module != null ? Module.Info.Id : null;

        public static LevelSelectScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<LevelSelectScreen>(ctx, parent, "LevelSelectScreen", bodyColumn: false);
            var screen = layout.Root.gameObject.AddComponent<LevelSelectScreen>();
            screen._ctx = ctx;
            screen._layout = layout;
            var ui = ctx.Ui;
            var s = ctx.Style;

            ScreenLayout.TopBarButton(ctx, layout.TopBar, "icon_back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            screen._title = ScreenLayout.TopBarTitle(ctx, layout.TopBar, null, s.headerSize).gameObject.AddComponent<LocalizedText>();

            // ErNa-010: round arrows at the bottom, left and right (previous / next chapter), instead of the dots
            screen._arrows = new CarouselArrows(ctx, layout.Body, () => screen.StepPage(-1), () => screen.StepPage(1));
            screen._arrows.Place(new Vector2(0, 0), new Vector2(s.screenPadding, s.screenPadding), new Vector2(1, 0), new Vector2(-s.screenPadding, s.screenPadding), s.iconButtonSize);

            // the pages: a drag anywhere on them moves them (the tiles are buttons, which take only taps)
            screen._pager = ui.Rect("Pages", layout.Body);
            UiFactory.Stretch(screen._pager, 0, s.screenPadding + s.iconButtonSize + 8, 0, 0);
            var hit = screen._pager.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            screen._pager.gameObject.AddComponent<RectMask2D>();   // the next page slides in from the edge of the screen
            screen._pager.gameObject.AddComponent<DragRelay>().Target = screen;
            return screen;
        }

        /// <summary>Shows chapter <paramref name="chapterNumber"/> (1-based) of <paramref name="module"/>. Call before pushing the screen.</summary>
        public void Open(MinigameModule module, int chapterNumber)
        {
            if (Module != module || _pages.Count != module.Story.Chapters.Count) Build(module);
            _carousel.JumpTo(chapterNumber - 1);
            _shownChapter = -1;
            ApplyPositions();
            Refresh();
        }

        /// <summary>A Story level of <paramref name="module"/> starts (GameScreen): the pages move to its chapter, for the way back.</summary>
        public void FollowLevel(MinigameModule module, int levelIndex)
        {
            if (module == null || Module != module) return;
            int chapter = module.Story.ChapterOf(levelIndex).Number;
            if (_carousel.Target == chapter - 1 && _carousel.Resting) return;
            _carousel.JumpTo(chapter - 1);
            _shownChapter = -1;
            ApplyPositions();
        }

        void Build(MinigameModule module)
        {
            Module = module;
            var book = module.Story;
            int count = book.Chapters.Count;
            while (_pages.Count < count) _pages.Add(CreatePage(_pages.Count));
            for (int i = 0; i < _pages.Count; i++)
            {
                bool used = i < count;
                _pages[i].Rect.gameObject.SetActive(used);
                if (!used) continue;
                var page = _pages[i];
                page.Chapter = book.Chapter(i + 1);
                int levels = page.Chapter.LevelCount;
                while (page.Buttons.Count < levels) page.Buttons.Add(CreateButton(page, page.Buttons.Count));
                for (int k = 0; k < page.Buttons.Count; k++) page.Buttons[k].Button.gameObject.SetActive(k < levels);
                var s = _ctx.Style;
                int rows = (levels + s.levelColumns - 1) / s.levelColumns;
                float h = rows * s.levelButtonSize.y + Mathf.Max(0, rows - 1) * s.levelGridSpacing;
                page.GridLayout.preferredHeight = h;
                page.GridLayout.minHeight = h;
            }
            _carousel = new CardCarousel(count, Mathf.Max(1f, _pageWidth));
        }

        Page CreatePage(int index)
        {
            var ui = _ctx.Ui;
            var s = _ctx.Style;
            var page = new Page();
            page.Rect = ui.Rect("Chapter_" + (index + 1), _pager);
            UiFactory.Stretch(page.Rect);
            ui.Column(page.Rect, s.spacing, new RectOffset((int)s.screenPadding, (int)s.screenPadding, 8, 0));

            page.Subtitle = ScreenLayout.Badge(_ctx, page.Rect, "Subtitle", null, s.bodySize, out var badge);
            UiFactory.Layout(badge, preferredHeight: 84);

            page.Grid = ui.Rect("Levels", page.Rect);
            var grid = page.Grid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = s.levelButtonSize;
            grid.spacing = new Vector2(s.levelGridSpacing, s.levelGridSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = s.levelColumns;
            grid.childAlignment = TextAnchor.UpperCenter;
            page.GridLayout = UiFactory.Layout(page.Grid, preferredHeight: 0);
            return page;
        }

        LevelButton CreateButton(Page page, int slot)
        {
            var ui = _ctx.Ui;
            var s = _ctx.Style;
            var b = new LevelButton();
            b.Button = ui.Pressable("Level_" + (slot + 1), page.Grid, s.tileLevel, Color.white);
            b.Background = (Image)b.Button.targetGraphic;
            b.Button.onClick.AddListener(() => Play(page, slot));

            b.Number = ui.Label("Number", b.Button.transform, null, s.levelNumberSize, s.text, TextAnchor.MiddleCenter, bold: true);
            var nrt = b.Number.rectTransform;
            nrt.anchorMin = new Vector2(0, 0.43f);
            nrt.anchorMax = new Vector2(1, 1);
            nrt.offsetMin = new Vector2(16, 0);
            nrt.offsetMax = new Vector2(-16, -18);

            b.Lock = ui.Icon("Lock", b.Button.transform, "icon_lock", s.levelLockSize);
            UiFactory.Place(b.Lock.rectTransform, 0.5f, 0.5f, new Vector2(s.levelLockSize, s.levelLockSize), new Vector2(0, 8));

            b.Stars = new Image[StarRating.MaxStars];
            float step = s.levelStarSize + 6;
            for (int i = 0; i < b.Stars.Length; i++)
            {
                b.Stars[i] = ui.Picture("Star" + (i + 1), b.Button.transform, s.starEmpty, s.levelStarSize);
                // the middle star sits a little higher, like on the win panel
                UiFactory.Place(b.Stars[i].rectTransform, 0.5f, 0, new Vector2(s.levelStarSize, s.levelStarSize),
                    new Vector2((i - 1) * step, 36 + s.levelStarSize / 2 + (i == 1 ? 8 : 0)));
                b.Stars[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            return b;
        }

        void Play(Page page, int slot)
        {
            if (_carousel.Dragging || page.Chapter == null || page.Chapter.Number - 1 != _carousel.Target) return;   // only the page at rest
            var game = _ctx.Screens.Get<GameScreen>(ScreenIds.Game);
            game.StartLevel(Module, page.Chapter.FirstLevel + slot);
            _ctx.Screens.Push(ScreenIds.Game);
        }

        void Refresh()
        {
            if (Module == null) return;
            var s = _ctx.Style;
            var book = Module.Story;
            var progress = _ctx.Data.progress;
            for (int p = 0; p < book.Chapters.Count && p < _pages.Count; p++)
            {
                var page = _pages[p];
                var chapter = page.Chapter;
                page.Subtitle.text = string.Format(_ctx.Loc.Get(LocKeys.LevelsSubtitle), chapter.Number,
                    StoryProgress.ChapterStars(progress, book, chapter), chapter.MaxStars);
                for (int i = 0; i < chapter.LevelCount; i++)
                {
                    var b = page.Buttons[i];
                    int level = chapter.FirstLevel + i;
                    bool unlocked = StoryProgress.IsLevelUnlocked(progress, book, level);   // false for every level of a locked chapter
                    int stars = StoryProgress.Stars(progress, book, level);
                    b.Button.interactable = unlocked;   // locked: the grey tile and the lock say it
                    b.Background.sprite = !unlocked ? s.tileLocked : stars > 0 ? s.tileDone : s.tileLevel;
                    b.Number.text = (level + 1).ToString();
                    b.Number.color = stars > 0 ? s.onPrimary : s.text;
                    _ctx.Ui.SetOutlined(b.Number, stars > 0);
                    b.Number.gameObject.SetActive(unlocked);
                    b.Lock.gameObject.SetActive(!unlocked);
                    for (int k = 0; k < b.Stars.Length; k++)
                    {
                        b.Stars[k].gameObject.SetActive(unlocked);
                        b.Stars[k].sprite = k < stars ? s.starFull : s.starEmpty;
                    }
                }
            }
        }

        public override void OnShow()
        {
            Refit();
            Refresh();
        }

        // ------------------------------------------------------------------ pages

        void Refit()
        {
            float w = _pager.rect.width;
            if (w <= 0f) return;
            _pageWidth = w;
            if (Module == null) return;
            int target = _carousel.Target;
            _carousel = new CardCarousel(Module.Story.Chapters.Count, w);
            _carousel.JumpTo(target);
            ApplyPositions();
        }

        /// <summary>Pages side by side; only the ones on screen are active (at rest: the centred one only).</summary>
        void ApplyPositions()
        {
            if (Module == null) return;
            int count = Module.Story.Chapters.Count;
            for (int i = 0; i < count && i < _pages.Count; i++)
            {
                float offset = _carousel.Offset(i);
                bool visible = Mathf.Abs(i - _carousel.Position) < 0.999f;
                var rt = _pages[i].Rect;
                if (rt.gameObject.activeSelf != visible) rt.gameObject.SetActive(visible);
                rt.anchoredPosition = new Vector2(offset, 0);
            }
            int centred = _carousel.Centered;
            if (centred != _shownChapter) ShowChapter(centred);
            _arrows.Refresh(_carousel.Target, count);
        }

        /// <summary>Title and backdrop follow the chapter in the centre.</summary>
        void ShowChapter(int index)
        {
            if (_shownChapter >= 0 && _shownChapter != index) GameAudio.Play(AudioEvents.UiSwipe);
            _shownChapter = index;
            var chapter = Module.Story.Chapter(index + 1);
            _title.Bind(_ctx.Loc, chapter.Theme.nameKey);
            _layout.SetTheme(_ctx.Themes != null ? _ctx.Themes.ForChapter(chapter) : null);
        }

        /// <summary>ErNa-010: an arrow: the previous or the next chapter page (the finger still scrolls).</summary>
        void StepPage(int delta)
        {
            if (Module == null) return;
            int target = Mathf.Clamp(_carousel.Target + delta, 0, Module.Story.Chapters.Count - 1);
            if (target != _carousel.Target) _carousel.MoveTo(target);
            _wasMoving = true;
        }

        void Update()
        {
            if (Module == null) return;
            bool moving = _carousel.Tick(Time.unscaledDeltaTime, _ctx.Style.homeSnapTime) || _carousel.Dragging;
            if (moving || _wasMoving) ApplyPositions();   // one more time when the snap ends: the side pages switch off
            _wasMoving = moving;
        }

        void LateUpdate()
        {
            if (!Mathf.Approximately(_pager.rect.width, _pageWidth)) Refit();
        }

        // ------------------------------------------------------------------ input

        /// <summary>On the pages: drags that start on a tile bubble up here (tiles are buttons, which take only taps).</summary>
        sealed class DragRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public LevelSelectScreen Target;
            public void OnBeginDrag(PointerEventData e) => Target.OnBeginDrag(e);
            public void OnDrag(PointerEventData e) => Target.OnDrag(e);
            public void OnEndDrag(PointerEventData e) => Target.OnEndDrag(e);
        }

        bool LocalPoint(PointerEventData e, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_pager, e.position, e.pressEventCamera, out local);

        void OnBeginDrag(PointerEventData e)
        {
            if (Module == null || !LocalPoint(e, out _dragStartLocal)) return;
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
    }
}
