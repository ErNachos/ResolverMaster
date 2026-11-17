using ResolverMaster.Core.Games;
using UnityEngine.Scripting;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>Plumber: rotate pipe pieces to bring water from the sources to the outlets. Rules, generator and art come in a later Update.</summary>
    [Preserve]
    public sealed class PlumberModule : MinigameModule
    {
        public const string Id = "plumber";

        static readonly GameInfo s_info = new GameInfo
        {
            Id = Id,
            NameKey = "game.plumber.name",
            IconName = "game_plumber",
            Availability = GameAvailability.ComingSoon,
            Order = 10,
        };

        [Preserve] public PlumberModule() { }

        public override GameInfo Info => s_info;
    }
}
