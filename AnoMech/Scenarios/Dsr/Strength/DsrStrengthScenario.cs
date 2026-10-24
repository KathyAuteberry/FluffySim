using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dsr.Strength;

public sealed class DsrStrengthScenario : IScenario
{
    public bool SupportsMultiplayer => true;
    public string Name => "Strength of the Ward — Part 1 (Elemental)";
    public string DisplayNameOverride => "P2 Strength of the Ward 1";
    public IPhase Phase => DsrZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrStrengthAi()];
    public DsrStrengthStateOverrides Overrides { get; } = new();
    public object SettingsOverrides => Overrides;
    public DsrStrengthState? LastState { get; private set; }
    public bool Completed { get; private set; }
    private SimWorld world = null!;
    private DsrStrengthState state = null!;
    private SimEnemy boss = null!;
    private SimEnemy heavy = null!;
    private SimEnemy heavyHelper = null!;
    private readonly List<SimEnemy> actors = [];
    private readonly List<SimEnemy> knights = [];
    private readonly List<SimEnemy> additional = [];
    private readonly List<SimEnemy> lightning = [];
    private readonly List<SimEnemy> cones = [];

    public void DrawSettings()
    {
        ImGui.TextWrapped("Practice Spiral Thrust, Lightning Storm and all five Heavy Impacts. Read the knights, spread in the safe sectors, then follow the expanding impacts toward the center.");
        var axis = (Overrides.SafeAxis ?? -1) + 1;
        if (ImGui.Combo("Safe sectors", ref axis, "Random\0N / S\0NE / SW\0E / W\0SE / NW\0")) Overrides.SafeAxis = axis == 0 ? null : axis - 1;
        var heavy = (Overrides.HeavyCardinal ?? -1) + 1;
        if (ImGui.Combo("Heavy Impact", ref heavy, "Random\0North\0East\0South\0West\0")) Overrides.HeavyCardinal = heavy == 0 ? null : heavy - 1;
        var sides = Overrides.KnightSides ?? -1;
        if (ImGui.SliderInt("Knight sides (-1 random)", ref sides, -1, 7)) Overrides.KnightSides = sides < 0 ? null : sides;
        var pattern = Overrides.AdditionalPattern ?? -1;
        if (ImGui.SliderInt("Additional knights (-1 random)", ref pattern, -1, 7)) Overrides.AdditionalPattern = pattern < 0 ? null : pattern;
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        LastState = state = new(world.Rng, Overrides);
        Completed = false;
        actors.Clear(); knights.Clear(); additional.Clear(); lightning.Clear(); cones.Clear();
        world.Events.Add(0f, SpawnBoss);
        world.Events.Add(3f, () => boss.Cast(DsrActions.Strength));
        world.Events.Add(10.1f, () => boss.PlayActionTimeline(7737));
        world.Events.Add(11f, SpawnInitialKnights);
        world.Events.Add(11.41f, () =>
        {
            boss.PlayActionTimeline(7747);
            foreach (var actor in knights.Append(heavy)) actor.PlayActionTimeline(7747);
        });
        world.Events.Add(13.64f, () =>
        {
            boss.Cast(DsrActions.LightningCast);
            foreach (var knight in knights) knight.Cast(DsrActions.Spiral);
            heavy.Cast(DsrActions.HeavyCast);
            heavyHelper.Cast(DsrActions.Heavy(1));
        });
        world.Events.Add(19.81f, ResolveLightning);
        world.Events.Add(21.02f, () => heavy.PlayActionTimeline(4573));
        world.Events.Add(21.4f, SpawnAdditionalKnights);
        world.Events.Add(21.54f, () => heavyHelper.Cast(DsrActions.Heavy(2)));
        world.Events.Add(21.7f, () =>
        {
            foreach (var actor in additional) actor.PlayActionTimeline(7747);
        });
        world.Events.Add(21.71f, () => boss.Cast(DsrActions.MercyCast));
        world.Events.Add(23.41f, () => heavyHelper.Cast(DsrActions.Heavy(3)));
        world.Events.Add(24.78f, BaitMercy);
        world.Events.Add(25.28f, () => heavyHelper.Cast(DsrActions.Heavy(4)));
        world.Events.Add(27.15f, () => heavyHelper.Cast(DsrActions.Heavy(5)));
        world.Events.Add(30f, () =>
        {
            foreach (var actor in actors) actor.Despawn();
            actors.Clear();
            Completed = true;
        });
        if (selectedAi is { } index) ((IScenarioAi<DsrStrengthState>)AiStrats[index]).Run(state, world);
    }
    private SimEnemy Spawn(uint id, Vector3 position)
    {
        var rotation = MathF.Atan2(-position.X, -position.Z);
        var actor = world.SpawnEnemy(DsrActors.Config(id, new(position, rotation)))
            ?? throw new InvalidOperationException($"Could not create DSR actor {id}.");
        actors.Add(actor);
        return actor;
    }
    private SimEnemy Helper(Vector3 position, uint nameId = 3632)
    {
        var actor = world.SpawnEnemy(new(9020, nameId, Level: 90, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper, Placement: new(position, 0f)))
            ?? throw new InvalidOperationException("Could not create DSR effect helper.");
        actors.Add(actor);
        return actor;
    }
    private void SpawnBoss()
    {
        boss = Spawn(12604, Vector3.Zero);
        heavyHelper = Helper(state.HeavyPosition, 3641);
        for (var i = 0; i < 8; ++i) { lightning.Add(Helper(Vector3.Zero)); cones.Add(Helper(Vector3.Zero)); }
    }
    private void SpawnInitialKnights()
    {
        uint[] ids = [12633, 12634, 12635];
        for (var i = 0; i < 3; ++i) knights.Add(Spawn(ids[i], DsrStrengthState.Direction(state.KnightClocks[i]) * 23f));
        heavy = Spawn(12637, state.HeavyPosition);
    }
    public static IReadOnlyList<Vector3> AdditionalPositions(int pattern) => pattern switch
    {
        0 => [new(-9.6593f,0,2.5882f),new(2.5882f,0,-9.6593f),new(10.6066f,0,-10.6066f),new(-16.2635f,0,-16.2635f)],
        1 => [new(-5,0,-8.6603f),new(-5,0,8.6602f),new(0,0,-15),new(-23,0,0)],
        2 => [new(-2.5882f,0,-9.6593f),new(9.6593f,0,2.5882f),new(10.6066f,0,-10.6066f),new(16.2635f,0,-16.2635f)],
        3 => [new(8.6602f,0,5),new(-8.6603f,0,5),new(0,0,-15),new(0,0,23)],
        4 => [new(2.5882f,0,9.6593f),new(-9.6593f,0,-2.5882f),new(10.6066f,0,-10.6066f),new(-16.2635f,0,16.2635f)],
        5 => [new(9.6593f,0,-2.5882f),new(-2.5882f,0,9.6593f),new(10.6066f,0,-10.6066f),new(16.2635f,0,16.2635f)],
        6 => [new(8.6603f,0,-5),new(-8.6603f,0,-5),new(0,0,-15),new(0,0,-23)],
        7 => [new(5,0,-8.6603f),new(5,0,8.6603f),new(0,0,-15),new(23,0,0)],
        _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
    };
    private void SpawnAdditionalKnights()
    {
        uint[] ids = [12601, 12632, 12602, 12636];
        var positions = AdditionalPositions(state.AdditionalPattern);
        for (var i = 0; i < 4; ++i) additional.Add(Spawn(ids[i], positions[i]));
    }
    private void ResolveLightning()
    {
        for (var i = 0; i < 8; ++i)
            if (world.Party.Get((PartyRole)i) is { } target)
            {
                lightning[i].SetPosition(target.Position);
                lightning[i].Cast(DsrActions.Lightning, target);
            }
    }
    private void BaitMercy()
    {
        for (var i = 0; i < 8; ++i)
            if (world.Party.Get((PartyRole)i) is { } target)
            {
                cones[i].SetPosition(new Placement(Vector3.Zero, MathF.Atan2(target.Position.X, target.Position.Z)));
                cones[i].Cast(DsrActions.Mercy);
            }
    }
}
