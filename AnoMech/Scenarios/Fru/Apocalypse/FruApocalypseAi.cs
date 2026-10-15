using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.Apocalypse;

public sealed class ApocalypseManaAssignments
{
    private readonly FruApocalypseState state;
    private readonly bool[] swapped;
    private static readonly int[] Priority = [4,5,0,1,6,2,7,3];
    public ApocalypseManaAssignments(FruApocalypseState state) { this.state=state;swapped=FruApocalypseState.Swappers(state.Water); }
    public bool Swapped(PartyRole role)=>swapped[(int)role];
    public int Group(PartyRole role)=>((int)role<4?0:1)^(swapped[(int)role]?1:0);
    public int Position(PartyRole role)=>Array.IndexOf(Priority.Where(i=>Group((PartyRole)i)==Group(role)).ToArray(),(int)role);
    private float Reference => (state.Axis<2?state.Axis:state.Axis+4)*MathF.PI/4;
    private float Safe => Reference-(state.Clockwise?1:-1)*MathF.PI/4;
    public Vector3 Spread(PartyRole role)
    {
        var slot=Position(role);var half=Group(role)*MathF.PI;
        var angle=Safe+half;
        if(slot<2) angle+=(state.Clockwise?slot==0?0:-1:slot==0?1:0)*MathF.PI/4;
        else angle+=(slot==2?1:-1)*MathF.Atan2(8,44);
        return FruApocalypseState.Direction(angle)*(slot<2?9.8f:18.95f);
    }
    public Vector3 Box(PartyRole role,bool initial=false)
    {
        var group=initial ? (int)role<4?0:1 : Group(role);
        var slot=initial ? (int)role%4 : Position(role);
        Vector3[] positions=[new(-.22f,0,-7.34f),new(-5.04f,0,-5.34f),new(-2.29f,0,-12.42f),new(-7.17f,0,-10.40f)];
        return positions[slot]*(group==0?1:-1);
    }
    public Vector3 FirstStack(PartyRole role)=>new Vector3(-3.5f,0,-8)*(Group(role)==0?1:-1);
    public Vector3 Corridor(PartyRole role)=>FruApocalypseState.Direction(Safe+Group(role)*MathF.PI)*(Position(role)<2?4.2f:14f);
    public Vector3 SecondStack(PartyRole role)
    {
        var angle=state.Clockwise?Safe:(state.Axis-3)*MathF.PI/4;
        return FruApocalypseState.Direction(angle+Group(role)*MathF.PI)*4.3f;
    }
    public Vector3 DanceBait()
    {
        var angle=(state.Clockwise ? (state.Axis<2?state.Axis:state.Axis+4)-2 : state.Axis-2)*MathF.PI/4;
        return FruApocalypseState.Direction(angle+(Swapped(state.BaitTank)?MathF.PI:0))*18f;
    }
    public Vector3 BehindBoss(PartyRole role,Vector3 boss,float distance)
    {
        var axis=MathF.Atan2(boss.X,-boss.Z);
        return boss-FruApocalypseState.Direction(axis+(Group(role)==0?MathF.PI/6:-MathF.PI/6))*distance;
    }
}

public sealed class FruApocalypseAi : IScenarioAi<FruApocalypseState>
{
    public string Name=>"Mana Permaswap / Coloured Apocalypse";
    public string Group=>"JP";
    public void Run(FruApocalypseState state,SimWorld world)
    {
        var assignments=new ApocalypseManaAssignments(state);
        void Move(Func<PartyRole,Vector3> position,float? deadline=null)
        { foreach(var role in FruApocalypseState.Roles) world.BotMovement.Move(world.Party.Get(role),position(role),deadline); }
        world.Events.Add(.5f,()=>Move(r=>assignments.Box(r,true),12f));
        world.Events.Add(14f,()=>Move(r=>assignments.Box(r),19f));
        world.Events.Add(19.6f,()=>Move(assignments.FirstStack));
        world.Events.Add(22.96f,()=>Move(r=>assignments.Box(r)));
        world.Events.Add(26f,()=>Move(assignments.Spread,30.1f));
        world.Events.Add(36.12f,()=>Move(assignments.Corridor));
        world.Events.Add(37f,()=>Move(assignments.SecondStack));
        world.Events.Add(42.04f,()=>
        {
            var member=world.Party.Get(state.BaitTank)!;
            ((ISimPartyMember)member).UseSprint(10);member.MoveTo(assignments.DanceBait(),7.8f);
        });
        world.Events.Add(43.7f,()=>
        {
            var member=world.Party.Get(state.BaitTank)!;
            if(!world.Party.IsBotDriven(member))return;
            var job=member.Proxy?.ClassJob;
            var (action,status)=job switch {19=>(30u,(ushort)82),21=>(43u,(ushort)409),32=>(3638u,(ushort)810),_=>(16152u,(ushort)1836)};
            if(member is SimPartyNpc npc)npc.PlayAction(action);member.AddStatus(status,10);
        });
        world.Events.Add(44.65f,()=>Move(r=>assignments.BehindBoss(r,((FruApocalypseScenario)Plugin.Game.ActiveScenario!).BossPosition,4.65f)));
        world.Events.Add(47.7f,()=>Move(r=>assignments.BehindBoss(r,((FruApocalypseScenario)Plugin.Game.ActiveScenario!).BossPosition,7.8f)));
    }
}
