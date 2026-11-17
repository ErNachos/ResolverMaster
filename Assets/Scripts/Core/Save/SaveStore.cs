using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ResolverMaster.Core.Save
{
    /// <summary>
    /// Reads and writes the save file at a given path. Safe write: the JSON goes to "&lt;file&gt;.tmp" first, then replaces the old file,
    /// so a crash mid-write never leaves a half-written save.
    /// </summary>
    public sealed class SaveStore
    {
        public const string FileName = "save.json";

        public string Path { get; }

        public SaveStore(string path) { Path = path; }

        /// <summary>The store used by the app: persistentDataPath/save.json.</summary>
        public static SaveStore Default() => new SaveStore(System.IO.Path.Combine(Application.persistentDataPath, FileName));

        public SaveData Load()
        {
            try
            {
                return File.Exists(Path) ? SaveSerializer.FromJson(File.ReadAllText(Path, Encoding.UTF8)) : new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveStore: cannot read the save, defaults used. " + e.Message);
                return new SaveData();
            }
        }

        public bool Save(SaveData data)
        {
            string tmp = Path + ".tmp";
            try
            {
                string dir = System.IO.Path.GetDirectoryName(Path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(tmp, SaveSerializer.ToJson(data), new UTF8Encoding(false));
                if (File.Exists(Path)) File.Replace(tmp, Path, null);
                else File.Move(tmp, Path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("SaveStore: save failed. " + e.Message);
                return false;
            }
        }
    }
}
