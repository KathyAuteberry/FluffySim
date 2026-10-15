using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.UltimateRelativity;

public sealed class FruUltimateRelativityAi : IScenarioAi<FruUltimateRelativityState>
{
    public string Name => "Mana 大 / local markers";
    public string Group => "JP";
    private FruUltimateRelativityState state = null!;
    private SimWorld world = null!;
    public void Run(FruUltimateRelativityState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        world.Events.Add(0f, Center);
        world.Events.Add(20.7f, () => Fire(1));
        world.Events.Add(24.99f, () => BaitAndRecord(1));
        world.Events.Add(30.14f, () => Fire(2, intermediate: true));
        world.Events.Add(32.8f, () => Fire(2));
        world.Events.Add(35.0f, () => BaitAndRecord(2));
        world.Events.Add(40.2f, () => Fire(3));
        world.Events.Add(45.0f, () => BaitAndRecord(3));
        world.Events.Add(51.2f, Center);
        world.Events.Add(53f, FaceOut);
        world.Events.Add(57.95f, Center);
    }
    public static Vector3 BaitPosition(FruUltimateRelativityState state, PartyRole role)
    {
        var clock = state.Clock(role);
        var d = state.Direction(clock);
        var heading = MathF.Atan2(d.X, d.Z);
        var spin = state.Clockwise(clock) ? -MathF.PI / 12f : MathF.PI / 12f;
        var offset = heading - 4.5f * spin;
        return state.Hourglass(clock) + new Vector3(MathF.Sin(offset), 0f, MathF.Cos(offset)) * 1.5f;
    }
    public static Vector3 RecordPosition(FruUltimateRelativityState state, PartyRole role) =>
        state.Direction(state.Clock(role)) * (state.FinalStatus(role) switch { 2460 => 9f, 2456 => 2f, _ => 1f });
    private void Center()
    {
        foreach (var role in state.Roles) Move(role, state.Direction(state.Clock(role)));
    }
    private void Fire(int order, bool intermediate = false)
    {
        foreach (var role in state.Roles)
            Move(role, state.Direction(state.Clock(role)) * (state.FireOrder(role) == order ? intermediate ? 9f : 14.5f : 1f), intermediate || order == 3 ? null : order == 1 ? 24.6f : 34.6f);
    }
    private void BaitAndRecord(int wave)
    {
        foreach (var role in state.Roles)
            Move(role, state.LaserOrder(role) == wave ? BaitPosition(state, role)
                : state.ReturnOrder(role) == wave ? RecordPosition(state, role) : state.Direction(state.Clock(role)));
    }
    private void FaceOut()
    {
        foreach (var role in state.Roles)
        {
            var d = state.Direction(state.Clock(role));
            var member = world.Party.Get(role);
            if (member is SimPlayer && !DebugBotControl.Enabled) continue;
            member?.SetRotation(MathF.Atan2(d.X, d.Z));
        }
    }
    private void Move(PartyRole role, Vector3 position, float? deadline=null) => world.BotMovement.Move(world.Party.Get(role),position,deadline);
}
