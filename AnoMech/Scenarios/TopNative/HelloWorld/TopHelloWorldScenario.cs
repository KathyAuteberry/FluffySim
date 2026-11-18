using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;
using Actions = AnoMech.Scenarios.TopNative.TopNativeActions;

namespace AnoMech.Scenarios.TopNative.HelloWorld;

// The bugs and the Nisis are tracked here, not read back off status timers: every transfer is
// decided by who stands where, the way the debuffs move in retail.
public sealed class TopHelloWorldScenario : IMultiplayerReplayable
{
    public const float HelloWorldCast = 5f;
    public const float BugsAt = HelloWorldCast + 8.01f;
    public static readonly float[] TowerCasts = [HelloWorldCast + 19.14f, HelloWorldCast + 40.18f, HelloWorldCast + 61.27f, HelloWorldCast + 82.30f];
    public const float ResolveAfterCast = 9.99f;
    public const float RegressionBeforeResolve = 1.12f;
    public const float RegressionDuration = 10f;
    public const float CriticalErrorCast = HelloWorldCast + 105.45f;
    public const float TowerRadius = 14f;
    public const float NisiDuration = 27f;
    public const float BugDuration = 21f;
    public const float TouchDistance = 1.2f;
    public const float TetherBreakDistance = 10f;

    public static float ResolveTime(int loop) => TowerCasts[loop] + ResolveAfterCast;

