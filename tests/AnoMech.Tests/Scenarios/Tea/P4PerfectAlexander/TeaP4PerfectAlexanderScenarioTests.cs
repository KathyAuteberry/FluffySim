using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Tea;
using AnoMech.Scenarios.Tea.P4PerfectAlexander;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Tea.TeaConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Scenario time 0 is 12s before The Final Word.
//
//    12.00  The Final Word; its debuffs at 16.80 (10s). The light beacon waits north of A, the dark
//           beacon on C, the followers on C's north edge.
//    19.15  The first Ordained cast, checked at 23.21; 26.61 the Ordained statuses (5s), and the
//           followers march 20y at 5y/s from 27.41: light ones toward their beacon, dark ones
//           straight away from theirs, a dark one struck down as it passes 24y; the second Ordained
//           check at 34.32.
//    41.48  Optical Sight, hits at 46.57; the other one at 47.56, hits at 52.64. Stacks: MT H1 M1 PR
//           west, OT H2 M2 CA east.
//    68.73  Fate Calibration α; for real from 97.90: Ordained, 100.89 the defamation, 101.43 the
//           shared stack, 101.97 the aggravated hits and the first Sacrament, 102.00 Ordained.
//   113.21  Ordained Capital Punishment's hits at 119.31 / 120.38 / 121.45, Ordained Punishment 125.37.
//   143.61  Fate Calibration β; for real: 183.73 the Ordained statuses and tethers, 190.79 the
//           tethers' checks and the shared stack, 191.79 J Jumps, 197.90 Optical Sight, 202.82 the donut.
//   232.49  Almighty Judgment: tiles lit from 238.60, blasts 246.63 / 248.62 / 250.61.
//   315.06  Temporal Prison, jails from 325.06 every 5s.
public class TeaP4PerfectAlexanderScenarioTests
{
    private static NegativeRun<TeaP4PerfectAlexanderScenario> Run(PartyRole player, Action<TeaP4PerfectAlexanderStateOverrides>? tweak = null)
        => Negative<TeaP4PerfectAlexanderScenario>(player).Overrides<TeaP4PerfectAlexanderStateOverrides>(o => tweak?.Invoke(o));

    private static NegativeRun<TeaP4PerfectAlexanderScenario> Practice(P4Practice practice, PartyRole player,
        Action<TeaP4PerfectAlexanderStateOverrides>? tweak = null)
        => Run(player, o =>
        {
            o.Practice = practice;
            tweak?.Invoke(o);
        });

    private static void FinalWordPinned(TeaP4PerfectAlexanderStateOverrides o)
    {
        o.FinalWordMotionFirst = true;
        o.FinalWord[CasterDps] = FinalWordDebuff.LightBeacon;
        o.FinalWord[ShieldHealer] = FinalWordDebuff.DarkBeacon;
        foreach (var role in new[] { OffTank, MeleeDpsB, PhysRangedDps }) o.FinalWord[role] = FinalWordDebuff.LightFollower;
        foreach (var role in new[] { MainTank, RegenHealer, MeleeDpsA }) o.FinalWord[role] = FinalWordDebuff.DarkFollower;
    }

    [Test]
    public void DiesWalkingIntoWall()
        => Run(MeleeDpsA)
            .TeleportAt(5f, to: new(0, 20.5f))
            .ShouldKill(TheEnvironment, MeleeDpsA);

    // 6y off the dark beacon on C, the march ends 26y from it.
    [Test]
    public void DarkFollowerStartingTooFarFromItsBeaconIsStruckDown()
        => Run(MeleeDpsA, FinalWordPinned)
            .TeleportAt(25.5f, to: new(0, 8))
            .ShouldKill(ActionId.DivineRetributionDark, MeleeDpsA);

