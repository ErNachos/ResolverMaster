using System.Collections.Generic;
using System.Linq;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Games.Plumber.View;
using UnityEngine;
using UnityEngine.Scripting;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>Plumber: rotate the pipes to join every source to the fountains of its color, then open the water.</summary>
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
            CardImage = "card_plumber",
            CardColor = UiKitStyle.Hex(0x9B7BEA),
        };

        List<PlumberLevel> _story;
        StoryBook _book;

        [Preserve] public PlumberModule() { }

        public override GameInfo Info => s_info;

        /// <summary>Every Story level, in number order (index = level number - 1).</summary>
        public IReadOnlyList<PlumberLevel> StoryLevels => _story ??= LoadStory();

        public override StoryBook Story => _book ??= BuildBook(StoryLevels, PlumberConfig.LoadOrDefault().chapters);

        public override PuzzleBoard CreateBoard(AppServices ctx, RectTransform parent) => PlumberBoardView.Create(ctx, parent, this);

        IChallengeSource _challenge;

        /// <summary>ErNa-007: the Plumber Challenge (step table and margins from PlumberConfig, generator + exact solver).</summary>
        public override IChallengeSource Challenge
        {
            get
            {
                if (_challenge != null) return _challenge;
                var cfg = PlumberConfig.LoadOrDefault();
                return _challenge = new PlumberChallenge(cfg.challengeDifficulty, cfg.challenge, cfg.challengeMaxAttempts);
            }
        }

        /// <summary>Groups the numbered levels by their chapter field; the chapter themes come from PlumberConfig.</summary>
        public static StoryBook BuildBook(IReadOnlyList<PlumberLevel> levels, IReadOnlyList<ChapterTheme> themes) =>
            StoryBook.Build(Id, levels.Select(l => l.chapter).ToList(), themes, "game_plumber");   // TJ-001: the grouping is Core

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
