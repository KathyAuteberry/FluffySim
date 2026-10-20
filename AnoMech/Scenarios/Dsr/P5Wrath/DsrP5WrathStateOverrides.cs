using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P5Wrath;
public enum WrathPracticeAssignment { Random, BlueMarker, DragoonTether, GreenMarker, Thunderstruck, EruptionBait, LiquidHeavenBait, NoEarlyMarker }

public sealed class DsrP5WrathStateOverrides
{
    public WrathPracticeAssignment PracticeAssignment { get; set; }
    public float? DragonBearing { get; set; }
    public bool? MageOnDragonSide { get; set; }
    public IReadOnlyList<PartyRole>? Tethered { get; set; }
    public PartyRole? Defamation { get; set; }
    public PartyRole? Divebomb { get; set; }
    public IReadOnlyList<PartyRole>? Lightning { get; set; }
    public PartyRole? LiquidHeaven { get; set; }
    public PartyRole? Eruptions { get; set; }
}
