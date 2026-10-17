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
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dsr.Sanctity1;

public sealed class DsrSanctity1Scenario : IScenario
{
    private sealed class ResolveEffect(Action<EnemyActionContext> resolve) : IEnemyActionEffect
    {
        public void Apply(EnemyActionContext ctx, System.Collections.Generic.IReadOnlyList<SimCharacter> hits) => resolve(ctx);
    }
    public string Name => "Sanctity of the Ward — Part 1: Sacred Severs (Elemental)";
    public string DisplayNameOverride => "P2 Sanctity of the Ward (Part 1)";
    public IPhase Phase => DsrZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrSanctity1Ai()];
    public DsrSanctity1StateOverrides Overrides { get; } = new();
    public object SettingsOverrides => Overrides;
    public DsrSanctity1State? LastState { get; private set; }
    public bool Completed { get; private set; }
    public int ResolvedJumps { get; private set; }
    public int ResolvedFlares { get; private set; }
    public int ResolvedCharges { get; private set; }
    public IReadOnlyList<float> JumpDistances => jumpDistances;
    public IReadOnlyList<string> SettingsConflicts => Overrides.AdvancedMode && Overrides.Sword1 is { } one && Overrides.Sword2 == one
        ? ["Sword 1 and Sword 2 must target distinct players."] : [];
    public static readonly float[] JumpTimes = [17.55f,19.33f,21.11f,22.90f];
    public static readonly float[] ChargeTimes = [17.67f,19.235f,20.795f];
    public static readonly float[] OrbSpawnTimes = [18.22f,18.64f,19.00f,19.80f,20.17f,20.56f,21.37f,21.74f,22.14f];
    public static readonly float[] OrbCastTimes = [19.04f,19.35f,19.71f,20.645f,20.96f,21.315f,22.205f,22.52f,22.875f];
    private SimWorld world = null!;
    private DsrSanctity1State state = null!;
    private SimEnemy boss = null!;
    private SimEnemy zephirin = null!;
    private readonly List<SimEnemy> actors = [];
    private readonly SimEnemy[] knights = new SimEnemy[2];
    private readonly SimEnemy?[,] orbs = new SimEnemy?[2,9];
    private readonly List<float> jumpDistances = [];
    private SimEnemy glory = null!;

    public void DrawSettings()
    {
        ImGui.TextWrapped("Practice Sacred Severs through all four jumps. Read the swords, swap with your role partner when needed, and look away from both gazes. Wait for the orb ahead to explode before rotating in the direction the knights face.");
        var advanced = Overrides.AdvancedMode;
        if (ImGui.Checkbox("Use Advanced / Exact Pattern Overrides", ref advanced)) Overrides.AdvancedMode = advanced;
        var rotation = Overrides.Clockwise is { } cw ? cw ? 1 : 2 : 0;
        if (ImGui.Combo("Rotation", ref rotation,"Random\0Clockwise\0Counterclockwise\0")) Overrides.Clockwise = rotation == 0 ? null : rotation == 1;
        ImGui.BeginDisabled(advanced);
        var assignment = (int)Overrides.PracticeAssignment;
        if (ImGui.Combo("Practice Assignment",ref assignment,"Random\0Sword 1\0Sword 2\0Unmarked — stay\0Unmarked — role-partner swap\0"))
            Overrides.PracticeAssignment = (SacredSeverPractice)assignment;
        ImGui.EndDisabled();
        if (ImGui.CollapsingHeader("Advanced / Exact Pattern Overrides"))
        {
            ImGui.TextWrapped(advanced ? "Exact positions and swords active; Practice Assignment inactive." : "Stored exact patterns inactive. Rotation applies in either mode.");
            ImGui.BeginDisabled(!advanced);
            var zeph = (Overrides.ZephirinPosition ?? -1)+1;
            if (ImGui.Combo("Zephirin",ref zeph,"Random\0NE\0SE\0SW\0NW\0")) Overrides.ZephirinPosition = zeph == 0 ? null : zeph-1;
            var thordan = (Overrides.ThordanPosition ?? -1)+1;
            if (ImGui.Combo("Thordan",ref thordan,"Random\0N\0NE\0E\0SE\0S\0SW\0W\0NW\0")) Overrides.ThordanPosition = thordan == 0 ? null : thordan-1;
            var eye = (Overrides.EyeSeparation ?? -1)+1;
            if (ImGui.Combo("Eye from Thordan",ref eye,"Random\0+90°\0+135°\0180°\0-135°\0-90°\0")) Overrides.EyeSeparation = eye == 0 ? null : eye-1;
            Overrides.Sword1 = RoleCombo("Sword 1",Overrides.Sword1);
            Overrides.Sword2 = RoleCombo("Sword 2",Overrides.Sword2);
            ImGui.EndDisabled();
        }
        if (LastState is { } s) ImGui.TextWrapped($"Sword 1: {SettingsGrid.RoleLabel(s.Sword1)}; Sword 2: {SettingsGrid.RoleLabel(s.Sword2)}. Swaps: {string.Join(", ", DsrSanctity1State.Roles.Where(s.Swaps).Select(SettingsGrid.RoleLabel))}.");
    }
    private static PartyRole? RoleCombo(string label, PartyRole? role)
    {
        var index = (int)(role ?? (PartyRole)(-1))+1;
        if (ImGui.Combo(label,ref index,"Random\0MT\0ST\0H1\0H2\0D1\0D2\0D3\0D4\0")) return index == 0 ? null : (PartyRole)(index-1);
        return role;
    }
    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam; state = LastState = new(world.Rng, Overrides, world.Party.PlayerRole);
        Completed = false; ResolvedJumps = 0; ResolvedFlares = 0; ResolvedCharges = 0;
        actors.Clear(); jumpDistances.Clear(); Array.Clear(orbs);
        world.Events.Add(0f, () => { boss = Spawn(12604,Vector3.Zero,0); boss.Cast(DsrSanctity1Actions.Sanctity); });
        world.Events.Add(7.1f, () => { boss.SetPosition(new Placement(state.ThordanSpawn, MathF.Atan2(-state.ThordanSpawn.X,-state.ThordanSpawn.Z))); });
        world.Events.Add(8.29f, SpawnActors);
        world.Events.Add(9.45f, () =>
        {
            world.Party.Get(state.Sword1)?.AttachLockonVfx(50,15f,persistent:false);
            world.Party.Get(state.Sword2)?.AttachLockonVfx(51,15f,persistent:false);
        });
        world.Events.Add(12.62f, () => boss.Cast(DsrSanctity1Actions.GazeCast));
        world.Events.Add(17.74f, () =>
        {
            boss.Cast(DsrSanctity1Actions.Gaze); glory.Cast(DsrSanctity1Actions.Glory);
            world.Map.AddEffect(0x00100001,(byte)state.EyePosition);
        });
        world.Events.Add(18.67f, () => world.Map.AddEffect(0x00400020,(byte)state.EyePosition));
        for (var i = 0; i < JumpTimes.Length; ++i)
        {
            var jump = i;
            world.Events.Add(JumpTimes[i], () => Jump(jump));
        }
        for (var i = 0; i < ChargeTimes.Length; ++i)
        {
            var charge = i;
            world.Events.Add(ChargeTimes[i], () => Charge(charge));
        }
        for (var i = 0; i < OrbCastTimes.Length; ++i)
        {
            var orb = i;
            world.Events.Add(OrbSpawnTimes[i], () => SpawnOrbs(orb));
            world.Events.Add(OrbCastTimes[i], () =>
            {
                for (var side = 0; side < 2; ++side) orbs[side,orb]!.Cast(DsrSanctity1Actions.Flare with
                { Effects = [Damage(Severity.Lethal), new ResolveEffect(_ => ++ResolvedFlares)] });
            });
            world.Events.Add(OrbCastTimes[i]+3.1f, () => { for (var side=0;side<2;++side) orbs[side,orb]?.Despawn(); });
        }
        world.Events.Add(26f, () => { foreach (var actor in actors) actor.Despawn(); actors.Clear(); Completed = ResolvedJumps==4 && ResolvedFlares==18 && ResolvedCharges==6; });
        if (selectedAi is { } index) ((IScenarioAi<DsrSanctity1State>)AiStrats[index]).Run(state,world);
    }
    private SimEnemy Spawn(uint id, Vector3 position, float rotation)
    {
        var actor = world.SpawnEnemy(DsrActors.Config(id,new(position,rotation))) ?? throw new InvalidOperationException("Could not create Sacred Severs actor.");
        actors.Add(actor); return actor;
    }
    private SimEnemy Helper(Vector3 position)
    {
        var actor = world.SpawnEnemy(new(9020,3632,Level:90,EnemyList:EnemyListMode.Never,Visibility: SpawnVisibility.InvisibleHelper,
            Placement:new(position,0))) ?? throw new InvalidOperationException("Could not create gaze source.");
        actors.Add(actor); return actor;
    }
    private void SpawnActors()
    {
        var map = world.Map;
        var eyeSlot = (byte)state.EyePosition;
        world.OnDespawn(() => map.AddEffect(0x00400020,eyeSlot));
        zephirin = Spawn(12592,state.ZephirinSpawn,MathF.Atan2(-state.ZephirinSpawn.X,-state.ZephirinSpawn.Z));
        for(var side=0;side<2;++side)
        {
            var east=side==1;
            var id = east == state.Clockwise ? 12601u : 12632u;
            var p=state.ChargePoints(east);
            knights[side]=Spawn(id,p[0],MathF.Atan2(p[1].X,p[1].Z));
        }
        glory=Helper(state.EyeSpawn);
        world.Map.AddEffect(0x00020001,(byte)state.EyePosition);
    }
    private void SpawnOrbs(int orb)
    {
        for(var side=0;side<2;++side) orbs[side,orb]=Spawn(13070,state.OrbPositions(side==1)[orb],0);
    }
    private void Jump(int jump)
    {
        var target=world.Party.Get(jump%2==0 ? state.Sword1 : state.Sword2)!;
        var distance=Vector3.Distance(zephirin.Position,target.Position);
        jumpDistances.Add(distance);
        zephirin.SetRotation(MathF.Atan2(target.Position.X-zephirin.Position.X,target.Position.Z-zephirin.Position.Z));
        zephirin.Cast(DsrSanctity1Actions.SeverAtDistance(distance),target);
        zephirin.CarryTo(target.Position);
        ++ResolvedJumps;
    }
    private void Charge(int charge)
    {
        for(var side=0;side<2;++side)
        {
            var points=state.ChargePoints(side==1); var from=points[charge]; var to=points[charge+1];
            var knight=knights[side];
            knight.SetRotation(MathF.Atan2(to.X-from.X,to.Z-from.Z));
            knight.Cast(DsrSanctity1Actions.Blade with
            {
                Area = new() { AdjustTargets = (ctx,_) => ctx.Party.ActiveMembers().Where(m => InsideCharge(m.Position,from,to)).ToArray() },
                Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0f },
            },to);
            knight.CarryTo(to);
            ++ResolvedCharges;
        }
    }
    public static bool InsideCharge(Vector3 position, Vector3 from, Vector3 to)
    {
        var delta=to-from; var length=delta.LengthSquared();
        if(length<.001f) return false;
        var fraction=Vector3.Dot(position-from,delta)/length;
        return fraction>=0 && fraction<=1 && Vector3.DistanceSquared(position,from+delta*fraction)<=9f;
    }
}
