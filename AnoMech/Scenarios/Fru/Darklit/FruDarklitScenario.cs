using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Fru.Darklit;

public static class FruDarklitRules
{
    public static float TowerRadius => Natives.Data.Action(40213)!.EffectRange;
    public const float FragmentHitbox = 3.5f;
    public static float SpiritRadius => Natives.Data.Action(40289)!.EffectRange;
    // UNVERIFIED exact retail thresholds: documented approximate practice interval.
    public const float MinimumFateSeparation = 8;
    public const float MaximumFateSeparation = 26;
    public static bool FateFails(Vector3 a, Vector3 b, bool aliveA = true, bool aliveB = true)
        => !aliveA || !aliveB || Vector3.Distance(a, b) < MinimumFateSeparation || Vector3.Distance(a, b) > MaximumFateSeparation;
    public static bool SpiritHitsFragment(Vector3 landing, Vector3 fragment)
        => Vector3.Distance(landing, fragment) <= SpiritRadius + FragmentHitbox;
}
public sealed class FruDarklitScenario : IScenario
{
    public string Name => "Darklit Dragonsong (Mana)";
    public string DisplayNameOverride => "P4 Darklit Dragonsong";
    public bool IsIncomplete => true;
    public bool EnablePhaseBgm => true;
    public IPhase Phase => FruZone.P4;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruDarklitAi()];
    private readonly FruDarklitSettingsWindow settings = new();
    public object SettingsOverrides => settings.Overrides;
    public void DrawSettings() => settings.Draw();
    public IReadOnlyList<string> SettingsConflicts => settings.Conflicts;
    public FruDarklitState? LastState { get; private set; }
    public bool Completed { get; private set; }
    public bool FragmentCompromised { get; private set; }
    public uint FragmentHealth { get; private set; }
    public const uint FragmentMaximumHealth = 100;
    public static readonly Vector3 FragmentPosition = new(0, 0, -16);
    public static readonly Vector3[] TowerPositions = [new(0, 0, -8), new(0, 0, 8)];
    public Vector3 OraclePosition => oracle?.Position ?? new(0, 0, 5);
    public Vector3 SpiritLanding { get; private set; }
    public IReadOnlyList<Vector3> ActiveTowerPositions => towerVisible && fragment is { IsActive: true } ? TowerPositions : [];
    public bool WingsVisible { get; private set; }
    public int ResolvedTowers { get; private set; }
    public int ResolvedCones { get; private set; }
    public int ResolvedSomberHits { get; private set; }
    public IReadOnlyList<PartyRole> SomberTargets => somberTargets;
    public IReadOnlySet<PartyRole> SomberVulnerable => somberVulnerable;
    public IReadOnlyList<(float Time, uint Action)> Resolutions => resolutions;
    private readonly List<(float, uint)> resolutions = [];
    private readonly List<PartyRole> somberTargets = [];
    private readonly HashSet<PartyRole> somberVulnerable = [];
    private readonly Dictionary<PartyRole, SimEnemy> pathSources = [];
    private readonly List<SimEnemy> actors = [];
    private readonly List<SimTether> chains = [];
    private readonly SimEnemy[] helpers = new SimEnemy[16];
    private readonly uint[] towerStates = [0, 0];
    private int helperIndex;
    private bool towerVisible, fateActive, fateFailed, lightFailed, ended;
    private SimWorld world = null!;
    private SimEnemy shiva = null!, oracle = null!, fragment = null!;
    private FruDarklitState state = null!;
    private PartyRole spiritTarget;
    private Vector3 spiritSnapshot;
    // UNVERIFIED jump displacement offset, scaled from the offline simulator.
    private const float JumpLandingBuffer = 6.35f;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        world.PracticeInvulnerability.Start();
        world.OnDespawn(ClearPathPresentation);
        pathSources.Clear();
        PathTargets = []; PathCones = []; PathSnapshotCandidates = []; PathSnapshotTime = -1;
        var mt = world.Party.Get(PartyRole.MainTank)!;
        state = LastState = new(world.Rng, settings.Overrides, world.Party.PlayerRole,
            mt.Proxy?.ClassJob == 19 ? PartyRole.OffTank : PartyRole.MainTank);
        actors.Clear(); chains.Clear(); resolutions.Clear(); somberTargets.Clear(); somberVulnerable.Clear();
        Completed = FragmentCompromised = towerVisible = fateActive = fateFailed = lightFailed = WingsVisible = ended = false;
        ResolvedTowers = ResolvedCones = ResolvedSomberHits = helperIndex = 0;
        FragmentHealth = FragmentMaximumHealth;
        Array.Clear(towerStates);
        world.Events.Add(0, SpawnActors);
        world.Events.Add(2.01f, ShowInitialWaterIndicators);
        world.Events.Add(2.1f, () => { shiva.Cast(FruDarklitActions.Darklit); oracle.Cast(FruDarklitActions.OracleDarklit); });
        world.Events.Add(7.083f, AssignWater);
        world.Events.Add(7.928f, AssignChains);
        world.Events.Add(10.144f, ShowTowers);
        world.Events.Add(10.233f, () => shiva.Cast(FruDarklitActions.Path));
        world.Events.Add(14.916f, ActivateFate);
        world.Events.Add(18.249f, ResolveTowers);
        world.Events.Add(18.249f, () => oracle.Cast(FruDarklitActions.SpiritCast));
        world.Events.Add(19.095f, ResolveCones);
        world.Events.Add(21.097f, ShowWaterStacks);
        world.Events.Add(21.25f, () => { spiritTarget = state.SpiritTarget; spiritSnapshot = world.Party.Get(spiritTarget)!.Position; });
        world.Events.Add(21.319f, StartWings);
        world.Events.Add(21.85f, ResolveSpirit);
        world.Events.Add(24.392f, () => oracle.Cast(FruDarklitActions.SomberCast));
                world.Events.Add(26.1f, ResolveWater);
        world.Events.Add(26.319f, () => WingsVisible = false);
        world.Events.Add(28.916f, ClearFate);
        world.Events.Add(29.792f, () => ResolveSomber(false));
        world.Events.Add(32.992f, () => ResolveSomber(true));
        world.Events.Add(36, () => { Cleanup(); Completed = !FragmentCompromised; });
        if (selectedAi is { } index) ((IScenarioAi<FruDarklitState>)AiStrats[index]).Run(state, world);
    }
    private void SpawnActors()
    {
        shiva = Spawn(FruConstants.BNpcBaseId.UsurperOfFrostP4, FruConstants.BNpcNameId.UsurperOfFrost, Vector3.Zero, true);
        oracle = Spawn(FruConstants.BNpcBaseId.OracleOfDarknessP4, FruConstants.BNpcNameId.OracleOfDarkness, new(0, 0, 5), true, new(65537, 4295237459));
        oracle.SetRotation(0);
        fragment = Spawn(FruConstants.BNpcBaseId.FragmentOfFate, FruConstants.BNpcNameId.FragmentOfFate, FragmentPosition, true);
        world.Map.AddEffect(0x00020001, 0x2E);
        actors.AddRange(FruP4Presentation.SpawnVisions(world));
        for (var i = 0; i < helpers.Length; i++) helpers[i] = Spawn(FruConstants.BNpcBaseId.Helper, 0, Vector3.Zero, false);
    }
    private SimEnemy Spawn(uint id, uint name, Vector3 position, bool visible, NpcAppearance? appearance = null)
    {
        var actor = world.SpawnEnemy(new EnemySpawnConfig(id, name, Level: 100, Targetable: visible,
            EnemyList: visible ? EnemyListMode.Always : EnemyListMode.Never, Visibility: visible ? SpawnVisibility.Visible : SpawnVisibility.InvisibleHelper,
            Placement: new(position, MathF.PI), Appearance: appearance)) ?? throw new InvalidOperationException("Darklit actor creation failed.");
        actors.Add(actor); return actor;
    }
    private SimEnemy Helper(Vector3 position)
    { var actor = helpers[helperIndex++]; actor.SetPosition(position); return actor; }
    private void ShowInitialWaterIndicators()
    {
        foreach (var role in new[] { state.Pattern.TetherWater, state.Pattern.UntetherWater }) world.Party.Get(role)!.AttachLockonVfx(62, 5.1f, persistent: false);
    }
    private void AssignWater()
    {
        foreach (var role in new[] { state.Pattern.TetherWater, state.Pattern.UntetherWater })
        {
            world.Party.Get(role)!.AddStatus(2461, 19);
            
        }
    }
    private void AssignChains()
    {
        foreach (var member in world.Party.ActiveMembers()) member.AddStatus(2257, 12, 3, overrideStacks: true);
        foreach (var role in state.Pattern.Tethered) world.Party.Get(role)!.AddStatus(2253, 7);
        CreateChains(110);
    }
    private void CreateChains(ushort id)
    {
        foreach (var chain in chains) chain.Despawn(); chains.Clear();
        var ring = state.Pattern.ChainRing;
        for (var i = 0; i < 4; i++) chains.Add(world.Tether(world.Party.Get(ring[i]), world.Party.Get(ring[(i + 1) % 4]), id));
    }
    private void ActivateFate()
    {
        foreach (var role in state.Pattern.Tethered)
        { var member = world.Party.Get(role)!; member.RemoveStatus(2253); member.AddStatus(2255, 14); }
        CreateChains(111); fateActive = true;
    }
    private void ClearFate()
    {
        fateActive = false;
        foreach (var chain in chains) chain.Despawn(); chains.Clear();
        foreach (var role in state.Pattern.Tethered) world.Party.Get(role)!.RemoveStatus(2255);
    }
    private void ShowTowers()
    {
        towerVisible = true;
        for (byte i = 0; i < 2; i++)
        {
            world.Map.AddEffect(0x00020001, (byte)(0x2A + i));
            world.Map.AddEffect(0x00010004, (byte)(0x2C + i));
            world.Map.AddEffect(0x00200010, (byte)(0x2A + i));
            towerStates[i] = 0x00200010;
        }
    }
    private void ResolveTowers()
    {
        for (byte i = 0; i < 2; i++)
        {
            Helper(TowerPositions[i]).Cast(new EnemyAction(40213)
            {
                Timing = new() { DamageDelay = 0 },
                Effects = [Damage(), new ResolveEffect(ctx =>
                {
                    if (ctx.Hits.Count != 2 || ctx.Hits.Any(m => !state.Pattern.IsTethered(((ISimPartyMember)m).Role)))
                        world.Party.WipeAllPlayers(40213, "Darklit tower needs exactly two tethered players");
                    AddLight(ctx.Hits);
                })],
            });
            world.Map.AddEffect(0x00080004, (byte)(0x2A + i));
        }
        towerVisible = false; ResolvedTowers = 2; resolutions.Add((world.Events.Elapsed, 40213));
    }
    public static float PathDistance(Vector3 source, Vector3 target, float sourceRadius, float targetRadius)
        => MathF.Max(0, new Vector2(target.X - source.X, target.Z - source.Z).Length() - sourceRadius - targetRadius);
    public Vector3 ShivaPosition => shiva?.Position ?? Vector3.Zero;
    public float ShivaRotation => shiva?.Rotation ?? 0;
    public Vector3? NativeShivaPosition => shiva?.Proxy is { Exists: true } proxy ? proxy.Position : null;
    public float? NativeShivaRotation => shiva?.Proxy is { Exists: true } proxy ? proxy.Rotation : null;
    public IReadOnlyList<DarklitPathCandidate> PathCandidates => world == null ? [] : world.Party.ActiveMembers()
        .Select(m => new DarklitPathCandidate(((ISimPartyMember)m).Role, m.Position, m.Proxy is { Exists: true } proxy ? proxy.Position : null, PathDistance(ShivaPosition, m.Position, shiva?.HitboxRadius ?? 0, m.HitboxRadius), m.IsAlive()))
        .OrderBy(m => m.Distance).ToArray();
    public IReadOnlyList<DarklitPathNativeSource> PathNativeSources => pathSources.Select(pair =>
        new DarklitPathNativeSource(pair.Key, pair.Value.Proxy is { Exists: true } proxy ? proxy.Position : null,
            pair.Value.Proxy is { Exists: true } source ? source.Rotation : null)).ToArray();
    public IReadOnlyList<DarklitPathCandidate> PathSnapshotCandidates { get; private set; } = [];
    public IReadOnlyList<DarklitPathCone> PathCones { get; private set; } = [];
    public float PathSnapshotTime { get; private set; } = -1;
    public IReadOnlyList<PartyRole> PathTargets { get; private set; } = [];
    private void ResolveCones()
    {
        var targets = world.Party.ActiveMembers().Where(m => m.IsAlive()).OrderBy(m => PathDistance(shiva.Position, m.Position, shiva.HitboxRadius, m.HitboxRadius)).Take(4).ToArray();
        PathTargets = targets.Select(m => ((ISimPartyMember)m).Role).ToArray();
        PathSnapshotTime = world.Events.Elapsed;
        PathSnapshotCandidates = PathCandidates;
        PathCones = targets.Select(target =>
        {
            var direction = target.Position - shiva.Position;
            return new DarklitPathCone(((ISimPartyMember)target).Role, new Placement(shiva.Position, MathF.Atan2(direction.X, direction.Z)));
        }).ToArray();
        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var snapshot = PathCones[i].Source;
            var helper = Helper(shiva.Position);
            helper.SetRotation(snapshot.Rotation);
            pathSources[PathCones[i].Target] = helper;
            helper.Cast(new EnemyAction(40190)
            {
                Timing = new() { DamageDelay = 0 }, Area = new() { Size = MathF.PI / 6, AdjustTargets = (_, _) => world.Party.Find.InsideCone(snapshot, MathF.PI / 6, Natives.Data.Action(40190)!.EffectRange) },
                Effects = [Damage(), new ResolveEffect(ctx => AddLight(ctx.Hits))],
            }, target);
        }
        ResolvedCones = targets.Length; resolutions.Add((world.Events.Elapsed, 40190));
    }
    private void AddLight(IEnumerable<SimCharacter> members)
    {
        foreach (var member in members.Where(m => m.IsAlive()))
        {
            member.AddStatus(2257, 36);
            if (member.FindStatus(2257)?.Stacks >= 5 && !lightFailed)
            {
                lightFailed = true;
                Helper(Vector3.Zero).Cast(new EnemyAction(40216) { Cast = new() { TargetsPartyForPresentation = true } });
                world.Party.WipeAllPlayers(40216, "Five Lightsteeped stacks");
            }
        }
    }
    private void ResolveSpirit()
    {
        SpiritLanding = spiritSnapshot;
        JumpTo(spiritSnapshot);
        oracle.Cast(FruDarklitActions.Spirit with
        {
            Area = new() { AdjustTargets = (_, _) => world.Party.Find.InsideCircle(spiritSnapshot, FruDarklitRules.SpiritRadius) },
            Effects = [OnTarget(Damage()), new ResolveEffect(ctx =>
            {
                foreach (var hit in ctx.Hits.Where(h => h != world.Party.Get(spiritTarget)))
                    ctx.Knockback(hit, 40, 40, 0);
            })],
        }, world.Party.Get(spiritTarget));
        if (FruDarklitRules.SpiritHitsFragment(spiritSnapshot, FragmentPosition)) DamageFragment(40289);
        resolutions.Add((world.Events.Elapsed, 40289));
    }
    private void JumpTo(Vector3 target)
    {
        var vector = target - oracle.Position;
        if (vector.Length() > .001f) oracle.SetPosition(oracle.Position + Vector3.Normalize(vector) * MathF.Max(0, vector.Length() - JumpLandingBuffer));
    }
    private void DamageFragment(uint action)
    {
        // UNVERIFIED HP amount: practice HP tracks avoidable damage, not retail phase attrition.
        FragmentHealth = FragmentHealth > 25 ? FragmentHealth - 25 : 0;
        FragmentCompromised = true;
        Plugin.Game.RecordMechanicMistake(action, "Fragment of Fate damaged; the remaining phase cannot be cleared");
    }
    private void StartWings()
    {
        WingsVisible = true;
        shiva.Cast(new EnemyAction(state.EastWing ? 40228u : 40227u)
        {
            Area = new() { AdjustTargets = (_, _) => world.Party.ActiveMembers().Where(m => state.EastWing ? m.Position.X >= 0 : m.Position.X <= 0).ToArray() },
            Timing = new() { ResolveSnapshotOffset = .3f, DamageDelay = 0 }, Effects = [Damage(Severity.Lethal)],
        });
    }
    private void ShowWaterStacks()
    {
        foreach (var role in new[] { state.Pattern.TetherWater, state.Pattern.UntetherWater }) world.Party.Get(role)!.AttachLockonVfx(184, 5.1f, persistent: false);
    }
    private void ResolveWater()
    {
        var targets = new[] { state.Pattern.TetherWater, state.Pattern.UntetherWater }.Select(r => world.Party.Get(r)!).ToArray();
        foreach (var member in world.Party.ActiveMembers())
            if (targets.All(t => Vector3.Distance(t.Position, member.Position) <= Natives.Data.Action(40271)!.EffectRange)) member.Die(40271, "Overlapping Dark Water stacks");
        foreach (var target in targets)
        {
            target.RemoveStatus(2461);
            Helper(target.Position).Cast(FruDarklitActions.Water with { Effects = FruDarklitActions.Water.Effects.Append(new ResolveEffect(ctx =>
            { if (ctx.Hits.Count != 4) foreach (var hit in ctx.Hits) ctx.Kill(hit, "Dark Water needs exactly four players"); })).ToArray() }, target);
            if (Vector3.Distance(target.Position, FragmentPosition) <= Natives.Data.Action(40271)!.EffectRange + FruDarklitRules.FragmentHitbox) DamageFragment(40271);
        }
        resolutions.Add((world.Events.Elapsed, 40271));
    }
    private void ResolveSomber(bool second)
    {
        var candidates = world.Party.ActiveMembers().Where(m => m.IsAlive()).OrderBy(m => Vector3.DistanceSquared(m.Position, oracle.Position)).ToArray();
        if (candidates.Length == 0) return;
        var target = second ? candidates[0] : candidates[^1];
        var id = second ? 40285u : 40284u;
        var impact = target.Position;
        somberTargets.Add(((ISimPartyMember)target).Role);
        foreach (var hit in world.Party.Find.InsideCircle(impact, Natives.Data.Action(id)!.EffectRange))
            if (hit != target || !((ISimPartyMember)target).Role.IsTank() || !(target == world.Party.Player && world.PracticeInvulnerability.Active) && !new ushort[] { 82, 409, 810, 1836 }.Any(target.HasStatus))
                hit.Die(id, "Somber Dance requires an isolated invulnerable tank");
        somberVulnerable.Add(((ISimPartyMember)target).Role);
        if (Vector3.Distance(impact, FragmentPosition) <= Natives.Data.Action(id)!.EffectRange + FruDarklitRules.FragmentHitbox) DamageFragment(id);
        JumpTo(impact); oracle.Cast(second ? FruDarklitActions.SomberSecond : FruDarklitActions.SomberFirst, target);
        ++ResolvedSomberHits; resolutions.Add((world.Events.Elapsed, id));
    }
    public void Tick(float delta, float elapsed)
    {
        if (ended || fragment is { IsActive: false }) return;
        if (fragment is { IsActive: true, Proxy: { Exists: true } proxy }) { proxy.ReceiveStatusList([], proxy.MaxHealth * FragmentHealth / FragmentMaximumHealth, proxy.Mana, 0); }
        if (fateActive && !fateFailed && chains.Any(t => t.A is { } a && t.B is { } b && FruDarklitRules.FateFails(a.Position, b.Position, a.IsAlive(), b.IsAlive())))
        {
            fateFailed = true;
            Helper(Vector3.Zero).Cast(new EnemyAction(40215) { Cast = new() { TargetsPartyForPresentation = true } });
            world.Party.WipeAllPlayers(40215, "Refulgent Fate tether too close, too far, or partner lost");
        }
        if (!towerVisible) return;
        for (byte i = 0; i < 2; i++)
        {
            var flag = world.Party.Find.InsideCircle(TowerPositions[i], FruDarklitRules.TowerRadius).Count == 2 ? 0x00800040u : 0x00200010u;
            if (flag == towerStates[i]) continue;
            towerStates[i] = flag; world.Map.AddEffect(flag, (byte)(0x2A + i));
        }
    }
    private void Cleanup()
    {
        ended = true;
        world.PracticeInvulnerability.Clear();
        ClearFate(); somberVulnerable.Clear(); towerVisible = WingsVisible = false;
        ClearPathPresentation();
        foreach (var actor in actors) actor.Despawn(); actors.Clear();
        foreach (var member in world.Party.ActiveMembers()) foreach (var id in new ushort[] { 2253, 2255, 2257, 2461 }) member.RemoveStatus(id);
        foreach (var index in new byte[] { 0x2A, 0x2B, 0x2E }) world.Map.AddEffect(0x00040004, index);
    }
    private void ClearPathPresentation()
    {
        pathSources.Clear();
        PathCones = []; PathSnapshotCandidates = []; PathSnapshotTime = -1;
    }
    private sealed class ResolveEffect(Action<EnemyActionContext> apply) : IEnemyActionEffect
    { public void Apply(EnemyActionContext context, System.Collections.Generic.IReadOnlyList<SimCharacter> hits) => apply(context); }
}

public readonly record struct DarklitPathCandidate(PartyRole Role, Vector3 Position, Vector3? NativeWorldPosition, float Distance, bool Alive);
public readonly record struct DarklitPathCone(PartyRole Target, Placement Source);

public readonly record struct DarklitPathNativeSource(PartyRole Target, Vector3? WorldPosition, float? Rotation);
