#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.FallOfFaith;

namespace AnoMech.Tests;

public class FruFallOfFaithTests
{
    [TestCase(0)] [TestCase(15)] [TestCase(5)] [TestCase(10)]
    public void CompleteFragmentUsesRealNearestBaitsAndVisibleSourceIdentity(int mask)
    {
        var scenario = new FruFallOfFaithScenario();
        var requests = new List<FaithCone>();
        var sources = new HashSet<uint>();
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 3, new()
        {
            ScenarioFactory = () => scenario,
            Overrides = o => { var s = (FruFallOfFaithStateOverrides)o; s.Advanced = true; s.FireMask = mask; s.TetherPermutation = 0; },
            Probe = p =>
            {
                foreach (var source in scenario.SourceActors)
                {
                    sources.Add(source.BNpcBaseId);
                    Assert.That((source.SpawnConfig.Visibility == SpawnVisibility.Visible), Is.True);
                    Assert.That(((FakeBattleChara)source.Proxy!).ModelHidden, Is.False);
                }
                foreach (var cone in scenario.Cones.Where(c => !requests.Contains(c)))
                {
                    var snapshots = p.World.Children.OfType<SimEnemy>()
                        .SelectMany(e => ((FakeBattleChara)e.Proxy!).EffectSourceSnapshots)
                        .Where(e => e.Effect.ActionId == (cone.Fire ? 40156u : 40143u)).ToArray();
                    Assert.That(snapshots.Any(s => Vector3.Distance(s.Position, cone.Origin + p.World.ScenarioOrigin) < .01f
                        && MathF.Abs(s.Effect.Rotation - cone.Rotation) < .001f), Is.True);
                    Assert.That(cone.Hits.Count, Is.EqualTo(cone.Fire ? 4 : 1));
                    requests.Add(cone);
                }
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Completed, Is.True);
        Assert.That(scenario.ResolvedCount, Is.EqualTo(4));
        Assert.That(scenario.Understacks, Is.Zero);
        Assert.That(requests.Count, Is.EqualTo(Enumerable.Range(0, 4).Sum(i => (mask & (1 << i)) != 0 ? 1 : 3)));
        Assert.That(sources, Is.EquivalentTo(new uint[] { 0x459B, 0x459C }));
    }

    [TestCase(0)] [TestCase(15)]
    public void NearbyFirstHolderKeepsBossInPlaceWhileAllClonesStillCharge(int mask)
    {
        var scenario = new FruFallOfFaithScenario();
        var checkedAfterFourth = false;
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario,
            Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.TetherPermutation = 0; x.FireMask = mask; },
            Probe = p =>
            {
                foreach (var boss in scenario.SourceActors.Where(s => s.BNpcBaseId == 0x459B))
                {
                    Assert.That(boss.Position, Is.EqualTo(Vector3.Zero));
                    Assert.That(((FakeBattleChara)boss.Proxy!).EffectSourceSnapshots.Any(e => e.Effect.ActionId is 40138 or 40141), Is.False);
                }
                if (!p.Crossed(24.6f)) return;
                checkedAfterFourth = true;
                Assert.That(scenario.SourceActors.Where(s => s.BNpcBaseId == 0x459C).Count(), Is.EqualTo(3));
                foreach (var clone in scenario.SourceActors.Where(s => s.BNpcBaseId == 0x459C))
                    Assert.That(((FakeBattleChara)clone.Proxy!).EffectSourceSnapshots.Any(e => e.Effect.ActionId is 40138 or 40141), Is.True);
                Assert.That(scenario.Cones.All(c => c.Hits.Count == (c.Fire ? 4 : 1)), Is.True);
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedAfterFourth && scenario.Completed, Is.True);
    }

    [TestCase(0)] [TestCase(15)]
    public void DistantFirstHolderStillCausesARealChargeWithoutMovingTheConeOrigin(int mask)
    {
        var scenario = new FruFallOfFaithScenario();
        var observed = false;
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, PlayerRole = PartyRole.MainTank, StopAt = 16.5f,
            Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.TetherPermutation = 0; x.FireMask = mask; },
            Takeovers = [new(12, new Vector2(-12, 0))],
            Probe = p =>
            {
                if (!p.Crossed(16.48f)) return;
                observed = true;
                var boss = scenario.SourceActors.Single(s => s.BNpcBaseId == 0x459B);
                Assert.That(Vector3.Distance(boss.Position, new(-12, 0, 0)), Is.LessThan(.01f));
                Assert.That(((FakeBattleChara)boss.Proxy!).EffectSourceSnapshots.Any(e => e.Effect.ActionId == (mask == 0 ? 40141u : 40138u)), Is.True);
                var cones = scenario.Cones.Where(c => c.Order == 0).ToArray();
                Assert.That(cones.Length, Is.EqualTo(mask == 0 ? 3 : 1));
                Assert.That(cones.All(c => c.Origin == new Vector3(-12, 0, 0)), Is.True);
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(observed, Is.True);
    }

