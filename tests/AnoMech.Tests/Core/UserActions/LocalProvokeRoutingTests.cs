#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.P5Death;
using AnoMech.Scenarios.Dsr.P5Wrath;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Tests;

public class LocalProvokeRoutingTests
{
    private static IEnumerable<TestCaseData> Tanks() =>
        from wrath in new[] { true, false }
        from job in new[] { 19, 21, 32, 37 }
        from enabled in new[] { false, true }
        select new TestCaseData(wrath, job, enabled);

    [TestCaseSource(nameof(Tanks))]
    public void ExecutionCallbackSwapsDuringLockedHeelWithoutStance(bool wrath, int job, bool enabled)
    {
        IScenario scenario = wrath ? new DsrP5WrathScenario() : new DsrP5DeathScenario();
        for (var repeat = 0; repeat < 2; repeat++) Run(scenario, wrath, job, enabled, "during");
    }

    [TestCase(true, "after")] [TestCase(false, "after")]
    [TestCase(true, "early")] [TestCase(false, "early")]
    [TestCase(true, "late")] [TestCase(false, "late")]
    [TestCase(true, "missing")] [TestCase(false, "missing")]
    [TestCase(true, "wrong")] [TestCase(false, "wrong")]
    [TestCase(true, "none")] [TestCase(false, "none")]
    [TestCase(true, "native-rejected")] [TestCase(false, "native-rejected")]
    [TestCase(true, "unchanged-sequence")] [TestCase(false, "unchanged-sequence")]
    [TestCase(true, "paused")] [TestCase(false, "paused")]
    [TestCase(true, "transition")] [TestCase(false, "transition")]
    public void RoutingFailuresRemainDistinctFromValidSwaps(bool wrath, string press)
        => Run(wrath ? new DsrP5WrathScenario() : new DsrP5DeathScenario(), wrath, 32, false, press);

