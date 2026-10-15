#pragma warning disable CA1416
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

public class LocalInputLockTests
{
    [TestCase(false, false)] [TestCase(false, true)]
    [TestCase(true, false)] [TestCase(true, true)]
    public void PauseRejectsAllActionsIncludingAutoCancelAndPreservesMechanicLocks(bool movement, bool actions)
    {
        var gate = new LocalInputLock { ZeroMovement = movement, DisableAllActions = actions, PauseInput = true };
        Assert.That(gate.MovementBlocked, Is.True);
        Assert.That(gate.RejectAction(false), Is.True);
        Assert.That(gate.RejectAction(true), Is.True);
        Assert.That(gate.CancelAutoAttacks, Is.False);
        var left = 1f; var forward = -1f; var turn = 2f; byte strafe = 1;
        gate.SuppressMovement(ref left, ref forward, ref turn, ref strafe);
        Assert.That((left, forward, turn, strafe), Is.EqualTo((0f, 0f, 0f, (byte)0)));
        gate.PauseInput = false;
        Assert.That(gate.MovementBlocked, Is.EqualTo(movement));
        Assert.That(gate.RejectAction(false), Is.EqualTo(actions));
        Assert.That(gate.CancelAutoAttacks, Is.EqualTo(actions));
    }

    [Test]
    public void OrdinaryMovementIsUnaffectedOutsidePauseAndMechanicLocks()
    {
        var gate = new LocalInputLock();
        var left = 1f; var forward = -1f; var turn = 2f; byte strafe = 1;
        gate.SuppressMovement(ref left, ref forward, ref turn, ref strafe);
        Assert.That((left, forward, turn, strafe), Is.EqualTo((1f, -1f, 2f, (byte)1)));
        Assert.That(gate.RejectAction(false), Is.False);
    }
}