    [Test]
    public void EveryPracticeSeatCanReceiveEachTetherOrRemainUntethered()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        foreach (var practice in Enum.GetValues<FaithPracticeAssignment>())
        foreach (var fire in new bool?[] { null, true, false })
        {
            var state = new FruFallOfFaithState(new Rng(17), new() { PracticeAssignment = practice, PlayerFire = fire }, role);
            Assert.That(state.Targets.Distinct().Count(), Is.EqualTo(4));
            if (practice == FaithPracticeAssignment.Untethered) Assert.That(state.Order(role), Is.EqualTo(-1));
            else if (practice != FaithPracticeAssignment.Random) Assert.That(state.Order(role), Is.EqualTo((int)practice - 1));
            if (state.Order(role) >= 0 && fire is { } requested) Assert.That(state.Fire(state.Order(role)), Is.EqualTo(requested));
        }
    }

    [Test]
    public void ManaPriorityFormsOppositeGroupsAndSwapsTheTetherHolders()
    {
        Assert.That(FruFallOfFaithState.Permutations.Count, Is.EqualTo(1680));
        foreach (var index in Enumerable.Range(0, 1680))
        {
            var state = new FruFallOfFaithState(new Rng(0), new() { Advanced = true, TetherPermutation = index }, PartyRole.MainTank);
            Assert.That(FruFallOfFaithAi.GroupRoles(state, 0).Length, Is.EqualTo(4));
            Assert.That(FruFallOfFaithAi.GroupRoles(state, 1).Length, Is.EqualTo(4));
            Assert.That(state.Targets.Select((r, i) => FruFallOfFaithAi.Side(state, r) == i % 2).All(x => x), Is.True);
            var remaining = FruFallOfFaithAi.Priority.Where(r => state.Order(r) < 0).ToArray();
            Assert.That(remaining.Select(r => FruFallOfFaithAi.Side(state, r)), Is.EqualTo(new[] { 0, 0, 1, 1 }));
            Assert.That(FruFallOfFaithAi.Position(state, state.Targets[0], 0), Is.EqualTo(FruFallOfFaithAi.Position(state, state.Targets[2], 2)));
        }
    }

    public static IEnumerable<TestCaseData> OrderedAssignments() => Enumerable.Range(0, 1680).Select(i => new TestCaseData(i));

    [TestCaseSource(nameof(OrderedAssignments))]
    public void AllOrderedAssignmentsClearAllSixteenElements(int permutation)
    {
        for (var mask = 0; mask < 16; mask++)
        {
            var scenario = new FruFallOfFaithScenario();
            var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, permutation, new()
            {
                ScenarioFactory = () => scenario,
                Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.TetherPermutation = permutation; x.FireMask = mask; },
                WriteArtifactsOnFailure = false,
            });
            Assert.That(run.Passed, $"permutation {permutation}, mask {mask}: {run}");
            Assert.That(scenario.Completed && scenario.ResolvedCount == 4 && scenario.Understacks == 0, Is.True);
        }
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void EveryHumanSeatAndElementUsesIdenticalPromptEasyHardMovement(int seat)
    {
        for (var mask = 0; mask < 16; mask++)
        {
            var paths = new Dictionary<BotDifficulty, string>();
            foreach (var difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Hard })
            {
                var observed = false;
                var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, mask * 43 + seat, new()
                {
                    PlayerRole = (PartyRole)seat, BotDifficulty = difficulty, ShowSimulatedParty = mask % 2 == 0,
                    Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.FireMask = mask; },
                    Probe = p =>
                    {
                        Assert.That(p.World.BotMovement.DelayedMoves, Is.Zero);
                        foreach (var d in p.World.BotMovement.Departures) Assert.That(d.Speed, Is.EqualTo(6));
                        if (!p.Crossed(25)) return;
                        paths[difficulty] = string.Join(";", p.World.BotMovement.Departures.Select(d => $"{((ISimPartyMember)d.Member).Role}:{d.RequestedAt:R}:{d.DepartAt:R}:{d.Destination}"));
                        Assert.That(p.World.Party.ActiveMembers().Count(), Is.EqualTo(8));
                        Assert.That(p.World.Party.ActiveMembers().OfType<SimPartyNpc>().Count(), Is.EqualTo(7));
                        observed = true;
                    }, WriteArtifactsOnFailure = false,
                });
                Assert.That(run.Passed, run.ToString);
                Assert.That(observed, Is.True);
            }
            Assert.That(paths[BotDifficulty.Easy], Is.EqualTo(paths[BotDifficulty.Hard]));
        }
    }

    [Test]
    public void RevealsAreStaggeredAndFettersPreventMovementUntilPromptSwap()
    {
        var scenario = new FruFallOfFaithScenario();
        var before = Vector3.Zero;
        var frozen = false; var swapped = false;
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario,
            Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.TetherPermutation = 0; x.FireMask = 0; },
            Probe = p =>
            {
                if (p.Crossed(2.9f)) Assert.That(scenario.RevealedCount, Is.Zero);
                if (p.Crossed(5.9f)) Assert.That(scenario.RevealedCount, Is.EqualTo(1));
                if (p.Crossed(8.4f)) Assert.That(scenario.RevealedCount, Is.EqualTo(2));
                if (p.Crossed(10.9f)) Assert.That(scenario.RevealedCount, Is.EqualTo(3));
                var first = p.World.Party.Get(scenario.LastState!.Targets[0])!;
                if (p.Crossed(12.8f))
                {
                    before = first.Position; Assert.That(first.MovementLocked, Is.True);
                    first.MoveTo(new Vector3(-15, 0, 0));
                }
                if (p.Crossed(16.6f)) { Assert.That(first.Position, Is.EqualTo(before)); frozen = true; }
                if (p.Crossed(17.3f)) { Assert.That(first.Position.X, Is.LessThan(before.X - 1.9f)); swapped = true; }
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(frozen && swapped, Is.True);
    }

    [TestCase(false)] [TestCase(true)]
    public void WrongNearestPlayerAndConeOverlapReallyFailUnlessGodMode(bool god)
    {
        var scenario = new FruFallOfFaithScenario();
        var diagnosed = false;
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, PlayerRole = PartyRole.RegenHealer, GodMode = god,
            Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.TetherPermutation = 0; x.FireMask = 0; },
            Takeovers = [new(12, new Vector2(FruFallOfFaithAi.Holder(0).X, .1f))],
            Probe = p => { diagnosed |= p.Game.HasScenarioMistake; }, WriteArtifactsOnFailure = false,
        });
        Assert.That(diagnosed, Is.True);
        if (god) { Assert.That(run.Deaths, Is.Empty); Assert.That(scenario.ResolvedCount, Is.EqualTo(4)); }
        else Assert.That(run.Deaths, Is.Not.Empty);
    }

    [Test]
    public void FireUnderstackAppliesMortalityAndRecordsMistakeWithoutInventedInstantKo()
    {
        var scenario = new FruFallOfFaithScenario();
        var seen = false;
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, PlayerRole = PartyRole.RegenHealer, StopAt = 16.3f,
            Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.TetherPermutation = 0; x.FireMask = 15; },
            Takeovers = [new(12, new Vector2(0, 12))],
            Probe = p => { if (p.Crossed(16.2f)) { seen = true; Assert.That(scenario.Understacks, Is.EqualTo(1)); Assert.That(p.Game.HasScenarioMistake, Is.True); Assert.That(p.World.Party.Get(PartyRole.MainTank)!.HasStatus(4372), Is.True); } },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(seen, Is.True);
        Assert.That(run.Deaths, Is.Empty);
    }

    [TestCase(0)] [TestCase(5)] [TestCase(15)]
    public void NativeSpawningMayBeAsynchronousWithoutAbortingTheMechanic(int frames)
    {
        var scenario = new FruFallOfFaithScenario();
        var nativeCasts = new HashSet<uint>();
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 4, new()
        {
            ScenarioFactory = () => scenario,
            ConfigureNative = f => f.BattleCharas.PacketSpawnDelayFrames = frames,
            Probe = p => { foreach (var source in scenario.SourceActors) foreach (var cast in ((FakeBattleChara)source.Proxy!).ReceivedCasts) nativeCasts.Add(cast.ActionId); },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(nativeCasts.Any(a => a is 40137 or 40140), Is.True);
        Assert.That(scenario.IsRunning, Is.False);
        Assert.That(scenario.SourceActors, Is.Empty);
    }

    [Test]
    public void RegisteredBeforeP2WithSnapshotBackedP1WeatherAndMusic()
    {
        var catalog = ScenarioCatalog.Create().Scenarios;
        Assert.That(catalog[0], Is.TypeOf<FruFallOfFaithScenario>());
        Assert.That(AnoMech.Core.Game.Game.DisplayName(catalog[0]), Is.EqualTo("P1 Fall of Faith"));
        Assert.That(catalog[0].Phase.Weather, Is.EqualTo(2));
        Assert.That(PhaseMusic.TrackFor(catalog[0]), Is.EqualTo(800));
        Assert.That(catalog[1], Is.TypeOf<AnoMech.Scenarios.Fru.LightRampant.FruLightRampantScenario>());
    }

    [Test]
    public void FireSelectsTheActualClosestPlayerEvenFromTheOppositeGroup()
    {
        var scenario = new FruFallOfFaithScenario();
        var observed = false;
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, PlayerRole = PartyRole.ShieldHealer, StopAt = 16.3f,
            Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.TetherPermutation = 0; x.FireMask = 15; },
            Takeovers = [new(12, new Vector2(FruFallOfFaithAi.Holder(0).X, .1f))],
            Probe = p => { if (p.Crossed(16.2f)) { observed = true; Assert.That(scenario.Cones[0].Target, Is.EqualTo(PartyRole.ShieldHealer)); Assert.That(scenario.Cones[0].Rotation, Is.EqualTo(0).Within(.001f)); } },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(observed, Is.True);
    }

    [Test]
    public void CrossingIntoNextGroupsAttackWhileVulnerableIsLethal()
    {
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MeleeDpsA, StopAt = 19.5f,
            Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.TetherPermutation = 0; x.FireMask = 15; },
            Takeovers = [new(17.5f, new Vector2(7.5f, .4f))], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MeleeDpsA && d.ActionId == 40156), Is.True, run.ToString);
    }

    [Test]
    public void MissingFirstHolderSwapCausesRealThirdWaveOverlap()
    {
        var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, StopAt = 22.2f,
            Overrides = o => { var x = (FruFallOfFaithStateOverrides)o; x.Advanced = true; x.TetherPermutation = 0; x.FireMask = 0; },
            Takeovers = [new(16.74f, null)], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths, Is.Not.Empty, run.ToString);
        Assert.That(run.Deaths.Any(d => d.Time >= 21.62f), Is.True, run.ToString);
    }

    [TestCase(1f)] [TestCase(6.11f)] [TestCase(13f)] [TestCase(23f)]
    public void CancellationAndReentryClearPendingActorsTethersAndFetters(float stop)
    {
        var scenario = new FruFallOfFaithScenario();
        for (var pass = 0; pass < 2; pass++)
        {
            FakeGame? native = null;
            SimCharacter? player = null;
            var run = ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, pass, new()
            {
                ScenarioFactory = () => scenario, StopAt = pass == 0 ? stop : null,
                ConfigureNative = n => { native = n; n.BattleCharas.PacketSpawnDelayFrames = 6; },
                Probe = p => player = p.World.Party.Player,
                WriteArtifactsOnFailure = false,
            });
            Assert.That(run.Passed, run.ToString);
            for (var frame = 0; frame < 30; frame++) { native!.BattleCharas.Tick(ScenarioRun.FrameSeconds);  }
            Assert.That(native!.BattleCharas.OccupiedSlots, Is.Zero);
            Assert.That(native.BattleCharas.PendingSpawns, Is.Zero);
            Assert.That(player!.HasStatus(2304), Is.False);
            Assert.That(player.HasStatus(1051), Is.False);
            Assert.That(scenario.SourceActors, Is.Empty);
            Assert.That(scenario.IsRunning, Is.False);
        }
        Assert.That(scenario.Completed, Is.True);
    }

    [Test]
    public void P1PresetRequestsRecordedLayersWithoutP2OrP3Activations()
    {
        FakeGame? native = null;
        var seen = false;
        ScenarioRun.Execute(typeof(FruFallOfFaithScenario), 0, 0, new()
        {
            StopAt = 4, ConfigureNative = n => native = n,
            Probe = p => { if (p.Crossed(3.5f))
            {
                var holds = ((FakeLayoutFunctions)AnoMech.Core.Native.Interfaces.Natives.Layout).HeldLayerStates;
                for (ushort key = 0x5F88; key <= 0x5F94; key++) Assert.That(holds[(1238, key)], Is.True);
                Assert.That(holds[(1238, 0x5F9F)], Is.True);
                Assert.That(holds[(1238, 0x5FC9)], Is.False);
                Assert.That(native!.MapEffects.Applied, Has.Count.EqualTo(54));
                Assert.That(native.MapEffects.Applied.All(e => e.Flags == 0x00040004), Is.True);
                seen = true;
            } }, WriteArtifactsOnFailure = false,
        });
        Assert.That(seen, Is.True);
    }
}
