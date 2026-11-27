using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Dmu.Elements;

public sealed class DmuElementsAi : IScenarioAi<DmuElementsState>
{
    public string Name => "Yan Flash TLB3";
    public string Group => "Mana";

    private const float ChaosHold = 12.5f;
    private const float FrontSafe = 32f;
    private const float SideSafe = 58f;

    private DmuElementsState state = null!;
    private SimWorld world = null!;

    public void Run(DmuElementsState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);
        ai.Move(0.2f, SouthOfKefka);
        ai.Move(8.0f, HeroSidesBesideTheirBoss, arrivalTime: 11.8f);
        ai.Move(13.0f, TanksHoldBothBossesMid, arrivalTime: 15f);
        ai.Move(34.2f, ChaosTankPullsChaosToTheEdgeBehindTheWindCrystal);
        ai.Move(34.2f, ExdeathTankPullsExdeathToTheEmptyCorner);
        ai.Move(34.2f, PartyToTlb3Spots, arrivalTime: 43f);
        ai.Move(40.0f, ChaosTankBackToTheStack, arrivalTime: 42.5f);
        ai.Move(44.3f, ExdeathTankBackToTheStack, arrivalTime: 48f);
        ai.Move(52.4f, ExdeathTankTakesTheBusterBeyondExdeath);
        world.Events.Add(59.6f, () => BotActions.UseInvulnerability(world, ExdeathTank));
        ai.Move(63.6f, ExdeathTankBringsExdeathToChaos, arrivalTime: 66f);
        ai.Move(66.2f, ExdeathTankBackToTheStack, arrivalTime: 68f);
        ai.Move(69.0f, () => ImplosionSpots(firstWave: true), arrivalTime: 73.4f);
        ai.Move(74.1f, () => ImplosionSpots(firstWave: false), arrivalTime: 75.8f);
        ai.Move(76.1f, AllToTlb3Spots, arrivalTime: 77.8f);
    }

    private Vector2 Wind => DmuElementsCorners.Direction(state.WindCorner);
    private Vector2 Water => DmuElementsCorners.Direction(state.WaterCorner);

    private Vector2 Spot(float towardWind, float towardWater) => Wind * towardWind + Water * towardWater;

    private static IAiMove SouthOfKefka() => AiMove.ByRole(PerRole.All.Select(r => (r, new Vector2(0f, 4f))).ToArray());

    private IAiMove HeroSidesBesideTheirBoss() => AiMove.ByRole(
        (ChaosTank, new(-4.5f, 1f)), (ShieldHealer, new(-3.5f, 2.5f)), (MeleeDpsA, new(-4.5f, 2.5f)), (MeleeDpsB, new(-3.5f, 3.5f)),
        (ExdeathTank, new(4.5f, 1f)), (RegenHealer, new(3.5f, 2.5f)), (PhysRangedDps, new(4.5f, 2.5f)), (CasterDps, new(3.5f, 3.5f)));

    private IAiMove TanksHoldBothBossesMid() => AiMove.ByRole(
        (ChaosTank, new(5.5f, -1.5f)), (ExdeathTank, new(-5.5f, -1.5f)),
        (RegenHealer, new(-0.5f, 4f)), (ShieldHealer, new(0.5f, 4f)), (MeleeDpsA, new(-0.5f, 5f)), (MeleeDpsB, new(0.5f, 5f)),
        (PhysRangedDps, new(-0.5f, 6f)), (CasterDps, new(0.5f, 6f)));

    private PartyRole ChaosTank => state.ChaosTank;
    private PartyRole ExdeathTank => state.ExdeathTank;

    private IAiMove ChaosTankPullsChaosToTheEdgeBehindTheWindCrystal() => AiMove.ByRole((ChaosTank, Spot(19.5f, 0f)));

    private IAiMove ExdeathTankPullsExdeathToTheEmptyCorner() => AiMove.ByRole((ExdeathTank, Spot(-15.5f, 0f)));

    private Vector2 Tlb3Spot(PartyRole role) => role switch
    {
        MainTank => Spot(6f, 0.6f),
        OffTank => Spot(6f, -0.6f),
        RegenHealer => Spot(5.4f, 0.3f),
        ShieldHealer => Spot(5.4f, -0.3f),
        MeleeDpsA => Spot(18f, 0.5f),
        MeleeDpsB => Spot(18f, -0.5f),
        PhysRangedDps => Spot(-6.5f, 17f),
        _ => Spot(6.5f, 15f),
    };

    private IAiMove PartyToTlb3Spots() =>
        AiMove.ByRole(PerRole.All.Where(r => !r.IsTank()).Select(r => (r, Tlb3Spot(r))).ToArray());

    private IAiMove AllToTlb3Spots() => AiMove.ByRole(PerRole.All.Select(r => (r, Tlb3Spot(r))).ToArray());

    private IAiMove ChaosTankBackToTheStack() => AiMove.ByRole((ChaosTank, Tlb3Spot(ChaosTank)));

    private IAiMove ExdeathTankBackToTheStack() => AiMove.ByRole((ExdeathTank, Tlb3Spot(ExdeathTank)));

    private IAiMove ExdeathTankTakesTheBusterBeyondExdeath() => AiMove.ByRole((ExdeathTank, Spot(-12f, 0f)));

    private IAiMove ExdeathTankBringsExdeathToChaos() => AiMove.ByRole((ExdeathTank, Spot(11f, -5f)));

    private IAiMove ImplosionSpots(bool firstWave)
    {
        var (origin, front) = ChaosPlacement();
        var side = new Vector2(-front.Y, front.X);
        if (Vector2.Dot(side, Water) < 0f) side = -side;
        var frontAndBackFirst = !state.Latitudinal;
        var dodgeIntoSides = firstWave == frontAndBackFirst;
        Vector2 Around(float radius, float degrees)
        {
            var rad = degrees * MathF.PI / 180f;
            return origin + (front * MathF.Cos(rad) + side * MathF.Sin(rad)) * radius;
        }
        return AiMove.ByRole(PerRole.All.Select(role => (role, role switch
        {
            MeleeDpsA or MeleeDpsB => Around(5.5f, dodgeIntoSides ? 180f - SideSafe : 180f) + side * (role == MeleeDpsA ? 0.5f : -0.5f),
            PhysRangedDps => Around(20f, dodgeIntoSides ? SideSafe : FrontSafe - 6f),
            CasterDps => Around(15f, dodgeIntoSides ? SideSafe + 6f : FrontSafe),
            _ => Around(6.5f, dodgeIntoSides ? SideSafe : 0f) + side * StackOffset(role),
        })).ToArray());
    }

    private static float StackOffset(PartyRole role) => role switch
    {
        MainTank => 0.6f,
        OffTank => -0.6f,
        RegenHealer => 0.3f,
        _ => -0.3f,
    };

    private (Vector2 Origin, Vector2 Front) ChaosPlacement()
    {
        if (world.Children.OfType<SimEnemy>().FirstOrDefault(e => e.BNpcBaseId == DmuConstants.BNpcBaseId.ChaosP3 && e.IsActive) is { } chaos)
            return (new Vector2(chaos.Position.X, chaos.Position.Z), new Vector2(MathF.Sin(chaos.Rotation), MathF.Cos(chaos.Rotation)));
        return (Wind * ChaosHold, -Wind);
    }
}
