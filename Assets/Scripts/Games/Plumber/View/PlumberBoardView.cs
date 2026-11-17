using System;
using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Games.Plumber.View
{
    /// <summary>
    /// The plumber board on the Core game screen: a square card fitted into the puzzle area, one pooled cell per grid cell.
    /// Playing: a tap rotates the piece at once in the model (the counter updates at once) and queues the visual turn; pipes stay grey,
    /// a source is drawn in its water color, a sink sits on a halo of its color (colors only, from PlumberConfig). "Open the water" (the Core main action, not a move) starts the water from every
    /// source together, cell after cell (taps are blocked meanwhile):
    /// success = the fountains fill, then <see cref="PuzzleBoard.Completed"/>; failure = the water stops at the first problem, its effect
    /// plays there (splash for a leak, blinking cells for mixed or wrong colors, blinking fountain when dry) with a short message,
    /// then the level restarts (moves 0, undo empty).
    /// </summary>
    public sealed class PlumberBoardView : PuzzleBoard
    {
        enum Phase { Playing, Flowing, Filling, Won, FailEffect, FailPause }

        const int SplashDrops = 5;

        static readonly MainActionInfo s_openWater = new MainActionInfo { LabelKey = PlumberKeys.OpenWater, IconName = "faucet" };

        PlumberModule _module;
        PlumberConfig _cfg;
        PipeSprites _sprites;
        UiFactory _ui;
        RectTransform _frame, _cells, _effects;
        ComponentPool<PipeCellView> _pool;
        readonly List<PipeCellView> _views = new List<PipeCellView>();
        readonly List<int> _sinks = new List<int>();
        readonly Image[] _drops = new Image[SplashDrops];
        readonly PlumberPuzzle _puzzle = new PlumberPuzzle();

        Phase _phase;
        float _clock;
        WaterState _water;
        bool[] _shown = Array.Empty<bool>();
        Vector2 _fittedSize;

        public PlumberPuzzle Puzzle => _puzzle;
        public override IPuzzleSession Session => _puzzle;
        public override RectTransform GridRect => _cells;
        public override int GridColumns => _puzzle.Grid?.Width ?? 0;
        public override MainActionInfo MainAction => s_openWater;
        /// <summary>The water is running (or its failure effect): taps, undo, restart and the main action wait.</summary>
        public override bool IsBusy => _phase != Phase.Playing && _phase != Phase.Won;
        public int PooledCells => _pool.Created;

        public static PlumberBoardView Create(AppServices ctx, RectTransform parent, PlumberModule module)
        {
            var go = new GameObject("PlumberBoard", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            UiFactory.Stretch(rt);
            var view = go.AddComponent<PlumberBoardView>();
            view.Build(ctx, module);
            return view;
        }

        void Build(AppServices ctx, PlumberModule module)
        {
            _module = module;
            _ui = ctx.Ui;
            _cfg = PlumberConfig.LoadOrDefault();
            _sprites = PipeSprites.For(_cfg);
            var frame = _ui.Image("Frame", transform, ctx.Style.cardSprite, _cfg.boardColor);
            _frame = frame.rectTransform;
            UiFactory.Place(_frame, 0.5f, 0.5f, new Vector2(100, 100), Vector2.zero);
            _cells = _ui.Rect("Cells", _frame);
            UiFactory.Stretch(_cells, _cfg.boardInset, _cfg.boardInset, _cfg.boardInset, _cfg.boardInset);
            _effects = _ui.Rect("Effects", _frame);
            UiFactory.Stretch(_effects, _cfg.boardInset, _cfg.boardInset, _cfg.boardInset, _cfg.boardInset);
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i] = _ui.Image("Drop" + (i + 1), _effects, _sprites.Disk, _cfg.WaterColor(0));
                _drops[i].gameObject.AddComponent<Decoration>();
                UiFactory.Place(_drops[i].rectTransform, 0.5f, 0.5f, new Vector2(10, 10), Vector2.zero);
                _drops[i].gameObject.SetActive(false);
            }
            _pool = new ComponentPool<PipeCellView>(CreateCell);
        }

        PipeCellView CreateCell()
        {
            var tile = _ui.Image("Cell", _cells, _sprites.Tile, _cfg.tileColor);
            tile.raycastTarget = true;
            var halo = _ui.Image("Halo", tile.transform, _sprites.Disk, _cfg.SinkHaloColor(0));
            halo.gameObject.AddComponent<Decoration>();   // under the sink ring on purpose
            SetAnchors(halo.rectTransform, _cfg.sinkHaloSize / 2f);
            halo.gameObject.SetActive(false);
            var pipe = _ui.Image("Pipe", tile.transform, null, _cfg.pipeEmpty);
            UiFactory.Stretch(pipe.rectTransform);
            var fill = _ui.Image("SinkFill", pipe.transform, _sprites.Disk, _cfg.WaterColor(0));
            float r = Mathf.Max(0.05f, _cfg.sinkRingRadius - _cfg.sinkRingWidth / 2f + 0.01f);
            SetAnchors(fill.rectTransform, r);
            fill.gameObject.SetActive(false);
            var view = tile.gameObject.AddComponent<PipeCellView>();
            view.Tile = tile;
            view.Pipe = pipe;
            view.SinkFill = fill;
            view.Halo = halo;
            view.Tapped = OnTap;
            return view;
        }

        static void SetAnchors(RectTransform rt, float halfSize)
        {
            rt.anchorMin = new Vector2(0.5f - halfSize, 0.5f - halfSize);
            rt.anchorMax = new Vector2(0.5f + halfSize, 0.5f + halfSize);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------ PuzzleBoard

        public override void Load(LevelRequest request)
        {
            if (request.Mode != GameMode.Story) throw new NotSupportedException("Plumber Challenge comes in a later Update");
            var levels = _module.StoryLevels;
            if (levels.Count == 0) throw new InvalidOperationException("no Plumber Story levels");
            _puzzle.Load(levels[Mathf.Clamp(request.Index, 0, levels.Count - 1)]);
            BuildCells();
            _phase = Phase.Playing;
            SnapVisuals();
            Refit();
            RaiseChanged();
        }

        void BuildCells()
        {
            _pool.ReleaseAll();
            _views.Clear();
            _sinks.Clear();
            var grid = _puzzle.Grid;
            int w = grid.Width, h = grid.Height;
            _shown = new bool[grid.CellCount];
            for (int c = 0; c < grid.CellCount; c++)
            {
                var v = _pool.Get();
                int x = c % w, y = c / w;
                var rt = (RectTransform)v.transform;
                rt.anchorMin = new Vector2((float)x / w, 1f - (float)(y + 1) / h);
                rt.anchorMax = new Vector2((float)(x + 1) / w, 1f - (float)y / h);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.SetSiblingIndex(c);
                v.name = $"Cell_{x}_{y}";
                v.Index = c;
                var piece = grid.Pieces[c];
                bool fixedEnd = piece.Kind == PieceKind.Source || piece.Kind == PieceKind.Sink;
                v.Pipe.sprite = _sprites.Get(piece.Kind);
                v.Pipe.enabled = piece.Kind != PieceKind.Empty;
                v.DryColor = fixedEnd ? _cfg.WaterColor(piece.Color) : _cfg.pipeEmpty;
                v.SinkFill.color = _cfg.WaterColor(piece.Color);
                v.Halo.gameObject.SetActive(piece.Kind == PieceKind.Sink);
                v.Halo.color = _cfg.SinkHaloColor(piece.Color);
                if (piece.Kind == PieceKind.Sink) _sinks.Add(c);
                _views.Add(v);
            }
        }

        public override void Undo()
        {
            if (_phase != Phase.Playing || !_puzzle.Undo(out int cell)) return;
            _views[cell].AddSteps(-1);
            RaiseChanged();
        }

        public override void Restart()
        {
            if (_phase != Phase.Playing) return;
            _puzzle.Restart();
            SnapVisuals();
            RaiseChanged();
        }

        public override void Refit()
        {
            var size = ((RectTransform)transform).rect.size;
            float side = Mathf.Floor(Mathf.Min(size.x, size.y));
            if (side <= 0) return;
            _frame.sizeDelta = new Vector2(side, side);
            _fittedSize = size;
        }

        /// <summary>Rotations jump to their end; a running opening jumps to its result (won, or restarted after a failure).</summary>
        public override void SnapVisuals()
        {
            var grid = _puzzle.Grid;
            if (grid == null) return;
            for (int c = 0; c < _views.Count; c++) _views[c].SnapRotation(grid.Pieces[c].Rotation);
            if (_phase == Phase.Flowing || _phase == Phase.Filling || _phase == Phase.Won)
            {
                if (_water != null && _water.Success)
                {
                    ShowWater(_water.StopStep);
                    SetSinkFill(1f);
                    _phase = Phase.Won;
                    return;
                }
            }
            if (_phase != Phase.Playing && _phase != Phase.Won)
            {
                _puzzle.Restart();
                _phase = Phase.Playing;
                for (int c = 0; c < _views.Count; c++) _views[c].SnapRotation(grid.Pieces[c].Rotation);
            }
            if (_phase == Phase.Playing) ShowDry();
        }

        public override void PreparePreview(BoardPreview state)
        {
            _puzzle.Restart();
            _phase = Phase.Playing;
            var sol = PlumberSolver.Solve(_puzzle.Grid);
            switch (state)
            {
                case BoardPreview.Partial:
                    for (int i = 0; i < sol.Taps.Count / 2; i++) _puzzle.ApplyMove(sol.Taps[i]);
                    break;
                case BoardPreview.Solved:
                case BoardPreview.ActionSuccess:
                    foreach (int tap in sol.Taps) _puzzle.ApplyMove(tap);
                    break;
                case BoardPreview.ActionFailure:
                    // the first state along the solution whose opening fails with a leak (almost always the start)
                    for (int i = 0; i <= sol.Taps.Count; i++)
                    {
                        var w = WaterFlow.Compute(_puzzle.Grid);
                        if (!w.Success && w.FirstProblem.Kind == WaterProblemKind.Leak) break;
                        if (i < sol.Taps.Count) _puzzle.ApplyMove(sol.Taps[i]);
                    }
                    break;
            }
            for (int c = 0; c < _views.Count; c++) _views[c].SnapRotation(_puzzle.Grid.Pieces[c].Rotation);
            ShowDry();
            if (state == BoardPreview.Solved || state == BoardPreview.ActionSuccess || state == BoardPreview.ActionFailure)
            {
                OpenWater();
                // jump to the end of the flow, then to the moment of the effect
                ShowWater(_water.StopStep);
                if (_water.Success)
                {
                    SetSinkFill(1f);
                    _phase = Phase.Won;
                }
                else
                {
                    StartFailEffect();
                    TickFailEffect(_cfg.failEffect * 0.5f / Mathf.Max(1, _cfg.failBlinks));   // first blink peak
                }
            }
            RaiseChanged();
        }

        // ------------------------------------------------------------------ input and animation

        void OnTap(int cell)
        {
            if (_phase != Phase.Playing || !_puzzle.ApplyMove(cell)) return;
            _views[cell].AddSteps(1);
            RaiseChanged();
        }

        /// <summary>"Open the water": pending rotations end at once, then the water starts from every source together.</summary>
        public override void RunMainAction()
        {
            if (_phase != Phase.Playing || _puzzle.Grid == null) return;
            for (int c = 0; c < _views.Count; c++) _views[c].SnapRotation(_puzzle.Grid.Pieces[c].Rotation);
            OpenWater();
            RaiseChanged();
        }

        void OpenWater()
        {
            _water = _puzzle.OpenWater();
            _phase = Phase.Flowing;
            _clock = 0f;
            ShowDry();
        }

        void Update()
        {
            if (_puzzle.Grid == null) return;
            float dt = Time.unscaledDeltaTime;
            foreach (var v in _views) v.TickRotation(dt, _cfg.rotateDuration);
            switch (_phase)
            {
                case Phase.Flowing:
                    _clock += dt;
                    ShowWater(Mathf.Min(_water.StopStep, Mathf.FloorToInt(_clock / Mathf.Max(0.01f, _cfg.waterStep))));
                    if (_clock >= (_water.StopStep + 1) * _cfg.waterStep)
                    {
                        if (_water.Success)
                        {
                            _phase = Phase.Filling;
                            _clock = 0f;
                        }
                        else StartFailEffect();
                    }
                    break;
                case Phase.Filling:
                    _clock += dt;
                    SetSinkFill(EaseOutBack(Mathf.Clamp01(_clock / Mathf.Max(0.01f, _cfg.sinkFill))));
                    if (_clock >= _cfg.sinkFill + _cfg.winPause)
                    {
                        _phase = Phase.Won;
                        RaiseChanged();
                        RaiseCompleted();
                    }
                    break;
                case Phase.FailEffect:
                    _clock += dt;
                    TickFailEffect(_clock);
                    if (_clock >= _cfg.failEffect)
                    {
                        EndFailEffect();
                        _phase = Phase.FailPause;
                        _clock = 0f;
                    }
                    break;
                case Phase.FailPause:
                    _clock += dt;
                    if (_clock >= _cfg.failPause)
                    {
                        _puzzle.Restart();
                        _phase = Phase.Playing;
                        SnapVisuals();
                        RaiseChanged();
                    }
                    break;
            }
        }

        void LateUpdate()
        {
            if (((RectTransform)transform).rect.size != _fittedSize) Refit();
        }

        // ------------------------------------------------------------------ water visuals

        /// <summary>Every pipe grey (sources and sinks in their color), fountains empty, no effect.</summary>
        void ShowDry()
        {
            for (int c = 0; c < _views.Count; c++)
            {
                _shown[c] = false;
                var v = _views[c];
                v.Pipe.color = v.DryColor;
                v.Tile.color = _puzzle.Grid.Pieces[c].Locked ? _cfg.lockedTileColor : _cfg.tileColor;
                v.SinkFill.gameObject.SetActive(false);
            }
            EndFailEffect();
        }

        /// <summary>Shows the water of every cell reached at a step up to <paramref name="step"/>.</summary>
        void ShowWater(int step)
        {
            for (int c = 0; c < _views.Count; c++)
            {
                if (_shown[c] || _water.Step[c] < 0 || _water.Step[c] > step) continue;
                _shown[c] = true;
                _views[c].Pipe.color = _cfg.WaterColor(_water.Color[c]);
            }
        }

        void SetSinkFill(float scale)
        {
            foreach (int c in _sinks)
            {
                if (_water == null || _water.Step[c] < 0) continue;
                var fill = _views[c].SinkFill;
                fill.gameObject.SetActive(true);
                fill.color = _cfg.WaterColor(_water.Color[c]);
                fill.rectTransform.localScale = new Vector3(scale, scale, 1);
            }
        }

        // ------------------------------------------------------------------ failure effects

        void StartFailEffect()
        {
            _phase = Phase.FailEffect;
            _clock = 0f;
            var p = _water.FirstProblem;
            if (p.Kind == WaterProblemKind.Leak)
                foreach (var d in _drops)
                {
                    d.gameObject.SetActive(true);
                    d.color = _cfg.WaterColor(_water.Color[p.Cell]);
                }
            if (p.Kind == WaterProblemKind.WrongColor) SetSinkFill(1f);   // the wrong water is visible in the fountain
            RaiseMessage(PlumberKeys.ForProblem(p.Kind));
        }

        void TickFailEffect(float t)
        {
            var p = _water.FirstProblem;
            float u = Mathf.Clamp01(t / Mathf.Max(0.01f, _cfg.failEffect));
            float pulse = 0.5f - 0.5f * Mathf.Cos(u * Mathf.Max(1, _cfg.failBlinks) * 2f * Mathf.PI);
            switch (p.Kind)
            {
                case WaterProblemKind.Leak:
                    UpdateSplash(p.Cell, p.Direction, u);
                    break;
                case WaterProblemKind.ColorMix:
                    Blink(p.Cell, pulse);
                    Blink(p.OtherCell, pulse);
                    break;
                case WaterProblemKind.WrongColor:
                    Blink(p.Cell, pulse);
                    break;
                case WaterProblemKind.DrySink:
                    Blink(p.Cell, pulse);
                    var v = _views[p.Cell];
                    v.Pipe.color = Color.Lerp(v.DryColor, _cfg.errorColor, pulse);
                    break;
            }
        }

        void Blink(int cell, float pulse)
        {
            if (cell < 0) return;
            var baseColor = _puzzle.Grid.Pieces[cell].Locked ? _cfg.lockedTileColor : _cfg.tileColor;
            _views[cell].Tile.color = Color.Lerp(baseColor, _cfg.errorColor, pulse * 0.85f);
        }

        /// <summary>Drops fly out of the leaking opening in a small fan, slowing down and fading.</summary>
        void UpdateSplash(int cell, int dir, float u)
        {
            var grid = _puzzle.Grid;
            var size = _cells.rect.size;
            float cw = size.x / grid.Width, ch = size.y / grid.Height;
            int x = cell % grid.Width, y = cell / grid.Width;
            var center = new Vector2((x + 0.5f) * cw - size.x / 2f, size.y / 2f - (y + 0.5f) * ch);
            var dv = DirVector(dir);
            var edge = center + new Vector2(dv.x * cw, dv.y * ch) * 0.5f;
            float baseAngle = Mathf.Atan2(dv.y, dv.x);
            float ease = 1f - (1f - u) * (1f - u);
            float drop = _cfg.splashDropSize * cw;
            for (int i = 0; i < _drops.Length; i++)
            {
                float k = _drops.Length == 1 ? 0.5f : (float)i / (_drops.Length - 1);
                float angle = baseAngle + Mathf.Lerp(-0.9f, 0.9f, k);
                float reach = _cfg.splashDistance * cw * (i % 2 == 0 ? 1f : 0.7f);
                var pos = edge + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach * ease;
                var rt = _drops[i].rectTransform;
                float s = drop * (i % 2 == 0 ? 1f : 0.75f) * Mathf.Lerp(1f, 0.6f, u);
                rt.sizeDelta = new Vector2(s, s);
                rt.anchoredPosition = pos;
                var c = _drops[i].color;
                c.a = 1f - u * u;
                _drops[i].color = c;
            }
        }

        static Vector2 DirVector(int dir)
        {
            switch (dir)
            {
                case SquareTopology.Up: return Vector2.up;
                case SquareTopology.Right: return Vector2.right;
                case SquareTopology.Down: return Vector2.down;
                default: return Vector2.left;
            }
        }

        void EndFailEffect()
        {
            foreach (var d in _drops) d.gameObject.SetActive(false);
            for (int c = 0; c < _views.Count; c++)
                _views[c].Tile.color = _puzzle.Grid.Pieces[c].Locked ? _cfg.lockedTileColor : _cfg.tileColor;
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
