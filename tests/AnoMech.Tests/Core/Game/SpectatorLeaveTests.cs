#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P3Nidhogg;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Tests;

public class SpectatorLeaveTests
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void LeaveStopsActivityBeforeRestoration(bool paused, bool forced)
    {
        FakeGame? native = null;
        var requested = false;
        var stoppedBeforeCleanup = false;
        var leaked = false;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 7391, new()
        {
            Spectator = true, PlayerRole = PartyRole.CasterDps,
            ConfigureNative = f => native = f,
            StopWhen = p => requested && p.Game.Status == SessionStatus.Offline,
            Probe = p =>
            {
                if (requested || p.Time < 1) return;
                requested = true;
                var actors = p.World.Party.AllMembers().ToArray();
                foreach (var actor in actors)
                {
                    if (forced) actor.IceSlideTo(actor.Position + new Vector3(10, 0, 0), 10);
                    else actor.MoveTo(actor.Position + new Vector3(10, 0, 0));
                    actor.Casting.NativeCast(7533, ActionType.Action, 0, 10, false);
                }
                p.World.Events.Add(1, () => leaked = true);
                native!.PlayerInput.Steer(Vector2.UnitX, 6);
                p.World.OnDespawn(() =>
                {
                    var positions = actors.Select(a => a.Proxy!.Position).ToArray();
                    foreach (var actor in actors) actor.Tick(1);
                    stoppedBeforeCleanup = p.World.Events.IsEmpty
                        && actors.All(a => !a.Casting.IsBusy && !a.Casting.IsCasting)
                        && actors.Select(a => a.Proxy!.Position).SequenceEqual(positions)
                        && native.PlayerInput.SteerDirection is null;
                });
                p.Game.Paused = paused;
                p.Game.Leave();
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(requested, Is.True);
        Assert.That(stoppedBeforeCleanup, Is.True);
        Assert.That(leaked, Is.False);
        Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(native.SpectatorCamera.Target, Is.Null);
        Assert.That(native.PlayerInput.SpectatorInput, Is.False);
    }
}
