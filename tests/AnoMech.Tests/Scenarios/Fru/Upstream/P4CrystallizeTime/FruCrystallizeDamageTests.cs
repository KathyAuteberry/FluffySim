using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

namespace AnoMech.Tests;

public class FruCrystallizeDamageTests
{
    private static IEnumerable<TestCaseData> Replays() =>
        from north in new[] { false, true }
        from frontAgain in new[] { false, true }
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
        select new TestCaseData(north, frontAgain, difficulty);

    [TestCaseSource(nameof(Replays))]
    public void D3TwoWingDamageUsesActualOrderAndEffectiveHp(bool north, bool frontAgain, BotDifficulty difficulty)
        => Replay(north, frontAgain, difficulty, inadequate: false);

    [TestCase(false)] [TestCase(true)]
    public void DoubleFrontWithoutSupportDiesFromDamage(bool north)
        => Replay(north, true, BotDifficulty.Hard, inadequate: true);

    [Test]
    public void NormalPracticeSuppliesOnlyTheMissingTankCooldown()
    {
        var checkedDamage = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.MainTank, UserActionsEnabled = true, Takeovers = [new(0, null)], StopAt = 1,
            ConfigureNative = f => f.BattleCharas.Player.Health = f.BattleCharas.Player.MaxHealth = 225000,
            Probe = p =>
            {
                if (!p.Crossed(.5f)) return;
                var tank = p.World.Party.Player!;
                uint Hit(bool strict)
                {
                    var action = FruP4CrystallizeTimeActions.Wings(strict);
                    var context = new EnemyActionContext(action, tank, tank, new(tank.Position, 0), p.World.Party, 0);
                    action.Effects[0].Apply(context, [tank]);
                    return context.Outcomes.Single().Damage!.Value.Amount;
                }
                var supplied = Hit(false);
                tank.AddStatus(3829, 6);
                Assert.That(Hit(false), Is.EqualTo(supplied));
                Assert.That(Hit(true), Is.EqualTo(supplied));
                checkedDamage = true;
            },
        });
        Assert.That(checkedDamage, Is.True);
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void TankPartyShieldHasThirtyYalmCoverage()
    {
        var away = Vector3.Zero;
        var checkedRange = false;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, StopAt = 49, Takeovers = [new(48.52f, new(-14, 0))],
            Overrides = o => ((FruP4CrystallizeTimeStateOverrides)o).Corner = CrystallizeCorner.NW,
            Probe = p =>
            {
                var tank = p.Member(PartyRole.MainTank)!;
                if (p.Crossed(48.55f)) { away = tank.Position; tank.SetPosition(new Vector3(18, 0, 0)); }
                if (!p.Crossed(48.7f)) return;
                Assert.That(p.World.Party.Player!.HasStatus(1457), Is.False);
                Assert.That(p.World.Party.Player.HasStatus(1362), Is.True);
                tank.SetPosition(away);
                checkedRange = true;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(checkedRange, Is.True);
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(54f)] [TestCase(60.5f)]
    public void RepeatedEntryResetsWingBudgetAndNativeHealth(float stop)
    {
        var scenario = new FruP4CrystallizeTimeScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame native = null!;
            var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
            {
                PlayerRole = PartyRole.PhysRangedDps, ScenarioFactory = () => scenario, StopAt = stop,
                ConfigureNative = f => native = f, Takeovers = [new(50, null)],
                Probe = p => { if (p.Crossed(50.6f)) p.World.Party.Player!.AddStatus(1209, 6); },
                WriteArtifactsOnFailure = false,
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(native.BattleCharas.Player.Health, Is.EqualTo(native.BattleCharas.Player.MaxHealth));
            Assert.That(native.BattleCharas.OccupiedSlots, Is.Zero);
            Assert.That(native.EventObjects.OccupiedSlots, Is.Zero);
        }
    }

    private static void Replay(bool north, bool frontAgain, BotDifficulty difficulty, bool inadequate)
    {
        var ranks = new List<int>();
        var damages = new List<uint>();
        var before = 0u;
        var after = 0u;
        var saved = frontAgain ? new Vector2(-.03f, north ? -1.9f : 1.9f) : (Vector2?)null;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, UserActionsEnabled = true, BotDifficulty = difficulty, StopAt = 61,
            ConfigureNative = f => f.BattleCharas.Player.Health = f.BattleCharas.Player.MaxHealth = 157_000,
            BeforeStart = (_, f) => f.BattleCharas.Player.ClassJob = 31,
            Takeovers = saved is { } spot ? [new(44.6f, spot)] : [new(50, null)],
            Overrides = o => ((FruP4CrystallizeTimeStateOverrides)o).Corner = north ? CrystallizeCorner.NW : CrystallizeCorner.SW,
            Probe = p =>
            {
                var player = p.World.Party.Player!;
                if (frontAgain && p.Crossed(45.5f)) player.SetPosition(new Vector3(-10, 0, 0));
                if (p.Crossed(50.6f)) player.AddStatus(1209, 6);
                if (p.Crossed(53.8f))
                    foreach (ushort id in new ushort[] { 3985, 2711, 1951, 1457, 1362, 297 })
                        Assert.That(player.HasStatus(id), Is.True, $"planned support {id} missing");
                if (inadequate && p.Crossed(58.4f))
                    foreach (ushort id in new ushort[] { 3985, 2711, 1934, 1951, 1826, 297, 1457, 1362 }) player.RemoveStatus(id);
                if (p.Crossed(58.4f)) before = player.Proxy!.Health;
                if (p.Crossed(54.3f) || p.Crossed(58.9f))
                {
                    var effect = p.World.Children.OfType<SimEnemy>().SelectMany(e => ((FakeBattleChara)e.Proxy!).ReceivedEffects)
                        .Last(e => e.ActionId == 40332);
                    var hits = effect.Targets!.Where(t => t.Effects!.Any(e => e.Type == 3)).ToArray();
                    var rank = Array.FindIndex(hits, t => t.Id == player.GameObjectId) + 1;
                    ranks.Add(rank);
                    if (ranks.Count == 2)
                        Assert.That(hits.Take(2).Select(t => ((ISimPartyMember)p.World.Party.ActiveMembers().Single(m => m.GameObjectId == t.Id)).Role),
                            Is.EquivalentTo(new[] { PartyRole.MainTank, PartyRole.OffTank }));
                    var damage = hits[rank - 1].Effects!.Single(e => e.Type == 3);
                    damages.Add((uint)(damage.Value | damage.Param3 << 16));
                    Assert.That(player.HasStatus(2941), Is.True, "D3 must have first-wing vulnerability");
                    TestContext.WriteLine($"seed=9 north={north} frontAgain={frontAgain} time={p.Time} rank={rank} damage={damages[^1]} position={player.Position}");
                }
                if (p.Crossed(60.5f)) after = player.Proxy!.Health;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(ranks, Has.Count.EqualTo(2), run.ToString);
        Assert.That(ranks[0], frontAgain ? Is.EqualTo(4) : Is.InRange(2, 4));
        Assert.That(ranks[1], frontAgain ? Is.EqualTo(3) : Is.InRange(5, 8));
        var baseHit = new uint[] { 160000, 70000, 55000, 55000, 40000, 15000, 10000, 10000 }[ranks[1] - 1];
        var expected = (uint)MathF.Ceiling(baseHit * 4f * (inadequate ? 1f : .9f * .9f * .85f));
        Assert.That(damages[1], Is.EqualTo(expected));
        Assert.That(before, Is.EqualTo(157_000));
        var shield = Math.Max(0f, 157000f * .25f - damages[0]) + 20000f;
        Assert.That(after, Is.EqualTo(inadequate ? 0 : before - Math.Max(0, (long)MathF.Ceiling(expected - shield))));
        TestContext.WriteLine($"second hpBefore={before} shieldBefore={shield} damage={expected} hpAfter={after}");
        Assert.That(run.Passed, Is.EqualTo(!inadequate), run.ToString);
        if (inadequate) Assert.That(run.Deaths.Single(d => d.Role == PartyRole.PhysRangedDps).Cause, Does.Contain("damage exceeded effective HP"));
    }
}
