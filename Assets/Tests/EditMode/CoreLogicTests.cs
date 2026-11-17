using System.IO;
using NUnit.Framework;
using ResolverMaster.Core.Localization;
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
        public void BumpVersionCode_IncreasesByOne()
        {
            int stored = 7;
            int result = VersionTools.Bump(() => stored, v => stored = v);
            Assert.AreEqual(8, result);
            Assert.AreEqual(8, stored);
        }
    }
}
