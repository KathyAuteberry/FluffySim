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

namespace AnoMech.Scenarios.Dsr.Sanctity2;

public sealed class DsrSanctity2Scenario : IScenario
{
    public string Name => "Sanctity of the Ward — Part 2: Meteors (Elemental)";
    public string DisplayNameOverride => "P2 Sanctity of the Ward (Part 2)";
    public bool IsIncomplete => true;
    public bool EnablePhaseBgm => true;
    public IPhase Phase => DsrZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrSanctity2Ai()];
    public DsrSanctity2StateOverrides Overrides { get; } = new();
    public object SettingsOverrides => Overrides;
    public DsrSanctity2State? LastState { get; private set; }
    public bool Completed { get; private set; }
    public int ResolvedTowers { get; private set; }
    public int ResolvedMeteors { get; private set; }
    public int FailedTowers { get; private set; }
    public IReadOnlyList<Vector3> CometPositions => comets;
    public sealed record TowerVisual(Vector3 Position, float Radius, float From, float Until, bool SecondWave);
    private readonly List<TowerVisual> towerVisuals = [];
    public IReadOnlyList<TowerVisual> ActiveTowerVisuals => towerVisuals.Where(t => world.Events.Elapsed >= t.From && world.Events.Elapsed < t.Until).ToArray();
    private readonly List<(SimOmen Omen, bool SecondWave)> nativeTowerOmens = [];
    public int ActiveNativeTowerCount => nativeTowerOmens.Count(t => t.Omen.IsActive);

    private SimWorld world = null!;
    private DsrSanctity2State state = null!;
    private SimEnemy hermenost = null!;
    private SimEnemy grinnaux = null!;
    private SimEnemy haumeric = null!;
    private SimEnemy charibert = null!;
    private SimEnemy noudenet = null!;
    private readonly List<SimEnemy> actors = [];
    private readonly List<SimEventObject> visuals = [];
    private readonly List<Vector3> comets = [];
    private readonly Dictionary<int, List<(SimEnemy Actor, bool Linked)>> pendingComets = [];
    private readonly List<(Vector3 Position,float From,float Until,uint Action)> puddles = [];

    public IReadOnlyList<string> SettingsConflicts
    {
        get
        {
            if (!Overrides.AdvancedMode) return [];
            if (Overrides.TargetPair is < 0 or > 11) return ["Meteor target pair must be 0–11."];
            if (Overrides.TowerPattern is < 0 or > 809) return ["Outer tower pattern must be 0–809."];
            if (Overrides.InnerPattern is { } mask && (mask is < 1 or > 14 || BitOperations.PopCount((uint)mask) is not (2 or 3)))
                return ["Inner mask must select exactly two or three positions (NE/SE/SW/NW bits 1/2/4/8)."];
            if (Overrides.TowerPattern is { } p)
            {
                var pattern=DsrSanctity2State.Patterns[p];
                if (Overrides.Cursed is { } cursed && pattern.ForcedCursed != cursed) return ["Exact tower pattern conflicts with the selected cursed filter."];
                if (Overrides.InnerPattern is { } inner && BitOperations.PopCount((uint)inner)+pattern.OutsideCount != 8)
                    return ["Inner and outer patterns must total eight towers."];
            }
            return [];
        }
    }

    public void DrawSettings()
    {
        ImGui.TextWrapped("EXPERIMENTAL — log-verified Meteor impacts and geometry; Conviction uses native omen VFX; native appearance, hazard activation and pattern distribution still need client validation. Current Elemental pairs: N MT/D3, E H2/D2, S ST/D4, W H1/D1. Read the bots' fixed tower claims. Meteor players should Sprint; outside players use the local-only Practice Anti-Knockback button after meteor five. Hug the inner rim of inside towers.");
        var advanced = Overrides.AdvancedMode;
        if (ImGui.Checkbox("Use Advanced / Exact Pattern Overrides",ref advanced)) Overrides.AdvancedMode = advanced;
        ImGui.BeginDisabled(advanced);
        var practice = (int)Overrides.PracticeAssignment;
        if (ImGui.Combo("Practice Assignment",ref practice,"Random\0Meteor\0No Meteor\0")) Overrides.PracticeAssignment = (MeteorPractice)practice;
        ImGui.EndDisabled();
        var cursed = Overrides.Cursed is { } c ? c ? 2 : 1 : 0;
        if (ImGui.Combo("N/S tower difficulty",ref cursed,"Random\0Not forced cursed\0Forced cursed\0")) Overrides.Cursed = cursed == 0 ? null : cursed == 2;
        if (ImGui.CollapsingHeader("Advanced / Exact Pattern Overrides"))
        {
            ImGui.TextWrapped(advanced ? "Exact overrides active; practice selector inactive. Seed uses the normal scenario seed control." : "Stored exact patterns inactive.");
            ImGui.BeginDisabled(!advanced);
            Overrides.TargetPair = Slider("Meteor target pair",Overrides.TargetPair,11);
            Overrides.TowerPattern = Slider("Outer tower pattern",Overrides.TowerPattern,809);
            Overrides.InnerPattern = Slider("Inner tower bitmask NE/SE/SW/NW",Overrides.InnerPattern,14);
            if (Overrides.TargetPair is >= 0 and < 12) { var pair=DsrSanctity2State.TargetPairs[Overrides.TargetPair.Value]; ImGui.TextWrapped($"Targets: {SettingsGrid.RoleLabel(pair.One)}, {SettingsGrid.RoleLabel(pair.Two)}"); }
            if (Overrides.TowerPattern is >= 0 and < 810) { var pattern=DsrSanctity2State.Patterns[Overrides.TowerPattern.Value]; ImGui.TextWrapped($"Outer N/E/S/W masks: {string.Join(" / ",pattern.Outer)}. Bits: 1 CCW, 2 center, 4 CW. {pattern.OutsideCount} outside; forced cursed: {pattern.ForcedCursed}."); }
            ImGui.EndDisabled();
        }
        if (LastState is { } s) ImGui.TextWrapped($"Targets: {string.Join(", ",s.Targets.Select(SettingsGrid.RoleLabel))}; outer pattern {s.PatternIndex}; inner mask {s.InnerMask}.");
    }
    private static int? Slider(string label,int? value,int max) { var n=value ?? -1; ImGui.SliderInt(label+" (-1 random)",ref n,-1,max); return n < 0 ? null : n; }
    public void Run(SimWorld worldParam,int? selectedAi)
    {
        world=worldParam; world.PracticeAntiKnockback.Start(); state=LastState=new(world.Rng,Overrides,world.Party.PlayerRole);
        Completed=false; ResolvedTowers=0; ResolvedMeteors=0; FailedTowers=0;
        actors.Clear(); visuals.Clear(); comets.Clear(); puddles.Clear(); puddlePresentation.Clear();
        world.OnDespawn(Cleanup);
        world.Events.Add(0f,SpawnOpening);
        world.Events.Add(0f,() =>
        {
            foreach (var role in state.Targets) { var member=world.Party.Get(role)!; member.AddStatus(562,23); member.AttachLockonVfx(285,23,persistent:false); }
        });
        world.Events.Add(DsrSanctity2Timing.FirstTowerCast,() => { StartTowers(false); hermenost.Cast(DsrSanctity2Actions.FirstTowerCast); });
        world.Events.Add(DsrSanctity2Timing.CometCast,() => noudenet.Cast(DsrSanctity2Actions.CometCast));
        world.Events.Add(DsrSanctity2Timing.FireCast,() =>
        {
            charibert.Cast(DsrSanctity2Actions.FireCast);
            foreach (var q in Enumerable.Range(0,4)) Helper(FirePosition(q)).Cast(DsrSanctity2Actions.Fire);
            Helper(Vector3.Zero,3635).Cast(DsrSanctity2Actions.Donut);
        });
        world.Events.Add(DsrSanctity2Timing.StormCast,() => haumeric.Cast(DsrSanctity2Actions.StormCast));
        world.Events.Add(DsrSanctity2Timing.Fire,() =>
        {
            foreach (var q in Enumerable.Range(0,4)) AddPuddle(FirePosition(q),28591,false);
        });
        world.Events.Add(DsrSanctity2Timing.Storm,ResolveStorms);
        world.Events.Add(DsrSanctity2Timing.FirePresentation, () =>
        {
            foreach (var q in Enumerable.Range(0,4)) ShowPuddle(FirePosition(q), 0x1EB686, DsrSanctity2Timing.PuddleEnd-world.Events.Elapsed);
        });
        world.Events.Add(DsrSanctity2Timing.IcePresentation, () =>
        {
            foreach (var puddle in puddles.Where(p => p.Action == 25575))
                ShowPuddle(puddle.Position,0x1EB682,DsrSanctity2Timing.PuddleEnd-world.Events.Elapsed);
        });
        world.Events.Add(DsrSanctity2Timing.FirstTowers,() => ClearTowerOmens(false));
        world.Events.Add(DsrSanctity2Timing.SecondTowerCast,() => { StartTowers(true); hermenost.Cast(DsrSanctity2Actions.SecondTowerCast); });
        for(var i=0;i<7;++i)
        {
            var drop = i;
            world.Events.Add(DsrSanctity2Timing.Meteor(i)-DsrSanctity2Timing.MeteorSpawnLead,() => SpawnComets(drop));
            world.Events.Add(DsrSanctity2Timing.Meteor(i),() => ResolveComets(drop));
        }
        world.Events.Add(DsrSanctity2Timing.KnockbackCast,() => grinnaux.Cast(DsrSanctity2Actions.Knockback));
        world.Events.Add(23f,() => { foreach(var role in state.Targets) world.Party.Get(role)?.RemoveStatus(562); });
        world.Events.Add(DsrSanctity2Timing.SecondTowers,() => ClearTowerOmens(true));
        world.Events.Add(DsrSanctity2Timing.Complete,() => { Cleanup(); Completed=true; });
        if(selectedAi is { } index) ((IScenarioAi<DsrSanctity2State>)AiStrats[index]).Run(state,world);
    }
    private SimEnemy Spawn(uint id,Vector3 position)
    {
        var actor=world.SpawnEnemy(DsrActors.Config(id,new(position,MathF.Atan2(-position.X,-position.Z)))) ?? throw new InvalidOperationException("Could not create Meteor actor.");
        actors.Add(actor);return actor;
    }
    private SimEnemy Helper(Vector3 position, uint name = 3639)
    {
        var actor=world.SpawnEnemy(new(9020,name,Level:90,EnemyList:EnemyListMode.Never,Visibility: SpawnVisibility.InvisibleHelper,Placement:new(position,0))) ?? throw new InvalidOperationException("Could not create Meteor helper.");
        actors.Add(actor);return actor;
    }
    private void SpawnOpening()
    {
        Spawn(12604,new(0,0,-19.02f));
        haumeric=Spawn(12638,new(-6,0,0)); charibert=Spawn(12603,new(0,0,-6));
        hermenost=Spawn(12636,new(0,0,6)); noudenet=Spawn(12639,new(6,0,0)); grinnaux=Spawn(12602,Vector3.Zero);
    }
    private void StartTowers(bool second)
    {
        var indices=second ? Enumerable.Range(0,8) : state.TowerIndices;
        foreach(var index in indices)
        {
            var position=second ? DsrSanctity2State.Direction(index*MathF.PI/4)*18f : DsrSanctity2State.TowerPosition(index);
            towerVisuals.Add(new(position,3,world.Events.Elapsed,
                (second ? DsrSanctity2Timing.SecondTowers : DsrSanctity2Timing.FirstTowers),second));
            var actor=Helper(position,3640);
            var action=second ? DsrSanctity2Actions.SecondTower : DsrSanctity2Actions.FirstTower;
            nativeTowerOmens.Add((world.SpawnOmen(action.ActionId,new(position,0)),second));
            actor.Cast(action with { Effects = action.Effects.Append(new ResolveEffect(ctx =>
            {
                ++ResolvedTowers;
                if (ctx.Hits.Count != 1) { ++FailedTowers; ctx.AfterResolve(() => world.Party.WipeAllPlayers(action.ActionId,"Tower needs exactly one player")); }
                ctx.AfterResolve(() => world.Events.Add(.9f,actor.Despawn));
            })).ToArray() });
        }
    }
    private void ClearTowerOmens(bool second)
    {
        foreach (var tower in nativeTowerOmens.Where(t => t.SecondWave == second)) tower.Omen.Despawn();
        nativeTowerOmens.RemoveAll(t => t.SecondWave == second);
    }
    public static Vector3 FirePosition(int quadrant) => DsrSanctity2State.Direction((quadrant*90+45)*MathF.PI/180)*12f;
    private void ResolveStorms()
    {
        foreach(var role in DsrSanctity2State.Roles.Where(state.IsMeteorRole))
        {
            var target=world.Party.Get(role)!; Helper(target.Position).Cast(DsrSanctity2Actions.Storm,target);
            AddPuddle(target.Position,25575,true);
        }
    }
    private void AddPuddle(Vector3 position,uint action,bool ice)
    {
        puddles.Add((position,ice ? DsrSanctity2Timing.IceActivation : DsrSanctity2Timing.FirePresentation,DsrSanctity2Timing.PuddleSafe,action));
    }
    public IReadOnlyList<(float Time, uint Object)> PuddlePresentation => puddlePresentation;
    private readonly List<(float, uint)> puddlePresentation = [];
    private void ShowPuddle(Vector3 position, uint id, float lifetime)
    {
        puddlePresentation.Add((world.Events.Elapsed, id));
        var visual=world.SpawnEventObject(new EventObjectSpawnConfig { EObjId=id,Placement=new(position,0),Lifetime=lifetime });
        if(visual is not null) visuals.Add(visual);
    }
    private void SpawnComets(int drop)
    {
        var pending = pendingComets[drop] = [];
        foreach(var role in state.Targets)
        {
            var position=world.Party.Get(role)!.Position;
            var linked=comets.Any(p => Vector3.Distance(p,position) < 5);
            pending.Add((Spawn(12591,position),linked));
            comets.Add(position);
        }
    }
    private void ResolveComets(int drop)
    {
        foreach(var (comet,linked) in pendingComets[drop])
        {
            comet.Cast(linked ? DsrSanctity2Actions.Impact : DsrSanctity2Actions.Comet);
            if(linked) world.Party.WipeAllPlayers(25578,"Meteors linked: less than five yalms apart");
            ++ResolvedMeteors;
        }
        pendingComets.Remove(drop);
    }
    public void Tick(float delta,float elapsed)
    {
        if(Completed)return;
        foreach(var puddle in puddles.Where(p => elapsed>=p.From && elapsed<p.Until))
            foreach(var member in world.Party.Find.InsideCircle(puddle.Position,7)) member.Die(puddle.Action,"Entered an active persistent puddle");
    }
    private void Cleanup()
    {
        world.PracticeAntiKnockback.Clear();
        ClearTowerOmens(false); ClearTowerOmens(true);
        foreach(var actor in actors) actor.Despawn(); foreach(var visual in visuals) visual.Despawn();
        actors.Clear();visuals.Clear();puddles.Clear();comets.Clear();pendingComets.Clear();towerVisuals.Clear();
        foreach(var role in DsrSanctity2State.Roles) { world.Party.Get(role)?.RemoveStatus(562); world.Party.Get(role)?.RemoveStatus(2903); }
    }
    private sealed class ResolveEffect(Action<EnemyActionContext> resolve) : IEnemyActionEffect
    { public void Apply(EnemyActionContext ctx, System.Collections.Generic.IReadOnlyList<SimCharacter> hits) => resolve(ctx); }
}
