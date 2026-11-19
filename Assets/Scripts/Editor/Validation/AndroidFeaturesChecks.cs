using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor.Validation
{
    /// <summary>
    /// ErNa-014: the final manifest declares android.hardware.screen.portrait with required="false" and no other non-essential
    /// uses-feature is required (only OpenGL ES 3.0). Checked on a simulated Unity manifest (the rules) and on the merged manifests of
    /// the last Android build, when that build already ran with <see cref="AndroidFeaturesWriter"/> (older outputs are skipped).
    /// The app stays portrait-only.
    /// </summary>
    public sealed class AndroidFeaturesCheck : IValidationCheck
    {
        public string Name => "AndroidFeatures";

        public void Run(ValidationReport report)
        {
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
                report.Error("the app must stay portrait-only (Setup > Apply Project Settings)");

            // the rules on a manifest shaped like Unity's: required touchscreen and no portrait declaration
            var library = XDocument.Parse(
                "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\">" +
                "<uses-feature android:glEsVersion=\"0x00030000\"/>" +
                "<uses-feature android:name=\"android.hardware.touchscreen\" android:required=\"true\"/>" +
                "<uses-feature android:name=\"android.hardware.screen.portrait\"/></manifest>");
            if (AndroidFeatures.CheckFinal(library).Count != 2) report.Error("CheckFinal does not reject required portrait/touchscreen");
            var launcher = XDocument.Parse("<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"/>");
            AndroidFeatures.Patch(launcher);
            var merged = new XDocument(new XElement("manifest", launcher.Root.Elements("uses-feature"),
                library.Root.Elements("uses-feature").Where(e => e.Attribute(AndroidFeatures.AndroidNs + "name") == null)));
            foreach (var p in AndroidFeatures.CheckFinal(merged)) report.Error("patched launcher manifest: " + p);

            // the last build, if it already used the writer
            string launcherSrc = AndroidFeatures.LauncherManifest(Path.Combine("Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "unityLibrary"));
            if (!File.Exists(launcherSrc)) return;
            var src = XDocument.Load(launcherSrc);
            bool patched = src.Root.Elements("uses-feature").Any(e =>
                (string)e.Attribute(AndroidFeatures.AndroidNs + "name") == AndroidFeatures.Portrait
                && (string)e.Attribute(AndroidFeatures.ToolsNs + "node") == "replace");
            if (!patched)
            {
                Debug.Log("AndroidFeatures: the last Gradle project predates ErNa-014, its merged manifest is not checked.");
                return;
            }
            var since = File.GetLastWriteTimeUtc(launcherSrc);
            foreach (var file in AndroidFeatures.LastMergedManifests().Where(f => File.GetLastWriteTimeUtc(f) >= since))
                foreach (var p in AndroidFeatures.CheckFinal(XDocument.Load(file)))
                    report.Error($"{file.Replace('\\', '/')}: {p}");
        }
    }
}
