#pragma warning disable CA1416
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.FallOfFaith;
using AnoMech.Core.Game.Ai;
using AnoMech.Windows;

namespace AnoMech.Tests;

public class PauseInvestigationTests
{
    [TestCaseSource(typeof(SharedPracticeTests), nameof(SharedPracticeTests.AllScenarios))]
    public void InternalFreezePreservesAuthoritativeStateAndResumesAllNineScenarios(Type type)
    {
        var cycles = 0;
        FakeGame? native = null;
        var run = ScenarioRun.Execute(type, 0, 3, new()
        {
            ShowSimulatedParty = false, GodMode = true, BotDifficulty = BotDifficulty.Hard,
            ConfigureNative = f => native = f,
            Probe = p =>
            {
                if (!new[] { 2f, 8f, 12.5f, 16.05f, 19.3f }.Any(p.Crossed)) return;
                var time = p.World.Events.Elapsed;
                var members = p.World.Party.ActiveMembers().ToArray();
                var positions = members.Select(m => m.Position).ToArray();
                var durations = members.SelectMany(m => m.ActiveStatuses).Select(s => s.RemainingTime).ToArray();
                var departures = p.World.BotMovement.Departures.Count;
                Assert.That(ScenarioPauseControls.Press(p.Game, false), Is.True);
                var captured = p.Game.PauseStatuses.ToArray();
                Assert.That(captured.Count(c => c.Character is ISimPartyMember), Is.EqualTo(8));
                Assert.That(Natives.PlayerInput.PauseInput, Is.True);
                try
                {
                    for (var i = 0; i < 10; i++) native!.Frame(.5f, p.Game.Tick);
                    Assert.That(p.World.Events.Elapsed, Is.EqualTo(time));
                    Assert.That(p.Game.EventClockNow, Is.EqualTo(time));
                    Assert.That(members.Select(m => m.Position), Is.EqualTo(positions));
                    Assert.That(members.SelectMany(m => m.ActiveStatuses).Select(s => s.RemainingTime), Is.EqualTo(durations));
                    Assert.That(p.World.BotMovement.Departures.Count, Is.EqualTo(departures));
                    cycles++;
                }
                finally { Assert.That(ScenarioPauseControls.Press(p.Game, false), Is.True); }
                Assert.That(Natives.PlayerInput.PauseInput, Is.False);
                foreach (var entry in captured)
                    Assert.That(entry.Character.ActiveStatuses.Select(s => s.Snapshot), Is.EqualTo(entry.Statuses));
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(cycles, Is.EqualTo(5));
    }

    [Test]
    public void NativeCastMayAdvanceButPauseOwnsInputAndVirtualDurationsRemainFrozen()
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
                native.PlayerConditions.SetAfflictions(false, false);
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
                    Assert.That(native.PlayerConditions.StatusAffliction && native.PlayerConditions.ControlAffliction, Is.True);
                    Assert.That(input.PauseInput, Is.True);
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
