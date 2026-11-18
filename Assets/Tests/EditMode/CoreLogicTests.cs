using System.IO;
using NUnit.Framework;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.Save;
using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Themes;
using ResolverMaster.Editor;

namespace ResolverMaster.Tests
{
    public sealed class CoreLogicTests
    {
        [Test]
        public void InitialLanguage_ItalianPhoneGetsItalian_OthersGetEnglish()
        {
            Assert.AreEqual(Language.Italian, LanguagePicker.Resolve("", "Italian"));
            Assert.AreEqual(Language.Italian, LanguagePicker.Resolve(null, "Italian"));
            Assert.AreEqual(Language.English, LanguagePicker.Resolve("", "English"));
            Assert.AreEqual(Language.English, LanguagePicker.Resolve("", "French"));
            Assert.AreEqual(Language.English, LanguagePicker.Resolve("", "Unknown"));
        }

        [Test]
        public void SavedLanguage_WinsOverPhoneLanguage()
        {
            Assert.AreEqual(Language.English, LanguagePicker.Resolve("en", "Italian"));
            Assert.AreEqual(Language.Italian, LanguagePicker.Resolve("it", "German"));
            // an unknown saved code counts as "never chosen"
            Assert.AreEqual(Language.Italian, LanguagePicker.Resolve("xx", "Italian"));
        }

        [Test]
        public void Save_WriteThenRead_ReturnsSameValues()
        {
            string dir = Path.Combine("Temp", "ResolverMasterTests");
            string path = Path.Combine(dir, "save_roundtrip.json");
            if (File.Exists(path)) File.Delete(path);
            var store = new SaveStore(path);
            var data = new SaveData();
            data.settings.language = "en";
            data.settings.sound = false;
            data.settings.vibration = false;

            Assert.IsTrue(store.Save(data));
            data.settings.sound = true;   // second save goes through the replace path
            Assert.IsTrue(store.Save(data));
            var read = store.Load();

            Assert.AreEqual(SaveData.CurrentVersion, read.version);
            Assert.AreEqual("en", read.settings.language);
            Assert.IsTrue(read.settings.sound);
            Assert.IsFalse(read.settings.vibration);
            Assert.IsFalse(File.Exists(path + ".tmp"));
            File.Delete(path);
        }

        [Test]
        public void Save_UnknownKeysIgnored_MissingBlocksGetDefaults()
        {
            var noSettings = SaveSerializer.FromJson("{\"version\":1,\"futureBlock\":{\"a\":5},\"extra\":\"x\"}");
            Assert.IsNotNull(noSettings.settings);
            Assert.AreEqual("", noSettings.settings.language);
            Assert.IsTrue(noSettings.settings.sound);
            Assert.IsTrue(noSettings.settings.vibration);

            var partial = SaveSerializer.FromJson("{\"version\":1,\"settings\":{\"sound\":false,\"unknown\":3}}");
            Assert.IsFalse(partial.settings.sound);
            Assert.IsTrue(partial.settings.vibration);
            Assert.AreEqual("", partial.settings.language);

            var corrupt = SaveSerializer.FromJson("{ not json");
            Assert.AreEqual(SaveData.CurrentVersion, corrupt.version);
            Assert.IsTrue(corrupt.settings.sound);
        }

