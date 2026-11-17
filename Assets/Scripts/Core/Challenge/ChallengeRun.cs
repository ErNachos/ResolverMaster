using System;
using System.Collections.Generic;

namespace ResolverMaster.Core.Challenge
{
    /// <summary>
    /// A Challenge run (ErNa-007), saved at every move and at every level change: an endless sequence of generated levels; the
    /// score is the sum of the stars (ties: levels cleared). Pure data + rules, no UI: the game screen applies them.
    /// </summary>
    [Serializable]
    public sealed class ChallengeRunState
    {
        public const int UndosPerRun = 3;

        /// <summary>Minigame id (<c>GameInfo.Id</c>): one active run per game.</summary>
        public string game;
        /// <summary>Random when the run starts; every level comes from it (<see cref="ChallengeRules.LevelSeed"/>).</summary>
        public int runSeed;
        /// <summary>1-based level being played.</summary>
        public int level = 1;
        /// <summary>
        /// ErNa-008: times the current level was replaced with "Change level" (part of the level seed; back to 0 on the next level).
        /// </summary>
        public int changes;
        /// <summary>ErNa-008: "Change level" is allowed in this run (not in the daily challenge: its levels are the same for everyone).</summary>
        public bool allowChange = true;
        /// <summary>ErNa-008: the day (YYYYMMDD, phone local date) of a daily challenge run; 0 = the endless Challenge.</summary>
        public int dayKey;
        /// <summary>ErNa-008: the step of each level of a run with a fixed number of levels (daily challenge); empty = endless.</summary>
        public List<int> fixedSteps = new List<int>();

        // ---- the current level (kept so a resumed run never depends on a newer generator)
        public string levelData = "";
        public int minMoves;
        public int maxMoves;
        public int step;
        public string tierId = "";
        public int levelSeed;

        // ---- the play on the current level
        /// <summary>Rotations of the current level (game format, e.g. one digit per cell).</summary>
        public string rotations = "";
        public int moves;
        /// <summary>Moves on the undo stack, oldest first (game format: the Plumber uses the cell index).</summary>
        public List<int> undoHistory = new List<int>();

        // ---- the whole run
        public int undosLeft = UndosPerRun;
        /// <summary>ErNa-008: the lifeline ("Continue" after a loss, with a rewarded ad) was used: one per run.</summary>
        public bool lifelineUsed;
        /// <summary>ErNa-008: the water failed and the run waits for the player (Continue, or the end). Saved: closing the app here keeps the choice.</summary>
        public bool lost;
        public int stars;
        public int levelsCleared;
        public bool over;

        public bool HasLevel => !string.IsNullOrEmpty(levelData);

        /// <summary>ErNa-008: a daily challenge run (it counts for its own day, also when it is finished on a later day).</summary>
        public bool IsDaily => dayKey != 0;

        /// <summary>Levels of a fixed run (daily: 5); 0 = endless.</summary>
        public int LevelCount => fixedSteps != null ? fixedSteps.Count : 0;

        /// <summary>A fixed run with every level solved.</summary>
        public bool IsComplete => LevelCount > 0 && levelsCleared >= LevelCount;

        /// <summary>The step forced on the current level (daily challenge), -1 = by level number.</summary>
        public int FixedStep(int levelNumber) => LevelCount > 0 && levelNumber >= 1 && levelNumber <= LevelCount ? fixedSteps[levelNumber - 1] : -1;

        public static ChallengeRunState Start(string game, int runSeed) => new ChallengeRunState { game = game, runSeed = runSeed };

        /// <summary>The generated level becomes the current one (no move made yet).</summary>
        public void SetLevel(ChallengeLevel l)
        {
            level = l.number;
            levelData = l.data;
            minMoves = l.minMoves;
            maxMoves = l.maxMoves;
            step = l.step;
            tierId = l.tierId;
            levelSeed = l.seed;
            rotations = "";
            moves = 0;
            undoHistory.Clear();
        }

        /// <summary>At the maximum the pieces no longer turn: only undo or opening the water are left.</summary>
        public bool CanMove => !over && !lost && moves < maxMoves;

        /// <summary>Moves left before the maximum.</summary>
        public int MovesLeft => Math.Max(0, maxMoves - moves);

        public void OnMove(int move)
        {
            moves++;
            undoHistory.Add(move);
        }

        /// <summary>3 undos per run; an undo also takes the move off the counter.</summary>
        public bool CanUndo => !over && !lost && undosLeft > 0 && undoHistory.Count > 0;

        public bool TryUndo(out int move)
        {
            move = -1;
            if (!CanUndo) return false;
            move = undoHistory[undoHistory.Count - 1];
            undoHistory.RemoveAt(undoHistory.Count - 1);
            undosLeft--;
            moves = Math.Max(0, moves - 1);
            return true;
        }

        /// <summary>Level solved: its stars add to the score, the next level comes.</summary>
        public void OnSolved(int levelStars)
        {
            stars += Math.Max(0, levelStars);
            levelsCleared++;
            level++;
            changes = 0;
            levelData = "";
            rotations = "";
            moves = 0;
            undoHistory.Clear();
        }

        /// <summary>
        /// Any failure of the water (leak, mixed colors, wrong color, dry fountain) stops the run. ErNa-008: with the lifeline still
        /// unused the run waits for "Continue" (<see cref="lost"/>); otherwise it is over at once.
        /// </summary>
        public void OnFailed()
        {
            if (over) return;
            lost = true;
            if (lifelineUsed) over = true;
        }

