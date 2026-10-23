#pragma warning disable CA1416
using AnoMech.Core.SimObjects;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game;
using AnoMech.Scenarios.Dsr.P3Nidhogg;
using AnoMech.Scenarios.Dsr.Strength;
using AnoMech.Scenarios.Fru.LightRampant;

namespace AnoMech.Tests;

public class SpectatorPartyLifecycleTests
{
    private static IEnumerable<TestCaseData> Seats() =>
        from scenario in new[] { typeof(DsrP3NidhoggScenario), typeof(FruLightRampantScenario) }
        from role in Enum.GetValues<PartyRole>()
        select new TestCaseData(scenario, role);

    [TestCaseSource(nameof(Seats))]
    public void SpectatorEnumerationHasEveryRoleAndWatchedActorInSelfRow(Type scenario, PartyRole watched)
    {
        FakeGame? native = null;
        var checkedRoster = false;
        var run = ScenarioRun.Execute(scenario, 0, 7391, new()
        {
            Spectator = true, PlayerRole = watched, StopAt = 4,
            ConfigureNative = f => native = f,
            Probe = p =>
            {
                if (!p.Crossed(3)) return;
                checkedRoster = true;
                var rows = native!.PartyHud.Members.ToArray();
                Assert.That(rows, Has.Length.EqualTo(8));
                Assert.That(rows[0].Role, Is.EqualTo(watched));
                Assert.That(rows.Select(r => r.Role), Is.EquivalentTo(Enum.GetValues<PartyRole>()));
                Assert.That(rows.Select(r => r.Actor), Is.Unique);
                Assert.That(rows.Select(r => r.Actor), Does.Not.Contain(native.BattleCharas.Player.EntityId));
                p.Game.Paused = true;
                native.Frame(0, p.Game.Tick);
                Assert.That(native.PartyHud.Members.ToArray(), Is.EqualTo(rows.ToArray()));
                p.Game.Paused = false;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedRoster, Is.True);
        Assert.That(native!.PartyHud.Members, Is.Empty);
    }
    [TestCase(false)] [TestCase(true)]
    public void RoleChangesRestartAndNormalPracticeReplaceRowsWithoutStaleMembers(bool fail)
    {
        FakeGame? native = null;
        RunScenarioParams? current = null;
        var starts = 0;
        var requestedAt = 0;
        var killed = false;
        var checkedFinal = false;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 7391, new()
        {
            Spectator = true, PlayerRole = PartyRole.CasterDps, StopAt = 4,
            WriteArtifactsOnFailure = false,
            ConfigureNative = f => native = f,
            BeforeStart = (g, _) => g.RunStarted += (p, _) => { current = p; starts++; },
            Probe = p =>
            {
                if (fail && starts == 1 && !killed && p.Time > .5f)
                {
                    killed = true;
                    p.Member(PartyRole.CasterDps)!.Die(123, "intentional party restoration test");
                }
                var rows = native!.PartyHud.Members;
                if (p.Time > .2f)
                {
                    Assert.That(rows, Has.Count.EqualTo(8));
                    Assert.That(rows.Select(r => r.Role), Is.EquivalentTo(Enum.GetValues<PartyRole>()));
                    Assert.That(rows.Select(r => r.Actor), Is.Unique);
                    Assert.That(rows[0].Actor, Is.EqualTo(current!.WatchedRole is { } watched
                        ? p.Member(watched)!.Proxy!.EntityId : native.BattleCharas.Player.EntityId));
                }
                if (starts == 4) { checkedFinal = true; return; }
                if (p.Time < 1 || starts == requestedAt) return;
                requestedAt = starts;
                p.Game.StartScenario(starts switch
                {
                    1 => current! with { Scenario = new DsrStrengthScenario(), WatchedRole = PartyRole.MainTank },
                    2 => current! with { Party = new LocalPartyProvider { RoleOverride = PartyRole.CasterDps }, WatchedRole = null },
                    _ => current! with { Scenario = new DsrP3NidhoggScenario(), Party = new LocalPartyProvider { Spectator = true }, WatchedRole = PartyRole.CasterDps },
                });
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(run.Deaths.Count, Is.EqualTo(fail ? 1 : 0));
        Assert.That(starts, Is.EqualTo(4));
        Assert.That(checkedFinal, Is.True);
        Assert.That(native!.PartyHud.Members, Is.Empty);
        Assert.That(native.BattleCharas.OccupiedSlots, Is.Zero);
    }

}
