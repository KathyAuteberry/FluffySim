using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.Trine;

// From just before Trine to Ultimate Embrace. Trine's cast starts at 3 s.
public sealed class DmuTrineScenario : IScenario
{
    private const float TrineStart = 3f;
    private const int HelperCount = 12;
    private static readonly float[] AutoAttacks = [2.78f, 8.84f, 15.88f, 18.92f];

    public string Name => "Trine";
    public IPhase Phase => DmuZone.P2;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DmuTrineAi()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    public float BgmSecondsAtStart => 147f;

    public DmuTrineState? LastState { get; private set; }

    private readonly DmuTrineSettingsWindow settingsWindow = new();
    private readonly List<SimEnemy> helpers = [];
    private SimWorld world = null!;
    private DmuTrineState state = null!;
    private SimEnemy? kefka;
    private int nextHelper;
    private uint nextTriangleArg;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new DmuTrineState(world.Rng, settingsWindow.Overrides);
        LastState = state;
        helpers.Clear();
        nextHelper = 0;
        nextTriangleArg = 1;
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<DmuTrineState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, Spawn);
        foreach (var at in AutoAttacks) world.Events.Add(at, AutoAttack);
        world.Events.Add(TrineStart, () => kefka?.Cast(DmuTrineActions.Trine));
        Wave(TrineStart + 4.10f, state.FirstWaveTriangles);
        Wave(TrineStart + 6.10f, [state.SecondWaveTriangle]);
        world.Events.Add(TrineStart + 6.15f, WingsOfDestruction);
        Wave(TrineStart + 8.10f, state.ThirdWaveTriangles);
        world.Events.Add(TrineStart + 13.19f, () => kefka?.SetAnimationState(0, 0));
        world.Events.Add(TrineStart + 16.32f, () =>
        {
            kefka?.SetAnimationState(0, 3);
            kefka?.Cast(DmuTrineActions.WingsBusterCast);
        });
        world.Events.Add(TrineStart + 20.37f, WingsBusters);
        world.Events.Add(TrineStart + 22.33f, () => kefka?.SetAnimationState(0, 0));
        world.Events.Add(TrineStart + 22.55f, UltimateEmbrace);
        world.Events.Add(TrineStart + 30f, () => DiagnosticLog.Info("[DmuTrine] End after Ultimate Embrace."));
    }

    private void Spawn()
    {
        kefka = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.GodKefka, NameId: BNpcNameId.Kefka, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.GodKefka,
            Placement: new Placement(Vector3.Zero, float.Pi)));
        for (var i = 0; i < HelperCount; i++)
            if (world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.Kefka, Level: 1,
                    Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
                    Placement: new Placement(new Vector3(0f, 0f, -10f), 0f))) is { } helper)
                helpers.Add(helper);
    }

    private SimEnemy? Helper(Vector2 at)
    {
        if (helpers.Count == 0) return null;
        var helper = helpers[nextHelper++ % helpers.Count];
        helper.SetPosition(new Placement(new Vector3(at.X, 0f, at.Y), 0f));
        return helper;
    }

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } m && m.IsAlive() ? m : null;

    private void AutoAttack()
    {
        if (kefka is not { IsActive: true, IsCasting: false } boss || Alive(PartyRole.MainTank) is not { } tank) return;
        boss.Cast(DmuActions.AutoAttack, tank);
    }

    // Each telegraph appears, winds up, then bursts its three corners 11.77 s after spawning.
    private void Wave(float spawn, IReadOnlyList<TrineTriangle> triangles)
    {
        var props = new List<SimEventObject>();
        world.Events.Add(spawn, () =>
        {
            foreach (var triangle in triangles)
                if (world.SpawnEventObject(new EventObjectSpawnConfig
                    {
                        EObjId = triangle.EObjId,
                        LayoutId = triangle.LayoutId,
                        EventId = EventId.StatueDirector,
                        TargetableStatus = 5,
                        Arg2 = 0x400003u + (TriangleArg(triangle) << 16),
                        Placement = new Placement(new Vector3(triangle.Centre.X, 0f, triangle.Centre.Y), 0f),
                        TimelineState = 2,
                    }) is { } prop)
                    props.Add(prop);
        });
        world.Events.Add(spawn + 1.16f, () => props.ForEach(p => p.PlayBeat(0x10, 0x20)));
        world.Events.Add(spawn + 2.67f, () => props.ForEach(p => p.PlayBeat(0x40, 0x80)));
        world.Events.Add(spawn + 11.68f, () => props.ForEach(p => p.PlayBeat(0x4, 0x8)));
        world.Events.Add(spawn + 11.77f, () =>
        {
            foreach (var corner in triangles.SelectMany(t => t.Corners)) Helper(corner)?.Cast(DmuTrineActions.TrineBurst);
        });
        world.Events.Add(spawn + 12.85f, () =>
        {
            props.ForEach(p => p.Despawn());
            props.Clear();
        });
    }

    // Retail numbers the telegraphs in spawn order; the centre one is always last.
    private uint TriangleArg(TrineTriangle triangle) => triangle.Centre == Vector2.Zero ? 7u : nextTriangleArg++;

    private void WingsOfDestruction()
    {
        if (kefka == null) return;
        kefka.SetRotation(float.Pi);
        kefka.SetAnimationState(0, 2);
        kefka.Cast(state.WingsCleaveLeft ? DmuTrineActions.WingsLeft : DmuTrineActions.WingsRight);
    }

    private void WingsBusters()
    {
        if (kefka == null) return;
        var byDistance = world.Party.ActiveMembers().Where(m => m.IsAlive())
            .OrderBy(m => Vector3.DistanceSquared(m.Position, kefka.Position)).ToList();
        if (byDistance.Count == 0) return;
        var targets = byDistance.Count > 1 ? new[] { byDistance[0], byDistance[^1] } : [byDistance[0]];
        DiagnosticLog.Info($"[DmuTrine] Wings busters on {string.Join(",", targets.Select(t => ((ISimPartyMember)t).Role))}");
        foreach (var target in targets) Helper(new Vector2(0f, -10f))?.Cast(DmuTrineActions.WingsBuster, target);
    }

    private void UltimateEmbrace()
    {
        if (kefka == null || Alive(PartyRole.MainTank) is not { } tank) return;
        tank.AttachLockonVfx(LockonId.UltimateEmbrace);
        kefka.Cast(DmuTrineActions.UltimateEmbrace, tank);
    }
}
