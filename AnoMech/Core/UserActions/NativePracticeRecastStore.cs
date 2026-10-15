using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

internal sealed unsafe class NativePracticeRecastStore : IPracticeRecastStore
{
    public int Group(uint action)
    {
        var am = ActionManager.Instance();
        return am == null ? -1 : am->GetRecastGroup((int)ActionType.Action, action);
    }
    public LocalRecast? Read(int group)
    {
        var am = ActionManager.Instance();
        if (am == null) return null;
        var detail = am->GetRecastGroupDetail(group);
        return detail == null ? null : new(detail->IsActive, detail->ActionId, detail->Elapsed, detail->Total);
    }
    public void Write(int group, LocalRecast value)
    {
        var am = ActionManager.Instance();
        if (am == null) throw new System.InvalidOperationException("Action manager unavailable during recast restoration");
        var detail = am->GetRecastGroupDetail(group);
        if (detail == null) throw new System.InvalidOperationException("Recast group unavailable during restoration");
        detail->IsActive = value.Active;
        detail->ActionId = value.ActionId;
        detail->Elapsed = value.Elapsed;
        detail->Total = value.Total;
    }
}
