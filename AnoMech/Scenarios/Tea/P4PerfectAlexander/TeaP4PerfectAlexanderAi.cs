using System.Numerics;
using AnoMech.Core.Game.Party;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Tea.P4PerfectAlexander;

public sealed class TeaP4PerfectAlexanderAi : TeaP4PerfectAlexanderAiBase
{
    public override string Name => "Nukemaru";
    public override string Group => "Mana";

    protected override Vector2 FinalWordPosition(FinalWordDebuff debuff, int index)
    {
        var spot = base.FinalWordPosition(debuff, index);
        return new Vector2(spot.Y, -spot.X);
    }

    protected override Vector2 SpreadSpot(PartyRole role) => role switch
    {
        MainTank => new(-5f, -10f),
        OffTank => new(5f, -10f),
        RegenHealer => new(-5f, 0f),
        ShieldHealer => new(5f, 0f),
        PhysRangedDps => new(-13f, 10f),
        MeleeDpsA => new(-4f, 10f),
        MeleeDpsB => new(4f, 10f),
        _ => new(13f, 10f),
    };

    protected override bool InFirstStack(PartyRole role)
        => role is MainTank or OffTank or RegenHealer or ShieldHealer;

    protected override PartyRole Counterpart(PartyRole role) => role switch
    {
        MainTank => MeleeDpsA,
        OffTank => MeleeDpsB,
        RegenHealer => PhysRangedDps,
        ShieldHealer => CasterDps,
        MeleeDpsA => MainTank,
        MeleeDpsB => OffTank,
        PhysRangedDps => RegenHealer,
        _ => ShieldHealer,
    };

    protected override Vector2 StackPosition(bool first) => new(0f, first ? -7.5f : 7.5f);

    protected override Vector2 ForTheStacks(BetaRole role) => role switch
    {
        BetaRole.DarkBeacon => new(0.6f, 6f),
        BetaRole.DarkUntethered => new(-1f, 6.4f),
        BetaRole.DarkFar => new(-0.2f, 7.3f),
        BetaRole.DarkNear => new(0.2f, 5.4f),
        _ => AfterTheMarch(role),
    };
}

public sealed class TeaP4PerfectAlexanderLegacyAi : TeaP4PerfectAlexanderAiBase;
