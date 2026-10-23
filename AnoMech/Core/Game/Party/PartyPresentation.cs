using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game.Party;

public static class PartyPresentation
{
    public static IEnumerable<SimCharacter> Members(SimParty party)
        => party.AllMembers().OrderBy(member => member == (party.WatchedRole is { } role ? party.Get(role) : party.Player) ? 0 : 1);
}
