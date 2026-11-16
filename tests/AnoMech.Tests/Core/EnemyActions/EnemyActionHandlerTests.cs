using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

namespace AnoMech.Tests;

public class EnemyActionHandlerTests
{
    [Test]
    public void ZeroCastOverrideSendsTheActionEffectAtOnce()
    {
        var sentAtOnce = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, Takeovers = [new(0, null)], StopAt = 1,
            Probe = p =>
            {
                if (!p.Crossed(.5f)) return;
                var player = p.World.Party.Player!;
                var caster = p.World.Children.OfType<SimEnemy>().First();
                var proxy = (FakeBattleChara)caster.Proxy!;
                var before = proxy.ReceivedEffects.Count;
                caster.Cast(FruP4CrystallizeTimeActions.Wings(false) with
                {
                    Cast = new() { CastSecondsOverride = 0 },
                    Effects = [],
                }, player);
                Assert.That(proxy.ReceivedEffects.Count, Is.EqualTo(before + 1));
                sentAtOnce = true;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(sentAtOnce, Is.True, run.ToString);
    }
}
