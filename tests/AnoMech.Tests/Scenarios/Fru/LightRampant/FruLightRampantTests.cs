using AnoMech.Scenarios;
using static AnoMech.Core.Game.Party.PartyRole;
using AnoMech.Scenarios.Fru.LightRampant;
using AnoMech.Core.SimObjects;

namespace AnoMech.Tests;

public class FruLightRampantTests
{
    private static IEnumerable<TestCaseData> Variants()
        => from pair in Enumerable.Range(0, 28)
           from spread in new[] { false, true }
           from north in new[] { false, true }
           select new TestCaseData(pair, spread, north);

    [TestCaseSource(nameof(Variants))]
    public void ManaClears(int pair, bool spread, bool north)
    {
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, pair, new ScenarioRunOptions
        {
            Overrides = settings =>
            {
                var state = (FruLightRampantStateOverrides)settings;
                state.PuddlePair = pair;
                state.Spread = spread;
                state.NorthOrbFirst = north;
                state.WeightEdge = pair % 6;
            },
            Probe = p =>
            {
                var state = ((FruLightRampantScenario)p.Game.ActiveScenario!).LastState!;
                if (p.Crossed(19.1f))
                {
                    AnoMech.Core.Game.Party.PartyRole[] priority = [RegenHealer, ShieldHealer, MainTank, OffTank, CasterDps, PhysRangedDps, MeleeDpsB, MeleeDpsA];
                    var chained = priority.Where(r => !state.PuddleTargets.Contains(r)).ToArray();
                    for (var i = 0; i < chained.Length; i++)
                        Assert.That(System.Numerics.Vector3.Distance(p.Member(chained[i])!.Position, FruLightRampantScenario.OuterTowers[i]), Is.LessThan(chained[i] == p.World.Party.PlayerRole ? 6 * ScenarioRun.FrameSeconds + .001f : .05f));
                }
                if (p.Crossed(24.85f))
                    foreach (var target in state.WeightTargets)
                        Assert.That(p.World.Party.Find.InsideCircle(p.Member(target)!.Position, 5f).Count, Is.EqualTo(4));
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void PuddleLockonsAreNotOwnedForLaterNativeDestruction()
    {
        var inspected = false;
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            StopAt = 9f,
            Probe = p =>
            {
                if (!p.Crossed(8.9f)) return;
                inspected = true;
                var state = ((FruLightRampantScenario)p.Game.ActiveScenario!).LastState!;
                var icon = AnoMech.Core.Native.Interfaces.Natives.Vfx.LockonIconName(375);
                foreach (var role in state.PuddleTargets)
                {
                    Assert.That(p.Member(role)!.LastLockonVfxId, Is.EqualTo(375));
                    Assert.That(p.Member(role)!.FindVfx($"vfx/lockon/eff/{icon}.avfx"), Is.Null);
                }
            },
        });
        Assert.That(inspected, Is.True);
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void SeedReproducesAssignments()
    {
        var a = new FruLightRampantState(new Rng(123), new());
        var b = new FruLightRampantState(new Rng(123), new());
        Assert.Multiple(() =>
        {
            Assert.That(b.PuddleTargets, Is.EqualTo(a.PuddleTargets));
            Assert.That(b.InitialLightsteeped, Is.EqualTo(a.InitialLightsteeped));
            Assert.That(b.WeightEdge, Is.EqualTo(a.WeightEdge));
            Assert.That(b.NorthOrbFirst, Is.EqualTo(a.NorthOrbFirst));
            Assert.That(b.Spread, Is.EqualTo(a.Spread));
        });
    }

    [Test]
    public void InitialStacksCoverEveryFourRecipientCombination()
    {
        foreach (var mask in Enumerable.Range(0, 256).Where(m => System.Numerics.BitOperations.PopCount((uint)m) == 4))
        foreach (var pair in Enumerable.Range(0, 28))
        {
            var state = new FruLightRampantState(new Rng(0), new() { PuddlePair = pair, ExtraLightsteepedMask = mask });
            Assert.That(state.InitialLightsteeped.Sum(), Is.EqualTo(6));
            Assert.That(state.InitialLightsteeped, Is.All.InRange(0, 2));
            Assert.That(state.PuddleTargets.Distinct().Count(), Is.EqualTo(2));
        }
    }

    [TestCase(24.99f, true)]
    [TestCase(25f, false)]
    [TestCase(36f, false)]
    public void ChainDistanceRuleIsIsolated(float distance, bool fails)
        => Assert.That(FruLightRampantRules.ChainFails(default, new(distance, 100f, 0f), true, true), Is.EqualTo(fails));

    [Test]
    public void DeadChainEndpointFails()
        => Assert.That(FruLightRampantRules.ChainFails(default, new(36f, 0f, 0f), false, true), Is.True);

    [TestCase(1, false, false)]
    [TestCase(2, true, false)]
    [TestCase(3, false, false)]
    [TestCase(4, false, false)]
    [TestCase(5, false, true)]
    public void LightsteepedRules(int stacks, bool center, bool overload)
    {
        Assert.That(FruLightRampantRules.MustSoakCenter(stacks), Is.EqualTo(center));
        Assert.That(FruLightRampantRules.LightsteepedOverloads(stacks), Is.EqualTo(overload));
    }

    [Test]
    public void FullSequenceBalancesStacksAndCleansUp()
    {
        var checkpoints = 0;
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 17, new ScenarioRunOptions
        {
            Probe = p =>
            {
                int[] Stacks() => Enumerable.Range(0, 8).Select(i => (int)(p.World.Party.Get(i)!.FindStatus(AnoMech.Scenarios.Fru.FruConstants.StatusId.Lightsteeped)?.Stacks ?? 0)).ToArray();
                if (p.Crossed(9f)) { Assert.That(Stacks().Sum(), Is.EqualTo(6)); checkpoints++; }
                if (p.Crossed(25.1f))
                {
                    Assert.That(Stacks().Order(), Is.EqualTo(new[] { 2, 2, 2, 2, 3, 3, 3, 3 }));
                    Assert.That(p.Game.HasScenarioMistake, Is.False);
                    checkpoints++;
                }
                if (p.Crossed(34f)) { Assert.That(Stacks(), Is.All.EqualTo(3)); checkpoints++; }
                if (p.Crossed(46.2f)) { Assert.That(Stacks(), Is.All.EqualTo(4)); checkpoints++; }
                if (p.Crossed(49f))
                {
                    Assert.That(Stacks(), Is.All.EqualTo(0));
                    Assert.That(p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Any(e => e.IsActive), Is.False);
                    Assert.That(p.World.Children.OfType<AnoMech.Core.SimObjects.SimTether>().Any(e => e.IsActive), Is.False);
                    Assert.That(p.World.Children.OfType<AnoMech.Core.SimObjects.SimEventObject>().Any(e => e.IsActive), Is.False);
                    Assert.That(((FruLightRampantScenario)p.Game.ActiveScenario!).Completed, Is.True);
                    checkpoints++;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkpoints, Is.EqualTo(5));
    }

    [Test]
    public void ActiveShortChainWipes()
    {
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            Overrides = o => ((FruLightRampantStateOverrides)o).PuddlePair = 0,
            PlayerRole = AnoMech.Core.Game.Party.PartyRole.RegenHealer,
            Takeovers = [new(19.3f, new(0f, 0f))],
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == AnoMech.Scenarios.Fru.FruConstants.ActionId.RefulgentFate), Is.True, run.ToString);
    }

    [Test]
    public void StayingInPuddleFails()
    {
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            Overrides = o => ((FruLightRampantStateOverrides)o).PuddlePair = 0,
            PlayerRole = AnoMech.Core.Game.Party.PartyRole.MainTank,
            Takeovers = [new(15.9f, null)],
        });
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("persistent puddle")), Is.True, run.ToString);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PuddleIsVisibleBeforeItBecomesDangerous(bool escape)
    {
        var graceChecked = false;
        var takeovers = new List<Takeover> { new(15.9f, null) };
        if (escape) takeovers.Add(new(17.2f, new(19f, 0f)));
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            Overrides = o => ((FruLightRampantStateOverrides)o).PuddlePair = 0,
            PlayerRole = AnoMech.Core.Game.Party.PartyRole.MainTank,
            StopAt = 17.43f,
            Takeovers = takeovers,
            Probe = p =>
            {
                if (!p.Crossed(16.9f)) return;
                Assert.That(p.World.Party.Player!.IsAlive(), Is.True);
                Assert.That(p.World.Children.OfType<SimEventObject>(), Is.Not.Empty);
                graceChecked = true;
            }
        });
        Assert.That(graceChecked, Is.True);
        if (escape) Assert.That(run.Passed, run.ToString);
        else
        {
            var death = run.Deaths.Single(d => d.Role == AnoMech.Core.Game.Party.PartyRole.MainTank);
            Assert.That(death.Cause, Does.Contain("persistent puddle"));
            Assert.That(death.Time, Is.InRange(17.38f, 17.43f));
        }
    }

    private static IEnumerable<int> InitialMasks() => Enumerable.Range(0, 256).Where(m => System.Numerics.BitOperations.PopCount((uint)m) == 4);

    [TestCaseSource(nameof(InitialMasks))]
    public void EveryInitialStackVariationClears(int mask)
    {
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, mask, new ScenarioRunOptions
        {
            Overrides = o => ((FruLightRampantStateOverrides)o).ExtraLightsteepedMask = mask,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
    public void EveryWeightEdgeClears(int edge)
    {
        foreach (var north in new[] { false, true })
        {
            var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, edge, new ScenarioRunOptions
            {
                Overrides = o =>
                {
                    var settings = (FruLightRampantStateOverrides)o;
                    settings.WeightEdge = edge;
                    settings.NorthOrbFirst = north;
                },
            });
            Assert.That(run.Passed, run.ToString);
        }
    }

    [TestCase(19.05f, 19.3f, AnoMech.Core.Game.Party.PartyRole.RegenHealer)]
    [TestCase(24.7f, 25.2f, AnoMech.Core.Game.Party.PartyRole.MainTank)]
    public void MissedSoakRecordsNonlethalMistake(float takeover, float stop, AnoMech.Core.Game.Party.PartyRole role)
    {
        var mistake = false;
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            Overrides = o => ((FruLightRampantStateOverrides)o).PuddlePair = 0,
            PlayerRole = role,
            StopAt = stop,
            Takeovers = [new(takeover, takeover < 20f ? new(-19f, -3f) : new(19f, 0f))],
            Probe = p => mistake |= p.Game.HasScenarioMistake,
        });
        Assert.That(run.Deaths, Is.Empty, run.ToString);
        Assert.That(mistake, Is.True);
    }

    [TestCase(26.9f, 40219u)]
    [TestCase(36.9f, 40223u)]
    [TestCase(45.9f, 40188u)]
    public void OverlappingAoeFails(float takeover, uint action)
    {
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            Overrides = o =>
            {
                var settings = (FruLightRampantStateOverrides)o;
                settings.PuddlePair = 0;
                settings.Spread = true;
                settings.NorthOrbFirst = true;
            },
            PlayerRole = AnoMech.Core.Game.Party.PartyRole.MainTank,
            Takeovers = [new(takeover, takeover < 30f ? new(0f, -16f) : new(8f, 0f))],
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == action), Is.True, run.ToString);
    }

    [Test]
    public void SameSeedCanRunAgainAfterCleanup()
    {
        var a = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 99);
        var b = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 99);
        Assert.That(a.Passed && b.Passed, Is.True, $"{a}\n{b}");
        Assert.That(b.Elapsed, Is.EqualTo(a.Elapsed));
    }

    [Test]
    public void MissingCentralSoakerRecordsMistake()
    {
        var mistake = false;
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            StopAt = 34f,
            Probe = p =>
            {
                if (p.Crossed(33.7f))
                {
                    var member = Enumerable.Range(0, 8).Select(i => p.World.Party.Get(i)!)
                        .First(m => m.FindStatus(AnoMech.Scenarios.Fru.FruConstants.StatusId.Lightsteeped)?.Stacks == 2);
                    member.StopMoving();
                    member.SetPosition(new System.Numerics.Vector3(19f, 0f, 0f));
                }
                mistake |= p.Game.HasScenarioMistake;
            },
        });
        Assert.That(run.Deaths, Is.Empty, run.ToString);
        Assert.That(mistake, Is.True);
    }

    [Test]
    public void FifthLightsteepedStackWipes()
    {
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            Probe = p =>
            {
                if (p.Crossed(24.8f)) p.World.Party.Get(0)!.AddStatus(AnoMech.Scenarios.Fru.FruConstants.StatusId.Lightsteeped, 36f, 4, overrideStacks: true);
            },
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == AnoMech.Scenarios.Fru.FruConstants.ActionId.Lightsteep), Is.True, run.ToString);
    }

    [Test]
    public void BanishPairWithoutPartnerFails()
    {
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            Overrides = o =>
            {
                var settings = (FruLightRampantStateOverrides)o;
                settings.PuddlePair = 0;
                settings.Spread = false;
                settings.PairTargetsDps = false;
            },
            PlayerRole = AnoMech.Core.Game.Party.PartyRole.MainTank,
            Takeovers = [new(36.9f, new(0f, -16f))],
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == AnoMech.Scenarios.Fru.FruConstants.ActionId.BanishPairsHit), Is.True, run.ToString);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EitherBanishTargetGroupClears(bool dps)
    {
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 31, new ScenarioRunOptions
        {
            Overrides = o =>
            {
                var settings = (FruLightRampantStateOverrides)o;
                settings.Spread = false;
                settings.PairTargetsDps = dps;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void ScenarioInstanceCanRestartAfterCleanup()
    {
        var runs = 0;
        FruLightRampantState? first = null;
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 99, new ScenarioRunOptions
        {
            Probe = p =>
            {
                if (!p.Crossed(49f)) return;
                var scenario = (FruLightRampantScenario)p.Game.ActiveScenario!;
                Assert.That(scenario.Completed, Is.True);
                runs++;
                if (runs == 1)
                {
                    first = scenario.LastState;
                    p.Game.RunScenario(new LegacyRunScenarioParams(scenario, p.World.Party.PlayerRole, 0, 0, 99));
                }
                else
                {
                    Assert.That(scenario.LastState!.PuddleTargets, Is.EqualTo(first!.PuddleTargets));
                    Assert.That(scenario.LastState.InitialLightsteeped, Is.EqualTo(first.InitialLightsteeped));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(runs, Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VisibleOrbTriosSpawnCastResolveAndDespawnInOrder(bool northFirst)
    {
        var captured = new List<SimEnemy>();
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            Overrides = o => ((FruLightRampantStateOverrides)o).NorthOrbFirst = northFirst,
            Probe = p =>
            {
                var orbs = p.World.Children.OfType<SimEnemy>()
                    .Where(e => e.SpawnConfig.BNpcBaseId == AnoMech.Scenarios.Fru.FruConstants.BNpcBaseId.HolyLight).ToArray();
                if (p.Crossed(19.5f)) { Assert.That(orbs, Is.Empty); checks++; }
                if (p.Crossed(19.6f))
                {
                    Assert.That(orbs, Has.Length.EqualTo(3));
                    var expected = FruLightRampantScenario.OuterTowers.Where((_, i) => (i >= 3) == northFirst);
                    Assert.That(orbs.Select(e => e.Position), Is.EquivalentTo(expected));
                    captured.AddRange(orbs);
                    checks++;
                }
                if (p.Crossed(22.4f))
                {
                    Assert.That(orbs, Has.Length.EqualTo(3));
                    Assert.That(orbs.Select(e => e.Casting.ActionId), Is.All.EqualTo(AnoMech.Scenarios.Fru.FruConstants.ActionId.Burst));
                    checks++;
                }
                if (p.Crossed(22.5f))
                {
                    Assert.That(orbs, Has.Length.EqualTo(6));
                    Assert.That(orbs.Select(e => e.Position), Is.EquivalentTo(FruLightRampantScenario.OuterTowers));
                    Assert.That(orbs.Select(e => (e.SpawnConfig.Visibility == SpawnVisibility.Visible)), Is.All.True);
                    Assert.That(orbs.Select(e => e.SpawnConfig.NameId), Is.All.EqualTo(9318u));
                    Assert.That(orbs.Select(e => e.PacketSpawnFailed), Is.All.False);
                    Assert.That(orbs.Select(e => e.ModelHidden), Is.All.False);
                    Assert.That(orbs.Select(e => e.SpawnConfig.Targetable), Is.All.False);
                    captured.AddRange(orbs.Except(captured));
                    checks++;
                }
                if (p.Crossed(27.4f))
                {
                    Assert.That(captured.Take(3).Select(e => e.Casting.EffectActionId), Is.All.EqualTo(AnoMech.Scenarios.Fru.FruConstants.ActionId.Burst));
                    Assert.That(captured.Skip(3).Select(e => e.Casting.EffectSeq), Is.All.Zero);
                    checks++;
                }
                if (p.Crossed(29.1f))
                {
                    Assert.That(orbs, Has.Length.EqualTo(3));
                    Assert.That(captured.Take(3).Select(e => e.IsActive), Is.All.False);
                    checks++;
                }
                if (p.Crossed(30.4f))
                {
                    Assert.That(orbs.Select(e => e.Casting.EffectActionId), Is.All.EqualTo(AnoMech.Scenarios.Fru.FruConstants.ActionId.Burst));
                    checks++;
                }
                if (p.Crossed(32.1f)) { Assert.That(orbs, Is.Empty); checks++; }
            }
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(8));
        Assert.That(captured.Select(e => e.IsActive), Is.All.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BotEnteringFirstPuddleObeysItsExpiration(bool expired)
    {
        System.Numerics.Vector3 firstPuddle = default;
        var checkedEntry = false;
        var entry = expired ? 26.8f : 26.6f;
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            PlayerRole = RegenHealer,
            Overrides = o => ((FruLightRampantStateOverrides)o).PuddlePair = 0,
            StopAt = entry + 0.06f,
            Probe = p =>
            {
                if (p.Crossed(15.9f)) firstPuddle = p.Member(OffTank)!.Position;
                if (!p.Crossed(entry)) return;
                Assert.That(p.Member(OffTank), Is.TypeOf<SimPartyNpc>());
                p.Member(OffTank)!.StopMoving();
                p.Member(OffTank)!.SetPosition(firstPuddle);
                checkedEntry = true;
            }
        });
        Assert.That(checkedEntry, Is.True);
        Assert.That(run.Deaths.Any(d => d.Role == OffTank && d.Cause.Contains("persistent puddle")), Is.EqualTo(!expired), run.ToString);
        if (expired) Assert.That(run.Passed, run.ToString);
    }

    [TestCase(20f)]
    [TestCase(23f)]
    public void LeavingWithOrbsPresentDisposesThem(float leaveAt)
    {
        SimEnemy[] captured = [];
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            StopAt = leaveAt,
            Probe = p => captured = p.World.Children.OfType<SimEnemy>()
                .Where(e => e.SpawnConfig.BNpcBaseId == AnoMech.Scenarios.Fru.FruConstants.BNpcBaseId.HolyLight).ToArray()
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(captured, Has.Length.EqualTo(leaveAt < 22.47f ? 3 : 6));
        Assert.That(captured.Select(e => e.IsActive), Is.All.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ManaRouteAvoidsLivePuddlesAndReachesCenterAtWalkingSpeed(bool northFirst)
    {
        float[] drops = [15.89f, 17.45f, 19.01f, 20.57f, 22.13f];
        var puddles = new List<(System.Numerics.Vector3 Position, float From, float Until)>();
        var previous = new Dictionary<AnoMech.Core.Game.Party.PartyRole, System.Numerics.Vector3>();
        var centerChecked = false;
        float? stackAt = null, centerAt = null, secondBurstAt = null;
        var run = ScenarioRun.Execute(typeof(FruLightRampantScenario), 0, 0, new ScenarioRunOptions
        {
            Overrides = o =>
            {
                var settings = (FruLightRampantStateOverrides)o;
                settings.PuddlePair = 0;
                settings.NorthOrbFirst = northFirst;
            },
            Probe = p =>
            {
                var state = ((FruLightRampantScenario)p.Game.ActiveScenario!).LastState!;
                var actors = p.World.Children.OfType<SimEnemy>().ToArray();
                if (stackAt is null && actors.Any(e => e.Casting.EffectActionId == AnoMech.Scenarios.Fru.FruConstants.ActionId.PowerfulLight)) stackAt = p.Time;
                if (centerAt is null && actors.Any(e => e.Position == System.Numerics.Vector3.Zero && e.Casting.EffectActionId == AnoMech.Scenarios.Fru.FruConstants.ActionId.BrightHunger)) centerAt = p.Time;
                if (secondBurstAt is null && actors.Any(e => e.SpawnConfig.BNpcBaseId == AnoMech.Scenarios.Fru.FruConstants.BNpcBaseId.HolyLight && FruLightRampantScenario.OuterTowers.Where((_, i) => (i >= 3) != northFirst).Contains(e.Position) && e.Casting.EffectActionId == AnoMech.Scenarios.Fru.FruConstants.ActionId.Burst)) secondBurstAt = p.Time;
                foreach (var drop in drops.Where(p.Crossed))
                    foreach (var role in state.PuddleTargets)
                        puddles.Add((p.Member(role)!.Position,
                            p.Time + FruLightRampantRules.PuddleHazardActivationDelay,
                            p.Time + FruLightRampantRules.PuddleSpawnDelay + FruLightRampantRules.PuddleLifetime));
                foreach (var role in Enum.GetValues<AnoMech.Core.Game.Party.PartyRole>().Where(r => (int)r < 8))
                {
                    var member = p.Member(role)!;
                    foreach (var puddle in puddles.Where(x => p.Time >= x.From && p.Time < x.Until))
                        Assert.That(System.Numerics.Vector3.Distance(member.Position, puddle.Position),
                            Is.GreaterThan(FruLightRampantRules.PuddleRadius), $"{role} at {p.Time}");
                    if (p.Time > 30.5f && p.Time < 33.8f && FruLightRampantRules.MustSoakCenter(member.FindStatus(AnoMech.Scenarios.Fru.FruConstants.StatusId.Lightsteeped)?.Stacks ?? 0) && previous.TryGetValue(role, out var before))
                        Assert.That(System.Numerics.Vector3.Distance(before, member.Position), Is.LessThanOrEqualTo(6f / 60f + 0.002f));
                    previous[role] = member.Position;
                }
                if (p.Crossed(33.7f))
                {
                    var soakers = Enumerable.Range(0, 8).Select(p.World.Party.Get)
                        .Where(m => FruLightRampantRules.MustSoakCenter(m!.FindStatus(AnoMech.Scenarios.Fru.FruConstants.StatusId.Lightsteeped)!.Stacks)).ToArray();
                    Assert.That(soakers, Has.Length.EqualTo(4));
                    foreach (var member in soakers)
                        Assert.That(member!.Position.Length(), Is.LessThan(member is SimPlayer ? 6 * ScenarioRun.FrameSeconds + .001f : .05f));
                    centerChecked = true;
                }
            }
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(centerChecked, Is.True);
        Assert.That(stackAt, Is.Not.Null);
        Assert.That(centerAt, Is.Not.Null);
        Assert.That(secondBurstAt, Is.Not.Null);
        Assert.That(centerAt - stackAt, Is.InRange(8.805f, 9.08f));
        Assert.That(centerAt - secondBurstAt, Is.InRange(3.373f, 3.627f));
    }
}