        /// <summary>ErNa-008: "Continue" is offered (lost, lifeline unused).</summary>
        public bool CanContinue => lost && !over && !lifelineUsed;

        /// <summary>
        /// ErNa-008, the lifeline (after the ad was watched): the same level again from the start (0 moves), the undos back to
        /// <see cref="UndosPerRun"/>; stars and levels cleared stay. Once per run.
        /// </summary>
        public bool Continue()
        {
            if (!CanContinue) return false;
            lifelineUsed = true;
            lost = false;
            undosLeft = UndosPerRun;
            rotations = "";
            moves = 0;
            undoHistory.Clear();
            return true;
        }

        /// <summary>
        /// ErNa-008: "Change level" (after a rewarded ad, as many times as wanted) only with 0 moves on the level, never in a run
        /// that does not allow it.
        /// </summary>
        public bool CanChangeLevel => allowChange && !over && !lost && HasLevel && moves == 0 && undoHistory.Count == 0;

        /// <summary>
        /// ErNa-008: a new level of the same step replaces the current one (seed = hash(run seed, level, changes + 1)); the level
        /// number does not change. The caller generates and sets the new level (<see cref="SetLevel"/>).
        /// </summary>
        public bool ChangeLevel()
        {
            if (!CanChangeLevel) return false;
            changes++;
            levelData = "";
            rotations = "";
            moves = 0;
            undoHistory.Clear();
            return true;
        }

        /// <summary>The player gave up the lifeline (Home, New run) or had none: the run is over.</summary>
        public void Finish() => over = true;
    }

    /// <summary>The best run of a game (kept on the phone).</summary>
    [Serializable]
    public sealed class ChallengeRecord
    {
        public string game;
        public int stars;
        public int levels;

        /// <summary>The score is the sum of the stars; with the same stars, more levels cleared wins.</summary>
        public static bool IsBetter(int stars, int levels, ChallengeRecord than) =>
            than == null || stars > than.stars || (stars == than.stars && levels > than.levels);
    }

    /// <summary>
    /// Save block of the Challenge (optional: missing = no run, no record). ErNa-008: also the daily challenge (its run in progress
    /// in <see cref="runs"/> with a <see cref="ChallengeRunState.dayKey"/>, its results in <see cref="daily"/>).
    /// </summary>
    [Serializable]
    public sealed class ChallengeBlock
    {
        public List<ChallengeRunState> runs = new List<ChallengeRunState>();
        public List<ChallengeRecord> records = new List<ChallengeRecord>();
        /// <summary>ErNa-008: results of the daily challenge, one entry per game.</summary>
        public List<DailyStats> daily = new List<DailyStats>();

        /// <summary>The endless run in progress of a game (null if none or over).</summary>
        public ChallengeRunState ActiveRun(string game) => Find(game, false);

        /// <summary>ErNa-008: the daily challenge in progress of a game, of any day (null if none).</summary>
        public ChallengeRunState ActiveDaily(string game) => Find(game, true);

        ChallengeRunState Find(string game, bool daily)
        {
            foreach (var r in runs)
                if (r != null && r.game == game && !r.over && r.IsDaily == daily) return r;
            return null;
        }

        /// <summary>Replaces the run of the game of the same kind (one endless run and one daily challenge per game).</summary>
        public void SetRun(ChallengeRunState run)
        {
            runs.RemoveAll(r => r == null || (r.game == run.game && r.IsDaily == run.IsDaily));
            runs.Add(run);
        }

        /// <summary>Removes the endless run of the game.</summary>
        public void EndRun(string game) => runs.RemoveAll(r => r == null || (r.game == game && !r.IsDaily));

        /// <summary>ErNa-008: removes the daily challenge run of the game.</summary>
        public void EndDaily(string game) => runs.RemoveAll(r => r == null || (r.game == game && r.IsDaily));

        /// <summary>ErNa-008: the daily results of a game (null if never played).</summary>
        public DailyStats Daily(string game)
        {
            foreach (var d in daily)
                if (d != null && d.game == game) return d;
            return null;
        }

        /// <summary>ErNa-008: the daily results of a game, created when missing.</summary>
        public DailyStats DailyFor(string game)
        {
            var d = Daily(game);
            if (d != null) return d;
            d = new DailyStats { game = game };
            daily.Add(d);
            return d;
        }

        public ChallengeRecord Record(string game)
        {
            foreach (var r in records)
                if (r != null && r.game == game) return r;
            return null;
        }

        /// <summary>Keeps the better score. True = new record.</summary>
        public bool SubmitScore(string game, int stars, int levels)
        {
            var current = Record(game);
            if (!ChallengeRecord.IsBetter(stars, levels, current) || (stars == 0 && levels == 0)) return false;
            records.RemoveAll(r => r == null || r.game == game);
            records.Add(new ChallengeRecord { game = game, stars = stars, levels = levels });
            return true;
        }

        public void Sanitize()
        {
            runs ??= new List<ChallengeRunState>();
            records ??= new List<ChallengeRecord>();
            daily ??= new List<DailyStats>();
            runs.RemoveAll(r => r == null || string.IsNullOrEmpty(r.game));
            records.RemoveAll(r => r == null || string.IsNullOrEmpty(r.game));
            daily.RemoveAll(d => d == null || string.IsNullOrEmpty(d.game));
            foreach (var r in runs)
            {
                r.undoHistory ??= new List<int>();
                r.levelData ??= "";
                r.rotations ??= "";
                r.tierId ??= "";
                r.fixedSteps ??= new List<int>();
            }
        }
    }
}