    [TestCase(true)] [TestCase(false)]
    public void RestartKeepsTheInputSubscriberAndResetsTheRunAndRecast(bool wrath)
    {
        IScenario scenario = wrath ? new DsrP5WrathScenario() : new DsrP5DeathScenario();
        FakeGame native = null!;
        UserActions? actions = null;
        var restarted = false;
        TankSwapSequence? first = null;
        TankSwapSequence Sequence() => (wrath ? ((DsrP5WrathScenario)scenario).TankSwap : ((DsrP5DeathScenario)scenario).TankSwap)!;
        var heel = wrath ? 71.84f : 61f;
        var last = wrath ? 82.24f : 71.3f;
        ScenarioRun run;
        try
        {
            run = ScenarioRun.Execute(scenario.GetType(), 0, 0, new()
            {
                ScenarioFactory = () => scenario, PlayerRole = PartyRole.OffTank, UserActionsEnabled = true,
                StopAt = wrath ? 89 : 85,
                ConfigureNative = f => native = f,
                BeforeStart = (_, f) =>
                {
                    actions = new UserActions(f.PlayerInput);
                    f.UserActions.ScenarioStarting = actions.OnScenarioStart;
                },
                Probe = p =>
                {
                    if (p.Crossed(heel - 1))
                    {
                        DebugBotControl.Enabled = false;
                        p.World.Party.Player!.StopMoving();
                        p.World.Party.Player.SetPosition(new Vector3(2, 0, 4));
                        p.World.Party.Player.AddStatus(1191, 20);
                    }
                    if (p.Crossed(heel + .6f))
                    {
                        var boss = p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == DsrP5Constants.BNpcBaseId.DarkThordan);
                        if (restarted)
                        {
                            native.PlayerInput.ReportNativeExecution(ActionType.Action, 7533, boss.GameObjectId.ObjectId, 1);
                            Assert.That(Sequence().Holder, Is.EqualTo(PartyRole.MainTank));
                        }
                        native.PlayerInput.ReportNativeExecution(ActionType.Action, 7533, boss.GameObjectId.ObjectId, restarted ? (ushort)2 : (ushort)1);
                        Assert.That(Sequence().Holder, Is.EqualTo(PartyRole.OffTank));
                    }
                    if (restarted || !p.Crossed(last + 1)) return;
                    first = Sequence();
                    restarted = true;
                    p.Game.Reset();
                    native.Frame(0, p.Game.Tick);
                    p.Game.StartScenario(new RunScenarioParams(scenario, new LocalPartyProvider { RoleOverride = PartyRole.OffTank }, 0, 0, 0));
                    native.Frame(ScenarioRun.FrameSeconds, p.Game.Tick);
                    DebugBotControl.Enabled = true;
                }, WriteArtifactsOnFailure = false,
            });
        }
        finally { actions?.Dispose(); }
        Assert.That(run.Passed, run.ToString);
        Assert.That(restarted, Is.True);
        Assert.That(native.Zone.ResetRecasts.Count(a => a == 7533), Is.EqualTo(2));
        foreach (var sequence in new[] { first!, Sequence() })
        {
            Assert.That(sequence.Snapshots.Select(s => s.Target), Is.EqualTo(new[] { PartyRole.MainTank, PartyRole.OffTank, PartyRole.OffTank, PartyRole.OffTank }));
            foreach (var hit in sequence.Snapshots)
                Assert.That(hit.Recipients, Is.EqualTo(new[] { hit.Target }));
        }
    }

    private static void Run(IScenario scenario, bool wrath, int job, bool enabled, string press)
    {
        FakeGame native = null!;
        UserActions? actions = null;
        TankSwapSequence Sequence() => (wrath ? ((DsrP5WrathScenario)scenario).TankSwap : ((DsrP5DeathScenario)scenario).TankSwap)!;
        var heel = wrath ? 71.84f : 61f;
        var cleave = wrath ? 78.94f : 68f;
        var pressAt = press switch { "early" => heel - .2f, "after" => cleave - .4f, "late" => cleave + .4f, _ => heel + .6f };
        var observed = 0;
        var relayed = 0;
        var shouldPass = press is "during" or "after";
        ScenarioRun run;
        try
        {
            run = ScenarioRun.Execute(scenario.GetType(), 0, 0, new()
            {
                ScenarioFactory = () => scenario, PlayerRole = PartyRole.OffTank,
                UserActionsEnabled = true, StopAt = wrath ? 89 : 85,
                ConfigureNative = f => { native = f; f.BattleCharas.Player.ClassJob = (byte)job; },
                BeforeStart = (_, f) =>
                {
                    actions = new UserActions(f.PlayerInput);
                    actions.Acted += action => { if (action.ActionId == 7533) relayed++; };
                    if (enabled) actions.Enable();
                },
                Takeovers = [new(heel - 1, new Vector2(2, 4))],
                Probe = p =>
                {
                    if (p.Crossed(heel - .5f))
                    {
                        p.World.Party.Player!.AddStatus(1191, 20);
                        p.World.ActionResolved += (actor, action, _) =>
                        {
                            if (action == 7533 && ReferenceEquals(actor, p.World.Party.Player)) observed++;
                        };
                    }
                    if (press == "missing" || !p.Crossed(pressAt)) return;
                    var boss = p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == DsrP5Constants.BNpcBaseId.DarkThordan);
                    Assert.That(p.World.Party.Player!.ActiveStatuses.Any(s => s.StatusId is 79 or 91 or 743 or 1833), Is.False);
                    var target = press switch { "wrong" => 0xDEADBEEFUL, "none" => 0xE0000000UL, _ => boss.GameObjectId.ObjectId };
                    var sequence = press == "unchanged-sequence" ? (ushort)0 : (ushort)1;
                    native.PlayerInput.PauseInput = press == "paused";
                    native.PlayerInput.TransitionInput = press == "transition";
                    native.PlayerInput.ReportNativeExecution(ActionType.Action, 7533, target, sequence, press != "native-rejected");
                    native.PlayerInput.PauseInput = native.PlayerInput.TransitionInput = false;
                    if (shouldPass)
                    {
                        Assert.That(Sequence().Holder, Is.EqualTo(PartyRole.OffTank));
                        native.PlayerInput.ReportNativeExecution(ActionType.Action, 7533, target, sequence);
                        Assert.That(observed, Is.EqualTo(1));
                    }
                }, WriteArtifactsOnFailure = false,
            });
        }
        finally { actions?.Dispose(); }
        TestContext.WriteLine(run.ToString());
        foreach (var hit in Sequence().Snapshots)
            TestContext.WriteLine($"time={hit.Time:F2} action={hit.Action} target={hit.Target} recipients=[{string.Join(",", hit.Recipients)}] statuses=[{string.Join(",", hit.TargetStatuses)}]");
        Assert.That(run.Passed, Is.EqualTo(shouldPass), run.ToString);
        Assert.That(observed, Is.EqualTo(press is "during" or "after" or "early" or "late" ? 1 : 0));
        Assert.That(relayed, Is.EqualTo(press is "missing" or "native-rejected" or "unchanged-sequence" or "paused" or "transition" ? 0 : 1),
            "upstream action presentation follows native observation, including locally rejected targets, without duplicates");
        var snapshots = Sequence().Snapshots;
        Assert.That(snapshots.First().Target, Is.EqualTo(PartyRole.MainTank));
        Assert.That(snapshots.First().Recipients, Is.EqualTo(new[] { PartyRole.MainTank }));
        Assert.That(snapshots.First().TargetStatuses, Does.Contain(DsrP5Constants.StatusId.SlashingResistanceDown));
        if (shouldPass)
        {
            Assert.That(snapshots, Has.Count.EqualTo(4));
            foreach (var hit in snapshots.Skip(1))
            {
                Assert.That(hit.Target, Is.EqualTo(PartyRole.OffTank));
                Assert.That(hit.Recipients, Is.EqualTo(new[] { PartyRole.OffTank }));
                Assert.That(hit.TargetStatuses, Does.Not.Contain(DsrP5Constants.StatusId.SlashingResistanceDown));
                Assert.That(hit.TargetStatuses, Does.Contain(1191));
            }
        }
        else
        {
            Assert.That(snapshots[1].Target, Is.EqualTo(PartyRole.MainTank));
            Assert.That(run.Deaths.Any(d => d.Cause.Contains("missing MT→ST")), Is.True, run.ToString);
        }
    }
}
