using ResolverMaster.Core.Audio;
using System;
using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Themes;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Games.Plumber.View
{
    /// <summary>
    /// The plumber board on the Core game screen: a cream Gemini window fitted into the puzzle area (as big as possible, centred,
    /// columns and rows may differ), the ground of the chapter theme between the cells, one pooled cell per grid cell on a theme slab.
    /// Playing: a tap rotates the piece at once in the model (the counter updates at once) and queues the visual turn; pipes stay grey.
    /// Sources and sinks are fountain drawings (one of 4 each, picked from the level seed) that never rotate, on a soft shadow; a pipe
    /// stub enters under them towards their opening; a source's mask has its water color, a sink sits on a halo of its color.
    /// "Open the water" (the Core main action, not a move): pending rotations end, then the water runs continuously from every source
    /// at the same speed, following the shape of each pipe (<see cref="WaterTiming"/>, <see cref="WaterGraphic"/>); after a T both
    /// branches fill together; a fountain fills up when the water reaches it. Success = water splashes out of the fountains, then
    /// <see cref="PuzzleBoard.Completed"/>; failure = the water runs up to the first problem, its effect plays there (splash for a leak,
    /// blinking cells for mixed or wrong colors, blinking fountain when dry) with a short message, then the level restarts.
    /// Every animation uses the real elapsed time (same duration at 30 and 60 fps).
    /// </summary>
    public sealed class PlumberBoardView : PuzzleBoard
    {
        enum Phase { Playing, Flowing, Spraying, Won, FailEffect, FailPause, Lost }

        const int SplashDrops = 5;

        static readonly MainActionInfo s_openWater = new MainActionInfo { LabelKey = PlumberKeys.OpenWater, IconName = "icon_faucet" };

        PlumberModule _module;
        PlumberConfig _cfg;
        PipeSprites _sprites;
        PlumberArt _art;
        UiFactory _ui;
        BoardFrame _board;   // TJ-001: the window around the grid is Core
        RectTransform _cells, _effects;
        ComponentPool<PipeCellView> _pool;
        readonly List<PipeCellView> _views = new List<PipeCellView>();
        readonly List<int> _sinks = new List<int>();
        readonly Image[] _drops = new Image[SplashDrops];
        readonly List<Image> _winDrops = new List<Image>();
        readonly PlumberPuzzle _puzzle = new PlumberPuzzle();

        Phase _phase;
        float _clock;
        WaterState _water;
        WaterTiming _timing;
        WaterMotion _motion;
        /// <summary>Time from the opening when every fountain is full (success) or when the water reaches the first problem (failure).</summary>
        float _flowEnd;
        Vector2 _fittedSize;

        public PlumberPuzzle Puzzle => _puzzle;
        public override IPuzzleSession Session => _puzzle;
        public override RectTransform GridRect => _cells;
        public override int GridColumns => _puzzle.Grid?.Width ?? 0;
        public override int GridRows => _puzzle.Grid?.Height ?? 0;
        public override MainActionInfo MainAction => s_openWater;
        /// <summary>ErNa-007: the light bulb can fix a piece (playing, not solved, a piece still to turn).</summary>
        public override bool CanHint => _phase == Phase.Playing && _puzzle.Grid != null && !_puzzle.IsSolved && _puzzle.TryFindHint(out _, out _);
        public override bool HintUsed => _puzzle.HintUsed;
        /// <summary>The water is running (or its failure effect): taps, undo, restart and the main action wait.</summary>
        public override bool IsBusy => _phase != Phase.Playing && _phase != Phase.Won && _phase != Phase.Lost;
        public int PooledCells => _pool.Created;
        /// <summary>The fountain drawings of the loaded level (previews measure them).</summary>
        public IEnumerable<RectTransform> EndpointRects
        {
            get
            {
                foreach (var v in _views)
                    if (v.Object.gameObject.activeSelf) yield return v.Object.rectTransform;
            }
        }

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
            _art = PlumberArt.Load();
            _board = BoardFrame.Create(_ui, transform, _cfg.boardInset, _cfg.compactCellBelow, _cfg.compactFrameScale, _cfg.compactOverflow);
            _cells = _board.Cells;
            _effects = _board.Effects;
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i] = _ui.Image("Drop" + (i + 1), _effects, _sprites.Drop, _cfg.WaterColor(0));
                _drops[i].gameObject.AddComponent<Decoration>();
                UiFactory.Place(_drops[i].rectTransform, 0.5f, 0.5f, new Vector2(10, 10), Vector2.zero);
                _drops[i].gameObject.SetActive(false);
            }
            _pool = new ComponentPool<PipeCellView>(CreateCell);
        }

        PipeCellView CreateCell()
        {
            var tile = _ui.Image("Cell", _cells, _sprites.Tile, Color.white);
            tile.raycastTarget = true;
            var halo = _ui.Image("Halo", tile.transform, _sprites.Disk, _cfg.SinkHaloColor(0));
            halo.gameObject.AddComponent<Decoration>();   // under the fountain on purpose
            SetAnchors(halo.rectTransform, _cfg.sinkHaloSize / 2f, 0f);
            halo.gameObject.SetActive(false);
            var fill = _ui.Image("Fill", halo.transform, _sprites.Disk, _cfg.WaterColor(0));
            fill.gameObject.AddComponent<Decoration>();
            UiFactory.Place(fill.rectTransform, 0.5f, 0.5f, Vector2.zero, Vector2.zero);
            var plate = _ui.Image("LockPlate", tile.transform, _sprites.LockPlate, Color.white);
            plate.gameObject.AddComponent<Decoration>();   // under the pipe on purpose
            UiFactory.Stretch(plate.rectTransform);
            plate.gameObject.SetActive(false);
            var glow = _ui.Image("HintGlow", tile.transform, _sprites.HintGlow, _cfg.hintGlowColor);
            glow.gameObject.AddComponent<Decoration>();   // on the rim of the tile on purpose
            UiFactory.Stretch(glow.rectTransform);
            glow.gameObject.SetActive(false);
            var pipe = _ui.Image("Pipe", tile.transform, null, _cfg.pipeEmpty);
            UiFactory.Stretch(pipe.rectTransform);
            var water = NewWater("Water", tile.transform);
            var pipeTop = _ui.Image("PipeTop", tile.transform, _sprites.BridgeTop, _cfg.pipeEmpty);
            pipeTop.gameObject.AddComponent<Decoration>();   // the upper pipe of a bridge, over the lower one on purpose
            UiFactory.Stretch(pipeTop.rectTransform);
            pipeTop.gameObject.SetActive(false);
            var waterTop = NewWater("WaterTop", tile.transform);
            var shadow = _ui.Image("Shadow", tile.transform, _sprites.Shadow, new Color(0, 0, 0, _cfg.shadowAlpha));
            shadow.gameObject.AddComponent<Decoration>();
            var srt = shadow.rectTransform;
            srt.anchorMin = new Vector2(0.5f - _cfg.shadowWidth / 2f, 0.5f + _cfg.shadowOffset - _cfg.shadowHeight / 2f);
            srt.anchorMax = new Vector2(0.5f + _cfg.shadowWidth / 2f, 0.5f + _cfg.shadowOffset + _cfg.shadowHeight / 2f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            var obj = _ui.Image("Fountain", tile.transform, null, Color.white);
            obj.gameObject.AddComponent<Decoration>();     // on its pipe stub on purpose
            obj.preserveAspect = true;
            SetAnchors(obj.rectTransform, _cfg.endpointSize / 2f, _cfg.endpointLift);
            var mask = _ui.Image("Water", obj.transform, null, _cfg.WaterColor(0));
            mask.preserveAspect = true;
            UiFactory.Stretch(mask.rectTransform);
            obj.gameObject.SetActive(false);
            shadow.gameObject.SetActive(false);
            var view = tile.gameObject.AddComponent<PipeCellView>();
            view.Tile = tile;
            view.Pipe = pipe;
            view.Water = water;
            view.Plate = plate;
            view.Glow = glow;
            view.PipeTop = pipeTop;
            view.WaterTop = waterTop;
            view.Halo = halo;
            view.Fill = fill;
            view.Shadow = shadow;
            view.Object = obj;
            view.ObjectMask = mask;
            view.Tapped = OnTap;
            return view;
        }

        WaterGraphic NewWater(string name, Transform parent)
        {
            var go = _ui.Rect(name, parent).gameObject;
            go.AddComponent<CanvasRenderer>();   // added first: the Graphic needs it to be drawn
            var water = go.AddComponent<WaterGraphic>();
            water.raycastTarget = false;
            UiFactory.Stretch(water.rectTransform);
            go.AddComponent<Decoration>();   // runs inside the pipe on purpose
            return water;
        }

        /// <summary>Visual quarter turns of a cell: a bridge is always drawn north-south over east-west (ErNa-006).</summary>
        int ViewSteps(PipeGrid grid, int cell) => grid.Pieces[cell].Kind == PieceKind.Bridge ? 0 : grid.Pieces[cell].Rotation;

        static void SetAnchors(RectTransform rt, float halfSize, float lift)
        {
            rt.anchorMin = new Vector2(0.5f - halfSize, 0.5f - halfSize + lift);
            rt.anchorMax = new Vector2(0.5f + halfSize, 0.5f + halfSize + lift);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------ PuzzleBoard

        public override void SetMoveLimit(int limit) => _puzzle.MoveLimit = Mathf.Max(0, limit);

        public override IReadOnlyList<int> MoveHistory => _puzzle.History;

        /// <summary>One rotation digit (0-3) per cell, in reading order.</summary>
        public override string CaptureState()
        {
            var grid = _puzzle.Grid;
            if (grid == null) return "";
            var chars = new char[grid.CellCount];
            for (int c = 0; c < chars.Length; c++) chars[c] = (char)('0' + grid.Pieces[c].Rotation);
            return new string(chars);
        }

        public override bool RestoreMoves(IReadOnlyList<int> history)
        {
            if (history == null || history.Count == 0) return true;
            bool ok = _puzzle.Replay(history);
            if (!ok) _puzzle.Restart();
            _phase = Phase.Playing;
            SnapVisuals();
            RaiseChanged();
            return ok;
        }

        public override void Load(LevelRequest request)
        {
            if (request.Mode == GameMode.Challenge)
            {
                // ErNa-007: a generated level (cells in the codec form, exact minimum from the generator)
                var grid = PlumberLevelCodec.Parse(request.Data);
                LoadLevel(new PlumberLevel
                {
                    number = request.Index + 1, chapter = 0, index = request.Index, seed = request.Seed,
                    width = grid.Width, height = grid.Height, minMoves = request.MinMoves, cells = request.Data,
                });
                return;
            }
            var levels = _module.StoryLevels;
            if (levels.Count == 0) throw new InvalidOperationException("no Plumber Story levels");
            LoadLevel(levels[Mathf.Clamp(request.Index, 0, levels.Count - 1)]);
        }

        /// <summary>Plays any level (Story, Challenge, or a hand-made one for the previews).</summary>
        public void LoadLevel(PlumberLevel level)
        {
            _puzzle.Load(level);
            _water = null;
            _timing = null;
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
            int seed = _puzzle.Level.seed, variants = Mathf.Max(1, _cfg.artVariants);
            int sources = 0, sinks = 0;
            var theme = Theme;
            _board.SetTheme(theme);
            var waterWidth = (_cfg.PipeWidth - 2f * _cfg.pipeOutline) * _cfg.waterWidth;
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
                // the slab of the theme: a variant per cell from the level seed (the same level always looks the same)
                v.Tile.sprite = theme != null ? ThemeArt.Slab(theme, ArtVariants.Pick(seed, 3, c, ThemeArt.SlabVariants)) : _sprites.Tile;
                v.TileColor = piece.Locked ? _cfg.lockedTileColor : Color.white;
                v.Pipe.sprite = _sprites.Get(piece.Kind);
                v.Pipe.enabled = piece.Kind != PieceKind.Empty;
                v.Plate.gameObject.SetActive(piece.Locked && piece.Kind != PieceKind.Empty);
                v.PipeTop.gameObject.SetActive(piece.Kind == PieceKind.Bridge);
                v.StopShake();
                v.Water.SetWidth(waterWidth);
                v.Water.Clear();
                v.WaterTop.SetWidth(waterWidth);
                v.WaterTop.Clear();
                v.Halo.gameObject.SetActive(piece.Kind == PieceKind.Sink);
                v.Halo.color = _cfg.SinkHaloColor(piece.Color);

                Sprite drawing = null, mask = null;
                if (piece.Kind == PieceKind.Source)
                {
                    int variant = ArtVariants.Pick(seed, PlumberArt.SourceGroup, sources++, variants);
                    drawing = _art.Source(variant);
                    mask = _art.SourceMask(variant);
                }
                else if (piece.Kind == PieceKind.Sink)
                {
                    int variant = ArtVariants.Pick(seed, PlumberArt.SinkGroup, sinks++, variants);
                    drawing = _art.Sink(variant);
                    _sinks.Add(c);
                }
                bool endpoint = piece.Kind == PieceKind.Source || piece.Kind == PieceKind.Sink;
                v.Object.gameObject.SetActive(endpoint);
                v.Shadow.gameObject.SetActive(endpoint);
                // missing drawing (art not imported yet): a disk in the water color, so the level stays playable
                v.Object.sprite = drawing != null ? drawing : _sprites.Disk;
                v.Object.color = drawing != null ? Color.white : _cfg.WaterColor(piece.Color);
                v.ObjectMask.sprite = mask;
                v.ObjectMask.gameObject.SetActive(mask != null);
                v.ObjectMask.color = _cfg.WaterColor(piece.Color);
                _views.Add(v);
            }
        }

        public override void Undo()
        {
            if (_phase != Phase.Playing || !_puzzle.Undo(out int cell)) return;
            if (!_puzzle.Locks.IsPinned(cell)) _views[cell].AddSteps(-1);   // a piece fixed by a hint does not move back
            RaiseChanged();
        }

        /// <summary>ErNa-007: the hinted piece turns into place (animated; ErNa-011: it counts its turns) and gets the glowing outline.</summary>
        public override void ApplyHint()
        {
            if (_phase != Phase.Playing || !_puzzle.ApplyHint(out int cell, out int taps)) return;
            _views[cell].AddSteps(taps);
            _views[cell].Glow.gameObject.SetActive(true);
            RaiseChanged();
        }

        public override void Restart()
        {
            if (_phase != Phase.Playing) return;
            _puzzle.Restart();
            SnapVisuals();
            RaiseChanged();
        }

        /// <summary>
        /// The biggest square cells that fit the area, the window around them, centred (Core <see cref="BoardFrame"/>; big grids:
        /// thinner window frame, <see cref="PlumberConfig.compactCellBelow"/>).
        /// </summary>
        public override void Refit()
        {
            var size = ((RectTransform)transform).rect.size;
            var grid = _puzzle.Grid;
            if (grid == null || !_board.Fit(size, grid.Width, grid.Height)) return;
            _fittedSize = size;
        }

        /// <summary>Rotations jump to their end; a running opening jumps to its result (won, or restarted after a failure).</summary>
        public override void SnapVisuals()
        {
            var grid = _puzzle.Grid;
            if (grid == null) return;
            for (int c = 0; c < _views.Count; c++)
            {
                _views[c].SnapRotation(ViewSteps(grid, c));
                _views[c].StopShake();
                _views[c].Glow.gameObject.SetActive(_puzzle.Locks.IsPinned(c));
            }
            if ((_phase == Phase.Flowing || _phase == Phase.Spraying || _phase == Phase.Won) && _water != null && _water.Success)
            {
                ShowFlow(float.MaxValue);
                SetWinSplash(-1f);
                _phase = Phase.Won;
                return;
            }
            if (_phase == Phase.Lost) return;
            if (_phase != Phase.Playing && _phase != Phase.Won)
            {
                if (!RestartOnFailure)
                {
                    // Challenge: a failure being shown jumps to its end (lost)
                    EndFailEffect();
                    _phase = Phase.Lost;
                    RaiseFailed();
                    return;
                }
                _puzzle.Restart();
                _phase = Phase.Playing;
                for (int c = 0; c < _views.Count; c++) _views[c].SnapRotation(ViewSteps(grid, c));
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
                case BoardPreview.ActionFlowing:
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
            for (int c = 0; c < _views.Count; c++) _views[c].SnapRotation(ViewSteps(_puzzle.Grid, c));
            ShowDry();
            if (state == BoardPreview.ActionFlowing)
            {
                OpenWater();
                // halfway along the longest route: the round fronts and the branches after a T are visible
                _clock = _motion.TimeAt(_timing.EndDistance * 0.55f);
                ShowFlow(_clock);
            }
            else if (state == BoardPreview.Solved || state == BoardPreview.ActionSuccess || state == BoardPreview.ActionFailure)
            {
                OpenWater();
                // jump to the end of the flow (every fountain full), then to the moment of the effect
                _clock = _flowEnd;
                ShowFlow(_clock);
                if (_water.Success)
                {
                    _phase = Phase.Won;
                    if (state == BoardPreview.ActionSuccess) SetWinSplash(0.45f);   // the splash near its highest point
                }
                else
                {
                    StartFailEffect();
                    TickFailEffect(_cfg.failEffect * 0.5f / Mathf.Max(1, _cfg.failBlinks));   // first blink peak
                }
            }
            RaiseChanged();
        }

        /// <summary>Editor previews: the water <paramref name="seconds"/> after the opening of the current state (no effects).</summary>
        public void PreviewFlowAt(float seconds)
        {
            OpenWater();
            _clock = Mathf.Min(seconds, _flowEnd);
            ShowFlow(_clock);
        }

        // ------------------------------------------------------------------ input and animation

        void OnTap(int cell)
        {
            if (_phase != Phase.Playing) return;
            if (!_puzzle.ApplyMove(cell))
            {
                // a cross, a bridge or a locked piece: a shake, not a move (ErNa-006)
                if (_puzzle.Grid.Pieces[cell].IsPipe)
                {
                    _views[cell].StartShake();
                    GameAudio.Play(AudioEvents.PlumberLocked);
                }
                return;
            }
            GameAudio.Play(AudioEvents.PlumberRotate);
            _views[cell].AddSteps(1);
            RaiseChanged();
        }

        /// <summary>"Open the water": pending rotations end at once, then the water starts from every source together.</summary>
        public override void RunMainAction()
        {
            if (_phase != Phase.Playing || _puzzle.Grid == null) return;
            for (int c = 0; c < _views.Count; c++) _views[c].SnapRotation(ViewSteps(_puzzle.Grid, c));
            GameAudio.Play(AudioEvents.PlumberOpen);
            OpenWater();
            RaiseChanged();
        }

        void OpenWater()
        {
            _water = _puzzle.OpenWater();
            _timing = WaterTiming.Compute(_puzzle.Grid, _water);
            _motion = _cfg.Motion;
            _phase = Phase.Flowing;
            _clock = 0f;
            ShowDry();
            PrepareWater();
            // success: the flow ends when every fountain is full; failure: when the water reaches the first problem
            if (_water.Success)
            {
                _flowEnd = _motion.TimeAt(_timing.EndDistance);
                foreach (int s in _sinks)
                    _flowEnd = Mathf.Max(_flowEnd, _motion.TimeAt(_timing.NodeDistance(_timing.CenterNode(s))) + _cfg.fountainFill);
            }
            else _flowEnd = _motion.TimeAt(_timing.StopDistance);
        }

        /// <summary>Gives every cell its water parts with the distances at which the water reaches their two ends.</summary>
        void PrepareWater()
        {
            var grid = _puzzle.Grid;
            for (int c = 0; c < _views.Count; c++)
            {
                var wg = _views[c].Water;
                wg.Clear();
                _views[c].WaterTop.Clear();
                foreach (var p in _timing.Parts(c))
                {
                    var ca = ColorOf(p.NodeA);
                    var cb = ColorOf(p.NodeB);
                    if (p.Through)
                    {
                        // a bridge: the north-south water runs above the upper pipe, the east-west one under it
                        var target = PieceShapes.IsVertical(p.DirA) ? _views[c].WaterTop : wg;
                        target.AddThrough(p.DirA, p.DirB, _timing.NodeDistance(p.NodeA), _timing.NodeDistance(p.NodeB), ca, cb);
                    }
                    else if (p.Arc) wg.AddArc(p.DirA, p.DirB, _timing.NodeDistance(p.NodeA), _timing.NodeDistance(p.NodeB), ca, cb);
                    else
                    {
                        wg.AddArm(p.DirB, _timing.NodeDistance(p.NodeA), _timing.NodeDistance(p.NodeB), ca, cb);
                    }
                }
                var kind = grid.Pieces[c].Kind;
                if ((kind == PieceKind.Tee || kind == PieceKind.Cross) && _timing.Reached(_timing.CenterNode(c)))
                    wg.AddJunction(_timing.NodeDistance(_timing.CenterNode(c)), ColorOf(_timing.CenterNode(c)));
            }
        }

        Color ColorOf(int node)
        {
            int col = _timing.NodeColor(node);
            return _cfg.WaterColor(col < 0 ? 0 : col);
        }

        void Update()
        {
            if (_puzzle.Grid == null) return;
            float dt = Time.unscaledDeltaTime;
            foreach (var v in _views)
            {
                v.TickRotation(dt, _cfg.rotateDuration);
                v.TickShake(dt, _cfg.shakeDuration, _cfg.shakeAmplitude, _cfg.shakeCycles);
            }
            switch (_phase)
            {
                case Phase.Flowing:
                    _clock = Mathf.Min(_clock + dt, _flowEnd);
                    ShowFlow(_clock);
                    if (_clock >= _flowEnd)
                    {
                        if (_water.Success)
                        {
                            _phase = Phase.Spraying;
                            _clock = 0f;
                        }
                        else StartFailEffect();
                    }
                    break;
                case Phase.Spraying:
                    _clock += dt;
                    SetWinSplash(Mathf.Clamp01(_clock / Mathf.Max(0.01f, _cfg.winSpray)));
                    if (_clock >= _cfg.winSpray + _cfg.winPause)
                    {
                        SetWinSplash(-1f);
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
                        if (!RestartOnFailure)
                        {
                            // Challenge: the run is lost, the board stays as it is under the end panel
                            _phase = Phase.Lost;
                            RaiseChanged();
                            RaiseFailed();
                            break;
                        }
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

        /// <summary>Every pipe grey and empty, fountains dry, no effect.</summary>
        void ShowDry()
        {
            for (int c = 0; c < _views.Count; c++)
            {
                var v = _views[c];
                v.Water.SetFront(-1f);
                v.WaterTop.SetFront(-1f);
                v.Tile.color = v.TileColor;
                v.Fill.rectTransform.sizeDelta = Vector2.zero;
                v.Fill.gameObject.SetActive(false);
            }
            SetWinSplash(-1f);
            EndFailEffect();
        }

        /// <summary>The water <paramref name="seconds"/> after the opening (clamped to the end of the flow); fountains fill as it arrives.</summary>
        void ShowFlow(float seconds)
        {
            if (_timing == null) return;
            seconds = Mathf.Min(seconds, _flowEnd);
            float front = _water.Success ? _motion.DistanceAt(seconds) : Mathf.Min(_motion.DistanceAt(seconds), _timing.StopDistance);
            foreach (var v in _views)
            {
                v.Water.SetFront(front);
                v.WaterTop.SetFront(front);
            }
            foreach (int s in _sinks)
            {
                var v = _views[s];
                float reach = _timing.NodeDistance(_timing.CenterNode(s));
                bool wet = !float.IsInfinity(reach) && front >= reach;
                if (wet && !v.Fill.gameObject.activeSelf) GameAudio.Play(AudioEvents.PlumberFountain);
                v.Fill.gameObject.SetActive(wet);
                if (!wet) continue;
                float u = Mathf.Clamp01((seconds - _motion.TimeAt(reach)) / Mathf.Max(0.01f, _cfg.fountainFill));
                u = 1f - (1f - u) * (1f - u);
                var halo = v.Halo.rectTransform.rect.size;
                v.Fill.rectTransform.sizeDelta = halo * (0.86f * u);
                v.Fill.color = ColorOf(_timing.CenterNode(s));
            }
        }

        /// <summary>
        /// Win effect: drops of the right color jump out of every fountain the water reached and fall back, fading.
        /// <paramref name="u"/> = progress 0..1; negative = hidden.
        /// </summary>
        void SetWinSplash(float u)
        {
            int n = Mathf.Max(1, _cfg.winDrops);
            int used = 0;
            if (u >= 0f && _timing != null)
            {
                var grid = _puzzle.Grid;
                var size = _cells.rect.size;
                float cw = size.x / grid.Width, ch = size.y / grid.Height;
                foreach (int c in _sinks)
                {
                    var centre = _timing.CenterNode(c);
                    if (!_timing.Reached(centre)) continue;
                    int x = c % grid.Width, y = c / grid.Width;
                    var origin = new Vector2((x + 0.5f) * cw - size.x / 2f, size.y / 2f - (y + 0.5f) * ch + ch * (0.2f + _cfg.endpointLift));
                    var color = ColorOf(centre);
                    for (int k = 0; k < n; k++)
                    {
                        if (used == _winDrops.Count)
                        {
                            var img = _ui.Image("WinDrop", _effects, _sprites.Drop, color);
                            img.gameObject.AddComponent<Decoration>();
                            img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                            _winDrops.Add(img);
                        }
                        var d = _winDrops[used++];
                        d.gameObject.SetActive(true);
                        float spread = n == 1 ? 0f : Mathf.Lerp(-1f, 1f, (float)k / (n - 1));
                        // a fountain-like fan: the middle drops jump highest, the outer ones fly wider
                        float height = _cfg.winSplashHeight * ch * (1f - 0.45f * spread * spread);
                        // ballistic arc: up to the top at u = 0.5, back down at u = 1
                        var pos = origin + new Vector2(spread * 0.75f * cw * u, height * 4f * u * (1f - u));
                        d.rectTransform.anchoredPosition = pos;
                        float s = _cfg.winDropSize * cw * (k % 2 == 0 ? 1f : 0.75f);
                        d.rectTransform.sizeDelta = new Vector2(s, s);
                        var cc = color;
                        cc.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, u));
                        d.color = cc;
                    }
                }
            }
            for (int i = used; i < _winDrops.Count; i++) _winDrops[i].gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ failure effects

        WaterProblem FirstProblem => _water.Problems[Mathf.Max(0, _timing.FirstProblemIndex)];

        void StartFailEffect()
        {
            _phase = Phase.FailEffect;
            _clock = 0f;
            var p = FirstProblem;
            if (p.Kind == WaterProblemKind.Leak)
                foreach (var d in _drops)
                {
                    d.gameObject.SetActive(true);
                    d.color = ColorOf(_timing.PortNode(p.Cell, p.Direction));
                }
            if (p.Kind == WaterProblemKind.WrongColor) SetWinSplash(0.35f);   // the wrong water splashes out of the fountain
            RaiseMessage(PlumberKeys.ForProblem(p.Kind));
            GameAudio.Play(AudioEvents.PlumberFail);
            GameAudio.Vibrate();
        }

        void TickFailEffect(float t)
        {
            var p = FirstProblem;
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
                    _views[p.Cell].Object.color = Color.Lerp(Color.white, _cfg.errorColor, pulse);
                    break;
            }
        }

        void Blink(int cell, float pulse)
        {
            if (cell < 0) return;
            var v = _views[cell];
            v.Tile.color = Color.Lerp(v.TileColor, _cfg.errorColor, pulse * 0.85f);
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
            {
                var v = _views[c];
                v.Tile.color = v.TileColor;
                if (v.Object.sprite != _sprites.Disk) v.Object.color = Color.white;
            }
        }
    }
}
