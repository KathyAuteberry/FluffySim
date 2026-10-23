using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.Game.Party;

public static class BotActions
{
    public static bool UseInvulnerability(SimWorld world, PartyRole role)
    {
        if (world.Party.Get(role) is not SimPartyNpc { Dead: false, ActionsLocked: false } bot) return false;
        var action = PracticeDefensiveActions.TankAction(bot.ClassJob);
        if (action == 0) return false;
        world.Party.Act(bot, new EnemyAction(action) { Cast = new() { CastSecondsOverride = 0, FaceTarget = false } }, bot);
        return PracticeDefensiveActions.Apply(bot, action);
    }
}
