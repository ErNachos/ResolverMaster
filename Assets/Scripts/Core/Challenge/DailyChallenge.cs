using System;
using System.Collections.Generic;

namespace ResolverMaster.Core.Challenge
{
    /// <summary>What the Home card shows for today's daily challenge.</summary>
    public enum DailyStatus
    {
        /// <summary>Today's challenge can be started.</summary>
        ToDo,
        /// <summary>A daily challenge is in progress (today's, or a pending one of an earlier day).</summary>
        InProgress,
        /// <summary>Today's attempt is over (completed or lost): closed until tomorrow.</summary>
        Done,
    }

    /// <summary>The daily challenge results of a game, kept on the phone (save block <c>challenge.daily</c>).</summary>
    [Serializable]
    public sealed class DailyStats
    {
        public string game;
        /// <summary>The last day (YYYYMMDD) whose challenge was started: one attempt per day.</summary>
        public int lastStartedDay;
        /// <summary>The last finished challenge: its day, stars and whether all its levels were solved ("today's result").</summary>
        public int resultDay;
        public int resultStars;
        public bool resultCompleted;
        /// <summary>The last day whose challenge was completed (the streak goes on from it).</summary>
        public int lastCompletedDay;
        public int streak;
        public int bestStreak;
        /// <summary>Best score (stars, at most 3 per level).</summary>
        public int bestScore;
    }

    /// <summary>
    /// The daily challenge (ErNa-008), pure rules: 5 levels a day, the same for everyone (seed = the local date YYYYMMDD), difficulty
    /// rising from a medium to a hard step (<see cref="ChallengeSettings.dailyTiers"/>), no "Change level", one attempt a day; a
    /// pending challenge stays playable after the date changes but counts for its own day. Score = stars (at most 15). Streak: +1
    /// for a completed challenge on the day after the last completed one, back to 0 when a day is skipped or the challenge is lost.
    /// The date comes from the caller (the app passes the phone's local date): no clock in here.
    /// </summary>
    public static class DailyChallenge
    {
        public const int Levels = 5;
        public const int MaxStars = Levels * 3;

        /// <summary>The local date as YYYYMMDD.</summary>
        public static int DayKey(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

        /// <summary>Days since 2000-01-01 of a YYYYMMDD key (consecutive days differ by 1); -1 for an invalid key.</summary>
        public static int DayIndex(int key)
        {
            int y = key / 10000, m = key / 100 % 100, d = key % 100;
            if (y < 2000 || y > 9999 || m < 1 || m > 12 || d < 1 || d > DateTime.DaysInMonth(y, m)) return -1;
            return (int)(new DateTime(y, m, d) - new DateTime(2000, 1, 1)).TotalDays;
        }

        /// <summary>The run seed of a day: the date itself, so every phone gets the same 5 levels.</summary>
        public static int RunSeed(int dayKey) => dayKey;

        /// <summary>The step (index in the Challenge table) of each daily level, from the tier ids of the settings; unknown ids are skipped.</summary>
        public static List<int> Steps(IChallengeSource source)
        {
            var steps = new List<int>();
            var ids = source?.Settings?.dailyTiers;
            if (ids == null) return steps;
            foreach (var id in ids)
            {
                int i = source.Table.tiers.FindIndex(t => t.id == id);
                if (i >= 0) steps.Add(i);
            }
            return steps;
        }

        /// <summary>A new daily run for <paramref name="dayKey"/>: fixed steps, no "Change level".</summary>
        public static ChallengeRunState NewRun(string game, int dayKey, IReadOnlyList<int> steps)
        {
            var run = ChallengeRunState.Start(game, RunSeed(dayKey));
            run.dayKey = dayKey;
            run.allowChange = false;
            run.fixedSteps = new List<int>(steps);
            return run;
        }

        /// <summary>One attempt per day: today's challenge can start if no challenge of today (or later: clock set back) was started.</summary>
        public static bool CanStart(DailyStats stats, int today) => stats == null || DayIndex(today) > DayIndex(stats.lastStartedDay);

        /// <summary>
        /// Starts today's challenge (marks today as attempted) and saves the run in the block. Null when a daily challenge is still in
        /// progress (it has to be finished first) or today's was already played.
        /// </summary>
        public static ChallengeRunState Begin(ChallengeBlock block, string game, int today, IReadOnlyList<int> steps)
        {
            if (block.ActiveDaily(game) != null || steps == null || steps.Count == 0) return null;
            var stats = block.Daily(game);
            if (!CanStart(stats, today)) return null;
            stats = block.DailyFor(game);
            stats.lastStartedDay = today;
            var run = NewRun(game, today, steps);
            block.SetRun(run);
            return run;
        }

        /// <summary>
        /// The daily run is over (all its levels solved, or lost): today's result, best score and streak; the run leaves the save.
        /// The streak counts the days of the challenges, not the day they were finished on.
        /// </summary>
        public static void Finish(ChallengeBlock block, ChallengeRunState run)
        {
            if (run == null || !run.IsDaily) return;
            run.over = true;
            bool completed = run.IsComplete;
            var s = block.DailyFor(run.game);
            s.resultDay = run.dayKey;
            s.resultStars = run.stars;
            s.resultCompleted = completed;
            s.bestScore = Math.Max(s.bestScore, run.stars);
            if (completed)
            {
                bool consecutive = s.lastCompletedDay != 0 && DayIndex(run.dayKey) - DayIndex(s.lastCompletedDay) == 1;
                s.streak = consecutive ? s.streak + 1 : 1;
                s.lastCompletedDay = run.dayKey;
                s.bestStreak = Math.Max(s.bestStreak, s.streak);
            }
            else s.streak = 0;
            block.EndDaily(run.game);
        }

        /// <summary>The streak shown today: it is still alive only if the last completed challenge is of today or yesterday.</summary>
        public static int CurrentStreak(DailyStats stats, int today)
        {
            if (stats == null || stats.streak <= 0 || stats.lastCompletedDay == 0) return 0;
            int gap = DayIndex(today) - DayIndex(stats.lastCompletedDay);
            return gap >= 0 && gap <= 1 ? stats.streak : 0;
        }

        /// <summary>Today's state for the Home card; <paramref name="stars"/> = today's result when done.</summary>
        public static DailyStatus Status(ChallengeBlock block, string game, int today, out int stars)
        {
            stars = 0;
            if (block.ActiveDaily(game) != null) return DailyStatus.InProgress;
            var s = block.Daily(game);
            if (CanStart(s, today)) return DailyStatus.ToDo;
            if (s.resultDay == s.lastStartedDay) stars = s.resultStars;
            return DailyStatus.Done;
        }
    }
}