    public string Name => "Hello World";
    public IPhase Phase => TopNativeZone.P3;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new TopHelloWorldAi()];
    public void DrawSettings() => settings.Draw();
    public bool HasPerPlayerSettings => true;
    public void DrawPerPlayerSettings() => settings.DrawPerPlayer();
    public object SettingsOverrides => settings.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settings.Overrides.Validate().Problems;
    public TopHelloWorldState? LastState { get; private set; }

    private sealed record Nisi(bool Blue, float ExpiresAt);

    private readonly TopHelloWorldSettingsWindow settings = new();
    private SimWorld world = null!;
    private TopHelloWorldState state = null!;
    private SimEnemy? omega;
    private readonly List<SimEnemy?> helpers = [];
    private readonly Dictionary<SimCharacter, Nisi> nisis = [];
    private readonly HashSet<SimCharacter> overflowBugs = [];
    private readonly HashSet<SimCharacter> syncBugs = [];
    private readonly SimTether?[] tethers = new SimTether?[2];
    private readonly SimTether?[] prepTethers = new SimTether?[2];
    private readonly bool[] tetherBroken = new bool[2];
    private readonly (SimCharacter? A, SimCharacter? B)[] tetherPairs = new (SimCharacter?, SimCharacter?)[2];
    private int activeLoop = -1;
    private float now;
    private int nextHelper;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = LastState = new TopHelloWorldState(world.Rng, world.Party, settings.Overrides);
        nisis.Clear();
        overflowBugs.Clear();
        syncBugs.Clear();
        activeLoop = -1;
        now = 0f;
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<TopHelloWorldState>)AiStrats[idx]).Run(state, world);

        omega = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaP3, NameId: BNpcNameId.OmegaP3, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.OmegaP3,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        helpers.Clear();
        nextHelper = 0;
        for (var i = 0; i < 28; i++) helpers.Add(SpawnHelper());
        world.Events.Add(HelloWorldCast - 0.5f, () => world.Map.SetWeather(WeatherId.HelloWorld, transition: 1f));
        world.Events.Add(HelloWorldCast, () => omega?.Cast(new EnemyAction(ActionId.HelloWorld) { Effects = [Damage()] }));
        world.Events.Add(HelloWorldCast + 4.98f, InitialDebuffs);
        world.Events.Add(BugsAt, FirstBugs);
        for (var loop = 0; loop < TopHelloWorldState.Loops; loop++) Loop(loop);
        world.Events.Add(HelloWorldCast + 4.98f + 27f, () => KillStillCarrying(StatusId.LatentDefect, ActionId.LatentDefectFail, "Latent Defect: no Critical Overflow Bug reached you"));
        world.Events.Add(HelloWorldCast + 4.98f + 69f, () => KillStillCarrying(StatusId.LatentSynchronizationBug, ActionId.LatentSynchronizationDefect, "Latent Synchronization Bug: never shared a stack"));
        world.Events.Add(CriticalErrorCast, () => omega?.Cast(new EnemyAction(ActionId.CriticalError) { Effects = [Damage(), Resolve(CriticalError)] }));
        world.Events.Add(CriticalErrorCast + 9.5f, () => world.Map.SetWeather(WeatherId.P3, transition: 1f));
    }

    public void Tick(float delta, float elapsed)
    {
        now = elapsed;
        if (state == null || world == null) return;
        PassNisisByTouch();
        CheckTetherBreaks();
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.OmegaP3,
        Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private SimEnemy? Helper() => helpers[4 + nextHelper++ % (helpers.Count - 4)];

    private SimCharacter? Member(PartyRole role) => world.Party.Get(role);

    private IEnumerable<SimCharacter> PairMembers(int pair) => state.Pair.Where(kv => kv.Value == pair).Select(kv => Member(kv.Key)).OfType<SimCharacter>();

    private IEnumerable<SimCharacter> WithRole(HelloRole role, int loop) => PerRole.All.Where(r => state.RoleOf(r, loop) == role).Select(Member).OfType<SimCharacter>();

    private void InitialDebuffs()
    {
        foreach (var (role, pair) in state.Pair)
        {
            if (Member(role) is not { } m) continue;
            m.AddStatus(StatusId.LatentSynchronizationBug, 69f);
            var first = TopHelloWorldState.RoleInLoop[0][pair];
            var blueNisi = first == HelloRole.Overflow ? state.OverflowWithBlue : !state.OverflowWithBlue;
            if (first == HelloRole.Overflow) m.AddStatus(StatusId.OverflowCodeSmell, 3f);
            if (first == HelloRole.Synchronization) m.AddStatus(StatusId.SynchronizationCodeSmell, 3f);
            if (first is HelloRole.Overflow or HelloRole.Synchronization)
                m.AddStatus(blueNisi ? StatusId.PerformanceCodeSmell : StatusId.UnderflowCodeSmell, 3f);
            if (first == HelloRole.Local) m.AddStatus(StatusId.LatentDefect, 27f);
            for (var loop = 0; loop < TopHelloWorldState.Loops; loop++)
            {
                var r = TopHelloWorldState.RoleInLoop[loop][pair];
                if (r == HelloRole.Local) m.AddStatus(StatusId.LocalCodeSmell, 23f + 21f * loop);
                if (r == HelloRole.Remote) m.AddStatus(StatusId.RemoteCodeSmell, 23f + 21f * loop);
            }
        }
    }

    private void FirstBugs()
    {
        foreach (var m in WithRole(HelloRole.Overflow, 0))
        {
            GiveOverflow(m);
            GiveNisi(m, state.OverflowWithBlue);
        }
        foreach (var m in WithRole(HelloRole.Synchronization, 0))
        {
            GiveSync(m);
            GiveNisi(m, !state.OverflowWithBlue);
        }
        PrepTethers(0);
    }

    private void PrepTethers(int loop)
    {
        var local = WithRole(HelloRole.Local, loop).ToArray();
        var remote = WithRole(HelloRole.Remote, loop).ToArray();
        if (local.Length == 2) prepTethers[0] = world.Tether(local[0], local[1], TetherId.LocalCodeSmell);
        if (remote.Length == 2) prepTethers[1] = world.Tether(remote[0], remote[1], TetherId.RemoteCodeSmell);
    }

    private void Loop(int loop)
    {
        var cast = TowerCasts[loop];
        var resolve = ResolveTime(loop);
        world.Events.Add(cast, () => omega?.Cast(ActionId.LatentDefect, animationLock: 0.6f));
        for (var i = 0; i < 4; i++)
        {
            var bearing = state.Towers[loop].Bearing(i);
            var blue = i < 2 ? state.OverflowWithBlue : !state.OverflowWithBlue;
            var helper = helpers[i];
            world.Events.Add(cast - 0.05f, () => helper?.SetPosition(new Placement(Polar(bearing, TowerRadius), 0f).Face(Vector3.Zero)));
            world.Events.Add(cast, () => helper?.Cast(Tower(blue)));
        }
        world.Events.Add(resolve - RegressionBeforeResolve, () => ActivateTethers(loop));
        world.Events.Add(resolve, () => ResolveBugs(loop));
        world.Events.Add(resolve - RegressionBeforeResolve + RegressionDuration, () => FailUnbrokenTethers(loop));
    }

    private EnemyAction Tower(bool blue) => new(blue ? ActionId.BlueTower : ActionId.RedTower)
    {
        Cast = new() { AnimationLock = 0.1f },
        Timing = new() { DamageDelay = 0.6f },
        Effects =
        [
            Damage(),
            Resolve(ctx => Soak(blue, ctx.Hits)),
            FollowUp(new EnemyAction(blue ? ActionId.BlueTowerUnsoakedWipe : ActionId.RedTowerUnsoakedWipe)
            {
                Cast = new() { CastSecondsOverride = 0f },
                Effects = [Damage(Lethal)],
                DeathExplanation = "a Hello World tower was left empty",
            }, ctx => ctx.Hits.Count == 0),
        ],
    };

    private void Soak(bool blue, IReadOnlyList<SimCharacter> soakers)
    {
        foreach (var m in soakers)
        {
            m.AddStatus(blue ? StatusId.LatentPerformanceDefect : StatusId.CascadingLatentDefect, 10f);
            world.Events.Add(10.67f, () =>
            {
                if (!m.IsAlive() || !m.HasStatus(blue ? StatusId.LatentPerformanceDefect : StatusId.CascadingLatentDefect)) return;
                DiagnosticLog.Info($"[TopHelloWorld] {(m as ISimPartyMember)?.Role} kept a {(blue ? "blue" : "red")} tower defect");
                Helper()?.Cast(new EnemyAction(blue ? ActionId.BlueTowerExpireWipe : ActionId.RedTowerExpireWipe)
                {
                    Cast = new() { CastSecondsOverride = 0f },
                    Effects = [Damage(Lethal)],
                    DeathExplanation = $"a {(blue ? "blue" : "red")} tower defect expired: its soaker had no matching Nisi running out",
                });
            });
        }
    }

    private void ActivateTethers(int loop)
    {
        activeLoop = loop;
        foreach (var t in prepTethers) t?.Despawn();
        var local = WithRole(HelloRole.Local, loop).ToArray();
        var remote = WithRole(HelloRole.Remote, loop).ToArray();
        tetherPairs[0] = local.Length == 2 ? (local[0], local[1]) : (null, null);
        tetherPairs[1] = remote.Length == 2 ? (remote[0], remote[1]) : (null, null);
        for (var i = 0; i < 2; i++)
        {
            tetherBroken[i] = false;
            var (a, b) = tetherPairs[i];
            if (a == null || b == null) { tetherBroken[i] = true; continue; }
            tethers[i] = world.Tether(a, b, i == 0 ? TetherId.LocalRegression : TetherId.RemoteRegression);
            a.AddStatus(i == 0 ? StatusId.LocalRegression : StatusId.RemoteRegression, RegressionDuration);
            b.AddStatus(i == 0 ? StatusId.LocalRegression : StatusId.RemoteRegression, RegressionDuration);
        }
    }

    private void CheckTetherBreaks()
    {
        if (activeLoop < 0) return;
        for (var i = 0; i < 2; i++)
        {
            if (tetherBroken[i] || tetherPairs[i] is not ({ } a, { } b)) continue;
            var distance = Vector2.Distance(new Vector2(a.Position.X, a.Position.Z), new Vector2(b.Position.X, b.Position.Z));
            var broken = i == 0 ? distance < TetherBreakDistance : distance > TetherBreakDistance;
            if (!broken) continue;
            tetherBroken[i] = true;
            tethers[i]?.Despawn();
            a.RemoveStatus(i == 0 ? StatusId.LocalRegression : StatusId.RemoteRegression);
            b.RemoveStatus(i == 0 ? StatusId.LocalRegression : StatusId.RemoteRegression);
            DiagnosticLog.Info($"[TopHelloWorld] loop {activeLoop + 1} {(i == 0 ? "local" : "remote")} tether broke at {distance:F1}y");
            Helper()?.Cast(Actions.Patch);
        }
    }

    private void FailUnbrokenTethers(int loop)
    {
        if (activeLoop != loop) return;
        for (var i = 0; i < 2; i++)
        {
            if (tetherBroken[i]) continue;
            tetherBroken[i] = true;
            tethers[i]?.Despawn();
            Helper()?.Cast(Actions.PatchFail);
        }
    }

    private void ResolveBugs(int loop)
    {
        var overflows = overflowBugs.Where(m => m.IsAlive()).ToList();
        var syncs = syncBugs.Where(m => m.IsAlive()).ToList();
        overflowBugs.Clear();
        syncBugs.Clear();
        foreach (var m in overflows) m.RemoveStatus(StatusId.CriticalOverflowBug);
        foreach (var m in syncs) m.RemoveStatus(StatusId.CriticalSynchronizationBug);
        foreach (var holder in overflows)
        {
            var h = holder;
            Helper()?.Cast(new EnemyAction(ActionId.CriticalOverflowBug)
            {
                Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
                Effects = [Damage(), Resolve(ctx => Defamation(h, ctx.Hits))],
            }, h);
        }
        foreach (var holder in syncs)
        {
            var h = holder;
            Helper()?.Cast(new EnemyAction(ActionId.CriticalSynchronizationBug)
            {
                Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
                Effects = [Damage(), Resolve(ctx => Stack(h, ctx.Hits))],
            }, h);
        }
        if (loop + 1 < TopHelloWorldState.Loops) world.Events.Add(0.98f, () => PrepTethers(loop + 1));
    }

    private void Defamation(SimCharacter holder, IReadOnlyList<SimCharacter> hits)
    {
        holder.AddStatus(StatusId.OverflowDebugger);
        foreach (var m in hits.Where(m => m != holder))
        {
            m.RemoveStatus(StatusId.LatentDefect);
            if (m.HasStatus(StatusId.OverflowDebugger)) { m.RemoveStatus(StatusId.OverflowDebugger); continue; }
            GiveOverflow(m);
        }
    }

    private void GiveOverflow(SimCharacter m)
    {
        overflowBugs.Add(m);
        m.AddStatus(StatusId.CriticalOverflowBug, BugDuration);
    }

    private void GiveSync(SimCharacter m)
    {
        syncBugs.Add(m);
        m.AddStatus(StatusId.CriticalSynchronizationBug, BugDuration);
    }

    private void Stack(SimCharacter holder, IReadOnlyList<SimCharacter> hits)
    {
        holder.AddStatus(StatusId.SynchronizationDebugger);
        if (hits.Count < 2) return;
        foreach (var m in hits)
        {
            m.RemoveStatus(StatusId.LatentSynchronizationBug);
            if (m == holder || m.HasStatus(StatusId.SynchronizationDebugger)) continue;
            GiveSync(m);
        }
    }

    private static ushort NisiStatus(bool blue) => blue ? StatusId.CriticalPerformanceBug : StatusId.CriticalUnderflowBug;
    private static ushort Debugger(bool blue) => blue ? StatusId.PerformanceDebugger : StatusId.UnderflowDebugger;

    private void GiveNisi(SimCharacter m, bool blue)
    {
        if (nisis.TryGetValue(m, out var old)) m.RemoveStatus(NisiStatus(old.Blue));
        var nisi = new Nisi(blue, now + NisiDuration);
        nisis[m] = nisi;
        m.AddStatus(NisiStatus(blue), NisiDuration);
        world.Events.Add(NisiDuration, () => ExpireNisi(m, nisi));
    }

    private void PassNisisByTouch()
    {
        foreach (var (holder, nisi) in nisis.ToArray())
        {
            if (!holder.IsAlive() || !holder.IsActive) continue;
            foreach (var other in world.Party.ActiveMembers())
            {
                if (other == holder || !other.IsAlive() || nisis.ContainsKey(other)) continue;
                if (other.HasStatus(Debugger(nisi.Blue)) || !MayReceive(other, nisi.Blue)) continue;
                if (Vector2.Distance(new Vector2(holder.Position.X, holder.Position.Z), new Vector2(other.Position.X, other.Position.Z)) > TouchDistance) continue;
                DiagnosticLog.Info($"[TopHelloWorld] {(holder as ISimPartyMember)?.Role} passed a {(nisi.Blue ? "blue" : "red")} Nisi to {(other as ISimPartyMember)?.Role}");
                GiveNisi(other, nisi.Blue);
            }
        }
    }

    // Bots take a Nisi only when the strat hands them one; players crossing paths dodge each
    // other by eye. The human takes one whenever they touch a carrier.
    private bool MayReceive(SimCharacter member, bool blue)
    {
        if (!world.Party.IsBotDriven(member)) return true;
        if (member is not ISimPartyMember m) return false;
        for (var loop = 0; loop < TopHelloWorldState.Loops - 1; loop++)
        {
            var resolve = ResolveTime(loop);
            if (now < resolve || now > resolve + 6f) continue;
            var role = state.RoleOf(m.Role, loop);
            return blue == state.OverflowWithBlue ? role == HelloRole.Local : role == HelloRole.Remote;
        }
        return false;
    }

    private void ExpireNisi(SimCharacter m, Nisi nisi)
    {
        if (!nisis.TryGetValue(m, out var current) || current != nisi) return;
        nisis.Remove(m);
        if (!m.IsAlive()) return;
        var blue = nisi.Blue;
        Helper()?.Cast(new EnemyAction(blue ? ActionId.CriticalPerformanceBug : ActionId.CriticalUnderflowBug)
        {
            Cast = new() { CastSecondsOverride = 0f, AnimationLock = 0.1f },
            Effects = [OnOthers(Damage(Lethal))],
            DeathExplanation = $"stood in someone's {(blue ? "blue" : "red")} Nisi explosion",
        }, m);
        m.RemoveStatus(blue ? StatusId.LatentPerformanceDefect : StatusId.CascadingLatentDefect);
        m.AddStatus(Debugger(blue));
    }

    private void KillStillCarrying(ushort status, uint actionId, string reason)
    {
        foreach (var m in world.Party.ActiveMembers().Where(m => m.IsAlive() && m.HasStatus(status)).ToList())
            Helper()?.Cast(new EnemyAction(actionId) { Cast = new() { CastSecondsOverride = 0f }, Effects = [Damage(Lethal)], DeathExplanation = reason }, m);
    }

    private void CriticalError(EnemyActionContext ctx)
    {
        foreach (var m in ctx.Hits)
        {
            if (m.HasStatus(StatusId.OverflowDebugger)) { m.RemoveStatus(StatusId.OverflowDebugger); continue; }
            m.AddStatus(StatusId.CriticalOverflowBug, BugDuration);
            DiagnosticLog.Info($"[TopHelloWorld] {(m as ISimPartyMember)?.Role} has no Overflow Debugger for Critical Error");
        }
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s
            ? new TopHelloWorldAiReplayStateMessage(new Dictionary<PartyRole, int>(s.Pair), s.OverflowWithBlue,
                s.Towers.Select(t => t.Intercardinal).ToArray(), s.Towers.Select(t => t.Rotation).ToArray())
            : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not TopHelloWorldAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (TopHelloWorldState.FromNetworkReplay(msg.Pair, msg.OverflowWithBlue, msg.Intercardinal, msg.Rotation) is not { } shadow) return null;
        ((IScenarioAi<TopHelloWorldState>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }
}
