using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Fru.LightRampant;

public sealed class FruLightRampantAi : IScenarioAi<FruLightRampantState>
{
    public string Name => "Mana 4:4 East adjust";
    public string Group => "JP";
    public static IReadOnlyList<PartyRole> Lineup { get; } =
        [RegenHealer, ShieldHealer, MainTank, OffTank, CasterDps, PhysRangedDps, MeleeDpsB, MeleeDpsA];
    private FruLightRampantState state = null!;
    private SimWorld world = null!;
    private readonly Dictionary<PartyRole, int> towerFor = [];
    private PartyRole northPuddle;
    private PartyRole southPuddle;

    public void Run(FruLightRampantState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        towerFor.Clear();
        world.Events.Add(0f, Preposition);
        world.Events.Add(9.1f, AssignTowersAndPuddles);
        world.Events.Add(15.91f, () => MovePuddles(1));
        world.Events.Add(17.47f, () => MovePuddles(2));
        world.Events.Add(19.03f, () => MovePuddles(3));
        world.Events.Add(19.35f, () => MoveChainArc(0f));
        world.Events.Add(19.75f, () => MoveChainArc(1f / 3f));
        world.Events.Add(21.25f, () => MoveChainArc(2f / 3f));
        world.Events.Add(22.75f, () => MoveChainArc(1f));
        world.Events.Add(20.59f, () => MovePuddles(4));
        world.Events.Add(22.15f, () =>
        {
            Move(northPuddle, new(0f, 0f, -18f), 7.8f);
            Move(southPuddle, new(0f, 0f, 18f), 7.8f);
        });
        world.Events.Add(23.05f, MovePuddleCamps);
        world.Events.Add(27.40f, MoveSecondOrbCamps);
        world.Events.Add(30.45f, MoveCentralTower);
        world.Events.Add(33f, MoveNonSoakersToClocks);
        world.Events.Add(34.00f, MoveBanish);
        world.Events.Add(37.30f, MoveClocks);
    }

