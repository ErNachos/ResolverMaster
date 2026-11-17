using System;

namespace ResolverMaster.Core.Save
{
    /// <summary>
    /// Root of the save file. Every block is optional: a block missing from the file keeps the defaults written here,
    /// unknown keys are ignored (older app reading a newer file, or removed fields).
    /// Future blocks (per-minigame progress) are added as new fields; bump <see cref="CurrentVersion"/> only when old data must be converted.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public SettingsBlock settings = new SettingsBlock();
        /// <summary>Stars per level, per minigame and mode (ErNa-002). Missing in older files: no progress.</summary>
        public ProgressBlock progress = new ProgressBlock();
    }

    /// <summary>User settings. Empty <see cref="language"/> = never chosen (first launch: the phone language decides).</summary>
    [Serializable]
    public sealed class SettingsBlock
    {
        public string language = "";
        public bool sound = true;
        public bool vibration = true;
    }
}
