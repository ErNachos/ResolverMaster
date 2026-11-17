using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.UI;
using ResolverMaster.Games.Plumber.View;
using UnityEngine;
using UnityEngine.Scripting;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>Plumber: rotate pipe pieces to bring the water from the source to the sink.</summary>
    [Preserve]
    public sealed class PlumberModule : MinigameModule
    {
        public const string Id = "plumber";
        /// <summary>Resources path of the Story levels (Assets/Resources/Games/Plumber/plumber_story.json).</summary>
        public const string StoryResourcesPath = "Games/Plumber/plumber_story";

        static readonly GameInfo s_info = new GameInfo
        {
            Id = Id,
            NameKey = "game.plumber.name",
            IconName = "game_plumber",
            Availability = GameAvailability.Available,
            Order = 10,
        };

        List<PlumberLevel> _story;

        [Preserve] public PlumberModule() { }

        public override GameInfo Info => s_info;

        public IReadOnlyList<PlumberLevel> StoryLevels => _story ??= LoadStory();

        public override int StoryLevelCount => StoryLevels.Count;

        public override PuzzleBoard CreateBoard(AppServices ctx, RectTransform parent) => PlumberBoardView.Create(ctx, parent, this);

        static List<PlumberLevel> LoadStory()
        {
            var text = Resources.Load<TextAsset>(StoryResourcesPath);
            if (text == null)
            {
                Debug.LogWarning("PlumberModule: Story levels missing (Resources/" + StoryResourcesPath + "). Run Resolver Master > Idraulico > Genera livelli Storia.");
                return new List<PlumberLevel>();
            }
            var set = JsonUtility.FromJson<PlumberLevelSet>(text.text);
            return set?.levels ?? new List<PlumberLevel>();
        }
    }
}
