using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.Strength;

public sealed class DsrStrengthAi : IScenarioAi<DsrStrengthState>
{
    public string Name => "Elemental triangles";
    public string Group => "JP";
    public static Vector3 Triangle(DsrStrengthState state, PartyRole role)
    {
        var direction = DsrStrengthState.Direction(state.MainGroupClock + ((int)role % 2) * 4);
        var tangent = new Vector3(-direction.Z, 0, direction.X);
        var radial = (int)role < 2 ? 19.5f : (int)role < 4 ? 13.5f : 18.55f;
        var lateral = (int)role < 4 ? 0 : (int)role < 6 ? 5.65f : -5.65f;
        return direction * radial + tangent * lateral;
    }
    public static Vector3 Bait(DsrStrengthState state, PartyRole role, bool dodged = false)
    {
        var angle = (state.MainGroupClock + (int)role % 2 * 4) * MathF.PI / 4 + (dodged ? MathF.PI / 3 : 0);
        return new(MathF.Sin(angle) * 2, 0, -MathF.Cos(angle) * 2);
    }
    public void Run(DsrStrengthState state, SimWorld world)
    {
        world.Events.Add(11.5f, () => Each(role => Move(role, Triangle(state, role))));
        world.Events.Add(19.9f, () => Each(role =>
        {
            var initial = Triangle(state, role);
            var delta = initial - state.HeavyPosition;
            Move(role, state.HeavyPosition + Vector3.Normalize(delta) * (delta.Length() <= 14f ? 4.8f : 12.5f));
        }));
        world.Events.Add(21.65f, () => Each(role => Move(role, Bait(state, role))));
        world.Events.Add(25f, () => Each(role => Move(role, Bait(state, role, true))));
        void Move(PartyRole role, Vector3 point) => world.Party.Get(role)?.MoveTo(point, 6f);
    }
    private static void Each(Action<PartyRole> action)
    {
        for (var i = 0; i < 8; ++i) action((PartyRole)i);
    }
}
