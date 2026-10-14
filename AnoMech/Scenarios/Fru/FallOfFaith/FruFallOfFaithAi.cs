using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.FallOfFaith;

public sealed class FruFallOfFaithAi : IScenarioAi<FruFallOfFaithState>
{
    public static readonly PartyRole[] Priority = [PartyRole.RegenHealer, PartyRole.ShieldHealer,
        PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB,
        PartyRole.PhysRangedDps, PartyRole.CasterDps];
    public static int Side(FruFallOfFaithState state, PartyRole role) => state.Order(role) is var order && order >= 0
        ? order % 2 : Array.IndexOf(Priority.Where(r => state.Order(r) < 0).ToArray(), role) / 2;
    public static PartyRole[] GroupRoles(FruFallOfFaithState state, int side) => Priority.Where(r => Side(state, r) == side).ToArray();
    public string Name => "Mana West-to-East conga";
    public string Group => "JP";
    public static Vector3 Conga(PartyRole role) => new((Array.IndexOf(Priority, role) - 3.5f) * 2.3f, 0, 6);
    public static Vector3 Holder(int side) => new(side == 0 ? -5f : 5.5f, 0, 0);
    public static Vector3 Position(FruFallOfFaithState state, PartyRole role, int wave)
    {
        var side = wave % 2;
        var origin = Holder(side);
        if (state.Targets[wave] == role) return origin;
        var outward = side == 0 ? -1 : 1;
        var order = state.Order(role);
        if (order >= 0) return origin + new Vector3(outward * 2, 0, 0);
        var helpers = Priority.Where(r => state.Order(r) < 0 && Side(state, r) == side).ToArray();
        if (!state.Fire(wave)) return origin + new Vector3(0, 0, helpers[0] == role ? -2 : 2);
        return origin + new Vector3(outward * 2, 0, helpers[0] == role ? -.4f : .4f);
    }
    public void Run(FruFallOfFaithState state, SimWorld world)
    {
        void Move(PartyRole role, Vector3 position) => world.BotMovement.Move(world.Party.Get(role), position);
        world.Events.Add(0, () => { foreach (var role in Priority) Move(role, Conga(role)); });
        for (var i = 0; i < 4; i++)
        {
            var order = i;
            world.Events.Add(FruFallOfFaithTiming.Reveal[i] + .02f, () =>
                Move(state.Targets[order], Holder(order % 2) + (order < 2 ? Vector3.Zero : new Vector3(order % 2 == 0 ? -2 : 2, 0, 0))));
        }
        world.Events.Add(FruFallOfFaithTiming.Reveal[3] + .1f, () =>
        {
            foreach (var role in Priority) Move(role, Position(state, role, Side(state, role)));
        });
        world.Events.Add(FruFallOfFaithTiming.Swap[0], () => { foreach (var role in GroupRoles(state, 0)) Move(role, Position(state, role, 2)); });
        world.Events.Add(FruFallOfFaithTiming.Swap[1], () => { foreach (var role in GroupRoles(state, 1)) Move(role, Position(state, role, 3)); });
    }
}
