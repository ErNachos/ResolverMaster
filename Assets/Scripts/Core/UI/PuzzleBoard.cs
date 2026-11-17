using System;
using ResolverMaster.Core.Puzzle;
using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>Board states the editor previews can ask for.</summary>
    public enum BoardPreview
    {
        Start,
        /// <summary>About half of the solution applied (partial progress visible).</summary>
        Partial,
        Solved,
    }

    /// <summary>
    /// The game-specific part of the common game screen: draws the puzzle, takes the taps, runs the animations.
    /// The Core <c>GameScreen</c> owns the bars, the counters and the win panel and talks to the board only through this class.
    /// </summary>
    public abstract class PuzzleBoard : MonoBehaviour
    {
        /// <summary>A move, undo, restart or load changed the counters.</summary>
        public event Action Changed;

        /// <summary>The level is solved and the win effect has finished: show the win panel.</summary>
        public event Action Completed;

        public abstract IPuzzleSession Session { get; }

        /// <summary>The rect holding the cells (measured by the previews).</summary>
        public abstract RectTransform GridRect { get; }

        public abstract int GridColumns { get; }

        public abstract void Load(LevelRequest request);
        public abstract void Undo();
        public abstract void Restart();

        /// <summary>Fits the board into its current rect (called on size changes and by the previews after layout).</summary>
        public abstract void Refit();

        /// <summary>Ends every running animation at once (previews: nothing ticks in edit mode).</summary>
        public abstract void SnapVisuals();

        /// <summary>Editor previews: puts the loaded level in the requested state, without animation.</summary>
        public virtual void PreparePreview(BoardPreview state) { }

        protected void RaiseChanged() => Changed?.Invoke();
        protected void RaiseCompleted() => Completed?.Invoke();
    }
}
