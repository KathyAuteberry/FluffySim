using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.Sanctity2;

public sealed class DsrSanctity2Ai : IScenarioAi<DsrSanctity2State>
{
    public string Name => "Elemental cardinal pairs";
    public string Group => "JP";
    private static bool TakesSideTower(DsrSanctity2State state, PartyRole role)
        => state.FirstTowers[(int)role] < 12 && state.FirstTowers[(int)role]%3 != 1;

    internal static IReadOnlyList<Vector3> FirstTowerRoute(DsrSanctity2State state, PartyRole role)
    {
        var start = state.PairPosition(role);
        var end = state.FirstPosition(role);
        var control = Vector3.Normalize(start)*20f;
        return Enumerable.Range(0,9).Select(i =>
        {
            var t = i/8f;
            return (1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;
        }).ToArray();
    }

    internal static float MeteorSweep(DsrSanctity2State state, PartyRole role)
    {
        var other = state.Targets.Single(r => r != role);
        var start = state.FirstPosition(role);
        var end = state.FirstPosition(other);
        var angle = MathF.Atan2(end.X,-end.Z)-MathF.Atan2(start.X,-start.Z);
        return (angle+2*MathF.PI)%(2*MathF.PI);
    }

    internal static Vector3 MeteorPosition(DsrSanctity2State state, PartyRole role, int drop)
    {
        if (!state.IsMeteor(role) || drop is < 0 or > 6) throw new ArgumentOutOfRangeException(nameof(drop));
        var first = state.FirstPosition(role);
        var start = MathF.Atan2(first.X,-first.Z);
        var sweep = MathF.Min(162*MathF.PI/180,MeteorSweep(state,role)-16*MathF.PI/180);
        return DsrSanctity2State.Direction(start+sweep*drop/6)*19.8f;
    }

    internal static IReadOnlyList<Vector3> MeteorLeg(DsrSanctity2State state, PartyRole role, int drop)
    {
        var start = MeteorPosition(state,role,drop-1);
        var end = MeteorPosition(state,role,drop);
        var angle = MathF.Atan2(start.X,-start.Z);
        var sweep = MathF.Atan2(start.Z*end.X-start.X*end.Z,Vector3.Dot(start,end));
        sweep = MathF.Abs(sweep);
        var weave = MeteorSweep(state,role) < MathF.PI-.001f;
        return Enumerable.Range(0,17).Select(i =>
        {
            var t = i/16f;
            var heading = angle+sweep*t;
            var innerRim = 18.7f-1.1f*MathF.Cos(4*heading);
            var dodge = MathF.Sin(2*MathF.PI*t);
            var radius = weave ? 19.8f-MathF.Min(1.2f,19.8f-innerRim)*dodge*dodge : 19.8f;
            return DsrSanctity2State.Direction(heading)*radius;
        }).ToArray();
    }

    public void Run(DsrSanctity2State state, SimWorld world)
    {
        world.Events.Add(.1f, () => Each(role => Move(role,DsrSanctity2State.Direction(DsrSanctity2State.BaseQuadrants[(int)role]*MathF.PI/2)*10)));
        world.Events.Add(1.6f, () => Each(role => Move(role,state.PairPosition(role),3.4f)));
        world.Events.Add(3.7f, () => Each(role => Move(role,state.CommunicationPosition(role))));
        world.Events.Add(5.9f, () => Each(role => Move(role,state.PairPosition(role),7.5f)));
        world.Events.Add(8.05f, () => { foreach (var role in DsrSanctity2State.Roles.Where(r => !TakesSideTower(state,r))) Move(role,
            state.TakesKnockback(role) ? Vector3.Zero : DsrSanctity2State.Direction(state.Quadrant(role)*MathF.PI/2)*18f,
            9.75f,8.05f); });
        foreach (var role in DsrSanctity2State.Roles.Where(r => TakesSideTower(state,r)))
        {
            var route = FirstTowerRoute(state,role);
            var departure = 8.05f;
            for (var i = 1; i < route.Count; ++i)
            {
                var destination = route[i];
                world.Events.Add(departure, () => Move(role,destination));
                departure += Vector3.Distance(route[i-1],destination)/6f;
            }
        }
        world.Events.Add(9.8f, () => { foreach (var role in DsrSanctity2State.Roles.Where(r => !TakesSideTower(state,r))) Move(role,state.FirstPosition(role),11.7f); });
        world.Events.Add(11.85f, () =>
        {
            foreach (var role in state.Targets)
            {
                Move(role,Vector3.Normalize(state.FirstPosition(role))*19.8f);
            }
            foreach (var role in DsrSanctity2State.Roles.Where(r => !state.IsMeteor(r)))
                Move(role,state.TakesKnockback(role) ? state.KnockbackPosition(role) : Vector3.Normalize(state.FirstPosition(role))*19.8f);
        });
        world.Events.Add(11.85f, () => { foreach (var role in state.Targets) Move(role,MeteorPosition(state,role,0)); });
        for (var step = 1; step <= 6; ++step)
        {
            var fraction=step/6f;
            world.Events.Add(12.3f+step*.8f, () =>
            {
                foreach (var role in DsrSanctity2State.Roles.Where(r => !state.IsMeteor(r) && !state.TakesKnockback(r)))
                    Move(role,Vector3.Normalize(Vector3.Lerp(state.FirstPosition(role),state.FinalPosition(role),fraction))*19.8f);
            });
        }
        world.Events.Add(DsrSanctity2Timing.PuddleSafe+.05f, () =>
        {
            foreach (var role in DsrSanctity2State.Roles.Where(r => !state.IsMeteor(r) && !state.TakesKnockback(r))) Move(role,state.FinalPosition(role),DsrSanctity2Timing.SecondTowers-.5f);
        });
        world.Events.Add(11.8f, () => { foreach (var role in state.Targets) (world.Party.Get(role) as ISimPartyMember)?.UseSprint(10); });
        for (var i = 1; i < 7; ++i)
        {
            var drop = i;
            foreach (var role in state.Targets)
            {
                var route = MeteorLeg(state,role,drop);
                var departure = DsrSanctity2Timing.Meteor(i-1)-DsrSanctity2Timing.MeteorSpawnLead+.08f;
                for (var node = 1; node < route.Count; ++node)
                {
                    var destination = route[node];
                    world.Events.Add(departure, () => Move(role,destination));
                    departure += Vector3.Distance(route[node-1],destination)/7.8f;
                }
            }
        }
        world.Events.Add(21.5f, () =>
        {
            foreach (var role in DsrSanctity2State.Roles.Where(r => !state.TakesKnockback(r)))
            {
                var member = world.Party.Get(role);
                if (member is null || !world.Party.IsBotDriven(member)) continue;
                var caster = role is PartyRole.RegenHealer or PartyRole.ShieldHealer or PartyRole.CasterDps;
                if (member is SimNpc npc) npc.PlayAction(caster ? 7559u : 7548u); member?.AddStatus(caster ? (ushort)160 : (ushort)1209,6);
            }
        });
        world.Events.Add(DsrSanctity2Timing.Meteor(6)-DsrSanctity2Timing.MeteorSpawnLead+.08f, () => { foreach (var role in state.Targets) Move(role,state.FinalPosition(role)); });
        world.Events.Add(23f, () => { foreach (var role in DsrSanctity2State.Roles.Where(state.TakesKnockback)) Move(role,state.FinalPosition(role)); });
        void Each(Action<PartyRole> action) { foreach (var role in DsrSanctity2State.Roles) action(role); }
        void Move(PartyRole role, Vector3 position, float? deadline=null, float? leaveBy=null)
        { var member=world.Party.Get(role); world.BotMovement.Move(member,position,deadline,member?.HasStatus(50) == true ? 7.8f : 6f,leaveBy:leaveBy); }
    }
}
