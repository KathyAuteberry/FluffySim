using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dsr.Strength2;

public sealed class DsrStrength2Scenario : IScenario
{
    public string Name => "Strength of the Ward — Part 2 (Elemental)";
    public string DisplayNameOverride => "P2 Strength of the Ward 2";
    public IPhase Phase => DsrZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrStrength2Ai()];
    public DsrStrength2StateOverrides Overrides { get; } = new();
    public object SettingsOverrides => Overrides;
    public DsrStrength2State? LastState { get; private set; }
    public bool Completed { get; private set; }
    public int FailedTowers { get; private set; }
    private SimWorld world = null!;
    private DsrStrength2State state = null!;
    private SimEnemy boss = null!;
    private SimEnemy grinnaux = null!;
    private SimEnemy hermenost = null!;
    private SimEnemy rage = null!;
    private readonly List<SimEnemy> actors = [];
    private readonly List<SimEnemy> knights = [];
    private readonly List<SimEnemy> leapKnights = [];
    private readonly List<SimEnemy> towers = [];
    private readonly List<SimEnemy> collapses = [];
    private readonly List<SimEnemy> blades = [];
    private readonly List<SimTether> tethers = [];
    private readonly List<Placement> bashPositions = [];

    public void DrawSettings()
    {
        ImGui.TextWrapped("Read the bots' tower communication: they keep their choices even if you contest them.");
        var advanced = Overrides.UsesAdvancedPatterns;
        if (ImGui.Checkbox("Use Advanced / Exact Pattern Overrides", ref advanced)) Overrides.AdvancedMode = advanced;
        ImGui.BeginDisabled(advanced);
        var role = Plugin.MainWindow.SelectedRoleOverride ?? PartyPresets.SkipRoleForJob(Plugin.ObjectTable.LocalPlayer?.ClassJob.RowId ?? 0);
        if (role.IsTank())
            ImGui.TextWrapped("Practice Assignment: Tank — Elemental crossed tethers (MT west to east; ST east to west). Non-tank practice choices do not apply to this seat.");
        else
        {
            var assignment = (int)Overrides.PracticeAssignment;
            if (ImGui.Combo("Practice Assignment", ref assignment, "Random\0Skyward Leap — Any\0Skyward Leap — West\0Skyward Leap — East\0Skyward Leap — South\0Unmarked — Tethers / Stack / Towers\0"))
                Overrides.PracticeAssignment = (DsrStrength2PracticeAssignment)assignment;
        }
        ImGui.EndDisabled();
        if (ImGui.CollapsingHeader("Advanced / Exact Pattern Overrides"))
        {
            ImGui.TextWrapped(advanced ? "Exact overrides are active; Practice Assignment is inactive." : "Stored exact overrides are inactive. Enable Advanced mode to use them.");
            ImGui.BeginDisabled(!advanced);
            Overrides.KnightPattern = Slider("Central knight pattern", Overrides.KnightPattern, 7);
            Overrides.LeapTrio = Slider("Blue marker trio", Overrides.LeapTrio, 19);
            Overrides.LeapOrder = Slider("West / east / south marker order", Overrides.LeapOrder, 5);
            Overrides.TowerCommunication = Slider("Bot tower communication", Overrides.TowerCommunication, 5);
            Overrides.TetherTargets = Slider("Initial tether targets", Overrides.TetherTargets, 5);
            if (Overrides.LeapTrio is { } trio && trio >= 0 && trio < DsrStrength2State.LeapTrios.Length)
            {
                var roles = DsrStrength2State.LeapTrios[trio];
                if (Overrides.LeapOrder is { } order && order >= 0 && order < DsrStrength2State.Orders.Length)
                    ImGui.TextWrapped($"Blue W/E/S: {string.Join(" / ", DsrStrength2State.Orders[order].Select(i => SettingsGrid.RoleLabel(roles[i])))}");
                else ImGui.TextWrapped($"Blue players: {string.Join(", ", roles.Select(SettingsGrid.RoleLabel))}; spread order random.");
                var unmarked = DsrStrength2State.NonTanks.Except(roles).ToArray();
                if (Overrides.TowerCommunication is { } communication && communication >= 0 && communication < DsrStrength2State.Orders.Length)
                    ImGui.TextWrapped($"Tower claims NW/N/NE: {string.Join(" / ", DsrStrength2State.Orders[communication].Select(i => SettingsGrid.RoleLabel(unmarked[i])))} (relative to Thordan).");
                if (Overrides.TetherTargets is { } targets && targets >= 0 && targets < DsrStrength2State.Orders.Length)
                {
                    var assignments = DsrStrength2State.Orders[targets].Select(i => SettingsGrid.RoleLabel(unmarked[i])).ToArray();
                    ImGui.TextWrapped($"West tether: {assignments[0]}; east tether: {assignments[1]}; stack target: {assignments[2]}.");
                }
            }
            ImGui.EndDisabled();
        }
        if (LastState is { } s) ImGui.TextWrapped($"Blue W/E/S: {string.Join(", ", s.Leaps)}. Initial tethers: {string.Join(", ", s.InitialTetherTargets)}.");
    }
    private static int? Slider(string label, int? value, int max)
    {
        var n = value ?? -1;
        ImGui.SliderInt(label + " (-1 random)", ref n, -1, max);
        return n < 0 ? null : n;
    }
    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam; LastState = state = new(world.Rng, Overrides, world.Party.PlayerRole);
        Completed = false; FailedTowers = 0;
        actors.Clear(); knights.Clear(); leapKnights.Clear(); towers.Clear(); collapses.Clear(); blades.Clear(); tethers.Clear(); bashPositions.Clear();
        world.Events.Add(0f, SpawnOpening);
        world.Events.Add(3f, () => { foreach (var role in state.Leaps) world.Party.Get(role)!.AttachLockonVfx(330, 9.2f, persistent: false); });
        world.Events.Add(3.08f, () => { grinnaux.Cast(DsrStrength2Actions.CollapseCast); foreach (var helper in collapses) helper.Cast(DsrStrength2Actions.Collapse); });
        world.Events.Add(4.82f, () =>
        {
            hermenost.Cast(DsrStrength2Actions.TowerCast);
            foreach (var tower in towers) tower.Cast(DsrStrength2Actions.Tower with
            {
                Effects = DsrStrength2Actions.Tower.Effects.Append(new ResolveEffect(ctx =>
                {
                    if (ctx.Hits.Count != 1 || ((ISimPartyMember)ctx.Hits[0]).Role.IsTank()) ++FailedTowers;
                })).ToArray(),
            });
        });
        world.Events.Add(6.38f, () => boss.Cast(DsrStrength2Actions.RageCast));
        world.Events.Add(7.31f, () =>
        {
            for (var i = 0; i < 2; ++i) tethers.Add(world.Tether(knights[i], End.Passable(world.Party.Get(state.InitialTetherTargets[i]), exclusive: true), 84));
        });
        world.Events.Add(12.12f, () => { for (var i = 0; i < 3; ++i) leapKnights[i].Cast(DsrStrength2Actions.Leap, world.Party.Get(state.Leaps[i])); });
        world.Events.Add(12.48f, ResolveBashes);
        world.Events.Add(12.53f, () =>
        {
            var target = world.Party.Get(state.RageTarget)!;
            rage.SetPosition(target.Position); rage.Cast(DsrStrength2Actions.Rage, target);
        });
        world.Events.Add(13.19f, () => { foreach (var helper in blades) helper.Cast(DsrStrength2Actions.BashSecondary); });
        world.Events.Add(14.1f, () => { for (var i = 0; i < bashPositions.Count; ++i) knights[i].SetPosition(bashPositions[i]); });
        world.Events.Add(14.57f, () => { foreach (var knight in knights) knight.Cast(DsrStrength2Actions.BladeCast); });
        world.Events.Add(15.51f, ResolveBlades);
        world.Events.Add(15.96f, ResolveBlades);
        world.Events.Add(16.405f, ResolveBlades);
        world.Events.Add(16.85f, ResolveBlades);
        world.Events.Add(17.295f, ResolveBlades);
        world.Events.Add(17.74f, ResolveBlades);
        world.Events.Add(17.93f, () => { if (FailedTowers > 0) rage.Cast(DsrStrength2Actions.TowerFailure); });
        world.Events.Add(21f, () =>
        {
            foreach (var tether in tethers) tether.Despawn();
            foreach (var actor in actors) actor.Despawn();
            actors.Clear(); Completed = true;
        });
        if (selectedAi is { } index) ((IScenarioAi<DsrStrength2State>)AiStrats[index]).Run(state, world);
    }
    private SimEnemy Spawn(uint id, Vector3 position)
    {
        var actor = world.SpawnEnemy(DsrActors.Config(id, new(position, MathF.Atan2(-position.X, -position.Z))))
            ?? throw new InvalidOperationException("Could not create DSR knight.");
        actors.Add(actor); return actor;
    }
    private SimEnemy Helper(Vector3 position, uint name = 3632)
    {
        var actor = world.SpawnEnemy(new(9020, name, Level: 90, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
            Placement: new(position, 0))) ?? throw new InvalidOperationException("Could not create DSR effect helper.");
        actors.Add(actor); return actor;
    }
    private void SpawnOpening()
    {
        boss = Spawn(12604, state.Rotate(new(0,0,-23)));
        knights.Add(Spawn(state.WestKnightId, state.WestKnight)); knights.Add(Spawn(state.EastKnightId, state.EastKnight));
        grinnaux = Spawn(12602, state.AdditionalPositions[2]); hermenost = Spawn(12636, state.AdditionalPositions[3]);
        uint[] ids = [12633,12634,12635];
        for (var i = 0; i < 3; ++i) leapKnights.Add(Spawn(ids[i], state.Rotate(Strength.DsrStrengthState.Direction(i+1)*23)));
        foreach (var position in state.CollapsePositions) collapses.Add(Helper(position, 3639));
        foreach (var clock in new[] { 0,1,2,4,6,7 }) towers.Add(Helper(state.Rotate(Strength.DsrStrengthState.Direction(clock)*12),3640));
        blades.Add(Helper(Vector3.Zero,3634)); blades.Add(Helper(Vector3.Zero,3635)); rage = Helper(Vector3.Zero);
    }
    private void ResolveBashes()
    {
        for (var i = 0; i < 2; ++i)
        {
            var target = tethers[i].B;
            tethers[i].Despawn();
            if (target is null) continue;
            var position = new Placement(target.Position, MathF.Atan2(target.Position.X-knights[i].Position.X, target.Position.Z-knights[i].Position.Z));
            bashPositions.Add(position); blades[i].SetPosition(position);
            knights[i].Cast(DsrStrength2Actions.Bash, target);
        }
    }
    private void ResolveBlades() { foreach (var helper in blades) helper.Cast(DsrStrength2Actions.Blade); }
    private sealed class ResolveEffect(Action<EnemyActionContext> resolve) : IEnemyActionEffect
    {
        public void Apply(EnemyActionContext ctx, System.Collections.Generic.IReadOnlyList<SimCharacter> hits) => resolve(ctx);
    }
}
