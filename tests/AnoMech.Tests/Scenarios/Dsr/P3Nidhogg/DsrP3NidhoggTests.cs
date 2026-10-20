#pragma warning disable CA1416
using AnoMech.Core.Game.Ai;
using AnoMech.Scenarios;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P3Nidhogg;

namespace AnoMech.Tests;

public class DsrP3NidhoggTests
{
    [Test]
    public void HumanMainTankDoesNotPullBotsIntoItsCleave()
    {
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 4, new()
        {
            PlayerRole = PartyRole.MainTank, StopAt = 73f,
            Takeovers = [new(69.4f, new System.Numerics.Vector2(-6, -6))],
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void LanceTargetsRemainVariedWithinEveryPreparationPattern()
    {
        var states = Enumerable.Range(0, 128).Select(seed => new DsrP3NidhoggState(new Rng(seed), new(), null)).ToArray();
        foreach (var pattern in states.GroupBy(s => s.PreparationPattern))
            Assert.That(pattern.Select(s => s.DrachenlanceTarget).Distinct().Count(), Is.EqualTo(8));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void TankBaitsIndependentlyAndPartyPreparesForRandomLance(int seed)
    {
        var checks = 0;
        var scenario = new DsrP3NidhoggScenario();
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, seed, new()
        {
            ScenarioFactory = () => scenario,
            Probe = p =>
            {
                if (p.Crossed(69.65f))
                {
                    checks++;
                    Assert.That(p.Member(PartyRole.MainTank)!.Position.X, Is.LessThan(-5));
                    foreach (var role in Enum.GetValues<PartyRole>().Where(r => r != PartyRole.MainTank))
                    {
                        Assert.That(Math.Abs(p.Member(role)!.Position.X), Is.LessThan(1));
                        Assert.That(p.Member(role)!.Position.Z, Is.LessThan(-4));
                    }
                }
                if (p.Crossed(74.02f))
                {
                    checks++;
                    Assert.That(Enum.GetValues<PartyRole>().Count(r => p.Member(r)!.Position.Z > 0), Is.GreaterThanOrEqualTo(5));
                }
                if (p.Crossed(96.3f))
                {
                    checks++;
                    Assert.That(p.Member(PartyRole.MainTank)!.Position.Z, Is.LessThan(-5));
                    foreach (var role in Enum.GetValues<PartyRole>().Where(r => r != PartyRole.MainTank))
                        Assert.That(p.Member(role)!.Position.Z, Is.GreaterThan(5));
                    var boss = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Single(e => e.BNpcBaseId == 12605);
                    Assert.That(Math.Cos(boss.Rotation), Is.LessThan(-.99));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(3));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void EasthoggClears(int seed)
    {
        var scenario = new DsrP3NidhoggScenario();
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, seed, new() { ScenarioFactory = () => scenario });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Completed, Is.True);
    }
    private static IEnumerable<TestCaseData> Seats() =>
        from role in Enum.GetValues<PartyRole>()
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
        select new TestCaseData(role, difficulty);

    [TestCaseSource(nameof(Seats))]
    public void EverySeatClearsAndElementalTanksOwnTheirTethers(PartyRole role, BotDifficulty difficulty)
    {
        var checkedTethers = false;
        FakeGame? native = null;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 3100 + (int)role * 19, new()
        {
            PlayerRole = role, BotDifficulty = difficulty,
            ConfigureNative = f => { native = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
            Probe = p =>
            {
                if (!p.Crossed(90.9f)) return;
                checkedTethers = true;
                foreach (var tether in p.World.Children.OfType<AnoMech.Core.SimObjects.SimTether>().Where(t => t.IsActive))
                {
                    var bossTether = tether.A!.Position.LengthSquared() < 1;
                    Assert.That(tether.B, Is.SameAs(p.Member(bossTether ? PartyRole.MainTank : PartyRole.OffTank)), $"source={tether.A.Position}, holder={((AnoMech.Core.SimObjects.ISimPartyMember)tether.B!).Role} at {tether.B.Position}; MT={p.Member(PartyRole.MainTank)!.Position} ST={p.Member(PartyRole.OffTank)!.Position}");
                    Assert.That(tether.B!.Position.Length(), Is.LessThan(4));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedTethers, Is.True);
        Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
    }

    [Test]
    public void PracticeOverridesKeepNumbersAndArrowsValidForEverySeat()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        foreach (var line in new[] { 1, 2, 3 })
        foreach (var arrow in Enum.GetValues<DiveArrow>())
        {
            var state = new DsrP3NidhoggState(new Rng((int)role + line), new() { PlayerLine = line, PlayerArrow = arrow }, role);
            Assert.That(state.Lines[role], Is.EqualTo(line));
            Assert.That(state.Arrows[role], Is.EqualTo(arrow));
            Assert.That(state.Lines.Values.GroupBy(n => n).OrderBy(g => g.Key).Select(g => g.Count()), Is.EqualTo(new[] { 3, 2, 3 }));
            Assert.That(Enumerable.Range(1, 3).Count(state.LineHasArrows), Is.InRange(1, 2));
            Assert.That(state.TowerSoakers.Sum(), Is.EqualTo(8));
        }
    }

    [TestCase(0f)] [TestCase(36f)] [TestCase(43f)] [TestCase(84f)] [TestCase(90f)] [TestCase(128f)]
    public void RestartAndLeaveReleaseActorsTethersAndStatuses(float stop)
    {
        var scenario = new DsrP3NidhoggScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame? native = null;
            var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, repeat, new()
            {
                ScenarioFactory = () => scenario, StopAt = stop,
                ConfigureNative = f => { native = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
        }
    }

    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.OffTank)]
    public void HumanTankMustUseInvulnerabilityForSoulTethers(PartyRole role)
    {
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            PlayerRole = role, UserActionsEnabled = true,
            Takeovers = [new(90.85f, null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == role && d.ActionId == 26396), Is.True, run.ToString);
    }

    [Test]
    public void MissingDiveTowerWipesTheParty()
    {
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank,
            Overrides = o => ((DsrP3NidhoggStateOverrides)o).PlayerLine = 3,
            Takeovers = [new(41.5f, new System.Numerics.Vector2(0, -10))],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == 26395), Is.True, run.ToString);
    }
    [Test]
    public void BoundedRandomizedEasthoggClears()
    {
        for (var seed = 0; seed < 32; seed++)
        foreach (var difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard })
        {
            var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 4000 + seed, new()
            {
                BotDifficulty = difficulty,
                PlayerRole = (PartyRole)(seed % 8),
            });
            Assert.That(run.Passed, run.ToString);
        }
    }
    [Test]
    public void NativeOpeningWaitsForEngagementAndKeepsWaitingDiversHidden()
    {
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            StopAt = 12f,
            ConfigureNative = f => f.BattleCharas.PacketSpawnDelayFrames = 5,
            Probe = p =>
            {
                if (p.Crossed(1f))
                {
                    checks++;
                    var enemies = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().ToArray();
                    var boss = enemies.Single(e => e.BNpcBaseId == 12605);
                    Assert.That(boss.Proxy!.TargetableStatus & 3, Is.Zero);
                    var divers = enemies.Where(e => e.BNpcBaseId == 12606).ToArray();
                    Assert.That(divers.Length, Is.EqualTo(8));
                    Assert.That(divers.All(e => ((FakeBattleChara)e.Proxy!).ModelHidden), Is.True);
                }
                if (p.Crossed(11f))
                {
                    checks++;
                    var boss = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Single(e => e.BNpcBaseId == 12605);
                    Assert.That(boss.Proxy!.TargetableStatus & 3, Is.Not.Zero);
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(2));
    }
    [Test]
    public void EasthoggForwardArrowClaimsAndSoaksWest()
    {
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, StopAt = 44f,
            Overrides = o =>
            {
                var settings = (DsrP3NidhoggStateOverrides)o;
                settings.PlayerLine = 3; settings.PlayerArrow = DiveArrow.Up;
            },
            Probe = p =>
            {
                if (p.Crossed(29f))
                {
                    checks++;
                    Assert.That(p.Member(PartyRole.MainTank)!.Position.X, Is.LessThan(0));
                }
                if (p.Crossed(43.6f))
                {
                    checks++;
                    Assert.That(p.Member(PartyRole.MainTank)!.Position.X, Is.LessThan(0));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(2));
    }

    [TestCase(false)] [TestCase(true)]
    public void DiveEffectsPreserveCapturedTargetPoseAndNativeTowerEmitter(bool visible)
    {
        var scenario = new DsrP3NidhoggScenario();
        var checks = 0;
        var poses = new Dictionary<FFXIVClientStructs.FFXIV.Client.Game.Object.GameObjectId, (float Rotation, System.Numerics.Vector3 Position)>();
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, ShowSimulatedParty = visible, StopAt = 70,
            ConfigureNative = f => f.BattleCharas.PacketSpawnDelayFrames = 5,
            Probe = p =>
            {
                foreach (var (at, line) in new[] { (37.25f, 1), (47.24f, 2), (58.26f, 3) })
                    if (p.Crossed(at))
                        foreach (var role in scenario.LastState!.Line(line))
                        {
                            var member = p.Member(role)!;
                            poses[member.GameObjectId] = (member.Rotation, member.Position);
                        }
                if (!p.Crossed(69)) return;
                var divers = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Where(e => e.BNpcBaseId == 12606).ToArray();
                foreach (var diver in divers)
                {
                    var effects = ((FakeBattleChara)diver.Proxy!).ReceivedEffects;
                    foreach (var effect in effects.Where(e => e.ActionId is >= 26382 and <= 26384))
                    {
                        checks++;
                        var pose = poses[effect.AnimationTarget!.Value];
                        Assert.That(effect.Rotation, Is.EqualTo(pose.Rotation).Within(.01f));
                        Assert.That(effect.AnimationLock, Is.EqualTo(1.1f));
                        Assert.That(effect.Targets!.Any(t => t.Id == effect.AnimationTarget), Is.True);
                    }
                    foreach (var effect in effects.Where(e => e.ActionId == 26385))
                    {
                        checks++;
                        Assert.That(effect.AnimationTarget, Is.EqualTo(diver.GameObjectId));
                        Assert.That(effect.AnimationLock, Is.EqualTo(1.1f));
                    }
                }
            },
        });
        Assert.That(run.Passed, run.ToString); Assert.That(checks, Is.EqualTo(16));
    }
}
