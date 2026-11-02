#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P7DragonKing;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrP7Constants;
using Scenario = AnoMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingScenario;
using State = AnoMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingState;

namespace AnoMech.Tests;

public class DsrP7PracticeTests
{
    private static IEnumerable<TestCaseData> Seats() =>
        from role in Enum.GetValues<PartyRole>()
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
        select new TestCaseData(role, difficulty);

    [TestCaseSource(nameof(Seats))]
    public void EverySeatClearsWithDelayedSpawns(PartyRole role, BotDifficulty difficulty)
    {
        FakeGame? fake = null;
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 100 + (int)role, new()
        {
            PlayerRole = role, BotDifficulty = difficulty,
            ConfigureNative = f => { fake = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
    }

    [TestCase(AscalonElement.Flames)]
    [TestCase(AscalonElement.Ice)]
    public void ElementalGroupsHoldAllThreeAkhMorns(AscalonElement element)
    {
        var checkedHits = 0;
        PartyRole[][] groups = [[RegenHealer, MeleeDpsA, PhysRangedDps], [ShieldHealer, MeleeDpsB, CasterDps], [MainTank, OffTank]];
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 7, new()
        {
            Overrides = o => ((DsrP7DragonKingStateOverrides)o).Elements = Enumerable.Repeat(element, 8).ToArray(),
            Probe = p =>
            {
                for (var cycle = 0; cycle < State.Cycles; cycle++)
                {
                    if (!p.Crossed(Scenario.AkhMornLastHit(cycle) - .1f)) continue;
                    for (var spot = 0; spot < groups.Length; spot++)
                    {
                        var at = State.Spot((EdgeSpot)spot);
                        var occupants = Enum.GetValues<PartyRole>().Where(role =>
                        {
                            var position = p.Member(role)!.Position;
                            return Vector2.Distance(new(position.X, position.Z), at) < 4f;
                        });
                        Assert.That(occupants, Is.EquivalentTo(groups[spot]), $"cycle {cycle}, tower {spot}");
                        foreach (var role in groups[spot]) Assert.That(p.Member(role)!.Position.Length(), Is.LessThan(8f));
                    }
                    checkedHits++;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedHits, Is.EqualTo(3));
    }

    [Test]
    public void TrinityBaitsRotateMeleeRangedHealersAndTanksTradeElements()
    {
        PartyRole[] baits = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps, RegenHealer, ShieldHealer];
        var seen = 0;
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 9, new()
        {
            Probe = p =>
            {
                for (var i = 0; i < Scenario.TrinityHits.Length; i++)
                {
                    if (!p.Crossed(Scenario.TrinityHits[i] + .1f)) continue;
                    Assert.That(p.Member(baits[i % 6])!.HasStatus(StatusId.PhysicalVulnerabilityUp), Is.True);
                    var darkTank = i / 2 % 2 == 0 ? MainTank : OffTank;
                    Assert.That(p.Member(darkTank)!.FindStatus(StatusId.DarkResistanceDown)!.Stacks, Is.EqualTo(i % 2 + 1));
                    seen++;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(seen, Is.EqualTo(16));
    }

    [TestCase(12f)] [TestCase(45f)] [TestCase(100f)] [TestCase(200f)]
    public void RepeatedInterruptedEntryCleansActors(float stop)
    {
        var scenario = new Scenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame? fake = null;
            var run = ScenarioRun.Execute(typeof(Scenario), 0, repeat, new()
            {
                ScenarioFactory = () => scenario, StopAt = stop,
                ConfigureNative = f => fake = f,
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
        }
    }

    [TestCase(false)] [TestCase(true)]
    public void HiddenPartyAndSpectatorClear(bool spectator)
    {
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 42, new() { Spectator = spectator, ShowSimulatedParty = false });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void PausePreservesClockAndRunCompletes()
    {
        var paused = false;
        var scenario = new Scenario();
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario,
            Probe = p =>
            {
                if (!p.Crossed(20)) return;
                var before = p.Time;
                p.Game.Paused = true;
                for (var i = 0; i < 30; i++) p.Game.Tick(1f / 60);
                Assert.That(p.Time, Is.EqualTo(before));
                p.Game.Paused = false;
                paused = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(paused && scenario.Completed, Is.True);
    }
    [TestCase(false)] [TestCase(true)]
    public void HumanOffTankMustProvokeBeforeTheNextTrinityPair(bool provoke)
    {
        var scenario = new Scenario();
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 0, new()
        {
            PlayerRole = OffTank, ScenarioFactory = () => scenario,
            StopAt = Scenario.TrinityHits[2] + .2f,
            Takeovers = [new(35.55f, new(0, 9)), new(51f, new(0, -5))],
            Overrides = o => ((DsrP7DragonKingStateOverrides)o).Elements = Enumerable.Repeat(AscalonElement.Flames, 8).ToArray(),
            Probe = p =>
            {
                if (!provoke || !p.Crossed(46f)) return;
                var boss = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Single(e => e.BNpcBaseId == BNpcBaseId.DragonKingThordan);
                new AnoMech.Core.UserActions.LocalProvokeHandler().OnAction(FFXIVClientStructs.FFXIV.Client.Game.ActionType.Action, 7533, boss.GameObjectId.ObjectId);
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(scenario.AggroTank, Is.EqualTo(provoke ? OffTank : MainTank));
        if (provoke) Assert.That(run.Passed, run.ToString);
        else Assert.That(run.Deaths.Any(d => d.Role == MainTank && d.ActionId == ActionId.TrinityFirstEnmity), Is.True, run.ToString);
    }

}
