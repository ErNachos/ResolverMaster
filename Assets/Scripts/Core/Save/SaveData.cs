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
        /// <summary>Stars per level, per minigame, mode and chapter (ErNa-003). Missing in older files: no progress.</summary>
        public ProgressBlock progress = new ProgressBlock();
        /// <summary>Home state (ErNa-004). Missing in older files: first card.</summary>
        public HomeBlock home = new HomeBlock();
        /// <summary>Challenge runs in progress and records (ErNa-007). Missing in older files: none.</summary>
        public ResolverMaster.Core.Challenge.ChallengeBlock challenge = new ResolverMaster.Core.Challenge.ChallengeBlock();
    }

    /// <summary>Home: the id (<c>GameInfo.Id</c>) of the last card chosen, shown again at start-up. Empty = first card.</summary>
    [Serializable]
    public sealed class HomeBlock
    {
        public string selectedGame = "";
    }

    /// <summary>User settings. Empty <see cref="language"/> = never chosen (first launch: the phone language decides).</summary>
    [Serializable]
    public sealed class SettingsBlock
    {
        public string language = "";
        /// <summary>The sound effects (ErNa-010: the old "sound" switch, now "Effects").</summary>
        public bool sound = true;
        /// <summary>ErNa-010: the music, switched on and off apart from the effects. Missing in older files: on.</summary>
        public bool music = true;
        public bool vibration = true;
    }
}
