using System.IO;
using NUnit.Framework;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.Save;
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

        [Test]
        public void BumpVersionCode_IncreasesByOne()
        {
            int stored = 7;
            int result = VersionTools.Bump(() => stored, v => stored = v);
            Assert.AreEqual(8, result);
            Assert.AreEqual(8, stored);
        }
    }
}
