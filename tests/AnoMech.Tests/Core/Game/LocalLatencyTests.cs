#pragma warning disable CA1416
using System.Numerics;
using System.Reflection;
using Dalamud.Plugin.Services;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.UserActions;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.P5Death;
using AnoMech.Scenarios.Fru.Darklit;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Tests;

public class LocalLatencyTests
{
    private static Placement At(float x, float rotation = 0) => new(new Vector3(x, 0, 0), rotation);

    [TestCase(0, .2f, true)] [TestCase(100, .2f, true)] [TestCase(150, .2f, true)]
    [TestCase(0, .02f, true)] [TestCase(100, .02f, false)] [TestCase(150, .02f, false)]
    [TestCase(0, -.02f, false)] [TestCase(100, -.02f, false)] [TestCase(150, -.02f, false)]
    public void DodgeSucceedsOnlyIfMovementReachesTheServerBeforeSnapshot(int rtt, float beforeSnapshot, bool safe)
    {
        var latency = new LocalLatency();
        latency.Begin(rtt, At(0));
        var local = At(0);
        for (var i = 1; i <= 1000; ++i)
        {
            if (i >= (int)MathF.Round((1 - beforeSnapshot) * 1000)) local = At(10);
            latency.Sample(.001f, local);
        }
        Assert.That(latency.ServerPose(local).Position.X > 5, Is.EqualTo(safe));
        if (beforeSnapshot > 0) Assert.That(local.Position.X, Is.EqualTo(10), "Native model moves immediately");
    }

    [Test]
    public void HistorySelectsAnActualSampleAndFacingRatherThanInterpolatingAcrossNativeWarps()
    {
        var latency = new LocalLatency();
        latency.Begin(150, At(0));
        latency.Sample(.05f, At(10, 1));
        latency.Sample(.05f, At(20, 2));
        Assert.That(latency.ServerPose(At(20)), Is.EqualTo(At(0)));
        latency.Sample(.03f, At(30, 3));
        Assert.That(latency.ServerPose(At(30)), Is.EqualTo(At(10, 1)));
        latency.ResetPose(At(100, 4));
        Assert.That(latency.ServerPose(At(100)), Is.EqualTo(At(100, 4)));
        latency.Clear();
        Assert.That(latency.ServerPose(At(200)), Is.EqualTo(At(200)));
    }

    [TestCase(0)] [TestCase(100)] [TestCase(150)]
    public void AcceptedEffectsUseOutgoingHalfOfRttAndRemainOrdered(int rtt)
    {
        var latency = new LocalLatency();
        latency.Begin(rtt, At(0));
        var events = new EventScheduler();
        var effects = new List<int>();
        latency.Accept(events, 1, () => effects.Add(1));
        latency.Accept(events, 1, () => effects.Add(2));
        if (rtt > 0)
        {
            events.Tick(rtt / 2000f - .001f);
            Assert.That(effects, Is.Empty);
            events.Tick(.002f);
        }
        Assert.That(effects, Is.EqualTo(new[] { 1, 2 }));
        events.Tick(1);
        Assert.That(effects, Has.Count.EqualTo(2));
    }

    [TestCase(100, .06f, true)] [TestCase(150, .06f, false)]
    public void MitigationSubmissionEitherReachesOrMissesAnAuthoritativeDeadline(int rtt, float lead, bool protectedAtSnapshot)
    {
        var latency = new LocalLatency();
        latency.Begin(rtt, At(0));
        var events = new EventScheduler();
        var active = false;
        bool? snapshot = null;
        events.Add(lead, () => snapshot = active);
        latency.Accept(events, 1, () => active = true);
        events.Tick(.2f);
        Assert.That(snapshot, Is.EqualTo(protectedAtSnapshot));
        Assert.That(active, Is.True);
    }

