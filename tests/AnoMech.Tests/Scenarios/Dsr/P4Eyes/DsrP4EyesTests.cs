#pragma warning disable CA1416
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P4Eyes;

namespace AnoMech.Tests;

public class DsrP4EyesTests
{
    private static IEnumerable<TestCaseData> Seats() =>
        from role in Enum.GetValues<PartyRole>()
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
        select new TestCaseData(role, difficulty);

    [TestCaseSource(nameof(Seats))]
    public void EverySeatTradesColoursAndUsesNorthRelativeMirageSwaps(PartyRole role, BotDifficulty difficulty)
    {
        var scenario = new DsrP4EyesScenario();
        FakeGame? native = null;
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 100 + (int)role, new()
        {
            PlayerRole = role, BotDifficulty = difficulty, ScenarioFactory = () => scenario,
            ConfigureNative = f => { native = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
            Probe = p =>
            {
                if (p.Crossed(40.8f))
                {
                    checks++;
                    Assert.That(scenario.LastState!.Red.Order(), Is.EqualTo(new[] { PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer }.Order()));
                }
                if (p.Crossed(58.8f))
                {
                    checks++;
                    Assert.That(scenario.LastState!.Red.Order(), Is.EqualTo(new[] { PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps }.Order()));
                }
                if (p.Crossed(64.8f))
                {
                    checks++;
                    Assert.That(scenario.LastState!.HoldsRed(PartyRole.RegenHealer), Is.True);
                    Assert.That(scenario.LastState!.HoldsRed(PartyRole.ShieldHealer), Is.True);
                    var firstDps = scenario.LastState.DiveTargets[0].Min();
                    var corner = firstDps switch
                    {
                        PartyRole.MeleeDpsA => new System.Numerics.Vector3(-15, 0, -5),
                        PartyRole.MeleeDpsB => new System.Numerics.Vector3(-15, 0, 5),
                        PartyRole.PhysRangedDps => new System.Numerics.Vector3(-5, 0, 5),
                        _ => new System.Numerics.Vector3(-5, 0, -5),
                    };
                    Assert.That(System.Numerics.Vector3.Distance(p.Member(PartyRole.RegenHealer)!.Position, corner), Is.LessThan(.2f));

                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(3));
        Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(native.EventObjects.OccupiedSlots, Is.Zero);
    }

    [TestCase(0f)] [TestCase(34f)] [TestCase(42f)] [TestCase(61f)] [TestCase(92f)] [TestCase(119f)] [TestCase(143f)] [TestCase(165f)]
    public void ReusedScenarioReleasesActorsStatusesAndTethers(float stop)
    {
        var scenario = new DsrP4EyesScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame? native = null;
            var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, repeat, new()
            {
                ScenarioFactory = () => scenario, StopAt = stop,
                ConfigureNative = f => native = f,
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(native.EventObjects.OccupiedSlots, Is.Zero);
        }
    }

    [Test]
    public void BoundedRandomizedEyesClear()
    {
        for (var seed = 0; seed < 24; seed++)
        {
            var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, seed + 500, new() { PlayerRole = (PartyRole)(seed % 8) });
            Assert.That(run.Passed, run.ToString);
        }
    }

    [Test]
    public void MissingSoulBuffAndEarlyOrbContactFail()
    {
        var missing = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, StopAt = 18,
            Takeovers = [new(1, new System.Numerics.Vector2(0, -15))], WriteArtifactsOnFailure = false,
        });
        Assert.That(missing.Deaths.Any(d => d.ActionId is 26821 or 26822), Is.True, missing.ToString);
        var early = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, StopAt = 38,
            Takeovers = [new(33.8f, new System.Numerics.Vector2(-17, 0))], WriteArtifactsOnFailure = false,
        });
        Assert.That(early.Deaths.Any(d => d.ActionId == 26817), Is.True, early.ToString);
    }

    [TestCase(false)] [TestCase(true)]
    public void HumanTankMustResolveLbAfterFriendAppears(bool useLb)
    {
        var pressed = false;
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, StopAt = 120,
            Takeovers = [new(118.5f, null)], WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (!useLb || !p.Crossed(119.1f)) return;
                p.World.NotifyActionResolved(p.Member(PartyRole.MainTank)!, 199, 0);
                pressed = true;
            },
        });
        Assert.That(pressed, Is.EqualTo(useLb));
        Assert.That(run.Deaths.Any(d => d.ActionId == 25314), Is.EqualTo(!useLb), run.ToString);
        if (useLb) Assert.That(run.Passed, run.ToString);
    }

    [TestCase(42f)] [TestCase(61f)] [TestCase(131f)]
    public void PauseFreezesEyesAndRescueThenBotsFinish(float at)
    {
        FakeGame native = null!;
        var paused = false;
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, 47, new()
        {
            ConfigureNative = f => native = f,
            Probe = p =>
            {
                if (paused || !p.Crossed(at)) return;
                paused = true;
                var time = p.Time;
                var positions = p.World.Party.AllMembers().Select(m => m.Position).ToArray();
                p.Game.Paused = true;
                for (var frame = 0; frame < 120; frame++) native.Frame(1f / 60f, p.Game.Tick);
                Assert.That(p.Time, Is.EqualTo(time));
                Assert.That(p.World.Party.AllMembers().Select(m => m.Position), Is.EqualTo(positions));
                p.Game.Paused = false;
            },
        });
        Assert.That(paused, Is.True);
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void ElementalEyesClear(int seed)
    {
        var scenario = new DsrP4EyesScenario();
        var run = ScenarioRun.Execute(typeof(DsrP4EyesScenario), 0, seed, new() { ScenarioFactory = () => scenario });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Completed, Is.True);
        Assert.That(scenario.RescueCompleted, Is.True);
        Assert.That(scenario.BlueHealingHits, Is.Zero);
        Assert.That(scenario.RedDamageHits, Is.EqualTo(16));
    }
}
