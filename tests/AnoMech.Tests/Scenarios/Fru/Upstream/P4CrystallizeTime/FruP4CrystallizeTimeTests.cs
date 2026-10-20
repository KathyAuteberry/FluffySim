using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.UserActions;
using AnoMech.Core.SimObjects;
using AnoMech.Windows;
using AnoMech.Scenarios;
using AnoMech.Core.EnemyActions;
using AnoMech.Scenarios.Fru.Upstream;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;

namespace AnoMech.Tests;

public class FruP4CrystallizeTimeTests
{
    private static IEnumerable<TestCaseData> ManaPatterns()
        => from role in Enum.GetValues<PartyRole>()
           from corner in Enum.GetValues<CrystallizeCorner>()
           from slow in new[] { false, true }
           from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
           select new TestCaseData(role, corner, slow, difficulty);

    [TestCaseSource(nameof(ManaPatterns))]
    public void ManaClearsEveryRoleAndWavePattern(PartyRole role, CrystallizeCorner corner, bool slow, BotDifficulty difficulty)
    {
        var recorded = false;
        var scenario = new FruP4CrystallizeTimeScenario();
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 47 + (int)role, new()
        {
            PlayerRole = role,
            ScenarioFactory = () => scenario,
            BotDifficulty = difficulty,
            Probe = p =>
            {
                if (!p.Crossed(FruP4CrystallizeTimeState.ReturnSavedAt)) return;
                recorded = true;
                var state = scenario.LastState;
                var puddles = state.AllDragonPuddles.OrderByDescending(puddle => puddle.Value.X).ToArray();
                Assert.That(state.PuddleConsumers, Has.Count.EqualTo(4));
                for (var i = 0; i < puddles.Length; i++)
                {
                    var expected = state.CleanseNumbers.Single(n => n.Value == i + 1).Key;
                    Assert.That(state.PuddleConsumers[puddles[i].Key], Is.EqualTo(expected));
                }
                foreach (var tank in new[] { PartyRole.MainTank, PartyRole.OffTank })
                {
                    var north = corner is CrystallizeCorner.NW or CrystallizeCorner.NE;
                    var east = corner is CrystallizeCorner.NE or CrystallizeCorner.SE;
                    Assert.That(p.World.Party.Get(tank)!.Position.Length(), Is.InRange(18.7f, 19f));
                    Assert.That(Vector3.Distance(p.World.Party.Get(tank)!.Position,
                        new Vector3((tank == PartyRole.MainTank ? -12f : 12f) * (north ? 1f : -1f), 0, north ? -14.5f : 14.5f)), Is.LessThan(0.1f));
                }
            },
            Overrides = o =>
            {
                var settings = (FruP4CrystallizeTimeStateOverrides)o;
                settings.Corner = corner;
                settings.SlowNorthwest = slow;
            },
        });
        Assert.That(recorded, Is.True);
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void DebuffDeckAndForcedTrainingAssignmentsRemainValid()
    {
        for (var seed = 0; seed < 128; seed++)
        foreach (var role in Enum.GetValues<PartyRole>())
        foreach (var wanted in Enum.GetValues<CrystallizeDebuff>())
        {
            var settings = new FruP4CrystallizeTimeStateOverrides { PlayerDebuffs = [wanted] };
            var state = new FruP4CrystallizeTimeState(new Rng(seed), role, settings);
            var same = new FruP4CrystallizeTimeState(new Rng(seed), role, settings);
            Assert.That(state.Debuff(role), Is.EqualTo(wanted));
            Assert.That(state.Holders(CrystallizeDebuff.RedAero).Count(), Is.EqualTo(2));
            Assert.That(state.Holders(CrystallizeDebuff.RedIce).Count(), Is.EqualTo(2));
            foreach (var blue in Enum.GetValues<CrystallizeDebuff>().Where(d => !FruP4CrystallizeTimeState.IsRed(d)))
                Assert.That(state.Holders(blue).Count(), Is.EqualTo(1));
            Assert.That(state.Quietus.Distinct().Count(), Is.EqualTo(3));
            Assert.That(Enum.GetValues<PartyRole>().Select(state.Debuff), Is.EqualTo(Enum.GetValues<PartyRole>().Select(same.Debuff)));
        }
    }