    [Test]
    public void PauseStopsAcceptanceAndResetDropsQueuedEffects()
    {
        var latency = new LocalLatency();
        latency.Begin(150, At(0));
        var events = new EventScheduler();
        var count = 0;
        latency.Accept(events, 1, () => count++);
        events.Suspended = true;
        events.Tick(10);
        Assert.That(count, Is.Zero);
        events.Suspended = false;
        events.Tick(.1f);
        Assert.That(count, Is.EqualTo(1));
        latency.Accept(events, 1, () => count++);
        events.Clear();
        latency.Clear();
        events.Tick(10);
        Assert.That(count, Is.EqualTo(1));
    }

    [TestCase(-10, 0)] [TestCase(0, 0)] [TestCase(150, 150)] [TestCase(500, 300)]
    public void PersistedConfigurationBoundsRtt(int input, int expected)
    {
        var configuration = new Configuration { SimulatedPingRttMs = input };
        var saved = Newtonsoft.Json.JsonConvert.SerializeObject(configuration);
        Assert.That(Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(saved)!.SimulatedPingRttMs, Is.EqualTo(expected));
    }

    [Test]
    public void HumanMechanicPoseLagsWithoutMovingNativeModelAndTeleportClearsHistory()
    {
        var native = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Config), new Configuration { SimulatedPingRttMs = 150 });
        using var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        game.RunScenario(new LegacyRunScenarioParams(new FruDarklitScenario(), PartyRole.MainTank, 0, 0, 0));
        native.Frame(.01f, game.Tick);
        var start = game.Player!.Position;
        native.BattleCharas.Player.Position += new Vector3(10, 0, 0);
        native.Frame(.02f, game.Tick);
        Assert.That(game.Player.Position, Is.EqualTo(start));
        Assert.That(game.Player.LocalPose.Position, Is.EqualTo(start + new Vector3(10, 0, 0)));
        game.Player.TeleportTo(At(15));
        Assert.That(game.Player.Position, Is.EqualTo(At(15).Position));
        game.Paused = true;
        native.Frame(1, game.Tick);
        game.Paused = false;
        Assert.That(game.Player.Position, Is.EqualTo(At(15).Position));
        game.Leave();
        native.Frame(.01f, game.Tick);
        Assert.That(game.World.Latency.PingRttMs, Is.Zero);
    }

    [Test]
    public void NativeMitigationDispatchCapturesSequenceAndDelaysEffectOnlyOnce()
    {
        var native = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Config), new Configuration { SimulatedPingRttMs = 150 });
        using var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        game.RunScenario(new LegacyRunScenarioParams(new FruDarklitScenario(), PartyRole.MainTank, 0, 0, 0));
        native.Frame(.01f, game.Tick);
        using var actions = new UserActions(native.PlayerInput);
        var count = 0;
        actions.Acted += _ =>
        {
            count++;
            Assert.That(game.Player!.HasStatus(82), Is.True, "Defensive status precedes resolved confirmation");
        };
        native.PlayerInput.ReportNativeExecution(ActionType.Action, 30, 0, 1);
        native.PlayerInput.ReportNativeExecution(ActionType.Action, 30, 0, 1);
        Assert.That(game.Player!.HasStatus(82), Is.False);
        native.Frame(.05f, game.Tick);
        Assert.That(game.Player.HasStatus(82), Is.False);
        native.Frame(.03f, game.Tick);
        Assert.That(game.Player.HasStatus(82), Is.True);
        Assert.That(count, Is.EqualTo(1));
    }
    [Test]
    public void SubstituteDefenseReservesUseUntilReceiptAndCannotCarryAcrossReset()
    {
        var events = new EventScheduler();
        var latency = new LocalLatency();
        latency.Begin(150, At(0));
        var action = new PracticeAction(6);
        action.Start();
        action.DelayActivation = effect => latency.Accept(events, 1, effect);
        Assert.That(action.Activate(true), Is.True);
        Assert.That(action.Ready, Is.False);
        Assert.That(action.Active, Is.False);
        Assert.That(action.Activate(true), Is.False);
        events.Tick(.08f);
        Assert.That(action.Active, Is.True);
        action.Start();
        Assert.That(action.Activate(true), Is.True);
        action.Start();
        events.Tick(.08f);
        Assert.That(action.Active, Is.False);
    }

    [Test]
    public void SpeedScalingMaintainsOneWayWallClockDelayAtFixedSpeed()
    {
        var events = new EventScheduler();
        var latency = new LocalLatency();
        latency.Begin(150, At(0));
        var active = false;
        latency.Accept(events, 2, () => active = true);
        events.Tick(.07f * 2);
        Assert.That(active, Is.False);
        events.Tick(.01f * 2);
        Assert.That(active, Is.True);
    }

    [TestCase(0, .03f, true)] [TestCase(100, .3f, true)] [TestCase(150, .3f, true)]
    [TestCase(100, .03f, false)] [TestCase(150, .03f, false)]
    public void NativeProvokeMustReachThordanBeforeHisActualCleaveSnapshot(int rtt, float lead, bool swapped)
    {
        var scenario = new DsrP5DeathScenario();
        FakeGame native = null!;
        UserActions? actions = null;
        try
        {
            ScenarioRun.Execute(typeof(DsrP5DeathScenario), 0, 0, new()
            {
                ScenarioFactory = () => scenario, PlayerRole = PartyRole.OffTank, UserActionsEnabled = true,
                Configure = c => c.SimulatedPingRttMs = rtt,
                ConfigureNative = f => { native = f; f.BattleCharas.Player.ClassJob = 19; },
                BeforeStart = (_, f) => actions = new UserActions(f.PlayerInput),
                Takeovers = [new(61.1f, new Vector2(2, 4))], StopAt = 69,
                Probe = p =>
                {
                    if (p.Crossed(62)) p.World.Party.Player!.AddStatus(1191, 20);
                    if (!p.Crossed(68 - lead)) return;
                    var boss = p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == DsrP5Constants.BNpcBaseId.DarkThordan);
                    native.PlayerInput.ReportNativeExecution(ActionType.Action, 7533, boss.GameObjectId.ObjectId, 1);
                    native.PlayerInput.ReportNativeExecution(ActionType.Action, 7533, boss.GameObjectId.ObjectId, 1);
                }, WriteArtifactsOnFailure = false,
            });
            Assert.That(scenario.TankSwap!.Snapshots[1].Target, Is.EqualTo(swapped ? PartyRole.OffTank : PartyRole.MainTank));
        }
        finally { actions?.Dispose(); }
    }

    [TestCase(0, .03f, true)] [TestCase(100, .3f, true)] [TestCase(150, .3f, true)]
    [TestCase(100, .03f, false)] [TestCase(150, .03f, false)]
    public void NativeInvulnerabilityMustActivateBeforeDarklitsActualDamageSnapshot(int rtt, float lead, bool survives)
    {
        FakeGame native = null!;
        UserActions? actions = null;
        try
        {
            var result = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
            {
                PlayerRole = PartyRole.MainTank, UserActionsEnabled = true,
                Configure = c => c.SimulatedPingRttMs = rtt,
                ConfigureNative = f => native = f,
                BeforeStart = (_, f) => actions = new UserActions(f.PlayerInput),
                Overrides = o => ((FruDarklitStateOverrides)o).BaitTank = PartyRole.MainTank,
                Probe = p =>
                {
                    if (p.Crossed(29.1f))
                        foreach (var status in new ushort[] {82, 409, 810, 1836}) p.World.Party.Player!.RemoveStatus(status);
                    if (p.Crossed(29.792f - lead)) native.PlayerInput.ReportNativeExecution(ActionType.Action, 30, 0, 1);
                }, WriteArtifactsOnFailure = false,
            });
            Assert.That(result.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId is 40284 or 40285), Is.EqualTo(!survives), result.ToString());
        }
        finally { actions?.Dispose(); }
    }

    [Test]
    public void ConfiguredHumanRunCanRestartAndSwitchToSpectatorWithoutPenalizingBots()
    {
        RunScenarioParams? current = null;
        var restarted = false;
        var switched = false;
        var inspected = false;
        ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            StopAt = 4, Configure = c => c.SimulatedPingRttMs = 150,
            BeforeStart = (g, _) => g.RunStarted += (p, _) => current = p,
            Probe = p =>
            {
                if (!restarted && p.Time > .5f)
                {
                    Assert.That(p.World.Latency.PingRttMs, Is.EqualTo(150));
                    restarted = true;
                    p.Game.Restart();
                    return;
                }
                if (restarted && !switched && p.Time > 1)
                {
                    Assert.That(p.World.Latency.PingRttMs, Is.EqualTo(150));
                    switched = true;
                    p.Game.StartScenario(current! with { Party = new LocalPartyProvider { Spectator = true }, WatchedRole = PartyRole.MainTank });
                }
                if (p.Game.WatchedRole is null) return;
                inspected = true;
                Assert.That(p.World.Latency.PingRttMs, Is.Zero);
                Assert.That(p.World.Party.Player, Is.Null);
                Assert.That(p.World.Party.AllMembers().OfType<SimPartyNpc>().Count(), Is.EqualTo(8));
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(restarted && switched && inspected, Is.True);
    }

    public class LoginState : DalamudServices.NullService
    {
        public bool LoggedIn { get; set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
            => method?.Name == "get_IsLoggedIn" ? LoggedIn : base.Invoke(method, args);
    }

    [TestCase(false)] [TestCase(true)]
    public void OfflineAndLoggedInInnUseTheSameLocalLatencyModel(bool loggedIn)
    {
        var client = DispatchProxy.Create<IClientState, LoginState>();
        ((LoginState)(object)client).LoggedIn = loggedIn;
        DalamudServices.Install(nameof(Plugin.ClientState), client);
        var inspected = false;
        ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            StopAt = .2f, Configure = c => c.SimulatedPingRttMs = 150,
            Probe = p =>
            {
                inspected = true;
                Assert.That(client.IsLoggedIn, Is.EqualTo(loggedIn));
                Assert.That(p.World.Latency.PingRttMs, Is.EqualTo(150));
                Assert.That(p.World.Party.AllMembers().OfType<SimPartyNpc>().Count(), Is.EqualTo(7));
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(inspected, Is.True);
    }

    [TestCase(0)] [TestCase(150)]
    public void RestartDiscardsInFlightNativeEffectsAndPriorExecutionSequence(int rtt)
    {
        var native = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Config), new Configuration { SimulatedPingRttMs = rtt });
        using var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        using var actions = new UserActions(native.PlayerInput);
        native.UserActions.ScenarioStarting = actions.OnScenarioStart;
        game.RunScenario(new LegacyRunScenarioParams(new FruDarklitScenario(), PartyRole.MainTank, 0, 0, 0));
        native.Frame(.01f, game.Tick);
        native.PlayerInput.ReportNativeExecution(ActionType.Action, 30, 0, 1);
        Assert.That(game.Player!.HasStatus(82), Is.EqualTo(rtt == 0));
        game.Restart();
        native.Frame(.01f, game.Tick);
        native.Frame(.1f, game.Tick);
        Assert.That(game.Player!.HasStatus(82), Is.False);
        native.PlayerInput.ReportNativeExecution(ActionType.Action, 30, 0, 1);
        native.Frame(.1f, game.Tick);
        Assert.That(game.Player.HasStatus(82), Is.False);
        native.PlayerInput.ReportNativeExecution(ActionType.Action, 30, 0, 2);
        Assert.That(game.Player.HasStatus(82), Is.EqualTo(rtt == 0));
        native.Frame(.1f, game.Tick);
        Assert.That(game.Player.HasStatus(82), Is.True);
    }

}
