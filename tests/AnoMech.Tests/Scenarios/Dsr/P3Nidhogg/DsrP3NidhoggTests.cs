#pragma warning disable CA1416
using AnoMech.Core.Game.Ai;
using AnoMech.Scenarios;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P3Nidhogg;

namespace AnoMech.Tests;

public class DsrP3NidhoggTests
{
    [Test]
    public void HumanMainTankDoesNotPullBotsIntoItsCleave()
    {
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 4, new()
        {
            PlayerRole = PartyRole.MainTank, StopAt = 73f,
            Takeovers = [new(69.4f, new System.Numerics.Vector2(-6, -6))],
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void LanceTargetsRemainVariedWithinEveryPreparationPattern()
    {
        var states = Enumerable.Range(0, 128).Select(seed => new DsrP3NidhoggState(new Rng(seed), new(), null)).ToArray();
        foreach (var pattern in states.GroupBy(s => s.PreparationPattern))
            Assert.That(pattern.Select(s => s.DrachenlanceTarget).Distinct().Count(), Is.EqualTo(8));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void TankBaitsIndependentlyAndPartyPreparesForRandomLance(int seed)
    {
        var checks = 0;
        var scenario = new DsrP3NidhoggScenario();
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, seed, new()
        {
            ScenarioFactory = () => scenario,
            Probe = p =>
            {
                if (p.Crossed(69.65f))
                {
                    checks++;
                    Assert.That(p.Member(PartyRole.MainTank)!.Position.X, Is.LessThan(-5));
                    foreach (var role in Enum.GetValues<PartyRole>().Where(r => r != PartyRole.MainTank))
                    {
                        Assert.That(Math.Abs(p.Member(role)!.Position.X), Is.LessThan(1));
                        Assert.That(p.Member(role)!.Position.Z, Is.LessThan(-4));
                    }
                }
                if (p.Crossed(74.02f))
                {
                    checks++;
                    Assert.That(Enum.GetValues<PartyRole>().Count(r => p.Member(r)!.Position.Z > 0), Is.GreaterThanOrEqualTo(5));
                }
                if (p.Crossed(96.3f))
                {
                    checks++;
                    Assert.That(p.Member(PartyRole.MainTank)!.Position.Z, Is.LessThan(-5));
                    foreach (var role in Enum.GetValues<PartyRole>().Where(r => r != PartyRole.MainTank))
                        Assert.That(p.Member(role)!.Position.Z, Is.GreaterThan(5));
                    var boss = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Single(e => e.BNpcBaseId == 12605);
                    Assert.That(Math.Cos(boss.Rotation), Is.LessThan(-.99));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(3));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void EasthoggClears(int seed)
    {
        var scenario = new DsrP3NidhoggScenario();
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, seed, new() { ScenarioFactory = () => scenario });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Completed, Is.True);
    }
    private static IEnumerable<TestCaseData> Seats() =>
        from role in Enum.GetValues<PartyRole>()
        from difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard }
        select new TestCaseData(role, difficulty);

    [TestCaseSource(nameof(Seats))]
    public void EverySeatClearsAndElementalTanksOwnTheirTethers(PartyRole role, BotDifficulty difficulty)
    {
        var checkedTethers = false;
        FakeGame? native = null;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 3100 + (int)role * 19, new()
        {
            PlayerRole = role, BotDifficulty = difficulty,
            ConfigureNative = f => { native = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
            Probe = p =>
            {
                if (!p.Crossed(90.9f)) return;
                checkedTethers = true;
                foreach (var tether in p.World.Children.OfType<AnoMech.Core.SimObjects.SimTether>().Where(t => t.IsActive))
                {
                    var bossTether = tether.A!.Position.LengthSquared() < 1;
                    Assert.That(tether.B, Is.SameAs(p.Member(bossTether ? PartyRole.MainTank : PartyRole.OffTank)), $"source={tether.A.Position}, holder={((AnoMech.Core.SimObjects.ISimPartyMember)tether.B!).Role} at {tether.B.Position}; MT={p.Member(PartyRole.MainTank)!.Position} ST={p.Member(PartyRole.OffTank)!.Position}");
                    Assert.That(tether.B!.Position.Length(), Is.LessThan(4));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedTethers, Is.True);
        Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
    }

    [Test]
    public void PracticeOverridesKeepNumbersAndArrowsValidForEverySeat()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        foreach (var line in new[] { 1, 2, 3 })
        foreach (var arrow in Enum.GetValues<DiveArrow>())
        {
            var state = new DsrP3NidhoggState(new Rng((int)role + line), new() { PlayerLine = line, PlayerArrow = arrow }, role);
            Assert.That(state.Lines[role], Is.EqualTo(line));
            Assert.That(state.Arrows[role], Is.EqualTo(arrow));
            Assert.That(state.Lines.Values.GroupBy(n => n).OrderBy(g => g.Key).Select(g => g.Count()), Is.EqualTo(new[] { 3, 2, 3 }));
            Assert.That(Enumerable.Range(1, 3).Count(state.LineHasArrows), Is.InRange(1, 2));
            Assert.That(state.TowerSoakers.Sum(), Is.EqualTo(8));
        }
    }

    [TestCase(0f)] [TestCase(36f)] [TestCase(43f)] [TestCase(84f)] [TestCase(90f)] [TestCase(128f)]
    public void RestartAndLeaveReleaseActorsTethersAndStatuses(float stop)
    {
        var scenario = new DsrP3NidhoggScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame? native = null;
            var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, repeat, new()
            {
                ScenarioFactory = () => scenario, StopAt = stop,
                ConfigureNative = f => { native = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
        }
    }

    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.OffTank)]
    public void HumanTankMustUseInvulnerabilityForSoulTethers(PartyRole role)
    {
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            PlayerRole = role, UserActionsEnabled = true,
            Takeovers = [new(90.85f, null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == role && d.ActionId == 26396), Is.True, run.ToString);
    }

    [Test]
    public void MissingDiveTowerWipesTheParty()
    {
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank,
            Overrides = o => ((DsrP3NidhoggStateOverrides)o).PlayerLine = 3,
            Takeovers = [new(41.5f, new System.Numerics.Vector2(0, -10))],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == 26395), Is.True, run.ToString);
    }
    [Test]
    public void BoundedRandomizedEasthoggClears()
    {
        for (var seed = 0; seed < 32; seed++)
        foreach (var difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard })
        {
            var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 4000 + seed, new()
            {
                BotDifficulty = difficulty,
                PlayerRole = (PartyRole)(seed % 8),
            });
            Assert.That(run.Passed, run.ToString);
        }
    }
    [Test]
    public void NativeOpeningWaitsForEngagementAndKeepsWaitingDiversHidden()
    {
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            StopAt = 12f,
            ConfigureNative = f => f.BattleCharas.PacketSpawnDelayFrames = 5,
            Probe = p =>
            {
                if (p.Crossed(1f))
                {
                    checks++;
                    var enemies = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().ToArray();
                    var boss = enemies.Single(e => e.BNpcBaseId == 12605);
                    Assert.That(boss.Proxy!.TargetableStatus & 3, Is.Zero);
                    var divers = enemies.Where(e => e.BNpcBaseId == 12606).ToArray();
                    Assert.That(divers.Length, Is.EqualTo(8));
                    Assert.That(divers.All(e => !((FakeBattleChara)e.Proxy!).IsDrawObjectVisible), Is.True);
                }
                if (p.Crossed(11f))
                {
                    checks++;
                    var boss = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Single(e => e.BNpcBaseId == 12605);
                    Assert.That(boss.Proxy!.TargetableStatus & 3, Is.Not.Zero);
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(2));
    }
    [Test]
    public void EasthoggForwardArrowClaimsAndSoaksWest()
    {
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, StopAt = 44f,
            Overrides = o =>
            {
                var settings = (DsrP3NidhoggStateOverrides)o;
                settings.PlayerLine = 3; settings.PlayerArrow = DiveArrow.Up;
            },
            Probe = p =>
            {
                if (p.Crossed(29f))
                {
                    checks++;
                    Assert.That(p.Member(PartyRole.MainTank)!.Position.X, Is.LessThan(0));
                }
                if (p.Crossed(43.6f))
                {
                    checks++;
                    Assert.That(p.Member(PartyRole.MainTank)!.Position.X, Is.LessThan(0));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(2));
    }

    [TestCase(false)] [TestCase(true)]
    public void DiveEffectsPreserveCapturedTargetPoseAndNativeTowerEmitter(bool visible)
    {
        var scenario = new DsrP3NidhoggScenario();
        var checks = 0;
        var poses = new Dictionary<FFXIVClientStructs.FFXIV.Client.Game.Object.GameObjectId, (float Rotation, System.Numerics.Vector3 Position)>();
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, ShowSimulatedParty = visible, StopAt = 70,
            ConfigureNative = f => f.BattleCharas.PacketSpawnDelayFrames = 5,
            Probe = p =>
            {
                foreach (var (at, line) in new[] { (37.25f, 1), (47.24f, 2), (58.26f, 3) })
                    if (p.Crossed(at))
                        foreach (var role in scenario.LastState!.Line(line))
                        {
                            var member = p.Member(role)!;
                            poses[member.GameObjectId] = (member.Rotation, member.Position);
                        }
                if (!p.Crossed(69)) return;
                var divers = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Where(e => e.BNpcBaseId == 12606).ToArray();
                foreach (var diver in divers)
                {
                    var effects = ((FakeBattleChara)diver.Proxy!).ReceivedEffects;
                    foreach (var effect in effects.Where(e => e.ActionId is >= 26382 and <= 26384))
                    {
                        checks++;
                        var pose = poses[effect.AnimationTarget!.Value];
                        Assert.That(effect.Rotation, Is.EqualTo(pose.Rotation).Within(.01f));
                        Assert.That(effect.AnimationLock, Is.EqualTo(1.1f));
                        Assert.That(effect.Targets!.Any(t => t.Id == effect.AnimationTarget), Is.True);
                    }
                    foreach (var effect in effects.Where(e => e.ActionId == 26385))
                    {
                        checks++;
                        Assert.That(effect.AnimationTarget, Is.EqualTo(diver.GameObjectId));
                        Assert.That(effect.AnimationLock, Is.EqualTo(1.1f));
                    }
                }
            },
        });
        Assert.That(run.Passed, run.ToString); Assert.That(checks, Is.EqualTo(16));
    }
    [Test]
    public void EveryCloneLineUsesRecordedLateOmenAndOriginalReleaseTime()
    {
        var checks = 0;
        var directions = new Dictionary<uint, float>();
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            StopAt = 92,
            ConfigureNative = f => f.BattleCharas.PacketSpawnDelayFrames = 5,
            Probe = p =>
            {
                foreach (var start in new[] { 46.51f, 56.50f, 67.48f, 86.68f })
                {
                    if (p.Crossed(start + .05f))
                    {
                        directions.Clear();
                        foreach (var clone in p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>()
                            .Where(e => e.Casting.ActionId == 26378 && e.Casting.IsBusy))
                        {
                            var native = (FakeBattleChara)clone.Proxy!;
                            var cast = native.ReceivedCasts.Last();
                            Assert.That(cast.CastTime, Is.EqualTo(4.2f).Within(.001));
                            Assert.That(cast.OmenDelay, Is.EqualTo(3.5f));
                            var bait = p.World.Party.ActiveMembers().Where(m => !m.Dead)
                                .MinBy(m => System.Numerics.Vector3.DistanceSquared(m.Position, clone.Position))!;
                            Assert.That(cast.Rotation, Is.EqualTo(clone.Placement().Face(bait.Position).Rotation).Within(.05));
                            directions.Add(clone.EntityId, cast.Rotation);
                            checks++;
                        }
                        Assert.That(directions.Count, Is.InRange(2, 3));
                    }
                    if (p.Crossed(start + 4.1f))
                        foreach (var clone in p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>()
                            .Where(e => directions.ContainsKey(e.EntityId)))
                            Assert.That(((FakeBattleChara)clone.Proxy!).ReceivedEffects.Count(e => e.ActionId == 26378), Is.Zero);
                    if (p.Crossed(start + 4.6f))
                        foreach (var clone in p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>()
                            .Where(e => directions.ContainsKey(e.EntityId)))
                        {
                            var effect = ((FakeBattleChara)clone.Proxy!).ReceivedEffects.Single(e => e.ActionId == 26378);
                            Assert.That(effect.Rotation, Is.EqualTo(directions[clone.EntityId]).Within(.001));
                        }
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(11));
        var preloads = ((FakeActionTimelinePreload)AnoMech.Core.Native.Interfaces.Natives.TimelinePreload).Rows;
        Assert.That(preloads, Is.Empty);
    }

    [TestCase(1, DiveArrow.Circle)] [TestCase(1, DiveArrow.Up)] [TestCase(1, DiveArrow.Down)]
    [TestCase(2, DiveArrow.Circle)] [TestCase(2, DiveArrow.Up)] [TestCase(2, DiveArrow.Down)]
    [TestCase(3, DiveArrow.Circle)] [TestCase(3, DiveArrow.Up)] [TestCase(3, DiveArrow.Down)]
    public void LandingPresentationFinishesBeforeCloneIsHidden(int line, DiveArrow arrow)
    {
        var scenario = new DsrP3NidhoggScenario();
        var at = new[] { 37.28f, 47.27f, 58.29f }[line - 1];
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, StopAt = at + 1.3f, PlayerRole = PartyRole.MainTank,
            ConfigureNative = f =>
            {
                f.BattleCharas.PacketSpawnDelayFrames = 5;
                f.BattleCharas.NpcDrawObjectDelayFrames = 12;
            },
            Overrides = o =>
            {
                var settings = (DsrP3NidhoggStateOverrides)o;
                settings.PlayerLine = line; settings.PlayerArrow = arrow;
            },
            Probe = p =>
            {
                foreach (var (offset, hidden) in new[] { (.8f, false), (1.2f, true) })
                {
                    if (!p.Crossed(at + offset)) continue;
                    var clones = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>()
                        .Where(e => e.Casting.EffectActionId is >= 26382 and <= 26384).ToArray();
                    Assert.That(clones.Length, Is.EqualTo(scenario.LastState!.Line(line).Count()));
                    foreach (var clone in clones)
                    {
                        var native = (FakeBattleChara)clone.Proxy!;
                        Assert.That(native.IsDrawObjectVisible, Is.EqualTo(!hidden));
                        Assert.That(native.IsAnimationReady, Is.True);
                        Assert.That(native.ModelHidden, Is.False);
                        Assert.That(native.UnrenderableActions.Where(id => id is >= 26382 and <= 26384), Is.Empty);
                        Assert.That(native.ReceivedEffects.Count(e => e.ActionId is >= 26382 and <= 26384), Is.EqualTo(1));
                    }
                    var playerEffect = clones.SelectMany(c => ((FakeBattleChara)c.Proxy!).ReceivedEffects)
                        .Single(e => e.AnimationTarget == p.Member(PartyRole.MainTank)!.GameObjectId);
                    Assert.That(playerEffect.ActionId, Is.EqualTo(arrow switch
                    {
                        DiveArrow.Up => 26383u,
                        DiveArrow.Down => 26384u,
                        _ => 26382u,
                    }));
                    checks++;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(2));
    }

    [TestCase(DiveArrow.Circle)] [TestCase(DiveArrow.Up)] [TestCase(DiveArrow.Down)]
    public void DivePlacementUsesPoseAtLandingAndIgnoresSubsequentMovement(DiveArrow arrow)
    {
        var scenario = new DsrP3NidhoggScenario();
        var checkedPlacement = false;
        var lockedPosition = new System.Numerics.Vector3(-10, 0, -5);
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, StopAt = 37.6f, PlayerRole = PartyRole.MainTank,
            Takeovers = [new(37.2f, null)], WriteArtifactsOnFailure = false,
            Overrides = o =>
            {
                var settings = (DsrP3NidhoggStateOverrides)o;
                settings.PlayerLine = 1; settings.PlayerArrow = arrow;
            },
            Probe = p =>
            {
                if (p.Crossed(37.25f)) p.Member(PartyRole.MainTank)!.SetPosition(new AnoMech.Core.Game.Placement(lockedPosition, MathF.PI / 2));
                if (!p.Crossed(37.4f)) return;
                p.Member(PartyRole.MainTank)!.SetPosition(new AnoMech.Core.Game.Placement(new(0, 0, 10), 0));
                var tower = scenario.LastState!.DiveTowers[0].Single(t => t.Role == PartyRole.MainTank);
                Assert.That(tower.Spot.X, Is.EqualTo(lockedPosition.X + DsrP3NidhoggState.DiveOffset(arrow)).Within(.01));
                Assert.That(tower.Spot.Z, Is.EqualTo(lockedPosition.Z).Within(.01));
                checkedPlacement = true;
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(checkedPlacement, Is.True);
    }

    [Test]
    public void LockedGeirskogulStillHitsAtOriginalSnapshot()
    {
        var moved = false;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            StopAt = 51, PlayerRole = PartyRole.MainTank,
            Overrides = o => ((DsrP3NidhoggStateOverrides)o).PlayerLine = 1,
            Takeovers = [new(46.7f, null)], WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (!p.Crossed(47)) return;
                var clone = p.World.Children.OfType<SimEnemy>()
                    .First(e => e.Casting.ActionId == 26378 && e.Casting.IsBusy);
                p.Member(PartyRole.MainTank)!.SetPosition(clone.Placement().MoveForward(2));
                moved = true;
            },
        });
        Assert.That(moved, Is.True);
        var hit = run.Deaths.Single(d => d.Role == PartyRole.MainTank && d.ActionId == 26378);
        Assert.That(hit.Time, Is.EqualTo(46.51f + 4.2f).Within(.04));
    }

