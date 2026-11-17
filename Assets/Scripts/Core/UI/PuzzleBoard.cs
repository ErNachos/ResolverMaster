using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI.Themes;
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
        /// <summary>Main action run on the solved board, at the end of its success animation (before the win panel).</summary>
        ActionSuccess,
        /// <summary>Main action run on an unsolved board, at the moment of its failure effect (message visible).</summary>
        ActionFailure,
        /// <summary>Main action run on the solved board, halfway through the flow (the water front is visible).</summary>
        ActionFlowing,
    }

    /// <summary>Optional big action button of the game screen (e.g. "Open the water"): text and icon chosen by the game.</summary>
    public sealed class MainActionInfo
    {
        public string LabelKey;
        public string IconName;
    }

    /// <summary>
    /// The game-specific part of the common game screen: draws the puzzle, takes the taps, runs the animations.
    /// The Core <c>GameScreen</c> owns the bars, the counters, the optional main action button, the short messages and the win panel,
    /// and talks to the board only through this class.
    /// </summary>
    public abstract class PuzzleBoard : MonoBehaviour
    {
        /// <summary>A move, undo, restart or load changed the counters.</summary>
        public event Action Changed;

        /// <summary>The level is solved and the win effect has finished: show the win panel.</summary>
        public event Action Completed;

        /// <summary>Show a short message (localization key) on the game screen, e.g. why an attempt failed.</summary>
        public event Action<string> MessageRequested;

        /// <summary>ErNa-007: an attempt failed and its effect has finished (raised only when <see cref="RestartOnFailure"/> is false).</summary>
        public event Action Failed;

        /// <summary>Story: a failed attempt restarts the level. Challenge (ErNa-007): it does not; the board stops and raises <see cref="Failed"/>.</summary>
        public bool RestartOnFailure { get; set; } = true;

        /// <summary>ErNa-007, Challenge: moves allowed on this level (0 = no limit). Set before or after <see cref="Load"/>.</summary>
        public virtual void SetMoveLimit(int limit) { }

        /// <summary>ErNa-007, Challenge save: the moves on the undo stack (oldest first) as integers in the game's format.</summary>
        public virtual IReadOnlyList<int> MoveHistory => Array.Empty<int>();

        /// <summary>ErNa-007, Challenge save: the state of the pieces in the game's format (e.g. one rotation digit per cell).</summary>
        public virtual string CaptureState() => "";

        /// <summary>ErNa-007, resume: applies the saved moves to the freshly loaded level. False if they do not fit it.</summary>
        public virtual bool RestoreMoves(IReadOnlyList<int> history) => history == null || history.Count == 0;

        public abstract IPuzzleSession Session { get; }

        /// <summary>The rect holding the cells (measured by the previews).</summary>
        public abstract RectTransform GridRect { get; }

        public abstract int GridColumns { get; }

        /// <summary>Rows of the grid (columns and rows can differ, ErNa-005).</summary>
        public abstract int GridRows { get; }

        /// <summary>Theme of the chapter being played (terrain of the cells), set by the game screen before <see cref="Load"/>. May be null.</summary>
        public VisualTheme Theme { get; set; }

        /// <summary>Null = no main action button (default). Read once when the board is shown.</summary>
        public virtual MainActionInfo MainAction => null;

        /// <summary>An animation is running: the main action, undo and restart are disabled.</summary>
        public virtual bool IsBusy => false;

        /// <summary>The player pressed the main action button (only called when <see cref="IsBusy"/> is false).</summary>
        public virtual void RunMainAction() { }

        /// <summary>ErNa-007, the light bulb: a hint can fix something now (false = button disabled; games without hints keep false).</summary>
        public virtual bool CanHint => false;

        /// <summary>A hint was used since the level was loaded (restarts after a failure keep it): at most 2 stars.</summary>
        public virtual bool HintUsed => false;

        /// <summary>Applies one hint (called after the rewarded ad was watched). Not a move.</summary>
        public virtual void ApplyHint() { }

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
        protected void RaiseMessage(string key) => MessageRequested?.Invoke(key);
        protected void RaiseFailed() => Failed?.Invoke();
    }
}
