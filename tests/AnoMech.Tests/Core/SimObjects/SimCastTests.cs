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
}