    [TestCase(false)] [TestCase(true)]
    public void ColdRendererWaitsWithoutMovingThePlacementSnapshotAndCancelsOnLeave(bool leaveEarly)
    {
        var scenario = new DsrP3NidhoggScenario();
        SimEnemy? clone = null;
        FakeCharacter? nativeActor = null;
        FakeGame? client = null;
        System.Numerics.Vector3 lockedPosition = default;
        var checkedSnapshot = false;
        var checkedDispatch = false;
        var run = ScenarioRun.Execute(typeof(DsrP3NidhoggScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, PlayerRole = PartyRole.MainTank,
            StopAt = leaveEarly ? 37.4f : 38f,
            ConfigureNative = f => client = f,
            Takeovers = [new(37.2f, null)],
            Overrides = o => ((DsrP3NidhoggStateOverrides)o).PlayerLine = 1,
            Probe = p =>
            {
                if (p.Crossed(37.2f))
                {
                    foreach (var actor in p.World.Children.OfType<SimEnemy>().Where(e => e.BNpcBaseId == 12606))
                        client!.BattleCharas.Find(actor.EntityId)!.HasLoadedModel = false;
                    lockedPosition = p.Member(PartyRole.MainTank)!.Position;
                }
                if (p.Crossed(37.35f))
                {
                    clone = p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == 12606
                        && System.Numerics.Vector3.DistanceSquared(e.Position, lockedPosition) < .01f);
                    var native = (FakeBattleChara)clone.Proxy!;
                    Assert.That(native.ReceivedEffects, Is.Empty);
                    Assert.That(scenario.LastState!.DiveTowers[0], Is.Not.Empty);
                    Assert.That(p.Member(PartyRole.MainTank)!.HasStatus(DsrP3NidhoggActions.FireResistanceDown), Is.True);
                    nativeActor = client!.BattleCharas.Find(clone.EntityId);
                    var beforePause = p.Time;
                    p.Game.Paused = true;
                    client.Frame(.6f, p.Game.Tick);
                    Assert.That(p.Time, Is.EqualTo(beforePause));
                    Assert.That(native.ReceivedEffects, Is.Empty);
                    p.Game.Paused = false;
                    checkedSnapshot = true;
                }
                if (p.Crossed(37.5f))
                {
                    foreach (var actor in p.World.Children.OfType<SimEnemy>().Where(e => e.BNpcBaseId == 12606))
                        client!.BattleCharas.Find(actor.EntityId)!.HasLoadedModel = true;
                }
                if (p.Crossed(37.7f))
                {
                    var native = (FakeBattleChara)clone!.Proxy!;
                    var effect = native.ReceivedEffects.Single();
                    Assert.That(effect.ActionId, Is.InRange(26382u, 26384u));
                    Assert.That(effect.AnimationTarget, Is.EqualTo(p.Member(PartyRole.MainTank)!.GameObjectId));
                    Assert.That(effect.Position, Is.EqualTo(p.World.Coordinates.ToGlobal(lockedPosition)));
                    Assert.That(native.UnrenderableActions, Is.Empty);
                    checkedDispatch = true;
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedSnapshot, Is.True);
        Assert.That(checkedDispatch, Is.EqualTo(!leaveEarly));
        Assert.That(nativeActor!.ActionEffects.Count, Is.EqualTo(leaveEarly ? 0 : 1));
    }

}
