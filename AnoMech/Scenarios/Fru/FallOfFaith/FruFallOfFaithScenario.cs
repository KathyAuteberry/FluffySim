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

namespace AnoMech.Scenarios.Fru.FallOfFaith;

public sealed record FaithCone(int Order, bool Fire, float Time, Vector3 Origin, float Rotation, PartyRole Target, IReadOnlyList<PartyRole> Hits);

public sealed class FruFallOfFaithScenario : IScenario
{
    public bool SupportsMultiplayer => true;
    public string Name => "Fall of Faith";
    public IPhase Phase => FruZone.P1;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruFallOfFaithAi()];
    public object SettingsOverrides => settings.Overrides;
    public void DrawSettings() => settings.Draw();
    public FruFallOfFaithState? LastState { get; private set; }
    public bool Completed { get; private set; }
    public bool IsRunning => active && sources.Any(a => a?.IsActive == true);
    public IReadOnlyList<SimEnemy> SourceActors => sources.OfType<SimEnemy>().Where(a => a.IsActive).ToArray();
    public int RevealedCount { get; private set; }
    public int ResolvedCount { get; private set; }
    public int Understacks { get; private set; }
    public IReadOnlyList<FaithCone> Cones => cones;
    public bool PracticeOutlines => settings.Overrides.PracticeOutlines;
    public static readonly Vector3[] Sources = [Vector3.Zero, new(-14.1421f, 0, -14.1421f), new(0, 0, -20), new(14.1421f, 0, -14.1421f)];
    private readonly FruFallOfFaithSettingsWindow settings = new();
    private readonly List<FaithCone> cones = [];
    private readonly List<SimEnemy> actors = [];
    private readonly SimEnemy?[] sources = new SimEnemy?[4];
    private readonly SimTether?[] tethers = new SimTether?[4];
    private readonly bool[] presentationStarted = new bool[4];
    private readonly SimEnemy[] helpers = new SimEnemy[16];
    private SimWorld world = null!;
    private FruFallOfFaithState state = null!;
    private bool active;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = LastState = new(world.Rng, settings.Overrides, world.Party.PlayerRole);
        Completed = false; active = true; RevealedCount = ResolvedCount = Understacks = 0;
        actors.Clear(); cones.Clear(); Array.Clear(sources); Array.Clear(tethers); Array.Clear(presentationStarted);
        world.Events.Add(0, () =>
        {
            sources[0] = Spawn(0x459B, 9707, Sources[0], true);
            for (var i = 0; i < helpers.Length; i++) helpers[i] = Spawn(0x233C, 0, Vector3.Zero, false);
        });
        world.Events.Add(3, () => Reveal(0));
        world.Events.Add(6.1f, () => Reveal(1));
        world.Events.Add(8.6f, () => Reveal(2));
        world.Events.Add(11.1f, () => Reveal(3));
        world.Events.Add(12.7f, () => Immobilize(0));
        world.Events.Add(14.77f, () => Charge(0));
        world.Events.Add(15.72f, () => Immobilize(1));
        world.Events.Add(16.1f, () => Impact(0));
        world.Events.Add(16.412f, () => LightningCones(0));
        world.Events.Add(17.8f, () => Charge(1));
        world.Events.Add(18.22f, () => Immobilize(2));
        world.Events.Add(19.13f, () => Impact(1));
        world.Events.Add(19.442f, () => LightningCones(1));
        world.Events.Add(20.30f, () => Charge(2));
        world.Events.Add(20.75f, () => Immobilize(3));
        world.Events.Add(21.62f, () => Impact(2));
        world.Events.Add(21.932f, () => LightningCones(2));
        world.Events.Add(22.8f, () => Charge(3));
        world.Events.Add(24.15f, () => Impact(3));
        world.Events.Add(24.462f, () => LightningCones(3));
        world.Events.Add(FruFallOfFaithTiming.End, Cleanup);
        if (selectedAi is { } index) ((IScenarioAi<FruFallOfFaithState>)AiStrats[index]).Run(state, world);
    }

    private SimEnemy Spawn(uint id, uint name, Vector3 position, bool visible)
    {
        var actor = world.SpawnEnemy(new EnemySpawnConfig(id, name, Level: 100, Targetable: id == 0x459B,
            EnemyList: id == 0x459B ? EnemyListMode.Always : EnemyListMode.Never, Visibility: visible ? SpawnVisibility.Visible : SpawnVisibility.InvisibleHelper,
            Placement: new(position, new Placement(position, MathF.PI).Face(Vector3.Zero).Rotation)))
            ?? throw new InvalidOperationException("Fall of Faith actor creation failed.");
        actors.Add(actor); return actor;
    }
    private void Reveal(int order)
    {
        if (order > 0) sources[order] = Spawn(0x459C, 9708, Sources[order], true);
        world.Party.Get(state.Targets[order])!.AddStatus(FruFallOfFaithActions.Prey, 0);
        RevealedCount = order + 1;
    }
    public void Tick(float delta, float elapsed)
    {
        if (!active) return;
        for (var i = 0; i < RevealedCount; i++)
        {
            if (presentationStarted[i] || sources[i] is not { IsActive: true, PacketSpawnPending: false, PacketSpawnFailed: false } source) continue;
            presentationStarted[i] = true;
            var target = world.Party.Get(state.Targets[i])!;
            tethers[i] = world.Tether(source, target, state.Fire(i) ? (ushort)249 : (ushort)287);
            var remaining = MathF.Max(.3f, FruFallOfFaithTiming.Reveal[i] + 9 - elapsed);
            source.Cast(new EnemyAction(state.Fire(i) ? 40137u : 40140u) { Cast = new() { CastSecondsOverride = remaining } });
        }
    }
    private void Immobilize(int order)
    {
        var target = world.Party.Get(state.Targets[order])!;
        target.StopMoving();
        target.AddStatusParam(FruFallOfFaithActions.Fetters, 0xC8, FruFallOfFaithTiming.FettersDuration);
        sources[order]!.Cast(40171, target);
    }
    private void Charge(int order)
    {
        var source = sources[order]!;
        var target = world.Party.Get(state.Targets[order])!;
        var separation = new Vector2(target.Position.X - source.Position.X, target.Position.Z - source.Position.Z);
        // Contact with the boss hitbox needs no travel animation; the holder still resolves the same hit.
        if (order == 0 && source.HitboxRadius > 0 && separation.Length() <= source.HitboxRadius + target.HitboxRadius)
            return;
        source.SetRotation(new Placement(source.Position, source.Rotation).Face(target.Position).Rotation);
        source.Cast(new EnemyAction(state.Fire(order) ? 40138u : 40141u) { Cast = new() { AnimationLock = 0 } }, target);
        // UNVERIFIED NPC travel interpolation; cast-to-impact time determines arrival, not player speed.
        source.MoveTo(target.Position, Vector3.Distance(source.Position, target.Position) /
            (FruFallOfFaithTiming.Impact[order] - FruFallOfFaithTiming.Charge[order]));
    }
    private void Impact(int order)
    {
        var holder = world.Party.Get(state.Targets[order])!;
        if (!state.Fire(order))
        {
            helpers[order * 4].SetPosition(holder.Position);
            helpers[order * 4].Cast(FruFallOfFaithActions.PrimaryLightning with { DeathExplanation = $"Tether {order + 1}: Sinsmite" }, holder);
        }
        else ResolveCones(order);
        holder.RemoveStatus(FruFallOfFaithActions.Prey);
        tethers[order]?.Despawn();
        if (state.Fire(order)) ResolvedCount++;
    }
    private void LightningCones(int order)
    {
        if (state.Fire(order)) return;
        ResolveCones(order); ResolvedCount++;
    }
    private void ResolveCones(int order)
    {
        var holder = world.Party.Get(state.Targets[order])!;
        var origin = holder.Position;
        var targets = world.Party.ActiveMembers().Where(m => m.IsAlive() && m != holder)
            .OrderBy(m => (m.Position.X - origin.X) * (m.Position.X - origin.X) + (m.Position.Z - origin.Z) * (m.Position.Z - origin.Z)).ThenBy(m => ((ISimPartyMember)m).Role)
            .Take(state.Fire(order) ? 1 : 3).ToArray();
        if (targets.Length == 0)
        {
            Plugin.Game.RecordMechanicMistake(state.Fire(order) ? 40156u : 40143u, $"Tether {order + 1}: no living cone baiters.");
            return;
        }
        for (var i = 0; i < targets.Length; i++)
        {
            var placement = new Placement(origin, 0).Face(targets[i].Position);
            var helper = helpers[order * 4 + (state.Fire(order) ? 0 : i + 1)];
            helper.SetPosition(placement); helper.SetRotation(placement.Rotation);
            var action = state.Fire(order) ? FruFallOfFaithActions.FireCone : FruFallOfFaithActions.LightningCone;
            helper.Cast(action with
            {
                Area = action.Area with { AdjustTargets = (_, hits) => state.Fire(order) ? hits.Append(holder).Distinct().ToArray() : hits.Where(h => h != holder).ToArray() },
                Effects = action.Effects.Append(Resolve(ctx =>
                {
                    var roles = ctx.Hits.OfType<ISimPartyMember>().Select(m => m.Role).ToArray();
                    cones.Add(new(order, state.Fire(order), world.Events.Elapsed, origin, placement.Rotation,
                        ((ISimPartyMember)targets[i]).Role, roles));
                    if (!state.Fire(order) || ctx.Hits.Count >= 4) return;
                    Understacks++;
                    foreach (var member in ctx.Hits) member.AddStatus(FruFallOfFaithActions.MarkOfMortality, 180);
                    Plugin.Game.RecordMechanicMistake(40156, $"Tether {order + 1}: Sinblaze hit {ctx.Hits.Count}/4 players; Mark of Mortality.");
                })).ToArray(),
                DeathExplanation = $"Tether {order + 1}: {(state.Fire(order) ? "Sinblaze" : "Bow Shock")}",
            });
        }
    }
    private void Cleanup()
    {
        Completed = ResolvedCount == 4;
        active = false;
        foreach (var tether in tethers) tether?.Despawn();
        foreach (var actor in actors) actor.Despawn();
        actors.Clear(); cones.Clear();
        foreach (var member in world.Party.ActiveMembers())
            foreach (var status in new[] { FruFallOfFaithActions.Prey, FruFallOfFaithActions.Fetters, FruFallOfFaithActions.MagicVulnerability, FruFallOfFaithActions.MarkOfMortality }) member.RemoveStatus(status);
    }
}
