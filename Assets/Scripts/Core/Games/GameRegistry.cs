using System;
using System.Collections.Generic;
using System.Linq;

namespace ResolverMaster.Core.Games
{
    /// <summary>The minigames shown in Home, sorted by <see cref="GameInfo.Order"/>.</summary>
    public sealed class GameRegistry
    {
        /// <summary>Only assemblies whose name starts with this are scanned (fast startup).</summary>
        public const string AssemblyPrefix = "ResolverMaster";

        readonly List<MinigameModule> _modules = new List<MinigameModule>();
        readonly List<GameInfo> _games = new List<GameInfo>();

        public IReadOnlyList<GameInfo> Games => _games;
        public IReadOnlyList<MinigameModule> Modules => _modules;

        public void Register(MinigameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (module.Info == null) throw new ArgumentException("module without GameInfo: " + module.GetType().Name);
            _modules.Add(module);
            _modules.Sort((a, b) => a.Info.Order.CompareTo(b.Info.Order));
            _games.Clear();
            _games.AddRange(_modules.Select(m => m.Info));
        }

        public GameInfo Find(string id) => _games.FirstOrDefault(g => g.Id == id);

        public MinigameModule FindModule(string id) => _modules.FirstOrDefault(m => m.Info.Id == id);

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
                    registry.Register((MinigameModule)Activator.CreateInstance(type));
                }
            }
            return registry;
        }
    }
}