    [Test]
    public void ManaPriorityUsesHealersAtOppositeEnds()
        => Assert.That(FruP4CrystallizeTimeAi.ManaPriority, Is.EqualTo(new[]
        {
            PartyRole.RegenHealer, PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA,
            PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.ShieldHealer,
        }));

    [Test]
    public void H2SortsEastForEveryPossibleRedIcePartner()
    {
        foreach (var partner in Enum.GetValues<PartyRole>().Where(r => r != PartyRole.ShieldHealer))
        {
            var redIce = new[] {partner, PartyRole.ShieldHealer};
            var west = FruP4CrystallizeTimeAi.ManaPriority.First(redIce.Contains);
            Assert.That(west, Is.EqualTo(partner));
        }
    }

    [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
    public void YFormationPutsTanksFirstAndNeverDoubleHitsVulnerableNonTanks(bool east, bool north)
    {
        var roles = Enum.GetValues<PartyRole>();
        var firstSource = new Vector3(east ? 20 : -20, 0, 0);
        var secondSource = new Vector3(0, 0, north ? -20 : 20);
        PartyRole[] Front(Vector3 source) => roles.OrderBy(r => Vector3.DistanceSquared(source,
            FruP4CrystallizeTimeAi.ManaRewindSpot(r, east, north))).Take(4).ToArray();
        var first = Front(firstSource);
        var second = Front(secondSource);
        Assert.That(first[0].IsTank(), Is.True);
        Assert.That(second[0].IsTank(), Is.True);
        Assert.That(first.Intersect(second).All(r => r.IsTank()), Is.True);
        Assert.That(roles.Where(r => !r.IsTank()).All(r => FruP4CrystallizeTimeAi.ManaRewindSpot(r, east, north).Length() < 2), Is.True);
    }

    [TestCase(PartyRole.OffTank, true, false, true)]
    [TestCase(PartyRole.OffTank, false, false, false)]
    [TestCase(PartyRole.MainTank, true, false, true)]
    [TestCase(PartyRole.MainTank, true, true, false)]
    [TestCase(PartyRole.MainTank, false, false, false)]
    [TestCase(PartyRole.MeleeDpsA, true, true, true)]
    public void VulnerableWingRequiresTankAndConfiguredMitigation(PartyRole role, bool checks, bool mitigated, bool lethal)
    {
        bool? killed = null;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = role,
            UserActionsEnabled = true,
            Configure = c => c.EnableTankMitigation = !checks,
            StopAt = 1f,
            Takeovers = [new Takeover(0f, null)],
            Probe = p =>
            {
                if (!p.Crossed(0.5f)) return;
                var member = p.Member(role)!;
                member.AddStatus(FruConstants.StatusId.MagicVulnerabilityUp, 6f);
                if (mitigated)
                    foreach (ushort status in new ushort[] { 3829, 1191, 1193 }) member.AddStatus(status, 6f);
                var action = FruP4CrystallizeTimeActions.Wings(checks);
                var context = new EnemyActionContext(action, member, member, new(member.Position, 0), p.World.Party, 0);
                action.Effects[0].Apply(context, role == PartyRole.OffTank ? [p.Member(PartyRole.MainTank)!, member] : [member]);
                killed = context.IsKilled(member);
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(killed, Is.EqualTo(lethal));
    }

    [TestCase(false)] [TestCase(true)]
    public void WarriorWingChecksActualActionOrderAndBotPartyMitigation(bool consumeDamnation)
    {
        bool? killed = null;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 9, new()
        {
            PlayerRole = PartyRole.MainTank, UserActionsEnabled = true,
            Configure = c => c.EnableTankMitigation = true,
            StopAt = 56f, Takeovers = [new Takeover(53f, null)],
            Probe = p =>
            {
                if (p.Crossed(48.7f))
                {
                    var player = (SimPlayer)p.Member(PartyRole.MainTank)!;
                    void Press(uint action)
                    {
                        JobActions.ClearStatuses(player, action);
                        JobActions.ApplyEffects(player, action, (ulong)player.GameObjectId, new Random(0));
                    }
                    if (consumeDamnation) { Press(36923); Press(7388); }
                    else { Press(7388); Press(36923); }
                    Assert.That(player.HasStatus(3832), Is.EqualTo(!consumeDamnation));
                    Assert.That(player.HasStatus(1457), Is.True);
                }
                if (!p.Crossed(55f)) return;
                var member = p.Member(PartyRole.MainTank)!;
                foreach (ushort status in new ushort[] {3985, 2711, 1934})
                    Assert.That(member.HasStatus(status), Is.True, $"missing party reduction {status}");
                member.AddStatus(FruConstants.StatusId.MagicVulnerabilityUp, 6f);
                var action = FruP4CrystallizeTimeActions.HallowedWingsKnockback;
                var context = new EnemyActionContext(action, member, member, new(member.Position, 0), p.World.Party, 0);
                action.Effects[0].Apply(context, [member]);
                killed = context.IsKilled(member);
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(killed, Is.EqualTo(consumeDamnation), run.ToString);
    }

    [TestCase(true)] [TestCase(false)]
    public void HumanAntiKnockbackSuppressesOnlyTheFirstWing(bool timely)
    {
        bool? firstPush = null, secondPush = null;
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 53, new()
        {
            PlayerRole = PartyRole.RegenHealer,
            StopAt = 59.1f,
            Overrides = o => ((FruP4CrystallizeTimeStateOverrides)o).Corner = CrystallizeCorner.NE,
            Takeovers = [new Takeover(49f, null)],
            Probe = p =>
            {
                if (p.Crossed(50.6f))
                {
                    var practice = p.World.PracticeAntiKnockback;
                    Assert.That(PracticeAntiKnockbackControls.Evaluate(p.Game.ActiveScenario, true, practice).Visible, Is.True);
                    if (timely) Assert.That(PracticeAntiKnockbackControls.Press(p.Game.ActiveScenario, true, practice), Is.True);
                }
                bool Pushes()
                {
                    var member = p.Member(PartyRole.RegenHealer)!;
                    var action = FruP4CrystallizeTimeActions.HallowedWingsKnockback;
                    var context = new EnemyActionContext(action, member, member, new(member.Position, 0), p.World.Party, 0);
                    action.Effects[2].Apply(context, [member]);
                    return context.Outcomes.Any(o => o.Push is not null);
                }
                if (p.Crossed(53f)) firstPush = Pushes();
                if (timely && p.Crossed(59f)) secondPush = Pushes();
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.EqualTo(timely), run.ToString);
        Assert.That(firstPush, Is.EqualTo(!timely));
        if (timely) Assert.That(secondPush, Is.True);
        else Assert.That(run.Deaths.Any(d => d.Role == PartyRole.RegenHealer && d.Cause.Contains("arena")), Is.True, run.ToString);
    }

    [Test]
    public void HardBotsLeaveLessGuidanceTimeOnTheFinalRegroup()
    {
        var distances = new List<float>();
        foreach (var difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard })
        {
            var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 47, new()
            {
                PlayerRole = PartyRole.MainTank,
                BotDifficulty = difficulty,
                StopAt = 77.6f,
                Probe = p =>
                {
                    if (p.Crossed(77.5f)) distances.Add(p.Member(PartyRole.OffTank)!.Position.Length());
                },
            });
            Assert.That(run.Passed, run.ToString);
        }
        Assert.That(distances, Has.Count.EqualTo(2));
        Assert.That(distances[0], Is.LessThan(distances[1] - 0.5f));
    }

    [Test]
    public void UnhandledRedDragonAssignmentFails()
    {
        var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), 1, 57, new()
        {
            Overrides = o => ((FruP4CrystallizeTimeStateOverrides)o).PlayerDebuffs = [CrystallizeDebuff.RedIce],
            Takeovers = [new Takeover(12f, new Vector2(0, 8))],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths, Is.Not.Empty);
    }
}
