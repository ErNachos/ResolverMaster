using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Games.Penguin;

namespace ResolverMaster.Tests
{
    /// <summary>PG-002: which Pino drawing shows what (pose of a penguin, turn by direction, tiles turned and mirrored by position).</summary>
    public sealed class PenguinLookTests
    {
        const float Turn = 0.16f, Dizzy = 0.6f;

        static PenguinMood NewMood() => new PenguinMood { TurnTime = Turn, DizzyTime = Dizzy, MinDizzyCells = 3 };

        /// <summary>Lets the time pass in small steps (as the frames do).</summary>
        static void Wait(PenguinMood m, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.01f) m.Tick(0.01f);
        }

        [Test]
        public void Mood_PicksTheDrawingByState_StillSlidingAndEvents_SwipeInterruptsAtOnce()
        {
            var m = NewMood();
            Assert.AreEqual("Penguin_Orange_Front_Normal", m.SpriteName(0), "start of the level: normal, from the front");
            Assert.AreEqual("Penguin_Purple_Front_Normal", m.SpriteName(1), "the second penguin has its own drawings");

            m.BeginSlide(SquareTopology.Right);
            Assert.AreEqual("Penguin_Orange_Top_Slide", m.SpriteName(0), "the first swipe: from above at once, sliding");
            Assert.AreEqual(SquareTopology.Right, m.Dir);
            m.EndSlide(PenguinTrackEnd.Bump, 2);
            Wait(m, 1f);
            Assert.AreEqual("Penguin_Orange_Top_Idle", m.SpriteName(0), "a stop after 2 cells: still, from above, no expression");

            m.BeginSlide(SquareTopology.Up);
            m.EndSlide(PenguinTrackEnd.Bump, 3);
            Wait(m, Turn * 0.25f);
            Assert.IsFalse(m.Front, "the first half of the turn still shows the view from above");
            Assert.Less(m.SquashX, 1f, "squashed while it turns");
            Wait(m, Turn);
            Assert.AreEqual("Penguin_Orange_Front_Dizzy", m.SpriteName(0), "a stop after 3 cells: dizzy, from the front");
            Assert.AreEqual(1f, m.SquashX);
            Wait(m, Dizzy + Turn + 0.05f);
            Assert.AreEqual("Penguin_Orange_Top_Idle", m.SpriteName(0), "the dizzy face lasts a little, then back to the view from above");

            m.BeginSlide(SquareTopology.Left);
            m.EndSlide(PenguinTrackEnd.Push, 4);
            Wait(m, Turn + 0.05f);
            Assert.AreEqual(PenguinFace.Dizzy, m.Face, "pushing a block after a long slide is a hard stop too");
            m.BeginSlide(SquareTopology.Down);
            Assert.AreEqual("Penguin_Orange_Top_Slide", m.SpriteName(0), "a swipe interrupts the dizzy face at once");
            Assert.AreEqual(1f, m.SquashX);

            m.EndSlide(PenguinTrackEnd.Enter, 2);
            Wait(m, 5f);
            Assert.AreEqual("Penguin_Orange_Front_Happy", m.SpriteName(0), "in the igloo: happy until the end of the level");

            var fall = NewMood();
            fall.BeginSlide(SquareTopology.Down);
            fall.EndSlide(PenguinTrackEnd.FallOff, 1);
            Wait(fall, 5f);
            Assert.AreEqual("Penguin_Purple_Front_Scared", fall.SpriteName(1), "falling in the water: scared");

            var other = NewMood();
            other.Interrupt();
            Wait(other, Turn + 0.05f);
            Assert.AreEqual("Penguin_Orange_Top_Idle", other.SpriteName(0), "a swipe that does not move this penguin ends its normal face too");
            Assert.IsFalse(other.Busy);
        }

        [Test]
        public void TopRotation_TurnsTheHeadTowardsTheMove()
        {
            for (int dir = 0; dir < 4; dir++)
            {
                // the drawing has its head up (0, 1) on screen; turned by the rotation it must point where the move goes (y up on screen)
                double a = PenguinLook.TopRotation(dir) * System.Math.PI / 180.0;
                double x = -System.Math.Sin(a), y = System.Math.Cos(a);
                Assert.AreEqual(PenguinLayout.Dx(dir), x, 1e-6, $"dir {dir}: x");
                Assert.AreEqual(-PenguinLayout.Dy(dir), y, 1e-6, $"dir {dir}: y");
            }
        }

        [Test]
        public void TileLook_SameCellSameLook_TilesNotAllAlike()
        {
            var turns = new HashSet<int>();
            var mirrors = new HashSet<bool>();
            int differentSalt = 0;
            for (int y = 0; y < 9; y++)
            for (int x = 0; x < 7; x++)
            {
                PenguinLook.TileLook(x, y, 0, out int q, out bool m);
                PenguinLook.TileLook(x, y, 0, out int q2, out bool m2);
                Assert.AreEqual(q, q2, "the same cell always looks the same");
                Assert.AreEqual(m, m2);
                Assert.That(q, Is.InRange(0, 3));
                turns.Add(q);
                mirrors.Add(m);
                PenguinLook.TileLook(x, y, 2, out int q3, out bool m3);
                if (q3 != q || m3 != m) differentSalt++;
            }
            Assert.AreEqual(4, turns.Count, "all four turns are used on a 7x9 grid");
            Assert.AreEqual(2, mirrors.Count, "mirrored and not mirrored tiles");
            Assert.Greater(differentSalt, 20, "another kind of tile has its own pattern");
        }

        [Test]
        public void EveryDrawingName_IsAFileOfThePinoArt()
        {
            var names = PenguinLook.AllNames().ToList();
            Assert.AreEqual(names.Count, names.Distinct().Count(), "unique names");
            Assert.AreEqual(2 * (4 + 2 + 2) + 8, names.Count);
            foreach (var name in names)
                Assert.IsTrue(File.Exists($"{ResolverMaster.Games.Penguin.View.PenguinArt.AssetDir}/{name}.png"), name + ".png missing (python Tools/Art/make_penguin_art.py)");
        }
    }
}
