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
            // before the opening, one level per chapter (theme): 3x4 (level 1), 5x7 (level 14), 6x8 (level 27)
            GameShot("game_c1", 0, BoardPreview.Start, win: false),
            GameShot("game_c2", 13, BoardPreview.Start, win: false),
            GameShot("game_c3", 26, BoardPreview.Start, win: false),
            GameShot("game_flowing", 26, BoardPreview.ActionFlowing, win: false),
            GameShot("game_open_ok", 26, BoardPreview.ActionSuccess, win: false),
            GameShot("game_open_leak", 9, BoardPreview.ActionFailure, win: false),
            GameShot("game_win", 0, BoardPreview.Solved, win: true),
            // ErNa-007: the test rewarded ad over the game screen (the wait is over)
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
            // ErNa-006: a hand-made level with a bridge (orange east-west, blue north-south), a decoy cross and locked pieces
            CustomShot("game_bridge", BridgeLevel, BoardPreview.Start),
            CustomShot("game_bridge_flowing", BridgeLevel, BoardPreview.ActionFlowing),
            CustomShot("game_bridge_ok", BridgeLevel, BoardPreview.ActionSuccess),
            // the lock plate on the other terrains (garden, farm)
            CustomShot("game_locked_c1", BridgeLevel, BoardPreview.Start, storyIndex: 0),
            CustomShot("game_locked_c2", BridgeLevel, BoardPreview.Start, storyIndex: 9),
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
                    foreach (var shot in Shots)
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
                var px = PixelRect(img.rectTransform, d);
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
