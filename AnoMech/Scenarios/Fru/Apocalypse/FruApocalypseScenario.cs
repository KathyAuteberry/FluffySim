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
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.Apocalypse;

public sealed class FruApocalypseScenario : IScenario
{
    public string Name=>"Apocalypse (Mana)";
    public string DisplayNameOverride=>"P3 Apocalypse";
    public bool IsIncomplete=>true;
    public bool EnablePhaseBgm=>true;
    public IPhase Phase=>FruZone.P3;
    public IReadOnlyList<IScenarioAi> AiStrats=>[new FruApocalypseAi()];
    public FruApocalypseStateOverrides Overrides { get; }=new();
    public object SettingsOverrides=>Overrides;
    public FruApocalypseState? LastState { get; private set; }
    public bool Completed { get; private set; }
    public int ResolvedWaves { get; private set; }
    public int ResolvedWaters { get; private set; }
    public PartyRole? DanceTarget { get; private set; }
    public Vector3 BossPosition=>boss?.Position??Vector3.Zero;
    private const float UnverifiedDanceLandingOffset=6.35f;
    private SimWorld world=null!;
    private SimEnemy boss=null!;
    private FruApocalypseState state=null!;
    private readonly List<SimEnemy> actors=[];
    private readonly SimEnemy[] helpers=new SimEnemy[64];
    private int helperIndex;
    public bool UsePracticeLightOverlay { get; set; }=true;
    private bool practiceLightOverlay=true;
    private readonly List<(float Time,uint Action)> resolutions=[];
    public IReadOnlyList<(float Time,uint Action)> Resolutions=>resolutions;
    public IReadOnlyList<Vector3> CurrentPulse { get; private set; }=[];
    public float LightElapsed { get; private set; }=-1;
    public IReadOnlyList<Vector3> LightPositions
    {
        get
        {
            if(!HasLightTelegraphs)return [];
            var lights=new List<Vector3>();
            for(var pair=0;pair<3;pair++)
            {
                var elapsed=LightElapsed-(pair==2?2:0);
                if(elapsed<0)continue;
                var outward=pair==0?1:Math.Clamp(elapsed/2,0,1);
                var travel=pair==0?elapsed:MathF.Max(0,elapsed-2);
                var p=FruApocalypseState.Direction((state.Axis+(state.Clockwise?1:-1)*travel/2)*MathF.PI/4)*14*outward;
                lights.Add(p);lights.Add(-p);
            }
            return lights;
        }
    }
    public bool HasLightTelegraphs=>LightElapsed>=0 && LightElapsed<10;
    public IReadOnlyList<Vector3> CurrentExplosion { get; private set; }=[];
    private float lastExplosion=-1;
    public void DrawSettings()
    {
        ImGui.TextWrapped("Mana Permaswap / Coloured Apocalypse — full sequence, native presentation pending playtest. Read the initial exterior lights; swaps remain permanent. Lights/trails use a local practice overlay. Use the local-only practice invulnerability button if baiting Darkest Dance.");
        if(ImGui.Checkbox("Local Apocalypse light/trail overlay",ref practiceLightOverlay))UsePracticeLightOverlay=practiceLightOverlay;
        ImGui.Checkbox("Advanced / Exact Pattern Overrides",ref advanced);Overrides.Advanced=advanced;
        if(!advanced)
        {
            var water=Overrides.PracticeWater is { } w?(int)w+1:0;
            if(ImGui.Combo("Practice Water Assignment",ref water,"Random\0No Water\0Short Water\0Medium Water\0Long Water\0"))Overrides.PracticeWater=water==0?null:(ApocalypseWater)(water-1);
            var swap=(int)Overrides.SwapPractice;
            if(ImGui.Combo("Swap Practice",ref swap,"Random\0Force player swap (H2/D4 ineligible)\0Force no player swap\0"))Overrides.SwapPractice=(ApocalypseSwapPractice)swap;
        }
        var rotation=Overrides.Clockwise is null?0:Overrides.Clockwise.Value?1:2;
        if(ImGui.Combo("Apocalypse Rotation",ref rotation,"Random\0Clockwise\0Counterclockwise\0"))Overrides.Clockwise=rotation==0?null:rotation==1;
        if(!advanced)return;
        Overrides.WaterPermutation=Slider("Water permutation",Overrides.WaterPermutation,2519);
        Overrides.InitialAxis=Slider("First lights N/S, NE/SW, E/W, SE/NW",Overrides.InitialAxis,3);
        var spirit=Overrides.SpiritTarget is { } t?(int)t:-1;
        if(ImGui.SliderInt("Spirit Taker seat (-1 random)",ref spirit,-1,7))Overrides.SpiritTarget=spirit<0?null:(PartyRole)spirit;
        var tank=Overrides.BaitTank is { } b?(int)b:-1;
        if(ImGui.SliderInt("Dance bait seat (-1 PLD > ST; exact must match priority)",ref tank,-1,1))Overrides.BaitTank=tank<0?null:(PartyRole)tank;
    }
    private bool advanced;
    private static int? Slider(string label,int? value,int max) { var v=value??-1;return ImGui.SliderInt(label+" (-1 random)",ref v,-1,max)?v<0?null:v:value; }
    public IReadOnlyList<string> SettingsConflicts=>!Overrides.Advanced && Overrides.SwapPractice==ApocalypseSwapPractice.Swap && (Plugin.MainWindow?.SelectedRoleOverride ?? Plugin.Game.World.Party.PlayerRole) is PartyRole.ShieldHealer or PartyRole.CasterDps ? ["H2 and D4 are lowest priority and never swap."] : [];
    public void Run(SimWorld worldParam,int? selectedAi)
    {
        world=worldParam;world.PracticeInvulnerability.Start();Completed=false;ResolvedWaters=0;ResolvedWaves=0;DanceTarget=null;actors.Clear();resolutions.Clear();CurrentPulse=[];CurrentExplosion=[];LightElapsed=-1;
        var paladin=world.Party.ActiveMembers().FirstOrDefault(m=>m.Proxy?.ClassJob==19);
        state=LastState=new(world.Rng,Overrides,world.Party.PlayerRole,paladin is ISimPartyMember pm?pm.Role:PartyRole.OffTank);
        helperIndex=0;
        world.Events.Add(0,()=>
        {
            boss=Spawn(FruConstants.BNpcBaseId.OracleOfDarkness,Vector3.Zero,true);
            for(var i=0;i<helpers.Length;i++)helpers[i]=Spawn(FruConstants.BNpcBaseId.Helper,Vector3.Zero);
        });
        world.Events.Add(2.1f,()=>boss.Cast(FruApocalypseActions.Refrain));
        world.Events.Add(7.2f,()=>
        {
            boss.Cast(FruApocalypseActions.WaterCast);
            foreach(var role in FruApocalypseState.Roles.Where(r=>state.Order(r)>0))world.Party.Get(role)!.AttachLockonVfx(62,43.7f,persistent:false);
        });
        world.Events.Add(12.2f,()=>
        {
            foreach(var role in FruApocalypseState.Roles.Where(r=>state.Order(r)>0))
            {var target=world.Party.Get(role)!;Helper(target.Position).Cast(new EnemyAction(40272),target);}
        });
        world.Events.Add(12.8f,AssignWater);
        world.Events.Add(15.36f,()=>boss.Cast(FruApocalypseActions.Apocalypse));
        world.Events.Add(17.78f,()=>ShowStack(1));
        world.Events.Add(20.11f,()=>world.Map.AddEffect(0x00200010,0x29));
        world.Events.Add(21.5f,()=>boss.Cast(FruApocalypseActions.SpiritCast));
        world.Events.Add(22,()=>LightElapsed=0);
        world.Events.Add(22.87f,()=>ResolveWater(1));
        world.Events.Add(24.95f,ResolveSpirit);
        world.Events.Add(26,()=>boss.MoveTo(Vector3.Zero,12));
        world.Events.Add(31.03f,()=>
        {
            boss.Cast(FruApocalypseActions.EruptionCast);
            foreach(var member in world.Party.ActiveMembers())member.AttachLockonVfx(139,5,persistent:false);
        });
        world.Events.Add(33.28f,()=>ResolveWave(0));
        world.Events.Add(35.29f,()=>ResolveWave(1));
        world.Events.Add(35.78f,()=>ShowStack(2));
        world.Events.Add(36.03f,ResolveSpreads);
        world.Events.Add(37.3f,()=>ResolveWave(2));
        world.Events.Add(38.65f,()=>boss.Cast(FruApocalypseActions.DanceCast));
        world.Events.Add(39.31f,()=>ResolveWave(3));
        world.Events.Add(41.32f,()=>ResolveWave(4));
        world.Events.Add(41.86f,()=>ResolveWater(2));
        world.Events.Add(43.33f,()=>ResolveWave(5));
        world.Events.Add(44.1f,ResolveDance);
        world.Events.Add(45.23f,()=>world.Map.AddEffect(0x00080001,0x29));
        world.Events.Add(45.78f,()=>ShowStack(3));
        world.Events.Add(46.9f,()=> { resolutions.Add((world.Events.Elapsed,40183));boss.Cast(FruApocalypseActions.Knockback); });
        world.Events.Add(50.86f,()=>ResolveWater(3));
        world.Events.Add(51.2f,()=>boss.Cast(FruApocalypseActions.Pulsar));
        world.Events.Add(58,()=> { Cleanup();Completed=true; });
        if(selectedAi is { } index)((IScenarioAi<FruApocalypseState>)AiStrats[index]).Run(state,world);
    }
    private SimEnemy Spawn(uint id,Vector3 position,bool visible=false)
    {
        var actor=world.SpawnEnemy(new EnemySpawnConfig(id,FruConstants.BNpcNameId.OracleOfDarkness,Level:100,Targetable:visible,
            EnemyList:visible?EnemyListMode.Always:EnemyListMode.Never,Visibility: visible ? SpawnVisibility.Visible : SpawnVisibility.InvisibleHelper,Placement:new(position,MathF.PI),
            Appearance:visible?new NpcAppearance(65537,4295237459):null))??throw new InvalidOperationException("Could not spawn Apocalypse actor.");
        actors.Add(actor);return actor;
    }
    private SimEnemy Helper(Vector3 position)
    {
        var helper=helpers[helperIndex++];helper.SetPosition(position);return helper;
    }
    private void AssignWater()
    {
        foreach(var role in FruApocalypseState.Roles)
            if(state.Order(role)>0)
            {
                var member=world.Party.Get(role)!;
                member.AddStatus(2461,state.Order(role) switch {1=>10,2=>29,_=>38});

            }
    }
    private void ShowStack(int order)
    {
        foreach(var role in FruApocalypseState.Roles.Where(r=>state.Order(r)==order))world.Party.Get(role)!.AttachLockonVfx(184,5.1f,persistent:false);
    }
    private void ResolveWater(int order)
    {
        var targets=FruApocalypseState.Roles.Where(r=>state.Order(r)==order).Select(r=>world.Party.Get(r)!).ToArray();
        foreach(var member in world.Party.ActiveMembers())
            if(targets.All(t=>Vector3.Distance(t.Position,member.Position)<=6))member.Die(40271,"Overlapping Water stacks");
        foreach(var role in FruApocalypseState.Roles.Where(r=>state.Order(r)==order))
        {
            var target=world.Party.Get(role)!;target.RemoveStatus(2461);

            var helper=Helper(target.Position);helper.Cast(FruApocalypseActions.Water with
            {
                Effects=FruApocalypseActions.Water.Effects.Append(new ResolveEffect(ctx=>
                {if(ctx.Hits.Count!=4)foreach(var hit in ctx.Hits)ctx.Kill(hit,"Water requires exactly four players");})).ToArray(),
            },target);
        }
        ++ResolvedWaters;resolutions.Add((world.Events.Elapsed,40271));
    }
    private void ResolveSpirit()
    {
        var target=world.Party.Get(state.SpiritTarget)!;
        boss.SetPosition(target.Position);boss.Cast(FruApocalypseActions.Spirit,target);
        resolutions.Add((world.Events.Elapsed,40289));
    }
    private void ResolveSpreads()
    {
        foreach(var member in world.Party.ActiveMembers())Helper(member.Position).Cast(FruApocalypseActions.Eruption,member);
        resolutions.Add((world.Events.Elapsed,40274));
    }
    private void ResolveWave(int wave)
    {
        CurrentExplosion=state.Wave(wave);lastExplosion=world.Events.Elapsed;
        foreach(var p in CurrentExplosion)Helper(p).Cast(FruApocalypseActions.Explosion);
        ++ResolvedWaves;resolutions.Add((world.Events.Elapsed,40297));
    }
    private void ResolveDance()
    {
        var target=world.Party.ActiveMembers().MaxBy(m=>Vector3.DistanceSquared(m.Position,boss.Position))!;
        DanceTarget=((ISimPartyMember)target).Role;
        var position=target.Position;
        foreach(var hit in world.Party.Find.InsideCircle(position,8))
            if(hit!=target || !((ISimPartyMember)target).Role.IsTank() || !(target == world.Party.Player && world.PracticeInvulnerability.Active) && !new ushort[]{82,409,810,1836}.Any(target.HasStatus))hit.Die(40182,"Darkest Dance requires an isolated invulnerable tank");
        var direction=Vector3.Normalize(position);boss.SetPosition(direction*MathF.Max(0,position.Length()-UnverifiedDanceLandingOffset));boss.Face(target);
        boss.Cast(FruApocalypseActions.Dance,target);
        resolutions.Add((world.Events.Elapsed,40182));
    }
    public void Tick(float delta,float elapsed)
    {
        if(Completed)return;
        if(elapsed>=22 && elapsed<32)
        {
            LightElapsed=elapsed-22;
            CurrentPulse=state.Wave(Math.Clamp((int)(LightElapsed/2),0,5));
        }
        else {LightElapsed=-1;CurrentPulse=[];}
        if(elapsed-lastExplosion>.3f)CurrentExplosion=[];
    }
    private sealed class ResolveEffect(Action<EnemyActionContext> effect) : IEnemyActionEffect
    { public void Apply(EnemyActionContext context, System.Collections.Generic.IReadOnlyList<SimCharacter> hits)=>effect(context); }
    private void Cleanup()
    {
        world.PracticeInvulnerability.Clear();
        foreach(var actor in actors)actor.Despawn();actors.Clear();
        foreach(var member in world.Party.ActiveMembers())member.RemoveStatus(2461);
        CurrentPulse=[];CurrentExplosion=[];LightElapsed=-1;
    }
}
