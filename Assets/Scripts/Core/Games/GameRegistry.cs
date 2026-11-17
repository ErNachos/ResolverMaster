using System;
using System.Collections.Generic;
using System.Linq;

namespace ResolverMaster.Core.Games
{
    /// <summary>The list of minigames shown in Home, sorted by <see cref="GameInfo.Order"/>.</summary>
    public sealed class GameRegistry
    {
        /// <summary>Only assemblies whose name starts with this are scanned (fast startup).</summary>
        public const string AssemblyPrefix = "ResolverMaster";

        readonly List<GameInfo> _games = new List<GameInfo>();

        public IReadOnlyList<GameInfo> Games => _games;

        public void Register(GameInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            _games.Add(info);
            _games.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public GameInfo Find(string id) => _games.FirstOrDefault(g => g.Id == id);

        /// <summary>Creates the registry from every <see cref="MinigameModule"/> subclass in the ResolverMaster assemblies.</summary>
        public static GameRegistry Discover()
        {
            var registry = new GameRegistry();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name.StartsWith(AssemblyPrefix, StringComparison.Ordinal)) continue;
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || !typeof(MinigameModule).IsAssignableFrom(type)) continue;
                    var module = (MinigameModule)Activator.CreateInstance(type);
                    registry.Register(module.Info);
                }
            }
            return registry;
        }
    }
}
