using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Tea.P2BjCc;
using static AnoMech.Scenarios.Tea.TeaConstants;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Tests;

public class TeaManaP2Tests
{
    public static IEnumerable<TestCaseData> SeatsAndDifficulties()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        foreach (var difficulty in Enum.GetValues<BotDifficulty>())
        foreach (var spectator in new[] { false, true })
            yield return new TestCaseData(role, difficulty, spectator);
    }

    public static IEnumerable<TestCaseData> FirstCompressionAssignments()
    {
        foreach (var nisiOnDps in new[] { false, true })
        foreach (var water in new[] { RegenHealer, ShieldHealer })
        foreach (var lightning in TeaManaP2Assignments.Dps)
        for (var axis = 0; axis < 4; axis++)
            yield return new TestCaseData(nisiOnDps, water, lightning, axis);
    }

    [Test]
    public void InitialNisiPairsAndLinePrioritiesFollowTheJapaneseMacro()
    {
        Assert.That(TeaManaP2Assignments.Dps.Select(TeaManaP2Assignments.FirstPairSupport),
            Is.EqualTo(new[] { MainTank, OffTank, RegenHealer, ShieldHealer }));
        Assert.That(TeaManaP2Assignments.ThirdPassLine,
            Is.EqualTo(new[] { MainTank, MeleeDpsA, PhysRangedDps, CasterDps, MeleeDpsB, OffTank }));
        Assert.That(TeaManaP2Assignments.FinalSupportLine,
            Is.EqualTo(new[] { MainTank, RegenHealer, ShieldHealer, OffTank }));
    }

    [Test]
    public void EveryCompressionUsesTheManaRoleGroups()
    {
        foreach (var water in new[] { RegenHealer, ShieldHealer })
        foreach (var lightning in TeaManaP2Assignments.Dps)
        {
            var first = TeaManaP2Assignments.WaterParty(1, water, lightning);
            Assert.That(first, Has.Count.EqualTo(4));
            Assert.That(first, Does.Contain(water));
            Assert.That(first, Does.Not.Contain(lightning));
            Assert.That(TeaManaP2Assignments.LightningParty(1, lightning), Is.EqualTo(new[] { MainTank, lightning }));
            Assert.That(TeaManaP2Assignments.WaterParty(2, water, lightning), Is.EquivalentTo(TeaManaP2Assignments.Dps));
            Assert.That(TeaManaP2Assignments.LightningParty(2, lightning), Is.EqualTo(new[] { MainTank, RegenHealer }));
            foreach (var vulnerable in TeaManaP2Assignments.Dps)
            {
                var third = TeaManaP2Assignments.WaterParty(3, water, lightning, vulnerable);
                Assert.That(third, Has.Count.EqualTo(4));
                Assert.That(third, Does.Contain(OffTank));
                Assert.That(third, Does.Not.Contain(vulnerable));
            }
            Assert.That(TeaManaP2Assignments.LightningParty(3, lightning), Is.EqualTo(new[] { RegenHealer, ShieldHealer }));
        }
    }

    [Test]
    public void EveryEnumerationPairSeparatesItsMarkersIntoThreePlayerGroups()
    {
        var priority = TeaManaP2Assignments.EnumerationPriority;
        for (var i = 0; i < priority.Count; i++)
        for (var j = i + 1; j < priority.Count; j++)
        {
            var (west, east) = TeaManaP2Assignments.EnumerationParties([priority[i], priority[j]]);
            Assert.That(west, Has.Count.EqualTo(3));
            Assert.That(east, Has.Count.EqualTo(3));
            Assert.That(west, Does.Contain(priority[i]));
            Assert.That(east, Does.Contain(priority[j]));
            Assert.That(west.Intersect(east), Is.Empty);
            Assert.That(west.Concat(east), Is.EquivalentTo(new[] { RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps }));
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    public void ManaBotsClearTheTwoBossPhase(int seed)
    {
        var scenario = new TeaP2BjCcScenario();
        var run = ScenarioRun.Execute(typeof(TeaP2BjCcScenario), 0, seed, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = MainTank,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(run.Elapsed, Is.InRange(183f, 188f));
    }

    [TestCase(1288239178, RegenHealer)]
    [TestCase(1271130084, ShieldHealer)]
    [TestCase(2141041402, MainTank)]
    [TestCase(287856692, MainTank)]
    [TestCase(1037, MeleeDpsA)]
    [TestCase(1068, MeleeDpsB)]
    [TestCase(1099, PhysRangedDps)]
    [TestCase(1224910100, PhysRangedDps)]
    public void DeterministicRoutingRegressionsPreserveNisiSeparation(int seed, PartyRole role)
    {
        var scenario = new TeaP2BjCcScenario();
        var run = ScenarioRun.Execute(typeof(TeaP2BjCcScenario), 0, seed, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = role,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(run.Elapsed, Is.InRange(183f, 188f));
    }

    [Test]
    public void BoundedFullPhaseCampaignPreservesNisiSeparation()
    {
        var failures = new List<string>();
        for (var seed = 0; seed < 128; seed++)
        {
            var scenario = new TeaP2BjCcScenario();
            var receivedAt = new Dictionary<PartyRole, float>();
            var run = ScenarioRun.Execute(typeof(TeaP2BjCcScenario), 0, seed, new()
            {
                ScenarioFactory = () => scenario,
                PlayerRole = (PartyRole)(seed % 8),
                Probe = p => RecordThirdTransfers(scenario, p, receivedAt),
            });
            if (!run.Passed) failures.Add(run.ToString());
            else if (receivedAt.Count != 4 || Math.Max(receivedAt[RegenHealer], receivedAt[ShieldHealer]) >= Math.Min(receivedAt[MainTank], receivedAt[OffTank]))
                failures.Add($"seed {seed}: third transfer did not complete both healer pairs before either tank pair ({string.Join(", ", receivedAt)})");
        }
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NisiAvoidancePauseRestartAndLeaveReleaseActorsAndInput(bool spectator)
    {
        FakeGame? native = null;
        var scenario = new TeaP2BjCcScenario();
        var starts = 0;
        var restarted = false;
        var leaving = false;
        SimCharacter[] originalParty = [];
        SimEnemy[] originalEnemies = [];
        KeyValuePair<PartyRole, NisiLetter>[] originalNisi = [];
        var run = ScenarioRun.Execute(typeof(TeaP2BjCcScenario), 0, 29, new()
        {
            ScenarioFactory = () => scenario,
            Spectator = spectator,
            PlayerRole = PhysRangedDps,
            ConfigureNative = value => native = value,
            BeforeStart = (game, _) => game.RunStarted += (_, _) => starts++,
            Probe = p =>
            {
                if (starts == 1 && !restarted && p.Time > 94f)
                {
                    originalParty = p.World.Party.AllMembers().ToArray();
                    originalEnemies = p.World.Children.OfType<SimEnemy>().Where(e => e.IsActive).ToArray();
                    originalNisi = scenario.LastState!.Nisi.ToArray();
                    p.Game.Paused = true;
                    var time = p.Time;
                    var poses = originalParty.Select(m => m.Position).ToArray();
                    for (var frame = 0; frame < 30; frame++) native!.Frame(1f / 60f, p.Game.Tick);
                    Assert.That(p.Time, Is.EqualTo(time));
                    Assert.That(originalParty.Select(m => m.Position), Is.EqualTo(poses));
                    p.Game.Paused = false;
                    restarted = true;
                    p.Game.Restart();
                }
                if (starts < 2 || p.Time < 95f || leaving) return;
                Assert.That(originalEnemies.All(e => !e.IsActive), Is.True);
                Assert.That(p.World.Party.AllMembers(), Is.EquivalentTo(originalParty));
                Assert.That(scenario.LastState!.Nisi.ToArray(), Is.EqualTo(originalNisi));
                leaving = true;
                p.Game.Leave();
            },
            StopWhen = p => leaving && p.Game.ActiveScenario is null,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(restarted && leaving, Is.True);
        Assert.That(starts, Is.EqualTo(2));
        Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(native.PlayerInput.PauseInput, Is.False);
        Assert.That(native.PlayerInput.SpectatorInput, Is.False);
        Assert.That(native.SpectatorCamera.Target, Is.Null);
    }

    [Test]
    public void NisiAvoidanceDoesNotConstrainHumanMovementOrSuppressArenaFalls()
    {
        var scenario = new TeaP2BjCcScenario();
        var run = ScenarioRun.Execute(typeof(TeaP2BjCcScenario), 0, 29, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = MainTank,
            Takeovers = [new(94f, new(21f, 0f))],
            StopAt = 94.2f,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Any(d => d.Role == MainTank && d.Cause == "Walked out of arena"), Is.True, run.ToString);
    }

    [TestCaseSource(nameof(FirstCompressionAssignments))]
    public void EveryInitialAssignmentCompletesItsFirstTransfersAndCompression(bool nisiOnDps, PartyRole water, PartyRole lightning, int axis)
    {
        var checkedTransfers = false;
        var scenario = new TeaP2BjCcScenario();
        var run = ScenarioRun.Execute(typeof(TeaP2BjCcScenario), 0, 29, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = MainTank,
            StopAt = 54.2f,
            Overrides = o =>
            {
                var roll = (TeaP2BjCcStateOverrides)o;
                roll.NisiOnDps = nisiOnDps;
                roll.FirstWaterHealer = water;
                roll.FirstLightning[lightning] = true;
                roll.ChakramAxis = axis;
            },
            Probe = p =>
            {
                if (!p.Crossed(54.05f)) return;
                var state = scenario.LastState!;
                foreach (var dps in TeaManaP2Assignments.Dps)
                {
                    var support = TeaManaP2Assignments.FirstPairSupport(dps);
                    var recipient = nisiOnDps ? support : dps;
                    var donor = nisiOnDps ? dps : support;
                    Assert.That(p.Member(recipient)!.HasStatus(StatusId.FinalDecreeNisi(state.Nisi[donor])), Is.True, $"{donor} -> {recipient}");
                }
                checkedTransfers = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedTransfers, Is.True);
    }

    [TestCaseSource(nameof(SeatsAndDifficulties))]
    public void EveryRoleClearsOrdinaryAndSpectatorPractice(PartyRole role, BotDifficulty difficulty, bool spectator)
    {
        var scenario = new TeaP2BjCcScenario();
        var run = ScenarioRun.Execute(typeof(TeaP2BjCcScenario), 0, 913 + (int)role * 31, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = role,
            BotDifficulty = difficulty,
            Spectator = spectator,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(run.Elapsed, Is.InRange(183f, 188f));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void ThirdTransferActuallyReachesBothHealerPairsBeforeTankPairs(int seed)
    {
        var scenario = new TeaP2BjCcScenario();
        var receivedAt = new Dictionary<PartyRole, float>();
        var run = ScenarioRun.Execute(typeof(TeaP2BjCcScenario), 0, seed, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = MainTank,
            Probe = p => RecordThirdTransfers(scenario, p, receivedAt),
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(receivedAt.Keys, Is.EquivalentTo(TeaManaP2Assignments.FinalSupportLine));
        var lastHealer = Math.Max(receivedAt[RegenHealer], receivedAt[ShieldHealer]);
        var firstTank = Math.Min(receivedAt[MainTank], receivedAt[OffTank]);
        Assert.That(lastHealer, Is.LessThan(firstTank));
    }

    private static void RecordThirdTransfers(TeaP2BjCcScenario scenario, ScenarioProbe p, Dictionary<PartyRole, float> receivedAt)
    {
        if (p.Time < 90f || p.Time > 110f || scenario.LastState is not { } state) return;
        foreach (var support in TeaManaP2Assignments.FinalSupportLine)
        {
            var recipient = state.NisiOnDps ? support : TeaManaP2Assignments.Dps.Single(d => state.Judgment[d] == state.Nisi[support]);
            if (p.Member(recipient)!.HasStatus(StatusId.FinalDecreeNisi(state.Judgment[recipient])))
                receivedAt.TryAdd(support, p.Time);
        }
    }

    [TestCase(MainTank)]
    [TestCase(OffTank)]
    [TestCase(MeleeDpsA)]
    public void MineDamageChecksActualMitigationOnEverySeat(PartyRole role)
    {
        var checkedDamage = false;
        var scenario = new TeaP2BjCcScenario();
        var run = ScenarioRun.Execute(typeof(TeaP2BjCcScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = MainTank,
            StopAt = 1f,
            Probe = p =>
            {
                if (!p.Crossed(.5f)) return;
                var target = p.Member(role)!;
                var caster = p.World.Children.OfType<SimEnemy>().First();
                EnemyActionContext Hit()
                {
                    var ctx = new EnemyActionContext(TeaP2BjCcActions.HiddenMine, caster, target, target.Placement(), p.World.Party, 0f, p.World.Events);
                    TeaP2BjCcActions.HiddenMine.Effects.Single().Apply(ctx, [target]);
                    return ctx;
                }
                Assert.That(Hit().IsKilled(target), Is.True);
                target.AddStatus(1191, 20);
                target.AddStatus(3829, 15);
                Assert.That(Hit().IsKilled(target), Is.EqualTo(!role.IsTank()));
                checkedDamage = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedDamage, Is.True);
    }
}
