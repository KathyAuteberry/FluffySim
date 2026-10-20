using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

internal sealed class LocalProvokeHandler : IUserActionHandler
{
    public bool RunsWhenDisabled => true;
    public void OnAction(ActionType type, uint action, ulong target)
    {
        if (type == ActionType.Action && action == 7533 && Plugin.Game?.Player is { } player)
            Plugin.Game.World.NotifyActionResolved(player, action, target);
    }
}
