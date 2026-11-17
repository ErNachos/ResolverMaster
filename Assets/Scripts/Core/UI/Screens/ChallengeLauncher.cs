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
            var game = ctx.Screens.Get<GameScreen>(ScreenIds.Game);
            game.StartChallenge(module, run);
            if (ctx.Screens.Top != game) ctx.Screens.Push(ScreenIds.Game);
        }
    }
}
