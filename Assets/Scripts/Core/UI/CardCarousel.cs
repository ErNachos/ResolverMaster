using System;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Logic of the Home card carousel, in pure C# (no Unity types, tested in EditMode): a continuous <see cref="Position"/>
    /// (0 = first card centred, 1 = second...), dragging with a soft resistance past the first and the last card, and the snap:
    /// on release the target is the card nearest to where a flick would end, at most one card away from where the drag started,
    /// never before the first nor after the last. The view only reads <see cref="Offset"/> and <see cref="Distance"/>.
    /// </summary>
    public sealed class CardCarousel
    {
        /// <summary>How much of a drag past the first/last card moves the cards (soft wall).</summary>
        public const float OverscrollFactor = 0.3f;

        float _dragStart;

        public int Count { get; private set; }
        /// <summary>Distance between the centres of two cards (canvas units).</summary>
        public float Step { get; }
        public float Position { get; private set; }
        /// <summary>The card the carousel is moving to (or resting on).</summary>
        public int Target { get; private set; }
        public bool Dragging { get; private set; }
        /// <summary>The card nearest to the centre right now.</summary>
        public int Centered => Nearest(Position, Count);
        public bool Resting => !Dragging && Position == Target;

        public CardCarousel(int count, float step)
        {
            if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));
            Step = step;
            Count = Math.Max(0, count);
        }

        /// <summary>The nearest card to a position, clamped to the first and the last.</summary>
        public static int Nearest(float position, int count)
        {
            if (count <= 0) return 0;
            int i = (int)Math.Floor(position + 0.5f);
            return Clamp(i, 0, count - 1);
        }

        /// <summary>Shows card <paramref name="index"/> at once (start-up, previews).</summary>
        public void JumpTo(int index)
        {
            Target = Clamp(index, 0, Math.Max(0, Count - 1));
            Position = Target;
            Dragging = false;
        }

        /// <summary>Moves to card <paramref name="index"/> with the snap animation (e.g. tap on a side card).</summary>
        public void MoveTo(int index)
        {
            Target = Clamp(index, 0, Math.Max(0, Count - 1));
            Dragging = false;
        }

        public void BeginDrag()
        {
            Dragging = true;
            _dragStart = Position;
        }

        /// <param name="totalDelta">Horizontal finger movement since <see cref="BeginDrag"/> (canvas units, right = positive).</param>
        public void Drag(float totalDelta)
        {
            if (!Dragging) return;
            float p = _dragStart - totalDelta / Step;
            float last = Math.Max(0, Count - 1);
            if (p < 0) p *= OverscrollFactor;
            else if (p > last) p = last + (p - last) * OverscrollFactor;
            Position = p;
        }

        /// <summary>Ends the drag and chooses the card to snap to.</summary>
        /// <param name="velocity">Finger speed at release (canvas units per second, right = positive).</param>
        /// <param name="flickTime">A flick carries the position on by velocity * flickTime.</param>
        public int EndDrag(float velocity, float flickTime)
        {
            if (!Dragging) return Target;
            Dragging = false;
            float projected = Position - velocity * flickTime / Step;
            int start = Nearest(_dragStart, Count);
            int t = Clamp(Nearest(projected, Count), start - 1, start + 1);
            Target = Clamp(t, 0, Math.Max(0, Count - 1));
            return Target;
        }

        /// <summary>Snap animation: the position eases to the target. Returns true while still moving.</summary>
        /// <param name="snapTime">Time constant (s): after it ~63% of the way is done.</param>
        public bool Tick(float dt, float snapTime)
        {
            if (Dragging || Position == Target) return false;
            float k = snapTime <= 0 ? 1f : 1f - (float)Math.Exp(-dt / snapTime);
            Position += (Target - Position) * k;
            if (Math.Abs(Target - Position) < 0.0005f) Position = Target;
            return Position != Target;
        }

        /// <summary>Horizontal offset of card <paramref name="index"/> from the centre (canvas units).</summary>
        public float Offset(int index) => (index - Position) * Step;

        /// <summary>0 for the centred card, 1 for a card one step (or more) away: drives the side scale and alpha.</summary>
        public float Distance(int index) => Math.Min(1f, Math.Abs(index - Position));

        static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
