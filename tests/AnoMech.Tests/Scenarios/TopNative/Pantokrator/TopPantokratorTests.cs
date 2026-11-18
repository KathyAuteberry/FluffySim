using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.TopNative.Pantokrator;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

public class TopPantokratorTests
{
    [Test]
    public void StandingInTheFirstFlameThrowerKills()
        => Negative<TopPantokratorScenario>(MeleeDpsA)
            .Overrides<TopPantokratorStateOverrides>(o => o.FirstFlameBearing = 0)
            .TeleportAt(TopPantokratorScenario.FirstFlameCast + 0.5f, to: new Vector2(0f, -10f))
            .ShouldKill(ActionId.FlameThrowerFirst, MeleeDpsA);

    [Test]
    public void ASecondRunOnTheSameScenarioStillCastsTheFlames()
    {
        var scenario = new TopPantokratorScenario();
        ScenarioRun Once() => ScenarioRun.Execute(typeof(TopPantokratorScenario), 0, Random.Shared.Next(), new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = MeleeDpsA,
            StopAt = TopPantokratorScenario.FirstFlameCast + 9f,
            Overrides = o => ((TopPantokratorStateOverrides)o).FirstFlameBearing = 0,
            Takeovers = [new Takeover(TopPantokratorScenario.FirstFlameCast + 0.5f, new Vector2(0f, -10f))],
        });
        var first = Once();
        Assert.That(first.Deaths.Select(d => d.ActionId), Does.Contain(ActionId.FlameThrowerFirst), first.ToString);
        var second = Once();
        Assert.That(second.Deaths.Select(d => d.ActionId), Does.Contain(ActionId.FlameThrowerFirst), second.ToString);
    }

    [TestCase(1, true, false, true, false)]
    [TestCase(2, false, false, false, false)]
    [TestCase(3, true, true, true, true)]
    [TestCase(4, null, true, null, false)]
    public void TheChosenNumberAndAdjustingPairsAreDealtAndCleared(int mine, bool? ones, bool? twos, bool? threes, bool? fours)
    {
        bool?[] wanted = [ones, twos, threes, fours];
        foreach (var seed in Enumerable.Range(1, 6))
        {
            var scenario = new TopPantokratorScenario();
            var run = ScenarioRun.Execute(typeof(TopPantokratorScenario), 0, seed, new()
            {
                ScenarioFactory = () => scenario,
                PlayerRole = MeleeDpsA,
                Overrides = o =>
                {
                    var overrides = (TopPantokratorStateOverrides)o;
                    overrides.Number[MeleeDpsA] = mine;
                    (overrides.OnesAdjust, overrides.TwosAdjust, overrides.ThreesAdjust, overrides.FoursAdjust) = (ones, twos, threes, fours);
                },
            });
            Assert.That(run.Deaths, Is.Empty, run.ToString);
            var numbers = scenario.LastState!.Numbers;
            Assert.That(numbers[MeleeDpsA], Is.EqualTo(mine));
            for (var n = 1; n <= 4; n++)
                if (wanted[n - 1] is { } adjusts)
                    Assert.That(TopPantokratorPairs.Adjusts(numbers.ByRole, n), Is.EqualTo(adjusts), $"{n}s, seed {seed}");
        }
    }

    [Test]
    public void AnOddNumberOfAdjustingPairsCannotHappen()
    {
        var overrides = new TopPantokratorStateOverrides { OnesAdjust = true, TwosAdjust = false, ThreesAdjust = false, FoursAdjust = false };
        Assert.That(overrides.Validate().Any, Is.True);
        Assert.That(TopPantokratorPairs.Deals(new Dictionary<PartyRole, int>(), overrides.AdjustingPairs), Is.Empty);
    }
}
