using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Fru.DiamondDust;

public sealed class FruDiamondDustAi : IScenarioAi<FruDiamondDustState>
{
    public string Name => "Mana (development prefix)";
    public string Group => "JP";
    private SimWorld world = null!;
    private FruDiamondDustState state = null!;
    public void Run(FruDiamondDustState stateParam, SimWorld worldParam)
    {
        world = worldParam;
        state = stateParam;
        world.Events.Add(0f, () =>
        {
            foreach (var role in Enum.GetValues<PartyRole>()) Move(role, FruDiamondDustState.At(ClockSector(role), 8f));
        });
        world.Events.Add(16.3f, () => MoveSpreads(state.Axe ? 19f : 3f, state.Axe ? 17f : 1f));
        world.Events.Add(21.8f, () => { if (!state.Axe) MoveStonesOut(); });
        world.Events.Add(22.06f, () => { if (state.Axe) MoveConeBaitsIn(); });
        world.Events.Add(22.6f, () => { if (!state.Axe) MoveConeBaitsIn(); });
        world.Events.Add(24.36f, MoveStoneKnockbackGroups);
        world.Events.Add(24.81f, MoveKnockbackGroups);
    }
    public static int ClockSector(PartyRole role) => role switch
    {
        MainTank => 0, OffTank => 4, RegenHealer => 6, ShieldHealer => 2,
        MeleeDpsA => 5, MeleeDpsB => 3, PhysRangedDps => 7, CasterDps => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
    public static bool NorthWestGroup(PartyRole role) => role is MainTank or RegenHealer or MeleeDpsA or PhysRangedDps;
    public static int AdjustedSector(FruDiamondDustState state, PartyRole role)
    {
        var rotate = (state.FirstIcicleAxis % 2 == 0) == state.StonesOnSupports;
        return (ClockSector(role) + (rotate ? 1 : 0)) % 8;
    }
    public static Vector3 KnockbackPosition(FruDiamondDustState state, PartyRole role)
    {
        var first = state.FirstIcicleAxis >= 2 ? state.FirstIcicleAxis + 4 : state.FirstIcicleAxis;
        return FruDiamondDustState.At(first + (NorthWestGroup(role) ? 0 : 4), 6f);
    }
    private void MoveSpreads(float stoneRadius, float coneRadius)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            Move(role, FruDiamondDustState.At(AdjustedSector(state, role), state.HasStone(role) ? stoneRadius : coneRadius));
    }
    private void MoveStonesOut()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (state.HasStone(role)) Move(role, FruDiamondDustState.At(AdjustedSector(state, role), 8f));
    }
    private void MoveConeBaitsIn()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (!state.HasStone(role)) Move(role, FruDiamondDustState.At(AdjustedSector(state, role), 3f));
    }
    private void MoveStoneKnockbackGroups()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (state.HasStone(role)) Move(role, KnockbackPosition(state, role));
    }
    private void MoveKnockbackGroups()
    {
        foreach (var role in Enum.GetValues<PartyRole>()) Move(role, KnockbackPosition(state, role));
    }
    private void Move(PartyRole role, Vector3 position) => world.Party.Get(role)?.MoveTo(position);
}
