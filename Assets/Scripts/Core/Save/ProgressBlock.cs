using System;
using System.Collections.Generic;

namespace ResolverMaster.Core.Save
{
    /// <summary>
    /// Best stars of every level, per minigame (<c>GameInfo.Id</c>) and mode ("story", "challenge"). Optional block of the save:
    /// missing = no level played. Level n of the list is unlocked when level n-1 has at least one star.
    /// </summary>
    [Serializable]
    public sealed class ProgressBlock
    {
        public List<ModeProgress> entries = new List<ModeProgress>();

        public ModeProgress Find(string game, string mode, bool create)
        {
            foreach (var e in entries)
                if (e != null && e.game == game && e.mode == mode) return e;
            if (!create) return null;
            var added = new ModeProgress { game = game, mode = mode };
            entries.Add(added);
            return added;
        }

        /// <summary>0 = never solved.</summary>
        public int GetStars(string game, string mode, int levelIndex)
        {
            var e = Find(game, mode, create: false);
            return e != null && levelIndex >= 0 && levelIndex < e.stars.Count ? e.stars[levelIndex] : 0;
        }

        /// <summary>Keeps the best result. Returns true if the stored stars improved.</summary>
        public bool SetBest(string game, string mode, int levelIndex, int stars)
        {
            if (levelIndex < 0 || stars <= 0) return false;
            var e = Find(game, mode, create: true);
            while (e.stars.Count <= levelIndex) e.stars.Add(0);
            if (stars <= e.stars[levelIndex]) return false;
            e.stars[levelIndex] = stars;
            return true;
        }

        public bool IsUnlocked(string game, string mode, int levelIndex) =>
            levelIndex == 0 || (levelIndex > 0 && GetStars(game, mode, levelIndex - 1) > 0);

        /// <summary>Replaces nulls left by an old or hand-edited file.</summary>
        public void Sanitize()
        {
            if (entries == null) entries = new List<ModeProgress>();
            entries.RemoveAll(e => e == null);
            foreach (var e in entries)
                if (e.stars == null) e.stars = new List<int>();
        }
    }

    [Serializable]
    public sealed class ModeProgress
    {
        public string game;
        public string mode;
        /// <summary>Index = level - 1; value 0..3.</summary>
        public List<int> stars = new List<int>();
    }
}
