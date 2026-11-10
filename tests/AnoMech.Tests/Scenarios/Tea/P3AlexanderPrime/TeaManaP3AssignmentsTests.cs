using System.Numerics;
using AnoMech.Scenarios;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Tea.P3AlexanderPrime;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Tea.TeaConstants;

namespace AnoMech.Tests;

public class TeaManaP3AssignmentsTests
{
    private static readonly ushort[] InvulnerabilityStatuses = [82, 409, 810, 1836];
    private static readonly ushort[] TankLimitBreakStatuses = [196, 863, 864, 1931];
    [Test]
    public void FixedThirdAndFourthKeepTheirSidesWithoutReadingRobotSide()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TeaManaP3Assignments.InitialNumberSpot(3).X, Is.LessThan(0));
            Assert.That(TeaManaP3Assignments.InitialNumberSpot(4).X, Is.GreaterThan(0));
            Assert.That(TeaManaP3Assignments.InitialNumberSpot(3).Y, Is.GreaterThan(0));
            Assert.That(TeaManaP3Assignments.InitialNumberSpot(4).Y, Is.GreaterThan(0));
        });
    }

    [Test]
    public void EveryEnumerationPairUsesDpsPriorityWithTanksNorthAndHealersSouth()
    {
        foreach (var first in TeaP3AlexanderPrimeState.Dps)
        foreach (var second in TeaP3AlexanderPrimeState.Dps.Where(r => r > first))
        {
            PartyRole[] marked = [second, first];
            var north = new[] { first, MainTank, OffTank };
            var south = new[] { second, RegenHealer, ShieldHealer };
            Assert.That(north.All(r => TeaManaP3Assignments.EnumerationSpot(r, marked).Y < 0), Is.True);
            Assert.That(south.All(r => TeaManaP3Assignments.EnumerationSpot(r, marked).Y > 0), Is.True);
            foreach (var free in TeaP3AlexanderPrimeState.Dps.Except(marked))
            {
                var spot = TeaManaP3Assignments.EnumerationSpot(free, marked);
                Assert.That(Vector2.Distance(spot, TeaManaP3Assignments.EnumerationSpot(first, marked)), Is.GreaterThan(4));
                Assert.That(Vector2.Distance(spot, TeaManaP3Assignments.EnumerationSpot(second, marked)), Is.GreaterThan(4));
            }
        }
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void WormholeAllLayoutsResolveWithoutBotExemptions(bool justiceNorthWest, bool puddlesMirrored)
    {
        var scenario = new TeaP3AlexanderPrimeScenario();
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 137, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            Spectator = true,
            StopAt = 166.1f,
            Overrides = value =>
            {
                var o = (TeaP3AlexanderPrimeStateOverrides)value;
                o.Practice = P3Practice.Wormhole;
                o.BruteJusticeNorthWest = justiceNorthWest;
                o.PuddlesMirrored = puddlesMirrored;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(false, 0, MainTank)]
    [TestCase(true, 1, OffTank)]
    [TestCase(false, 2, RegenHealer)]
    [TestCase(true, 3, ShieldHealer)]
    [TestCase(false, 4, MeleeDpsA)]
    [TestCase(true, 5, MeleeDpsB)]
    [TestCase(false, 6, PhysRangedDps)]
    [TestCase(true, 7, CasterDps)]
    public void InceptionUsesManaBaitsForEverySeatAndSacramentSector(bool north, int sector, PartyRole role)
    {
        var scenario = new TeaP3AlexanderPrimeScenario();
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 811 + sector, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            PlayerRole = role,
            Overrides = value =>
            {
                var o = (TeaP3AlexanderPrimeStateOverrides)value;
                o.Practice = P3Practice.Inception;
                o.AlexanderPrimeNorth = north;
                o.SacramentSpot = sector;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(run.Elapsed, Is.LessThan(96.67f));
    }

    [TestCase(BotDifficulty.Easy, false)]
    [TestCase(BotDifficulty.Hard, true)]
    public void WholePhaseClearsInOrdinaryAndEightBotPractice(BotDifficulty difficulty, bool spectator)
    {
        var scenario = new TeaP3AlexanderPrimeScenario();
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 29, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            BotDifficulty = difficulty,
            Spectator = spectator,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void CyclicPermutationsExerciseEverySeatInEveryNumber(int rotation)
    {
        var scenario = new TeaP3AlexanderPrimeScenario();
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 910 + rotation, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            Spectator = true,
            PlayerRole = (PartyRole)rotation,
            BotDifficulty = rotation < 4 ? BotDifficulty.Easy : BotDifficulty.Hard,
            StopAt = 166.1f,
            Overrides = value =>
            {
                var o = (TeaP3AlexanderPrimeStateOverrides)value;
                o.Practice = P3Practice.Wormhole;
                o.BruteJusticeNorthWest = (rotation & 1) != 0;
                o.PuddlesMirrored = (rotation & 2) != 0;
                foreach (var role in PerRole.All)
                    o.Number[role] = ((int)role + rotation) % 8 + 1;
            },
        });
        Assert.That(run.Passed, run.ToString);
        foreach (var role in PerRole.All)
            Assert.That(scenario.LastState!.NumberOf(role), Is.EqualTo(((int)role + rotation) % 8 + 1));
    }

    private static ScenarioRun BrokenWormhole(int number, float at, Vector2 to)
        => ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 137, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP3AlexanderPrimeScenario(),
            PlayerRole = MainTank,
            StopAt = 148.5f,
            WriteArtifactsOnFailure = false,
            Overrides = value =>
            {
                var o = (TeaP3AlexanderPrimeStateOverrides)value;
                o.Practice = P3Practice.Wormhole;
                o.BruteJusticeNorthWest = true;
                o.PuddlesMirrored = false;
                o.Number[MainTank] = number;
            },
            Takeovers = [new Takeover(at, to)],
        });

    [Test]
    public void MissingTheFirstSoakTriggersConfession()
    {
        var run = BrokenWormhole(5, 146.2f, Vector2.Zero);
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths, Is.Not.Empty);
        Assert.That(run.Deaths[0].ActionId, Is.EqualTo(ActionId.Confession), run.ToString);
    }

    [Test]
    public void ARecentlySlashedPlayerCannotStealTheFirstSoak()
    {
        var run = BrokenWormhole(1, 144.8f, new(-17f, -5f));
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Any(d => d.Role == MainTank && d.ActionId == ActionId.Repentance1), Is.True, run.ToString);
    }

    [Test]
    public void AbandoningTheFirstTankBaitMakesTheOffTankTakeTwoFlames()
    {
        var scenario = new TeaP3AlexanderPrimeScenario();
        var firstFlameVulnerability = false;
        var nativeHitsOnOffTank = 0;
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 811, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            PlayerRole = MainTank,
            StopAt = 67f,
            WriteArtifactsOnFailure = false,
            Overrides = value =>
            {
                var o = (TeaP3AlexanderPrimeStateOverrides)value;
                o.Practice = P3Practice.Inception;
                o.AlexanderPrimeNorth = false;
            },
            Takeovers = [new Takeover(59.8f, TeaManaP3Assignments.FlameSpot(RegenHealer))],
            Probe = p =>
            {
                if (p.Crossed(61.2f)) firstFlameVulnerability = p.Member(OffTank)!.HasStatus(StatusId.FireResistanceDownII);
                if (!p.Crossed(63.5f)) return;
                var offTank = p.Member(OffTank)!;
                nativeHitsOnOffTank = ((FakeBattleChara)scenario.LastState!.Objects.BruteJustice!.Proxy!).ReceivedEffects
                    .Count(effect => effect.ActionId == ActionId.FlarethrowerP3 && effect.Targets?.Any(t => t.Id == offTank.GameObjectId) == true);
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(firstFlameVulnerability, Is.True, run.ToString);
        Assert.That(nativeHitsOnOffTank, Is.EqualTo(2), run.ToString);
        Assert.That(run.Deaths.Any(d => d.Role == OffTank && d.ActionId == ActionId.FlarethrowerP3 && d.Cause.Contains("vuln")), Is.True, run.ToString);
    }

    [Test]
    public void HoldingTheFirstFlamePositionBaitsAnotherFlameAndKillsTheTank()
    {
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 811, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP3AlexanderPrimeScenario(),
            PlayerRole = MainTank,
            StopAt = 67f,
            WriteArtifactsOnFailure = false,
            Overrides = value =>
            {
                var o = (TeaP3AlexanderPrimeStateOverrides)value;
                o.Practice = P3Practice.Inception;
                o.AlexanderPrimeNorth = false;
            },
            Takeovers = [new Takeover(60f, TeaManaP3Assignments.FlameSpot(MainTank))],
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Any(d => d.Role == MainTank && d.ActionId == ActionId.FlarethrowerP3), Is.True, run.ToString);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FlameTanksWithdrawSoThePartyNaturallyBecomesClosest(bool north)
    {
        var scenario = new TeaP3AlexanderPrimeScenario();
        var checkedPositions = false;
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 811, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            Spectator = true,
            StopAt = 67f,
            Overrides = value =>
            {
                var o = (TeaP3AlexanderPrimeStateOverrides)value;
                o.Practice = P3Practice.Inception;
                o.AlexanderPrimeNorth = north;
            },
            Probe = p =>
            {
                if (!p.Crossed(64.6f)) return;
                var boss = scenario.LastState!.Objects.BruteJustice!;
                var closestParty = PerRole.All.Where(r => r is not MainTank and not OffTank)
                    .Min(r => Vector3.Distance(p.Member(r)!.Position, boss.Position));
                Assert.That(Vector3.Distance(p.Member(MainTank)!.Position, boss.Position), Is.GreaterThan(closestParty));
                Assert.That(Vector3.Distance(p.Member(OffTank)!.Position, boss.Position), Is.GreaterThan(closestParty));
                checkedPositions = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedPositions, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WormholePauseRestartAndLeaveReleaseEveryActorAndInputGate(bool spectator)
    {
        FakeGame? native = null;
        var scenario = new TeaP3AlexanderPrimeScenario();
        var starts = 0;
        var paused = false;
        var restarted = false;
        var leaving = false;
        SimEnemy[] originalEnemies = [];
        SimEventObject[] originalObjects = [];
        PartyRole[] originalNumbers = [];
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 137, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            Spectator = spectator,
            PlayerRole = PhysRangedDps,
            ConfigureNative = value => native = value,
            BeforeStart = (game, _) => game.RunStarted += (_, _) => starts++,
            Overrides = value => ((TeaP3AlexanderPrimeStateOverrides)value).Practice = P3Practice.Wormhole,
            Probe = p =>
            {
                if (starts == 1 && !paused && p.Time > 136f)
                {
                    paused = true;
                    originalEnemies = p.World.Children.OfType<SimEnemy>().Where(e => e.IsActive).ToArray();
                    originalObjects = p.World.Children.OfType<SimEventObject>().Where(e => e.IsActive).ToArray();
                    originalNumbers = scenario.LastState!.Numbers.ToArray();
                    Assert.That(originalObjects, Has.Length.EqualTo(2));
                    p.Game.Paused = true;
                    var time = p.Time;
                    var poses = p.World.Party.AllMembers().Select(m => m.Placement()).ToArray();
                    for (var frame = 0; frame < 30; frame++) native!.Frame(1f / 60f, p.Game.Tick);
                    Assert.That(p.Time, Is.EqualTo(time));
                    Assert.That(p.World.Party.AllMembers().Select(m => m.Placement()), Is.EqualTo(poses));
                    p.Game.Paused = false;
                    restarted = true;
                    p.Game.Restart();
                }
                if (starts < 2 || p.Time < 136.1f || leaving) return;
                Assert.That(originalEnemies.All(e => !e.IsActive), Is.True);
                Assert.That(originalObjects.All(e => !e.IsActive), Is.True);
                Assert.That(scenario.LastState!.Numbers, Is.EqualTo(originalNumbers));
                Assert.That(p.World.Children.OfType<SimEventObject>().Count(e => e.IsActive), Is.EqualTo(2));
                leaving = true;
                p.Game.Leave();
            },
            StopWhen = p => leaving && p.Game.ActiveScenario is null,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(paused && restarted && leaving, Is.True);
        Assert.That(starts, Is.EqualTo(2));
        Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(native.PlayerInput.PauseInput, Is.False);
        Assert.That(native.PlayerInput.SpectatorInput, Is.False);
        Assert.That(native.SpectatorCamera.Target, Is.Null);
    }

    [Test]
    public void EightBotRunActuallyUsesInvulnerabilitiesAndTankLimitBreak()
    {
        var scenario = new TeaP3AlexanderPrimeScenario();
        var firstSpears = false;
        var secondSpears = false;
        var judgment = false;
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 29, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            Spectator = true,
            Probe = p =>
            {
                if (p.Crossed(20f))
                {
                    firstSpears = true;
                    Assert.That(InvulnerabilityStatuses.Any(id => p.Member(MainTank)!.HasStatus(id)), Is.True);
                    Assert.That(p.Member(MainTank)!.Position.Z, Is.GreaterThan(0f));
                    foreach (var role in PerRole.All.Where(r => r is not MainTank and not OffTank))
                        Assert.That(p.Member(role)!.Position.Z, Is.LessThan(0f));
                }
                if (p.Crossed(111.5f))
                {
                    secondSpears = true;
                    Assert.That(InvulnerabilityStatuses.Any(id => p.Member(OffTank)!.HasStatus(id)), Is.True);
                    Assert.That(p.Member(OffTank)!.Position.Z, Is.GreaterThan(0f));
                }
                if (p.Crossed(287.6f))
                {
                    judgment = true;
                    foreach (var role in PerRole.All)
                        Assert.That(TankLimitBreakStatuses.Any(id => p.Member(role)!.HasStatus(id)), Is.True, role.ToString());
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(firstSpears && secondSpears && judgment, Is.True);
    }

    [Test]
    public void BotWithoutItsActualInvulnerabilityDiesToDivineSpear()
    {
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 29, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP3AlexanderPrimeScenario(),
            Spectator = true,
            StopAt = 17f,
            WriteArtifactsOnFailure = false,
            Overrides = value => ((TeaP3AlexanderPrimeStateOverrides)value).Practice = P3Practice.Inception,
            Probe = p =>
            {
                if (!p.Crossed(14f)) return;
                foreach (var id in InvulnerabilityStatuses) p.Member(MainTank)!.RemoveStatus(id);
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Any(d => d.Role == MainTank && d.ActionId == ActionId.DivineSpear), Is.True, run.ToString);
    }

    [Test]
    public void BotsWithoutActualTankLimitBreakDieToDivineJudgment()
    {
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 29, new ScenarioRunOptions
        {
            ScenarioFactory = () => new TeaP3AlexanderPrimeScenario(),
            Spectator = true,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (!p.Crossed(287.2f)) return;
                foreach (var role in PerRole.All)
                foreach (var id in TankLimitBreakStatuses)
                    p.Member(role)!.RemoveStatus(id);
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Where(d => d.ActionId == ActionId.DivineJudgmentRaidwide).Select(d => d.Role),
            Is.EquivalentTo(PerRole.All), run.ToString);
    }
}
