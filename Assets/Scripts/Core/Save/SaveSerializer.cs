using System;
using UnityEngine;

namespace ResolverMaster.Core.Save
{
    /// <summary>
    /// JSON (de)serialization of <see cref="SaveData"/>. Reading never throws: an empty or corrupt file gives the defaults.
    /// Deserialization overwrites a default instance, so missing blocks and fields keep their defaults and unknown keys are ignored.
    /// </summary>
    public static class SaveSerializer
    {
        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, prettyPrint: true);

        public static SaveData FromJson(string json)
        {
            var data = new SaveData();
            if (string.IsNullOrWhiteSpace(json)) return data;
            try
            {
                JsonUtility.FromJsonOverwrite(json, data);
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSerializer: unreadable save, defaults used. " + e.Message);
                data = new SaveData();
            }
            if (data.settings == null) data.settings = new SettingsBlock();
            if (data.settings.language == null) data.settings.language = "";
            if (data.progress == null) data.progress = new ProgressBlock();
            data.progress.Sanitize();
            Migrate(data);
            return data;
        }

        /// <summary>Converts older versions to <see cref="SaveData.CurrentVersion"/>. Version 1 is the first: nothing to convert yet.</summary>
        static void Migrate(SaveData data)
        {
            if (data.version < SaveData.CurrentVersion) data.version = SaveData.CurrentVersion;
        }
    }
}
