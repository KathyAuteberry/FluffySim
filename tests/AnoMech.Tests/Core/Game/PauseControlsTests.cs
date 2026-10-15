#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.FallOfFaith;
using AnoMech.Windows;

namespace AnoMech.Tests;

public class PauseControlsTests
{
    private static (FakeGame Native, Game Game) Start()
    {
        var native = FakeGame.Install();
        DalamudServices.Install(nameof(Plugin.Config), new Configuration());
        var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        game.RunScenario(new LegacyRunScenarioParams(new FruFallOfFaithScenario(), PartyRole.MainTank, 0, 0, 0));
        native.Frame(.01f, game.Tick);
        return (native, game);
    }

    [Test]
    public void ResumeFailureStillReleasesPauseInputAndVirtualClockRestrictions()
    {
        var (native, game) = Start();
        using (game)
        {
            game.Paused = true;
            var actions = (FakeUserActions)Natives.UserActions;
            actions.ThrowOnResume = true;
            try
            {
                Assert.Throws<InvalidOperationException>(() => game.Paused = false);
                Assert.That(Natives.PlayerInput.PauseInput, Is.False);
                Assert.That(game.Paused, Is.False);
                Assert.That(game.World.PracticeInvulnerability.Suspended, Is.False);
                Assert.That(game.Events.Suspended, Is.False);
            }
            finally { actions.ThrowOnResume = false; }
        }
    }

    [Test]
    public void ButtonAvailabilityTogglesTheActiveRunAndRejectsIdleOrMultiplayer()
    {
        var (native, game) = Start();
        using (game)
        {
            Assert.That(ScenarioPauseControls.Evaluate(game, false).Label, Is.EqualTo("Pause"));
            Assert.That(ScenarioPauseControls.Press(game, true), Is.False);
            Assert.That(game.Paused, Is.False);
            Assert.That(ScenarioPauseControls.Press(game, false), Is.True);
            Assert.That(Natives.PlayerInput.PauseInput, Is.True);
            Assert.That(ScenarioPauseControls.Evaluate(game, false).Label, Is.EqualTo("Resume"));
            Assert.That(ScenarioPauseControls.Press(game, false), Is.True);
            Assert.That(Natives.PlayerInput.PauseInput, Is.False);
            Assert.That(game.EventClockNow - game.Events.Elapsed, Is.LessThan(.1f));
            game.Reset(); native.Frame(0, game.Tick);
            Assert.That(ScenarioPauseControls.Evaluate(game, false).Visible, Is.False);
            Assert.That(ScenarioPauseControls.Press(game, false), Is.False);
        }
    }

    [TestCase(false)] [TestCase(true)]
    public void ResumePreservesPreexistingDeathAndFettersInsteadOfRemovingTheirRestrictions(bool dead)
    {
        var (native, game) = Start();
        using (game)
        {
            var player = game.Player!;
            if (dead) player.OnKilled(false);
            else { player.AddStatusParam(2304, 0xC8, 4); player.Tick(0); }
            player.Tick(0);
            var before = player.ActiveStatuses.Select(s => s.Snapshot).ToArray();
            var input = (FakeLocalPlayerInput)Natives.PlayerInput;
            var movement = native.PlayerConditions.StatusAffliction; var actions = native.PlayerConditions.StatusAffliction;
            Assert.That(movement, Is.True);
            game.Paused = true;
            Assert.That(game.PauseStatuses.Any(s => s.Character == player), Is.True);
            native.Frame(60, game.Tick);
            game.Paused = false;
            Assert.That(input.PauseInput, Is.False);
            Assert.That((native.PlayerConditions.StatusAffliction, native.PlayerConditions.StatusAffliction), Is.EqualTo((movement, actions)));
            Assert.That(player.Dead, Is.EqualTo(dead));
            Assert.That(player.ActiveStatuses.Select(s => s.Snapshot), Is.EqualTo(before));
        }
    }

