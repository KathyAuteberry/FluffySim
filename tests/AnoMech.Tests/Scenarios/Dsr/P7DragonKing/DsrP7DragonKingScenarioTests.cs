using System.Linq;
using System.Numerics;
using AnoMech.Scenarios.Dsr.P7DragonKing;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrP7Constants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

public class DsrP7DragonKingScenarioTests
{
    private static void Pinned(DsrP7DragonKingStateOverrides o)
    {
        var pinned = DsrP7DragonKingStateTests.Pinned();
        o.ExaflareHeadings = pinned.ExaflareHeadings;
        o.Elements = pinned.Elements;
        o.GigaflareOrder = pinned.GigaflareOrder;
    }

    private static void IceFirst(DsrP7DragonKingStateOverrides o)
    {
        Pinned(o);
        o.Elements = [AscalonElement.Ice, .. o.Elements!.Skip(1)];
    }

    [Test]
    public void StandingOnAnExaflareStartKills()
        => Negative<DsrP7DragonKingScenario>(MeleeDpsB)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(22.50f, to: new Vector2(-6.93f, -4f))
            .ShouldKill(ActionId.ExaflaresEdgeFirst, MeleeDpsB);

    [Test]
    public void StayingNearTheDragonKingForFlamesKills()
        => Negative<DsrP7DragonKingScenario>(CasterDps)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(22.50f, to: new Vector2(2f, 0f))
            .ShouldKill(ActionId.FlamesOfAscalon, CasterDps);

    [Test]
    public void StayingAwayForIceKills()
        => Negative<DsrP7DragonKingScenario>(CasterDps)
            .Overrides<DsrP7DragonKingStateOverrides>(IceFirst)
            .TeleportAt(22.50f, to: new Vector2(0f, -15f))
            .ShouldKill(ActionId.IceOfAscalon, CasterDps);

    [Test]
    public void BaitingTrinityTwiceInARowKills()
        => Negative<DsrP7DragonKingScenario>(MeleeDpsA)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(34.50f, to: new Vector2(0f, 0.5f))
            .ShouldKill(ActionId.TrinityNearest, MeleeDpsA);

    [Test]
    public void StandingInTheMiddleForGigaflareKills()
        => Negative<DsrP7DragonKingScenario>(CasterDps)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(70.30f, to: Vector2.Zero)
            .ShouldKill(ActionId.GigaflaresEdgeFirst, CasterDps);
}
