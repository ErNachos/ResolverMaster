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
        /// <summary>Times the current level was replaced (part of the level seed; 0 for now, see Note-ErNa-007).</summary>
        public int changes;

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
        /// <summary>Reserved for the lifeline of ErNa-008 ("Continue" after a loss).</summary>
        public bool lifelineUsed;
        public int stars;
        public int levelsCleared;
        public bool over;

        public bool HasLevel => !string.IsNullOrEmpty(levelData);

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
        public bool CanMove => !over && moves < maxMoves;

        /// <summary>Moves left before the maximum.</summary>
        public int MovesLeft => Math.Max(0, maxMoves - moves);

        public void OnMove(int move)
        {
            moves++;
            undoHistory.Add(move);
        }

        /// <summary>3 undos per run; an undo also takes the move off the counter.</summary>
        public bool CanUndo => !over && undosLeft > 0 && undoHistory.Count > 0;

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
            levelData = "";
            rotations = "";
            moves = 0;
            undoHistory.Clear();
        }

        /// <summary>Any failure of the water (leak, mixed colors, wrong color, dry fountain) ends the run: no lives.</summary>
        public void OnFailed() => over = true;
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

    /// <summary>Save block of the Challenge (optional: missing = no run, no record).</summary>
    [Serializable]
    public sealed class ChallengeBlock
    {
        public List<ChallengeRunState> runs = new List<ChallengeRunState>();
        public List<ChallengeRecord> records = new List<ChallengeRecord>();

        /// <summary>The run in progress of a game (null if none or over).</summary>
        public ChallengeRunState ActiveRun(string game)
        {
            foreach (var r in runs)
                if (r != null && r.game == game && !r.over) return r;
            return null;
        }

        /// <summary>Replaces the run of the game (one active run per game).</summary>
        public void SetRun(ChallengeRunState run)
        {
            runs.RemoveAll(r => r == null || r.game == run.game);
            runs.Add(run);
        }

        public void EndRun(string game) => runs.RemoveAll(r => r == null || r.game == game);

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
            runs.RemoveAll(r => r == null || string.IsNullOrEmpty(r.game));
            records.RemoveAll(r => r == null || string.IsNullOrEmpty(r.game));
            foreach (var r in runs)
            {
                r.undoHistory ??= new List<int>();
                r.levelData ??= "";
                r.rotations ??= "";
                r.tierId ??= "";
            }
        }
    }
}
