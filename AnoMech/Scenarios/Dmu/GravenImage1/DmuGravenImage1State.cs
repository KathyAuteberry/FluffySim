using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dmu.GravenImage1;

public sealed class DmuGravenImage1Objects
{
    public SimEnemy? Kefka { get; set; }
}

public sealed class DmuGravenImage1State
{
    public static readonly PartyRole[] Supports = [MainTank, OffTank, RegenHealer, ShieldHealer];
    public static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    public DmuGravenImage1Objects Objects { get; } = new();
    public bool DpsTethered { get; }
    public IceRoll Ice1 { get; }
    public FireRoll Fire1 { get; }
    public IReadOnlyList<PartyRole> WaveCannonTargets { get; }
    public PartyRole TrapSupport { get; }
    public PartyRole TrapDps { get; }
    public IReadOnlyList<PartyRole> TrapPassOrder { get; }
    public IceRoll Ice2 { get; }
    public ThunderRoll Thunder2 { get; }

    public DmuGravenImage1State(Rng rng, DmuGravenImage1StateOverrides overrides)
    {
        DpsTethered = overrides.DpsTethered ?? rng.NextBool();
        Ice1 = new(overrides.Ice1Lie ?? rng.NextBool(), rng.NextBool());
        var stackSupport = rng.NextSupportRole();
        var stackDps = rng.NextDpsRole();
        Fire1 = new(overrides.Fire1Lie ?? rng.NextBool(), overrides.Fire1StackMarkers ?? rng.NextBool(), stackSupport, stackDps);
        WaveCannonTargets = [.. rng.Shuffle(Supports).Take(2), .. rng.Shuffle(Dps).Take(2)];
        TrapSupport = rng.NextSupportRole();
        TrapDps = rng.NextDpsRole();
        TrapPassOrder = rng.Shuffle(PerRole.All).ToList();
        Ice2 = new(overrides.Ice2Lie ?? rng.NextBool(), rng.NextBool());
        Thunder2 = new(overrides.Thunder2Lie ?? rng.NextBool(), rng.NextBool(), rng.NextBool());
        DiagnosticLog.Info($"[DmuGravenImage1] Roll: tether={(DpsTethered ? "DPS" : "supports")} ice1={Ice1} fire1={Fire1} "
                           + $"waveCannon=[{string.Join(",", WaveCannonTargets)}] trap={TrapSupport}+{TrapDps} ice2={Ice2} thunder2={Thunder2}");
    }

    private DmuGravenImage1State(bool dpsTethered, IceRoll ice1, FireRoll fire1, PartyRole[] waveCannon, PartyRole trapSupport, PartyRole trapDps,
        PartyRole[] passOrder, IceRoll ice2, ThunderRoll thunder2)
    {
        DpsTethered = dpsTethered;
        Ice1 = ice1;
        Fire1 = fire1;
        WaveCannonTargets = waveCannon;
        TrapSupport = trapSupport;
        TrapDps = trapDps;
        TrapPassOrder = passOrder;
        Ice2 = ice2;
        Thunder2 = thunder2;
    }

    public bool[] EncodeFlags() =>
    [
        DpsTethered, Ice1.Lie, Ice1.ShownSouthwestNortheast, Fire1.Lie, Fire1.StackMarkers,
        Ice2.Lie, Ice2.ShownSouthwestNortheast, Thunder2.Lie, Thunder2.RunsSoutheast, Thunder2.ShownFirstPair,
    ];

    public PartyRole[] EncodeRoles() => [Fire1.StackSupport, Fire1.StackDps, TrapSupport, TrapDps, .. WaveCannonTargets, .. TrapPassOrder];

    public static DmuGravenImage1State? FromNetworkReplay(bool[] flags, PartyRole[] roles)
    {
        if (flags.Length != 10 || roles.Length != 16) return null;
        if (roles.Any(r => (int)r is < 0 or > 7) || roles.Skip(8).Distinct().Count() != 8) return null;
        return new(flags[0], new(flags[1], flags[2]), new(flags[3], flags[4], roles[0], roles[1]), roles[4..8], roles[2], roles[3],
            roles[8..], new(flags[5], flags[6]), new(flags[7], flags[8], flags[9]));
    }
}

public sealed class DmuGravenImage1StateOverrides
{
    // Null = random.
    public bool? DpsTethered { get; set; }
    public bool? Ice1Lie { get; set; }
    public bool? Fire1Lie { get; set; }
    public bool? Fire1StackMarkers { get; set; }
    public bool? Ice2Lie { get; set; }
    public bool? Thunder2Lie { get; set; }
}
