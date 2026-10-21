#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.P5Wrath;
using AnoMech.Scenarios.Dsr.P5Death;
using FFXIVClientStructs.FFXIV.Client.Game;
namespace AnoMech.Tests;
public class DsrP5TankSwapTests
{
    [TestCase(19)] [TestCase(21)] [TestCase(32)] [TestCase(37)]
    public void EveryTankJobUsesTheSameConventionalSwap(int job)
    {
        foreach (var wrath in new[] { true, false })
        foreach (var role in new[] { PartyRole.MainTank, PartyRole.OffTank })
            Run(wrath, role, true, true, job);
    }

    [TestCase(true, PartyRole.MainTank)] [TestCase(false, PartyRole.MainTank)]
    [TestCase(true, PartyRole.OffTank)] [TestCase(false, PartyRole.OffTank)]
    public void ActualSnapshotsHitMtOnceThenStThreeTimes(bool wrath, PartyRole human)
        => Run(wrath, human, true, true);

    [TestCase(true)] [TestCase(false)]
    public void MtReclaimingAggroAfterTheFirstCleaveFailsTheNextSnapshot(bool wrath)
        => Run(wrath, PartyRole.MainTank, true, true, reclaim: true);

    [TestCase(true)] [TestCase(false)]
    public void MissingHumanProvokeLeavesTheVulnerableMtAsActualTarget(bool wrath)
        => Run(wrath, PartyRole.OffTank, false, true);

    [TestCase(true)] [TestCase(false)]
    public void CorrectSwapStillRequiresUnexpiredHumanStMitigation(bool wrath)
        => Run(wrath, PartyRole.OffTank, true, false);

    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.OffTank)]
    public void RepeatedWrathEntryRebuildsAggroThroughCompletion(PartyRole human)
    {
        var scenario = new DsrP5WrathScenario();
        for (var repeat = 0; repeat < 2; repeat++) Run(true, human, true, true, previous: scenario);
    }

    private static void Run(bool wrath, PartyRole human, bool provoke, bool mitigate, int job = 19, bool reclaim = false, IScenario? previous = null)
    {
        IScenario scenario = previous ?? (wrath ? new DsrP5WrathScenario() : new DsrP5DeathScenario());
        TankSwapSequence Sequence() => (wrath ? ((DsrP5WrathScenario)scenario).TankSwap : ((DsrP5DeathScenario)scenario).TankSwap)!;
        var begin = wrath ? 72f : 61.1f;
        var swap = wrath ? 77f : 66f;
        var notified = false;
        var run = ScenarioRun.Execute(scenario.GetType(), 0, 0, new()
        {
            PlayerRole = human, UserActionsEnabled = true, ScenarioFactory = () => scenario,
            StopAt = wrath ? 89 : 72,
            ConfigureNative = f => f.BattleCharas.Player.ClassJob = (byte)job,
            Takeovers = human == PartyRole.MainTank
                ? [new(begin, new Vector2(-2, 4)), new(swap, new Vector2(-6, 0))]
                : [new(begin, new Vector2(2, 4))],
            Probe = p =>
            {
                if (p.Crossed(begin + .1f)) p.World.Party.Player!.AddStatus(1191, mitigate ? 20 : .1f);
                if (reclaim && p.Crossed(wrath ? 79.2f : 68.2f))
                {
                    var boss = p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == DsrP5Constants.BNpcBaseId.DarkThordan);
                    new LocalProvokeHandler().OnAction(ActionType.Action, 7533, boss.GameObjectId.ObjectId);
                }
                if (human == PartyRole.OffTank && provoke && p.Crossed(swap))
                {
                    var boss = p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == DsrP5Constants.BNpcBaseId.DarkThordan);
                    new LocalProvokeHandler().OnAction(ActionType.Action, 7533, boss.GameObjectId.ObjectId);
                    notified = true;
                }
            }, WriteArtifactsOnFailure = false,
        });
        var snapshots = Sequence().Snapshots;
        foreach (var hit in snapshots) TestContext.WriteLine($"t={hit.Time:F2} action={hit.Action} holder={hit.Target} recipients=[{string.Join(",", hit.Recipients)}] statuses=[{string.Join(",", hit.TargetStatuses)}]");
        Assert.That(snapshots.First().Target, Is.EqualTo(PartyRole.MainTank));
        Assert.That(snapshots.First().Recipients, Is.EqualTo(new[] { PartyRole.MainTank }));
        Assert.That(snapshots.First().TargetStatuses, Does.Contain(DsrP5Constants.StatusId.SlashingResistanceDown));
        if (reclaim)
        {
            Assert.That(snapshots[1].Target, Is.EqualTo(PartyRole.OffTank));
            Assert.That(snapshots[2].Target, Is.EqualTo(PartyRole.MainTank));
            Assert.That(run.Deaths.Any(d => d.Cause.Contains("missing MT→ST")), Is.True, run.ToString);
            return;
        }
        if (human == PartyRole.OffTank && !provoke)
        {
            Assert.That(snapshots.Skip(1).First().Target, Is.EqualTo(PartyRole.MainTank));
            Assert.That(run.Deaths.Any(d => d.Cause.Contains("missing MT→ST")), Is.True, run.ToString);
            return;
        }
        Assert.That(snapshots.Skip(1).First().Target, Is.EqualTo(PartyRole.OffTank));
        if (!mitigate)
        {
            Assert.That(run.Deaths.Any(d => d.Role == PartyRole.OffTank && d.Cause.Contains("mitigation")), Is.True, run.ToString);
            return;
        }
        Assert.That(run.Passed, run.ToString);
        if (wrath) Assert.That(((DsrP5WrathScenario)scenario).Completed, Is.True);
        Assert.That(snapshots, Has.Count.EqualTo(4));
        foreach (var hit in snapshots.Skip(1))
        {
            Assert.That(hit.Target, Is.EqualTo(PartyRole.OffTank));
            Assert.That(hit.Recipients, Is.EqualTo(new[] { PartyRole.OffTank }));
            Assert.That(hit.TargetStatuses, Does.Not.Contain(DsrP5Constants.StatusId.SlashingResistanceDown));
        }
        if (human == PartyRole.OffTank) Assert.That(notified, Is.True);
    }
}
