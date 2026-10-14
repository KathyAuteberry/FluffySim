#pragma warning disable CA1416
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.FallOfFaith;

namespace AnoMech.Tests;

public class PauseInvestigationTests
{
    [TestCaseSource(typeof(SharedPracticeTests), nameof(SharedPracticeTests.AllScenarios))]
    public void InternalFreezePreservesAuthoritativeStateAndResumesAllNineScenarios(Type type)
    {
        var cycles = 0;
        var run = ScenarioRun.Execute(type, 0, 3, new()
        {
            ShowSimulatedParty = false, GodMode = true,
            Probe = p =>
            {
                if (!new[] { 2f, 8f, 12.5f, 16.05f, 19.3f }.Any(p.Crossed)) return;
                var time = p.World.Events.Elapsed;
                var members = p.World.Party.ActiveMembers().ToArray();
                var positions = members.Select(m => m.Position).ToArray();
                var durations = members.SelectMany(m => m.ActiveStatuses).Select(s => s.RemainingTime).ToArray();
                var departures = p.World.BotMovement.Departures.Count;
                p.Game.Paused = true;
                try
                {
                    for (var i = 0; i < 10; i++) p.Game.Tick(.5f);
                    Assert.That(p.World.Events.Elapsed, Is.EqualTo(time));
                    Assert.That(p.Game.EventClockNow, Is.EqualTo(time));
                    Assert.That(members.Select(m => m.Position), Is.EqualTo(positions));
                    Assert.That(members.SelectMany(m => m.ActiveStatuses).Select(s => s.RemainingTime), Is.EqualTo(durations));
                    Assert.That(p.World.BotMovement.Departures.Count, Is.EqualTo(departures));
                    cycles++;
                }
                finally { p.Game.Paused = false; }
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(cycles, Is.EqualTo(5));
    }

    [Test]
    public void InternalFreezeDoesNotFreezeNativeCastOrSupplyHumanInputLock()
    {
        FakeGame? native = null;
        var scenario = new FruFallOfFaithScenario();
        var observed = false;
        ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, StopAt = 5.2f,
            ConfigureNative = f => native = f,
            Probe = p =>
            {
                if (!p.Crossed(5)) return;
                var boss = scenario.SourceActors.Single(s => s.BNpcBaseId == 0x459B);
                var castBefore = boss.Proxy!.CurrentCastTime;
                var eventBefore = p.World.Events.Elapsed;
                var input = (FakeLocalPlayerInput)Natives.PlayerInput;
                input.ZeroMovement = input.DisableAllActions = false;
                p.World.PracticeInvulnerability.Start();
                p.World.PracticeAntiKnockback.Start();
                Assert.That(p.World.PracticeInvulnerability.Activate(true), Is.True);
                Assert.That(p.World.PracticeAntiKnockback.Activate(true), Is.True);
                p.Game.Paused = true;
                try
                {
                    native!.Frame(1f, p.Game.Tick);
                    Assert.That(p.World.Events.Elapsed, Is.EqualTo(eventBefore));
                    Assert.That(boss.Proxy.CurrentCastTime, Is.GreaterThan(castBefore));
                    Assert.That(input.ZeroMovement || input.DisableAllActions, Is.False);
                    Assert.That(p.World.PracticeInvulnerability.Remaining, Is.EqualTo(10));
                    Assert.That(p.World.PracticeAntiKnockback.Remaining, Is.EqualTo(6));
                    observed = true;
                }
                finally
                {
                    p.Game.Paused = false;
                    p.World.PracticeInvulnerability.Clear();
                    p.World.PracticeAntiKnockback.Clear();
                }
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(observed, Is.True);
    }
}
