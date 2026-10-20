using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Scenarios.Dsr.P3Nidhogg.DsrP3NidhoggActions;

namespace AnoMech.Scenarios.Dsr.P3Nidhogg;

public sealed class DsrP3NidhoggScenario : IScenario
{
    public string Name => "Nidhogg — Easthogg (Elemental)";
    public string DisplayNameOverride => "P3 Dive from Grace";
    public bool IsIncomplete => true;
    public bool EnablePhaseBgm => true;
    public IPhase Phase => DsrZone.P3;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP3NidhoggAi()];
    private readonly DsrP3NidhoggSettingsWindow settings = new();
    public object SettingsOverrides => settings.Overrides;
    public void DrawSettings() => settings.Draw();
    public DsrP3NidhoggState? LastState { get; private set; }
    public bool Completed { get; private set; }
    private SimWorld world = null!;
    private DsrP3NidhoggState state = null!;
    private SimEnemy boss = null!;
    private SimEnemy helper = null!;
    private readonly List<SimEnemy> actors = [];
    private readonly Queue<SimEnemy> waiting = [];
    private readonly List<SimEnemy>[] divers = [[], [], []];
    private readonly List<SimEnemy> fourTowers = [];
    private readonly SimTether?[] tethers = new SimTether?[2];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = LastState = new(world.Rng, settings.Overrides, world.Party.PlayerRole);
        Completed = false;
        actors.Clear(); waiting.Clear(); fourTowers.Clear(); Array.Clear(tethers);
        foreach (var group in divers) group.Clear();
        world.Events.Add(0, SpawnOpening);
        world.Events.Add(6.96f, () => boss.Cast(FinalChorus));
        world.Events.Add(10f, Engage);
        foreach (var at in new[] { 14.10f, 17.22f, 20.35f }) world.Events.Add(at, Auto);
        world.Events.Add(22.45f, MarkNumbers);
        world.Events.Add(22.45f, () => { boss.SetTarget(null, false); boss.Cast(DiveFromGrace); });
        world.Events.Add(28.15f, MarkArrows);
        world.Events.Add(29.60f, () => Windup(0));
        world.Events.Add(37.28f, () => DiveAt(1));
        world.Events.Add(37.45f, () => boss.Cast(Eye, world.Party.Get(state.EyeTargets[0])));
        world.Events.Add(40.89f, () => Wheel(state.LashFirst[0]));
        world.Events.Add(41.38f, () => CastDiveTowers(1));
        world.Events.Add(44.01f, () => Wheel(!state.LashFirst[0]));
        world.Events.Add(46.51f, () => CastLines(divers[0]));
        world.Events.Add(47.27f, () => DiveAt(2));
        world.Events.Add(51.15f, () => Windup(1));
        world.Events.Add(51.38f, () => CastDiveTowers(2));
        world.Events.Add(56.50f, () => CastLines(divers[1]));
        world.Events.Add(58.29f, () => DiveAt(3));
        world.Events.Add(59.01f, () => boss.Cast(Eye, world.Party.Get(state.EyeTargets[1])));
        world.Events.Add(62.40f, () => Wheel(state.LashFirst[1]));
        world.Events.Add(62.40f, () => CastDiveTowers(3));
        world.Events.Add(65.52f, () => Wheel(!state.LashFirst[1]));
        world.Events.Add(67.48f, () => CastLines(divers[2]));
        world.Events.Add(69.60f, ClearNumbers);
        world.Events.Add(69.60f, Engage);
        foreach (var at in new[] { 69.67f, 72.78f }) world.Events.Add(at, Auto);
        world.Events.Add(74.03f, () => CastLance(false));
        world.Events.Add(78f, SpawnFourTowers);
        world.Events.Add(79.06f, CastFourTowers);
        world.Events.Add(83.96f, SoulTethers);
        world.Events.Add(86.68f, () => CastLines(fourTowers.Where((_, i) => i != state.QuietTower)));
        world.Events.Add(91.10f, ResolveTethers);
        foreach (var at in new[] { 96.23f, 99.35f, 102.47f, 105.59f, 108.71f }) world.Events.Add(at, Auto);
        world.Events.Add(111.97f, () => CastLance(true));
        world.Events.Add(116.97f, () => boss.Cast(29750));
        world.Events.Add(127.90f, () =>
        {
            foreach (var actor in actors) actor.Despawn();
            actors.Clear(); Completed = true;
        });
        if (selectedAi is { } index) ((IScenarioAi<DsrP3NidhoggState>)AiStrats[index]).Run(state, world);
    }

    public void Tick(float delta, float elapsed) { }

    private SimEnemy Spawn(uint id, Vector3 at, bool targetable = false)
    {
        var actor = world.SpawnEnemy(new(id, 3458, Level: 90, Targetable: false,
            EnemyList: targetable ? EnemyListMode.Always : EnemyListMode.Never,
            Visibility: id == 9020 ? SpawnVisibility.InvisibleHelper : SpawnVisibility.Visible, Placement: new(at, 0)))
            ?? throw new InvalidOperationException($"Could not spawn DSR actor {id}.");
        actors.Add(actor);
        return actor;
    }

    private void SpawnOpening()
    {
        boss = Spawn(12605, Vector3.Zero, true);
        boss.SetTargetable(false);
        helper = Spawn(9020, Vector3.Zero);
        helper.SetModelHidden(true);
        for (var i = 0; i < 8; i++)
        {
            var actor = Spawn(12606, new(23, 0, 4));
            actor.SetModelHidden(true); waiting.Enqueue(actor);
        }
    }

    private void Engage() { boss.SetTargetable(true); boss.SetTarget(world.Party.Get(PartyRole.MainTank), false); }
    private void Auto() => boss.Cast(AutoAttack, world.Party.Get(PartyRole.MainTank));

    private void MarkNumbers()
    {
        foreach (var (role, line) in state.Lines)
        {
            world.Party.Get(role)?.AddStatus((ushort)(3003 + line));
            world.Party.Get(role)?.AttachLockonVfx((uint)(318 + line));
        }
    }

    private void MarkArrows()
    {
        foreach (var (role, arrow) in state.Arrows)
            world.Party.Get(role)?.AddStatus((ushort)(arrow switch { DiveArrow.Up => 2756, DiveArrow.Down => 2757, _ => 2755 }),
                new[] { 9f, 19f, 30f }[state.Lines[role] - 1]);
    }

    private void ClearNumbers()
    {
        foreach (var member in world.Party.ActiveMembers())
            for (ushort id = 3004; id <= 3006; id++) member.RemoveStatus(id);
    }

    private void Windup(int index) => boss.Cast(state.LashFirst[index] ? 26387u : 26386u);
    private void Wheel(bool lash) => boss.Cast(lash ? LashingWheel : GnashingWheel);

    private void DiveAt(int line)
    {
        foreach (var role in state.Line(line))
        {
            var target = world.Party.Get(role)!;
            var actor = waiting.Dequeue();
            actor.SetPosition(target.Placement()); actor.SetModelHidden(false);
            var facing = new Vector3(MathF.Sin(target.Rotation), 0, MathF.Cos(target.Rotation));
            var tower = target.Position + facing * DsrP3NidhoggState.DiveOffset(state.Arrows[role]);
            state.DiveTowers[line - 1].Add((role, tower));
            divers[line - 1].Add(actor);
            actor.Cast(Dive(state.Arrows[role]), target);
            world.Events.Add(.57f, () => actor.SetModelHidden(true));
        }
    }

    private EnemyAction Tower(uint id, int needed) => new(id)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects = [Resolve(ctx =>
            {
                if (ctx.Hits.Count != needed) helper.Cast(TowerFailure);
            }), Damage(VulnerableTo(FireResistanceDown)), ApplyStatus(FireResistanceDown, 11.96f), ApplyStatus(PhysicalVulnerability, 7f)],
        Timing = new() { DamageDelay = 0 },
    };

    private void CastDiveTowers(int line)
    {
        for (var i = 0; i < divers[line - 1].Count; i++)
        {
            var actor = divers[line - 1][i];
            actor.SetPosition(new Placement(state.DiveTowers[line - 1][i].Spot, 0).Face(Vector3.Zero));
            actor.Cast(Tower(26385, 1));
            world.Events.Add(2.85f, () => actor.SetModelHidden(false));
        }
    }

    private void CastLines(IEnumerable<SimEnemy> casters)
    {
        foreach (var actor in casters)
        {
            var closest = world.Party.ActiveMembers().Where(m => !m.Dead).MinBy(m => Vector3.DistanceSquared(m.Position, actor.Position));
            if (closest == null) continue;
            actor.SetRotation(actor.Placement().Face(closest.Position).Rotation);
            actor.Cast(Geirskogul);
            world.Events.Add(6.6f, () => actor.PlayActionTimeline(7747));
            world.Events.Add(7.3f, () => actor.SetModelHidden(true));
        }
    }

    private void CastLance(bool south)
    {
        boss.SetRotation(south ? 0 : boss.Placement().Face(world.Party.Get(state.DrachenlanceTarget)!.Position).Rotation);
        helper.SetPosition(boss.Placement());
        boss.Cast(26379);
        world.Events.Add(.09f, () => helper.Cast(Drachenlance));
    }

    private void SpawnFourTowers()
    {
        for (var i = 0; i < 4; i++)
        {
            var actor = Spawn(12606, DsrP3NidhoggState.FourTowerSpot(i));
            actor.SetModelHidden(true); fourTowers.Add(actor);
        }
    }

    private void CastFourTowers()
    {
        for (var i = 0; i < 4; i++)
        {
            var actor = fourTowers[i];
            actor.Cast(Tower((uint)(26390 + state.TowerSoakers[i]), state.TowerSoakers[i]));
            world.Events.Add(5.35f, () => actor.SetModelHidden(false));
        }
    }

    private void SoulTethers()
    {
        tethers[0] = world.Tether(boss, End.Passable(world.Party.Get(state.SoulTetherTargets[0]), exclusive: false), 84);
        tethers[1] = world.Tether(fourTowers[state.QuietTower], End.Passable(world.Party.Get(state.SoulTetherTargets[1]), exclusive: false), 84);
    }

    private void ResolveTethers()
    {
        foreach (var tether in tethers)
        {
            var holder = tether?.B;
            var source = tether?.A as SimEnemy;
            tether?.Despawn();
            if (holder != null) source?.Cast(SoulTether, holder);
        }
    }
}
