using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.TopNative;
using AnoMech.Scenarios.TopNative.BlueScreen;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

public class TopBlueScreenTests
{
    private static Vector2 MainTankSpread
    {
        get
        {
            var p = TopNativeConstants.Polar(293f, 13.7f);
            return new Vector2(p.X, p.Z);
        }
    }

    [Test]
    public void TanksRangedHealersAndMeleeSpreadWithTheMainTankWestAndTheOffTankEast()
    {
        var x = new Dictionary<PartyRole, float>();
        var run = ScenarioRun.Execute(typeof(TopBlueScreenScenario), 0, 3, new()
        {
            StopAt = TopBlueScreenScenario.LinesAt[0],
            Probe = p =>
            {
                if (!p.Crossed(TopBlueScreenScenario.LinesAt[0] - 0.1f)) return;
                foreach (var role in PerRole.All) x[role] = p.Member(role)!.Position.X;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(new[] { MainTank, PhysRangedDps, RegenHealer, MeleeDpsA }.Select(r => x[r]), Is.All.LessThan(0f), "west");
        Assert.That(new[] { OffTank, CasterDps, ShieldHealer, MeleeDpsB }.Select(r => x[r]), Is.All.GreaterThan(0f), "east");
    }

    [Test]
    public void StayingOnTheSpreadSpotAfterTheFirstProteansIsHitByYourLine()
        => Negative<TopBlueScreenScenario>(MainTank)
            .TeleportAt(TopBlueScreenScenario.LinesAt[0] + 1f, to: MainTankSpread)
            .ShouldKill(ActionId.WaveCannonP4Line, MainTank);

    [Test]
    public void StayingOutsideDuringTheSecondWaveIsHitByTheRepeaterRing()
        => Negative<TopBlueScreenScenario>(MainTank)
            .TeleportAt(TopBlueScreenScenario.LinesAt[1] + 0.7f, to: MainTankSpread)
            .ShouldKill(ActionId.WaveRepeater3, MainTank);

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void AForcedStackMarkerLandsInItsWaveAndEveryoneLives(int wave)
    {
        foreach (var role in new[] { MainTank, MeleeDpsB, PhysRangedDps })
        {
            var scenario = new TopBlueScreenScenario();
            var run = ScenarioRun.Execute(typeof(TopBlueScreenScenario), 0, 11 + wave, new()
            {
                ScenarioFactory = () => scenario,
                PlayerRole = role,
                Overrides = o => ((TopBlueScreenStateOverrides)o).Marker(wave)[role] = true,
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(scenario.LastState!.Markers[wave], Does.Contain(role));
        }
    }

    [Test]
    public void BothMarkersOnOneSideStillMakeTwoStacksOfFour()
    {
        var scenario = new TopBlueScreenScenario();
        var run = ScenarioRun.Execute(typeof(TopBlueScreenScenario), 0, 5, new()
        {
            ScenarioFactory = () => scenario,
            Overrides = o =>
            {
                var overrides = (TopBlueScreenStateOverrides)o;
                for (var wave = 0; wave < TopBlueScreenState.Waves; wave++)
                {
                    overrides.Marker(wave)[wave == 1 ? OffTank : MainTank] = true;
                    overrides.Marker(wave)[wave == 1 ? ShieldHealer : RegenHealer] = true;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
    }
}
