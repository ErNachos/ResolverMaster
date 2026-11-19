using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>The expression of a penguin seen from the front (PG-002).</summary>
    public enum PenguinFace
    {
        Normal,
        Happy,
        Scared,
    }

    /// <summary>
    /// PG-002, which drawing shows what (pure logic, no Unity types): the names of the sprites in Assets/Art/Penguin (prepared by
    /// Tools/Art/make_penguin_art.py), the turn of a penguin seen from above, the deterministic turn and mirror of every tile.
    /// Penguin 0 is orange (plain scarf), penguin 1 purple (striped scarf): never the color alone.
    /// </summary>
    public static class PenguinLook
    {
        public static readonly string[] ColorNames = { "Orange", "Purple" };

        public const string Fish = "Fish";
        public const string IceBlock = "IceBlock";
        public const string Rock = "Rock";
        public const string TileIce = "Tile_Ice";
        public const string TileSnow = "Tile_Snow";
        public const string TileWater = "Tile_Water";
        public const string TileSunkenBlock = "Tile_SunkenBlock";
        public const string TileSnowWall = "Tile_SnowWall";

        static string ColorOf(int penguin) => ColorNames[penguin <= 0 ? 0 : 1];

        /// <summary>The penguin seen from the front with an expression (feet on the bottom edge of the drawing).</summary>
        public static string FrontName(int penguin, PenguinFace face) => $"Penguin_{ColorOf(penguin)}_Front_{face}";

        /// <summary>The penguin seen from above, head up (still or sliding on its belly; the body centred in the drawing).</summary>
        public static string TopName(int penguin, bool sliding) => $"Penguin_{ColorOf(penguin)}_Top_{(sliding ? "Slide" : "Idle")}";

        /// <summary>The igloo of a penguin: the flag has its color and pattern.</summary>
        public static string IglooName(int penguin, bool open) => $"Igloo_{ColorOf(penguin)}_{(open ? "Open" : "Closed")}";

        /// <summary>The drawing of a penguin in a pose.</summary>
        public static string PenguinName(int penguin, bool front, PenguinFace face, bool sliding) =>
            front ? FrontName(penguin, face) : TopName(penguin, sliding);

        /// <summary>Every drawing the game uses (the validator checks them all).</summary>
        public static IEnumerable<string> AllNames()
        {
            for (int p = 0; p < ColorNames.Length; p++)
            {
                foreach (PenguinFace f in System.Enum.GetValues(typeof(PenguinFace))) yield return FrontName(p, f);
                yield return TopName(p, false);
                yield return TopName(p, true);
                yield return IglooName(p, false);
                yield return IglooName(p, true);
            }
            yield return Fish;
            yield return IceBlock;
            yield return Rock;
            yield return TileIce;
            yield return TileSnow;
            yield return TileWater;
            yield return TileSunkenBlock;
            yield return TileSnowWall;
        }

        /// <summary>
        /// The z rotation (degrees, counter-clockwise) of a penguin seen from above that looks towards <paramref name="dir"/> (the
        /// drawing has its head up): up 0, right -90, down 180, left 90.
        /// </summary>
        public static float TopRotation(int dir)
        {
            switch (dir)
            {
                case SquareTopology.Right: return -90f;
                case SquareTopology.Down: return 180f;
                case SquareTopology.Left: return 90f;
                default: return 0f;
            }
        }

        /// <summary>
        /// Quarter turns (0-3, counter-clockwise) and mirror of the tile at (<paramref name="x"/>, <paramref name="y"/>): the tiles do not
        /// all look the same, and the same level always looks the same (a hash of the position and <paramref name="salt"/>, e.g. the kind
        /// of tile or a border piece).
        /// </summary>
        public static void TileLook(int x, int y, int salt, out int quarterTurns, out bool mirrored)
        {
            unchecked
            {
                uint h = (uint)x * 0x9E3779B1u ^ (uint)y * 0x85EBCA77u ^ (uint)salt * 0xC2B2AE3Du;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                quarterTurns = (int)(h & 3u);
                mirrored = (h & 4u) != 0;
            }
        }

    }

    /// <summary>
    /// PG-002, the pose of one penguin (pure logic, real time): seen from above while playing (still, or sliding, turned towards the
    /// last move), it "turns towards us" (a horizontal squash that swaps the drawing at half way) to show an expression: normal at the
    /// start of a level until the first swipe, scared when it falls in the water, happy in its igloo (until the end of the level). A stop
    /// against a rock, a wall, a block or the other penguin keeps the view from above (ErNa-012: no dizzy face). A swipe interrupts the
    /// normal face at once: the expressions never slow the game down.
    /// </summary>
    public sealed class PenguinMood
    {
        /// <summary>The whole turn (squash and back), seconds.</summary>
        public float TurnTime = 0.16f;

        /// <summary>Seen from the front (true) or from above.</summary>
        public bool Front { get; private set; } = true;
        public PenguinFace Face { get; private set; } = PenguinFace.Normal;
        /// <summary>Sliding on its belly (seen from above).</summary>
        public bool Sliding { get; private set; }
        /// <summary>The direction of the last move (the penguin seen from above looks that way).</summary>
        public int Dir { get; private set; } = SquareTopology.Down;
        /// <summary>Horizontal scale of the turn: 1, down to 0 at half way (the drawing changes), back to 1.</summary>
        public float SquashX { get; private set; } = 1f;
        /// <summary>A turn or a timed expression is running.</summary>
        public bool Busy => _turn >= 0f;

        float _turn = -1f;
        bool _toFront, _swapped;
        PenguinFace _toFace;

        /// <summary>The drawing to show now (see <see cref="PenguinLook.PenguinName"/>).</summary>
        public string SpriteName(int penguin) => PenguinLook.PenguinName(penguin, Front, Face, Sliding);

        /// <summary>Shows a pose at once (start of a level, undo, the state after a jump).</summary>
        public void Show(bool front, PenguinFace face, int dir = -1)
        {
            _turn = -1f;
            Front = front;
            Face = face;
            Sliding = false;
            SquashX = 1f;
            if (dir >= 0) Dir = dir;
        }

        /// <summary>The penguin starts to slide: seen from above at once (an expression is interrupted).</summary>
        public void BeginSlide(int dir)
        {
            Show(false, Face, dir);
            Sliding = true;
        }

        /// <summary>The swipe moved the others, not this penguin: a normal face turns back to the view from above.</summary>
        public void Interrupt()
        {
            if (_turn >= 0f)
            {
                if (_toFront && Passing(_toFace)) Show(false, Face);   // turning towards us: back to the view from above at once
                return;
            }
            if (Front && Passing(Face)) TurnTo(false, Face);
        }

        /// <summary>Expressions a swipe interrupts (happy and scared stay until the end of the level).</summary>
        static bool Passing(PenguinFace face) => face == PenguinFace.Normal;

        /// <summary>The slide is over: the expression of the event, if any (a stop against something: none, still seen from above).</summary>
        public void EndSlide(PenguinTrackEnd end)
        {
            Sliding = false;
            switch (end)
            {
                case PenguinTrackEnd.Enter:
                    TurnTo(true, PenguinFace.Happy);
                    break;
                case PenguinTrackEnd.Fall:
                case PenguinTrackEnd.FallOff:
                    TurnTo(true, PenguinFace.Scared);
                    break;
            }
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (_turn >= 0f)
            {
                _turn += dt;
                float u = _turn / (TurnTime > 0.001f ? TurnTime : 0.001f);
                if (u >= 0.5f && !_swapped)
                {
                    _swapped = true;
                    Front = _toFront;
                    Face = _toFace;
                }
                if (u >= 1f)
                {
                    _turn = -1f;
                    SquashX = 1f;
                }
                else SquashX = u < 0.5f ? 1f - 2f * u : 2f * u - 1f;
            }
        }

        void TurnTo(bool front, PenguinFace face)
        {
            if (_turn < 0f && Front == front && (!front || Face == face)) return;   // already there
            _turn = 0f;
            _swapped = false;
            _toFront = front;
            _toFace = face;
        }
    }
}
