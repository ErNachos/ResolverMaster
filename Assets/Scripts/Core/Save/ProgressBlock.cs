using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Core.Save
{
    /// <summary>
    /// Best stars of every level, per minigame (<c>GameInfo.Id</c>), mode ("story", "challenge") and chapter. Optional block of the save:
    /// missing = no level played. A level is addressed by its stable <see cref="LevelId"/> (game, chapter, index in the chapter), so
    /// adding levels at the end of a chapter or new chapters never moves saved stars. The unlock rules are in <see cref="StoryProgress"/>.
    /// ErNa-003: replaces the ErNa-002 flat list (<c>stars</c> per mode), which is ignored when read.
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
        public int GetStars(string mode, LevelId id)
        {
            var chapter = Find(id.Game, mode, create: false)?.FindChapter(id.Chapter, create: false);
            return chapter != null && id.Index >= 0 && id.Index < chapter.stars.Count ? chapter.stars[id.Index] : 0;
        }

        /// <summary>Keeps the best result. Returns true if the stored stars improved.</summary>
        public bool SetBest(string mode, LevelId id, int stars)
        {
            if (id.Index < 0 || id.Chapter < 1 || stars <= 0) return false;
            var chapter = Find(id.Game, mode, create: true).FindChapter(id.Chapter, create: true);
            while (chapter.stars.Count <= id.Index) chapter.stars.Add(0);
            if (stars <= chapter.stars[id.Index]) return false;
            chapter.stars[id.Index] = stars;
            return true;
        }

        /// <summary>Replaces nulls left by an old or hand-edited file.</summary>
        public void Sanitize()
        {
            if (entries == null) entries = new List<ModeProgress>();
            entries.RemoveAll(e => e == null);
            foreach (var e in entries)
            {
                if (e.chapters == null) e.chapters = new List<ChapterProgress>();
                e.chapters.RemoveAll(c => c == null);
                foreach (var c in e.chapters)
                    if (c.stars == null) c.stars = new List<int>();
            }
        }
    }

    [Serializable]
    public sealed class ModeProgress
    {
        public string game;
        public string mode;
        public List<ChapterProgress> chapters = new List<ChapterProgress>();

        public ChapterProgress FindChapter(int chapter, bool create)
        {
            foreach (var c in chapters)
                if (c != null && c.chapter == chapter) return c;
            if (!create) return null;
            var added = new ChapterProgress { chapter = chapter };
            chapters.Add(added);
            return added;
        }
    }

    [Serializable]
    public sealed class ChapterProgress
    {
        /// <summary>1-based chapter number.</summary>
        public int chapter;
        /// <summary>Index = level index inside the chapter (0-based); value 0..3.</summary>
        public List<int> stars = new List<int>();
    }
}
