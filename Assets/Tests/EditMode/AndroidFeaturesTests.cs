using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using ResolverMaster.Editor;

namespace ResolverMaster.Tests
{
    /// <summary>ErNa-014: optional hardware features in the launcher manifest and the check of the final manifest.</summary>
    public sealed class AndroidFeaturesTests
    {
        const string Launcher =
            "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\">" +
            "<supports-screens android:anyDensity=\"true\"/>" +
            "<uses-feature android:name=\"android.hardware.touchscreen\" android:required=\"true\"/></manifest>";

        static XElement Feature(XDocument doc, string name) =>
            doc.Root.Elements("uses-feature").Single(e => (string)e.Attribute(AndroidFeatures.AndroidNs + "name") == name);

        [Test]
        public void Patch_DeclaresPortraitOptionalWithReplace()
        {
            var doc = XDocument.Parse(Launcher);
            AndroidFeatures.Patch(doc);
            var portrait = Feature(doc, AndroidFeatures.Portrait);
            Assert.AreEqual("false", (string)portrait.Attribute(AndroidFeatures.AndroidNs + "required"));
            Assert.AreEqual("replace", (string)portrait.Attribute(AndroidFeatures.ToolsNs + "node"));
            Assert.AreEqual("false", (string)Feature(doc, "android.hardware.touchscreen").Attribute(AndroidFeatures.AndroidNs + "required"));
            Assert.IsEmpty(AndroidFeatures.CheckFinal(doc));
        }

        [Test]
        public void Patch_IsIdempotent()
        {
            var once = XDocument.Parse(Launcher);
            AndroidFeatures.Patch(once);
            var twice = new XDocument(once);
            AndroidFeatures.Patch(twice);
            Assert.AreEqual(once.ToString(), twice.ToString());
            Assert.AreEqual(AndroidFeatures.Optional.Length, twice.Root.Elements("uses-feature").Count());
        }

        [Test]
        public void CheckFinal_AllowsOnlyOpenGlEsAsRequired()
        {
            var ok = XDocument.Parse(
                "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\">" +
                "<uses-feature android:glEsVersion=\"0x00030000\"/>" +
                "<uses-feature android:name=\"android.hardware.screen.portrait\" android:required=\"false\"/></manifest>");
            Assert.IsEmpty(AndroidFeatures.CheckFinal(ok));

            var missing = XDocument.Parse(
                "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\">" +
                "<uses-feature android:name=\"android.hardware.camera\"/></manifest>");
            var problems = AndroidFeatures.CheckFinal(missing);
            Assert.AreEqual(2, problems.Count);   // portrait not declared + camera required (default true)
        }
    }
}
