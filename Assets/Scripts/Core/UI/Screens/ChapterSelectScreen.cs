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
    /// Story of a minigame: one card per chapter with its theme icon, number, translated name, stars earned out of the maximum and a lock.
    /// A chapter unlocks when every level of the previous one is solved (<see cref="StoryProgress"/>). Tapping a card opens its 3x3 levels.
    /// </summary>
    public sealed class ChapterSelectScreen : UiScreen
    {
        public override string Id => ScreenIds.ChapterSelect;

        sealed class ChapterCard
        {
            public Button Button;
            public Image Background;
            public Image Circle;
            public Image Icon;
            public Image Lock;
            public Text Number;
            public LocalizedText Name;
            public Image Star;
            public Text Stars;
        }

        AppServices _ctx;
        LocalizedText _title;
        RectTransform _body;
        readonly List<ChapterCard> _cards = new List<ChapterCard>();

        public MinigameModule Module { get; private set; }

        public static ChapterSelectScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<ChapterSelectScreen>(ctx, parent, "ChapterSelectScreen");
            var screen = layout.Root.gameObject.AddComponent<ChapterSelectScreen>();
            screen._ctx = ctx;
            var s = ctx.Style;
            ScreenLayout.TopBarButton(ctx, layout.TopBar, "back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            screen._title = ScreenLayout.TopBarTitle(ctx, layout.TopBar, null, s.headerSize).gameObject.AddComponent<LocalizedText>();
            var subtitle = ctx.Ui.Label("Subtitle", layout.Body, LocKeys.ChaptersSubtitle, s.bodySize, s.textMuted, TextAnchor.MiddleCenter);
            UiFactory.Layout(subtitle, preferredHeight: 72);
            screen._body = layout.Body;
            return screen;
        }

        /// <summary>Shows the chapters of <paramref name="module"/>. Call before pushing the screen.</summary>
        public void Open(MinigameModule module)
        {
            Module = module;
            _title.Bind(_ctx.Loc, module.Info.NameKey);
            var chapters = module.Story.Chapters;
            while (_cards.Count < chapters.Count) _cards.Add(CreateCard(_cards.Count));
            for (int i = 0; i < _cards.Count; i++) _cards[i].Button.gameObject.SetActive(i < chapters.Count);
            Refresh();
        }

        ChapterCard CreateCard(int index)
        {
            var ui = _ctx.Ui;
            var s = _ctx.Style;
            const float pad = 32, starsWidth = 250;
            var c = new ChapterCard();
            c.Button = ui.Button("Chapter_" + (index + 1), _body, s.cardSprite, s.surface);
            c.Background = (Image)c.Button.targetGraphic;
            UiFactory.Layout(c.Button, preferredHeight: s.chapterCardHeight);
            c.Button.onClick.AddListener(() => OpenChapter(index + 1));

            c.Circle = ui.Image("IconBg", c.Button.transform, s.circleSprite, s.primary);
            UiFactory.Place(c.Circle.rectTransform, 0, 0.5f, new Vector2(s.chapterIconSize, s.chapterIconSize), new Vector2(pad, 0));
            c.Icon = ui.Icon("Icon", c.Circle.transform, null, s.chapterIconSize * 0.58f, s.onPrimary);
            UiFactory.Place(c.Icon.rectTransform, 0.5f, 0.5f, c.Icon.rectTransform.sizeDelta, Vector2.zero);
            c.Lock = ui.Icon("Lock", c.Circle.transform, "lock", s.chapterIconSize * 0.5f, s.textMuted);
            UiFactory.Place(c.Lock.rectTransform, 0.5f, 0.5f, c.Lock.rectTransform.sizeDelta, Vector2.zero);

            float left = pad + s.chapterIconSize + pad;
            c.Number = ui.Label("Number", c.Button.transform, null, s.smallSize, s.textMuted, TextAnchor.LowerLeft, bold: true);
            var nrt = c.Number.rectTransform;
            nrt.anchorMin = new Vector2(0, 0.56f);
            nrt.anchorMax = new Vector2(1, 1);
            nrt.offsetMin = new Vector2(left, 0);
            nrt.offsetMax = new Vector2(-starsWidth, -36);
            var name = ui.Label("Name", c.Button.transform, null, s.headerSize, s.text, TextAnchor.UpperLeft, bold: true);
            var art = name.rectTransform;
            art.anchorMin = new Vector2(0, 0);
            art.anchorMax = new Vector2(1, 0.56f);
            art.offsetMin = new Vector2(left, 24);
            art.offsetMax = new Vector2(-starsWidth, -4);
            c.Name = name.gameObject.AddComponent<LocalizedText>();

            c.Star = ui.Icon("Star", c.Button.transform, "star", s.chapterStarSize, s.starOn);
            UiFactory.Place(c.Star.rectTransform, 1, 0.5f, new Vector2(s.chapterStarSize, s.chapterStarSize), new Vector2(-(starsWidth - s.chapterStarSize - 8), 0));
            c.Stars = ui.Label("Stars", c.Button.transform, null, s.bodySize, s.text, TextAnchor.MiddleLeft, bold: true);
            UiFactory.Place(c.Stars.rectTransform, 1, 0.5f, new Vector2(starsWidth - s.chapterStarSize - 16 - pad, 72), new Vector2(-pad, 0));
            return c;
        }

        void OpenChapter(int number)
        {
            var levels = _ctx.Screens.Get<LevelSelectScreen>(ScreenIds.LevelSelect);
            levels.Open(Module, number);
            _ctx.Screens.Push(ScreenIds.LevelSelect);
        }

        void Refresh()
        {
            if (Module?.Story == null) return;
            var s = _ctx.Style;
            var book = Module.Story;
            var progress = _ctx.Data.progress;
            for (int i = 0; i < book.Chapters.Count; i++)
            {
                var chapter = book.Chapters[i];
                var c = _cards[i];
                bool unlocked = StoryProgress.IsChapterUnlocked(progress, book, chapter);
                c.Button.interactable = unlocked;
                c.Background.color = unlocked ? s.surface : s.badge;
                c.Circle.color = unlocked ? chapter.Theme.color : s.toggleOff;
                c.Icon.sprite = _ctx.Icons.Get(chapter.Theme.icon);
                c.Icon.gameObject.SetActive(unlocked);
                c.Lock.gameObject.SetActive(!unlocked);
                c.Number.text = string.Format(_ctx.Loc.Get(LocKeys.LevelsChapter), chapter.Number);
                c.Name.Bind(_ctx.Loc, chapter.Theme.nameKey);
                c.Name.GetComponent<Text>().color = unlocked ? s.text : s.textMuted;
                c.Stars.text = string.Format(_ctx.Loc.Get(LocKeys.ChaptersStars), StoryProgress.ChapterStars(progress, book, chapter), chapter.MaxStars);
                c.Star.color = unlocked ? s.starOn : s.starOff;
            }
        }

        public override void OnShow() => Refresh();
    }
}
