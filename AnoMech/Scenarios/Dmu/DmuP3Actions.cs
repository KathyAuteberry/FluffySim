using AnoMech.Core.EnemyActions;
using AnoMech.Scenarios.Umad;
using AnoMech.Scenarios.Umad.P3BlackHole;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dmu;

public static class DmuP3Actions
{
    // ISSIS: a Thunder III set the plan gives one tank must be invulned, mitigation setting or not.
    public static readonly Severity InvulnRequired = Severity.TankBuster.MinMit(1f) with { ForceMitigation = true };

    public static readonly EnemyAction InvulnedThunderIII = UmadActions.ThunderIII with
    {
        Effects =
        [
            Damage(InvulnRequired),
            ApplyStatus(UmadConstants.StatusId.LightningResistanceDownII, 3.96f),
        ],
        DeathExplanation = "Thunder III without a tank invulnerability",
    };

    // Every Cyclone lands on the one stack at once, so only a tank LB3 carries the party through the repeats.
    public static readonly EnemyAction LimitCutCyclone = UmadActions.Cyclone with
    {
        Effects =
        [
            Damage(VulnerableTo(UmadConstants.StatusId.WindResistanceDownII).ProtectedByLevelThree(), split: Distribution.Stack(2, understacked: Severity.Lethal)),
            ApplyStatus(UmadConstants.StatusId.WindResistanceDownII, 0.96f),
        ],
        DeathExplanation = "Cyclones without a tank LB3, or outside the stack",
    };

    public static bool IsInvulnPlan(ThunderIIIAssignment plan)
        => plan is ThunderIIIAssignment.MtInvulnsBoth or ThunderIIIAssignment.OtInvulnsBoth;

    // The invulning tank goes with its boss when the tanks swap bosses.
    public static ThunderIIIAssignment SwapInvulnTank(ThunderIIIAssignment plan) => plan switch
    {
        ThunderIIIAssignment.MtInvulnsBoth => ThunderIIIAssignment.OtInvulnsBoth,
        ThunderIIIAssignment.OtInvulnsBoth => ThunderIIIAssignment.MtInvulnsBoth,
        _ => plan,
    };

    public static EnemyAction ThunderIII(ThunderIIIAssignment plan) => IsInvulnPlan(plan) ? InvulnedThunderIII : UmadActions.ThunderIII;
}