        [Test]
        public void Chapters_NextChapterUnlocksOnlyWhenAllNineAreSolved_AnyStars()
        {
            var book = new StoryBook("game");
            for (int c = 0; c < 3; c++) book.AddChapter(new ChapterTheme("chapter", "icon", UnityEngine.Color.white), StoryBook.LevelsPerChapter);
            var progress = new ProgressBlock();
            var chapter1 = book.Chapter(1);
            var chapter2 = book.Chapter(2);
            Assert.AreEqual(27, book.LevelCount);
            Assert.AreEqual(new LevelId("game", 2, 0), book.IdOf(9), "levels are numbered across chapters, ids stay per chapter");

            Assert.IsTrue(StoryProgress.IsLevelUnlocked(progress, book, 0));
            Assert.IsFalse(StoryProgress.IsLevelUnlocked(progress, book, 1), "level 2 waits for level 1");
            Assert.IsFalse(StoryProgress.IsChapterUnlocked(progress, book, chapter2));

            // eight of nine levels with 1 star: chapter 2 still locked
            for (int i = 0; i < 8; i++) progress.SetBest(StoryProgress.Mode, book.IdOf(i), 1);
            Assert.IsTrue(StoryProgress.IsLevelUnlocked(progress, book, 8));
            Assert.IsFalse(StoryProgress.IsChapterUnlocked(progress, book, chapter2));
            Assert.IsFalse(StoryProgress.IsLevelUnlocked(progress, book, 9));

            // the ninth with 1 star too: chapter 2 opens (no star requirement), only its first level
            progress.SetBest(StoryProgress.Mode, book.IdOf(8), 1);
            Assert.IsTrue(StoryProgress.IsChapterComplete(progress, book, chapter1));
            Assert.IsTrue(StoryProgress.IsChapterUnlocked(progress, book, chapter2));
            Assert.IsTrue(StoryProgress.IsLevelUnlocked(progress, book, 9));
            Assert.IsFalse(StoryProgress.IsLevelUnlocked(progress, book, 10));
            Assert.IsFalse(StoryProgress.IsChapterUnlocked(progress, book, book.Chapter(3)));
            Assert.AreEqual(9, StoryProgress.ChapterStars(progress, book, chapter1));
            Assert.AreEqual(27, chapter1.MaxStars);

            // better stars are kept, worse ones ignored; the stars survive a save round trip by stable id
            Assert.IsTrue(progress.SetBest(StoryProgress.Mode, book.IdOf(0), 3));
            Assert.IsFalse(progress.SetBest(StoryProgress.Mode, book.IdOf(0), 2));
            var data = new SaveData { progress = progress };
            var read = SaveSerializer.FromJson(SaveSerializer.ToJson(data));
            Assert.AreEqual(3, read.progress.GetStars(StoryProgress.Mode, new LevelId("game", 1, 0)));
            Assert.IsTrue(StoryProgress.IsChapterUnlocked(read.progress, book, chapter2));
        }

        // ---------------- ErNa-004 ----------------

