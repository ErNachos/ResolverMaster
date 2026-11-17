using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI.Themes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// Story of a minigame: one yellow Gemini banner per chapter with a thumbnail of its theme (a cell slab of the theme with the chapter
    /// icon), "Chapter N · theme", the translated name, the stars earned out of the maximum and a lock. A chapter unlocks when every
    /// level of the previous one is solved (<see cref="StoryProgress"/>); a locked banner is desaturated. Tapping a card opens its 3x3 levels.
    /// The banners are in a vertical scroll list (ErNa-006: 9 chapters).
    /// </summary>
    public sealed class ChapterSelectScreen : UiScreen
    {
        public override string Id => ScreenIds.ChapterSelect;

        sealed class ChapterCard
        {
            public Button Button;
            public Image Thumb;
            public Image Icon;
            public Image Lock;
            public TextMeshProUGUI Number;
            public LocalizedText Name;
            public Image Star;
            public TextMeshProUGUI Stars;
        }

        AppServices _ctx;
        LocalizedText _title;
        RectTransform _body;
        ScrollRect _scroll;
        readonly List<ChapterCard> _cards = new List<ChapterCard>();

        public MinigameModule Module { get; private set; }

        public static ChapterSelectScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<ChapterSelectScreen>(ctx, parent, "ChapterSelectScreen");
            var screen = layout.Root.gameObject.AddComponent<ChapterSelectScreen>();
            screen._ctx = ctx;
            var s = ctx.Style;
            ScreenLayout.TopBarButton(ctx, layout.TopBar, "icon_back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            screen._title = ScreenLayout.TopBarTitle(ctx, layout.TopBar, null, s.headerSize).gameObject.AddComponent<LocalizedText>();
            ScreenLayout.Badge(ctx, layout.Body, "Subtitle", LocKeys.ChaptersSubtitle, s.bodySize, out var badge);
            UiFactory.Layout(badge, preferredHeight: 84);
            // ErNa-006: 9 chapters do not fit the screen: the banners scroll
            screen._scroll = ctx.Ui.ScrollList("Chapters", layout.Body, s.spacing, out screen._body, bottomPadding: 16);
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
            _body.anchoredPosition = Vector2.zero;   // from the first chapter
            _scroll.velocity = Vector2.zero;
            Refresh();
        }

        ChapterCard CreateCard(int index)
        {
            var ui = _ctx.Ui;
            var s = _ctx.Style;
            const float pad = 36, starsWidth = 260;
            var c = new ChapterCard();
            c.Button = ui.Pressable("Chapter_" + (index + 1), _body, s.chapterBanner, Color.white);
            UiFactory.Layout(c.Button, preferredHeight: s.chapterCardHeight);
            c.Button.onClick.AddListener(() => OpenChapter(index + 1));

            // theme thumbnail: a cell slab of the theme, the chapter icon (or the lock) on it
            c.Thumb = ui.Image("Theme", c.Button.transform, null, Color.white);
            c.Thumb.preserveAspect = true;
            UiFactory.Place(c.Thumb.rectTransform, 0, 0.5f, new Vector2(s.chapterThumbSize, s.chapterThumbSize), new Vector2(pad, 6));
            c.Icon = ui.MaskIcon("Icon", c.Thumb.transform, null, s.chapterIconSize, s.icon);
            UiFactory.Place(c.Icon.rectTransform, 0.5f, 0.5f, c.Icon.rectTransform.sizeDelta, Vector2.zero);
            c.Lock = ui.Icon("Lock", c.Thumb.transform, "icon_lock", s.chapterIconSize);
            UiFactory.Place(c.Lock.rectTransform, 0.5f, 0.5f, c.Lock.rectTransform.sizeDelta, Vector2.zero);

            float left = pad + s.chapterThumbSize + 32;
            c.Number = ui.Label("Number", c.Button.transform, null, s.smallSize, s.textMuted, TextAnchor.LowerLeft, bold: true);
            var nrt = c.Number.rectTransform;
            nrt.anchorMin = new Vector2(0, 0.55f);
            nrt.anchorMax = new Vector2(1, 1);
            nrt.offsetMin = new Vector2(left, 0);
            nrt.offsetMax = new Vector2(-starsWidth, -40);
            UiFactory.AutoSize(c.Number, s.smallSize * 0.7f);
            var name = ui.Label("Name", c.Button.transform, null, s.headerSize, s.text, TextAnchor.UpperLeft, bold: true);
            var art = name.rectTransform;
            art.anchorMin = new Vector2(0, 0);
            art.anchorMax = new Vector2(1, 0.55f);
            art.offsetMin = new Vector2(left, 44);
            art.offsetMax = new Vector2(-starsWidth, -4);
            UiFactory.AutoSize(name, s.bodySize);
            c.Name = name.gameObject.AddComponent<LocalizedText>();

            c.Star = ui.Picture("Star", c.Button.transform, s.starFull, s.chapterStarSize);
            UiFactory.Place(c.Star.rectTransform, 1, 0.5f, new Vector2(s.chapterStarSize, s.chapterStarSize), new Vector2(-(starsWidth - s.chapterStarSize - 16), 6));
            c.Stars = ui.Label("Stars", c.Button.transform, null, s.bodySize, s.text, TextAnchor.MiddleLeft, bold: true);
            UiFactory.Place(c.Stars.rectTransform, 1, 0.5f, new Vector2(starsWidth - s.chapterStarSize - 24 - pad, 72), new Vector2(-pad, 6));
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
                var theme = _ctx.Themes != null ? _ctx.Themes.ForChapter(chapter) : null;
                bool unlocked = StoryProgress.IsChapterUnlocked(progress, book, chapter);
                c.Thumb.sprite = theme != null ? ThemeArt.Slab(theme, 0) : s.tileLevel;
                c.Icon.sprite = _ctx.Icons.Get(chapter.Theme.icon);
                c.Icon.gameObject.SetActive(unlocked);
                c.Lock.gameObject.SetActive(!unlocked);
                string themeName = theme != null ? _ctx.Loc.Get(theme.nameKey) : "";
                string number = string.Format(_ctx.Loc.Get(LocKeys.LevelsChapter), chapter.Number);
                c.Number.text = string.IsNullOrEmpty(themeName) ? number : number + "  ·  " + themeName;
                c.Name.Bind(_ctx.Loc, chapter.Theme.nameKey);
                c.Name.GetComponent<TMP_Text>().color = unlocked ? s.text : s.textMuted;
                c.Stars.text = string.Format(_ctx.Loc.Get(LocKeys.ChaptersStars), StoryProgress.ChapterStars(progress, book, chapter), chapter.MaxStars);
                c.Star.sprite = unlocked ? s.starFull : s.starEmpty;
                UiFactory.SetInteractable(c.Button, unlocked);
            }
        }

        public override void OnShow() => Refresh();
    }
}
