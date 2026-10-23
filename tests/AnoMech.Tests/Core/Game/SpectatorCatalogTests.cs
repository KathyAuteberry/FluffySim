#pragma warning disable CA1416
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru;
using AnoMech.Scenarios.Dsr;

namespace AnoMech.Tests;

public class SpectatorCatalogTests
{
    internal static IEnumerable<IScenario> Scenarios() => ScenarioCatalog.Create().Scenarios
        .Where(s => s.Phase.Zone is FruZone or AnoMech.Scenarios.Fru.Upstream.FruZone or DsrZone);

    private static IEnumerable<TestCaseData> Runs() =>
        from scenario in Scenarios()
        from strat in Enumerable.Range(0, scenario.AiStrats.Count)
        from role in Enum.GetValues<PartyRole>()
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
        select new TestCaseData(scenario.GetType(), strat, role, difficulty)
            .SetName($"Spectator {scenario.FullName()} [{scenario.AiStrats[strat].Name}] {role} {difficulty}");

    [Test]
    public void EveryPlayableFruAndDsrEntryAcceptsSpectatorMode()
    {
        Assert.That(Scenarios().Count(), Is.EqualTo(28));
        foreach (var scenario in Scenarios())
        {
            TestContext.Out.WriteLine($"CATALOG|{scenario.GetType().FullName}|{scenario.Phase.Name}|{scenario.MenuName}|{string.Join(", ", scenario.AiStrats.Select(a => a.Name))}|{scenario.IsIncomplete}");
            Assert.That(scenario.SupportsSpectator, Is.True, scenario.FullName());
            Assert.That(scenario.AiStrats, Is.Not.Empty, scenario.FullName());
        }
    }

    [TestCaseSource(nameof(Lifecycles))]
    public void EveryEntryCanRestartThenLeave(Type scenario, int strat, bool fail)
    {
        FakeGame? native = null;
        var restarted = false;
        var left = false;
        var starts = 0;
        var killed = false;
        var run = ScenarioRun.Execute(scenario, strat, 7391, new()
        {
            Spectator = true, PlayerRole = PartyRole.CasterDps, StopAt = 5,
            ConfigureNative = f => native = f,
            BeforeStart = (g, _) => g.RunStarted += (_, _) => starts++,
            StopWhen = _ => left,
            Probe = p =>
            {
                if (fail && !killed && p.Time > 1)
                {
                    killed = true;
                    p.Member(PartyRole.CasterDps)!.Die(123, "intentional spectator lifecycle failure");
                }
                if (!restarted && p.Time > 2)
                {
                    restarted = true;
                    p.Game.Paused = true;
                    p.Game.Restart();
                }
                if (starts != 2 || left || p.Time < 3) return;
                left = true;
                p.Game.Leave();
                native!.Frame(0, p.Game.Tick);
                Assert.That(native.SpectatorCamera.Target, Is.Null);
                Assert.That(native.PlayerInput.SpectatorInput, Is.False);
                Assert.That(native.BattleCharas.OccupiedSlots, Is.Zero);
                Assert.That(p.World.Party.AllMembers(), Is.Empty);
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(restarted && left, Is.True);
        Assert.That(starts, Is.EqualTo(2));
        Assert.That(run.Deaths.Count, Is.EqualTo(fail ? 1 : 0));
    }

    private static IEnumerable<TestCaseData> Lifecycles() =>
        from scenario in Scenarios()
        from strat in Enumerable.Range(0, scenario.AiStrats.Count)
        from fail in new[] { false, true }
        select new TestCaseData(scenario.GetType(), strat, fail)
            .SetName($"Spectator lifecycle {scenario.FullName()} [{scenario.AiStrats[strat].Name}] failed={fail}");

    [TestCaseSource(nameof(Runs))]
    public void EveryRoleCompletesWithEightOrdinaryBots(Type scenario, int strat, PartyRole role, BotDifficulty difficulty)
    {
        FakeGame? native = null;
        var initialized = false;
        var run = ScenarioRun.Execute(scenario, strat, 7391, new()
        {
            Spectator = true, PlayerRole = role, UserActionsEnabled = true, BotDifficulty = difficulty,
            ConfigureNative = f => native = f,
            Probe = p =>
            {
                if (initialized) return;
                initialized = true;
                Assert.That(p.World.Party.AllMembers().Count(), Is.EqualTo(8));
                Assert.That(p.World.Party.AllMembers(), Has.All.TypeOf<SimPartyNpc>());
                Assert.That(p.World.Party.Player, Is.Null);
                Assert.That(p.Game.WatchedRole, Is.EqualTo(role));
                Assert.That(native!.SpectatorCamera.Target, Is.SameAs(p.Member(role)!.Proxy));
                var rows = native.PartyHud.Members;
                Assert.That(rows, Has.Count.EqualTo(8));
                Assert.That(rows[0].Role, Is.EqualTo(role));
                Assert.That(rows.Select(r => r.Role), Is.EquivalentTo(Enum.GetValues<PartyRole>()));
                Assert.That(rows.Select(r => r.Actor), Is.Unique);
                Assert.That(rows.Select(r => r.Actor), Does.Not.Contain(native.BattleCharas.Player.EntityId));
            },
        });
        Assert.That(initialized, Is.True, run.ToString);
        Assert.That(run.Passed, run.ToString);
        Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(native.SpectatorCamera.Target, Is.Null);
        Assert.That(native.PlayerInput.SpectatorInput, Is.False);
    }
}
