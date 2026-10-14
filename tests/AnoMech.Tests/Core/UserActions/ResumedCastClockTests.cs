#pragma warning disable CA1416
using AnoMech.Core.UserActions;

namespace AnoMech.Tests;

public class ResumedCastClockTests
{
    [Test]
    public void RepeatedPausesDoNotRefreshOrDiscardAnAlreadyStartedCast()
    {
        var clock = new ResumedCastClock();
        clock.Preserve(12, 5);
        Assert.That(clock.Remaining, Is.EqualTo(7));
        clock.Preserve(12, 12);
        Assert.That(clock.Remaining, Is.EqualTo(7));
        Assert.That(clock.Tick(2), Is.False);
        clock.Preserve(12, 12);
        Assert.That(clock.Remaining, Is.EqualTo(5));
        Assert.That(clock.Tick(5), Is.True);
        clock.Clear();
        Assert.That(clock.Remaining, Is.Null);
        clock.Preserve(3, 1);
        Assert.That(clock.Tick(1.9f), Is.False);
    }
}
