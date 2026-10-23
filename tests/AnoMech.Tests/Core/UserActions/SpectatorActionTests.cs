#pragma warning disable CA1416
using System.Reflection;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.UserActions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Tests;

public class SpectatorActionTests
{
    [Test]
    public void EnteringSpectatorModeInterruptsPendingHumanCastExactlyOnce()
    {
        var fake = FakeGame.Install();
        using var actions = new UserActions(fake.PlayerInput);
        var steps = new List<PlayerAction>();
        actions.Acted += steps.Add;
        void Set(string name, object value) => typeof(UserActions).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actions, value);
        Set("pendingResolve", true);
        Set("pendingType", ActionType.Action);
        Set("pendingAction", 119u);
        actions.OnSpectatorStart();
        actions.OnSpectatorStart();
        Assert.That(steps, Has.Count.EqualTo(1));
        Assert.That(steps[0].Step, Is.EqualTo(PlayerActionStep.Interrupted));
        Assert.That(typeof(UserActions).GetField("pendingResolve", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actions), Is.False);
    }
}
