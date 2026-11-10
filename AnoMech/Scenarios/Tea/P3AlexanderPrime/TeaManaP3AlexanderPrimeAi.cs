using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Tea.P3AlexanderPrime;

public sealed class TeaManaP3AlexanderPrimeAi : TeaP3AlexanderPrimeAi
{
    public override string Name => "3/4 fixed";
    public override string Group => "Mana";

    protected override PartyRole FirstHeatTank => MainTank;
    protected override PartyRole SecondHeatTank => OffTank;

    protected override Vector2 OpenerSpot(PartyRole role)
    {
        var source = base.OpenerSpot(role == MainTank ? OffTank : role == OffTank ? MainTank : role);
        return source with { Y = -source.Y };
    }

    protected override Vector2 SecondSetSpot(PartyRole role) => role switch
    {
        OffTank => new(-0.4f, 6.2f),
        MainTank => new(7.8f, -2.2f),
        _ => OpenerSpot(role),
    };

    protected override void GoToFlarethrowerSpot(PartyRole role)
        => SprintIfFar(role, InceptionSide(TeaManaP3Assignments.FlameSpot(role)));

    protected override void PlanFlareWithdrawals()
    {
        At(61.25f, () => Go(MainTank, InceptionSide(new Vector2(-1f, 0f))));
        At(62.7f, () => Go(OffTank, InceptionSide(new Vector2(-2f, -17f))));
        At(66.1f, () => GoAll(role => InceptionSide(InceptionWaitSpot(role))));
    }

    protected override void TakeSwordAndJumpSpots()
    {
        foreach (var role in PerRole.All)
            SprintIfFar(role, TeaManaP3Assignments.SwordSpot(role, state));
    }

    protected override void StepAwayFromTheJump()
    {
        var stack = new[] { OffTank, state.SharedSentence }.Concat(state.AggravatedAssault).ToList();
        var clear = new Vector2[] { new(6.6f, 4.2f), new(7.6f, 3.6f), new(5.8f, 3.4f), new(7f, 5f) };
        for (var k = 0; k < stack.Count; k++) Go(stack[k], clear[k]);
    }

    protected override void TakeNumberSpots()
    {
        for (var number = 1; number <= 8; number++)
        {
            var spot = TeaManaP3Assignments.InitialNumberSpot(number);
            if (number is 1 or 3)
                GoFacing(Number(number), number == 1 ? new(-1f, -1f) : new(-1f, 1f), false, spot);
            else
                Go(Number(number), spot);
        }
    }

    protected override void StepThirdAndFourthAfterJump()
    {
        GoFacing(Number(3), new(-1f, 1f), false, SouthWestSlashSpot);
        Go(Number(4), SouthEastChargeSpot);
    }

    protected override void TakeEnumerationSpots()
        => GoAll(role => TeaManaP3Assignments.EnumerationSpot(role, state.Enumerations));
}
