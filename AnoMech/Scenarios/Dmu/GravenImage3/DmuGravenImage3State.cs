using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dmu.GravenImage3;

public enum Arrow { North, South, East, West }

public sealed class DmuGravenImage3Objects
{
    public SimEnemy? Kefka { get; set; }
}

// A player's two Tele-portents: one group gets the same arrow twice, the other an arrow and the same
// arrow turned 90° clockwise. FirstExpiresFirst says which of the two is the 7s one.
public sealed record ArrowPair(Arrow First, Arrow Second, bool FirstExpiresFirst)
{
    public bool Identical => First == Second;
    public Arrow Early => FirstExpiresFirst ? First : Second;
    public Arrow Late => FirstExpiresFirst ? Second : First;
}

public sealed class DmuGravenImage3State
{
    public static readonly PartyRole[] Supports = [MainTank, OffTank, RegenHealer, ShieldHealer];
    public static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly Arrow[] Arrows = [Arrow.North, Arrow.South, Arrow.East, Arrow.West];

    public DmuGravenImage3Objects Objects { get; } = new();
    public bool DpsIdentical { get; }
    public IReadOnlyDictionary<PartyRole, ArrowPair> Arrows8 { get; }
    public bool DpsConfused { get; }
    public bool AveMaria { get; }
    public FireRoll Fire { get; }
    public ThunderRoll Thunder { get; }
    public PartyRole TrapSupport { get; }
    public PartyRole TrapDps { get; }

    public DmuGravenImage3State(Rng rng, DmuGravenImage3StateOverrides overrides)
    {
        DpsIdentical = overrides.DpsIdentical ?? rng.NextBool();
        var arrows = new Dictionary<PartyRole, ArrowPair>();
        foreach (var group in new[] { Supports, Dps })
        {
            var identical = group[0].IsDps() == DpsIdentical;
            var firsts = rng.Shuffle(Arrows).ToList();
            for (var i = 0; i < group.Length; i++)
                arrows[group[i]] = new(firsts[i], identical ? firsts[i] : Clockwise(firsts[i]), rng.NextBool());
        }
        Arrows8 = arrows;
        DpsConfused = overrides.DpsConfused ?? rng.NextBool();
        AveMaria = overrides.AveMaria ?? rng.NextBool();
        Fire = new(overrides.FireLie ?? rng.NextBool(), overrides.FireStackMarkers ?? rng.NextBool(), rng.NextSupportRole(), rng.NextDpsRole());
        Thunder = new(overrides.ThunderLie ?? rng.NextBool(), rng.NextBool(), rng.NextBool());
        TrapSupport = rng.NextSupportRole();
        TrapDps = rng.NextDpsRole();
        DiagnosticLog.Info($"[DmuGravenImage3] Roll: identical={(DpsIdentical ? "DPS" : "supports")} confused={(DpsConfused ? "DPS" : "supports")} "
                           + $"gaze={(AveMaria ? "Ave Maria" : "Indolent Will")} fire={Fire} thunder={Thunder} trap={TrapSupport}+{TrapDps} "
                           + string.Join(" ", arrows.Select(kv => $"{kv.Key}={kv.Value.First}/{kv.Value.Second}{(kv.Value.FirstExpiresFirst ? "" : "*")}")));
    }

    private DmuGravenImage3State(bool[] f, PartyRole[] roles, int[] arrows)
    {
        DpsIdentical = f[0];
        DpsConfused = f[1];
        AveMaria = f[2];
        Fire = new(f[3], f[4], roles[0], roles[1]);
        Thunder = new(f[5], f[6], f[7]);
        TrapSupport = roles[2];
        TrapDps = roles[3];
        Arrows8 = PerRole.All.ToDictionary(r => r, r => new ArrowPair((Arrow)arrows[3 * (int)r], (Arrow)arrows[3 * (int)r + 1], arrows[3 * (int)r + 2] != 0));
    }

    public static Arrow Clockwise(Arrow a) => a switch
    {
        Arrow.North => Arrow.East, Arrow.East => Arrow.South, Arrow.South => Arrow.West, _ => Arrow.North,
    };

    public static Vector2 Direction(Arrow a) => a switch
    {
        Arrow.North => new(0f, -1f), Arrow.South => new(0f, 1f), Arrow.East => new(1f, 0f), _ => new(-1f, 0f),
    };

    // EObj facing: 0 = south, π = north.
    public static float Facing(Arrow a) => a switch
    {
        Arrow.North => float.Pi, Arrow.South => 0f, Arrow.East => float.Pi / 2f, _ => -float.Pi / 2f,
    };

    public static int Index(Arrow a) => (int)a;

    // Yan's box (local XZ): a pair of the same arrow goes to the red box it flows from, a turned pair to the
    // quadrant its two arrows name, each arrow on the blue spot drawn with it.
    public static (Vector2 Early, Vector2 Late) DropSpots(ArrowPair pair)
    {
        if (pair.Identical)
        {
            var (a, b) = pair.First switch
            {
                Arrow.South => (new Vector2(-13f, -13f), new Vector2(-13f, -7f)),
                Arrow.West => (new Vector2(13f, -13f), new Vector2(6f, -13f)),
                Arrow.North => (new Vector2(13f, 12.5f), new Vector2(13f, 5.5f)),
                _ => (new Vector2(-13f, 12.5f), new Vector2(-6f, 12.5f)),
            };
            return (a, b);
        }
        return (BlueSpot(pair.Early, pair), BlueSpot(pair.Late, pair));
    }

    private static Vector2 BlueSpot(Arrow arrow, ArrowPair pair) => (pair.First, arrow) switch
    {
        (Arrow.West, Arrow.West) => new(-6f, -13f), (Arrow.West, _) => new(-6f, -7f),
        (Arrow.North, Arrow.East) => new(6f, -6.5f), (Arrow.North, _) => new(13f, -6.5f),
        (Arrow.East, Arrow.South) => new(6f, 5.5f), (Arrow.East, _) => new(6f, 12.5f),
        (Arrow.South, Arrow.South) => new(-13f, 6f), _ => new(-6f, 6f),
    };

    public bool[] EncodeFlags() => [DpsIdentical, DpsConfused, AveMaria, Fire.Lie, Fire.StackMarkers, Thunder.Lie, Thunder.RunsSoutheast, Thunder.ShownFirstPair];

    public PartyRole[] EncodeRoles() => [Fire.StackSupport, Fire.StackDps, TrapSupport, TrapDps];

    public int[] EncodeArrows() => PerRole.All.SelectMany(r => new[] { (int)Arrows8[r].First, (int)Arrows8[r].Second, Arrows8[r].FirstExpiresFirst ? 1 : 0 }).ToArray();

    public static DmuGravenImage3State? FromNetworkReplay(bool[] flags, PartyRole[] roles, int[] arrows)
    {
        if (flags.Length != 8 || roles.Length != 4 || arrows.Length != 24) return null;
        if (roles.Any(r => (int)r is < 0 or > 7) || arrows.Any(a => a is < 0 or > 3)) return null;
        return new(flags, roles, arrows);
    }
}

public sealed class DmuGravenImage3StateOverrides
{
    // Null = random.
    public bool? DpsIdentical { get; set; }
    public bool? DpsConfused { get; set; }
    public bool? AveMaria { get; set; }
    public bool? FireLie { get; set; }
    public bool? FireStackMarkers { get; set; }
    public bool? ThunderLie { get; set; }
}
