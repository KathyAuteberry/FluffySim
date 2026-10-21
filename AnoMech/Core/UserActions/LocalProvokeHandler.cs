using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

internal sealed class LocalProvokeHandler : IUserActionHandler
{
    public bool RunsWhenDisabled => true;
    public void OnAction(ActionType type, uint action, ulong target)
    {
        if (type != ActionType.Action || action != 7533) return;
        var game = Plugin.Game;
        var player = game?.Player;
        var enemy = game?.World.Children.OfType<SimEnemy>().FirstOrDefault(e => e.GameObjectId.ObjectId == target || e.EntityId == target);
        var reason = game?.World.Map.IsInInstance != true || player == null ? "no-active-player"
            : player.Dead ? "player-dead"
            : player.Role is not (PartyRole.MainTank or PartyRole.OffTank) || player.Proxy?.ClassJob is not (19 or 21 or 32 or 37) ? "not-a-tank"
            : enemy is not { IsActive: true, Targetable: true } ? "invalid-target" : "eligible";
        var stanceStatuses = player?.ActiveStatusSnapshot.Where(s => s.StatusId is 79 or 91 or 743 or 1833).Select(s => s.StatusId) ?? [];
        Core.DiagnosticLog.Info($"[ProvokeInput] event=local-validation action={action} target=0x{target:X} simTarget=0x{enemy?.GameObjectId.ObjectId:X} bnpc={enemy?.BNpcBaseId} role={player?.Role} job={player?.Proxy?.ClassJob} stanceRequired=false simStanceStatuses=[{string.Join(",", stanceStatuses)}] result={reason}");
        if (reason == "eligible") game!.World.NotifyActionResolved(player!, action, enemy!.GameObjectId.ObjectId);
    }
}
