using ResolverMaster.Core.Audio;
using System;
using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Games.Penguin.View
{
    /// <summary>
    /// The Pino board on the Core game screen (PG-001), inside the Core <see cref="BoardFrame"/> (cream window, ground of the chapter
    /// theme between the tiles), with the border (snow wall or water pieces) in the room around the cells. Swipes anywhere on the board
    /// (Core <see cref="SwipeArea"/>, a tap is not a swipe) move the penguins; swipes are ignored while something moves. The slides come
    /// from the times computed by the rules (<see cref="PenguinTrace"/>, <see cref="PenguinPlay"/>) and are drawn on the real elapsed time
    /// (same speed at 30 and 60 fps).
    /// PG-002, the drawings (<see cref="PenguinArt"/>): tiles of ice, snow and water turned and mirrored by position
    /// (<see cref="PenguinLook.TileLook"/>), the sunken block where a hole was filled; the penguins seen from above while they play (still or
    /// sliding, turned towards the move) and "turning towards us" for an expression (<see cref="PenguinMood"/>): normal at the start, scared
    /// when they fall (a stop against something keeps the view from above, ErNa-012), happy in the igloo; igloos closed/open with a little bounce, the fish jumping away. Rows
    /// lower on the screen are drawn over the rows above (penguins seen from the front and igloos stick out upwards).
    /// </summary>
    public sealed class PenguinBoardView : PuzzleBoard
    {
        const int Actors = 5, FirstBlock = 2;

        sealed class PenguinView
        {
            public RectTransform Root;
            /// <summary>Stretched over the root: the horizontal squash of the turn.</summary>
            public RectTransform Turn;
            public CanvasGroup Group;
            public Image Top, Front;
            public readonly PenguinMood Mood = new PenguinMood();
        }

        sealed class IglooView
        {
            public RectTransform Root;
            public Image Image;
        }

        enum FxKind
        {
            Splash,
            Sparkle,
        }

        struct Fx
        {
            public Image Image;
            public FxKind Kind;
            public float Time, Duration;
        }

        PenguinModule _module;
        PenguinConfig _cfg;
        PenguinSprites _sprites;
        PenguinArt _art;
        UiFactory _ui;
        BoardFrame _board;
        RectTransform _tilesRoot, _terrainRoot, _borderRoot, _itemsRoot, _blocksRoot, _actorsRoot, _fxRoot;
        ComponentPool<Image> _tiles, _terrain, _edges, _fishPool, _blockPool, _fxPool;
        readonly PenguinView[] _penguins = new PenguinView[PenguinLayout.MaxPenguins];
        readonly IglooView[] _igloos = new IglooView[PenguinLayout.MaxPenguins];
        readonly Image[] _arrows = new Image[PenguinLayout.MaxPenguins];
        readonly List<Image> _fishImages = new List<Image>();
        readonly List<Image> _blockImages = new List<Image>();
        readonly List<Image> _rockImages = new List<Image>();
        readonly List<Image> _tileImages = new List<Image>();
        // sizes of fish, blocks and rocks as a share of the cell, never bigger than their drawings (Refit)
        float _fishScale = 0.7f, _blockScale = 0.86f, _rockScale = 0.94f, _tileScale = 1f;
        readonly Dictionary<int, Image> _waterImages = new Dictionary<int, Image>();
        // border pieces: the image, its edge (or -1 - corner), its place along the edge, water or wall
        readonly List<Image> _edgeImages = new List<Image>();
        readonly List<int> _edgeOf = new List<int>();
        readonly List<int> _edgePart = new List<int>();
        readonly List<bool> _edgeWater = new List<bool>();
        readonly List<Fx> _fx = new List<Fx>();
        readonly List<(float row, int kind, RectTransform rt)> _order = new List<(float, int, RectTransform)>();
        readonly PenguinPuzzle _puzzle = new PenguinPuzzle();
        PenguinPlay _play;
        SwipeArea _swipe;
        Vector2 _fittedSize;
        /// <summary>No swipe yet since the level started: the penguins look at us (normal face).</summary>
        bool _fresh = true;

        // the move being played: start cells, the block view of each block actor, which track ends and events already ran
        readonly Vector2[] _start = new Vector2[Actors];
        readonly Image[] _actorBlock = new Image[Actors];
        readonly List<bool> _trackDone = new List<bool>();
        readonly List<bool> _eventDone = new List<bool>();
        readonly float[] _bump = new float[Actors];
        readonly float[] _vanish = new float[Actors];
        readonly bool[] _vanishStarted = new bool[Actors];
        readonly Vector2[] _vanishAt = new Vector2[Actors];
        readonly PenguinTrackEnd[] _vanishKind = new PenguinTrackEnd[Actors];
        /// <summary>The penguins that fell in the last move (gone, but not in their igloo).</summary>
        readonly bool[] _fallen = new bool[PenguinLayout.MaxPenguins];
        float[] _fishPop = Array.Empty<float>();
        float _iglooOpen = -1f;
        float _winTime = -1f, _failTime = -1f, _hintTime = -1f;

        public PenguinPuzzle Puzzle => _puzzle;
        public override IPuzzleSession Session => _puzzle;
        public override RectTransform GridRect => _board.Cells;
        public override int GridColumns => _puzzle.Layout?.Width ?? 0;
        public override int GridRows => _puzzle.Layout?.Height ?? 0;
        public override bool IsBusy => _play.Busy || _hintTime >= 0f || _winTime >= 0f || _failTime >= 0f;
        public override bool CanHint => !IsBusy && _puzzle.Layout != null && !_puzzle.IsSolved && !_puzzle.Fell && (_puzzle.IsStuck || _puzzle.TryFindHint(out _));
        public override bool HintUsed => _puzzle.HintUsed;
        public override bool LosesWhenOutOfMoves => true;
        public override bool IsStuck => _puzzle.IsStuck;
        /// <summary>"Splash!" after a fall: the game screen keeps the room of the message under the grid.</summary>
        public override bool ShowsMessages => true;
        public float CellSize => _board.Cell;

        /// <summary>The drawn penguins, the drawing shown now (previews measure them).</summary>
        public IEnumerable<RectTransform> PenguinRects
        {
            get
            {
                foreach (var p in _penguins)
                {
                    if (p == null || !p.Root.gameObject.activeInHierarchy) continue;
                    var img = p.Mood.Front ? p.Front : p.Top;
                    if (img.gameObject.activeInHierarchy) yield return img.rectTransform;
                }
            }
        }

        /// <summary>What the penguins show now: "front Normal", "top Slide", ... (previews report it).</summary>
        public IEnumerable<string> PenguinPoses
        {
            get
            {
                for (int i = 0; i < _penguins.Length; i++)
                {
                    var p = _penguins[i];
                    if (p != null && p.Root.gameObject.activeInHierarchy) yield return p.Mood.SpriteName(i);
                }
            }
        }

        public static PenguinBoardView Create(AppServices ctx, RectTransform parent, PenguinModule module)
        {
            var go = new GameObject("PenguinBoard", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            UiFactory.Stretch(rt);
            var view = go.AddComponent<PenguinBoardView>();
            view.Build(ctx, module);
            return view;
        }

        void Build(AppServices ctx, PenguinModule module)
        {
            _module = module;
            _ui = ctx.Ui;
            _cfg = PenguinConfig.LoadOrDefault();
            _sprites = PenguinSprites.For(_cfg);
            _art = PenguinArt.Load();
            _puzzle.MaxStates = Mathf.Max(1000, _cfg.maxStates);
            _play = new PenguinPlay(_puzzle) { CellsPerSecond = Mathf.Max(1f, _cfg.slideSpeed) };
            _board = BoardFrame.Create(_ui, transform, _cfg.boardInset, _cfg.compactCellBelow, _cfg.compactFrameScale, _cfg.compactOverflow);
            _tilesRoot = Layer("Tiles");
            _terrainRoot = Layer("Terrain");
            _borderRoot = Layer("Border");
            _itemsRoot = Layer("Items");
            _blocksRoot = Layer("Blocks");
            _actorsRoot = Layer("Actors");
            _fxRoot = Layer("Effects");
            _tiles = new ComponentPool<Image>(() =>
            {
                var img = _ui.Image("Cell", _tilesRoot, null, Color.white);
                img.raycastTarget = false;
                return img;
            });
            _terrain = new ComponentPool<Image>(() => Piece("Terrain", _terrainRoot, cellAnchored: true));
            _edges = new ComponentPool<Image>(() => Piece("Edge", _borderRoot, cellAnchored: false));
            _fishPool = new ComponentPool<Image>(() => Piece("Fish", _itemsRoot, cellAnchored: true));
            _blockPool = new ComponentPool<Image>(() => Piece("Block", _blocksRoot, cellAnchored: false));
            _fxPool = new ComponentPool<Image>(() => Piece("Effect", _fxRoot, cellAnchored: false));
            for (int i = 0; i < PenguinLayout.MaxPenguins; i++)
            {
                _igloos[i] = BuildIgloo(i);
                _penguins[i] = BuildPenguin(i);
                _arrows[i] = Piece("HintArrow" + (i + 1), _fxRoot, cellAnchored: false);
                _arrows[i].sprite = _sprites.Arrow;
                _arrows[i].color = _cfg.hintArrowColor;
                _arrows[i].gameObject.SetActive(false);
            }
            // swipes anywhere on the board, on top of everything (a tap is not a swipe)
            _swipe = SwipeArea.Create(transform);
            _swipe.Threshold = () => Mathf.Max(_cfg.swipeMinUnits, _cfg.swipeThreshold * Mathf.Max(1f, _board.Cell));
            _swipe.Swiped += OnSwiped;
        }

        RectTransform Layer(string name)
        {
            var rt = _ui.Rect(name, _board.Cells);
            UiFactory.Stretch(rt);
            return rt;
        }

        /// <summary>A piece drawn over the cells on purpose (a <see cref="Decoration"/>), anchored to a cell or placed in cell units.</summary>
        Image Piece(string name, Transform parent, bool cellAnchored)
        {
            var img = _ui.Image(name, parent, null, Color.white);
            img.raycastTarget = false;
            img.gameObject.AddComponent<Decoration>();
            if (!cellAnchored) Anchor(img.rectTransform);
            return img;
        }

        /// <summary>A drawing inside an actor, anchored at <paramref name="anchor"/> of its parent with the same pivot.</summary>
        Image Drawing(string name, Transform parent, Vector2 anchor)
        {
            var img = _ui.Image(name, parent, null, Color.white);
            img.raycastTarget = false;
            img.gameObject.AddComponent<Decoration>();
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = Vector2.zero;
            return img;
        }

        PenguinView BuildPenguin(int i)
        {
            var root = _ui.Rect("Penguin" + (i + 1), _actorsRoot);
            Anchor(root);
            var v = new PenguinView { Root = root, Group = root.gameObject.AddComponent<CanvasGroup>() };
            v.Group.blocksRaycasts = false;
            v.Turn = _ui.Rect("Turn", root);
            UiFactory.Stretch(v.Turn);
            v.Top = Drawing("Top", v.Turn, new Vector2(0.5f, 0.5f));      // the body centred: it turns around the centre of the cell
            v.Front = Drawing("Front", v.Turn, new Vector2(0.5f, 0f));    // the feet near the bottom of the cell
            v.Mood.TurnTime = Mathf.Max(0.01f, _cfg.turnDuration);
            root.gameObject.SetActive(false);
            return v;
        }

        IglooView BuildIgloo(int i)
        {
            var root = _ui.Rect("Igloo" + (i + 1), _actorsRoot);
            var v = new IglooView { Root = root, Image = Drawing("Igloo", root, new Vector2(0.5f, 0f)) };
            root.gameObject.SetActive(false);
            return v;
        }

        /// <summary>Positions in cell units from the top-left corner of the cells.</summary>
        static void Anchor(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static void SetCellAnchors(RectTransform rt, int x, int y, int w, int h)
        {
            rt.anchorMin = new Vector2((float)x / w, 1f - (float)(y + 1) / h);
            rt.anchorMax = new Vector2((float)(x + 1) / w, 1f - (float)y / h);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>The deterministic turn and mirror of a tile (<see cref="PenguinLook.TileLook"/>).</summary>
        static void TurnTile(RectTransform rt, int x, int y, int salt, float scale = 1f)
        {
            PenguinLook.TileLook(x, y, salt, out int quarter, out bool mirrored);
            rt.localRotation = Quaternion.Euler(0, 0, 90f * quarter);
            rt.localScale = new Vector3(mirrored ? -scale : scale, scale, 1f);
        }

        int BorderPieces => Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Max(0.1f, _cfg.borderDepth)));

        // ------------------------------------------------------------------ PuzzleBoard

        public override void SetMoveLimit(int limit) => _puzzle.MoveLimit = Mathf.Max(0, limit);

        public override IReadOnlyList<int> MoveHistory => _puzzle.EncodedHistory();

        /// <summary>The packed state (penguins, fish, blocks, holes, penguins in) in hexadecimal.</summary>
        public override string CaptureState() => _puzzle.CaptureState();

        public override bool RestoreMoves(IReadOnlyList<int> history)
        {
            if (history == null || history.Count == 0) return true;
            bool ok = _puzzle.ReplayEncoded(history);
            if (!ok) _puzzle.Restart();
            _fresh = _puzzle.MovesMade == 0;
            SnapVisuals();
            RaiseChanged();
            return ok;
        }

        public override void Load(LevelRequest request)
        {
            if (request.Mode == GameMode.Challenge)
            {
                LoadLevel(PenguinChallenge.ToLevel(request.Index + 1, "", request.Seed, request.Data, request.MinMoves));
                return;
            }
            var levels = _module.StoryLevels;
            if (levels.Count == 0) throw new InvalidOperationException("no Pino Story levels");
            LoadLevel(levels[Mathf.Clamp(request.Index, 0, levels.Count - 1)]);
        }

        /// <summary>Plays any level (Story, Challenge, or a hand-made one for the previews).</summary>
        public void LoadLevel(PenguinLevel level)
        {
            StopAll();
            _puzzle.Load(level);
            _fresh = true;
            BuildPieces();
            SnapVisuals();
            Refit();
            RaiseChanged();
        }

        void BuildPieces()
        {
            var l = _puzzle.Layout;
            int w = l.Width, h = l.Height;
            _board.SetTheme(Theme);
            // the ground: ice, snow and water tiles (rocks stand on ice), each turned and mirrored by its position
            _tiles.ReleaseAll();
            _tileImages.Clear();
            _waterImages.Clear();
            for (int c = 0; c < l.CellCount; c++)
            {
                var tile = _tiles.Get();
                var t = l.Terrain(c);
                int x = c % w, y = c / w;
                SetCellAnchors(tile.rectTransform, x, y, w, h);
                tile.sprite = _art.Get(t == PenguinTerrain.Snow ? PenguinLook.TileSnow : t == PenguinTerrain.Water ? PenguinLook.TileWater : PenguinLook.TileIce);
                tile.color = Color.white;
                tile.name = $"{(t == PenguinTerrain.Rock ? PenguinTerrain.Ice : t)}_{x}_{y}";
                TurnTile(tile.rectTransform, x, y, (int)t, _tileScale);
                _tileImages.Add(tile);
                if (t == PenguinTerrain.Water) _waterImages[c] = tile;
            }
            _terrain.ReleaseAll();
            _rockImages.Clear();
            for (int c = 0; c < l.CellCount; c++)
            {
                if (l.Terrain(c) != PenguinTerrain.Rock) continue;
                var img = _terrain.Get();
                SetCellAnchors(img.rectTransform, c % w, c / w, w, h);
                img.sprite = _art.Get(PenguinLook.Rock);
                img.preserveAspect = true;
                img.name = $"Rock_{c % w}_{c / w}";
                img.rectTransform.localScale = Vector3.one * _rockScale;
                img.color = Color.white;
                _rockImages.Add(img);
            }
            // the border: square pieces of snow wall (never turned: the visible side is at the bottom) or water, corners included
            _edges.ReleaseAll();
            _edgeImages.Clear();
            _edgeOf.Clear();
            _edgePart.Clear();
            _edgeWater.Clear();
            int n = BorderPieces;
            for (int e = 0; e < l.EdgeCount; e++)
                for (int k = 0; k < n; k++)
                    AddBorderPiece(e, k, l.EdgeWater(e));
            int[] cornerCells = { l.Cell(0, 0), l.Cell(w - 1, 0), l.Cell(w - 1, h - 1), l.Cell(0, h - 1) };
            int[,] cornerDirs = { { SquareTopology.Up, SquareTopology.Left }, { SquareTopology.Up, SquareTopology.Right }, { SquareTopology.Down, SquareTopology.Right }, { SquareTopology.Down, SquareTopology.Left } };
            for (int k = 0; k < 4; k++)
                AddBorderPiece(-1 - k, 0, l.EdgeWaterAt(cornerCells[k], cornerDirs[k, 0]) && l.EdgeWaterAt(cornerCells[k], cornerDirs[k, 1]));
            _fishPool.ReleaseAll();
            _fishImages.Clear();
            for (int f = 0; f < l.FishCount; f++)
            {
                var img = _fishPool.Get();
                int c = l.FishCell(f);
                SetCellAnchors(img.rectTransform, c % w, c / w, w, h);
                img.sprite = _art.Get(PenguinLook.Fish);
                img.preserveAspect = true;
                img.name = "Fish_" + f;
                _fishImages.Add(img);
            }
            _fishPop = new float[l.FishCount];
            for (int i = 0; i < PenguinLayout.MaxPenguins; i++)
            {
                bool used = i < l.PenguinCount;
                _igloos[i].Root.gameObject.SetActive(used);
                if (used) SetCellAnchors(_igloos[i].Root, l.X(l.Igloo(i)), l.Y(l.Igloo(i)), w, h);
            }
            _blockPool.ReleaseAll();
            _blockImages.Clear();
            for (int j = 0; j < l.BlockCount; j++)
            {
                var img = _blockPool.Get();
                img.sprite = _art.Get(PenguinLook.IceBlock);
                img.preserveAspect = true;
                img.name = "Block_" + j;
                _blockImages.Add(img);
            }
        }

        void AddBorderPiece(int edge, int part, bool water)
        {
            var img = _edges.Get();
            img.sprite = _art.Get(water ? PenguinLook.TileWater : PenguinLook.TileSnowWall);
            img.name = (water ? "EdgeWater_" : "EdgeWall_") + (edge >= 0 ? edge + "_" + part : "Corner" + (-1 - edge));
            img.color = Color.white;
            _edgeImages.Add(img);
            _edgeOf.Add(edge);
            _edgePart.Add(part);
            _edgeWater.Add(water);
        }

        public override void Undo()
        {
            if (IsBusy || !_puzzle.Undo(out _)) return;
            _fresh = _puzzle.MovesMade == 0;
            SnapVisuals();
            RaiseChanged();
        }

        public override void Restart()
        {
            if (IsBusy) return;
            _puzzle.Restart();
            _fresh = true;
            SnapVisuals();
            RaiseChanged();
        }

        /// <summary>
        /// The light bulb (after the ad): stuck = back to the last state with a solution at once; otherwise an arrow on the penguins,
        /// then the next right swipe (it counts 1 move, ErNa-011).
        /// </summary>
        public override void ApplyHint()
        {
            if (!CanHint) return;
            if (_puzzle.IsStuck)
            {
                _play.Hint();
                _fresh = _puzzle.MovesMade == 0;
                SnapVisuals();
                RaiseChanged();
                return;
            }
            if (!_puzzle.TryFindHint(out int dir)) return;
            ShowArrows(dir);
            _hintTime = 0f;
            RaiseChanged();
        }

        public override void Refit()
        {
            var l = _puzzle.Layout;
            var size = ((RectTransform)transform).rect.size;
            if (l == null || !_board.Fit(size, l.Width, l.Height, _cfg.borderDepth)) return;
            _fittedSize = size;
            float cell = _board.Cell;
            _fishScale = Share(PenguinLook.Fish, _cfg.fishSize, cell);
            _blockScale = Share(PenguinLook.IceBlock, _cfg.blockSize, cell);
            _rockScale = Share(PenguinLook.Rock, _cfg.rockSize, cell);
            foreach (var rock in _rockImages) rock.rectTransform.localScale = Vector3.one * _rockScale;
            // the tiles fill their cell, but never bigger than their drawing (a thin gap shows the ground of the theme on big cells)
            _tileScale = 1f;
            foreach (var name in new[] { PenguinLook.TileIce, PenguinLook.TileSnow, PenguinLook.TileWater, PenguinLook.TileSunkenBlock })
            {
                var sprite = _art.Get(name);
                if (sprite != null) _tileScale = Mathf.Min(_tileScale, Mathf.Min(sprite.rect.width, sprite.rect.height) / cell);
            }
            foreach (var tile in _tileImages)
            {
                var ts = tile.rectTransform.localScale;
                tile.rectTransform.localScale = new Vector3(Mathf.Sign(ts.x) * _tileScale, _tileScale, 1f);
            }
            foreach (var fish in _fishImages)
                if (fish.gameObject.activeSelf) fish.rectTransform.localScale = Vector3.one * _fishScale;
            PlaceBorder(l, cell);
            // drawings inside the actors: one size per family (the drawings of a family share one canvas)
            for (int i = 0; i < _penguins.Length; i++)
            {
                var v = _penguins[i];
                float th = _cfg.topPenguinHeight * cell, fh = _cfg.frontPenguinHeight * cell;
                v.Top.rectTransform.sizeDelta = new Vector2(th * _art.Aspect(PenguinLook.TopName(i, false)), th);
                v.Front.rectTransform.sizeDelta = new Vector2(fh * _art.Aspect(PenguinLook.FrontName(i, PenguinFace.Normal)), fh);
                v.Front.rectTransform.anchoredPosition = new Vector2(0f, _cfg.frontPenguinFeet * cell);
                var ig = _igloos[i].Image.rectTransform;
                float iw = _cfg.iglooWidth * cell;
                ig.sizeDelta = new Vector2(iw, iw / _art.Aspect(PenguinLook.IglooName(i, false)));
                ig.anchoredPosition = new Vector2(0f, cell * 0.02f);
            }
            if (_play.Busy) Animate(_play.TraceTime);
            else PlaceActors();
            for (int i = 0; i < _arrows.Length; i++)
                if (_arrows[i].gameObject.activeSelf) PlaceArrow(i, _hintDir);
        }

        /// <summary>
        /// A drawing in a square of <paramref name="share"/> of the cell (aspect kept): the share, made smaller if the drawing would be
        /// scaled up (never bigger than its resolution).
        /// </summary>
        float Share(string name, float share, float cell)
        {
            var s = _art.Get(name);
            if (s == null || cell <= 0f) return share;
            return Mathf.Min(share, Mathf.Max(s.rect.width, s.rect.height) / cell);
        }

        /// <summary>
        /// The border pieces around the cells: <see cref="BorderPieces"/> squares along each cell side, one in each corner. The snow wall
        /// keeps its proportions (top aligned: its visible side may stick out over the piece below); water tiles fill their square, turned
        /// and mirrored like the ground. Pieces lower on the screen are drawn over the ones above.
        /// </summary>
        void PlaceBorder(PenguinLayout l, float cell)
        {
            float depth = Mathf.Max(1f, _board.Margin - _board.Inset), len = cell / BorderPieces;
            float wpx = l.Width * cell, hpx = l.Height * cell;
            float wallAspect = _art.Aspect(PenguinLook.TileSnowWall);
            _order.Clear();
            for (int k = 0; k < _edgeImages.Count; k++)
            {
                int e = _edgeOf[k], part = _edgePart[k];
                float x0, top, w, h;
                if (e >= 0)
                {
                    l.EdgeCell(e, out int c, out int dir);
                    float along = l.X(c) * cell + part * len, down = l.Y(c) * cell + part * len;
                    switch (dir)
                    {
                        case SquareTopology.Up: x0 = along; top = depth; w = len; h = depth; break;
                        case SquareTopology.Down: x0 = along; top = -hpx; w = len; h = depth; break;
                        case SquareTopology.Left: x0 = -depth; top = -down; w = depth; h = len; break;
                        default: x0 = wpx; top = -down; w = depth; h = len; break;
                    }
                }
                else
                {
                    int corner = -1 - e;
                    x0 = corner == 0 || corner == 3 ? -depth : wpx;
                    top = corner < 2 ? depth : -hpx;
                    w = h = depth;
                }
                var rt = _edgeImages[k].rectTransform;
                if (_edgeWater[k])
                {
                    rt.sizeDelta = new Vector2(w, h);
                    rt.anchoredPosition = new Vector2(x0 + w / 2f, top - h / 2f);
                    TurnTile(rt, e, part, 101);
                }
                else
                {
                    float wh = w / Mathf.Max(0.01f, wallAspect);
                    rt.sizeDelta = new Vector2(w, wh);
                    rt.anchoredPosition = new Vector2(x0 + w / 2f, top - wh / 2f);
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }
                _order.Add((-top + x0 / (wpx + 4f * depth + 1f) * 0.01f, 0, rt));
            }
            ApplyOrder();
        }

        /// <summary>Every animation jumps to its end (the board shows the state of the rules).</summary>
        public override void SnapVisuals()
        {
            StopAll();
            if (_puzzle.Layout == null) return;
            ShowState(resetPoses: true);
            if (_board.Cell > 0) PlaceActors();
        }

        void StopAll()
        {
            _play?.Stop();
            _hintTime = _winTime = _failTime = -1f;
            _iglooOpen = -1f;
            for (int a = 0; a < Actors; a++)
            {
                _bump[a] = -1f;
                _vanish[a] = -1f;
                _actorBlock[a] = null;
            }
            for (int f = 0; f < _fishPop.Length; f++) _fishPop[f] = -1f;
            foreach (var fx in _fx) _fxPool.Release(fx.Image);
            _fx.Clear();
            foreach (var a in _arrows) a.gameObject.SetActive(false);
        }

        /// <summary>A penguin gone from the grid that is in its igloo (not one that fell in the last move).</summary>
        bool InIgloo(ulong s, int i) =>
            i < _puzzle.Layout.PenguinCount && PenguinLayout.PenguinPos(s, i) == PenguinLayout.Gone && !(_puzzle.Fell && _fallen[i]);

        /// <summary>
        /// What is visible in the current state of the rules: penguins on the grid or in their igloo, fish not taken, igloos closed or open,
        /// blocks, water holes and sunken blocks. <paramref name="resetPoses"/>: the penguins also take the pose of the state (normal before
        /// the first swipe, from above while playing, happy in the igloo); otherwise a running expression goes on.
        /// </summary>
        void ShowState(bool resetPoses)
        {
            var l = _puzzle.Layout;
            ulong s = _puzzle.State;
            if (!_puzzle.Fell)
                for (int i = 0; i < _fallen.Length; i++) _fallen[i] = false;
            for (int a = 0; a < Actors; a++) _vanish[a] = -1f;
            bool open = l.IglooOpen(s);
            for (int i = 0; i < PenguinLayout.MaxPenguins; i++)
            {
                var v = _penguins[i];
                bool used = i < l.PenguinCount;
                bool inIgloo = used && InIgloo(s, i);
                bool shown = used && (PenguinLayout.PenguinPos(s, i) != PenguinLayout.Gone || inIgloo);
                v.Root.gameObject.SetActive(shown);
                v.Group.alpha = 1f;
                v.Root.localScale = Vector3.one;
                if (resetPoses)
                {
                    if (inIgloo) v.Mood.Show(true, PenguinFace.Happy);
                    else v.Mood.Show(_fresh, PenguinFace.Normal);
                }
                ApplyPose(i);
                if (used)
                {
                    var ig = _igloos[i];
                    ig.Image.sprite = _art.Get(PenguinLook.IglooName(i, open));
                    ig.Image.rectTransform.localScale = Vector3.one;
                }
            }
            for (int f = 0; f < _fishImages.Count; f++)
            {
                var img = _fishImages[f];
                img.gameObject.SetActive(!l.FishTaken(s, f));
                img.color = Color.white;
                img.rectTransform.localScale = Vector3.one * _fishScale;
                img.rectTransform.anchoredPosition = Vector2.zero;
            }
            foreach (var kv in _waterImages) kv.Value.sprite = _art.Get(l.IsWater(s, kv.Key) ? PenguinLook.TileWater : PenguinLook.TileSunkenBlock);
            for (int j = 0; j < _blockImages.Count; j++)
            {
                var img = _blockImages[j];
                bool shown = PenguinLayout.BlockPos(s, j) != PenguinLayout.Gone;
                img.gameObject.SetActive(shown);
                img.color = Color.white;
                img.rectTransform.localScale = Vector3.one;
            }
        }

        /// <summary>The drawing of the pose of penguin <paramref name="i"/>: from above (turned towards its move) or from the front, and the squash of the turn.</summary>
        void ApplyPose(int i)
        {
            var v = _penguins[i];
            var m = v.Mood;
            var img = m.Front ? v.Front : v.Top;
            var other = m.Front ? v.Top : v.Front;
            var sprite = _art.Get(m.SpriteName(i));
            other.gameObject.SetActive(false);
            img.gameObject.SetActive(sprite != null);
            img.sprite = sprite;
            if (!m.Front) img.rectTransform.localRotation = Quaternion.Euler(0, 0, PenguinLook.TopRotation(m.Dir));
            v.Turn.localScale = new Vector3(m.SquashX, 1f, 1f);
        }

        /// <summary>Penguins (on the grid or in their igloo) and blocks on the cells of the current state.</summary>
        void PlaceActors()
        {
            var l = _puzzle.Layout;
            ulong s = _puzzle.State;
            for (int i = 0; i < l.PenguinCount; i++)
            {
                int p = PenguinLayout.PenguinPos(s, i);
                if (p != PenguinLayout.Gone) Place(_penguins[i].Root, CellPos(p));
                else if (InIgloo(s, i)) Place(_penguins[i].Root, CellPos(l.Igloo(i)));
            }
            for (int j = 0; j < _blockImages.Count; j++)
            {
                int b = PenguinLayout.BlockPos(s, j);
                if (b != PenguinLayout.Gone) Place(_blockImages[j].rectTransform, CellPos(b), _blockScale);
            }
            SortActors();
        }

        Vector2 CellPos(int cell) => new Vector2(_puzzle.Layout.X(cell), _puzzle.Layout.Y(cell));

        void Place(RectTransform rt, Vector2 cellPos, float size = 1f)
        {
            float cell = _board.Cell;
            rt.sizeDelta = new Vector2(cell * size, cell * size);
            rt.anchoredPosition = new Vector2((cellPos.x + 0.5f) * cell, -(cellPos.y + 0.5f) * cell);
        }

        /// <summary>Igloos and penguins: rows lower on the screen drawn over the rows above (at equal rows the penguin over the igloo).</summary>
        void SortActors()
        {
            var l = _puzzle.Layout;
            float cell = Mathf.Max(1f, _board.Cell);
            _order.Clear();
            for (int i = 0; i < l.PenguinCount; i++)
            {
                _order.Add((l.Y(l.Igloo(i)), 0, _igloos[i].Root));
                var root = _penguins[i].Root;
                if (root.gameObject.activeSelf) _order.Add((-root.anchoredPosition.y / cell - 0.5f, 1 + i, root));
            }
            ApplyOrder();
        }

        void ApplyOrder()
        {
            _order.Sort((a, b) => a.row != b.row ? a.row.CompareTo(b.row) : a.kind.CompareTo(b.kind));
            for (int k = 0; k < _order.Count; k++) _order[k].rt.SetSiblingIndex(k);
        }

        public override void PreparePreview(BoardPreview state)
        {
            _puzzle.Restart();
            var dirs = _puzzle.Space.Solution(_puzzle.State);
            int moves = state == BoardPreview.Partial ? dirs.Count / 2 : state == BoardPreview.Solved ? dirs.Count : 0;
            for (int i = 0; i < moves; i++) _puzzle.Swipe(dirs[i]);
            _fresh = moves == 0;
            SnapVisuals();
            RaiseChanged();
        }

        // ------------------------------------------------------------------ editor previews

        /// <summary>Previews: the arrows of the light bulb on the next right move, frozen.</summary>
        public void PreviewHintArrow()
        {
            if (_puzzle.TryFindHint(out int dir)) ShowArrows(dir);
        }

        /// <summary>Previews: the first move of the solution, frozen at <paramref name="fraction"/> of its slide.</summary>
        public void PreviewSlide(float fraction)
        {
            var dirs = _puzzle.Space.Solution(_puzzle.State);
            if (dirs.Count == 0 || !StartSwipe(dirs[0])) return;
            Seek(Mathf.Clamp01(fraction) * _play.Trace.Duration / _play.CellsPerSecond);
        }

        /// <summary>
        /// Previews: along the solution, the first swipe that makes a penguin fall, frozen while it is scared (<paramref name="scared"/>)
        /// or in the middle of the fall (with its message).
        /// </summary>
        public void PreviewFall(bool scared = false)
        {
            var dirs = _puzzle.Space.Solution(_puzzle.State);
            for (int k = 0; k <= dirs.Count; k++)
            {
                int mask = _puzzle.Space.FallMask(_puzzle.State);
                if (mask != 0)
                {
                    int dir = 0;
                    while ((mask & (1 << dir)) == 0) dir++;
                    SnapVisuals();
                    if (!StartSwipe(dir)) return;
                    int end = 0;
                    foreach (var t in _play.Trace.Tracks)
                        if (t.End == PenguinTrackEnd.Fall || t.End == PenguinTrackEnd.FallOff) end = t.T1;
                    float overshoot = _play.Trace.Tracks.Exists(t => t.End == PenguinTrackEnd.FallOff) ? _play.FallOffOvershoot : 0f;
                    float after = scared ? _cfg.turnDuration + _cfg.scaredHold * 0.5f : FallDelay + _cfg.fallDuration * 0.45f;
                    Seek((end + overshoot) / _play.CellsPerSecond + after);
                    RaiseMessage(PenguinKeys.Splash);
                    return;
                }
                if (k < dirs.Count) _puzzle.Swipe(dirs[k]);
            }
            SnapVisuals();
        }

        /// <summary>
        /// Previews (ErNa-012): along the solution, the first swipe where a penguin stops against something, frozen just after the stop
        /// (still seen from above: no expression). False if the solution has none.
        /// </summary>
        public bool PreviewBump()
        {
            var dirs = _puzzle.Space.Solution(_puzzle.State);
            foreach (int dir in dirs)
            {
                SnapVisuals();
                if (!StartSwipe(dir)) return false;
                foreach (var t in _play.Trace.Tracks)
                {
                    if (t.Actor >= FirstBlock || t.End != PenguinTrackEnd.Bump) continue;
                    Seek(t.T1 / _play.CellsPerSecond + _cfg.turnDuration + 0.05f);
                    return true;
                }
                Seek(_play.Seconds + 0.05f);
            }
            SnapVisuals();
            return false;
        }

        /// <summary>Previews: the solution but its last move: the igloo is open (all the fish taken), the penguins seen from above.</summary>
        public void PreviewBeforeLast()
        {
            _puzzle.Restart();
            var dirs = _puzzle.Space.Solution(_puzzle.State);
            for (int i = 0; i + 1 < dirs.Count; i++) _puzzle.Swipe(dirs[i]);
            _fresh = false;
            SnapVisuals();
            RaiseChanged();
        }

        /// <summary>Previews: the shortest way into a state with no solution left (the game screen shows "You are stuck").</summary>
        public void PreviewStuck()
        {
            _puzzle.Restart();
            foreach (int dir in _puzzle.Space.PathToStuck()) _puzzle.Swipe(dir);
            _fresh = _puzzle.MovesMade == 0;
            SnapVisuals();
            RaiseChanged();
        }

        /// <summary>Previews: the clock of the move set to <paramref name="seconds"/>, everything drawn there (nothing ticks in edit mode).</summary>
        void Seek(float seconds)
        {
            float step = 1f / 120f;
            bool playing = true;
            for (float t = 0f; t < seconds; t += step)
            {
                if (playing && _play.Tick(step))
                {
                    Animate(float.MaxValue);
                    FinishMove();
                    playing = false;
                }
                else if (playing) Animate(_play.TraceTime);
                TickTimers(step);
            }
        }

        // ------------------------------------------------------------------ input

        void OnSwiped(int dir)
        {
            if (IsBusy || _puzzle.Layout == null || _puzzle.IsSolved || _puzzle.Fell) return;
            if (StartSwipe(dir)) RaiseChanged();
        }

        /// <summary>Plays a swipe of the player (false = no move: empty swipe, or the Challenge maximum).</summary>
        bool StartSwipe(int dir)
        {
            if (!_play.Swipe(dir)) return false;
            GameAudio.Play(AudioEvents.PenguinSlide);
            BeginMove();
            return true;
        }

        /// <summary>A fall: the penguin turns and stays scared for a moment, then the fall effect.</summary>
        float FallDelay => _cfg.turnDuration + _cfg.scaredHold;

        /// <summary>
        /// The swipe was accepted: the start cells, the view of each block actor, the extra time of the end effects; the penguins that
        /// slide are seen from above at once, the others drop a normal face (an expression never slows the game down).
        /// </summary>
        void BeginMove()
        {
            var l = _puzzle.Layout;
            ulong before = _play.Move.Before;
            for (int a = 0; a < Actors; a++)
            {
                _actorBlock[a] = null;
                int p = a < FirstBlock
                    ? (a < l.PenguinCount ? PenguinLayout.PenguinPos(before, a) : PenguinLayout.Gone)
                    : (a - FirstBlock < l.BlockCount ? PenguinLayout.BlockPos(before, a - FirstBlock) : PenguinLayout.Gone);
                if (p != PenguinLayout.Gone) _start[a] = CellPos(p);
                else if (a < l.PenguinCount) _start[a] = CellPos(l.Igloo(a));   // already in its igloo: stays there
                if (a >= FirstBlock && a - FirstBlock < _blockImages.Count) _actorBlock[a] = _blockImages[a - FirstBlock];
            }
            _fresh = false;
            int dir = _play.Trace.Dir;
            for (int i = 0; i < l.PenguinCount; i++)
            {
                if (PenguinLayout.PenguinPos(before, i) == PenguinLayout.Gone) continue;
                bool slides = _play.Trace.Tracks.Exists(t => t.Actor == i);
                if (slides) _penguins[i].Mood.BeginSlide(dir);
                else _penguins[i].Mood.Interrupt();
                ApplyPose(i);
            }
            _trackDone.Clear();
            foreach (var _ in _play.Trace.Tracks) _trackDone.Add(false);
            _eventDone.Clear();
            foreach (var _ in _play.Trace.Events) _eventDone.Add(false);
            float tail = _cfg.bumpDuration;
            foreach (var t in _play.Trace.Tracks)
                switch (t.End)
                {
                    case PenguinTrackEnd.Enter: tail = Mathf.Max(tail, _cfg.turnDuration); break;
                    case PenguinTrackEnd.Fall:
                    case PenguinTrackEnd.FallOff: tail = Mathf.Max(tail, FallDelay + _cfg.fallDuration); break;
                    case PenguinTrackEnd.Sink: tail = Mathf.Max(tail, _cfg.sinkDuration); break;
                }
            if (_play.Trace.Events.Count > 0) tail = Mathf.Max(tail, Mathf.Max(_cfg.fishPop, _cfg.iglooOpen));
            _play.Tail = tail + 0.03f;
        }

        // ------------------------------------------------------------------ animation (real time)

        void Update()
        {
            if (_puzzle.Layout == null) return;
            float dt = Time.unscaledDeltaTime;
            if (_play.Busy)
            {
                if (_play.Tick(dt))
                {
                    Animate(float.MaxValue);
                    FinishMove();
                }
                else Animate(_play.TraceTime);
            }
            TickTimers(dt);
            if (_hintTime >= 0f)
            {
                _hintTime += dt;
                if (_hintTime >= _cfg.hintArrowTime)
                {
                    _hintTime = -1f;
                    foreach (var a in _arrows) a.gameObject.SetActive(false);
                    var r = _play.Hint();
                    if (r == PenguinHint.Moved) BeginMove();
                    else SnapVisuals();
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
            if (_failTime >= 0f)
            {
                _failTime += dt;
                if (_failTime >= _cfg.failPause)
                {
                    _failTime = -1f;
                    if (RestartOnFailure)
                    {
                        // Story: the level starts again with 0 moves, as after a failed opening of the water
                        _puzzle.Restart();
                        _fresh = true;
                        SnapVisuals();
                        RaiseChanged();
                    }
                    else
                    {
                        RaiseChanged();
                        RaiseFailed();
                    }
                }
            }
        }

        /// <summary>The actors at trace time <paramref name="t"/> (cells slid), and the effects of the slide ends and events reached.</summary>
        void Animate(float t)
        {
            var trace = _play.Trace;
            var pos = new Vector2[Actors];
            for (int a = 0; a < Actors; a++) pos[a] = _start[a];
            for (int k = 0; k < trace.Tracks.Count; k++)
            {
                var tr = trace.Tracks[k];
                if (t < tr.T0) continue;
                Vector2 from = CellPos(tr.From), to = CellPos(tr.To);
                float len = Mathf.Max(0.0001f, tr.T1 - tr.T0);
                pos[tr.Actor] = t >= tr.T1 ? to : Vector2.Lerp(from, to, (t - tr.T0) / len);
                if (tr.End == PenguinTrackEnd.FallOff && t > tr.T1)
                {
                    var dir = new Vector2(PenguinLayout.Dx(trace.Dir), PenguinLayout.Dy(trace.Dir));
                    pos[tr.Actor] = to + dir * Mathf.Min(t - tr.T1, _play.FallOffOvershoot);
                }
                if (!_trackDone[k] && t >= tr.T1 + (tr.End == PenguinTrackEnd.FallOff ? _play.FallOffOvershoot : 0f))
                {
                    _trackDone[k] = true;
                    EndOfSlide(tr, pos[tr.Actor]);
                }
            }
            for (int k = 0; k < trace.Events.Count; k++)
            {
                var e = trace.Events[k];
                if (_eventDone[k] || t < e.Time) continue;
                _eventDone[k] = true;
                if (e.Kind == PenguinTrace.EventKind.Fish)
                {
                    if (e.Index < _fishPop.Length) _fishPop[e.Index] = 0f;
                    GameAudio.Play(AudioEvents.PenguinFish);
                    AddFx(FxKind.Sparkle, CellPos(_puzzle.Layout.FishCell(e.Index)), _cfg.fishPop);
                }
                else
                {
                    _iglooOpen = 0f;
                    GameAudio.Play(AudioEvents.PenguinIglooOpen);
                }
            }
            if (_board.Cell <= 0) return;
            for (int i = 0; i < _puzzle.Layout.PenguinCount; i++)
                if (_penguins[i].Root.gameObject.activeSelf) Place(_penguins[i].Root, pos[i]);
            for (int a = FirstBlock; a < Actors; a++)
                if (_actorBlock[a] != null && _actorBlock[a].gameObject.activeSelf) Place(_actorBlock[a].rectTransform, pos[a], _blockScale);
            SortActors();
        }

        void EndOfSlide(PenguinTrace.Track tr, Vector2 at)
        {
            int a = tr.Actor;
            if (a < FirstBlock)
            {
                _penguins[a].Mood.EndSlide(tr.End);
                ApplyPose(a);
            }
            switch (tr.End)
            {
                case PenguinTrackEnd.Bump:
                case PenguinTrackEnd.Push:
                    GameAudio.Play(tr.End == PenguinTrackEnd.Push ? AudioEvents.PenguinPush : AudioEvents.PenguinBump);
                    _bump[a] = 0f;
                    break;
                case PenguinTrackEnd.Enter:
                    GameAudio.Play(AudioEvents.PenguinEnter);
                    break;
                case PenguinTrackEnd.Fall:
                case PenguinTrackEnd.FallOff:
                case PenguinTrackEnd.Sink:
                    // a penguin is scared first (the turn, then a moment), then it falls; a block sinks at once
                    _vanish[a] = 0f;
                    _vanishStarted[a] = false;
                    _vanishKind[a] = tr.End;
                    _vanishAt[a] = at;
                    if (a < FirstBlock) _fallen[a] = true;
                    break;
                case PenguinTrackEnd.Snow:
                    GameAudio.Play(AudioEvents.PenguinSnow);
                    break;
            }
        }

        /// <summary>Squashes, poses, falling and sinking actors, fish, the igloo doors and the effects (real time).</summary>
        void TickTimers(float dt)
        {
            var l = _puzzle.Layout;
            if (l == null) return;
            for (int a = 0; a < Actors; a++)
            {
                RectTransform rt = a < FirstBlock ? _penguins[a].Root : _actorBlock[a] != null ? _actorBlock[a].rectTransform : null;
                if (rt == null) continue;
                var scale = Vector3.one;
                if (_bump[a] >= 0f)
                {
                    _bump[a] += dt;
                    float u = Mathf.Clamp01(_bump[a] / Mathf.Max(0.01f, _cfg.bumpDuration));
                    float k = Mathf.Sin(u * Mathf.PI) * 0.14f;
                    scale = new Vector3(1f + k, 1f - k, 1f);
                    if (u >= 1f) _bump[a] = -1f;
                }
                if (_vanish[a] >= 0f)
                {
                    _vanish[a] += dt;
                    bool sink = _vanishKind[a] == PenguinTrackEnd.Sink;
                    float t = _vanish[a] - (a < FirstBlock ? FallDelay : 0f);
                    if (t >= 0f)
                    {
                        if (!_vanishStarted[a])
                        {
                            _vanishStarted[a] = true;
                            GameAudio.Play(sink ? AudioEvents.PenguinSink : AudioEvents.PenguinSplash);
                            if (!sink) GameAudio.Vibrate();
                            AddFx(FxKind.Splash, _vanishAt[a], sink ? _cfg.sinkDuration : _cfg.fallDuration);
                        }
                        float dur = sink ? _cfg.sinkDuration : _cfg.fallDuration;
                        float u = Mathf.Clamp01(t / Mathf.Max(0.01f, dur));
                        float s = Mathf.Lerp(1f, sink ? 0.55f : 0.25f, u);
                        scale = new Vector3(s, s, 1f);
                        if (a < FirstBlock) _penguins[a].Group.alpha = 1f - u;
                        else _actorBlock[a].color = new Color(1f, 1f, 1f, 1f - u);
                        if (u >= 1f)
                        {
                            _vanish[a] = -1f;
                            rt.gameObject.SetActive(false);
                            if (a >= FirstBlock)
                            {
                                // the hole is filled: the water tile becomes the sunken block
                                foreach (var tr in _play.Trace.Tracks)
                                    if (tr.Actor == a && tr.End == PenguinTrackEnd.Sink && _waterImages.TryGetValue(tr.To, out var water))
                                        water.sprite = _art.Get(PenguinLook.TileSunkenBlock);
                            }
                        }
                    }
                }
                rt.localScale = scale;
            }
            for (int i = 0; i < l.PenguinCount; i++)
            {
                _penguins[i].Mood.Tick(dt);
                ApplyPose(i);
            }
            for (int f = 0; f < _fishPop.Length; f++)
            {
                if (_fishPop[f] < 0f) continue;
                _fishPop[f] += dt;
                // the fish jumps up and away, and fades in the second half
                float u = Mathf.Clamp01(_fishPop[f] / Mathf.Max(0.01f, _cfg.fishPop));
                var rt = _fishImages[f].rectTransform;
                rt.localScale = Vector3.one * (_fishScale * Mathf.Lerp(1f, 1.25f, u));
                rt.anchoredPosition = new Vector2(0f, Mathf.Sin(u * Mathf.PI * 0.8f) * _cfg.fishJump * _board.Cell);
                _fishImages[f].color = new Color(1f, 1f, 1f, u < 0.5f ? 1f : 1f - (u - 0.5f) * 2f);
                if (u >= 1f)
                {
                    _fishPop[f] = -1f;
                    _fishImages[f].gameObject.SetActive(false);
                }
            }
            if (_iglooOpen >= 0f)
            {
                _iglooOpen += dt;
                // the door opens half way through a little squash and stretch
                float u = Mathf.Clamp01(_iglooOpen / Mathf.Max(0.01f, _cfg.iglooOpen));
                float k = Mathf.Sin(u * Mathf.PI) * 0.1f;
                for (int i = 0; i < l.PenguinCount; i++)
                {
                    var ig = _igloos[i];
                    if (u >= 0.5f) ig.Image.sprite = _art.Get(PenguinLook.IglooName(i, true));
                    ig.Image.rectTransform.localScale = new Vector3(1f + k, 1f - k * 0.6f, 1f);
                }
                if (u >= 1f) _iglooOpen = -1f;
            }
            for (int k = _fx.Count - 1; k >= 0; k--)
            {
                var fx = _fx[k];
                fx.Time += dt;
                float u = Mathf.Clamp01(fx.Time / Mathf.Max(0.01f, fx.Duration));
                float s = fx.Kind == FxKind.Splash ? Mathf.Lerp(0.4f, 1.25f, u) : Mathf.Lerp(0.5f, 1.1f, Mathf.Sin(u * Mathf.PI));
                fx.Image.rectTransform.localScale = new Vector3(s, s, 1f);
                fx.Image.color = new Color(1f, 1f, 1f, 1f - u * u);
                if (u >= 1f)
                {
                    _fxPool.Release(fx.Image);
                    _fx.RemoveAt(k);
                }
                else _fx[k] = fx;
            }
        }

        void AddFx(FxKind kind, Vector2 cellPos, float duration)
        {
            var img = _fxPool.Get();
            img.sprite = kind == FxKind.Splash ? _sprites.Splash : _sprites.Sparkle;
            img.name = kind.ToString();
            img.color = Color.white;
            img.rectTransform.localScale = Vector3.one * 0.4f;
            img.transform.SetAsLastSibling();
            if (_board.Cell > 0) Place(img.rectTransform, cellPos);
            _fx.Add(new Fx { Image = img, Kind = kind, Time = 0f, Duration = Mathf.Max(0.05f, duration) });
        }

        /// <summary>The move is over: what the rules say is shown (running expressions go on); a fall starts the failure, the last penguin in the win.</summary>
        void FinishMove()
        {
            foreach (var p in _penguins) p.Root.localScale = Vector3.one;
            ShowState(resetPoses: false);
            if (_board.Cell > 0) PlaceActors();
            if (_puzzle.Fell)
            {
                RaiseMessage(PenguinKeys.Splash);
                _failTime = 0f;
            }
            else if (_puzzle.IsSolved) _winTime = 0f;
            RaiseChanged();
        }

        int _hintDir;

        void ShowArrows(int dir)
        {
            _hintDir = dir;
            var l = _puzzle.Layout;
            for (int i = 0; i < _arrows.Length; i++)
            {
                bool shown = i < l.PenguinCount && PenguinLayout.PenguinPos(_puzzle.State, i) != PenguinLayout.Gone;
                _arrows[i].gameObject.SetActive(shown);
                if (!shown) continue;
                _arrows[i].transform.SetAsLastSibling();
                PlaceArrow(i, dir);
            }
        }

        void PlaceArrow(int penguin, int dir)
        {
            int p = PenguinLayout.PenguinPos(_puzzle.State, penguin);
            if (p == PenguinLayout.Gone || _board.Cell <= 0) return;
            var rt = _arrows[penguin].rectTransform;
            var at = CellPos(p) + new Vector2(PenguinLayout.Dx(dir), PenguinLayout.Dy(dir)) * 0.42f;
            Place(rt, at);
            rt.sizeDelta *= 0.7f;
            rt.localRotation = Quaternion.Euler(0, 0, -90f * dir);
        }

        void LateUpdate()
        {
            if (((RectTransform)transform).rect.size != _fittedSize) Refit();
        }
    }
}
