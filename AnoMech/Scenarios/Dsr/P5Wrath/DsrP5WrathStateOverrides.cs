using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P5Wrath;
public sealed class DsrP5WrathStateOverrides
{
    public float? DragonBearing { get; set; }
    public bool? MageOnDragonSide { get; set; }
    public IReadOnlyList<PartyRole>? Tethered { get; set; }
    public PartyRole? Defamation { get; set; }
    public PartyRole? Divebomb { get; set; }
    public IReadOnlyList<PartyRole>? Lightning { get; set; }
    public PartyRole? LiquidHeaven { get; set; }
    public PartyRole? Eruptions { get; set; }
}
