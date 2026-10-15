using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.Strength2;

public sealed class DsrStrength2Ai : IScenarioAi<DsrStrength2State>
{
    public string Name => "Elemental crossed tethers";
    public string Group => "JP";
    public void Run(DsrStrength2State state, SimWorld world)
    {
        world.Events.Add(3.1f, () =>
        {
            foreach(var role in state.Leaps) Move(role,state.LeapPosition(role),6.6f);
            foreach(var role in state.TowerCommunicators) Move(role,state.StackPosition,6.5f);
            Move(PartyRole.MainTank,state.Rotate(new(-2,0,-13)),6.8f);
            Move(PartyRole.OffTank,state.Rotate(new(2,0,-13)),6.8f);
        });
        world.Events.Add(7.35f, () =>
        {
            Move(PartyRole.MainTank,System.Numerics.Vector3.Lerp(state.StackPosition,state.WestKnight,.2f));
            Move(PartyRole.OffTank,System.Numerics.Vector3.Lerp(state.StackPosition,state.EastKnight,.2f));
        });
        world.Events.Add(8.2f, () =>
        {
            Move(PartyRole.MainTank,state.TankPosition(PartyRole.MainTank));
            Move(PartyRole.OffTank,state.TankPosition(PartyRole.OffTank));
        });
        world.Events.Add(8.5f, () => {foreach(var role in state.TowerCommunicators) Move(role,state.CommunicationPosition(role));});
        world.Events.Add(10.7f, () => {foreach(var role in state.TowerCommunicators) Move(role,state.StackPosition);});
        world.Events.Add(12.65f, () => {foreach(var role in DsrStrength2State.NonTanks) Move(role,state.TowerPosition(role),15.3f);});
        void Move(PartyRole role,System.Numerics.Vector3 position,float? deadline=null)=>world.BotMovement.Move(world.Party.Get(role),position,deadline);
    }
}
