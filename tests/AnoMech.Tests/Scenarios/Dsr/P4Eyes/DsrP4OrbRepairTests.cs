#pragma warning disable CA1416
using System.Numerics;
using System.Text.Json;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
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

    [TestCase(PartyRole.MainTank, BotDifficulty.Easy, 548184525)]
    [TestCase(PartyRole.MainTank, BotDifficulty.Easy, 1585846992)]
    [TestCase(PartyRole.MainTank, BotDifficulty.Hard, 548184525)]
    [TestCase(PartyRole.MainTank, BotDifficulty.Hard, 1585846992)]
    [TestCase(PartyRole.RegenHealer, BotDifficulty.Easy, 548184525)]
    [TestCase(PartyRole.RegenHealer, BotDifficulty.Hard, 548184525)]
    public void NativeLateMtHandoffDoesNotTransferBackToBlueSupport(PartyRole human, BotDifficulty difficulty, int seed)
    {
        var scenario = new DsrP4EyesScenario();
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, seed, new()
        {
            PlayerRole = human, BotDifficulty = difficulty, ScenarioFactory = () => scenario, StopAt = 80,
            Takeovers = [new(41, new Vector2(-14.4f, -1)), new(46, new Vector2(-12.34f, 0)), new(46.3f, Vector2.Zero)],
            Probe = p => { if (p.Crossed(50)) DebugBotControl.Enabled = true; },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Transfers.Any(t => t.Time > 41 && t.Time < 45 && t.From == human && t.To.IsDps()), Is.True);
        Assert.That(scenario.Transfers.Any(t => t.Time > 41 && t.Time < 49.06f && !t.From.IsDps() && !t.To.IsDps()), Is.False);
        Assert.That(scenario.OrbHits.Count(h => !h.Yellow), Is.EqualTo(4));
        Assert.That(scenario.OrbHits.Where(h => !h.Yellow).All(h => h.Time < 49.06f && h.Red && !h.Resistance), Is.True);
        Assert.That(scenario.OrbHits.Single(h => !h.Yellow && h.Role == PartyRole.CasterDps).Position, Is.EqualTo(new Vector3(10, 0, -7)));
        Assert.That(scenario.LastState!.DiveTargets.All(d => d.Count == 2), Is.True);
    }

    [Test]
    public void HumanMissingOwnOrbReportsResponsibilityAndActualState()
    {
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 548184525, new()
        {
            PlayerRole = PartyRole.CasterDps, StopAt = 50,
            Takeovers = [new(41, Vector2.Zero)], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("orb left to burst")), Is.True);
        const string tag = "[EyesOrb] ";
        var events = AnoMech.Core.DiagnosticLog.Snapshot().Where(l => l.Contains(tag))
            .Select(l => JsonDocument.Parse(l[(l.IndexOf(tag, StringComparison.Ordinal) + tag.Length)..])).ToArray();
        try
        {
            var failure = events.Single(e => e.RootElement.GetProperty("state").GetString() == "expired").RootElement;
            Assert.That(failure.GetProperty("humanResponsible").GetBoolean(), Is.True);
            Assert.That(failure.GetProperty("assigned")[0].GetString(), Is.EqualTo("CasterDps"));
            Assert.That(failure.GetProperty("failure").GetString(), Is.EqualTo("assigned soaker did not reach orb"));
            Assert.That(failure.GetProperty("position").GetProperty("x").GetSingle(), Is.EqualTo(10));
            Assert.That(failure.GetProperty("position").GetProperty("z").GetSingle(), Is.EqualTo(-7));
        }
        finally { foreach (var entry in events) entry.Dispose(); }
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
