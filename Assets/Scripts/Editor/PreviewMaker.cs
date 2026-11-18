using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.Save;
using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Screens;
using ResolverMaster.Games.Plumber;
using ResolverMaster.Games.Penguin;
using ResolverMaster.Games.TrafficJam;
using ResolverMaster.Core.UI.Themes;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Menu Resolver Master > Make Previews: renders Home (with the Plumber card and with the Traffic jam card in the centre), mode choice,
    /// Settings, chapter list, the level grid of every chapter (its theme) and game (one level per chapter before the opening: 3x4, 5x7,
    /// 6x8; a two-color opening halfway through the flow and at its end, a failed opening at the leak effect, win panel), in Italian
    /// and English, at 1080x1920 (9:16) and 1080x2520 (9:21) into Previews/, with the area outside the simulated safe area tinted red,
    /// and measures the layout: safe-area margins, elements off screen or outside the safe area, clipped texts (TextMesh Pro),
    /// overlapping elements, the smallest button, UI sprites drawn bigger than their resolution; on the game screen also the cell size,
    /// the free space above and below the grid, the size of the fountain drawings and the position of the main action button.
    /// TJ-001: Traffic jam too (mode, chapters, levels, two levels per chapter, light bulb, drag, win, Challenge with "Out of moves",
    /// daily challenge, Home with the record) and the short side of the vehicles.
    /// PG-001: Pino too (Home, mode, chapters, levels, two levels per chapter, a slide, a push, a fall, "You are stuck", light bulb,
    /// win, Challenge also stuck, daily challenge, Home with the record) and the drawn size of the penguins.
    /// Report: Previews/previews_report.txt (also in the log). Batch: -executeMethod ResolverMaster.Editor.PreviewMaker.MakePreviewsBatch
    /// </summary>
    public static class PreviewMaker
    {
        public const string OutDir = "Previews";
        /// <summary>Color-blind versions of the game screens (ErNa-006): Italian only, every device, one file per deficiency.</summary>
        public const string ColorBlindDir = OutDir + "/colorblind";

        /// <summary>A preview resolution with the safe area of a typical phone of that shape (pixels, origin bottom-left).</summary>
        public readonly struct Device
        {
            public readonly string Name;
            public readonly int Width, Height;
            public readonly RectInt Safe;

            public Device(string name, int w, int h, RectInt safe)
            {
                Name = name; Width = w; Height = h; Safe = safe;
            }
        }

        /// <summary>
        /// 9:16: classic phone without cutout (safe area = full screen).
        /// 9:21: tall phone with a punch-hole/notch (120 px status/cutout band on top) and a gesture bar (72 px at the bottom).
        /// </summary>
        public static readonly Device[] Devices =
        {
            new Device("9x16", 1080, 1920, new RectInt(0, 0, 1080, 1920)),
            new Device("9x21", 1080, 2520, new RectInt(0, 72, 1080, 2520 - 120 - 72)),
        };

        /// <summary>One preview: a name and how to bring the UI into the wanted state.</summary>
        sealed class Shot
        {
            public string Name;
            /// <summary>Right after the UI is built (push screens, load levels).</summary>
            public Action<AppServices, ScreenManager> Setup;
            /// <summary>After the first layout pass, when rect sizes are known (fit the board, preview states).</summary>
            public Action<AppServices, ScreenManager> AfterLayout;
        }

        static readonly Shot[] Shots =
        {
            HomeShot(ScreenIds.Home, PlumberModule.Id),
            HomeShot("home_trafficjam", "trafficjam"),
            new Shot
            {
                Name = ScreenIds.ModeSelect,
                Setup = (ctx, m) =>
                {
                    m.Get<ModeSelectScreen>(ScreenIds.ModeSelect).SetGame(ctx.Games.Find(PlumberModule.Id));
                    m.PushImmediate(ScreenIds.ModeSelect);
                },
            },
            new Shot { Name = ScreenIds.Settings, Setup = (ctx, m) => m.PushImmediate(ScreenIds.Settings) },
            new Shot
            {
                Name = "chapters",
                Setup = (ctx, m) =>
                {
                    SampleProgress(ctx);
                    m.Get<ChapterSelectScreen>(ScreenIds.ChapterSelect).Open(ctx.Games.FindModule(PlumberModule.Id));
                    m.PushImmediate(ScreenIds.ChapterSelect);
                },
            },
            LevelsShot(1),
            LevelsShot(2),
            LevelsShot(3),
            // ErNa-006: the themes of chapters 4-9
            LevelsShot(4), LevelsShot(5), LevelsShot(6), LevelsShot(7), LevelsShot(8), LevelsShot(9),
            // ErNa-009: chapters 10-13 (one page each of the swipeable level screen)
            LevelsShot(10), LevelsShot(11), LevelsShot(12), LevelsShot(13),
            // before the opening, one level per chapter (theme): 3x4 (level 1), 5x7 (level 14), 6x8 (level 27)
            GameShot("game_c1", 0, BoardPreview.Start, win: false),
            GameShot("game_c2", 13, BoardPreview.Start, win: false),
            GameShot("game_c3", 26, BoardPreview.Start, win: false),
            GameShot("game_flowing", 26, BoardPreview.ActionFlowing, win: false),
            GameShot("game_open_ok", 26, BoardPreview.ActionSuccess, win: false),
            GameShot("game_open_leak", 9, BoardPreview.ActionFailure, win: false),
            GameShot("game_win", 0, BoardPreview.Solved, win: true),
            // ErNa-007: the Challenge (2 moves left: red counter; pause; end of the run with a new record), mode choice with a run
            // in progress, confirmation of a new run, Home with the record on the card
            ChallengeShot("challenge_game", 2, null),
            ChallengeShot("challenge_pause", 3, (ctx, g) => g.ShowPauseForPreview()),
            ChallengeShot("challenge_over", 3, (ctx, g) =>
            {
                var over = ctx.Data.challenge.ActiveRun(PlumberModule.Id);
                over.stars = 41;
                over.levelsCleared = 17;
                ctx.Data.challenge.SubmitScore(PlumberModule.Id, over.stars, over.levelsCleared);
                g.ShowRunOver(over, newRecord: true);
            }),
            // ErNa-008: the daily challenge: Home card (done today, streak), level 3 of 5, lost with Continue, completed
            new Shot
            {
                Name = "home_daily",
                Setup = (ctx, m) =>
                {
                    SampleRun(ctx, 5);
                    var stats = ctx.Data.challenge.DailyFor(PlumberModule.Id);
                    stats.lastStartedDay = stats.resultDay = stats.lastCompletedDay = ctx.Today;
                    stats.resultStars = 12;
                    stats.resultCompleted = true;
                    stats.streak = 4;
                    stats.bestStreak = 6;
                    stats.bestScore = 14;
                },
                AfterLayout = (ctx, m) =>
                {
                    var home = m.Get<HomeScreen>(ScreenIds.Home);
                    home.OnShow();
                    home.ShowCard(Math.Max(0, ctx.Games.Games.ToList().FindIndex(g => g.Id == PlumberModule.Id)));
                },
            },
            DailyShot("daily_game", 2, null),
            DailyShot("daily_continue", 2, (ctx, g) =>
            {
                g.Run.OnFailed();
                g.ShowDailyOver(g.Run);
            }),
            DailyShot("daily_over", 4, (ctx, g) =>
            {
                var run = g.Run;
                run.OnSolved(3);
                ResolverMaster.Core.Challenge.DailyChallenge.Finish(ctx.Data.challenge, run);
                g.ShowDailyOver(run);
            }),
            // ErNa-008: a level with 0 moves: "Change level" (ad badge) enabled
            ChallengeShot("challenge_change", int.MaxValue, null),
            // ErNa-008: lost with the lifeline unused: "Continue" (ad icon) on the end panel
            ChallengeShot("challenge_continue", 3, (ctx, g) =>
            {
                var lost = ctx.Data.challenge.ActiveRun(PlumberModule.Id);
                lost.OnFailed();
                g.ShowRunOver(lost, newRecord: false);
            }),
            new Shot
            {
                Name = "mode_select_run",
                Setup = (ctx, m) =>
                {
                    SampleRun(ctx, 5);
                    m.Get<ModeSelectScreen>(ScreenIds.ModeSelect).SetGame(ctx.Games.Find(PlumberModule.Id));
                    m.PushImmediate(ScreenIds.ModeSelect);
                },
            },
            new Shot
            {
                Name = "mode_confirm_new_run",
                Setup = (ctx, m) =>
                {
                    SampleRun(ctx, 5);
                    m.Get<ModeSelectScreen>(ScreenIds.ModeSelect).SetGame(ctx.Games.Find(PlumberModule.Id));
                    m.PushImmediate(ScreenIds.ModeSelect);
                },
                AfterLayout = (ctx, m) => m.Get<ModeSelectScreen>(ScreenIds.ModeSelect).ShowConfirmForPreview(),
            },
            new Shot
            {
                Name = "home_challenge",
                Setup = (ctx, m) => SampleRun(ctx, 5),
                AfterLayout = (ctx, m) =>
                {
                    var home = m.Get<HomeScreen>(ScreenIds.Home);
                    home.OnShow();
                    home.ShowCard(Math.Max(0, ctx.Games.Games.ToList().FindIndex(g => g.Id == PlumberModule.Id)));
                },
            },
            // ErNa-007: two pieces fixed by the light bulb (glowing outline), and the test rewarded ad (the wait is over)
            new Shot
            {
                Name = "game_hint",
                Setup = (ctx, m) =>
                {
                    m.Get<GameScreen>(ScreenIds.Game).StartLevel(ctx.Games.FindModule(PlumberModule.Id), 40);
                    m.PushImmediate(ScreenIds.Game);
                },
                AfterLayout = (ctx, m) =>
                {
                    var board = m.Get<GameScreen>(ScreenIds.Game).Board;
                    board.Refit();
                    board.SnapVisuals();
                    board.ApplyHint();
                    board.ApplyHint();
                    board.SnapVisuals();
                },
            },
            new Shot
            {
                Name = "ad_test",
                Setup = (ctx, m) =>
                {
                    m.Get<GameScreen>(ScreenIds.Game).StartLevel(ctx.Games.FindModule(PlumberModule.Id), 20);
                    m.PushImmediate(ScreenIds.Game);
                },
                AfterLayout = (ctx, m) => ctx.TestAds.ShowForPreview(),
            },
            // ErNa-006: chapters 4-9 (bridges, locked pieces, 3 and 4 colors, 7x9), two levels each from chapter 6 on (also color-blind)
            GameShot("game_c4_33", 32, BoardPreview.Start, win: false),
            GameShot("game_c5_44", 43, BoardPreview.Start, win: false),
            GameShot("game_c6_47", 46, BoardPreview.Start, win: false),
            GameShot("game_c6_53", 52, BoardPreview.ActionSuccess, win: false),
            GameShot("game_c7_58", 57, BoardPreview.Start, win: false),
            GameShot("game_c7_63", 62, BoardPreview.Start, win: false),
            GameShot("game_c8_66", 65, BoardPreview.Start, win: false),
            GameShot("game_c8_72", 71, BoardPreview.ActionFlowing, win: false),
            GameShot("game_c9_75", 74, BoardPreview.Start, win: false),
            GameShot("game_c9_81", 80, BoardPreview.ActionSuccess, win: false),
            // ErNa-009: two levels per new chapter (branching routes; a cross used as a divider, opened, in level 87)
            GameShot("game_c10_82", 81, BoardPreview.Start, win: false),
            GameShot("game_c10_87", 86, BoardPreview.ActionSuccess, win: false),
            GameShot("game_c10_90", 89, BoardPreview.Start, win: false),
            GameShot("game_c11_91", 90, BoardPreview.Start, win: false),
            GameShot("game_c11_99", 98, BoardPreview.ActionSuccess, win: false),
            GameShot("game_c12_100", 99, BoardPreview.Start, win: false),
            GameShot("game_c12_108", 107, BoardPreview.ActionFlowing, win: false),
            GameShot("game_c13_109", 108, BoardPreview.Start, win: false),
            GameShot("game_c13_117", 116, BoardPreview.ActionSuccess, win: false),
            // ErNa-006: a hand-made level with a bridge (orange east-west, blue north-south), a decoy cross and locked pieces
            CustomShot("game_bridge", BridgeLevel, BoardPreview.Start),
            CustomShot("game_bridge_flowing", BridgeLevel, BoardPreview.ActionFlowing),
            CustomShot("game_bridge_ok", BridgeLevel, BoardPreview.ActionSuccess),
            // the lock plate on the other terrains (garden, farm)
            CustomShot("game_locked_c1", BridgeLevel, BoardPreview.Start, storyIndex: 0),
            CustomShot("game_locked_c2", BridgeLevel, BoardPreview.Start, storyIndex: 9),

            // TJ-001: Traffic jam: mode choice, chapters, levels, two levels per chapter (also color-blind), partial, light bulb, drag, win
            new Shot
            {
                Name = "tj_mode_select",
                Setup = (ctx, m) =>
                {
                    m.Get<ModeSelectScreen>(ScreenIds.ModeSelect).SetGame(ctx.Games.Find(TrafficJamModule.Id));
                    m.PushImmediate(ScreenIds.ModeSelect);
                },
            },
            new Shot
            {
                Name = "tj_chapters",
                Setup = (ctx, m) =>
                {
                    TjSampleProgress(ctx);
                    m.Get<ChapterSelectScreen>(ScreenIds.ChapterSelect).Open(ctx.Games.FindModule(TrafficJamModule.Id));
                    m.PushImmediate(ScreenIds.ChapterSelect);
                },
            },
            TjLevelsShot(1), TjLevelsShot(4), TjLevelsShot(7),
            TjGameShot("game_tj_c1_1", 0), TjGameShot("game_tj_c1_9", 8),
            TjGameShot("game_tj_c2_10", 9), TjGameShot("game_tj_c2_18", 17),
            TjGameShot("game_tj_c3_19", 18), TjGameShot("game_tj_c3_27", 26),
            TjGameShot("game_tj_c4_28", 27), TjGameShot("game_tj_c4_36", 35),
            TjGameShot("game_tj_c5_37", 36), TjGameShot("game_tj_c5_45", 44),
            TjGameShot("game_tj_c6_46", 45), TjGameShot("game_tj_c6_54", 53),
            TjGameShot("game_tj_c7_55", 54), TjGameShot("game_tj_c7_63", 62),
            TjGameShot("game_tj_partial", 40, BoardPreview.Partial),
            TjGameShot("game_tj_hint", 20, after: b => ((ResolverMaster.Games.TrafficJam.View.TrafficBoardView)b).PreviewHintArrow()),
            TjGameShot("game_tj_drag", 12, after: b => ((ResolverMaster.Games.TrafficJam.View.TrafficBoardView)b).PreviewDrag()),
            TjGameShot("game_tj_win", 2, BoardPreview.Solved, win: true),
            // TJ-002: a target car halfway out of its exit (happy), on a 4x6 and on a 7x9 grid
            TjGameShot("game_tj_exit_4x6", 0, after: b => ((ResolverMaster.Games.TrafficJam.View.TrafficBoardView)b).PreviewExit()),
            TjGameShot("game_tj_exit_7x9", 62, after: b => ((ResolverMaster.Games.TrafficJam.View.TrafficBoardView)b).PreviewExit()),
            // TJ-001: Traffic jam Challenge: playing, out of moves with undos left (panel), out of moves without (run over), daily challenge, Home
            TjChallengeShot("challenge_tj_game", 3, 2),
            TjChallengeShot("challenge_tj_out_of_moves", 0, 2),
            TjChallengeShot("challenge_tj_lost", 0, 0),
            new Shot
            {
                Name = "daily_tj_game",
                Setup = (ctx, m) =>
                {
                    var module = ctx.Games.FindModule(TrafficJamModule.Id);
                    var run = ResolverMaster.Core.Challenge.DailyChallenge.Begin(ctx.Data.challenge, TrafficJamModule.Id, ctx.Today,
                        ResolverMaster.Core.Challenge.DailyChallenge.Steps(module.Challenge));
                    run.OnSolved(3);
                    m.Get<GameScreen>(ScreenIds.Game).StartChallenge(module, run);
                    m.PushImmediate(ScreenIds.Game);
                },
                AfterLayout = (ctx, m) =>
                {
                    var game = m.Get<GameScreen>(ScreenIds.Game);
                    game.Board.Refit();
                    game.Board.SnapVisuals();
                },
            },
            new Shot
            {
                Name = "home_tj_challenge",
                Setup = (ctx, m) => TjSampleRun(ctx, 5, 2),
                AfterLayout = (ctx, m) =>
                {
                    var home = m.Get<HomeScreen>(ScreenIds.Home);
                    home.OnShow();
                    home.ShowCard(Math.Max(0, ctx.Games.Games.ToList().FindIndex(g => g.Id == TrafficJamModule.Id)));
                },
            },

            // PG-001: Pino: Home, mode choice, chapters, levels, two levels per chapter (also color-blind), a slide, a push, a fall,
            // stuck, light bulb (one and two penguins), win, Challenge (playing and stuck), daily challenge, Home with the record
            HomeShot("home_penguin", PenguinModule.Id),
            new Shot
            {
                Name = "pg_mode_select",
                Setup = (ctx, m) =>
                {
                    m.Get<ModeSelectScreen>(ScreenIds.ModeSelect).SetGame(ctx.Games.Find(PenguinModule.Id));
                    m.PushImmediate(ScreenIds.ModeSelect);
                },
            },
            new Shot
            {
                Name = "pg_chapters",
                Setup = (ctx, m) =>
                {
                    PgSampleProgress(ctx);
                    m.Get<ChapterSelectScreen>(ScreenIds.ChapterSelect).Open(ctx.Games.FindModule(PenguinModule.Id));
                    m.PushImmediate(ScreenIds.ChapterSelect);
                },
            },
            PgLevelsShot(1), PgLevelsShot(4), PgLevelsShot(7),
            PgGameShot("game_pg_c1_1", 0), PgGameShot("game_pg_c1_9", 8),
            PgGameShot("game_pg_c2_10", 9), PgGameShot("game_pg_c2_18", 17),
            PgGameShot("game_pg_c3_19", 18), PgGameShot("game_pg_c3_27", 26),
            PgGameShot("game_pg_c4_28", 27), PgGameShot("game_pg_c4_36", 35),
            PgGameShot("game_pg_c5_37", 36), PgGameShot("game_pg_c5_45", 44),
            PgGameShot("game_pg_c6_46", 45), PgGameShot("game_pg_c6_54", 53),
            PgGameShot("game_pg_c7_55", 54), PgGameShot("game_pg_c7_63", 62),
            PgGameShot("game_pg_partial", 40, BoardPreview.Partial),
            PgGameShot("game_pg_slide", 12, after: b => ((ResolverMaster.Games.Penguin.View.PenguinBoardView)b).PreviewSlide(0.5f)),
            PgGameShot("game_pg_push", 36, after: b => ((ResolverMaster.Games.Penguin.View.PenguinBoardView)b).PreviewSlide(0.75f)),
            PgGameShot("game_pg_fall", 35, after: b => ((ResolverMaster.Games.Penguin.View.PenguinBoardView)b).PreviewFall()),
            PgGameShot("game_pg_hint", 20, after: b => ((ResolverMaster.Games.Penguin.View.PenguinBoardView)b).PreviewHintArrow()),
            PgGameShot("game_pg_hint_2p", 50, after: b => ((ResolverMaster.Games.Penguin.View.PenguinBoardView)b).PreviewHintArrow()),
            PgGameShot("game_pg_stuck", -1, after: b => ((ResolverMaster.Games.Penguin.View.PenguinBoardView)b).PreviewStuck()),
            PgGameShot("game_pg_win", 2, BoardPreview.Solved, win: true),
            PgChallengeShot("challenge_pg_game", 23, stuck: false),
            PgChallengeShot("challenge_pg_stuck", 41, stuck: true),
            new Shot
            {
                Name = "daily_pg_game",
                Setup = (ctx, m) =>
                {
                    var module = ctx.Games.FindModule(PenguinModule.Id);
                    var run = ResolverMaster.Core.Challenge.DailyChallenge.Begin(ctx.Data.challenge, PenguinModule.Id, ctx.Today,
                        ResolverMaster.Core.Challenge.DailyChallenge.Steps(module.Challenge));
                    run.OnSolved(3);
                    m.Get<GameScreen>(ScreenIds.Game).StartChallenge(module, run);
                    m.PushImmediate(ScreenIds.Game);
                },
                AfterLayout = (ctx, m) =>
                {
                    var game = m.Get<GameScreen>(ScreenIds.Game);
                    game.Board.Refit();
                    game.Board.SnapVisuals();
                },
            },
            new Shot
            {
                Name = "home_pg_challenge",
                Setup = (ctx, m) =>
                {
                    PgSampleRun(ctx, 23, false);
                    ctx.Data.challenge.SubmitScore(PenguinModule.Id, 27, 11);
                },
                AfterLayout = (ctx, m) =>
                {
                    var home = m.Get<HomeScreen>(ScreenIds.Home);
                    home.OnShow();
                    home.ShowCard(Math.Max(0, ctx.Games.Games.ToList().FindIndex(g => g.Id == PenguinModule.Id)));
                },
            },
        };

        // ------------------------------------------------------------------ Pino shots (PG-001)

        /// <summary>A Pino level (<paramref name="levelIndex"/> -1: the first Story level from chapter 4 on with a way into "You are stuck").</summary>
        static Shot PgGameShot(string name, int levelIndex, BoardPreview state = BoardPreview.Start, bool win = false, Action<PuzzleBoard> after = null) => new Shot
        {
            Name = name,
            Setup = (ctx, m) =>
            {
                var module = (PenguinModule)ctx.Games.FindModule(PenguinModule.Id);
                int index = levelIndex >= 0 ? levelIndex : PgStuckLevel(module);
                m.Get<GameScreen>(ScreenIds.Game).StartLevel(module, index);
                m.PushImmediate(ScreenIds.Game);
            },
            AfterLayout = (ctx, m) =>
            {
                var game = m.Get<GameScreen>(ScreenIds.Game);
                game.Board.Refit();
                game.Board.SnapVisuals();
                game.Board.PreparePreview(state);
                after?.Invoke(game.Board);
                if (win) game.ShowWin(animate: false);
            },
        };

        static int PgStuckLevel(PenguinModule module)
        {
            var levels = module.StoryLevels;
            for (int i = 27; i < levels.Count; i++)
            {
                var p = new PenguinPuzzle();
                p.Load(levels[i]);
                if (p.Space.PathToStuck().Count > 0) return i;
            }
            return 0;
        }

        static Shot PgLevelsShot(int chapter) => new Shot
        {
            Name = "pg_levels_c" + chapter,
            Setup = (ctx, m) =>
            {
                PgSampleProgress(ctx);
                m.Get<LevelSelectScreen>(ScreenIds.LevelSelect).Open(ctx.Games.FindModule(PenguinModule.Id), chapter);
                m.PushImmediate(ScreenIds.LevelSelect);
            },
        };

        /// <summary>Sample progress: chapters 1-6 complete with mixed stars (chapter 7 open).</summary>
        static void PgSampleProgress(AppServices ctx)
        {
            var book = ctx.Games.FindModule(PenguinModule.Id).Story;
            for (int i = 0; i < book.LevelCount - 9; i++) ctx.Data.progress.SetBest(StoryProgress.Mode, book.IdOf(i), 1 + (i * 5 % 3));
        }

        /// <summary>
        /// A sample Pino run (fixed seed) at <paramref name="level"/>: two moves of the solution, or (<paramref name="stuck"/>) the way into a
        /// state with no solution left, with 2 undos.
        /// </summary>
        static ResolverMaster.Core.Challenge.ChallengeRunState PgSampleRun(AppServices ctx, int level, bool stuck)
        {
            var module = ctx.Games.FindModule(PenguinModule.Id);
            var run = ResolverMaster.Core.Challenge.ChallengeRunState.Start(PenguinModule.Id, 4242);
            run.SetLevel(ResolverMaster.Core.Challenge.ChallengeRules.Generate(module.Challenge, run.runSeed, level, 0));
            run.stars = 2 * (level - 1);
            run.levelsCleared = level - 1;
            run.undosLeft = 2;
            var p = new PenguinPuzzle();
            p.Load(PenguinChallenge.ToLevel(run.level, run.tierId, run.levelSeed, run.levelData, run.minMoves));
            var dirs = stuck ? p.Space.PathToStuck() : p.Space.Solution(p.State).Take(2).ToList();
            foreach (int d in dirs)
                if (p.Swipe(d)) run.OnMove(d);
            ctx.Data.challenge.SetRun(run);
            return run;
        }

        static Shot PgChallengeShot(string name, int level, bool stuck) => new Shot
        {
            Name = name,
            Setup = (ctx, m) =>
            {
                var run = PgSampleRun(ctx, level, stuck);
                ctx.Data.challenge.SubmitScore(PenguinModule.Id, 27, 11);
                m.Get<GameScreen>(ScreenIds.Game).StartChallenge(ctx.Games.FindModule(PenguinModule.Id), run);
                m.PushImmediate(ScreenIds.Game);
            },
            AfterLayout = (ctx, m) =>
            {
                var game = m.Get<GameScreen>(ScreenIds.Game);
                game.Board.Refit();
                game.Board.SnapVisuals();
            },
        };

        // ------------------------------------------------------------------ Traffic jam shots (TJ-001)

        static Shot TjGameShot(string name, int levelIndex, BoardPreview state = BoardPreview.Start, bool win = false, Action<PuzzleBoard> after = null) => new Shot
        {
            Name = name,
            Setup = (ctx, m) =>
            {
                m.Get<GameScreen>(ScreenIds.Game).StartLevel(ctx.Games.FindModule(TrafficJamModule.Id), levelIndex);
                m.PushImmediate(ScreenIds.Game);
            },
            AfterLayout = (ctx, m) =>
            {
                var game = m.Get<GameScreen>(ScreenIds.Game);
                game.Board.Refit();
                game.Board.SnapVisuals();
                game.Board.PreparePreview(state);
                after?.Invoke(game.Board);
                if (win) game.ShowWin(animate: false);
            },
        };

        static Shot TjLevelsShot(int chapter) => new Shot
        {
            Name = "tj_levels_c" + chapter,
            Setup = (ctx, m) =>
            {
                TjSampleProgress(ctx);
                m.Get<LevelSelectScreen>(ScreenIds.LevelSelect).Open(ctx.Games.FindModule(TrafficJamModule.Id), chapter);
                m.PushImmediate(ScreenIds.LevelSelect);
            },
        };

        /// <summary>Sample progress: chapters 1-6 complete with mixed stars (chapter 7 open), so every chapter shows its levels.</summary>
        static void TjSampleProgress(AppServices ctx)
        {
            var book = ctx.Games.FindModule(TrafficJamModule.Id).Story;
            for (int i = 0; i < book.LevelCount - 9; i++) ctx.Data.progress.SetBest(StoryProgress.Mode, book.IdOf(i), 1 + (i * 7 % 3));
        }

        /// <summary>
        /// A sample Traffic jam run (fixed seed) at level 23 with a record; <paramref name="movesLeft"/> before the maximum (real moves:
        /// one vehicle slid back and forth), <paramref name="undosLeft"/> undos.
        /// </summary>
        static ResolverMaster.Core.Challenge.ChallengeRunState TjSampleRun(AppServices ctx, int movesLeft, int undosLeft)
        {
            var module = ctx.Games.FindModule(TrafficJamModule.Id);
            var run = ResolverMaster.Core.Challenge.ChallengeRunState.Start(TrafficJamModule.Id, 4242);
            run.SetLevel(ResolverMaster.Core.Challenge.ChallengeRules.Generate(module.Challenge, run.runSeed, 23, 0));
            run.stars = 50;
            run.levelsCleared = 22;
            run.undosLeft = undosLeft;
            var layout = TrafficLayout.Parse(run.levelData, out ulong state);
            var buffer = new TrafficMove[layout.MaxMovesPerState];
            layout.Moves(state, buffer, exits: false);
            var there = buffer[0];
            var back = new TrafficMove(there.Vehicle, there.To, there.From);
            for (int i = 0; i < run.maxMoves - movesLeft; i++) run.OnMove((i % 2 == 0 ? there : back).Encode());
            ctx.Data.challenge.SetRun(run);
            ctx.Data.challenge.SubmitScore(TrafficJamModule.Id, 31, 14);
            return run;
        }

        static Shot TjChallengeShot(string name, int movesLeft, int undosLeft) => new Shot
        {
            Name = name,
            Setup = (ctx, m) =>
            {
                var run = TjSampleRun(ctx, movesLeft, undosLeft);
                m.Get<GameScreen>(ScreenIds.Game).StartChallenge(ctx.Games.FindModule(TrafficJamModule.Id), run);
                m.PushImmediate(ScreenIds.Game);
            },
            AfterLayout = (ctx, m) =>
            {
                var game = m.Get<GameScreen>(ScreenIds.Game);
                game.Board.Refit();
                game.Board.SnapVisuals();
            },
        };

        const string BridgeLevel = "C1 S2 C0 I0 C3/S1@1 B0 I0 I1# E3@1/T0 I1 C2 X0 I0/C3 E0 I1 C1# T2/I0 C0 C1 I1 C2/C2 T1 I0 C0 X0/C1 I1 C3 T3 C0";

        /// <summary>A game shot on a hand-made level (cells in the PlumberLevelCodec form), with the theme of the chapter of Story level <paramref name="storyIndex"/>.</summary>
        static Shot CustomShot(string name, string cells, BoardPreview state, int storyIndex = 18) => new Shot
        {
            Name = name,
            Setup = (ctx, m) =>
            {
                var game = m.Get<GameScreen>(ScreenIds.Game);
                game.StartLevel(ctx.Games.FindModule(PlumberModule.Id), storyIndex);
                var grid = PlumberLevelCodec.Parse(cells);
                var sol = PlumberSolver.Solve(grid);
                ((ResolverMaster.Games.Plumber.View.PlumberBoardView)game.Board).LoadLevel(new PlumberLevel
                {
                    number = storyIndex + 1, chapter = storyIndex / StoryBook.LevelsPerChapter + 1, seed = 1, width = grid.Width, height = grid.Height, minMoves = sol.MinMoves, path = sol.PathLength, cells = cells,
                });
                m.PushImmediate(ScreenIds.Game);
            },
            AfterLayout = (ctx, m) =>
            {
                var game = m.Get<GameScreen>(ScreenIds.Game);
                game.Board.Refit();
                game.Board.SnapVisuals();
                game.Board.PreparePreview(state);
            },
        };

        /// <summary>A sample Challenge run (fixed seed) at level 23 with a record of 28 stars; <paramref name="movesLeft"/> before the maximum.</summary>
        static ResolverMaster.Core.Challenge.ChallengeRunState SampleRun(AppServices ctx, int movesLeft)
        {
            var module = ctx.Games.FindModule(PlumberModule.Id);
            var run = ResolverMaster.Core.Challenge.ChallengeRunState.Start(PlumberModule.Id, 4242);
            run.SetLevel(ResolverMaster.Core.Challenge.ChallengeRules.Generate(module.Challenge, run.runSeed, 23, 0));
            run.stars = 52;
            run.levelsCleared = 22;
            run.undosLeft = 2;
            var grid = PlumberLevelCodec.Parse(run.levelData);
            int cell = Array.FindIndex(grid.Pieces, p => p.IsRotatable);
            for (int i = 0; i < run.maxMoves - movesLeft; i++) run.OnMove(cell);
            ctx.Data.challenge.SetRun(run);
            ctx.Data.challenge.SubmitScore(PlumberModule.Id, 28, 12);
            return run;
        }

        /// <summary>ErNa-008: the fixed date of the previews (daily challenge).</summary>
        static readonly DateTime PreviewDate = new DateTime(2026, 11, 17, 12, 0, 0);

        /// <summary>
        /// ErNa-008: today's daily challenge with <paramref name="solved"/> levels solved (3, 2, 3, 3 stars...), a streak of 3 from
        /// yesterday, then <paramref name="after"/>.
        /// </summary>
        static Shot DailyShot(string name, int solved, Action<AppServices, GameScreen> after) => new Shot
        {
            Name = name,
            Setup = (ctx, m) =>
            {
                var module = ctx.Games.FindModule(PlumberModule.Id);
                var block = ctx.Data.challenge;
                var stats = block.DailyFor(PlumberModule.Id);
                stats.lastStartedDay = stats.resultDay = stats.lastCompletedDay = 20261116;
                stats.streak = 3;
                stats.bestStreak = 5;
                var run = ResolverMaster.Core.Challenge.DailyChallenge.Begin(block, PlumberModule.Id, ctx.Today,
                    ResolverMaster.Core.Challenge.DailyChallenge.Steps(module.Challenge));
                for (int i = 0; i < solved; i++) run.OnSolved(i % 4 == 1 ? 2 : 3);
                m.Get<GameScreen>(ScreenIds.Game).StartChallenge(module, run);
                m.PushImmediate(ScreenIds.Game);
            },
            AfterLayout = (ctx, m) =>
            {
                var game = m.Get<GameScreen>(ScreenIds.Game);
                game.Board.Refit();
                game.Board.SnapVisuals();
                after?.Invoke(ctx, game);
            },
        };

        static Shot ChallengeShot(string name, int movesLeft, Action<AppServices, GameScreen> after) => new Shot
        {
            Name = name,
            Setup = (ctx, m) =>
            {
                var run = SampleRun(ctx, movesLeft);
                m.Get<GameScreen>(ScreenIds.Game).StartChallenge(ctx.Games.FindModule(PlumberModule.Id), run);
                m.PushImmediate(ScreenIds.Game);
            },
            AfterLayout = (ctx, m) =>
            {
                var game = m.Get<GameScreen>(ScreenIds.Game);
                game.Board.Refit();
                game.Board.SnapVisuals();
                after?.Invoke(ctx, game);
            },
        };

        /// <summary>The level grid of a chapter (its theme backdrop), with the sample progress.</summary>
        static Shot LevelsShot(int chapter) => new Shot
        {
            Name = "levels_c" + chapter,
            Setup = (ctx, m) =>
            {
                SampleProgress(ctx);
                m.Get<LevelSelectScreen>(ScreenIds.LevelSelect).Open(ctx.Games.FindModule(PlumberModule.Id), chapter);
                m.PushImmediate(ScreenIds.LevelSelect);
            },
        };

        /// <summary>Sample progress: chapter 1 complete with mixed stars, chapter 2 levels 10-12 solved (3, 2, 1 stars), chapter 3 locked.</summary>
        static void SampleProgress(AppServices ctx)
        {
            var book = ctx.Games.FindModule(PlumberModule.Id).Story;
            int[] stars = { 3, 3, 2, 3, 1, 2, 3, 3, 2, 3, 2, 1 };
            for (int i = 0; i < stars.Length && i < book.LevelCount; i++) ctx.Data.progress.SetBest(StoryProgress.Mode, book.IdOf(i), stars[i]);
        }

        /// <summary>Home with the card of <paramref name="gameId"/> in the centre.</summary>
        static Shot HomeShot(string name, string gameId) => new Shot
        {
            Name = name,
            AfterLayout = (ctx, m) =>
            {
                var home = m.Get<HomeScreen>(ScreenIds.Home);
                home.ShowCard(Math.Max(0, ctx.Games.Games.ToList().FindIndex(g => g.Id == gameId)));
            },
        };

        static Shot GameShot(string name, int levelIndex, BoardPreview state, bool win) => new Shot
        {
            Name = name,
            Setup = (ctx, m) =>
            {
                m.Get<GameScreen>(ScreenIds.Game).StartLevel(ctx.Games.FindModule(PlumberModule.Id), levelIndex);
                m.PushImmediate(ScreenIds.Game);
            },
            AfterLayout = (ctx, m) =>
            {
                var game = m.Get<GameScreen>(ScreenIds.Game);
                game.Board.Refit();
                game.Board.SnapVisuals();
                game.Board.PreparePreview(state);   // leaves the board frozen in the requested state (no time passes in edit mode)
                if (win) game.ShowWin(animate: false);
            },
        };

        [MenuItem("Resolver Master/Make Previews")]
        public static void MakePreviews() => Make();

        public static void MakePreviewsBatch()
        {
            try
            {
                Make();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// PG-001: an optional filter for quick checks: <c>-previewFilter a,b</c> on the command line keeps only the shots whose name
        /// contains one of the parts (no filter = every shot, as always before a commit).
        /// </summary>
        static bool Selected(Shot shot)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-previewFilter");
            if (i < 0 || i + 1 >= args.Length) return true;
            return args[i + 1].Split(',').Any(part => part.Length > 0 && shot.Name.Contains(part));
        }

        public static List<string> Make()
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(ColorBlindDir);
            var files = new List<string>();
            var report = new StringBuilder("Resolver Master previews - layout measurements\n");
            AppendPaletteReport(report);
            // batchmode starts on an unsaved untitled scene, which cannot host an additive one: replace it there
            bool additive = !string.IsNullOrEmpty(SceneManager.GetActiveScene().path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            try
            {
                // load AFTER NewScene: a Single NewScene unloads unused assets, leaving earlier-loaded sprite references null (ErNa-001)
                var style = AssetDatabase.LoadAssetAtPath<UiKitStyle>(UiKitAssets.StylePath);
                var icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.IconLibraryPath);
                if (style == null || icons == null) throw new InvalidOperationException("UI kit assets missing: run Resolver Master > Setup > Apply All");
                icons.InvalidateCache();
                foreach (var device in Devices)
                {
                    report.Append($"\n== {device.Width}x{device.Height} ({device.Name}) safe area x {device.Safe.x}..{device.Safe.xMax}, y {device.Safe.y}..{device.Safe.yMax} " +
                                  $"-> margins top {device.Height - device.Safe.yMax} px, bottom {device.Safe.y} px, left {device.Safe.x} px, right {device.Width - device.Safe.xMax} px\n");
                    foreach (var lang in LanguageCodes.All)
                    foreach (var shot in Shots.Where(Selected))
                    {
                        string file = $"{OutDir}/{shot.Name}_{LanguageCodes.ToCode(lang)}_{device.Width}x{device.Height}.png";
                        var issues = Shoot(scene, style, icons, device, lang, shot, file, out string margins, out string board);
                        files.Add(file);
                        report.Append($"  {Path.GetFileName(file)}: {margins}; {(issues.Count == 0 ? "no issues" : issues.Count + " issue(s)")}\n");
                        if (board != null) report.Append("    board: ").Append(board).Append('\n');
                        foreach (var i in issues) report.Append("    - ").Append(i).Append('\n');
                    }
                }
            }
            finally
            {
                if (additive) EditorSceneManager.CloseScene(scene, true);
            }
            File.WriteAllText($"{OutDir}/previews_report.txt", report.ToString(), new UTF8Encoding(false));
            Debug.Log("PreviewMaker: " + files.Count + " previews in " + Path.GetFullPath(OutDir) + "\n" + report);
            return files;
        }

        static List<string> Shoot(Scene scene, UiKitStyle style, IconLibrary icons, Device device, Language lang, Shot shot, string file, out string margins, out string board)
        {
            int w = device.Width, h = device.Height;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            var camGo = new GameObject("PreviewCamera");
            var canvasGo = new GameObject("PreviewCanvas", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            SceneManager.MoveGameObjectToScene(camGo, scene);
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = h / 2f;
                cam.transform.position = new Vector3(0, 0, -100);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.cullingMask = 1 << UiFactory.UiLayer;
                cam.nearClipPlane = 1;
                cam.farClipPlane = 1000;
                cam.targetTexture = rt;

                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = cam;
                var root = (RectTransform)canvasGo.transform;
                root.sizeDelta = new Vector2(w, h);
                root.position = Vector3.zero;

                var ctx = new AppServices
                {
                    Loc = LocalizationLoader.Load(lang),
                    Data = new SaveData(),
                    Store = null,
                    Games = GameRegistry.Discover(),
                    Style = style,
                    Icons = icons,
                    Cards = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.CardLibraryPath),
                    Puzzle = AssetDatabase.LoadAssetAtPath<PuzzleConfig>(ConfigAssets.PuzzleConfigPath) ?? ScriptableObject.CreateInstance<PuzzleConfig>(),
                    Themes = AssetDatabase.LoadAssetAtPath<ThemeConfig>(ConfigAssets.ThemeConfigPath) ?? ScriptableObject.CreateInstance<ThemeConfig>(),
                    Clock = () => PreviewDate,   // ErNa-008: the daily challenge of a fixed day
                };
                ctx.Data.settings.language = LanguageCodes.ToCode(lang);
                var manager = AppUi.Build(ctx, root);
                shot.Setup?.Invoke(ctx, manager);
                var safeRect = new Rect(device.Safe.x, device.Safe.y, device.Safe.width, device.Safe.height);
                foreach (var sa in root.GetComponentsInChildren<SafeArea>(true)) sa.Apply(safeRect, new Vector2(w, h));

                Relayout(root);
                if (shot.AfterLayout != null)
                {
                    shot.AfterLayout(ctx, manager);
                    Relayout(root);
                }
                foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true)) tmp.ForceMeshUpdate(true, true);
                board = MeasureBoard(manager, device);
                cam.Render();   // first render requests the font glyphs
                Canvas.ForceUpdateCanvases();
                cam.Render();

                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev;
                TintOutsideSafeArea(tex, device.Safe);
                File.WriteAllBytes(file, tex.EncodeToPNG());
                if (shot.Name.StartsWith("game", StringComparison.Ordinal) && lang == Language.Italian) SaveColorBlind(tex, file);
                Object.DestroyImmediate(tex);

                return Measure(root, device, style, out margins);
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
                Object.DestroyImmediate(camGo);
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }

        /// <summary>The same picture as seen with deuteranopia, protanopia and tritanopia (Machado 2009), in <see cref="ColorBlindDir"/>.</summary>
        static void SaveColorBlind(Texture2D tex, string file)
        {
            var src = tex.GetPixels32();
            var sim = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            try
            {
                foreach (var d in ColorVision.Deficiencies)
                {
                    sim.SetPixels32(ColorVision.SimulateImage(src, d));
                    sim.Apply();
                    File.WriteAllBytes($"{ColorBlindDir}/{Path.GetFileNameWithoutExtension(file)}_{ColorVision.Code(d)}.png", sim.EncodeToPNG());
                }
            }
            finally { Object.DestroyImmediate(sim); }
        }

        /// <summary>Water colors: lightness, and the smallest difference between two of them as seen normally and with each deficiency.</summary>
        static void AppendPaletteReport(StringBuilder report)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<PlumberConfig>(ConfigAssets.PlumberConfigPath);
            if (cfg == null) return;
            report.Append("water colors: ").Append(string.Join(", ", cfg.waterColors.Select((c, i) => $"{i} #{ColorUtility.ToHtmlStringRGB(c)} L* {ColorVision.Lightness(c):0.0}"))).Append('\n');
            foreach (var d in new[] { ColorDeficiency.None }.Concat(ColorVision.Deficiencies))
            {
                float min = PlumberPalette.MinDeltaE(cfg.waterColors, d, out int a, out int b);
                report.Append($"  {ColorVision.Code(d)}: smallest difference between two water colors delta E {min:0.0} (colors {a} and {b})\n");
            }
        }

        static void Relayout(RectTransform root)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            }
        }

        /// <summary>Game screen only: cell size, free space above and below the grid, gaps to the screen edges, the bars and the main action.</summary>
        static string MeasureBoard(ScreenManager manager, Device d)
        {
            var game = manager.Top as GameScreen;
            if (game == null || game.Board == null || game.Board.GridColumns == 0) return null;
            var grid = PixelRect(game.Board.GridRect, d);
            var top = PixelRect(game.TopBarRect, d);
            var bottom = PixelRect(game.BottomBarRect, d);
            int cols = game.Board.GridColumns, rows = game.Board.GridRows;
            float cell = grid.width / cols;
            var action = game.MainActionRect ?? game.MessageRect;
            float below = action != null ? grid.yMin - PixelRect(action, d).yMax : grid.yMin - bottom.yMax;
            var sb = new StringBuilder($"{cols}x{rows} cells of {cell:0.0}x{grid.height / Math.Max(1, rows):0.0} px, grid {Fmt(grid)}; free space above the grid (to the top bar) {top.yMin - grid.yMax:0} px, " +
                   $"below (to the {(action != null ? "main action" : "bottom bar")}) {below:0} px; gap to screen left {grid.xMin:0} / right {d.Width - grid.xMax:0} px, " +
                   $"to safe area left {grid.xMin - d.Safe.x:0} / right {d.Safe.xMax - grid.xMax:0} px; gap to the bottom bar {grid.yMin - bottom.yMax:0} px");
            // fountain drawings (sources and sinks): drawn size of the square they sit in
            // TJ-001: the vehicles: drawn size of a car (short side) as a share of the cell
            if (game.Board is ResolverMaster.Games.TrafficJam.View.TrafficBoardView traffic && traffic.Vehicles.Count > 0)
            {
                var shown = traffic.Vehicles.Where(v => v.gameObject.activeInHierarchy).Select(v => PixelRect((RectTransform)v.transform, d)).ToList();
                if (shown.Count > 0)
                {
                    float shortSide = shown.Min(r => Mathf.Min(r.width, r.height));
                    sb.Append($"; {shown.Count} vehicles, short side {shortSide:0.0} px ({shortSide / cell * 100:0}% of a cell)");
                    // TJ-002: the drawings inside (width across the vehicle, mirrors included) and the smallest gap left along the vehicle
                    var drawn = traffic.Vehicles.Where(v => v.gameObject.activeInHierarchy && v.Drawn).ToList();
                    if (drawn.Count > 0)
                    {
                        float ls = drawn[0].Picture.rectTransform.lossyScale.x;
                        float across = drawn.Min(v => v.Picture.rectTransform.rect.width) * ls;
                        float spare = drawn.Min(v => (v.Art.rect.height - v.Picture.rectTransform.rect.height) * ls);
                        sb.Append($"; {drawn.Count} drawn, width {across:0.0} px ({across / cell * 100:0}% of a cell), smallest room along a vehicle {spare:0.0} px");
                    }
                }
            }
            // PG-001: the penguins: drawn size as a share of the cell
            if (game.Board is ResolverMaster.Games.Penguin.View.PenguinBoardView penguin)
            {
                var pens = penguin.PenguinRects.Select(r => PixelRect(r, d)).ToList();
                if (pens.Count > 0) sb.Append($"; {pens.Count} penguin(s) drawn at {pens.Min(r => r.width):0.0} px ({pens.Min(r => r.width) / cell * 100:0}% of a cell)");
            }
            if (game.Board is ResolverMaster.Games.Plumber.View.PlumberBoardView plumber)
            {
                var ends = plumber.EndpointRects.Select(r => PixelRect(r, d)).ToList();
                if (ends.Count > 0)
                    sb.Append($"; {ends.Count} sources/sinks drawn at {ends.Min(r => r.width):0.0}x{ends.Min(r => r.height):0.0} px ({ends[0].width / cell * 100:0}% of a cell)");
            }
            // the main action button (or the message shown in its place) between the grid and the bottom bar
            var slotRt = game.MainActionRect ?? game.MessageRect;
            if (slotRt != null)
            {
                var slot = PixelRect(slotRt, d);
                sb.Append($"; {(game.MainActionRect != null ? "main action" : "message")} {Fmt(slot)} ({slot.width:0}x{slot.height:0} px): " +
                          $"{grid.yMin - slot.yMax:0} px below the grid, {slot.yMin - bottom.yMax:0} px above the bottom bar, centre offset {slot.center.x - grid.center.x:0} px from the grid centre");
            }
            return sb.ToString();
        }

        static void TintOutsideSafeArea(Texture2D tex, RectInt safe)
        {
            var px = tex.GetPixels32();
            var red = new Color32(230, 40, 40, 255);
            for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
            {
                if (safe.Contains(new Vector2Int(x, y))) continue;
                int i = y * tex.width + x;
                px[i] = Color32.Lerp(px[i], red, 0.35f);
            }
            tex.SetPixels32(px);
            tex.Apply();
        }

        // ---------------- measurements ----------------

        sealed class Item
        {
            public Graphic Graphic;
            public Rect Rect;   // pixels, origin bottom-left
            public string Path;
        }

        /// <summary>The first enabled <see cref="Decoration"/> on the graphic or its parents (a disabled one does not count).</summary>
        static Decoration Deco(Component g) => g.GetComponentsInParent<Decoration>(false).FirstOrDefault(x => x.enabled);

        static List<string> Measure(RectTransform root, Device device, UiKitStyle style, out string margins)
        {
            var issues = new List<string>();
            var screen = new Rect(0, 0, device.Width, device.Height);
            var safe = new Rect(device.Safe.x, device.Safe.y, device.Safe.width, device.Safe.height);
            var visible = root.GetComponentsInChildren<Graphic>(false).Where(g => IsVisible(g)).ToList();
            // a visible popup covers the rest on purpose: measure only the popup
            var overlay = root.GetComponentsInChildren<ModalOverlay>(false).FirstOrDefault();
            if (overlay != null) visible = visible.Where(g => g.transform.IsChildOf(overlay.transform)).ToList();
            var leaves = visible.Where(g => !visible.Any(o => o != g && o.transform.IsChildOf(g.transform)))
                .Select(g => new Item { Graphic = g, Rect = PixelRect(g.rectTransform, device), Path = PathOf(g.transform, root) })
                .ToList();

            float freeTop = float.MaxValue, freeBottom = float.MaxValue, freeLeft = float.MaxValue, freeRight = float.MaxValue;
            int scrolled = 0;
            foreach (var it in leaves)
            {
                var r = it.Rect;
                var deco = Deco(it.Graphic);
                if (deco != null && deco.mayLeaveScreen) continue;   // side cards of the Home carousel peek in from the edges
                // ErNa-006: inside a scroll list the list itself must be on screen; elements outside it are reached by scrolling
                var mask = it.Graphic.GetComponentInParent<RectMask2D>();
                if (mask != null)
                {
                    var view = PixelRect(mask.rectTransform, device);
                    if (!Inside(screen, view)) issues.Add($"scroll list off screen: {PathOf(mask.transform, root)} {Fmt(view)}");
                    else if (!Inside(safe, view)) issues.Add($"scroll list outside safe area: {PathOf(mask.transform, root)} {Fmt(view)}");
                    if (!Inside(view, r))
                    {
                        scrolled++;
                        continue;
                    }
                }
                if (!Inside(screen, r)) issues.Add($"off screen: {it.Path} {Fmt(r)}");
                else if (!Inside(safe, r)) issues.Add($"outside safe area: {it.Path} {Fmt(r)}");

                if (deco != null) continue;
                if (it.Graphic is TMP_Text t && IsClipped(t, out string how)) issues.Add($"text clipped ({how}): {it.Path} \"{t.text.Replace('\n', ' ')}\"");

                freeTop = Mathf.Min(freeTop, safe.yMax - r.yMax);
                freeBottom = Mathf.Min(freeBottom, r.yMin - safe.yMin);
                freeLeft = Mathf.Min(freeLeft, r.xMin - safe.xMin);
                freeRight = Mathf.Min(freeRight, safe.xMax - r.xMax);
            }

            foreach (var g in visible)
            {
                // an Image without sprite draws a plain rectangle: almost always a missing icon
                if (g is Image img && img.sprite == null && !IsScreenBackground(g))
                    issues.Add($"image without sprite (draws a plain rectangle): {PathOf(g.transform, root)}");
                if (Deco(g) != null) continue;   // transient effects overlap / leave the board on purpose
                // every element must stay inside its container (nearest ancestor drawn with a sprite: card, button, badge, circle)
                var container = g.transform.parent != null ? g.transform.parent.GetComponentsInParent<Image>(false).FirstOrDefault(p => p.sprite != null && IsVisible(p)) : null;
                if (container != null && !Inside(PixelRect(container.rectTransform, device), PixelRect(g.rectTransform, device)))
                    issues.Add($"spills out of its container: {PathOf(g.transform, root)} {Fmt(PixelRect(g.rectTransform, device))} outside {PathOf(container.transform, root)} {Fmt(PixelRect(container.rectTransform, device))}");
            }

            // overlaps: leaves (texts, icons) and buttons, ignoring parent/child pairs
            var boxes = leaves.Where(l => Deco(l.Graphic) == null).Concat(visible.Where(g => g.GetComponent<Selectable>() != null && Deco(g) == null && !leaves.Any(l => l.Graphic == g))
                .Select(g => new Item { Graphic = g, Rect = PixelRect(g.rectTransform, device), Path = PathOf(g.transform, root) })).ToList();
            for (int i = 0; i < boxes.Count; i++)
            for (int j = i + 1; j < boxes.Count; j++)
            {
                var a = boxes[i];
                var b = boxes[j];
                if (a.Graphic.transform.IsChildOf(b.Graphic.transform) || b.Graphic.transform.IsChildOf(a.Graphic.transform)) continue;
                float ix = Mathf.Min(a.Rect.xMax, b.Rect.xMax) - Mathf.Max(a.Rect.xMin, b.Rect.xMin);
                float iy = Mathf.Min(a.Rect.yMax, b.Rect.yMax) - Mathf.Max(a.Rect.yMin, b.Rect.yMin);
                if (ix > 1 && iy > 1) issues.Add($"overlap {ix:0}x{iy:0} px: {a.Path} <-> {b.Path}");
            }

            // touch size: the smallest visible button (side cards of the carousel excluded: they are scaled down on purpose)
            var buttons = visible.Where(g => g.GetComponent<Selectable>() != null && Deco(g) == null)
                .Select(g => (path: PathOf(g.transform, root), rect: PixelRect(g.rectTransform, device))).ToList();
            string smallest = "no buttons";
            if (buttons.Count > 0)
            {
                var min = buttons.OrderBy(b => Mathf.Min(b.rect.width, b.rect.height)).First();
                smallest = $"smallest button {min.rect.width:0}x{min.rect.height:0} px ({min.path})";
                foreach (var b in buttons.Where(b => Mathf.Min(b.rect.width, b.rect.height) < style.minTouchSize - 0.5f))
                    issues.Add($"button smaller than {style.minTouchSize:0} px: {b.path} {b.rect.width:0}x{b.rect.height:0}");
            }
            margins = $"content-to-safe-edge min gap top {freeTop:0}, bottom {freeBottom:0}, left {freeLeft:0}, right {freeRight:0} px; {smallest}; {MeasureScale(visible, device, root, issues)}" +
                      (scrolled > 0 ? $"; {scrolled} element(s) reached by scrolling" : "");
            return issues;
        }

        static bool IsScreenBackground(Graphic g) => g.transform.parent != null && g.transform.parent.GetComponent<Canvas>() != null;

        static bool IsVisible(Graphic g)
        {
            if (!g.enabled || !g.gameObject.activeInHierarchy || g.color.a <= 0.01f) return false;
            if (g is Image img && img.sprite == null && IsScreenBackground(g)) return false;
            if (g.GetComponent<ModalOverlay>() != null) return false;   // the dim layer itself
            if (g is TMP_Text txt && string.IsNullOrEmpty(txt.text)) return false;
            foreach (var cg in g.GetComponentsInParent<CanvasGroup>())
                if (cg.alpha <= 0.01f) return false;
            return true;
        }

        /// <summary>The drawn glyphs of a TextMesh Pro text go out of its rect (more than 2 units): too long, too tall, or wrapped onto too many lines.</summary>
        static bool IsClipped(TMP_Text t, out string how)
        {
            t.ForceMeshUpdate(true, true);
            var r = t.rectTransform.rect;
            var b = t.textBounds;
            how = null;
            if (t.textInfo == null || t.textInfo.characterCount == 0) return false;
            const float tol = 2f;
            if (b.min.x < r.xMin - tol || b.max.x > r.xMax + tol) how = $"text {b.size.x:0} wide in {r.width:0}";
            else if (b.min.y < r.yMin - tol || b.max.y > r.yMax + tol) how = $"text {b.size.y:0} tall in {r.height:0}";
            return how != null;
        }

        /// <summary>
        /// Scale at which every visible sprite is drawn (screen px per texture px): above 1 = scaled up (blurry). Sprites of the project
        /// (Assets/Art: Gemini UI, icons, illustrations) must never be scaled up (issue); sprites generated at run time (pipes, theme
        /// art) are only reported. Sliced and tiled sprites: the scale of their borders.
        /// </summary>
        static string MeasureScale(List<Graphic> visible, Device d, RectTransform root, List<string> issues)
        {
            float maxAsset = 0f, maxGenerated = 0f;
            string maxAssetPath = "-", maxGeneratedPath = "-";
            foreach (var g in visible)
            {
                if (!(g is Image img) || img.sprite == null) continue;
                // drawn size from the rect and the scale, not from the screen bounds: right also for a rotated image (TJ-002 vehicles)
                var rs = img.rectTransform.rect.size;
                var ls = img.rectTransform.lossyScale;
                var px = new Rect(0, 0, Mathf.Abs(rs.x * ls.x), Mathf.Abs(rs.y * ls.y));
                var sr = img.sprite.rect;
                float scale;
                if (img.type == Image.Type.Sliced || img.type == Image.Type.Tiled)
                {
                    scale = (100f / img.sprite.pixelsPerUnit) / Mathf.Max(0.01f, img.pixelsPerUnitMultiplier);
                    // borders larger than the rect are drawn smaller
                    var border = img.sprite.border;
                    float bw = border.x + border.z, bh = border.y + border.w;
                    float fit = 1f;
                    if (bw * scale > px.width + 0.5f) fit = Mathf.Min(fit, px.width / (bw * scale));
                    if (bh * scale > px.height + 0.5f) fit = Mathf.Min(fit, px.height / (bh * scale));
                    scale *= fit;
                }
                else if (img.preserveAspect) scale = Mathf.Min(px.width / sr.width, px.height / sr.height);
                else scale = Mathf.Max(px.width / sr.width, px.height / sr.height);
                bool asset = AssetDatabase.Contains(img.sprite);
                if (asset && scale > maxAsset)
                {
                    maxAsset = scale;
                    maxAssetPath = PathOf(g.transform, root) + " (" + img.sprite.name + ")";
                }
                if (!asset && scale > maxGenerated)
                {
                    maxGenerated = scale;
                    maxGeneratedPath = PathOf(g.transform, root) + " (" + img.sprite.name + ")";
                }
                if (asset && scale > 1.02f) issues.Add($"sprite scaled up x{scale:0.00}: {PathOf(g.transform, root)} ({img.sprite.name} {sr.width:0}x{sr.height:0} drawn {px.width:0}x{px.height:0})");
            }
            return $"largest sprite scale: project art x{maxAsset:0.00} ({maxAssetPath}), generated x{maxGenerated:0.00} ({maxGeneratedPath})";
        }

        static Rect PixelRect(RectTransform rt, Device d)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            float xMin = c.Min(p => p.x) + d.Width / 2f, xMax = c.Max(p => p.x) + d.Width / 2f;
            float yMin = c.Min(p => p.y) + d.Height / 2f, yMax = c.Max(p => p.y) + d.Height / 2f;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        static bool Inside(Rect outer, Rect r) =>
            r.xMin >= outer.xMin - 0.5f && r.yMin >= outer.yMin - 0.5f && r.xMax <= outer.xMax + 0.5f && r.yMax <= outer.yMax + 0.5f;

        static string Fmt(Rect r) => $"[x {r.xMin:0}..{r.xMax:0}, y {r.yMin:0}..{r.yMax:0}]";

        static string PathOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var p = t; p != null && p != root; p = p.parent) parts.Add(p.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
