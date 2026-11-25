using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// ErNa-016: Project Settings > Audio (ProjectSettings/AudioManager.asset). Unity audio on (Disable Unity Audio off) and the global
    /// volume above 0; written by Setup &gt; Apply Project Settings, checked by the validator (<c>AudioSettingsCheck</c>).
    /// </summary>
    public static class AudioProjectSettings
    {
        public const string AssetPath = "ProjectSettings/AudioManager.asset";
        public const float Volume = 1f;

        static SerializedObject Load()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetPath);
            return assets != null && assets.Length > 0 && assets[0] != null ? new SerializedObject(assets[0]) : null;
        }

        /// <summary>Audio on and at full global volume.</summary>
        public static void Apply(StringBuilder log)
        {
            var so = Load();
            if (so == null)
            {
                Debug.LogWarning($"ProjectSetup: {AssetPath} not found");
                return;
            }
            so.FindProperty("m_DisableAudio").boolValue = false;
            var volume = so.FindProperty("m_Volume");
            if (volume.floatValue <= 0f) volume.floatValue = Volume;
            so.ApplyModifiedPropertiesWithoutUndo();
            log?.Append($"\n  audio: Unity audio on, global volume {volume.floatValue:0.##}");
        }

        /// <summary>What is wrong with the audio settings (empty = right).</summary>
        public static List<string> Problems()
        {
            var problems = new List<string>();
            var so = Load();
            if (so == null)
            {
                problems.Add($"{AssetPath} not found");
                return problems;
            }
            var disable = so.FindProperty("m_DisableAudio");
            var volume = so.FindProperty("m_Volume");
            if (disable == null || volume == null)
            {
                problems.Add($"{AssetPath}: m_DisableAudio or m_Volume not found");
                return problems;
            }
            if (disable.boolValue) problems.Add("Project Settings > Audio: Disable Unity Audio is on (no sound at all; Setup > Apply Project Settings)");
            if (volume.floatValue <= 0f) problems.Add($"Project Settings > Audio: global volume {volume.floatValue} (must be above 0; Setup > Apply Project Settings)");
            return problems;
        }
    }
}