    // 6y off its beacon it passes 24y 3.6s into the march, and is struck down there, not at the end.
    [Test]
    public void TheDarkStrikeLandsAsTheMarchCrossesTheLimit()
    {
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 3, new ScenarioRunOptions
        {
            PlayerRole = MeleeDpsA,
            StopAt = 33f,
            Takeovers = [new Takeover(25.5f, new Vector2(0, 8))],
            Overrides = o => FinalWordPinned((TeaP4PerfectAlexanderStateOverrides)o),
        });
        var death = run.Deaths.Single(d => d.Role == MeleeDpsA);
        Assert.That(death.ActionId, Is.EqualTo(ActionId.DivineRetributionDark), run.ToString);
        Assert.That(death.Time, Is.InRange(31.4f, 31.8f), run.ToString);
    }

    [Test]
    public void LightFollowerStartingNearItsBeaconMarchesIntoIt()
        => Run(PhysRangedDps, FinalWordPinned)
            .TeleportAt(25.5f, to: new(0, 2))
            .ShouldKill(ActionId.DivineRetributionLight, PhysRangedDps);

    // A light follower already on its beacon when the Ordained statuses land dies before the march sets off.
    [Test]
    public void LightFollowerOnItsBeaconDiesBeforeTheMarch()
    {
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 3, new ScenarioRunOptions
        {
            PlayerRole = PhysRangedDps,
            StopAt = 27.4f,
            Takeovers = [new Takeover(25.5f, new Vector2(0.1f, -13.4f))],
            Overrides = o => FinalWordPinned((TeaP4PerfectAlexanderStateOverrides)o),
        });
        Assert.That(run.Deaths.Select(d => (d.Role, d.ActionId)), Is.EquivalentTo(new[] { (PhysRangedDps, (uint?)ActionId.DivineRetributionLight) }),
            run.ToString);
    }

    // The light beacon stands still through Ordained Motion; its followers go down with it, before any march.
    [Test]
    public void FollowersGoDownWithTheirBeacon()
    {
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 3, new ScenarioRunOptions
        {
            PlayerRole = CasterDps,
            StopAt = 26f,
            Takeovers = [new Takeover(21.5f, null)],
            Overrides = o => FinalWordPinned((TeaP4PerfectAlexanderStateOverrides)o),
        });
        var followers = run.Deaths.Where(d => d.ActionId == ActionId.DivineRetributionLight).Select(d => d.Role);
        Assert.That(followers, Is.EquivalentTo(new[] { OffTank, MeleeDpsB, PhysRangedDps }), run.ToString);
    }

    // Motion first, so the second check is Stillness; a step after the march breaks it.
    [Test]
    public void MovingBeforeOrdainedStillnessKills()
        => Run(RegenHealer, FinalWordPinned)
            .TeleportAt(33.2f, to: new(3, -6))
            .ShouldKill(ActionId.OrdainedStillnessFail, RegenHealer);

    // An instant action 0.8s before the Stillness check breaks it like a step does.
    [Test]
    public void ActingBeforeOrdainedStillnessKills()
        => Run(RegenHealer, FinalWordPinned)
            .At(33.5f, p => p.PressAction())
            .ShouldKill(ActionId.OrdainedStillnessFail, RegenHealer);

    [Test]
    public void AnAutoAttackSwingBeforeOrdainedStillnessKills()
        => Run(RegenHealer, FinalWordPinned)
            .At(33.5f, p => ((SimPlayer)p.Member(RegenHealer)!).CountAutoAttackSwing())
            .ShouldKill(ActionId.OrdainedStillnessFail, RegenHealer);

    // An action let go 2.3s before the check is fine, though its lock runs on into the window.
    [Test]
    public void AnEarlierActionsLockIsFine()
    {
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, Random.Shared.Next(), new ScenarioRunOptions
        {
            PlayerRole = RegenHealer,
            StopAt = 36f,
            Overrides = o => FinalWordPinned((TeaP4PerfectAlexanderStateOverrides)o),
            Probe = p =>
            {
                if (p.Crossed(32.0f)) ((ISimPartyMember)p.Member(MainTank)!).UseAction(KnockbackImmunity.ArmsLengthActionId);
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void ABotActingBeforeOrdainedStillnessDies()
        => Run(RegenHealer, FinalWordPinned)
            .At(33.5f, p => ((ISimPartyMember)p.Member(MainTank)!).UseAction(KnockbackImmunity.ArmsLengthActionId))
            .ShouldKill(ActionId.OrdainedStillnessFail, MainTank);

    // The last action 2.4s before the check, as real parties end theirs, is still allowed.
    [Test]
    public void ActingBeforeTheStillnessWindowIsFine()
    {
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, Random.Shared.Next(), new ScenarioRunOptions
        {
            PlayerRole = RegenHealer,
            StopAt = 36f,
            Overrides = o => FinalWordPinned((TeaP4PerfectAlexanderStateOverrides)o),
            Probe = p =>
            {
                if (p.Crossed(31.9f)) p.PressAction();
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    // The main tank as the dark beacon by C stands out of Perfect Alexander's reach.
    [Test]
    public void TheDarkBeaconSpotIsOutOfReachOfTheAutos()
    {
        string? boss = null, mainTank = null;
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 3, new ScenarioRunOptions
        {
            PlayerRole = RegenHealer,
            StopAt = 26f,
            Overrides = o =>
            {
                var overrides = (TeaP4PerfectAlexanderStateOverrides)o;
                overrides.FinalWordMotionFirst = true;
                overrides.FinalWord[MainTank] = FinalWordDebuff.DarkBeacon;
            },
            Probe = p =>
            {
                boss ??= p.World.Children.OfType<SimEnemy>().FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.PerfectAlexander)?.EntityId.ToString("X8");
                mainTank ??= p.Member(MainTank)?.EntityId.ToString("X8");
            },
        });
        Assert.That(run.Passed, run.ToString);
        var autos = ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows
            .Where(r => r["kind"] == "ability" && r["actor"] == boss && r["id"] == ActionId.PerfectAlexanderAttack.ToString() && r["target"] == mainTank)
            .Select(r => float.Parse(r["t"], System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        Assert.That(autos.Where(t => t < 16f), Is.Not.Empty, "it autos the main tank before the Final Word sends it south");
        Assert.That(autos.Where(t => t is > 21f and < 26f), Is.Empty);
    }

    [Test]
    public void StandingStillThroughOrdainedMotionKills()
        => Run(RegenHealer, FinalWordPinned)
            .FreezeAt(21.5f)
            .ShouldKill(ActionId.OrdainedMotionFail, RegenHealer);

    // The marched followers end up 20y north of their spots.
    [Test]
    public void TheMarchCarriesFollowersTwentyYalms()
    {
        Vector3? before = null, after = null;
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 3, new ScenarioRunOptions
        {
            PlayerRole = MainTank,
            StopAt = 33f,
            Overrides = o => FinalWordPinned((TeaP4PerfectAlexanderStateOverrides)o),
            Probe = p =>
            {
                if (p.Crossed(27.3f)) before = p.Member(PhysRangedDps)!.Position;
                if (p.Crossed(31.6f)) after = p.Member(PhysRangedDps)!.Position;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(Vector3.Distance(before!.Value, after!.Value), Is.EqualTo(20f).Within(0.3f));
        Assert.That(after!.Value.Z, Is.LessThan(before!.Value.Z - 19f));
    }

    // The player's own march goes out as the step and stop the client walks.
    [Test]
    public void ThePlayersOwnMarchWalksItThere()
    {
        Vector3? before = null, after = null;
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 3, new ScenarioRunOptions
        {
            PlayerRole = MainTank,
            StopAt = 33f,
            Overrides = o => FinalWordPinned((TeaP4PerfectAlexanderStateOverrides)o),
            Probe = p =>
            {
                if (p.Crossed(27.3f)) before = p.Member(MainTank)!.Position;
                if (p.Crossed(31.6f)) after = p.Member(MainTank)!.Position;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(Vector3.Distance(before!.Value, after!.Value), Is.EqualTo(20f).Within(0.3f));
        Assert.That(after!.Value.Z, Is.LessThan(before!.Value.Z - 19f));
    }

    // The stack first, on the player and M1; alone at the north wall, Faithlessness follows.
    [Test]
    public void StackTakenAloneSetsOffFaithlessness()
        => Run(RegenHealer, o =>
            {
                FinalWordPinned(o);
                o.OpticalSpreadFirst = false;
                o.OpticalStackTargets = [RegenHealer, MeleeDpsB];
            })
            .TeleportAt(44f, to: new(0, -17))
            .ShouldKill(ActionId.CollectiveReprobation, RegenHealer)
            .ShouldKill(ActionId.Faithlessness, AllBut(RegenHealer));

    // Spread first; H1 on the main tank's spread spot takes both hits, and so does the main tank.
    [Test]
    public void OverlappingSpreadsKillBoth()
        => Run(RegenHealer, o =>
            {
                FinalWordPinned(o);
                o.OpticalSpreadFirst = true;
            })
            .TeleportAt(45.5f, to: new(0, -12))
            .ShouldKill(ActionId.IndividualReprobation, RegenHealer, MainTank);

    private static void AlphaPinned(TeaP4PerfectAlexanderStateOverrides o, PartyRole player, AlphaSentence sentence)
    {
        o.Practice = P4Practice.FateCalibrationAlpha;
        o.AlphaSafeClone = 1;
        o.AlphaFirstMotion = false;
        o.AlphaSecondMotion = false;
        o.Alpha[player] = sentence;
    }

    // With the second Alexander safe the shared stack waits at (4.3,17.1); an aggravated player in it
    // carries the stack's vulnerability into its own hit.
    [Test]
    public void AggravatedPlayerInTheSharedStackDies()
        => Run(RegenHealer, o => AlphaPinned(o, RegenHealer, AlphaSentence.AggravatedAssault))
            .TeleportAt(99f, to: new(4.3f, 17.6f))
            .ShouldKill(ActionId.PlaintOfSeverity, RegenHealer);

    [Test]
    public void SomeoneInTheDefamationDies()
        => Run(MeleeDpsB, o => AlphaPinned(o, MeleeDpsB, AlphaSentence.Nothing))
            .TeleportAt(99f, to: new(-4f, -12f))
            .ShouldKill(ActionId.PlaintOfObloquy, MeleeDpsB);

    // (0,15) lies in the third Alexander's cross.
    [Test]
    public void StandingInASacramentCrossDies()
        => Run(MeleeDpsB, o => AlphaPinned(o, MeleeDpsB, AlphaSentence.Nothing))
            .TeleportAt(99f, to: new(0f, 15f))
            .ShouldKill(ActionId.Sacrament, MeleeDpsB);

    [Test]
    public void MovingBeforeTheFutureStillnessKills()
        => Run(MeleeDpsB, o => AlphaPinned(o, MeleeDpsB, AlphaSentence.Nothing))
            .TeleportAt(97f, to: new(4f, 16f))
            .ShouldKill(ActionId.OrdainedStillnessFail, MeleeDpsB);

    [Test]
    public void NonTankInCapitalPunishmentDies()
        => Practice(P4Practice.CapitalPunishmentOne, RegenHealer)
            .TeleportAt(118f, to: new(0, -9.3f))
            .ShouldKill(ActionId.OrdainedCapitalPunishmentHit, RegenHealer);

    // The off tank never provokes: Perfect Alexander keeps the main tank through Ordained
    // Punishment's vulnerability, and its next auto kills.
    [Test]
    public void MissedSwapAfterOrdainedPunishmentKillsTheTank()
        => Run(OffTank)
            .FreezeAt(122f)
            .ShouldKill(ActionId.PerfectAlexanderAttack, MainTank);

    [Test]
    public void StandingNextToOrdainedPunishmentDies()
        => Practice(P4Practice.CapitalPunishmentOne, RegenHealer)
            .TeleportAt(123.5f, to: new(-10f, -4.5f))
            .ShouldKill(ActionId.OrdainedPunishment, RegenHealer);

    private static void BetaPinned(TeaP4PerfectAlexanderStateOverrides o)
    {
        o.Practice = P4Practice.FateCalibrationBeta;
        o.BetaOpticalStack = true;
        o.BetaOpticalClone = 2;
        o.BetaDonutClone = 0;
        o.Beta[MainTank] = BetaRole.LightFar;
        o.Beta[RegenHealer] = BetaRole.DarkFar;
        o.Beta[ShieldHealer] = BetaRole.LightNear;
        o.Beta[MeleeDpsA] = BetaRole.DarkNear;
        o.Beta[OffTank] = BetaRole.LightShared;
        o.Beta[CasterDps] = BetaRole.DarkUntethered;
        o.Beta[MeleeDpsB] = BetaRole.LightBeacon;
        o.Beta[PhysRangedDps] = BetaRole.DarkBeacon;
    }

    [Test]
    public void HouseArrestPartnersApartBothDie()
        => Run(ShieldHealer, BetaPinned)
            .TeleportAt(189.5f, to: new(0, -5))
            .ShouldKill(ActionId.PlaintOfSurety, ShieldHealer, MeleeDpsA);

    // Next to the dark beacon, the east bait.
    [Test]
    public void StandingInSomeoneElsesJJumpDies()
        => Run(MainTank, BetaPinned)
            .TeleportAt(191f, to: new(13.5f, -1f))
            .ShouldKill(ActionId.JJump, MainTank);

    [Test]
    public void StayingOutsideRadiantSacramentDies()
        => Run(MainTank, BetaPinned)
            .TeleportAt(197.95f, to: new(-15f, 0f))
            .ShouldKill(ActionId.RadiantSacrament, MainTank);

    // The centre's set first, waymark 3's last: start on 3, step to the centre once the first set
    // goes off. Frozen on 3, the third blast lands on the player.
    [Test]
    public void StayingOnTheStartingTileDies()
        => Practice(P4Practice.AlmightyJudgmentOne, RegenHealer, o => o.JudgmentOrders[0] = [0, 1, 2])
            .FreezeAt(246.7f)
            .ShouldKill(ActionId.AlmightyJudgmentBlast, RegenHealer);

    [Test]
    public void SteppingDiagonallyIntoTheSecondSetDies()
        => Practice(P4Practice.AlmightyJudgmentOne, RegenHealer, o => o.JudgmentOrders[0] = [0, 1, 2])
            .TeleportAt(247f, to: new(8, 8))
            .ShouldKill(ActionId.AlmightyJudgmentBlast, RegenHealer);

    // The first jail (north-east) is H1's; frozen in the middle, it lands on nobody.
    [Test]
    public void AJailLandingOnNobodyEndsTheFight()
        => Practice(P4Practice.TemporalPrison, RegenHealer)
            .FreezeAt(318f)
            .ShouldKill(ActionId.TemporalPrison, PerRole.All);

    private static IEnumerable<TestCaseData> Practices()
        => Enum.GetValues<P4Practice>().Where(p => p != P4Practice.WholePhase).Select(p => new TestCaseData(p).SetArgDisplayNames(p.ToString()));

    // The second clone's Optical Sight mirrors the projections; the party keeps its spots, and the
    // south bait's jumper is the third clone.
    [TestCase(0, 0)]
    [TestCase(1, 2)]
    [TestCase(3, 3)]
    public void SecondCloneOpticalSightClears(int bait, int donut)
    {
        var state = StateWith(o =>
        {
            o.BetaOpticalClone = 1;
            o.BetaDonutClone = bait;
        });
        Assert.That(state.BetaDonutClone, Is.EqualTo(donut));
        foreach (var seed in Enumerable.Range(1, TestContext.Parameters.Get("PracticeSeeds", 8)))
        {
            var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, seed, new ScenarioRunOptions
            {
                Overrides = o =>
                {
                    var overrides = (TeaP4PerfectAlexanderStateOverrides)o;
                    overrides.Practice = P4Practice.FateCalibrationBeta;
                    overrides.BetaOpticalClone = 1;
                    overrides.BetaDonutClone = bait;
                },
            });
            Assert.That(run.Passed, run.ToString);
        }
    }

    // SunnyDay runs only the whole phase.
    [TestCaseSource(nameof(Practices))]
    public void EachPracticeClearsWithTheStrat(P4Practice practice)
    {
        foreach (var seed in Enumerable.Range(1, TestContext.Parameters.Get("PracticeSeeds", 8)))
        {
            var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, seed,
                new ScenarioRunOptions { Overrides = o => ((TeaP4PerfectAlexanderStateOverrides)o).Practice = practice });
            Assert.That(run.Passed, run.ToString);
        }
    }

    // The jails fill in order with the slowest kill, and the last player stays free.
    [Test]
    public void SlowestKillReachesTheSixthJail()
    {
        var jailed = new List<PartyRole>();
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 5, new ScenarioRunOptions
        {
            Overrides = o =>
            {
                var overrides = (TeaP4PerfectAlexanderStateOverrides)o;
                overrides.Practice = P4Practice.TemporalPrison;
            },
            Probe = p =>
            {
                foreach (var role in PerRole.All)
                    if (p.Member(role) is { } member && member.HasStatus(StatusId.TemporalPrison) && !jailed.Contains(role)) jailed.Add(role);
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(jailed, Is.EqualTo(TeaP4PerfectAlexanderAi.Prisoners.Take(6)));
    }

    // Its HP stays full until it dies, at the slowest real kill.
    [Test]
    public void PerfectAlexanderDiesAtFullHpAtTheSlowestKill()
    {
        var lowestHpBefore = 1f;
        float? diedAt = null;
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 3, new ScenarioRunOptions
        {
            Overrides = o => ((TeaP4PerfectAlexanderStateOverrides)o).Practice = P4Practice.TemporalPrison,
            Probe = p =>
            {
                if (p.World.Children.OfType<SimEnemy>().FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.PerfectAlexander) is not { } boss) return;
                if (boss.HpFraction > 0f) lowestHpBefore = MathF.Min(lowestHpBefore, boss.HpFraction);
                else diedAt ??= p.Time;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(lowestHpBefore, Is.EqualTo(1f));
        Assert.That(diedAt, Is.EqualTo(353.8f).Within(0.05f));
    }

    // After the first Ordained Punishment the off tank holds Perfect Alexander, and the main tank
    // still carries its vulnerability.
    [Test]
    public void FateCalibrationBetaStartsWhereThePhaseWouldLeaveIt()
    {
        SimEnemy? target = null;
        var vulnerable = false;
        var hp = 0f;
        var run = ScenarioRun.Execute(typeof(TeaP4PerfectAlexanderScenario), 0, 2, new ScenarioRunOptions
        {
            StopAt = 130f,
            Overrides = o =>
            {
                var overrides = (TeaP4PerfectAlexanderStateOverrides)o;
                overrides.Practice = P4Practice.FateCalibrationBeta;
            },
            Probe = p =>
            {
                if (!p.Crossed(129f)) return;
                target = p.World.Children.OfType<SimEnemy>().First(e => e.BNpcBaseId == BNpcBaseId.PerfectAlexander);
                vulnerable = p.Member(MainTank)!.HasStatus(StatusId.PhysicalVulnerabilityUp);
                hp = target.HpFraction;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(target, Is.Not.Null);
        Assert.That(vulnerable, Is.True);
        Assert.That(hp, Is.EqualTo(1f));
    }

    [Test]
    public void PracticeStartsOnItsPlaceInTheTrack()
    {
        var scenario = new TeaP4PerfectAlexanderScenario();
        var overrides = (TeaP4PerfectAlexanderStateOverrides)scenario.SettingsOverrides;
        Assert.That(scenario.Phase, Is.SameAs(TeaZone.P4));
        Assert.That(scenario.BgmSecondsAtStart, Is.EqualTo(49.94f).Within(1e-3f));
        overrides.Practice = P4Practice.FateCalibrationBeta;
        Assert.That(scenario.BgmSecondsAtStart, Is.EqualTo(177.94f).Within(1e-3f));
    }

    [Test]
    public void SeatSettingsTheFightCannotProduceAreRefused()
    {
        PerRole.ForceSeats = true;
        try
        {
            var overrides = new TeaP4PerfectAlexanderStateOverrides();
            Assert.That(overrides.Validate().Any, Is.False);
            overrides.FinalWord[MainTank] = FinalWordDebuff.LightBeacon;
            overrides.FinalWord[OffTank] = FinalWordDebuff.LightBeacon;
            overrides.Alpha[MainTank] = AlphaSentence.Defamation;
            overrides.Alpha[RegenHealer] = AlphaSentence.Defamation;
            overrides.Beta[MainTank] = BetaRole.DarkBeacon;
            overrides.Beta[OffTank] = BetaRole.DarkBeacon;
            Assert.That(overrides.Validate().Problems, Has.Count.EqualTo(3));
        }
        finally
        {
            PerRole.ForceSeats = false;
        }
    }

    [Test]
    public void ReplayStateRoundTrips()
    {
        var source = StateWith(_ => { });
        var message = new AnoMech.Multiplayer.TeaP4PerfectAlexanderAiReplayStateMessage(source.Practice, source.LightBeacon,
            source.DarkBeacon, source.LightFollowers.ToArray(), source.FinalWordMotionFirst, source.OpticalSpreadFirst,
            source.OpticalStackTargets.ToArray(), source.AlphaClones.ToArray(), source.AlphaFirstMotion, source.AlphaSecondMotion,
            source.AlphaSafeClone, source.BetaClones.ToArray(), source.BetaOpticalStack, source.BetaOpticalClone, source.BetaDonutClone,
            source.JudgmentOrders.Select(o => o.ToArray()).ToArray(), source.GraceTargets.ToArray());
        var shadow = TeaP4PerfectAlexanderState.FromNetworkReplay(message.Practice, message.LightBeacon, message.DarkBeacon,
            message.LightFollowers, message.FinalWordMotionFirst, message.OpticalSpreadFirst, message.OpticalStackTargets, message.AlphaClones,
            message.AlphaFirstMotion, message.AlphaSecondMotion, message.AlphaSafeClone, message.BetaClones, message.BetaOpticalStack,
            message.BetaOpticalClone, message.BetaDonutClone, message.JudgmentOrders, message.GraceTargets);
        Assert.That(shadow, Is.Not.Null);
        Assert.That(shadow!.DarkFollowers, Is.EquivalentTo(source.DarkFollowers));
        Assert.That(shadow.AlphaClones, Is.EqualTo(source.AlphaClones));
        Assert.That(shadow.BetaClones, Is.EqualTo(source.BetaClones));
        Assert.That(shadow.BetaOpticalClone, Is.EqualTo(source.BetaOpticalClone));
        Assert.That(shadow.BetaDonutClone, Is.EqualTo(source.BetaDonutClone));
        Assert.That(shadow.JudgmentOrders.SelectMany(o => o), Is.EqualTo(source.JudgmentOrders.SelectMany(o => o)));
    }

    [Test]
    public void MalformedReplayStateIsRefused()
    {
        PartyRole[] all = [MainTank, OffTank, RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
        TeaP4PerfectAlexanderState? Replay(PartyRole darkBeacon = OffTank, PartyRole[]? lights = null, PartyRole[]? alpha = null,
            int safe = 1, int optical = 2, int donut = 0, int[][]? orders = null)
            => TeaP4PerfectAlexanderState.FromNetworkReplay(P4Practice.WholePhase, MainTank, darkBeacon, lights ?? [RegenHealer, ShieldHealer, MeleeDpsA],
                true, true, [MainTank, CasterDps], alpha ?? all, false, true, safe, all, true, optical, donut, orders ?? [[0, 1, 2], [2, 1, 0]], [MainTank, OffTank]);

        Assert.That(Replay(), Is.Not.Null);
        Assert.That(Replay(darkBeacon: MainTank), Is.Null);
        Assert.That(Replay(lights: [MainTank, ShieldHealer, MeleeDpsA]), Is.Null);
        Assert.That(Replay(alpha: [MainTank, MainTank, RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps]), Is.Null);
        Assert.That(Replay(safe: 3), Is.Null);
        Assert.That(Replay(donut: 2), Is.Null);
        Assert.That(Replay(optical: 1, donut: 2), Is.Not.Null);
        Assert.That(Replay(optical: 1, donut: 1), Is.Null);
        Assert.That(Replay(optical: 3), Is.Null);
        Assert.That(Replay(orders: [[0, 1, 1], [2, 1, 0]]), Is.Null);
    }

    // Real projections walk different paths pull to pull, the Defamation one's included.
    [Test]
    public void AlphaProjectionsWalkPathsDealtPerRun()
    {
        var states = Enumerable.Range(0, 40)
            .Select(seed => new TeaP4PerfectAlexanderState(new Rng(seed), SimParty.Empty, new TeaP4PerfectAlexanderStateOverrides()))
            .ToList();
        Assert.That(states.All(s => s.AlphaPathOrder.Order().SequenceEqual(Enumerable.Range(0, 8))), "each run deals all eight paths once");
        Assert.That(states.Select(s => s.AlphaPathOrder[6]).Distinct().Count(), Is.GreaterThan(4), "the Defamation projection's path");
    }

    private static TeaP4PerfectAlexanderState StateWith(Action<TeaP4PerfectAlexanderStateOverrides> tweak)
    {
        var overrides = new TeaP4PerfectAlexanderStateOverrides();
        tweak(overrides);
        return new TeaP4PerfectAlexanderState(new Rng(11), SimParty.Empty, overrides);
    }
}
