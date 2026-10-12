using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.Sanctity1;

public sealed class DsrSanctity1Ai : IScenarioAi<DsrSanctity1State>
{
    public string Name => "Elemental DRK-relative role swaps";
    public string Group => "JP";
    public void Run(DsrSanctity1State state, SimWorld world)
    {
        world.Events.Add(9.5f, () =>
        {
            foreach (var role in DsrSanctity1State.Roles)
            {
                var position = state.InitialPosition(role);
                world.BotMovement.Move(world.Party.Get(role),position,17.1f,6f,AvoidBothGazes(position,state));
            }
        });
        world.Events.Add(state.MoveEarly ? 20.8f : 22.05f, () =>
        {
            foreach (var role in DsrSanctity1State.Roles) world.Party.Get(role)?.MoveTo(state.FinalPosition(role),6f);
        });
    }
    private static float AvoidBothGazes(Vector3 position, DsrSanctity1State state)
    {
        var boss = Vector3.Normalize(state.ThordanSpawn-position);
        var eye = Vector3.Normalize(state.EyeSpawn-position);
        var away = -(boss+eye);
        if (away.LengthSquared()<.0001f) away=new Vector3(-boss.Z,0,boss.X);
        return MathF.Atan2(away.X,away.Z);
    }
}
