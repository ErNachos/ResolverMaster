using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// ErNa-014: hardware features of the final manifest. Google Play infers <c>android.hardware.screen.portrait</c> as REQUIRED from the
    /// portrait-only activity (screenOrientation), which hides the app from devices that do not declare it (tablets, PCs, Google Play
    /// Games on PC, Android XR). The launcher module (highest priority in the manifest merge) therefore declares every non-essential
    /// feature with required="false" and tools:node="replace", so no library can turn it back into required. The app stays portrait-only.
    ///
    /// The only feature allowed to stay required is the OpenGL ES 3.0 declaration (glEsVersion, no name): Unity cannot render without it
    /// and Play uses it to filter devices that could not run the game anyway.
    /// </summary>
    public static class AndroidFeatures
    {
        public const string Portrait = "android.hardware.screen.portrait";

        /// <summary>Features declared optional in the launcher manifest. Touchscreen too: the game also works with a mouse/pointer (PC, XR).</summary>
        public static readonly string[] Optional =
        {
            Portrait,
            "android.hardware.touchscreen",
            "android.hardware.touchscreen.multitouch",
            "android.hardware.touchscreen.multitouch.distinct",
        };

        public static readonly XNamespace AndroidNs = "http://schemas.android.com/apk/res/android";
        public static readonly XNamespace ToolsNs = "http://schemas.android.com/tools";

        /// <summary>
        /// Adds (or rewrites) a uses-feature with required="false" and tools:node="replace" for every <see cref="Optional"/> feature.
        /// Idempotent: patching twice gives the same document.
        /// </summary>
        public static void Patch(XDocument manifest)
        {
            var root = manifest.Root;
            if (root.Attribute(XNamespace.Xmlns + "tools") == null) root.SetAttributeValue(XNamespace.Xmlns + "tools", ToolsNs.NamespaceName);
            foreach (var name in Optional)
            {
                var existing = root.Elements("uses-feature").Where(e => (string)e.Attribute(AndroidNs + "name") == name).ToList();
                foreach (var e in existing.Skip(1)) e.Remove();
                var el = existing.FirstOrDefault();
                if (el == null)
                {
                    el = new XElement("uses-feature");
                    root.Add(el);
                }
                el.SetAttributeValue(AndroidNs + "name", name);
                el.SetAttributeValue(AndroidNs + "required", "false");
                el.SetAttributeValue(ToolsNs + "node", "replace");
            }
        }

        /// <summary>A uses-feature is required unless android:required="false" (the Android default is true).</summary>
        public static bool IsRequired(XElement usesFeature) =>
            !string.Equals(((string)usesFeature.Attribute(AndroidNs + "required"))?.Trim(), "false", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Problems of a FINAL (merged) manifest: the portrait feature must be declared and optional, and every other required
        /// uses-feature must be the OpenGL ES declaration (no name).
        /// </summary>
        public static List<string> CheckFinal(XDocument manifest)
        {
            var problems = new List<string>();
            var features = manifest.Root?.Elements("uses-feature").ToList() ?? new List<XElement>();
            var portrait = features.Where(e => (string)e.Attribute(AndroidNs + "name") == Portrait).ToList();
            if (portrait.Count == 0) problems.Add($"{Portrait} not declared (Google Play would infer it as required)");
            else if (portrait.Any(IsRequired)) problems.Add($"{Portrait} is required");
            foreach (var e in features.Where(IsRequired))
            {
                string name = (string)e.Attribute(AndroidNs + "name");
                if (string.IsNullOrEmpty(name)) continue;   // glEsVersion: essential
                if (name != Portrait) problems.Add($"required uses-feature {name}");
            }
            return problems;
        }

        /// <summary>The launcher module of a generated Gradle project (path = its unityLibrary module, as Unity passes it).</summary>
        public static string LauncherManifest(string unityLibraryPath) =>
            Path.Combine(Path.GetDirectoryName(unityLibraryPath.TrimEnd('/', '\\')) ?? unityLibraryPath, "launcher", "src", "main", "AndroidManifest.xml");

        /// <summary>
        /// Merged manifests the last Android build left in the Gradle project (APK and AAB variants), newest first. Empty if no build.
        /// </summary>
        public static string[] LastMergedManifests()
        {
            string dir = Path.Combine("Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "launcher", "build", "intermediates");
            if (!Directory.Exists(dir)) return new string[0];
            return Directory.GetFiles(dir, "AndroidManifest.xml", SearchOption.AllDirectories)
                .Where(p => p.Replace('\\', '/').Contains("/release/"))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToArray();
        }
    }

    /// <summary>ErNa-014: writes the optional features into the launcher manifest of every generated Gradle project.</summary>
    public sealed class AndroidFeaturesWriter : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string file = AndroidFeatures.LauncherManifest(path);
            if (!File.Exists(file)) throw new BuildFailedException("AndroidFeatures: launcher manifest not found next to " + path);
            var doc = XDocument.Load(file);
            AndroidFeatures.Patch(doc);
            File.WriteAllText(file, doc.Declaration + "\n" + doc.Root, new UTF8Encoding(false));
        }
    }

    /// <summary>
    /// ErNa-014: after the writer, every manifest of the generated Gradle project (launcher, unityLibrary, our libraries) is checked as
    /// if merged: the launcher's tools:node="replace" wins, any other required feature stops the build.
    /// </summary>
    public sealed class AndroidFeaturesGradleCheck : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string root = Path.GetDirectoryName(path.TrimEnd('/', '\\')) ?? path;
            var launcher = XDocument.Load(AndroidFeatures.LauncherManifest(path));
            var replaced = new HashSet<string>(launcher.Root.Elements("uses-feature")
                .Where(e => (string)e.Attribute(AndroidFeatures.ToolsNs + "node") == "replace")
                .Select(e => (string)e.Attribute(AndroidFeatures.AndroidNs + "name")));
            var merged = new XDocument(new XElement("manifest", launcher.Root.Elements("uses-feature")));
            foreach (var file in Directory.GetFiles(root, "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                string rel = file.Substring(root.Length).Replace('\\', '/');
                if (rel.Contains("/build/") || rel.StartsWith("/launcher/")) continue;
                foreach (var e in XDocument.Load(file).Root.Elements("uses-feature"))
                    if (!replaced.Contains((string)e.Attribute(AndroidFeatures.AndroidNs + "name"))) merged.Root.Add(new XElement(e));
            }
            var problems = AndroidFeatures.CheckFinal(merged);
            if (problems.Count > 0) throw new BuildFailedException("AndroidFeatures: " + string.Join("; ", problems));
            Debug.Log("AndroidFeatures: Gradle project OK (screen.portrait and touchscreen optional, only OpenGL ES 3.0 required).");
        }
    }
}
