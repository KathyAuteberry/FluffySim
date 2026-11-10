using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Tea.P1LivingLiquid;

public sealed class TeaP1LivingLiquidAi : IScenarioAi<TeaP1LivingLiquidState>
{
    public string Name => "Doll Skip";
    public string Group => "Mana";

    private TeaP1LivingLiquidState state = null!;
    private SimWorld world = null!;
    private AiManager ai = null!;

    public void Run(TeaP1LivingLiquidState state, SimWorld world)
    {
        this.state = state;
        this.world = world;
        ai = new AiManager(world);
        ai.Move(0f, () => Positions(false,
            (MainTank, 0f, 6f), (OffTank, -3f, 4f), (RegenHealer, -2f, 5f), (ShieldHealer, 2f, 5f),
            (MeleeDpsA, -1f, 3f), (MeleeDpsB, 1f, 3f), (PhysRangedDps, -4f, 5f), (CasterDps, 4f, 5f)));
        ai.Move(2.5f, () => Positions(false, (MainTank, 0f, -4f)), jitter: 0f);
        ai.Move(20f, () => Positions(true,
            (MainTank, -5f, 15f), (OffTank, -3f, -8f), (RegenHealer, 17f, 8f), (ShieldHealer, 0f, 18f),
            (MeleeDpsA, -1f, 17f), (MeleeDpsB, 1f, 17f), (PhysRangedDps, -7f, -10f), (CasterDps, -17f, 8f)), jitter: 0f);
        ai.Move(25f, () => Positions(true, (MainTank, 6f, 17.5f)), jitter: 0f);
        ai.UseAction(29.85f, MainTank, Enmity.ProvokeActionId, () => state.Objects.LiquidHand);
        ai.Move(30f, () => Positions(true, (OffTank, 0f, 17f)), jitter: 0f);
        ai.Move(32.6f, () => Positions(true,
            (PhysRangedDps, 0f, 0f), (CasterDps, -13f, 13f), (RegenHealer, 13f, 13f)), jitter: 0f);
        ai.Move(34.6f, () => Positions(true, (PhysRangedDps, 0f, 11f)), jitter: 0f);
        ai.UseAction(35f, MainTank, Invulnerability);
        world.Events.Add(36f, PreparePartyMitigation);
        world.Events.Add(39f, ShieldAfterProtean);
        ai.Move(38.5f, () => Positions(true,
            (CasterDps, 0f, 18f), (RegenHealer, -1.5f, 18f)), jitter: 0f);
        ai.Move(46.4f, () => Positions(true,
            (MainTank, -6f, 18f), (OffTank, -6f, 17f), (RegenHealer, -2f, 18f), (ShieldHealer, 0f, 18f),
            (MeleeDpsA, -1f, 18f), (MeleeDpsB, 1f, 18f), (PhysRangedDps, 2f, 18f), (CasterDps, 3f, 18f)), jitter: 0f);
        ai.Move(48f, () => Positions(true, (MainTank, -12f, 14f), (OffTank, -10f, 15f)), jitter: 0f);
        ai.Move(49.5f, () => Positions(true,
            (MainTank, -17f, 8f), (OffTank, -15f, 12f), (RegenHealer, -9f, 16f), (ShieldHealer, -8f, 16f),
            (MeleeDpsA, -10f, 15f), (MeleeDpsB, -9f, 15f), (PhysRangedDps, -7f, 17f), (CasterDps, -6f, 17f)), jitter: 0f);
        ai.UseAction(50f, OffTank, Enmity.ProvokeActionId, () => state.Objects.LiquidHand);
        ai.Move(52f, () => Positions(true,
            (OffTank, -10f, 7f), (RegenHealer, -14f, 13f), (ShieldHealer, -13.8f, 13.5f),
            (MeleeDpsA, -13.5f, 13f), (MeleeDpsB, -14.3f, 12.8f),
            (PhysRangedDps, -14f, 13.8f), (CasterDps, -13.5f, 13.8f)), jitter: 0f);
    }

    private void PreparePartyMitigation()
    {
        if (world.Party.Get(OffTank) is { } tank && tank.IsAlive() && world.Party.IsBotDriven(tank))
            state.Objects.LiquidHand?.AddStatus(1193, 15f);
        foreach (var (role, status) in new (PartyRole, ushort)[] { (RegenHealer, 3985), (PhysRangedDps, 1934) })
        {
            if (world.Party.Get(role) is not { } caster || !caster.IsAlive() || !world.Party.IsBotDriven(caster)) continue;
            var applied = role == PhysRangedDps ? caster.Proxy?.ClassJob switch { 31 => (ushort)1951, 38 => (ushort)1826, _ => status } : status;
            foreach (var member in world.Party.ActiveMembers())
                if (Vector3.Distance(caster.Position, member.Position) <= 30f + member.HitboxRadius)
                    member.AddStatus(applied, 15f);
        }
    }

    private void ShieldAfterProtean()
    {
        if (world.Party.Get(ShieldHealer) is not { } healer || !healer.IsAlive() || !world.Party.IsBotDriven(healer)) return;
        foreach (var member in world.Party.ActiveMembers())
            if (Vector3.Distance(healer.Position, member.Position) <= 30f + member.HitboxRadius)
                member.AddStatus(297, 30f);
    }

    public static uint Invulnerability(byte job) => job switch
    {
        19 => 30,
        21 => 43,
        32 => 3638,
        37 => 16152,
        _ => 0,
    };

    private IAiMove Positions(bool relative, params (PartyRole Role, float X, float Z)[] positions)
    {
        var spots = new Vector2?[8];
        foreach (var (role, x, z) in positions)
            spots[(int)role] = relative ? ManaPosition(state.FirstTornadoes, new(x, z)) : new Vector2(x, z);
        return AiMove.Create(spots).NaturalOrder();
    }

    public static Vector2 ManaPosition(TornadoT tornadoes, Vector2 relative)
        => tornadoes.Abs(-relative);
}
