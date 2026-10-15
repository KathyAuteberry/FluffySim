#pragma warning disable CA1416
using AnoMech.Core.Native.Policies;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

public class PacketFilterPolicyTests
{
    [Test]
    public void EveryUnknownOpcodeIsDroppedWithoutCallingTransportOrRetainingAPacket()
    {
        var policy = new PacketFilterPolicy(1234);
        var sent = new List<ushort>();
        for (var opcode = 0; opcode <= ushort.MaxValue; opcode++)
            if (policy.Allow((ushort)opcode)) sent.Add((ushort)opcode);
        Assert.That(sent, Is.EqualTo(new ushort[] {1234}));
        Assert.That(policy.Allowed, Is.EqualTo(1));
        Assert.That(policy.Blocked, Is.EqualTo(65535));
        Assert.That(policy.Intercepted, Is.EqualTo(65536));
        Assert.That(policy.Allow(null), Is.False);
        Assert.That(policy.Invalid, Is.EqualTo(1));
        // The only retained state is counts, so there is no deferred transport queue to flush.
        Assert.That(sent, Has.Count.EqualTo(1));
    }

    [Test]
    public void MissingHeartbeatAndUnreadablePacketsFailClosed()
    {
        var policy = new PacketFilterPolicy(0);
        Assert.That(policy.Allow(0), Is.False);
        Assert.That(policy.Allow(null), Is.False);
        Assert.That(policy.Allowed, Is.Zero);
        Assert.That(policy.Blocked, Is.EqualTo(2));
    }

    [Test]
    public void ConcurrentCallsKeepIndependentNonPayloadCounters()
    {
        var policy = new PacketFilterPolicy(7);
        Parallel.For(0, 10000, i => policy.Allow((ushort)(i % 2 == 0 ? 7 : 9)));
        Assert.That(policy.Intercepted, Is.EqualTo(10000));
        Assert.That(policy.Allowed, Is.EqualTo(5000));
        Assert.That(policy.Blocked, Is.EqualTo(5000));
    }

    [TestCase(false,0,false,false)] [TestCase(false,10,false,false)]
    [TestCase(true,.5,false,false)] [TestCase(true,1,false,true)]
    [TestCase(true,5,true,false)] [TestCase(true,5,false,true)]
    public void FilterCannotBeLiftedDuringReloadSettleOrPendingActions(bool nativeInn,double seconds,bool queued,bool expected)
    {
        var enabled = true;
        if (FirewallLiftPolicy.BlockedReason(nativeInn,seconds,queued) is null) enabled = false;
        Assert.That(!enabled, Is.EqualTo(expected));
        var filter = new PacketFilterPolicy(7);
        var transportCalls = 0;
        if (!enabled || filter.Allow(123)) transportCalls++;
        Assert.That(transportCalls, Is.EqualTo(expected ? 1 : 0));
    }

    [Test]
    public void TransitionLockIsIndependentOfPauseAndNeverCallsAutoCancel()
    {
        var input = new LocalInputLock { DisableAllActions = true, PauseInput = true, TransitionInput = true };
        input.PauseInput = false;
        Assert.That(input.MovementBlocked, Is.True);
        Assert.That(input.RejectAction(true), Is.True);
        Assert.That(input.CancelAutoAttacks, Is.False);
        input.TransitionInput = false;
        Assert.That(input.RejectAction(false), Is.True);
        Assert.That(input.CancelAutoAttacks, Is.True);
    }
}
