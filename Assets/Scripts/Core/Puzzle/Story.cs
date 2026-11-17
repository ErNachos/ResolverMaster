using System;
using System.Collections.Generic;
using ResolverMaster.Core.Save;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>Stable id of a Story level: (game, chapter, index in the chapter). Used as the save key; never reuse an id for another level.</summary>
    public readonly struct LevelId : IEquatable<LevelId>
    {
        public readonly string Game;
        /// <summary>1-based.</summary>
        public readonly int Chapter;
        /// <summary>0-based position inside the chapter.</summary>
        public readonly int Index;

        public LevelId(string game, int chapter, int index)
        {
            Game = game;
            Chapter = chapter;
            Index = index;
        }

        public bool Equals(LevelId other) => Game == other.Game && Chapter == other.Chapter && Index == other.Index;
        public override bool Equals(object obj) => obj is LevelId other && Equals(other);
        public override int GetHashCode() => ((Game?.GetHashCode() ?? 0) * 397 ^ Chapter) * 397 ^ Index;
        public override string ToString() => $"{Game}/c{Chapter}/{Index + 1}";
    }

    /// <summary>One Story chapter of a game: its theme (name key, icon, color) and which levels of the numbered list it holds.</summary>
    public sealed class StoryChapter
    {
        /// <summary>1-based.</summary>
        public int Number;
        /// <summary>0-based index of its first level in the whole Story (levels are numbered across chapters).</summary>
        public int FirstLevel;
        public int LevelCount;
        public ChapterTheme Theme;

        public int MaxStars => LevelCount * StarRating.MaxStars;
        public bool Contains(int globalIndex) => globalIndex >= FirstLevel && globalIndex < FirstLevel + LevelCount;
    }

    /// <summary>
    /// The Story of one game: consecutive chapters of <see cref="LevelsPerChapter"/> levels, levels numbered 1, 2, ... across chapters.
    /// Pure C#: built by the game from its chapter themes and its saved levels.
    /// </summary>
    public sealed class StoryBook
    {
        /// <summary>Every chapter has 9 levels (3x3 grid on the chapter screen; checked by the validators).</summary>
        public const int LevelsPerChapter = 9;

        readonly List<StoryChapter> _chapters = new List<StoryChapter>();

        public string Game { get; }
        public IReadOnlyList<StoryChapter> Chapters => _chapters;
        public int LevelCount { get; private set; }

        public StoryBook(string game) => Game = game;

        /// <summary>Appends a chapter holding the next <paramref name="levelCount"/> levels.</summary>
        public StoryChapter AddChapter(ChapterTheme theme, int levelCount)
        {
            var chapter = new StoryChapter { Number = _chapters.Count + 1, FirstLevel = LevelCount, LevelCount = levelCount, Theme = theme };
            _chapters.Add(chapter);
            LevelCount += levelCount;
            return chapter;
        }

        /// <summary>1-based chapter, or null.</summary>
        public StoryChapter Chapter(int number) => number >= 1 && number <= _chapters.Count ? _chapters[number - 1] : null;

        /// <summary>The chapter holding level <paramref name="globalIndex"/> (0-based), or null.</summary>
        public StoryChapter ChapterOf(int globalIndex)
        {
            foreach (var c in _chapters)
                if (c.Contains(globalIndex)) return c;
            return null;
        }

        public LevelId IdOf(int globalIndex)
        {
            var c = ChapterOf(globalIndex) ?? throw new ArgumentOutOfRangeException(nameof(globalIndex));
            return new LevelId(Game, c.Number, globalIndex - c.FirstLevel);
        }

        public int GlobalIndex(LevelId id) => Chapter(id.Chapter).FirstLevel + id.Index;
    }

    /// <summary>
    /// Story unlock rules (Core, shared by every game):
    /// inside a chapter the next level unlocks when the previous one is solved; the next chapter unlocks when every level of the
    /// previous chapter is solved, with any number of stars. The first level of the first chapter is always open.
    /// </summary>
    public static class StoryProgress
    {
        public static readonly string Mode = GameModeCodes.ToCode(GameMode.Story);

        public static int Stars(ProgressBlock progress, StoryBook book, int globalIndex) => progress.GetStars(Mode, book.IdOf(globalIndex));

        public static bool IsSolved(ProgressBlock progress, StoryBook book, int globalIndex) => Stars(progress, book, globalIndex) > 0;

        public static bool IsChapterComplete(ProgressBlock progress, StoryBook book, StoryChapter chapter)
        {
            for (int i = 0; i < chapter.LevelCount; i++)
                if (progress.GetStars(Mode, new LevelId(book.Game, chapter.Number, i)) <= 0) return false;
            return true;
        }

        public static bool IsChapterUnlocked(ProgressBlock progress, StoryBook book, StoryChapter chapter)
        {
            if (chapter.Number <= 1) return true;
            var previous = book.Chapter(chapter.Number - 1);
            return previous != null && IsChapterUnlocked(progress, book, previous) && IsChapterComplete(progress, book, previous);
        }

        public static bool IsLevelUnlocked(ProgressBlock progress, StoryBook book, int globalIndex)
        {
            var chapter = book.ChapterOf(globalIndex);
            if (chapter == null || !IsChapterUnlocked(progress, book, chapter)) return false;
            return globalIndex == chapter.FirstLevel || IsSolved(progress, book, globalIndex - 1);
        }

        /// <summary>Stars earned in the chapter (out of <see cref="StoryChapter.MaxStars"/>).</summary>
        public static int ChapterStars(ProgressBlock progress, StoryBook book, StoryChapter chapter)
        {
            int sum = 0;
            for (int i = 0; i < chapter.LevelCount; i++) sum += progress.GetStars(Mode, new LevelId(book.Game, chapter.Number, i));
            return sum;
        }
    }
}
