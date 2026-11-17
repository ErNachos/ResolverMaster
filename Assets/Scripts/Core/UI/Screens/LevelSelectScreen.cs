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
    /// Story level choice of a minigame: a grid of buttons with the level number, the stars earned and a lock.
    /// Level n unlocks when level n-1 is solved (<see cref="Save.ProgressBlock.IsUnlocked"/>).
    /// </summary>
    public sealed class LevelSelectScreen : UiScreen
    {
        /// <summary>Only one chapter exists so far: every Story level belongs to chapter 1.</summary>
        public const int Chapter = 1;

        public override string Id => ScreenIds.LevelSelect;

        sealed class LevelButton
        {
            public Button Button;
            public Image Background;
            public Text Number;
            public Image Lock;
            public Image[] Stars;
        }

        AppServices _ctx;
        LocalizedText _title;
        Text _subtitle;
        RectTransform _grid;
        LayoutElement _gridLayout;
        readonly List<LevelButton> _buttons = new List<LevelButton>();

        public MinigameModule Module { get; private set; }

        public static LevelSelectScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<LevelSelectScreen>(ctx, parent, "LevelSelectScreen");
            var screen = layout.Root.gameObject.AddComponent<LevelSelectScreen>();
            screen._ctx = ctx;
            var ui = ctx.Ui;
            var s = ctx.Style;

            ScreenLayout.TopBarButton(ctx, layout.TopBar, "back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            screen._title = ScreenLayout.TopBarTitle(ctx, layout.TopBar, null, s.headerSize).gameObject.AddComponent<LocalizedText>();

            screen._subtitle = ui.Label("Subtitle", layout.Body, null, s.bodySize, s.textMuted, TextAnchor.MiddleCenter);
            UiFactory.Layout(screen._subtitle, preferredHeight: 72);

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

        /// <summary>Shows the Story levels of <paramref name="module"/>. Call before pushing the screen.</summary>
        public void Open(MinigameModule module)
        {
            Module = module;
            _title.Bind(_ctx.Loc, module.Info.NameKey);
            int count = module.StoryLevelCount;
            while (_buttons.Count < count) _buttons.Add(CreateButton(_buttons.Count));
            for (int i = 0; i < _buttons.Count; i++) _buttons[i].Button.gameObject.SetActive(i < count);
            var s = _ctx.Style;
            int rows = (count + s.levelColumns - 1) / s.levelColumns;
            float h = rows * s.levelButtonSize.y + Mathf.Max(0, rows - 1) * s.levelGridSpacing;
            _gridLayout.preferredHeight = h;
            _gridLayout.minHeight = h;
            Refresh();
        }

        LevelButton CreateButton(int index)
        {
            var ui = _ctx.Ui;
            var s = _ctx.Style;
            var b = new LevelButton();
            b.Button = ui.Button("Level_" + (index + 1), _grid, s.cardSprite, s.surface);
            b.Background = (Image)b.Button.targetGraphic;
            b.Button.onClick.AddListener(() => Play(index));

            b.Number = ui.Label("Number", b.Button.transform, null, s.headerSize, s.text, TextAnchor.MiddleCenter, bold: true);
            b.Number.text = (index + 1).ToString();
            var nrt = b.Number.rectTransform;
            nrt.anchorMin = new Vector2(0, 0.38f);
            nrt.anchorMax = new Vector2(1, 1);
            nrt.offsetMin = new Vector2(8, 0);
            nrt.offsetMax = new Vector2(-8, -12);

            b.Lock = ui.Icon("Lock", b.Button.transform, "lock", 72, s.textMuted);
            UiFactory.Place(b.Lock.rectTransform, 0.5f, 0.5f, new Vector2(72, 72), new Vector2(0, 0));

            b.Stars = new Image[StarRating.MaxStars];
            float step = s.levelStarSize + 4;
            for (int i = 0; i < b.Stars.Length; i++)
            {
                b.Stars[i] = ui.Icon("Star" + (i + 1), b.Button.transform, "star", s.levelStarSize, s.starOff);
                UiFactory.Place(b.Stars[i].rectTransform, 0.5f, 0, new Vector2(s.levelStarSize, s.levelStarSize),
                    new Vector2((i - 1) * step, 24 + s.levelStarSize / 2));
                b.Stars[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            return b;
        }

        void Play(int index)
        {
            var game = _ctx.Screens.Get<GameScreen>(ScreenIds.Game);
            game.StartLevel(Module, index);
            _ctx.Screens.Push(ScreenIds.Game);
        }

        void Refresh()
        {
            if (Module == null) return;
            var s = _ctx.Style;
            _subtitle.text = _ctx.Loc.Get(LocKeys.ModeStory) + "  ·  " + string.Format(_ctx.Loc.Get(LocKeys.LevelsChapter), Chapter);
            string game = Module.Info.Id;
            string mode = GameModeCodes.ToCode(GameMode.Story);
            int count = Module.StoryLevelCount;
            for (int i = 0; i < count; i++)
            {
                var b = _buttons[i];
                bool unlocked = _ctx.Data.progress.IsUnlocked(game, mode, i);
                int stars = _ctx.Data.progress.GetStars(game, mode, i);
                b.Button.interactable = unlocked;
                b.Background.color = unlocked ? s.surface : s.badge;
                b.Number.gameObject.SetActive(unlocked);
                b.Lock.gameObject.SetActive(!unlocked);
                for (int k = 0; k < b.Stars.Length; k++)
                {
                    b.Stars[k].gameObject.SetActive(unlocked);
                    b.Stars[k].color = k < stars ? s.starOn : s.starOff;
                }
            }
        }

        public override void OnShow() => Refresh();
    }
}
