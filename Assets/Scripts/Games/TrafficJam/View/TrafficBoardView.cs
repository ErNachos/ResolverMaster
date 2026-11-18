using System;
using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Themes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ResolverMaster.Games.TrafficJam.View
{
    /// <summary>
    /// The Traffic jam board on the Core game screen (TJ-001), inside the Core <see cref="BoardFrame"/> (cream window, theme ground,
    /// theme slab under every cell). Vehicles are dragged with the finger along their line only: they follow the finger and stop against
    /// vehicles, barriers and the edge; on release they snap to the nearest cell (a release where the drag started is no move). A target car
    /// with a free way to its exit can be pulled out through it (a colored gate in the window frame): it drives out and fades. Every
    /// animation runs on the real elapsed time (same speed at 30 and 60 fps). The light bulb shows an arrow on the vehicle of the next
    /// right move, then slides it (a free move). In the Challenge, at the maximum the vehicles do not move (a shake) and the Core shows
    /// "Out of moves".
    /// </summary>
    public sealed class TrafficBoardView : PuzzleBoard
    {
        TrafficJamModule _module;
        TrafficJamConfig _cfg;
        TrafficSprites _sprites;
        UiFactory _ui;
        BoardFrame _board;
        RectTransform _tilesRoot, _gatesRoot, _barriersRoot, _vehiclesRoot;
        Image _arrow;
        ComponentPool<Image> _tiles, _barriers, _gates;
        ComponentPool<TrafficVehicleView> _vehiclePool;
        readonly List<TrafficVehicleView> _vehicles = new List<TrafficVehicleView>();
        readonly List<int> _barrierCells = new List<int>();
        readonly List<Image> _gateImages = new List<Image>();
        readonly TrafficPuzzle _puzzle = new TrafficPuzzle();
        /// <summary>Front of each vehicle's art: 0 up, 1 right, 2 down, 3 left (target cars face their exit).</summary>
        int[] _facing = Array.Empty<int>();
        Vector2 _fittedSize;

        // drag in progress
        TrafficVehicleView _drag;
        int _dragPointer;
        Vector2 _dragStart;
        int _dragFrom, _dragLo, _dragHi;
        bool _dragCanExit;

        // animations that block the input: a car driving out, the light bulb, the pause before the win panel
        TrafficVehicleView _exiting;
        TrafficVehicleView _hintVehicle;
        TrafficMove _hintMove;
        float _hintTime = -1f, _winTime = -1f, _exitFrom;

        public TrafficPuzzle Puzzle => _puzzle;
        public override IPuzzleSession Session => _puzzle;
        public override RectTransform GridRect => _board.Cells;
        public override int GridColumns => _puzzle.Layout?.Width ?? 0;
        public override int GridRows => _puzzle.Layout?.Height ?? 0;
        public override bool IsBusy => _exiting != null || _hintTime >= 0f || _winTime >= 0f;
        public override bool CanHint => !IsBusy && _drag == null && _puzzle.Layout != null && !_puzzle.IsSolved;
        public override bool HintUsed => _puzzle.HintUsed;
        public override bool LosesWhenOutOfMoves => true;
        /// <summary>The vehicle views of the loaded level (previews measure them).</summary>
        public IReadOnlyList<TrafficVehicleView> Vehicles => _vehicles;
        public float CellSize => _board.Cell;

        public static TrafficBoardView Create(AppServices ctx, RectTransform parent, TrafficJamModule module)
        {
            var go = new GameObject("TrafficBoard", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            UiFactory.Stretch(rt);
            var view = go.AddComponent<TrafficBoardView>();
            view.Build(ctx, module);
            return view;
        }

        void Build(AppServices ctx, TrafficJamModule module)
        {
            _module = module;
            _ui = ctx.Ui;
            _cfg = TrafficJamConfig.LoadOrDefault();
            _sprites = TrafficSprites.For(_cfg);
            _puzzle.MaxStates = Mathf.Max(1000, _cfg.maxStates);
            _board = BoardFrame.Create(_ui, transform, _cfg.boardInset, _cfg.compactCellBelow, _cfg.compactFrameScale, _cfg.compactOverflow);
            _tilesRoot = Layer("Tiles");
            _gatesRoot = Layer("Gates");
            _barriersRoot = Layer("Barriers");
            _vehiclesRoot = Layer("Vehicles");
            _arrow = _ui.Image("HintArrow", _board.Cells, _sprites.Arrow, _cfg.hintArrowColor);
            _arrow.raycastTarget = false;
            _arrow.gameObject.AddComponent<Decoration>();   // over the vehicle on purpose
            Anchor(_arrow.rectTransform);
            _arrow.gameObject.SetActive(false);
            _tiles = new ComponentPool<Image>(() =>
            {
                var img = _ui.Image("Cell", _tilesRoot, _sprites.Tile, Color.white);
                img.raycastTarget = false;
                return img;
            });
            _barriers = new ComponentPool<Image>(() =>
            {
                var img = _ui.Image("Barrier", _barriersRoot, _sprites.Barrier, Color.white);
                img.raycastTarget = false;
                img.gameObject.AddComponent<Decoration>();   // on its cell slab on purpose
                return img;
            });
            _gates = new ComponentPool<Image>(() =>
            {
                var img = _ui.Image("Exit", _gatesRoot, _sprites.GateBody, Color.white);
                img.raycastTarget = false;
                img.gameObject.AddComponent<Decoration>();   // drawn in the window frame, outside the cells on purpose
                Anchor(img.rectTransform);
                var arrows = _ui.Image("Arrows", img.transform, _sprites.GateArrows, Color.white);
                arrows.raycastTarget = false;
                UiFactory.Stretch(arrows.rectTransform);
                return img;
            });
            _vehiclePool = new ComponentPool<TrafficVehicleView>(CreateVehicle);
        }

        RectTransform Layer(string name)
        {
            var rt = _ui.Rect(name, _board.Cells);
            UiFactory.Stretch(rt);
            return rt;
        }

        /// <summary>Positions in cell units from the top-left corner of the cells.</summary>
        static void Anchor(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        TrafficVehicleView CreateVehicle()
        {
            var go = new GameObject("Vehicle", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(_vehiclesRoot, false);
            Anchor(rt);
            var view = go.AddComponent<TrafficVehicleView>();
            view.Group = go.AddComponent<CanvasGroup>();
            view.Art = _ui.Rect("Art", rt);
            view.Art.anchorMin = view.Art.anchorMax = view.Art.pivot = new Vector2(0.5f, 0.5f);
            view.Wheels = ArtImage("Wheels", view.Art);
            view.Body = ArtImage("Body", view.Art);
            view.Body.raycastTarget = true;   // takes the touches of the whole vehicle
            view.Stripes = ArtImage("Stripes", view.Art);
            view.Details = ArtImage("Details", view.Art);
            view.Pressed = OnPressed;
            view.Dragged = OnDragged;
            view.Released = OnReleased;
            return view;
        }

        Image ArtImage(string name, RectTransform parent)
        {
            var img = _ui.Image(name, parent, null, Color.white);
            img.raycastTarget = false;
            UiFactory.Stretch(img.rectTransform);
            img.gameObject.AddComponent<Decoration>();   // the layers of one drawing, on top of each other on purpose
            return img;
        }

        // ------------------------------------------------------------------ PuzzleBoard

        public override void SetMoveLimit(int limit) => _puzzle.MoveLimit = Mathf.Max(0, limit);

        public override IReadOnlyList<int> MoveHistory => _puzzle.EncodedHistory();

        /// <summary>One character per vehicle: its position, or 'x' for a target car already out.</summary>
        public override string CaptureState() => _puzzle.CaptureState();

        public override bool RestoreMoves(IReadOnlyList<int> history)
        {
            if (history == null || history.Count == 0) return true;
            bool ok = _puzzle.ReplayEncoded(history);
            if (!ok) _puzzle.Restart();
            SnapVisuals();
            RaiseChanged();
            return ok;
        }

        public override void Load(LevelRequest request)
        {
            if (request.Mode == GameMode.Challenge)
            {
                LoadLevel(TrafficChallenge.ToLevel(request.Index + 1, "", request.Seed, request.Data, request.MinMoves));
                return;
            }
            var levels = _module.StoryLevels;
            if (levels.Count == 0) throw new InvalidOperationException("no Traffic jam Story levels");
            LoadLevel(levels[Mathf.Clamp(request.Index, 0, levels.Count - 1)]);
        }

        /// <summary>Plays any level (Story, Challenge, or a hand-made one for the previews and tests).</summary>
        public void LoadLevel(TrafficLevel level)
        {
            CancelDrag();
            _exiting = null;
            _hintTime = _winTime = -1f;
            _puzzle.Load(level);
            BuildPieces(level.seed);
            SnapVisuals();
            Refit();
            RaiseChanged();
        }

        void BuildPieces(int seed)
        {
            var layout = _puzzle.Layout;
            int w = layout.Width, h = layout.Height;
            var theme = Theme;
            _board.SetTheme(theme);
            _tiles.ReleaseAll();
            for (int c = 0; c < layout.CellCount; c++)
            {
                var tile = _tiles.Get();
                int x = c % w, y = c / w;
                SetCellAnchors(tile.rectTransform, x, y, w, h);
                tile.sprite = theme != null ? ThemeArt.Slab(theme, ArtVariants.Pick(seed, 3, c, ThemeArt.SlabVariants)) : _sprites.Tile;
                tile.name = $"Cell_{x}_{y}";
            }
            _barriers.ReleaseAll();
            _barrierCells.Clear();
            for (int c = 0; c < layout.CellCount; c++)
            {
                if ((layout.Obstacles >> c & 1) == 0) continue;
                var b = _barriers.Get();
                SetCellAnchors(b.rectTransform, c % w, c / w, w, h);
                _barrierCells.Add(c);
            }
            _gates.ReleaseAll();
            _gateImages.Clear();
            _vehiclePool.ReleaseAll();
            _vehicles.Clear();
            _facing = new int[layout.VehicleCount];
            int others = 0;
            for (int i = 0; i < layout.VehicleCount; i++)
            {
                var v = layout.Vehicles[i];
                var view = _vehiclePool.Get();
                view.Index = i;
                view.name = v.IsTarget ? (v.Target == 1 ? "TargetBlue" : "TargetRed") : (v.Kind == VehicleKind.Truck ? "Truck_" : "Car_") + i;
                bool truck = v.Kind == VehicleKind.Truck;
                view.Wheels.sprite = truck ? _sprites.TruckWheels : _sprites.CarWheels;
                view.Body.sprite = truck ? _sprites.TruckBody : _sprites.CarBody;
                view.Details.sprite = truck ? _sprites.TruckDetails : _sprites.CarDetails;
                view.Stripes.sprite = _sprites.CarStripes;
                view.Stripes.gameObject.SetActive(v.IsTarget);
                view.Stripes.color = _cfg.stripeColor;
                if (v.IsTarget)
                {
                    view.Body.color = _cfg.TargetColor(v.Target);
                    _facing[i] = v.ExitSide;
                    var gate = _gates.Get();
                    gate.color = _cfg.TargetColor(v.Target);
                    gate.name = "Exit_" + (v.Target == 1 ? "Blue" : "Red");
                    _gateImages.Add(gate);
                }
                else
                {
                    view.Body.color = _cfg.VehicleColor(ArtVariants.Pick(seed, 11, others, Mathf.Max(1, _cfg.vehicleColors.Count)));
                    bool flip = ArtVariants.Pick(seed, 12, others, 2) == 1;
                    _facing[i] = v.Horizontal ? (flip ? SquareTopology.Left : SquareTopology.Right) : (flip ? SquareTopology.Down : SquareTopology.Up);
                    others++;
                }
                view.StopSlide();
                view.StopShake();
                _vehicles.Add(view);
            }
        }

        static void SetCellAnchors(RectTransform rt, int x, int y, int w, int h)
        {
            rt.anchorMin = new Vector2((float)x / w, 1f - (float)(y + 1) / h);
            rt.anchorMax = new Vector2((float)(x + 1) / w, 1f - (float)y / h);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public override void Undo()
        {
            if (IsBusy || _drag != null || !_puzzle.Undo(out var move)) return;
            var view = _vehicles[move.Vehicle];
            if (move.IsExit)
            {
                // the car comes back in from its exit
                view.gameObject.SetActive(true);
                view.Group.alpha = 1f;
                view.Shown = OutPosition(move.Vehicle);
            }
            view.SlideTo(move.From, _cfg.snapDuration * 2f);
            RaiseChanged();
        }

        public override void Restart()
        {
            if (IsBusy) return;
            CancelDrag();
            _puzzle.Restart();
            SnapVisuals();
            RaiseChanged();
        }

        /// <summary>The light bulb (after the ad): an arrow on the vehicle of the next right move, then the vehicle slides there. Not a move.</summary>
        public override void ApplyHint()
        {
            if (!CanHint || !_puzzle.ApplyHint(out var move)) return;
            _hintMove = move;
            _hintVehicle = _vehicles[move.Vehicle];
            _hintTime = 0f;
            ShowArrow(move);
            RaiseChanged();
        }

        /// <summary>Editor previews: the arrow of the light bulb on the next right move, frozen (nothing moves).</summary>
        public void PreviewHintArrow()
        {
            if (_puzzle.TryFindHint(out var move)) ShowArrow(move);
        }

        /// <summary>Editor previews: the vehicle of the next right move held halfway through its drag.</summary>
        public void PreviewDrag()
        {
            if (!_puzzle.TryFindHint(out var move) || move.IsExit) return;
            var view = _vehicles[move.Vehicle];
            view.Shown = (move.From + move.To) / 2f;
            PlaceVehicle(view);
        }

        public override void Refit()
        {
            var layout = _puzzle.Layout;
            var size = ((RectTransform)transform).rect.size;
            if (layout == null || !_board.Fit(size, layout.Width, layout.Height, _cfg.exitDepth)) return;
            _fittedSize = size;
            float cell = _board.Cell, m = _board.Margin;
            int g = 0;
            for (int i = 0; i < layout.TargetCount && g < _gateImages.Count; i++, g++)
            {
                var v = layout.Vehicles[i];
                var rt = _gateImages[g].rectTransform;
                rt.sizeDelta = new Vector2(_cfg.exitWidth * cell, m);
                float along = (v.Line + 0.5f) * cell;
                switch (v.ExitSide)
                {
                    case SquareTopology.Up:
                        rt.anchoredPosition = new Vector2(along, m / 2f);
                        rt.localRotation = Quaternion.identity;
                        break;
                    case SquareTopology.Down:
                        rt.anchoredPosition = new Vector2(along, -layout.Height * cell - m / 2f);
                        rt.localRotation = Quaternion.Euler(0, 0, 180);
                        break;
                    case SquareTopology.Left:
                        rt.anchoredPosition = new Vector2(-m / 2f, -along);
                        rt.localRotation = Quaternion.Euler(0, 0, 90);
                        break;
                    default:
                        rt.anchoredPosition = new Vector2(layout.Width * cell + m / 2f, -along);
                        rt.localRotation = Quaternion.Euler(0, 0, -90);
                        break;
                }
            }
            foreach (var view in _vehicles) PlaceVehicle(view);
            if (_arrow.gameObject.activeSelf && _hintVehicle != null) PlaceArrow(_hintVehicle, _hintMove);
        }

        /// <summary>Slides and the light bulb jump to their end; a car driving out is out; nothing waits for the win panel.</summary>
        public override void SnapVisuals()
        {
            CancelDrag();
            _arrow.gameObject.SetActive(false);
            _hintTime = -1f;
            _hintVehicle = null;
            _exiting = null;
            _winTime = -1f;
            if (_puzzle.Layout == null) return;
            foreach (var view in _vehicles)
            {
                view.StopSlide();
                view.StopShake();
                int p = _puzzle.Pos(view.Index);
                bool inside = p != TrafficLayout.Exited;
                view.gameObject.SetActive(inside);
                view.Group.alpha = 1f;
                if (inside) view.Shown = p;
            }
            if (_board.Cell > 0) foreach (var view in _vehicles) PlaceVehicle(view);
        }

        public override void PreparePreview(BoardPreview state)
        {
            _puzzle.Restart();
            int moves = state == BoardPreview.Partial ? _puzzle.MinMoves / 2 : state == BoardPreview.Solved ? _puzzle.MinMoves : 0;
            for (int i = 0; i < moves && _puzzle.TryFindHint(out var move); i++) _puzzle.ApplyMove(move);
            SnapVisuals();
            RaiseChanged();
        }

        // ------------------------------------------------------------------ drawing

        float LineLength(int vehicle)
        {
            var layout = _puzzle.Layout;
            return layout.Vehicles[vehicle].Horizontal ? layout.Width : layout.Height;
        }

        /// <summary>Where a target car is fully out of the grid (position along its line, cells).</summary>
        float OutPosition(int vehicle)
        {
            var v = _puzzle.Layout.Vehicles[vehicle];
            return v.ExitsLow ? -(v.Length + 0.4f) : LineLength(vehicle) + 0.4f;
        }

        void PlaceVehicle(TrafficVehicleView view)
        {
            var v = _puzzle.Layout.Vehicles[view.Index];
            float cell = _board.Cell;
            var rt = (RectTransform)view.transform;
            float len = v.Length;
            Vector2 centre = v.Horizontal
                ? new Vector2((view.Shown + len / 2f) * cell, -(v.Line + 0.5f) * cell)
                : new Vector2((v.Line + 0.5f) * cell, -(view.Shown + len / 2f) * cell);
            rt.sizeDelta = v.Horizontal ? new Vector2(len * cell, cell) : new Vector2(cell, len * cell);
            view.Place(centre);
            view.Art.sizeDelta = new Vector2(cell, len * cell);
            view.Art.localRotation = Quaternion.Euler(0, 0, -90f * _facing[view.Index]);
        }

        void ShowArrow(TrafficMove move)
        {
            _arrow.gameObject.SetActive(true);
            _arrow.transform.SetAsLastSibling();
            PlaceArrow(_vehicles[move.Vehicle], move);
        }

        void PlaceArrow(TrafficVehicleView view, TrafficMove move)
        {
            var v = _puzzle.Layout.Vehicles[move.Vehicle];
            int dir = move.IsExit ? v.ExitSide
                : v.Horizontal ? (move.To > move.From ? SquareTopology.Right : SquareTopology.Left)
                : (move.To > move.From ? SquareTopology.Down : SquareTopology.Up);
            var rt = _arrow.rectTransform;
            float size = _board.Cell * 0.8f;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = ((RectTransform)view.transform).anchoredPosition;
            rt.localRotation = Quaternion.Euler(0, 0, -90f * dir);
        }

        // ------------------------------------------------------------------ input

        bool OutOfMoves => _puzzle.MoveLimit > 0 && _puzzle.MovesMade >= _puzzle.MoveLimit;

        void OnPressed(TrafficVehicleView view, PointerEventData e)
        {
            if (_drag != null || IsBusy || _puzzle.IsSolved || _puzzle.IsOut(view.Index)) return;
            if (OutOfMoves)
            {
                view.StartShake();   // Challenge: no move left (the Core shows "Out of moves")
                return;
            }
            view.StopSlide();
            view.Shown = _puzzle.Pos(view.Index);
            _puzzle.Range(view.Index, out _dragLo, out _dragHi, out _dragCanExit);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_board.Cells, e.position, e.pressEventCamera, out _dragStart);
            _drag = view;
            _dragPointer = e.pointerId;
            _dragFrom = _puzzle.Pos(view.Index);
        }

        void OnDragged(TrafficVehicleView view, PointerEventData e)
        {
            if (view != _drag || e.pointerId != _dragPointer || _board.Cell <= 0) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_board.Cells, e.position, e.pressEventCamera, out var now);
            var v = _puzzle.Layout.Vehicles[view.Index];
            float delta = v.Horizontal ? (now.x - _dragStart.x) / _board.Cell : -(now.y - _dragStart.y) / _board.Cell;
            float min = _dragLo, max = _dragHi;
            if (_dragCanExit)
            {
                if (v.ExitsLow) min -= v.Length;   // a free target car can be pulled out through its exit
                else max += v.Length;
            }
            view.Shown = Mathf.Clamp(_dragFrom + delta, min, max);
            PlaceVehicle(view);
        }

        void OnReleased(TrafficVehicleView view, PointerEventData e)
        {
            if (view != _drag || e.pointerId != _dragPointer) return;
            _drag = null;
            var v = _puzzle.Layout.Vehicles[view.Index];
            bool exit = _dragCanExit && (v.ExitsLow ? view.Shown < _dragLo - _cfg.exitThreshold : view.Shown > _dragHi + _cfg.exitThreshold);
            if (exit && _puzzle.ApplyMove(new TrafficMove(view.Index, _dragFrom, TrafficLayout.Exited)))
            {
                StartExit(view, _cfg.exitDuration);
                RaiseChanged();
                return;
            }
            int to = Mathf.Clamp(Mathf.RoundToInt(view.Shown), _dragLo, _dragHi);
            if (to != _dragFrom && _puzzle.ApplyMove(new TrafficMove(view.Index, _dragFrom, to)))
            {
                view.SlideTo(to, _cfg.snapDuration);
                RaiseChanged();
                return;
            }
            view.SlideTo(_dragFrom, _cfg.snapDuration);   // back where it was: no move
        }

        void CancelDrag()
        {
            if (_drag == null) return;
            _drag.SlideTo(_puzzle.IsOut(_drag.Index) ? _drag.Shown : _puzzle.Pos(_drag.Index), _cfg.snapDuration);
            _drag = null;
        }

        void StartExit(TrafficVehicleView view, float duration)
        {
            _exiting = view;
            _exitFrom = view.Shown;
            view.SlideTo(OutPosition(view.Index), duration);
        }

        // ------------------------------------------------------------------ animation (real time)

        void Update()
        {
            if (_puzzle.Layout == null) return;
            float dt = Time.unscaledDeltaTime;
            foreach (var view in _vehicles)
            {
                if (!view.gameObject.activeSelf) continue;
                bool sliding = view.IsSliding;
                bool ended = view.TickSlide(dt);
                if (sliding) PlaceVehicle(view);
                if (view == _exiting)
                {
                    // fades out over the last part of the way out
                    float total = Mathf.Abs(OutPosition(view.Index) - _exitFrom);
                    float left = Mathf.Abs(OutPosition(view.Index) - view.Shown);
                    view.Group.alpha = Mathf.Clamp01(left / Mathf.Max(0.01f, total * 0.45f));
                    if (ended) FinishExit(view);
                }
                var v = _puzzle.Layout.Vehicles[view.Index];
                if (view.IsShaking) view.TickShake(dt, _cfg.shakeDuration, _cfg.shakeAmplitude, _cfg.shakeCycles, v.Horizontal, _board.Cell);
            }
            if (_hintTime >= 0f)
            {
                _hintTime += dt;
                if (_hintTime >= _cfg.hintArrowTime && _hintVehicle != null)
                {
                    _arrow.gameObject.SetActive(false);
                    var view = _hintVehicle;
                    _hintVehicle = null;
                    _hintTime = -1f;
                    if (_hintMove.IsExit) StartExit(view, _cfg.exitDuration);
                    else view.SlideTo(_hintMove.To, _cfg.hintMoveDuration);
                    RaiseChanged();
                }
            }
            if (_winTime >= 0f)
            {
                _winTime += dt;
                if (_winTime >= _cfg.winPause)
                {
                    _winTime = -1f;
                    RaiseChanged();
                    RaiseCompleted();
                }
            }
        }

        void FinishExit(TrafficVehicleView view)
        {
            _exiting = null;
            view.gameObject.SetActive(false);
            view.Group.alpha = 1f;
            if (_puzzle.IsSolved) _winTime = 0f;
            RaiseChanged();
        }

        void LateUpdate()
        {
            if (((RectTransform)transform).rect.size != _fittedSize) Refit();
        }
    }
}
