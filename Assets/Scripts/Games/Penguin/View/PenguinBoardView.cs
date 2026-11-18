using ResolverMaster.Core.Audio;
using System;
using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Core.UI.Themes;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Games.Penguin.View
{
    /// <summary>
    /// The Pino board on the Core game screen (PG-001), inside the Core <see cref="BoardFrame"/> (cream window, theme ground, ice tiles
    /// of the theme), with the border (snow wall or water) in the room around the cells. Swipes anywhere on the board (Core
    /// <see cref="SwipeArea"/>, a tap is not a swipe) move the penguins; swipes are ignored while something moves. The slides come from
    /// the times computed by the rules (<see cref="PenguinTrace"/>, <see cref="PenguinPlay"/>) and are drawn on the real elapsed time
    /// (same speed at 30 and 60 fps): a little squash and a dizzy face after a stop, the fish popping, the igloo door opening, the penguin
    /// going in (happy), falling into the water (scared, a splash: then the level starts again, or the Challenge run is lost), the block
    /// sliding and sinking into a hole that turns to ice. The light bulb shows an arrow on the penguins, then makes the move (or, when
    /// stuck, undoes back to the last state with a solution).
    /// </summary>
    public sealed class PenguinBoardView : PuzzleBoard
    {
        const int Actors = 5, FirstBlock = 2;

        sealed class PenguinView
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Body, Face, Scarf, Stripes;
        }

        sealed class IglooView
        {
            public RectTransform Root;
            public Image Body, Closed, Flag, Stripes;
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
        UiFactory _ui;
        BoardFrame _board;
        RectTransform _tilesRoot, _terrainRoot, _borderRoot, _itemsRoot, _blocksRoot, _penguinsRoot, _fxRoot;
        ComponentPool<Image> _tiles, _terrain, _edges, _fishPool, _blockPool, _fxPool;
        readonly PenguinView[] _penguins = new PenguinView[PenguinLayout.MaxPenguins];
        readonly IglooView[] _igloos = new IglooView[PenguinLayout.MaxPenguins];
        readonly Image[] _arrows = new Image[PenguinLayout.MaxPenguins];
        readonly List<Image> _fishImages = new List<Image>();
        readonly List<Image> _blockImages = new List<Image>();
        readonly Dictionary<int, Image> _waterImages = new Dictionary<int, Image>();
        readonly List<Image> _edgeImages = new List<Image>();
        readonly List<int> _edgeOf = new List<int>();
        readonly List<Image> _corners = new List<Image>();
        readonly List<Fx> _fx = new List<Fx>();
        readonly PenguinPuzzle _puzzle = new PenguinPuzzle();
        PenguinPlay _play;
        SwipeArea _swipe;
        Vector2 _fittedSize;

        // the move being played: start cells, the block view of each block actor, which track ends and events already ran
        readonly Vector2[] _start = new Vector2[Actors];
        readonly Image[] _actorBlock = new Image[Actors];
        readonly List<bool> _trackDone = new List<bool>();
        readonly List<bool> _eventDone = new List<bool>();
        readonly float[] _bump = new float[Actors];
        readonly float[] _vanish = new float[Actors];
        readonly PenguinTrackEnd[] _vanishKind = new PenguinTrackEnd[Actors];
        readonly float[] _dizzy = new float[PenguinLayout.MaxPenguins];
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

        /// <summary>The drawn penguins (previews measure them).</summary>
        public IEnumerable<RectTransform> PenguinRects
        {
            get
            {
                foreach (var p in _penguins)
                    if (p != null && p.Root.gameObject.activeInHierarchy) yield return p.Root;
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
            _puzzle.MaxStates = Mathf.Max(1000, _cfg.maxStates);
            _play = new PenguinPlay(_puzzle) { CellsPerSecond = Mathf.Max(1f, _cfg.slideSpeed) };
            _board = BoardFrame.Create(_ui, transform, _cfg.boardInset, _cfg.compactCellBelow, _cfg.compactFrameScale, _cfg.compactOverflow);
            _tilesRoot = Layer("Tiles");
            _terrainRoot = Layer("Terrain");
            _borderRoot = Layer("Border");
            _itemsRoot = Layer("Items");
            _blocksRoot = Layer("Blocks");
            _penguinsRoot = Layer("Penguins");
            _fxRoot = Layer("Effects");
            _tiles = new ComponentPool<Image>(() =>
            {
                var img = _ui.Image("Cell", _tilesRoot, _sprites.Tile, Color.white);
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
            for (int k = 0; k < 4; k++)
            {
                var corner = Piece("Corner" + k, _borderRoot, cellAnchored: false);
                corner.sprite = _sprites.Corner;
                _corners.Add(corner);
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

        Image Child(string name, Transform parent, Sprite sprite)
        {
            var img = _ui.Image(name, parent, sprite, Color.white);
            img.raycastTarget = false;
            img.gameObject.AddComponent<Decoration>();
            UiFactory.Stretch(img.rectTransform);
            return img;
        }

        PenguinView BuildPenguin(int i)
        {
            var root = _ui.Rect("Penguin" + (i + 1), _penguinsRoot);
            Anchor(root);
            var v = new PenguinView { Root = root, Group = root.gameObject.AddComponent<CanvasGroup>() };
            v.Group.blocksRaycasts = false;
            v.Body = Child("Body", root, _sprites.Body);
            v.Scarf = Child("Scarf", root, _sprites.Scarf);
            v.Scarf.color = _cfg.ScarfColor(i);
            v.Stripes = Child("ScarfStripes", root, _sprites.ScarfStripes);
            v.Stripes.gameObject.SetActive(i == 1);   // never the color alone: the second penguin has a striped scarf
            v.Face = Child("Face", root, _sprites.FaceSprite(PenguinSprites.Face.Normal));
            root.gameObject.SetActive(false);
            return v;
        }

        IglooView BuildIgloo(int i)
        {
            var root = _ui.Rect("Igloo" + (i + 1), _itemsRoot);
            var v = new IglooView { Root = root };
            v.Body = Child("Body", root, _sprites.Igloo);
            v.Flag = Child("Flag", root, _sprites.Flag);
            v.Flag.color = _cfg.ScarfColor(i);
            v.Stripes = Child("FlagStripes", root, _sprites.FlagStripes);
            v.Stripes.gameObject.SetActive(i == 1);   // the flag matches the scarf of its penguin, pattern included
            v.Closed = Child("Closed", root, _sprites.IglooClosed);
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
            BuildPieces(level.seed);
            SnapVisuals();
            Refit();
            RaiseChanged();
        }

        void BuildPieces(int seed)
        {
            var l = _puzzle.Layout;
            int w = l.Width, h = l.Height;
            var theme = Theme;
            _board.SetTheme(theme);
            _tiles.ReleaseAll();
            for (int c = 0; c < l.CellCount; c++)
            {
                var tile = _tiles.Get();
                SetCellAnchors(tile.rectTransform, c % w, c / w, w, h);
                tile.sprite = theme != null ? ThemeArt.Slab(theme, ArtVariants.Pick(seed, 3, c, ThemeArt.SlabVariants)) : _sprites.Tile;
                tile.name = $"Cell_{c % w}_{c / w}";
            }
            _terrain.ReleaseAll();
            _waterImages.Clear();
            for (int c = 0; c < l.CellCount; c++)
            {
                var t = l.Terrain(c);
                if (t == PenguinTerrain.Ice) continue;
                var img = _terrain.Get();
                SetCellAnchors(img.rectTransform, c % w, c / w, w, h);
                img.sprite = t == PenguinTerrain.Rock ? _sprites.Rock : t == PenguinTerrain.Snow ? _sprites.Snow : _sprites.Water;
                img.name = t + $"_{c % w}_{c / w}";
                img.rectTransform.localScale = Vector3.one;
                img.color = Color.white;
                if (t == PenguinTerrain.Water) _waterImages[c] = img;
            }
            _edges.ReleaseAll();
            _edgeImages.Clear();
            _edgeOf.Clear();
            for (int e = 0; e < l.EdgeCount; e++)
            {
                var img = _edges.Get();
                bool water = l.EdgeWater(e);
                img.sprite = water ? _sprites.WaterEdge : _sprites.WallEdge;
                img.name = (water ? "EdgeWater_" : "EdgeWall_") + e;
                _edgeImages.Add(img);
                _edgeOf.Add(e);
            }
            _fishPool.ReleaseAll();
            _fishImages.Clear();
            for (int f = 0; f < l.FishCount; f++)
            {
                var img = _fishPool.Get();
                int c = l.FishCell(f);
                SetCellAnchors(img.rectTransform, c % w, c / w, w, h);
                img.sprite = _sprites.Fish;
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
                img.sprite = _sprites.Block;
                img.name = "Block_" + j;
                _blockImages.Add(img);
            }
        }

        public override void Undo()
        {
            if (IsBusy || !_puzzle.Undo(out _)) return;
            SnapVisuals();
            RaiseChanged();
        }

        public override void Restart()
        {
            if (IsBusy) return;
            _puzzle.Restart();
            SnapVisuals();
            RaiseChanged();
        }

        /// <summary>
        /// The light bulb (after the ad): stuck = back to the last state with a solution at once; otherwise an arrow on the penguins,
        /// then the next right swipe (a free move).
        /// </summary>
        public override void ApplyHint()
        {
            if (!CanHint) return;
            if (_puzzle.IsStuck)
            {
                _play.Hint();
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
            float cell = _board.Cell, depth = Mathf.Max(1f, _board.Margin - _board.Inset);
            for (int k = 0; k < _edgeImages.Count; k++)
            {
                l.EdgeCell(_edgeOf[k], out int c, out int dir);
                var rt = _edgeImages[k].rectTransform;
                float x = (l.X(c) + 0.5f) * cell, y = -(l.Y(c) + 0.5f) * cell, half = cell / 2f + depth / 2f;
                rt.sizeDelta = new Vector2(cell, depth);
                rt.anchoredPosition = new Vector2(x + PenguinLayout.Dx(dir) * half, y - PenguinLayout.Dy(dir) * half);
                rt.localRotation = Quaternion.Euler(0, 0, -90f * dir);
            }
            float wpx = l.Width * cell, hpx = l.Height * cell;
            var corners = new[] { new Vector2(-depth / 2f, depth / 2f), new Vector2(wpx + depth / 2f, depth / 2f), new Vector2(wpx + depth / 2f, -hpx - depth / 2f), new Vector2(-depth / 2f, -hpx - depth / 2f) };
            for (int k = 0; k < 4; k++)
            {
                var rt = _corners[k].rectTransform;
                rt.sizeDelta = new Vector2(depth, depth);
                rt.anchoredPosition = corners[k];
                // the quarter disc of snow is centred on the corner of the grid (the sprite draws it around its bottom-left corner)
                rt.localRotation = Quaternion.Euler(0, 0, k == 0 ? 90f : k == 1 ? 0f : k == 2 ? -90f : 180f);
            }
            if (_play.Busy) Animate(_play.TraceTime);
            else PlaceActors();
            for (int i = 0; i < _arrows.Length; i++)
                if (_arrows[i].gameObject.activeSelf) PlaceArrow(i, _hintDir);
        }

        /// <summary>Every animation jumps to its end (the board shows the state of the rules).</summary>
        public override void SnapVisuals()
        {
            StopAll();
            if (_puzzle.Layout == null) return;
            ShowState();
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
            for (int i = 0; i < _dizzy.Length; i++) _dizzy[i] = -1f;
            for (int f = 0; f < _fishPop.Length; f++) _fishPop[f] = -1f;
            foreach (var fx in _fx) _fxPool.Release(fx.Image);
            _fx.Clear();
            foreach (var a in _arrows) a.gameObject.SetActive(false);
        }

        /// <summary>What is visible in the current state of the rules: penguins out, fish not taken, igloo doors, blocks, water holes.</summary>
        void ShowState()
        {
            var l = _puzzle.Layout;
            ulong s = _puzzle.State;
            for (int i = 0; i < PenguinLayout.MaxPenguins; i++)
            {
                var v = _penguins[i];
                bool shown = i < l.PenguinCount && PenguinLayout.PenguinPos(s, i) != PenguinLayout.Gone;
                v.Root.gameObject.SetActive(shown);
                v.Group.alpha = 1f;
                v.Root.localScale = Vector3.one;
                v.Face.sprite = _sprites.FaceSprite(PenguinSprites.Face.Normal);
                if (i < l.PenguinCount)
                {
                    var ig = _igloos[i];
                    ig.Closed.gameObject.SetActive(!l.IglooOpen(s));
                    ig.Closed.color = Color.white;
                    ig.Closed.rectTransform.localScale = Vector3.one;
                    ig.Body.rectTransform.localScale = Vector3.one;
                }
            }
            for (int f = 0; f < _fishImages.Count; f++)
            {
                _fishImages[f].gameObject.SetActive(!l.FishTaken(s, f));
                _fishImages[f].color = Color.white;
                _fishImages[f].rectTransform.localScale = Vector3.one;
            }
            foreach (var kv in _waterImages) kv.Value.gameObject.SetActive(l.IsWater(s, kv.Key));
            for (int j = 0; j < _blockImages.Count; j++)
            {
                var img = _blockImages[j];
                bool shown = PenguinLayout.BlockPos(s, j) != PenguinLayout.Gone;
                img.gameObject.SetActive(shown);
                img.color = Color.white;
                img.rectTransform.localScale = Vector3.one;
            }
        }

        /// <summary>Penguins and blocks on the cells of the current state.</summary>
        void PlaceActors()
        {
            var l = _puzzle.Layout;
            ulong s = _puzzle.State;
            for (int i = 0; i < l.PenguinCount; i++)
            {
                int p = PenguinLayout.PenguinPos(s, i);
                if (p != PenguinLayout.Gone) Place(_penguins[i].Root, CellPos(p));
            }
            for (int j = 0; j < _blockImages.Count; j++)
            {
                int b = PenguinLayout.BlockPos(s, j);
                if (b != PenguinLayout.Gone) Place(_blockImages[j].rectTransform, CellPos(b));
            }
        }

        Vector2 CellPos(int cell) => new Vector2(_puzzle.Layout.X(cell), _puzzle.Layout.Y(cell));

        void Place(RectTransform rt, Vector2 cellPos)
        {
            float cell = _board.Cell;
            rt.sizeDelta = new Vector2(cell, cell);
            rt.anchoredPosition = new Vector2((cellPos.x + 0.5f) * cell, -(cellPos.y + 0.5f) * cell);
        }

        public override void PreparePreview(BoardPreview state)
        {
            _puzzle.Restart();
            var dirs = _puzzle.Space.Solution(_puzzle.State);
            int moves = state == BoardPreview.Partial ? dirs.Count / 2 : state == BoardPreview.Solved ? dirs.Count : 0;
            for (int i = 0; i < moves; i++) _puzzle.Swipe(dirs[i]);
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

        /// <summary>Previews: along the solution, the first swipe that makes a penguin fall, frozen in the middle of the fall (with its message).</summary>
        public void PreviewFall()
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
                    Seek((end + overshoot) / _play.CellsPerSecond + _cfg.fallDuration * 0.45f);
                    RaiseMessage(PenguinKeys.Splash);
                    return;
                }
                if (k < dirs.Count) _puzzle.Swipe(dirs[k]);
            }
            SnapVisuals();
        }

        /// <summary>Previews: the shortest way into a state with no solution left (the game screen shows "You are stuck").</summary>
        public void PreviewStuck()
        {
            _puzzle.Restart();
            foreach (int dir in _puzzle.Space.PathToStuck()) _puzzle.Swipe(dir);
            SnapVisuals();
            RaiseChanged();
        }

        /// <summary>Previews: the clock of the move set to <paramref name="seconds"/>, everything drawn there (nothing ticks in edit mode).</summary>
        void Seek(float seconds)
        {
            float step = 1f / 120f;
            for (float t = 0f; t < seconds; t += step)
            {
                if (_play.Tick(step))
                {
                    Animate(float.MaxValue);
                    FinishMove();
                    return;
                }
                Animate(_play.TraceTime);
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

        /// <summary>The swipe was accepted: the start cells, the view of each block actor, the extra time of the end effects.</summary>
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
                if (a >= FirstBlock && a - FirstBlock < _blockImages.Count) _actorBlock[a] = _blockImages[a - FirstBlock];
            }
            _trackDone.Clear();
            foreach (var _ in _play.Trace.Tracks) _trackDone.Add(false);
            _eventDone.Clear();
            foreach (var _ in _play.Trace.Events) _eventDone.Add(false);
            float tail = _cfg.bumpDuration;
            foreach (var t in _play.Trace.Tracks)
                switch (t.End)
                {
                    case PenguinTrackEnd.Enter: tail = Mathf.Max(tail, _cfg.enterDuration); break;
                    case PenguinTrackEnd.Fall:
                    case PenguinTrackEnd.FallOff: tail = Mathf.Max(tail, _cfg.fallDuration); break;
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
                if (_actorBlock[a] != null && _actorBlock[a].gameObject.activeSelf) Place(_actorBlock[a].rectTransform, pos[a]);
        }

        void EndOfSlide(PenguinTrace.Track tr, Vector2 at)
        {
            int a = tr.Actor;
            switch (tr.End)
            {
                case PenguinTrackEnd.Bump:
                case PenguinTrackEnd.Push:
                    GameAudio.Play(tr.End == PenguinTrackEnd.Push ? AudioEvents.PenguinPush : AudioEvents.PenguinBump);
                    _bump[a] = 0f;
                    if (a < FirstBlock) _dizzy[a] = 0f;
                    break;
                case PenguinTrackEnd.Enter:
                case PenguinTrackEnd.Fall:
                case PenguinTrackEnd.FallOff:
                case PenguinTrackEnd.Sink:
                    _vanish[a] = 0f;
                    _vanishKind[a] = tr.End;
                    GameAudio.Play(tr.End == PenguinTrackEnd.Enter ? AudioEvents.PenguinEnter : tr.End == PenguinTrackEnd.Sink ? AudioEvents.PenguinSink : AudioEvents.PenguinSplash);
                    if (tr.End == PenguinTrackEnd.Fall || tr.End == PenguinTrackEnd.FallOff) GameAudio.Vibrate();
                    if (tr.End != PenguinTrackEnd.Enter) AddFx(FxKind.Splash, at, tr.End == PenguinTrackEnd.Sink ? _cfg.sinkDuration : _cfg.fallDuration);
                    break;
                case PenguinTrackEnd.Snow:
                    GameAudio.Play(AudioEvents.PenguinSnow);
                    break;
            }
        }

        /// <summary>Squashes, faces, vanishing actors, fish, the igloo door and the effects (real time).</summary>
        void TickTimers(float dt)
        {
            var l = _puzzle.Layout;
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
                    float dur = _vanishKind[a] == PenguinTrackEnd.Enter ? _cfg.enterDuration : _vanishKind[a] == PenguinTrackEnd.Sink ? _cfg.sinkDuration : _cfg.fallDuration;
                    float u = Mathf.Clamp01(_vanish[a] / Mathf.Max(0.01f, dur));
                    float s = Mathf.Lerp(1f, _vanishKind[a] == PenguinTrackEnd.Sink ? 0.55f : 0.25f, u);
                    scale = new Vector3(s, s, 1f);
                    if (a < FirstBlock) _penguins[a].Group.alpha = 1f - u;
                    else _actorBlock[a].color = new Color(1f, 1f, 1f, 1f - u);
                    if (u >= 1f)
                    {
                        _vanish[a] = -1f;
                        rt.gameObject.SetActive(false);
                        if (a >= FirstBlock)
                        {
                            // the hole is ice now
                            foreach (var tr in _play.Trace.Tracks)
                                if (tr.Actor == a && tr.End == PenguinTrackEnd.Sink && _waterImages.TryGetValue(tr.To, out var water)) water.gameObject.SetActive(false);
                        }
                    }
                }
                rt.localScale = scale;
            }
            for (int i = 0; i < PenguinLayout.MaxPenguins; i++)
            {
                if (_dizzy[i] >= 0f)
                {
                    _dizzy[i] += dt;
                    if (_dizzy[i] >= _cfg.dizzyTime) _dizzy[i] = -1f;
                }
                var face = PenguinSprites.Face.Normal;
                if (_vanish[i] >= 0f) face = _vanishKind[i] == PenguinTrackEnd.Enter ? PenguinSprites.Face.Happy : PenguinSprites.Face.Scared;
                else if (_dizzy[i] >= 0f) face = PenguinSprites.Face.Dizzy;
                _penguins[i].Face.sprite = _sprites.FaceSprite(face);
            }
            for (int f = 0; f < _fishPop.Length; f++)
            {
                if (_fishPop[f] < 0f) continue;
                _fishPop[f] += dt;
                float u = Mathf.Clamp01(_fishPop[f] / Mathf.Max(0.01f, _cfg.fishPop));
                _fishImages[f].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, u);
                _fishImages[f].color = new Color(1f, 1f, 1f, 1f - u);
                if (u >= 1f)
                {
                    _fishPop[f] = -1f;
                    _fishImages[f].gameObject.SetActive(false);
                }
            }
            if (_iglooOpen >= 0f && l != null)
            {
                _iglooOpen += dt;
                float u = Mathf.Clamp01(_iglooOpen / Mathf.Max(0.01f, _cfg.iglooOpen));
                for (int i = 0; i < l.PenguinCount; i++)
                {
                    var ig = _igloos[i];
                    ig.Closed.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, u);
                    ig.Closed.color = new Color(1f, 1f, 1f, 1f - u);
                    float bounce = 1f + Mathf.Sin(u * Mathf.PI) * 0.08f;
                    ig.Body.rectTransform.localScale = new Vector3(bounce, bounce, 1f);
                    if (u >= 1f) ig.Closed.gameObject.SetActive(false);
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

        /// <summary>The move is over: what the rules say is shown; a fall starts the failure, the last penguin in the win.</summary>
        void FinishMove()
        {
            foreach (var p in _penguins) p.Root.localScale = Vector3.one;
            ShowStateKeepingFaces();
            if (_board.Cell > 0) PlaceActors();
            if (_puzzle.Fell)
            {
                RaiseMessage(PenguinKeys.Splash);
                _failTime = 0f;
            }
            else if (_puzzle.IsSolved) _winTime = 0f;
            RaiseChanged();
        }

        void ShowStateKeepingFaces()
        {
            var faces = new Sprite[PenguinLayout.MaxPenguins];
            for (int i = 0; i < faces.Length; i++) faces[i] = _penguins[i].Face.sprite;
            for (int a = 0; a < Actors; a++) _vanish[a] = -1f;
            ShowState();
            for (int i = 0; i < faces.Length; i++) _penguins[i].Face.sprite = faces[i];
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
