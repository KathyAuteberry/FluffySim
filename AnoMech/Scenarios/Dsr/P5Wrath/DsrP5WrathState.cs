using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.Geometry;
using static AnoMech.Scenarios.Dsr.DsrP5Constants;

namespace AnoMech.Scenarios.Dsr.P5Wrath;

public sealed class DsrP5WrathState
{
    public readonly List<WrathTwister> Twisters = [];

    public const float KnightRadius = 23f;
    public const float DragoonOffset = 30f;
    public const float CasterRadius = 9f;

    public float DragonBearing { get; }
    public bool MageOnDragonSide { get; }
    public IReadOnlyList<PartyRole> Tethered { get; }
    public PartyRole Defamation { get; }
    public PartyRole Divebomb { get; }
    public IReadOnlyList<PartyRole> Lightning { get; }
    public PartyRole LiquidHeaven { get; }
    public PartyRole Eruptions { get; }

    public DsrP5WrathState(Rng rng, DsrP5WrathStateOverrides? overrides = null, PartyRole player = PartyRole.MainTank)
    {
        overrides ??= new DsrP5WrathStateOverrides();
        DragonBearing = overrides.DragonBearing ?? 90f * rng.Next(4);
        MageOnDragonSide = overrides.MageOnDragonSide ?? rng.NextBool();
        var order = rng.Shuffle(Enum.GetValues<PartyRole>()).ToList();
        Tethered = overrides.Tethered ?? order.Take(2).ToList();
        Defamation = overrides.Defamation ?? order.Except(Tethered).First();
        Divebomb = overrides.Divebomb ?? order.Except(Tethered).Where(r => r != Defamation).First();
        var lightning = rng.Shuffle(Enum.GetValues<PartyRole>()).Take(2).ToList();
        Lightning = overrides.Lightning ?? lightning;
        var plain = Plain.Except(Lightning).ToList();
        var liquid = plain[rng.Next(plain.Count)];
        LiquidHeaven = overrides.LiquidHeaven ?? liquid;
        var marked = Marked.Append(Divebomb).Except(Lightning).ToList();
        var eruptions = marked[rng.Next(marked.Count)];
        Eruptions = overrides.Eruptions ?? eruptions;
        if (overrides.PracticeAssignment != WrathPracticeAssignment.Random)
        {
            var eligible = overrides.PracticeAssignment switch
            {
                WrathPracticeAssignment.BlueMarker => new[] { Defamation },
                WrathPracticeAssignment.DragoonTether => Tethered.ToArray(),
                WrathPracticeAssignment.GreenMarker => new[] { Divebomb },
                WrathPracticeAssignment.Thunderstruck => Lightning.ToArray(),
                WrathPracticeAssignment.EruptionBait => new[] { Eruptions },
                WrathPracticeAssignment.LiquidHeavenBait => new[] { LiquidHeaven },
                WrathPracticeAssignment.NoEarlyMarker => Enum.GetValues<PartyRole>().Except(Marked).ToArray(),
                _ => throw new ArgumentOutOfRangeException(nameof(overrides.PracticeAssignment)),
            };
            var selected = eligible[rng.Next(eligible.Length)];
            PartyRole Swap(PartyRole role) => role == selected ? player : role == player ? selected : role;
            Tethered = Tethered.Select(Swap).ToArray();
            Defamation = Swap(Defamation);
            Divebomb = Swap(Divebomb);
            Lightning = Lightning.Select(Swap).ToArray();
            LiquidHeaven = Swap(LiquidHeaven);
            Eruptions = Swap(Eruptions);
        }
    }
    public IEnumerable<PartyRole> Marked => Tethered.Append(Defamation).OrderBy(r => r);
    public IEnumerable<PartyRole> Plain => Enum.GetValues<PartyRole>().Where(r => !Tethered.Contains(r) && r != Defamation && r != Divebomb);

    public float WarriorBearing => MageOnDragonSide ? DragonBearing + 180f : DragonBearing;
    public float MageBearing => WarriorBearing + 180f;

    public Vector3 DragonSpot => AtBearing(DragonBearing, KnightRadius);
    public Vector3 DragoonSpot(int dragoon) => AtBearing(DragonBearing + (dragoon == 0 ? DragoonOffset : -DragoonOffset), KnightRadius);
    public Vector3 WarriorSpot => AtBearing(WarriorBearing, CasterRadius);
    public Vector3 MageSpot => AtBearing(MageBearing, CasterRadius);
}

public sealed record WrathTwister(PartyRole Owner, Vector3 Position)
{
    public const float CollisionRadius = 1.5f;
    public const float Activation = 46.70f;
    public const float Expiration = 50.90f;
    public TimedCircle Hazard => new(new(Position.X, Position.Z), CollisionRadius, Activation, Expiration);
}
