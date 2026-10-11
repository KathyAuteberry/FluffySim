using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

// Movement utility buttons remain usable when optional combat-action simulation is disabled.
internal static unsafe class AntiKnockbackHandler
{
    public static void Apply(SimCharacter? character, uint actionId)
    {
        if (character is null) return;
        if (actionId == 7548) character.AddStatus(1209,6f);
        if (actionId == 7559) character.AddStatus(160,6f);
    }
    public static void OnScenarioStart()
    {
        var manager=ActionManager.Instance();
        if (manager == null) return;
        foreach (var id in new uint[]{7548,7559})
        {
            var group=manager->GetRecastGroup((int)ActionType.Action,id);
            if(group<0)continue;
            var detail=manager->GetRecastGroupDetail(group);
            if(detail==null)continue;
            detail->IsActive=false;detail->Elapsed=0f;
        }
    }
}
