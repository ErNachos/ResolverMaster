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
    /// A tap rotates the piece at once in the model (the counter updates at once) and queues the visual turn; when no piece is turning
    /// the water is recomputed and spreads cell after cell. On win: water reaches the sink, the sink fills, then <see cref="PuzzleBoard.Completed"/>.
    /// </summary>
    public sealed class PlumberBoardView : PuzzleBoard
    {
        enum Phase { Playing, WaterToSink, SinkFilling, Done }

        PlumberModule _module;
        PlumberConfig _cfg;
        PipeSprites _sprites;
        UiFactory _ui;
        RectTransform _frame, _cells;
        ComponentPool<PipeCellView> _pool;
        readonly List<PipeCellView> _views = new List<PipeCellView>();
        readonly PlumberPuzzle _puzzle = new PlumberPuzzle();

        bool[] _wet = Array.Empty<bool>();
        float[] _wetAt = Array.Empty<float>();
        int[] _wetColor = Array.Empty<int>();
        bool _waterDirty, _waterPending;
        float _waterClock;
        Phase _phase;
        float _phaseTime;
        int _sinkCell = -1;
        Vector2 _fittedSize;

        public PlumberPuzzle Puzzle => _puzzle;
        public override IPuzzleSession Session => _puzzle;
        public override RectTransform GridRect => _cells;
        public override int GridColumns => _puzzle.Grid?.Width ?? 0;
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
            _pool = new ComponentPool<PipeCellView>(CreateCell);
        }

        PipeCellView CreateCell()
        {
            var tile = _ui.Image("Cell", _cells, _sprites.Tile, _cfg.tileColor);
            tile.raycastTarget = true;
            var pipe = _ui.Image("Pipe", tile.transform, null, _cfg.pipeEmpty);
            UiFactory.Stretch(pipe.rectTransform);
            var fill = _ui.Image("SinkFill", pipe.transform, _sprites.Disk, _cfg.WaterColor(0));
            float r = Mathf.Max(0.05f, _cfg.sinkRingRadius - _cfg.sinkRingWidth / 2f + 0.01f);
            fill.rectTransform.anchorMin = new Vector2(0.5f - r, 0.5f - r);
            fill.rectTransform.anchorMax = new Vector2(0.5f + r, 0.5f + r);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.gameObject.SetActive(false);
            var view = tile.gameObject.AddComponent<PipeCellView>();
            view.Tile = tile;
            view.Pipe = pipe;
            view.SinkFill = fill;
            view.Tapped = OnTap;
            return view;
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
            var grid = _puzzle.Grid;
            int w = grid.Width, h = grid.Height;
            _wet = new bool[grid.CellCount];
            _wetAt = new float[grid.CellCount];
            _wetColor = new int[grid.CellCount];
            _sinkCell = -1;
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
                v.Tile.color = piece.Locked ? _cfg.lockedTileColor : _cfg.tileColor;
                v.Pipe.sprite = _sprites.Get(piece.Kind);
                v.Pipe.enabled = piece.Kind != PieceKind.Empty;
                v.SinkFill.gameObject.SetActive(false);
                v.SinkFill.color = _cfg.WaterColor(piece.Color);
                if (piece.Kind == PieceKind.Sink) _sinkCell = c;
                _views.Add(v);
            }
        }

        public override void Undo()
        {
            if (_phase != Phase.Playing || !_puzzle.Undo(out int cell)) return;
            _views[cell].AddSteps(-1);
            _waterDirty = true;
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

        public override void SnapVisuals()
        {
            var grid = _puzzle.Grid;
            if (grid == null) return;
            for (int c = 0; c < _views.Count; c++) _views[c].SnapRotation(grid.Pieces[c].Rotation);
            var water = WaterFlow.Compute(grid);
            for (int c = 0; c < _views.Count; c++)
            {
                _wetAt[c] = -1f;
                SetWet(c, water.Step[c] >= 0, water.Color[c]);
            }
            _waterDirty = _waterPending = false;
            bool solved = _puzzle.IsSolved;
            if (_sinkCell >= 0)
            {
                var fill = _views[_sinkCell].SinkFill;
                fill.gameObject.SetActive(solved);
                fill.rectTransform.localScale = Vector3.one;
            }
            if (solved) _phase = Phase.Done;
        }

        public override void PreparePreview(BoardPreview state)
        {
            _puzzle.Restart();
            _phase = Phase.Playing;
            if (state != BoardPreview.Start)
            {
                var sol = PlumberSolver.Solve(_puzzle.Grid);
                var cells = new HashSet<int>(sol.PathCells);
                if (state == BoardPreview.Partial)
                {
                    cells.Clear();
                    for (int i = 0; i < sol.PathCells.Count / 2; i++) cells.Add(sol.PathCells[i]);
                }
                foreach (int tap in sol.Taps)
                    if (cells.Contains(tap)) _puzzle.ApplyMove(tap);
            }
            SnapVisuals();
            RaiseChanged();
        }

        // ------------------------------------------------------------------ input and animation

        void OnTap(int cell)
        {
            if (_phase != Phase.Playing || !_puzzle.ApplyMove(cell)) return;
            _views[cell].AddSteps(1);
            _waterDirty = true;
            if (_puzzle.IsSolved) _phase = Phase.WaterToSink;
            RaiseChanged();
        }

        void Update()
        {
            if (_puzzle.Grid == null) return;
            float dt = Time.unscaledDeltaTime;
            bool rotating = false;
            foreach (var v in _views) rotating |= v.TickRotation(dt, _cfg.rotateDuration);
            if (_waterDirty && !rotating) StartWater();
            TickWater(dt);
            TickWin(dt, rotating);
        }

        void LateUpdate()
        {
            if (((RectTransform)transform).rect.size != _fittedSize) Refit();
        }

        /// <summary>Dry cells empty at once; newly wet cells fill one propagation step after the other.</summary>
        void StartWater()
        {
            _waterDirty = false;
            _waterClock = 0f;
            var water = WaterFlow.Compute(_puzzle.Grid);
            int baseStep = int.MaxValue;
            for (int c = 0; c < _views.Count; c++)
                if (water.Step[c] >= 0 && !_wet[c]) baseStep = Math.Min(baseStep, water.Step[c]);
            _waterPending = false;
            for (int c = 0; c < _views.Count; c++)
            {
                _wetAt[c] = -1f;
                if (water.Step[c] < 0)
                {
                    if (_wet[c]) SetWet(c, false, -1);
                }
                else if (!_wet[c])
                {
                    _wetAt[c] = (water.Step[c] - baseStep) * _cfg.waterStep;
                    _wetColor[c] = water.Color[c];
                    _waterPending = true;
                }
            }
        }

        void TickWater(float dt)
        {
            if (!_waterPending) return;
            _waterClock += dt;
            bool any = false;
            for (int c = 0; c < _views.Count; c++)
            {
                if (_wetAt[c] < 0f) continue;
                if (_waterClock >= _wetAt[c])
                {
                    SetWet(c, true, _wetColor[c]);
                    _wetAt[c] = -1f;
                }
                else any = true;
            }
            _waterPending = any;
        }

        void TickWin(float dt, bool rotating)
        {
            if (_phase == Phase.WaterToSink && !rotating && !_waterDirty && !_waterPending)
            {
                _phase = Phase.SinkFilling;
                _phaseTime = 0f;
                if (_sinkCell >= 0)
                {
                    _views[_sinkCell].SinkFill.gameObject.SetActive(true);
                    _views[_sinkCell].SinkFill.rectTransform.localScale = Vector3.zero;
                }
            }
            if (_phase != Phase.SinkFilling) return;
            _phaseTime += dt;
            if (_sinkCell >= 0)
            {
                float t = Mathf.Clamp01(_phaseTime / Mathf.Max(0.01f, _cfg.sinkFill));
                float s = EaseOutBack(t);
                _views[_sinkCell].SinkFill.rectTransform.localScale = new Vector3(s, s, 1);
            }
            if (_phaseTime >= _cfg.sinkFill + _cfg.winPause)
            {
                _phase = Phase.Done;
                RaiseCompleted();
            }
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        void SetWet(int cell, bool wet, int color)
        {
            _wet[cell] = wet;
            _views[cell].Pipe.color = wet ? _cfg.WaterColor(color) : _cfg.pipeEmpty;
        }
    }
}
