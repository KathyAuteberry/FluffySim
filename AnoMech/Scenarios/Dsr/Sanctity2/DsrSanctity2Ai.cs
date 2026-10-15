using System;
using System.Numerics;
using System.Linq;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.Sanctity2;

public sealed class DsrSanctity2Ai : IScenarioAi<DsrSanctity2State>
{
    public string Name => "Elemental cardinal pairs";
    public string Group => "JP";
    public void Run(DsrSanctity2State state, SimWorld world)
    {
        world.Events.Add(.1f, () => Each(role => Move(role,DsrSanctity2State.Direction(DsrSanctity2State.BaseQuadrants[(int)role]*MathF.PI/2)*10)));
        world.Events.Add(1.6f, () => Each(role => Move(role,state.PairPosition(role),3.4f)));
        world.Events.Add(3.7f, () => Each(role => Move(role,state.CommunicationPosition(role))));
        world.Events.Add(5.9f, () => Each(role => Move(role,state.PairPosition(role),8.25f)));
        world.Events.Add(9.05f, () => Each(role => Move(role,state.FirstPosition(role),12.35f, DsrSanctity2Timing.Storm+DsrSanctity2Timing.PuddleActivation-7f/6f-.45f)));
        world.Events.Add(13.1f, () =>
        {
            foreach (var role in state.Targets)
            {
                Move(role,Vector3.Normalize(state.FirstPosition(role))*19.3f);
            }
            foreach (var role in DsrSanctity2State.Roles.Where(r => !state.IsMeteor(r)))
                Move(role,state.TakesKnockback(role) ? state.KnockbackPosition(role) : Vector3.Normalize(state.FirstPosition(role))*19.3f);
        });
        world.Events.Add(14f, () => { foreach (var role in state.Targets) Move(role,state.MeteorPosition(role,0)); });
        for (var step = 1; step <= 6; ++step)
        {
            var fraction=step/6f;
            world.Events.Add(14f+step*.8f, () =>
            {
                foreach (var role in DsrSanctity2State.Roles.Where(r => !state.IsMeteor(r) && !state.TakesKnockback(r)))
                    Move(role,Vector3.Normalize(Vector3.Lerp(state.FirstPosition(role),state.FinalPosition(role),fraction))*19.3f);
            });
        }
        world.Events.Add(19.4f, () =>
        {
            foreach (var role in DsrSanctity2State.Roles.Where(r => !state.IsMeteor(r) && !state.TakesKnockback(r))) Move(role,state.FinalPosition(role),22.6f);
        });
        world.Events.Add(15.8f, () => { foreach (var role in state.Targets) (world.Party.Get(role) as ISimPartyMember)?.UseSprint(10); });
        for (var i = 1; i < 7; ++i)
        {
            var drop = i;
            world.Events.Add(DsrSanctity2Timing.Meteor(i-1)+.08f, () =>
            {
                foreach (var role in state.Targets) Move(role,state.MeteorPosition(role,drop));
            });
        }
        world.Events.Add(22.2f, () =>
        {
            foreach (var role in DsrSanctity2State.Roles.Where(r => !state.TakesKnockback(r)))
            {
                var member = world.Party.Get(role);
                if (member is SimPlayer && !DebugBotControl.Enabled) continue;
                var caster = role is PartyRole.RegenHealer or PartyRole.ShieldHealer or PartyRole.CasterDps;
                if (member is SimNpc npc) npc.PlayAction(caster ? 7559u : 7548u); member?.AddStatus(caster ? (ushort)160 : (ushort)1209,6);
            }
        });
        world.Events.Add(DsrSanctity2Timing.Meteor(6)+.08f, () => { foreach (var role in state.Targets) Move(role,state.FinalPosition(role)); });
        world.Events.Add(24f, () => { foreach (var role in DsrSanctity2State.Roles.Where(state.TakesKnockback)) Move(role,state.FinalPosition(role)); });
        void Each(Action<PartyRole> action) { foreach (var role in DsrSanctity2State.Roles) action(role); }
        void Move(PartyRole role, Vector3 position, float? deadline=null, float? leaveBy=null)
        { var member=world.Party.Get(role); world.BotMovement.Move(member,position,deadline,member.HasStatus(50) ? 7.8f : 6f,leaveBy:leaveBy); }
    }
}
