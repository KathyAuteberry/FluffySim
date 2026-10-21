using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

namespace AnoMech.Tests;

public class DamageBudgetTests
{
    [Test]
    public void RefreshBetweenSnapshotAndImpactPreservesTheNewShield()
    {
        var checkedShield = false;
        var budget = new DamageBudget();
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, Takeovers = [new(0, null)], StopAt = 2,
            Probe = p =>
            {
                var player = p.World.Party.Player!;
                if (p.Crossed(.5f))
                {
                    player.AddStatus(297, 6);
                    p.World.Children.OfType<SimEnemy>().First().Cast(FruP4CrystallizeTimeActions.Wings(false) with
                    {
                        Cast = new() { CastSecondsOverride = 0 },
                        Timing = new() { DamageDelay = .5f },
                        Effects = [EnemyActionEffects.Resolve(ctx => budget.Apply(ctx, player, 30000))],
                    }, player);
                }
                if (p.Crossed(.7f)) player.AddStatus(297, 6);
                if (!p.Crossed(1.1f)) return;
                Assert.That(player.HasStatus(297), Is.True);
                Assert.That(player.FindStatus(297)!.RemainingShieldFraction, Is.Null);
                Assert.That(player.Proxy!.Health, Is.EqualTo(90000));
                checkedShield = true;
            },
        });
        Assert.That(checkedShield, Is.True);
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void DelayedDamageCannotRestoreHpAfterAnotherMechanicKillsTheTarget()
    {
        var checkedHealth = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, Takeovers = [new(0, null)], StopAt = 2,
            Probe = p =>
            {
                var player = p.World.Party.Player!;
                if (p.Crossed(.5f))
                    p.World.Children.OfType<SimEnemy>().First().Cast(FruP4CrystallizeTimeActions.Wings(false) with
                    {
                        Cast = new() { CastSecondsOverride = 0 },
                        Timing = new() { DamageDelay = .5f },
                        Effects = [EnemyActionEffects.Resolve(ctx => new DamageBudget().Apply(ctx, player, 15000))],
                    }, player);
                if (p.Crossed(.7f)) player.Die(40332, "another hit before impact");
                if (!p.Crossed(1.1f)) return;
                Assert.That(player.Dead, Is.True);
                Assert.That(player.Proxy!.Health, Is.Zero);
                checkedHealth = true;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(checkedHealth, Is.True, run.ToString);
        Assert.That(run.Deaths, Has.Count.EqualTo(1));
    }

    [Test]
    public void GodModePreservesHumanAndBotHpThroughLethalNumericDamage()
    {
        var checkedHealth = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, GodMode = true, Takeovers = [new(0, null)], StopAt = 2,
            Probe = p =>
            {
                var player = p.World.Party.Player!;
                var bot = p.Member(PartyRole.MainTank)!;
                if (p.Crossed(.5f))
                    p.World.Children.OfType<SimEnemy>().First().Cast(FruP4CrystallizeTimeActions.Wings(false) with
                    {
                        Cast = new() { CastSecondsOverride = 0 },
                        Timing = new() { DamageDelay = .5f },
                        Effects = [EnemyActionEffects.Resolve(ctx =>
                        {
                            var budget = new DamageBudget();
                            budget.Apply(ctx, player, 300000);
                            budget.Apply(ctx, bot, 300000);
                        })],
                    }, player);
                if (!p.Crossed(1.1f)) return;
                Assert.That(player.Dead || bot.Dead, Is.False);
                Assert.That(player.Proxy!.Health, Is.EqualTo(player.Proxy.MaxHealth));
                Assert.That(bot.Proxy!.Health, Is.EqualTo(bot.Proxy.MaxHealth));
                checkedHealth = true;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(checkedHealth, Is.True, run.ToString);
        Assert.That(run.Deaths, Is.Empty);
    }

    [TestCase(false)] [TestCase(true)]
    public void ShieldIsPartiallySpentAndOnlyRefreshedByANewApplication(bool refresh)
    {
        var checkedDamage = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, Takeovers = [new(0, null)], StopAt = 1,
            Probe = p =>
            {
                if (!p.Crossed(.5f)) return;
                var player = p.World.Party.Player!;
                player.AddStatus(297, 6);
                var budget = new DamageBudget();
                DamageBudget.Result Hit(float damage)
                {
                    var ctx = new EnemyActionContext(FruP4CrystallizeTimeActions.Wings(false), player, player,
                        player.Placement(), p.World.Party, 0);
                    return budget.Apply(ctx, player, damage);
                }
                Assert.That(Hit(15000).HpDamage, Is.Zero);
                if (refresh) player.AddStatus(297, 6);
                var second = Hit(15000);
                Assert.That(second.Damage, Is.EqualTo(15000));
                Assert.That(second.HpDamage, Is.EqualTo(refresh ? 0 : 10000));
                Assert.That(second.HpAfter, Is.EqualTo(refresh ? 100000 : 90000));
                checkedDamage = true;
            },
        });
        Assert.That(checkedDamage, Is.True);
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(false)] [TestCase(true)]
    public void ReductionAndShieldExpirationDoNotRefillMissingHp(bool expired)
    {
        var checkedDamage = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, Takeovers = [new(0, null)], StopAt = 1,
            ConfigureNative = f => f.BattleCharas.Player.Health = 80000,
            Probe = p =>
            {
                if (!p.Crossed(.5f)) return;
                var player = p.World.Party.Player!;
                player.AddStatus(297, 1)!.Tick(expired ? 2 : 0);
                player.AddStatus(3985, 1)!.Tick(expired ? 2 : 0);
                var ctx = new EnemyActionContext(FruP4CrystallizeTimeActions.Wings(false), player, player,
                    player.Placement(), p.World.Party, 0);
                var result = new DamageBudget().Apply(ctx, player, 100000);
                Assert.That(result.HpBefore, Is.EqualTo(80000));
                Assert.That(result.Damage, Is.EqualTo(expired ? 100000 : 90000));
                Assert.That(result.HpAfter, Is.EqualTo(expired ? 0 : 10000));
                Assert.That(ctx.IsKilled(player), Is.EqualTo(expired));
                checkedDamage = true;
            },
        });
        Assert.That(checkedDamage, Is.True);
        Assert.That(run.Passed, run.ToString);
    }
}
