#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P3Nidhogg;
using AnoMech.Scenarios.Dsr.Strength;
using AnoMech.Scenarios.Fru.LightRampant;

namespace AnoMech.Tests;

public class SpectatorTests
{
    private static IEnumerable<TestCaseData> Runs() =>
        from scenario in new[] { typeof(DsrP3NidhoggScenario), typeof(DsrStrengthScenario), typeof(FruLightRampantScenario) }
        from role in Enum.GetValues<PartyRole>()
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
        select new TestCaseData(scenario, role, difficulty);

    [TestCaseSource(nameof(Runs))]
    public void EightOrdinaryBotsClearWithoutHumanInput(Type scenario, PartyRole role, BotDifficulty difficulty)
    {
        FakeGame? fake = null;
        var checkedParty = false;
        var run = ScenarioRun.Execute(scenario, 0, 3400 + (int)role, new()
        {
            Spectator = true, PlayerRole = role, BotDifficulty = difficulty, UserActionsEnabled = true,
            ShowSimulatedParty = false, GodMode = true,
            ConfigureNative = f => fake = f,
            Probe = p =>
            {
                if (!p.Crossed(3)) return;
                checkedParty = true;
                Assert.That(DebugBotControl.Enabled, Is.False);
                Assert.That(fake!.UserActions.SpectatorStarts, Is.EqualTo(1));
                Assert.That(p.Game.GodMode, Is.False);
                Assert.That(p.World.ShowSimulatedParty, Is.True);
                Assert.That(p.World.Party.Player, Is.Null);
                Assert.That(p.World.Party.AllMembers().Count(), Is.EqualTo(8));
                Assert.That(p.World.Party.AllMembers(), Has.All.TypeOf<SimPartyNpc>());
                Assert.That(fake!.SpectatorCamera.Target, Is.SameAs(p.Member(role)!.Proxy));
                Assert.That(fake.PlayerInput.SpectatorInput, Is.True);
                Assert.That(fake.PlayerInput.SteerDirection, Is.Null);
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedParty, Is.True);
        Assert.That(fake!.SpectatorCamera.Target, Is.Null);
        Assert.That(fake.PlayerInput.SpectatorInput, Is.False);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
    }

    [TestCase(false)] [TestCase(true)]
    public void PauseRestartLeaveAndNormalPracticeRestorePresentation(bool hiddenBefore)
    {
        FakeGame? fake = null;
        var paused = false;
        var restarted = false;
        var returned = false;
        RunScenarioParams? restart = null;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 73, new()
        {
            Spectator = true, PlayerRole = PartyRole.MeleeDpsA, StopAt = 5,
            ConfigureNative = f => { fake = f; f.BattleCharas.Player.IsDrawObjectVisible = !hiddenBefore; },
            BeforeStart = (g, f) => g.RunStarted += (p, _) => restart = p,
            Probe = p =>
            {
                if (!paused && p.Time > 2)
                {
                    paused = true;
                    p.Game.Paused = true;
                    var time = p.Time;
                    var positions = p.World.Party.AllMembers().Select(m => m.Position).ToArray();
                    for (var i = 0; i < 30; i++) fake!.Frame(1f / 60, p.Game.Tick);
                    Assert.That(p.Time, Is.EqualTo(time));
                    Assert.That(p.World.Party.AllMembers().Select(m => m.Position), Is.EqualTo(positions));
                    p.Game.Paused = false;
                    Assert.That(fake!.PlayerInput.SpectatorInput, Is.True);
                }
                if (!restarted && p.Time > 3)
                {
                    restarted = true;
                    p.Game.Restart();
                }
                else if (restarted && !returned && p.Time > 4)
                {
                    returned = true;
                    p.Game.StartScenario(restart! with
                    {
                        Party = new LocalPartyProvider { RoleOverride = PartyRole.MeleeDpsA }, WatchedRole = null,
                    });
                }
                if (returned && p.Game.WatchedRole is null)
                {
                    Assert.That(fake!.PlayerInput.SpectatorInput, Is.False);
                    Assert.That(fake.SpectatorCamera.Target, Is.Null);
                    Assert.That(fake.BattleCharas.Player.IsDrawObjectVisible, Is.EqualTo(!hiddenBefore));
                    Assert.That(p.World.Party.Player, Is.Not.Null);
                    Assert.That(p.World.Party.AllMembers().OfType<SimPartyNpc>().Count(), Is.EqualTo(7));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(paused && restarted && returned, Is.True);
        Assert.That(fake!.BattleCharas.Player.IsDrawObjectVisible, Is.EqualTo(!hiddenBefore));
        Assert.That(fake.PlayerInput.SpectatorInput, Is.False);
    }

    [Test]
    public void SwitchingFromHumanPracticeReleasesItsControlAfflictions()
    {
        FakeGame? fake = null;
        RunScenarioParams? current = null;
        var requested = false;
        var checkedTransition = false;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 73, new()
        {
            PlayerRole = PartyRole.MeleeDpsA, StopAt = 4,
            ConfigureNative = f => fake = f,
            BeforeStart = (g, f) => g.RunStarted += (p, _) => current = p,
            Probe = p =>
            {
                if (!requested && p.Time > 2)
                {
                    requested = true;
                    fake!.PlayerConditions.SetAfflictions(true, true);
                    p.Game.StartScenario(current! with
                    {
                        Party = new LocalPartyProvider { Spectator = true }, WatchedRole = PartyRole.MeleeDpsA,
                    });
                }
                if (p.Game.WatchedRole is null) return;
                checkedTransition = true;
                Assert.That(p.World.Party.Player, Is.Null);
                Assert.That(fake!.PlayerConditions.StatusAffliction, Is.False);
                Assert.That(fake.PlayerConditions.ControlAffliction, Is.False);
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedTransition, Is.True);
        Assert.That(fake!.PlayerInput.SpectatorInput, Is.False);
    }

    [Test]
    public void MissingCameraRefusesBeforeCreatingPartyActors()
    {
        FakeGame? fake = null;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            Spectator = true, StopAt = 1, WriteArtifactsOnFailure = false,
            ConfigureNative = f => { fake = f; f.SpectatorCamera.Available = false; },
        });
        Assert.That(run.Failure, Is.EqualTo("did not start"));
        Assert.That(fake!.SpectatorCamera.Target, Is.Null);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(fake.PlayerInput.SpectatorInput, Is.False);
    }

    [Test]
    public void WatchedBotStillDiesAndFailsTheRun()
    {
        var killed = false;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            Spectator = true, PlayerRole = PartyRole.MeleeDpsA, GodMode = true,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (killed || p.Time < 2) return;
                killed = true;
                p.Member(PartyRole.MeleeDpsA)!.Die(123, "spectator failure test");
            },
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MeleeDpsA), Is.True);
        Assert.That(run.Passed, Is.False);
    }
}