    [TestCase("Stop")] [TestCase("Leave")] [TestCase("Restart")] [TestCase("Dispose")]
    public void TeardownReleasesOnlyTheOwnedPauseGate(string operation)
    {
        var (native, game) = Start();
        var input = (FakeLocalPlayerInput)Natives.PlayerInput;
        try
        {
            game.Paused = true;
            switch (operation)
            {
                case "Stop": game.Reset(); native.Frame(0, game.Tick); break;
                case "Leave": game.Leave(); native.Frame(0, game.Tick); break;
                case "Restart": game.RunScenario(new LegacyRunScenarioParams(new FruFallOfFaithScenario(), PartyRole.MainTank, 0, 0, 0)); native.Frame(0, game.Tick); break;
                case "Dispose": game.Dispose(); break;
            }
            Assert.That(game.Paused || input.PauseInput, Is.False);
            Assert.That(game.PauseStatuses, Is.Empty);
            Assert.That(game.World.PracticeInvulnerability.Suspended || game.World.PracticeAntiKnockback.Suspended, Is.False);
        }
        finally { game.Dispose(); }
    }

    [Test]
    public void PausedRuntimeExceptionReleasesInputWithoutCleansingStatuses()
    {
        var (native, game) = Start();
        using (game)
        {
            var status = game.Player!.AddStatusParam(2941, 3, 12);
            var snapshot = status.Snapshot;
            game.Paused = true;
            native.PlayerConditions.ThrowOnNextSet=true;
            Assert.Throws<InvalidOperationException>(() => game.Tick(60));
            Assert.That(game.Paused || Natives.PlayerInput.PauseInput, Is.False);
            Assert.That(status.Snapshot, Is.EqualTo(snapshot));
        }
    }

    [Test]
    public void ScheduledCallbackExceptionReleasesItsPauseAndRemainingEventsRunOnce()
    {
        var (native, game) = Start();
        using (game)
        {
            var count = 0;
            game.Events.Add(0, () => { game.Paused = true; throw new InvalidOperationException("Scheduled pause test"); });
            game.Events.Add(0, () => count++);
            game.Tick(0);
            Assert.That(game.Paused || Natives.PlayerInput.PauseInput || game.Events.Suspended, Is.False);
            Assert.That(count, Is.EqualTo(1));
            game.Tick(0); Assert.That(count, Is.EqualTo(1));
        }
    }

    [Test]
    public void AllPartyAndEnemyStatusesPreserveStacksDurationsOwnersAndInstancesAcrossLongPauses()
    {
        var (native, game) = Start();
        using (game)
        {
            var enemy = game.World.Children.OfType<SimEnemy>().First();
            var members = game.World.Party.AllMembers().Concat(game.World.Children.OfType<SimCharacter>()).Distinct().ToArray();
            foreach (var member in members)
            {
                member.AddStatus(2941, 12, 3, sourceObject: enemy.GameObjectId);
                member.AddStatus(2941, 20, 2, sourceObject: game.Player!.GameObjectId);
                member.AddStatusParam(2304, 0xC8, 4);
                member.AddStatus(SimOnlyStatus.First + 400, 1, sourceObject: enemy.GameObjectId);
                member.AddStatus(SimOnlyStatus.First + 400, 1, sourceObject: game.Player!.GameObjectId);
                member.Tick(.5f);
            }
            var before = members.ToDictionary(m => m, m => m.ActiveStatuses.Select(s => s.Snapshot).ToArray());
            for (var cycle = 0; cycle < 5; cycle++)
            {
                game.Paused = true;
                Assert.That(game.PauseStatuses.Count, Is.EqualTo(members.Length));
                foreach (var captured in game.PauseStatuses)
                    Assert.That(captured.Statuses, Is.EqualTo(before[captured.Character]));
                for (var i = 0; i < 4; i++) native.Frame(60, game.Tick);
                game.Paused = false;
                foreach (var member in members)
                    Assert.That(member.ActiveStatuses.Select(s => s.Snapshot), Is.EqualTo(before[member]));
            }
            var pending = 0;
            game.Events.Add(1, () => pending++);
            game.Tick(.5f);
            foreach (var member in members)
            {
                Assert.That(member.FindStatus(SimOnlyStatus.First + 400, enemy.GameObjectId), Is.Null);
                Assert.That(member.FindStatus(SimOnlyStatus.First + 400, game.Player!.GameObjectId), Is.Null);
                Assert.That(member.FindStatus(2941, enemy.GameObjectId)!.RemainingTime, Is.EqualTo(11));
                Assert.That(member.FindStatus(2941, game.Player!.GameObjectId)!.Stacks, Is.EqualTo(2));
            }
            game.Paused = true; native.Frame(100, game.Tick); game.Paused = false;
            Assert.That(pending, Is.Zero);
            game.Tick(.5f); Assert.That(pending, Is.EqualTo(1));
            game.Tick(.1f); Assert.That(pending, Is.EqualTo(1));
        }
    }

