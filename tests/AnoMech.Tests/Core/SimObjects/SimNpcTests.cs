#pragma warning disable CA1416
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Tests;

public class SimNpcTests
{
    [TestCase(false)] [TestCase(true)]
    public void DelayedPacketSpawnAppliesTheLatestRequestedModelVisibility(bool hidden)
    {
        var fake = FakeGame.Install();
        fake.BattleCharas.PacketSpawnDelayFrames = 3;
        using var world = new SimWorld(new EventScheduler());
        var actor = world.SpawnEnemy(new EnemySpawnConfig(12606))!;
        actor.SetModelHidden(!hidden);
        actor.SetModelHidden(hidden);
        actor.SetModelScale(1.525f);
        for (var i = 0; i < 5; i++) fake.Frame(.1f, actor.Tick);
        Assert.That(((FakeBattleChara)actor.Proxy!).ModelHidden, Is.EqualTo(hidden));
        Assert.That(((FakeBattleChara)actor.Proxy!).ModelScale, Is.EqualTo(1.525f));
        actor.SetModelHidden(!hidden);
        Assert.That(((FakeBattleChara)actor.Proxy!).ModelHidden, Is.EqualTo(!hidden));
    }
}
