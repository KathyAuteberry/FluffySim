using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P3Nidhogg;

public sealed class DsrP3NidhoggStateOverrides
{
    public IReadOnlyDictionary<PartyRole, int>? Lines { get; set; }
    public IReadOnlyDictionary<PartyRole, DiveArrow>? Arrows { get; set; }
    public IReadOnlyList<bool>? LashFirst { get; set; }
    public IReadOnlyList<int>? TowerSoakers { get; set; }
    public int? QuietTower { get; set; }
    public IReadOnlyList<PartyRole>? SoulTetherTargets { get; set; }
    public int? PlayerLine { get; set; }
    public DiveArrow? PlayerArrow { get; set; }
}
