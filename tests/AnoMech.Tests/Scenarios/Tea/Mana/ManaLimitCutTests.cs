using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Tea;
using AnoMech.Scenarios.Tea.I1LimitCut;

namespace AnoMech.Tests;

public class ManaLimitCutTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void AllNumbersClearEveryStartingAxis(int axis)
    {
        var failures = new List<ScenarioRun>();
        for (var seed = 0; seed < 4; seed++)
        {
            var run = ScenarioRun.Execute(typeof(TeaI1LimitCutScenario), 0, seed, new ScenarioRunOptions
            {
                Overrides = o => ((TeaI1LimitCutStateOverrides)o).FirstBlasterAxis = axis,
            });
            if (!run.Passed) failures.Add(run);
        }
        Assert.That(failures, Is.Empty, () => string.Join("\n", failures));
    }

    private static IEnumerable<TestCaseData> NumberAssignments()
        => from axis in Enumerable.Range(0, 4)
           from number in Enumerable.Range(1, 8)
           select new TestCaseData(axis, number);

    [TestCaseSource(nameof(NumberAssignments))]
    public void EveryHumanNumberClearsWithSevenBots(int axis, int number)
    {
        var run = ScenarioRun.Execute(typeof(TeaI1LimitCutScenario), 0, 174 + number, new ScenarioRunOptions
        {
            PlayerRole = (PartyRole)(number - 1),
            BotDifficulty = number % 2 == 0 ? BotDifficulty.Hard : BotDifficulty.Easy,
            Overrides = value =>
            {
                var settings = (TeaI1LimitCutStateOverrides)value;
                settings.FirstBlasterAxis = axis;
                settings.Number[(PartyRole)(number - 1)] = number;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HumanAndBotBothDieInsideAHawkBlaster(bool bot)
    {
        var run = ScenarioRun.Execute(typeof(TeaI1LimitCutScenario), 0, 31, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.MainTank,
            StopAt = 6.5f,
            Overrides = value => ((TeaI1LimitCutStateOverrides)value).FirstBlasterAxis = 0,
            Takeovers = [new Takeover(5.15f, new(0, -14), Bot: bot ? PartyRole.MainTank : null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == TeaConstants.ActionId.HawkBlaster), Is.True, run.ToString);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeNumbersAndRestartCleanupAreReproducible(bool spectator)
    {
        var scenario = new TeaI1LimitCutScenario();
        FakeGame? native = null;
        var starts = 0;
        var restarted = false;
        var left = false;
        PartyRole[] numbers = [];
        SimEnemy[] oldActors = [];
        var run = ScenarioRun.Execute(typeof(TeaI1LimitCutScenario), 0, 174, new ScenarioRunOptions
        {
            ScenarioFactory = () => scenario,
            Spectator = spectator,
            ConfigureNative = fake => native = fake,
            BeforeStart = (game, _) => game.RunStarted += (_, _) => starts++,
            Probe = p =>
            {
                if (p.Time < 4f || left) return;
                Assert.That(p.World.Party.AllMembers().Select(m => m.LastLockonVfxId).Order(), Is.EqualTo(TeaConstants.LimitCutLockons.Select(x => (uint?)x).Order()));
                if (starts == 1 && !restarted)
                {
                    numbers = scenario.LastState!.Numbers.ToArray();
                    oldActors = p.World.Children.OfType<SimEnemy>().ToArray();
                    restarted = true;
                    p.Game.Restart();
                }
                else if (starts == 2)
                {
                    Assert.That(scenario.LastState!.Numbers, Is.EqualTo(numbers));
                    Assert.That(oldActors.All(a => !a.IsActive), Is.True);
                    left = true;
                    p.Game.Leave();
                }
            },
            StopWhen = p => left && p.Game.ActiveScenario is null,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(restarted && left, Is.True);
        Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(native.PlayerInput.PauseInput, Is.False);
        Assert.That(native.PlayerInput.SpectatorInput, Is.False);
    }

    [Test]
    public void ManaEastWinsTheEastWestTie()
    {
        Assert.That(TeaI1LimitCutAi.StartHeading(2), Is.EqualTo(90));
        Assert.That(new TeaI1LimitCutAi().Group, Is.EqualTo("Mana"));
    }
}
