using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// The levels of one Story chapter in a 3x3 grid of Gemini tiles over the backdrop of the chapter theme: cream = to play,
    /// green = solved (with its stars), grey with the lock = locked. Inside a chapter a level unlocks when the previous one is solved
    /// (<see cref="StoryProgress.IsLevelUnlocked"/>).
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

        AppServices _ctx;
        ScreenLayout _layout;
        LocalizedText _title;
        TextMeshProUGUI _subtitle;
        RectTransform _grid;
        LayoutElement _gridLayout;
        readonly List<LevelButton> _buttons = new List<LevelButton>();

        public MinigameModule Module { get; private set; }
        public StoryChapter Chapter { get; private set; }

        public static LevelSelectScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<LevelSelectScreen>(ctx, parent, "LevelSelectScreen");
            var screen = layout.Root.gameObject.AddComponent<LevelSelectScreen>();
            screen._ctx = ctx;
            screen._layout = layout;
            var ui = ctx.Ui;
            var s = ctx.Style;

            ScreenLayout.TopBarButton(ctx, layout.TopBar, "icon_back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            screen._title = ScreenLayout.TopBarTitle(ctx, layout.TopBar, null, s.headerSize).gameObject.AddComponent<LocalizedText>();

            screen._subtitle = ScreenLayout.Badge(ctx, layout.Body, "Subtitle", null, s.bodySize, out var badge);
            UiFactory.Layout(badge, preferredHeight: 84);

            screen._grid = ui.Rect("Levels", layout.Body);
            var grid = screen._grid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = s.levelButtonSize;
            grid.spacing = new Vector2(s.levelGridSpacing, s.levelGridSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = s.levelColumns;
            grid.childAlignment = TextAnchor.UpperCenter;
            screen._gridLayout = UiFactory.Layout(screen._grid, preferredHeight: 0);
            return screen;
        }

        /// <summary>Shows chapter <paramref name="chapterNumber"/> (1-based) of <paramref name="module"/>. Call before pushing the screen.</summary>
        public void Open(MinigameModule module, int chapterNumber)
        {
            Module = module;
            Chapter = module.Story.Chapter(chapterNumber);
            _title.Bind(_ctx.Loc, Chapter.Theme.nameKey);
            _layout.SetTheme(_ctx.Themes != null ? _ctx.Themes.ForChapter(Chapter) : null);
            int count = Chapter.LevelCount;
            while (_buttons.Count < count) _buttons.Add(CreateButton(_buttons.Count));
            for (int i = 0; i < _buttons.Count; i++) _buttons[i].Button.gameObject.SetActive(i < count);
            var s = _ctx.Style;
            int rows = (count + s.levelColumns - 1) / s.levelColumns;
            float h = rows * s.levelButtonSize.y + Mathf.Max(0, rows - 1) * s.levelGridSpacing;
            _gridLayout.preferredHeight = h;
            _gridLayout.minHeight = h;
            Refresh();
        }

        LevelButton CreateButton(int slot)
        {
            var ui = _ctx.Ui;
            var s = _ctx.Style;
            var b = new LevelButton();
            b.Button = ui.Pressable("Level_" + (slot + 1), _grid, s.tileLevel, Color.white);
            b.Background = (Image)b.Button.targetGraphic;
            b.Button.onClick.AddListener(() => Play(slot));

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

        void Play(int slot)
        {
            var game = _ctx.Screens.Get<GameScreen>(ScreenIds.Game);
            game.StartLevel(Module, Chapter.FirstLevel + slot);
            _ctx.Screens.Push(ScreenIds.Game);
        }

        void Refresh()
        {
            if (Module == null || Chapter == null) return;
            var s = _ctx.Style;
            var book = Module.Story;
            var progress = _ctx.Data.progress;
            _subtitle.text = string.Format(_ctx.Loc.Get(LocKeys.LevelsSubtitle), Chapter.Number,
                StoryProgress.ChapterStars(progress, book, Chapter), Chapter.MaxStars);
            for (int i = 0; i < Chapter.LevelCount; i++)
            {
                var b = _buttons[i];
                int level = Chapter.FirstLevel + i;
                bool unlocked = StoryProgress.IsLevelUnlocked(progress, book, level);
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

        public override void OnShow() => Refresh();
    }
}
