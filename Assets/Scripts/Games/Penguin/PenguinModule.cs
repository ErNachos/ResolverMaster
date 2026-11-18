using System.Collections.Generic;
using System.Linq;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Puzzle;
using ResolverMaster.Core.UI;
using ResolverMaster.Games.Penguin.View;
using UnityEngine;
using UnityEngine.Scripting;

namespace ResolverMaster.Games.Penguin
{
    /// <summary>
    /// Pino (PG-001): a penguin slides on the ice until something stops it, and must reach its igloo (after taking every fish).
    /// The game gives only its rules, generator, solver, difficulty tables and art; Story, Challenge, daily challenge, stars, undo,
    /// light bulb, "You are stuck" and rewarded ads are Core. Technical name: Penguin (the shown name is a text key).
    /// </summary>
    [Preserve]
    public sealed class PenguinModule : MinigameModule
    {
        public const string Id = "penguin";
        /// <summary>Resources path of the Story levels (Assets/Resources/Games/Penguin/penguin_story.json).</summary>
        public const string StoryResourcesPath = "Games/Penguin/penguin_story";
        public const string Icon = "game_penguin";

        static readonly GameInfo s_info = new GameInfo
        {
            Id = Id,
            NameKey = PenguinKeys.Name,
            IconName = Icon,
            Availability = GameAvailability.Available,
            Order = 30,
            CardImage = "card_penguin",
            BackgroundColor = UiKitStyle.Hex(0xDDEEF4),   // ErNa-010: soft ice blue
            CardColor = UiKitStyle.Hex(0x9ED3E0),
        };

        List<PenguinLevel> _story;
        StoryBook _book;
        IChallengeSource _challenge;

        [Preserve] public PenguinModule() { }

        public override GameInfo Info => s_info;

        /// <summary>Every Story level, in number order (index = level number - 1).</summary>
        public IReadOnlyList<PenguinLevel> StoryLevels => _story ??= LoadStory();

        public override StoryBook Story => _book ??= BuildBook(StoryLevels, PenguinConfig.LoadOrDefault().chapters);

        public override PuzzleBoard CreateBoard(AppServices ctx, RectTransform parent) => PenguinBoardView.Create(ctx, parent, this);

        /// <summary>The Pino Challenge: step table and margins from PenguinConfig, generator with the exact solver.</summary>
        public override IChallengeSource Challenge => _challenge ??= PenguinConfig.LoadOrDefault().CreateChallenge();

        /// <summary>The Story chapters (Core <see cref="StoryBook.Build"/>), themes from PenguinConfig.</summary>
        public static StoryBook BuildBook(IReadOnlyList<PenguinLevel> levels, IReadOnlyList<ChapterTheme> themes) =>
            StoryBook.Build(Id, levels.Select(l => l.chapter).ToList(), themes, Icon);

        static List<PenguinLevel> LoadStory()
        {
            var text = Resources.Load<TextAsset>(StoryResourcesPath);
            if (text == null)
            {
                Debug.LogWarning("PenguinModule: Story levels missing (Resources/" + StoryResourcesPath + "). Run Resolver Master > Pino > Genera livelli Storia.");
                return new List<PenguinLevel>();
            }
            var set = JsonUtility.FromJson<PenguinLevelSet>(text.text);
            return set?.levels ?? new List<PenguinLevel>();
        }
    }
}
