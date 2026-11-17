using UnityEngine.Scripting;

namespace ResolverMaster.Core.Games
{
    /// <summary>
    /// Entry point of a minigame. Each minigame (Assets/Scripts/Games/&lt;Name&gt;/) has exactly one non-abstract subclass with a
    /// public parameterless constructor: <see cref="GameRegistry"/> finds it by reflection, so Core never references a game.
    /// Later Updates add here the hooks for Story/Challenge (level source, board view, rules).
    /// </summary>
    [Preserve]
    public abstract class MinigameModule
    {
        public abstract GameInfo Info { get; }
    }
}