        [Test]
        public void ArtVariants_SameSeedSameDrawings_DifferentPiecesDiffer()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var first = new int[4];
                for (int k = 0; k < 4; k++)
                {
                    first[k] = ArtVariants.Pick(seed, 1, k, 4);
                    Assert.AreEqual(first[k], ArtVariants.Pick(seed, 1, k, 4), "same seed, same piece: same drawing");
                    Assert.That(first[k], Is.InRange(0, 3));
                }
                CollectionAssert.AllItemsAreUnique(first, "4 pieces of one level get the 4 different drawings");
                Assert.AreEqual(first[0], ArtVariants.Pick(seed, 1, 4, 4), "the fifth piece starts the cycle again");
            }
            // the seed really decides: not every level gets the same first drawing
            var firsts = new System.Collections.Generic.HashSet<int>();
            for (int seed = 0; seed < 50; seed++) firsts.Add(ArtVariants.Pick(seed, 2, 0, 4));
            Assert.Greater(firsts.Count, 1);
        }

        [Test]
        public void Carousel_SnapsToNearestCard_NeverPastFirstOrLast()
        {
            Assert.AreEqual(1, CardCarousel.Nearest(1.4f, 3));
            Assert.AreEqual(2, CardCarousel.Nearest(1.6f, 3));
            Assert.AreEqual(0, CardCarousel.Nearest(-0.8f, 3));
            Assert.AreEqual(2, CardCarousel.Nearest(7f, 3));

            var c = new CardCarousel(3, 700);
            c.JumpTo(0);
            c.BeginDrag();
            c.Drag(-420);   // finger to the left by 0.6 of a step: the second card is nearer
            Assert.AreEqual(0.6f, c.Position, 1e-4);
            Assert.AreEqual(1, c.EndDrag(0, 0.18f));
            for (int i = 0; i < 200 && c.Tick(1f / 60f, 0.09f); i++) { }
            Assert.AreEqual(1f, c.Position);
            Assert.IsTrue(c.Resting);

            c.BeginDrag();
            c.Drag(-100);   // a short drag: back to the same card
            Assert.AreEqual(1, c.EndDrag(0, 0.18f));

            c.JumpTo(0);
            c.BeginDrag();
            c.Drag(5000);   // far past the first card: soft wall, snap back to the first
            Assert.Less(c.Position, 0f);
            Assert.Greater(c.Position, -5000f / 700f, "past the ends the cards move less than the finger");
            Assert.AreEqual(0, c.EndDrag(3000, 0.18f));

            c.JumpTo(2);
            c.BeginDrag();
            c.Drag(-5000);  // far past the last card, with a fast flick: still the last
            Assert.AreEqual(2, c.EndDrag(-9000, 0.18f));

            c.JumpTo(0);
            c.BeginDrag();
            c.Drag(-50);    // small drag but a fast flick to the left: one card only, not two
            Assert.AreEqual(1, c.EndDrag(-20000, 0.18f));
        }

        [Test]
        public void HomeCard_SavedAndRead_UnknownGameFallsBackToFirst()
        {
            var games = new[]
            {
                new GameInfo { Id = "plumber" },
                new GameInfo { Id = "trafficjam" },
            };
            var data = new SaveData();
            Assert.AreEqual(0, HomeSelection.IndexOf(games, data.home.selectedGame), "nothing saved: first card");

            data.home.selectedGame = "trafficjam";
            var read = SaveSerializer.FromJson(SaveSerializer.ToJson(data));
            Assert.AreEqual("trafficjam", read.home.selectedGame);
            Assert.AreEqual(1, HomeSelection.IndexOf(games, read.home.selectedGame));

            Assert.AreEqual(0, HomeSelection.IndexOf(games, "removed_game"), "a game that no longer exists: first card");
            var old = SaveSerializer.FromJson("{\"version\":1,\"settings\":{\"sound\":false}}");
            Assert.AreEqual("", old.home.selectedGame, "older save without the home block");
            Assert.AreEqual(0, HomeSelection.IndexOf(games, old.home.selectedGame));
        }

        // ErNa-005
        [Test]
        public void RectangularGrid_3x4_NeighboursOnEdgesAndCorners()
        {
            var g = new SquareTopology(3, 4);
            Assert.AreEqual(12, g.CellCount);
            const int up = SquareTopology.Up, right = SquareTopology.Right, down = SquareTopology.Down, left = SquareTopology.Left;
            // top-left corner (0): only right and down
            Assert.AreEqual(-1, g.Neighbor(0, up));
            Assert.AreEqual(-1, g.Neighbor(0, left));
            Assert.AreEqual(1, g.Neighbor(0, right));
            Assert.AreEqual(3, g.Neighbor(0, down));
            // top-right corner (2): no wrap to the next row
            Assert.AreEqual(-1, g.Neighbor(2, right));
            Assert.AreEqual(-1, g.Neighbor(2, up));
            Assert.AreEqual(5, g.Neighbor(2, down));
            // bottom-left (9) and bottom-right (11) corners of the 4th row
            Assert.AreEqual(-1, g.Neighbor(9, down));
            Assert.AreEqual(-1, g.Neighbor(9, left));
            Assert.AreEqual(6, g.Neighbor(9, up));
            Assert.AreEqual(-1, g.Neighbor(11, down));
            Assert.AreEqual(-1, g.Neighbor(11, right));
            Assert.AreEqual(10, g.Neighbor(11, left));
            // left edge (3) and right edge (8) cells: no wrap sideways
            Assert.AreEqual(-1, g.Neighbor(3, left));
            Assert.AreEqual(5, g.Neighbor(4, right));
            Assert.AreEqual(-1, g.Neighbor(8, right));
            Assert.AreEqual(11, g.Neighbor(8, down));
            Assert.AreEqual(2, g.X(11));
            Assert.AreEqual(3, g.Y(11));
            Assert.AreEqual(down, g.DirectionTo(8, 11));
        }

        // ErNa-005
        [Test]
        public void Themes_EveryChapterGetsItsTheme_MissingOrUnknownUsesTheDefault()
        {
            var cfg = UnityEngine.ScriptableObject.CreateInstance<ThemeConfig>();
            try
            {
                var book = new StoryBook("test");
                var c1 = book.AddChapter(new ChapterTheme("c1", "icon", UnityEngine.Color.white, ThemeConfig.Garden), 9);
                var c2 = book.AddChapter(new ChapterTheme("c2", "icon", UnityEngine.Color.white, ThemeConfig.Farm), 9);
                var c3 = book.AddChapter(new ChapterTheme("c3", "icon", UnityEngine.Color.white, ThemeConfig.City), 9);
                var none = book.AddChapter(new ChapterTheme("c4", "icon", UnityEngine.Color.white), 9);
                var unknown = book.AddChapter(new ChapterTheme("c5", "icon", UnityEngine.Color.white, "volcano"), 9);
                Assert.AreEqual(ThemeConfig.Garden, cfg.ForChapter(c1).id);
                Assert.AreEqual(ThemeConfig.Farm, cfg.ForChapter(c2).id);
                Assert.AreEqual(ThemeConfig.City, cfg.ForChapter(c3).id);
                Assert.AreEqual(cfg.defaultTheme, cfg.ForChapter(none).id, "a chapter without a theme uses the default one");
                Assert.AreEqual(cfg.defaultTheme, cfg.ForChapter(unknown).id, "an unknown theme id uses the default one");
                Assert.IsFalse(cfg.Catalog.Has("volcano"));

                var catalog = new ThemeCatalog(cfg.themes, "city");
                Assert.AreEqual("city", catalog.ForChapter(none).id, "the default is the configured one");
            }
            finally { UnityEngine.Object.DestroyImmediate(cfg); }
        }

        [Test]
        public void BumpVersionCode_IncreasesByOne()
        {
            int stored = 7;
            int result = VersionTools.Bump(() => stored, v => stored = v);
            Assert.AreEqual(8, result);
            Assert.AreEqual(8, stored);
        }

        // ------------------------------------------------------------------ ErNa-009

        [Test]
        public void ChapterPages_SwipeToNextAndPrevious_OnePageAtMost_LockedChapterVisibleButLocked_ReturnToTheLevelChapter()
        {
            var book = new ResolverMaster.Games.Plumber.PlumberModule().Story;
            int chapters = book.Chapters.Count;
            Assert.AreEqual(13, chapters);
            var pages = new ResolverMaster.Core.UI.CardCarousel(chapters, 1000f);
            pages.JumpTo(2);   // chapter 3

            // finger to the left past half a page: the next chapter; to the right: the previous one
            pages.BeginDrag();
            pages.Drag(-600f);
            Assert.AreEqual(3, pages.EndDrag(0f, 0.18f));
            pages.JumpTo(3);
            pages.BeginDrag();
            pages.Drag(600f);
            Assert.AreEqual(2, pages.EndDrag(0f, 0.18f));
            // a short drag goes back; a strong flick moves one page only
            pages.JumpTo(2);
            pages.BeginDrag();
            pages.Drag(-200f);
            Assert.AreEqual(2, pages.EndDrag(0f, 0.18f));
            pages.BeginDrag();
            pages.Drag(-100f);
            Assert.AreEqual(3, pages.EndDrag(-30000f, 0.18f));
            // never before the first nor after the last chapter
            pages.JumpTo(chapters - 1);
            pages.BeginDrag();
            pages.Drag(-900f);
            Assert.AreEqual(chapters - 1, pages.EndDrag(-5000f, 0.18f));
            pages.JumpTo(0);
            pages.BeginDrag();
            pages.Drag(900f);
            Assert.AreEqual(0, pages.EndDrag(5000f, 0.18f));

            // a locked chapter can be shown, but none of its levels can be played
            var progress = new ProgressBlock();
            Assert.IsTrue(StoryProgress.IsChapterUnlocked(progress, book, book.Chapter(1)));
            var locked = book.Chapter(4);
            Assert.IsFalse(StoryProgress.IsChapterUnlocked(progress, book, locked));
            for (int i = 0; i < locked.LevelCount; i++) Assert.IsFalse(StoryProgress.IsLevelUnlocked(progress, book, locked.FirstLevel + i));

            // back from a level (also after "Next" into another chapter): the page of the level's chapter
            Assert.AreEqual(5, book.ChapterOf(40).Number);
            Assert.AreEqual(10, book.ChapterOf(81).Number, "level 82 opens chapter 10");
        }
    }
}
