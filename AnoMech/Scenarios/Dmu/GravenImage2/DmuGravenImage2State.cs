using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dmu.GravenImage2;

public sealed class DmuGravenImage2Objects
{
    public SimEnemy? Kefka { get; set; }
    // Where each set's Gravitas puddles landed, local XZ.
    public Vector2? SouthPuddles { get; set; }
    public Vector2? NorthPuddles { get; set; }
}

// Per set, one role group is tethered to the east statue (Vitrophyre, spread) and the other to
// the middle one (Gravitas, stack then centre); the sets are rolled apart.
public sealed class DmuGravenImage2State
{
    public static readonly PartyRole[] Supports = [MainTank, OffTank, RegenHealer, ShieldHealer];
    public static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    public DmuGravenImage2Objects Objects { get; } = new();
    public IceRoll Ice { get; }
    public bool DpsSpreadFirst { get; }
    public bool DpsSpreadSecond { get; }
    public bool FirstCleaveEast { get; }
    public bool SecondCleaveEast { get; }
    public PartyRole TrapSupport { get; }
    public PartyRole TrapDps { get; }
    public IReadOnlyList<PartyRole> TrapPassOrder { get; }

    public DmuGravenImage2State(Rng rng, DmuGravenImage2StateOverrides overrides)
    {
        Ice = new(overrides.IceLie ?? rng.NextBool(), rng.NextBool());
        DpsSpreadFirst = overrides.DpsSpreadFirst ?? rng.NextBool();
        DpsSpreadSecond = overrides.DpsSpreadSecond ?? rng.NextBool();
        FirstCleaveEast = overrides.FirstCleaveEast ?? rng.NextBool();
        SecondCleaveEast = overrides.SecondCleaveEast ?? rng.NextBool();
        TrapSupport = rng.NextSupportRole();
        TrapDps = rng.NextDpsRole();
        TrapPassOrder = rng.Shuffle(PerRole.All).ToList();
        DiagnosticLog.Info($"[DmuGravenImage2] Roll: ice={Ice} spread1={(DpsSpreadFirst ? "DPS" : "supports")} spread2={(DpsSpreadSecond ? "DPS" : "supports")} "
                           + $"cleave1={(FirstCleaveEast ? "east" : "west")} cleave2={(SecondCleaveEast ? "east" : "west")} trap={TrapSupport}+{TrapDps}");
    }

    private DmuGravenImage2State(bool[] f, PartyRole[] roles)
    {
        Ice = new(f[0], f[1]);
        DpsSpreadFirst = f[2];
        DpsSpreadSecond = f[3];
        FirstCleaveEast = f[4];
        SecondCleaveEast = f[5];
        TrapSupport = roles[0];
        TrapDps = roles[1];
        TrapPassOrder = roles[2..];
    }

    public bool SpreadsInSet(PartyRole role, int set) => role.IsDps() == (set == 1 ? DpsSpreadFirst : DpsSpreadSecond);

    public bool[] EncodeFlags() => [Ice.Lie, Ice.ShownSouthwestNortheast, DpsSpreadFirst, DpsSpreadSecond, FirstCleaveEast, SecondCleaveEast];

    public PartyRole[] EncodeRoles() => [TrapSupport, TrapDps, .. TrapPassOrder];

    public static DmuGravenImage2State? FromNetworkReplay(bool[] flags, PartyRole[] roles)
    {
        if (flags.Length != 6 || roles.Length != 10) return null;
        if (roles.Any(r => (int)r is < 0 or > 7) || roles.Skip(2).Distinct().Count() != 8) return null;
        return new(flags, roles);
    }
}

public sealed class DmuGravenImage2StateOverrides
{
    // Null = random.
    public bool? IceLie { get; set; }
    public bool? DpsSpreadFirst { get; set; }
    public bool? DpsSpreadSecond { get; set; }
    public bool? FirstCleaveEast { get; set; }
    public bool? SecondCleaveEast { get; set; }
}
