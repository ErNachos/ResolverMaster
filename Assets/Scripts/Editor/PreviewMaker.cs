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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ResolverMaster.Editor
{
    /// <summary>
    /// Menu Resolver Master > Make Previews: renders Home, Settings, level choice and game (start, partial water, win panel), in Italian
    /// and English, at 1080x1920 (9:16) and 1080x2520 (9:21)
    /// into Previews/, with the area outside the simulated safe area tinted red, and measures the layout:
    /// safe-area margins, elements off screen or outside the safe area, clipped texts, overlapping elements; on the game screen also the
    /// cell size and the gaps between the grid, the screen edges and the bars.
    /// Report: Previews/previews_report.txt (also in the log). Batch: -executeMethod ResolverMaster.Editor.PreviewMaker.MakePreviewsBatch
    /// </summary>
    public static class PreviewMaker
    {
        public const string OutDir = "Previews";

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
            new Shot { Name = ScreenIds.Home },
            new Shot { Name = ScreenIds.Settings, Setup = (ctx, m) => m.PushImmediate(ScreenIds.Settings) },
            new Shot
            {
                Name = "levels",
                Setup = (ctx, m) =>
                {
                    // sample progress: levels 1-3 solved with 3, 2 and 1 stars, level 4 unlocked, 5+ locked
                    string mode = GameModeCodes.ToCode(GameMode.Story);
                    for (int i = 0; i < 3; i++) ctx.Data.progress.SetBest(PlumberModule.Id, mode, i, 3 - i);
                    m.Get<LevelSelectScreen>(ScreenIds.LevelSelect).Open(ctx.Games.FindModule(PlumberModule.Id));
                    m.PushImmediate(ScreenIds.LevelSelect);
                },
            },
            GameShot("game_start", 0, BoardPreview.Start, win: false),
            GameShot("game_partial", 7, BoardPreview.Partial, win: false),
            GameShot("game_win", 0, BoardPreview.Solved, win: true),
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
                game.Board.PreparePreview(state);
                game.Board.SnapVisuals();
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
            var files = new List<string>();
            var report = new StringBuilder("Resolver Master previews - layout measurements\n");
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
                    Puzzle = AssetDatabase.LoadAssetAtPath<PuzzleConfig>(ConfigAssets.PuzzleConfigPath) ?? ScriptableObject.CreateInstance<PuzzleConfig>(),
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
                Object.DestroyImmediate(tex);

                return Measure(root, device, out margins);
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
                Object.DestroyImmediate(camGo);
                rt.Release();
                Object.DestroyImmediate(rt);
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

        /// <summary>Game screen only: cell size and the gaps between the grid and the screen edges / the two bars.</summary>
        static string MeasureBoard(ScreenManager manager, Device d)
        {
            var game = manager.Top as GameScreen;
            if (game == null || game.Board == null || game.Board.GridColumns == 0) return null;
            var grid = PixelRect(game.Board.GridRect, d);
            var top = PixelRect(game.TopBarRect, d);
            var bottom = PixelRect(game.BottomBarRect, d);
            float cell = grid.width / game.Board.GridColumns;
            return $"{game.Board.GridColumns}x{game.Board.GridColumns} cells of {cell:0.0} px, grid {Fmt(grid)}; gap to screen left {grid.xMin:0} / right {d.Width - grid.xMax:0} px, " +
                   $"to safe area left {grid.xMin - d.Safe.x:0} / right {d.Safe.xMax - grid.xMax:0} px; gap to top bar {top.yMin - grid.yMax:0} px, to bottom bar {grid.yMin - bottom.yMax:0} px";
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

        static List<string> Measure(RectTransform root, Device device, out string margins)
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
            foreach (var it in leaves)
            {
                var r = it.Rect;
                if (!Inside(screen, r)) issues.Add($"off screen: {it.Path} {Fmt(r)}");
                else if (!Inside(safe, r)) issues.Add($"outside safe area: {it.Path} {Fmt(r)}");
                var mask = it.Graphic.GetComponentInParent<RectMask2D>();
                if (mask != null && !Inside(PixelRect(mask.rectTransform, device), r))
                    issues.Add($"hidden by scroll list (scroll to see it): {it.Path}");

                if (it.Graphic is Text t && IsClipped(t)) issues.Add($"text clipped: {it.Path} \"{t.text.Replace('\n', ' ')}\"");

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
                // every element must stay inside its container (nearest ancestor drawn with a sprite: card, button, badge, circle)
                var container = g.transform.parent != null ? g.transform.parent.GetComponentsInParent<Image>(false).FirstOrDefault(p => p.sprite != null && IsVisible(p)) : null;
                if (container != null && !Inside(PixelRect(container.rectTransform, device), PixelRect(g.rectTransform, device)))
                    issues.Add($"spills out of its container: {PathOf(g.transform, root)} {Fmt(PixelRect(g.rectTransform, device))} outside {PathOf(container.transform, root)} {Fmt(PixelRect(container.rectTransform, device))}");
            }

            // overlaps: leaves (texts, icons) and buttons, ignoring parent/child pairs
            var boxes = leaves.Concat(visible.Where(g => g.GetComponent<Selectable>() != null && !leaves.Any(l => l.Graphic == g))
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

            margins = $"content-to-safe-edge min gap top {freeTop:0}, bottom {freeBottom:0}, left {freeLeft:0}, right {freeRight:0} px";
            return issues;
        }

        static bool IsScreenBackground(Graphic g) => g.transform.parent != null && g.transform.parent.GetComponent<Canvas>() != null;

        static bool IsVisible(Graphic g)
        {
            if (!g.enabled || !g.gameObject.activeInHierarchy || g.color.a <= 0.01f) return false;
            if (g is Image img && img.sprite == null && IsScreenBackground(g)) return false;
            if (g.GetComponent<ModalOverlay>() != null) return false;   // the dim layer itself
            if (g is Text txt && string.IsNullOrEmpty(txt.text)) return false;
            foreach (var cg in g.GetComponentsInParent<CanvasGroup>())
                if (cg.alpha <= 0.01f) return false;
            return true;
        }

        static bool IsClipped(Text t)
        {
            var size = t.rectTransform.rect.size;
            var gen = t.cachedTextGeneratorForLayout;
            var settings = t.GetGenerationSettings(new Vector2(t.horizontalOverflow == HorizontalWrapMode.Wrap ? size.x : 0, 0));
            float ppu = t.pixelsPerUnit;
            float height = gen.GetPreferredHeight(t.text, settings) / ppu;
            float width = gen.GetPreferredWidth(t.text, settings) / ppu;
            return height > size.y + 1f || (t.horizontalOverflow == HorizontalWrapMode.Overflow && width > size.x + 1f);
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
