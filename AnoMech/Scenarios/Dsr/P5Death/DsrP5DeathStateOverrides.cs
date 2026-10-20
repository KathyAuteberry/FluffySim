using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P5Death;
public sealed class DsrP5DeathStateOverrides
{
    public float? WarriorBearing { get; set; }
    public IReadOnlyCollection<PartyRole>? Dooms { get; set; }
    public float? ThordanBearing { get; set; }
    public float? EyeBearing { get; set; }
    public bool? FirstDoomSquare { get; set; }
    public int? PlayerCongaSlot { get; set; }
}
