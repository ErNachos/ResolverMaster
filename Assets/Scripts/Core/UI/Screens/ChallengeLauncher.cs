using System;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Challenge;
using ResolverMaster.Core.Games;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// Opens the Challenge of a minigame (ErNa-007), from Home or from the mode choice: the saved run if there is one (unless a new
    /// one is asked for, after the confirmation), otherwise a new run with a random seed. One active run per game.
    /// </summary>
    public static class ChallengeLauncher
    {
        public static bool HasChallenge(MinigameModule module) => module != null && module.Challenge != null;

        public static ChallengeRunState ActiveRun(AppServices ctx, MinigameModule module) =>
            module == null ? null : ctx.Data.challenge.ActiveRun(module.Info.Id);

        /// <summary>A random run seed (only the run seed is random: every level then follows from it).</summary>
        public static int NewRunSeed() => unchecked(Environment.TickCount * 31 + Guid.NewGuid().GetHashCode()) & 0x7FFFFFFF;

        public static void Open(AppServices ctx, MinigameModule module, bool forceNew)
        {
            if (!HasChallenge(module)) return;
            var run = forceNew ? null : ActiveRun(ctx, module);
            if (run == null)
            {
                run = ChallengeRunState.Start(module.Info.Id, NewRunSeed());
                ctx.Data.challenge.SetRun(run);
                ctx.Save();
            }
            Play(ctx, module, run);
        }

        static void Play(AppServices ctx, MinigameModule module, ChallengeRunState run)
        {
            var game = ctx.Screens.Get<GameScreen>(ScreenIds.Game);
            game.StartChallenge(module, run);
            if (ctx.Screens.Top != game) ctx.Screens.Push(ScreenIds.Game);
        }

        /// <summary>ErNa-008: today's state of the daily challenge of a game (Home card).</summary>
        public static DailyStatus DailyStatusOf(AppServices ctx, MinigameModule module, out int stars)
        {
            stars = 0;
            return HasChallenge(module) ? DailyChallenge.Status(ctx.Data.challenge, module.Info.Id, ctx.Today, out stars) : DailyStatus.Done;
        }

        /// <summary>
        /// ErNa-008: opens the daily challenge: the pending one (also of an earlier day), otherwise today's if not played yet.
        /// False when today's is already over.
        /// </summary>
        public static bool OpenDaily(AppServices ctx, MinigameModule module)
        {
            if (!HasChallenge(module)) return false;
            var block = ctx.Data.challenge;
            var run = block.ActiveDaily(module.Info.Id)
                      ?? DailyChallenge.Begin(block, module.Info.Id, ctx.Today, DailyChallenge.Steps(module.Challenge));
            if (run == null) return false;
            ctx.Save();
            Play(ctx, module, run);
            return true;
        }
    }
}