    [Test]
    public void NativeCastCompletionDoesNotReleaseTheSimulatedBossMovementLockDuringPause()
    {
        var (native, game) = Start();
        using (game)
        {
            for (var i = 0; i < 240; i++) native.Frame(1f / 60f, game.Tick);
            var boss = game.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == 0x459B);
            Assert.That(boss.AnimationLock, Is.True);
            game.Paused = true; native.Frame(60, game.Tick);
            Assert.That(boss.Proxy!.IsCasting, Is.False);
            Assert.That(boss.AnimationLock, Is.True);
            game.Paused = false; game.Tick(.1f);
            Assert.That(boss.AnimationLock, Is.True);
        }
    }

    [Test]
    public void NativeCarryDriftIsRestoredWithoutDiscardingThePendingSimulatedPush()
    {
        var (native, game) = Start();
        using (game)
        {
            var player = game.Player!;
            var start = player.Position;
            var global = player.Proxy!.Position;
            player.Push(0, 3, .5f);
            game.Paused = true;
            native.BattleCharas.Player.StartCarry(global + new Vector3(10, 0, 0), 1);
            native.Frame(60, game.Tick);
            Assert.That(player.Position, Is.EqualTo(start));
            Assert.That(player.Proxy.Position, Is.Not.EqualTo(global));
            game.Paused = false;
            Assert.That(player.Proxy.Position, Is.EqualTo(global));
            game.Tick(.1f);
            Assert.That(player.IsMoving, Is.True);
            Assert.That(player.Position, Is.EqualTo(start));
        }
    }

    [TestCase(false)] [TestCase(true)]
    public void LostSimulationScopeReleasesPauseWithoutRestoringAnArenaPoseIntoTheInn(bool resumeFirst)
    {
        var (native, game) = Start();
        using (game)
        {
            var time = game.Events.Elapsed;
            game.Paused = true;
            native.Zone.Revert();
            var inn = native.BattleCharas.Player.Position;
            if (resumeFirst) game.Paused = false;
            game.Tick(60);
            Assert.That(game.Paused || Natives.PlayerInput.PauseInput, Is.False);
            Assert.That(game.CanPause, Is.False);
            Assert.That(game.Events.IsEmpty, Is.True);
            Assert.That(game.Events.Elapsed, Is.Zero);
            Assert.That(native.BattleCharas.Player.Position, Is.EqualTo(inn));
        }
    }

    [Test]
    public void WaitingThroughVulnerabilityDoesNotPreventTheNextActualConeFromKillingThePlayer()
    {
        FakeGame? native = null;
        var observed = false;
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MeleeDpsA, StopAt = 19.5f,
            ConfigureNative = f => native = f,
            Overrides = o => { var state = (FruFallOfFaithStateOverrides)o; state.Advanced = true; state.TetherPermutation = 0; state.FireMask = 15; },
            Takeovers = [new(17.5f, new Vector2(7.5f, .4f))],
            Probe = p =>
            {
                if (!p.Crossed(17.6f)) return;
                var vulnerability = p.Game.Player!.FindStatus(2941)!;
                var before = vulnerability.Snapshot;
                p.Game.Paused = true;
                native!.Frame(60, p.Game.Tick);
                p.Game.Paused = false;
                Assert.That(vulnerability.Snapshot, Is.EqualTo(before));
                observed = true;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(observed, Is.True);
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MeleeDpsA && d.ActionId == 40156), Is.True, run.ToString);
    }

    private sealed class ThrowingBgm : IBgm
    {
        public void Dispose() { }
        public void Play(ushort bgmId, float secondsIn = 0) { }
        public void Sync(float secondsIn) { }
        public void Reset() { }
        public void Tick(float deltaSeconds) => throw new InvalidOperationException("Pause recovery test");
        public void LogPosition(string label) { }
    }
}
