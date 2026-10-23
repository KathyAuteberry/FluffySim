using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Tests;

internal sealed class FakePartyHud : IPartyHud
{
    public List<(AnoMech.Core.Game.Party.PartyRole Role, uint Actor)> Members { get; } = new();
    public void Refresh(SimParty party)
    {
        Members.Clear();
        foreach (var member in AnoMech.Core.Game.Party.PartyPresentation.Members(party))
            if (member.Proxy is { Exists: true } proxy)
                Members.Add((((ISimPartyMember)member).Role, proxy.EntityId));
    }
    public void Clear() => Members.Clear();
}
