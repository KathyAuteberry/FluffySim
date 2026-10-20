#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P4Eyes;
namespace AnoMech.Tests;
public class DsrP4OrbRepairTests
{
    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.OffTank)]
    [TestCase(PartyRole.RegenHealer)] [TestCase(PartyRole.ShieldHealer)]
    [TestCase(PartyRole.MeleeDpsA)] [TestCase(PartyRole.MeleeDpsB)]
    [TestCase(PartyRole.PhysRangedDps)] [TestCase(PartyRole.CasterDps)]
    public void ActualOrbRecipientsAreTwoSupportPairsAndFourDistinctDps(PartyRole role)
    {
        var scenario = new DsrP4EyesScenario();
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 100 + (int)role, new()
        { PlayerRole = role, ScenarioFactory = () => scenario, StopAt = 80 });
        Assert.That(run.Passed, run.ToString);
        var yellow = scenario.OrbHits.Where(h => h.Yellow).ToArray();
        var blue = scenario.OrbHits.Where(h => !h.Yellow).ToArray();
        Assert.That(yellow.GroupBy(h => h.Position).Select(g => g.Count()), Is.EquivalentTo(new[] { 2, 2 }));
        Assert.That(yellow.Select(h => h.Role), Is.EquivalentTo(new[] { PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer }));
        Assert.That(blue.Select(h => h.Role), Is.EquivalentTo(new[] { PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps }));
        Assert.That(blue.Select(h => h.Position).Distinct().Count(), Is.EqualTo(4));
        Assert.That(scenario.OrbHits.All(h => h.Red && !h.Resistance), Is.True);
        foreach (var member in Enum.GetValues<PartyRole>())
        {
            var times = scenario.Transfers.Where(t => t.From == member || t.To == member).Select(t => t.Time).ToArray();
            for (var i = 1; i < times.Length; i++) Assert.That(times[i] - times[i - 1], Is.GreaterThanOrEqualTo(2.99f));
        }
        Assert.That(scenario.LastState!.DiveTargets.All(d => d.Count == 2), Is.True);
    }

    private static Vector2 Blue(PartyRole role) => role switch
    {
        PartyRole.MeleeDpsA => new(-10, -4.4f), PartyRole.MeleeDpsB => new(-10, 4.4f),
        PartyRole.PhysRangedDps => new(10, 4.4f), _ => new(10, -4.4f),
    };

    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.OffTank)]
    [TestCase(PartyRole.RegenHealer)] [TestCase(PartyRole.ShieldHealer)]
    [TestCase(PartyRole.MeleeDpsA)] [TestCase(PartyRole.MeleeDpsB)]
    [TestCase(PartyRole.PhysRangedDps)] [TestCase(PartyRole.CasterDps)]
    public void ManualHumanAssignmentRemainsAvailableWithoutRacingBots(PartyRole role)
    {
        var dps = (int)role >= (int)PartyRole.MeleeDpsA;
        var scenario = new DsrP4EyesScenario();
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 100 + (int)role, new()
        {
            PlayerRole = role, ScenarioFactory = () => scenario, StopAt = 50,
            Takeovers = [new(41, Vector2.Zero), new(dps ? 46.8f : 44.5f, dps ? Blue(role) : new Vector2(-11, 0))],
            Probe = p =>
            {
                if (dps && p.Crossed(46.7f)) Assert.That(scenario.OrbHits.Any(h => !h.Yellow && h.Role == role), Is.False);
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.OrbHits.Count(h => h.Role == role), Is.EqualTo(1));
        Assert.That(scenario.OrbHits.Count(h => !h.Yellow), Is.EqualTo(4));
    }

    [Test]
    public void SupportDeliberatelyEnteringBlueBlastStillDiesWithFireResistanceDown()
    {
        var scenario = new DsrP4EyesScenario();
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.RegenHealer, ScenarioFactory = () => scenario, StopAt = 50,
            Takeovers = [new(41, Vector2.Zero), new(45.3f, new Vector2(-10, -4.4f))], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.RegenHealer && d.Cause.Contains("second orb")), Is.True, run.ToString);
        Assert.That(scenario.OrbHits.Any(h => !h.Yellow && h.Role == PartyRole.RegenHealer && h.Resistance), Is.True);
    }

    [Test]
    public void DelayedHumanDpsSoakDoesNotCatchItsSupportPartner()
    {
        var scenario = new DsrP4EyesScenario();
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MeleeDpsB, ScenarioFactory = () => scenario, StopAt = 50,
            Takeovers = [new(41, Vector2.Zero), new(46.8f, new Vector2(-10, 4.4f))],
        });
        Assert.That(run.Passed, run.ToString);
    }
}
