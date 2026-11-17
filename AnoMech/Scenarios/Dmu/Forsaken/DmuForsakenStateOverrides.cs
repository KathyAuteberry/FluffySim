using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dmu.Forsaken;

// Null leaves a roll random.
public sealed class DmuForsakenStateOverrides
{
    public EndAttack?[] EndAttacks { get; } = new EndAttack?[4];
    public Direction? NewNorth { get; set; }
    public uint? SupportLockon { get; set; }
    public PartyRole? SupportStackRole { get; set; }
    public PartyRole? DpsStackRole { get; set; }
    public int? Rotation { get; set; }
    public bool? FinalConeMarksSwapped { get; set; }
    public bool? FinalAoeMarksSwapped { get; set; }
    // false shuffles each tower's new lockons; true deals them to its soakers in role order.
    public bool ReassignLockonsInRoleOrder { get; set; }
}
