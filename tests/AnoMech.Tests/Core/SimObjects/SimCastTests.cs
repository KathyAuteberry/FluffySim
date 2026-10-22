using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Tests;

public class SimCastTests
{
    [Test]
    public void PendingHelperReceivesOnlyRemainingCastAndOmenTime()
    {
        var fake = FakeGame.Install();
        fake.BattleCharas.PacketSpawnDelayFrames = 3;
        using var world = new SimWorld(new EventScheduler());
        var helper = world.SpawnEnemy(new EnemySpawnConfig(15724))!;
        helper.Casting.NativeCast(40235, ActionType.Action, .8f, 2f, false);
        for (var i = 0; i < 5; i++) fake.Frame(.1f, helper.Tick);
        var actor = fake.BattleCharas.Find(helper.EntityId)!;
        Assert.That(actor.ActorCasts, Has.Count.EqualTo(1));
        var cast = actor.ActorCasts.Single();
        Assert.That(cast.CastTime, Is.EqualTo(1.5f).Within(.001f));
        Assert.That(cast.OmenDelay, Is.EqualTo(.3f).Within(.001f));
        Assert.That(helper.Casting.CastSeq, Is.EqualTo(1));
    }

    [TestCase("cancel")]
    [TestCase("effect")]
    [TestCase("expire")]
    [TestCase("despawn")]
    public void EndedPendingCastCannotAppearAfterItsHelperArrives(string ending)
    {
        var fake = FakeGame.Install();
        fake.BattleCharas.PacketSpawnDelayFrames = 3;
        using var world = new SimWorld(new EventScheduler());
        var helper = world.SpawnEnemy(new EnemySpawnConfig(15724))!;
        helper.Casting.NativeCast(40235, ActionType.Action, 0f, ending == "expire" ? .2f : 2f, false);
        if (ending == "cancel") helper.Casting.Cancel(CastCancelReason.Silent);
        if (ending == "effect") helper.Casting.NativeActionEffect(40235, 0f, 40235, 0, ActionType.Action, 0);
        if (ending == "despawn") helper.Despawn();
        for (var i = 0; i < 6; i++) fake.Frame(.1f, helper.Tick);
        Assert.That(fake.BattleCharas.Player.ActorCasts, Is.Empty);
        Assert.That(fake.Timeline.Rows.Where(e => e["kind"] == "cast"), Is.Empty);
    }
    [TestCase(0)] [TestCase(3)] [TestCase(18)]
    public void GeirskogulRetainsLockedDirectionAndRemainingOmenDelayAcrossRegistrationAndPause(int spawnFrames)
    {
        var fake = FakeGame.Install();
        fake.BattleCharas.PacketSpawnDelayFrames = spawnFrames;
        using var world = new SimWorld(new EventScheduler());
        var clone = world.SpawnEnemy(new EnemySpawnConfig(12606))!;
        clone.Casting.NativeCast(26378, ActionType.Action, 3.5f, 4.2f, false, rotation: 1.2f);
        for (var i = 0; i < 10; i++) fake.Frame(.1f, clone.Tick);
        clone.Casting.PausePresentation(true);
        for (var i = 0; i < 10; i++) fake.Frame(.1f, clone.Tick);
        clone.Casting.PausePresentation(false);
        for (var i = 0; i < 10; i++) fake.Frame(.1f, clone.Tick);
        var casts = ((FakeBattleChara)clone.Proxy!).ReceivedCasts;
        Assert.That(casts, Is.Not.Empty);
        var last = casts.Last();
        Assert.That(last.Rotation, Is.EqualTo(1.2f));
        Assert.That(last.CastTime - last.OmenDelay, Is.EqualTo(.7f).Within(.001f));
        Assert.That(last.OmenDelay, Is.GreaterThan(1.4f));
        Assert.That(clone.Casting.CastSeq, Is.EqualTo(1));
        var count = casts.Count;
        clone.Casting.Despawn();
        for (var i = 0; i < 5; i++) fake.Frame(.1f, clone.Tick);
        Assert.That(casts.Count, Is.EqualTo(count));
    }

    [Test]
    public void ActorArrivingAfterOmenDeadlineStillReceivesTheRemainingWarning()
    {
        var fake = FakeGame.Install();
        fake.BattleCharas.PacketSpawnDelayFrames = 16;
        using var world = new SimWorld(new EventScheduler());
        var clone = world.SpawnEnemy(new EnemySpawnConfig(12606))!;
        clone.Casting.NativeCast(26378, ActionType.Action, 3.5f, 4.2f, false, rotation: 1.2f);
        for (var i = 0; i < 19; i++) fake.Frame(.2f, clone.Tick);
        var cast = ((FakeBattleChara)clone.Proxy!).ReceivedCasts.Single();
        Assert.That(cast.OmenDelay, Is.Zero);
        Assert.That(cast.CastTime, Is.InRange(.1f, .7f));
        Assert.That(cast.Rotation, Is.EqualTo(1.2f));
    }

}
