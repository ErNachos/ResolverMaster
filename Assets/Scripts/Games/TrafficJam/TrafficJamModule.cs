using ResolverMaster.Core.Games;
using UnityEngine.Scripting;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>Traffic jam: slide cars and trucks to let one vehicle out of the parking lot. Rules, generator and art come in a later Update.</summary>
    [Preserve]
    public sealed class TrafficJamModule : MinigameModule
    {
        public const string Id = "trafficjam";

        static readonly GameInfo s_info = new GameInfo
        {
            Id = Id,
            NameKey = "game.trafficjam.name",
            IconName = "game_trafficjam",
            Availability = GameAvailability.ComingSoon,
            Order = 20,
        };

        [Preserve] public TrafficJamModule() { }

        public override GameInfo Info => s_info;
    }
}