    public static Vector3 ClockPosition(PartyRole role) => role switch
    {
        MainTank => new(0f, 0f, -8f), OffTank => new(0f, 0f, 8f),
        RegenHealer => new(-8f, 0f, 0f), ShieldHealer => new(8f, 0f, 0f),
        MeleeDpsA => new(-5.657f, 0f, 5.657f), MeleeDpsB => new(5.657f, 0f, 5.657f),
        PhysRangedDps => new(-5.657f, 0f, -5.657f), CasterDps => new(5.657f, 0f, -5.657f),
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static Vector3 PairPosition(PartyRole role) => ClockPosition(role switch
    {
        MainTank => PhysRangedDps, OffTank => MeleeDpsB,
        RegenHealer => MeleeDpsA, ShieldHealer => CasterDps, _ => role,
    });

    public static IReadOnlyDictionary<PartyRole, int> TowerAssignments(IEnumerable<PartyRole> puddleTargets) =>
        Lineup.Where(r => !puddleTargets.Contains(r)).Select((r, i) => (r, i)).ToDictionary(x => x.r, x => x.i);

    private void Preposition()
    {
        Vector3[] positions =
        [
            new(-8f, 0f, -3f), new(-3f, 0f, -8f), new(3f, 0f, -8f), new(8f, 0f, -3f),
            new(8f, 0f, 3f), new(3f, 0f, 8f), new(-3f, 0f, 8f), new(-8f, 0f, 3f),
        ];
        for (var i = 0; i < 8; i++) Move(Lineup[i], positions[i]);
    }

    private void AssignTowersAndPuddles()
    {
        foreach (var (role, tower) in TowerAssignments(state.PuddleTargets))
        {
            towerFor[role] = tower;
            Move(role, FruLightRampantScenario.OuterTowers[tower]);
        }
        var north = state.PuddleTargets.Where(r => !r.IsDps()).ToArray();
        var south = state.PuddleTargets.Where(r => r.IsDps()).ToArray();
        if (north.Length == 1) { northPuddle = south[0]; southPuddle = north[0]; }
        else if (north.Length == 2)
        {
            northPuddle = Lineup.Last(r => north.Contains(r));
            southPuddle = north.Single(r => r != northPuddle);
        }
        else
        {
            southPuddle = Lineup.First(r => south.Contains(r));
            northPuddle = south.Single(r => r != southPuddle);
        }
        foreach (var role in state.PuddleTargets) (world.Party.Get(role) as ISimPartyMember)?.UseSprint(14f);
        MovePuddles(0);
    }

    private void MovePuddles(int drop)
    {
        float[] angles = [-135f, -135f, -90f, -55f, -23f];
        float[] radii = [4f, 10.5f, 12f, 12f, 10f];
        var angle = angles[drop] * MathF.PI / 180f;
        var radius = radii[drop];
        var position = new Vector3(MathF.Sin(angle) * radius, 0f, -MathF.Cos(angle) * radius);
        Move(northPuddle, position, 7.8f);
        Move(southPuddle, -position, 7.8f);
    }

    private Vector3 Camp(bool north, bool first)
    {
        var centerDangerous = north == (state.NorthOrbFirst == first);
        var angle = (centerDangerous ? 40f : 20f) * MathF.PI / 180f;
        var position = new Vector3(MathF.Sin(angle) * 18f, 0f, -MathF.Cos(angle) * 18f);
        return north ? position : -position;
    }

    private void MoveChainArc(float fraction)
    {
        foreach (var (role, tower) in towerFor)
        {
            var start = FruLightRampantScenario.OuterTowers[tower];
            var end = Camp(tower % 2 == 0, first: true);
            var angle = MathF.Atan2(start.X, -start.Z);
            var delta = MathF.IEEERemainder(MathF.Atan2(end.X, -end.Z) - angle, 2f * MathF.PI);
            angle += delta * fraction;
            var radius = fraction == 1f ? 18f : 19.5f;
            Move(role, new(MathF.Sin(angle) * radius, 0f, -MathF.Cos(angle) * radius));
        }
    }

    private void MovePuddleCamps()
    {
        Move(northPuddle, Camp(true, first: true), 7.8f);
        Move(southPuddle, Camp(false, first: true), 7.8f);
    }

    private void MoveSecondOrbCamps()
    {
        foreach (var (role, tower) in towerFor) Move(role, Camp(tower % 2 == 0, first: false));
        Move(northPuddle, Camp(true, first: false));
        Move(southPuddle, Camp(false, first: false));
    }

    private void MoveCentralTower()
    {
        for (var i = 0; i < 8; i++)
        {
            var role = (PartyRole)i;
            var stacks = world.Party.Get(role)?.FindStatus(FruConstants.StatusId.Lightsteeped)?.Stacks ?? 0;
            if (FruLightRampantRules.MustSoakCenter(stacks)) Move(role, Vector3.Zero);
        }
    }

    private void MoveNonSoakersToClocks()
    {
        for (var i = 0; i < 8; i++)
        {
            var role = (PartyRole)i;
            var stacks = world.Party.Get(role)?.FindStatus(FruConstants.StatusId.Lightsteeped)?.Stacks ?? 0;
            if (!FruLightRampantRules.MustSoakCenter(stacks)) Move(role, ClockPosition(role), 7.8f);
        }
    }

    private void MoveBanish()
    {
        for (var i = 0; i < 8; i++) Move((PartyRole)i, state.Spread ? ClockPosition((PartyRole)i) : PairPosition((PartyRole)i), 7.8f);
    }

    private void MoveClocks()
    {
        for (var i = 0; i < 8; i++) Move((PartyRole)i, ClockPosition((PartyRole)i));
    }

    private void Move(PartyRole role, Vector3 position, float speed = 6f) => world.Party.Get(role)?.MoveTo(position, speed);
}
