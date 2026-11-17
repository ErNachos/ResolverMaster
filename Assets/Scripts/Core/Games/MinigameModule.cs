using ResolverMaster.Core.App;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using UnityEngine;
using UnityEngine.Scripting;

namespace ResolverMaster.Core.Games
{
    /// <summary>
    /// Entry point of a minigame. Each minigame (Assets/Scripts/Games/&lt;Name&gt;/) has exactly one non-abstract subclass with a
    /// public parameterless constructor: <see cref="GameRegistry"/> finds it by reflection, so Core never references a game.
    /// The Core screens (chapters, level grid, game screen, win panel) use the hooks below; the game only provides its chapters and board.
    /// </summary>
    [Preserve]
    public abstract class MinigameModule
    {
        public abstract GameInfo Info { get; }

        /// <summary>The Story chapters and their levels. Null or empty = Story not playable yet (Mode choice shows the placeholder).</summary>
        public virtual StoryBook Story => null;

        public int StoryLevelCount => Story?.LevelCount ?? 0;

        /// <summary>ErNa-007: the game's Challenge (step table, margins, generator). Null = no Challenge yet (placeholder message).</summary>
        public virtual ResolverMaster.Core.Challenge.IChallengeSource Challenge => null;

        /// <summary>Creates the game's board under <paramref name="parent"/> (stretched to it). Called once per app run.</summary>
        public virtual PuzzleBoard CreateBoard(AppServices ctx, RectTransform parent) => null;
    }
}
