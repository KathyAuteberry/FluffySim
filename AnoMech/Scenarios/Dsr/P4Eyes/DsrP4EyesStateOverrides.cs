using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P4Eyes;
public sealed class DsrP4EyesStateOverrides
{
    public PartyRole? SoulMarked { get; set; }
    public IReadOnlyCollection<PartyRole>? FirstRed { get; set; }
    public IReadOnlyList<(int, int)>? DivePicks { get; set; }
}
