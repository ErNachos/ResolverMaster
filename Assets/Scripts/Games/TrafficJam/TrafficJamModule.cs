using System.Collections.Generic;
using System.Linq;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Games.TrafficJam.View;
using UnityEngine;
using UnityEngine.Scripting;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>
    /// Traffic jam (TJ-001): slide cars and trucks along their lines until the red car (and, later, the blue one) leaves by its exit.
    /// The game gives only its rules, generator, solver, difficulty tables and art; Story, Challenge, daily challenge, stars, undo,
    /// light bulb and rewarded ads are Core.
    /// </summary>
    [Preserve]
    public sealed class TrafficJamModule : MinigameModule
    {
        public const string Id = "trafficjam";
        /// <summary>Resources path of the Story levels (Assets/Resources/Games/TrafficJam/trafficjam_story.json).</summary>
        public const string StoryResourcesPath = "Games/TrafficJam/trafficjam_story";

        static readonly GameInfo s_info = new GameInfo
        {
            Id = Id,
            NameKey = TrafficJamKeys.Name,
            IconName = "game_trafficjam",
            Availability = GameAvailability.Available,
            Order = 20,
            CardImage = "card_trafficjam",
            CardColor = UiKitStyle.Hex(0xF7C948),
        };

        List<TrafficLevel> _story;
        StoryBook _book;
        IChallengeSource _challenge;

        [Preserve] public TrafficJamModule() { }

        public override GameInfo Info => s_info;

        /// <summary>Every Story level, in number order (index = level number - 1).</summary>
        public IReadOnlyList<TrafficLevel> StoryLevels => _story ??= LoadStory();

        public override StoryBook Story => _book ??= BuildBook(StoryLevels, TrafficJamConfig.LoadOrDefault().chapters);

        public override PuzzleBoard CreateBoard(AppServices ctx, RectTransform parent) => TrafficBoardView.Create(ctx, parent, this);

        /// <summary>The Traffic jam Challenge: step table and margins from TrafficJamConfig, generator with the exact solver.</summary>
        public override IChallengeSource Challenge
        {
            get
            {
                if (_challenge != null) return _challenge;
                var cfg = TrafficJamConfig.LoadOrDefault();
                return _challenge = new TrafficChallenge(cfg.challengeDifficulty, cfg.challenge, cfg.challengeMaxAttempts, cfg.challengeMaxStates,
                    cfg.maxMinMoves, cfg.climbSteps);
            }
        }

        /// <summary>The Story chapters (Core <see cref="StoryBook.Build"/>), themes from TrafficJamConfig.</summary>
        public static StoryBook BuildBook(IReadOnlyList<TrafficLevel> levels, IReadOnlyList<ChapterTheme> themes) =>
            StoryBook.Build(Id, levels.Select(l => l.chapter).ToList(), themes, "game_trafficjam");

        static List<TrafficLevel> LoadStory()
        {
            var text = Resources.Load<TextAsset>(StoryResourcesPath);
            if (text == null)
            {
                Debug.LogWarning("TrafficJamModule: Story levels missing (Resources/" + StoryResourcesPath + "). Run Resolver Master > Traffic jam > Genera livelli Storia.");
                return new List<TrafficLevel>();
            }
            var set = JsonUtility.FromJson<TrafficLevelSet>(text.text);
            return set?.levels ?? new List<TrafficLevel>();
        }
    }
}
