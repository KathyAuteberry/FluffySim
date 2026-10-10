using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru;
using AnoMech.Scenarios.Fru.DiamondDust;

namespace AnoMech.Tests;

public class FruDiamondDustTests
{
    private static IEnumerable<TestCaseData> Variants() =>
        from axe in new[] { false, true }
        from supports in new[] { false, true }
        from axis in Enumerable.Range(0, 4)
        from sector in Enumerable.Range(0, 8)
        select new TestCaseData(axe, supports, axis, sector);

    [TestCaseSource(nameof(Variants))]
    public void ManaClearsImplementedPrefix(bool axe, bool supports, int axis, int sector)
    {
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, axis * 8 + sector, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruDiamondDustScenario(),
            Overrides = o =>
            {
                var settings = (FruDiamondDustStateOverrides)o;
                settings.Axe = axe; settings.StonesOnSupports = supports;
                settings.FirstIcicleAxis = axis; settings.OracleSector = sector;
            },
            Probe = p =>
            {
                if (p.Crossed(21f))
                {
                    var state = ((FruDiamondDustScenario)p.Game.ActiveScenario!).LastState!;
                    var closest = Enum.GetValues<PartyRole>().OrderBy(r => p.Member(r)!.Position.LengthSquared()).Take(4);
                    Assert.That(closest.All(r => !state.HasStone(r)), Is.True);
                    Assert.That(p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == FruConstants.BNpcBaseId.OraclesReflection).SpawnConfig.IsVisible, Is.True);
                    checks++;
                }
                if (p.Crossed(28.5f))
                {
                    var state = ((FruDiamondDustScenario)p.Game.ActiveScenario!).LastState!;
                    foreach (var role in Enum.GetValues<PartyRole>())
                        Assert.That(Vector3.Distance(p.Member(role)!.Position, FruDiamondDustAi.KnockbackPosition(state, role)), Is.LessThan(0.05f));
                    checks++;
                }
                if (p.Crossed(29.4f))
                {
                    var state = ((FruDiamondDustScenario)p.Game.ActiveScenario!).LastState!;
                    foreach (var role in Enum.GetValues<PartyRole>())
                        Assert.That(Vector3.Distance(p.Member(role)!.Position, FruDiamondDustAi.KnockbackPosition(state, role) * 3f), Is.LessThan(0.05f));
                    checks++;
                }
                if (p.Crossed(32.6f))
                {
                    Assert.That(((FruDiamondDustScenario)p.Game.ActiveScenario!).PrefixCompleted, Is.True);
                    Assert.That(p.World.Children.OfType<SimEnemy>().Where(e => e.IsActive), Is.Empty);
                    checks++;
                }
            }
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(4));
    }

    [Test]
    public void SeedReproducesAllImplementedRolls()
    {
        var a = new FruDiamondDustState(new Rng(123), new());
        var b = new FruDiamondDustState(new Rng(123), new());
        Assert.That((b.Axe, b.StonesOnSupports, b.FirstIcicleAxis, b.OracleSector),
            Is.EqualTo((a.Axe, a.StonesOnSupports, a.FirstIcicleAxis, a.OracleSector)));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void IcicleWavesCoverEightPositionsWithoutDuplicates(int axis)
    {
        var state = new FruDiamondDustState(new Rng(0), new() { FirstIcicleAxis = axis });
        var positions = new[] { 0, 1, 2 }.SelectMany(w => Enumerable.Range(0, w == 1 ? 4 : 2).Select(i => state.IciclePosition(w, i))).ToArray();
        Assert.That(positions.Select(p => MathF.Round(MathF.Atan2(p.X, -p.Z), 3)).Distinct().Count(), Is.EqualTo(8));
        Assert.That(positions.Select(p => p.Length()), Is.All.InRange(15.999f, 16.001f));
    }

    [TestCase(false)] [TestCase(true)]
    public void InvalidAxesAreRejected(bool oracle)
    {
        var settings = new FruDiamondDustStateOverrides();
        if (oracle) settings.OracleSector = 8; else settings.FirstIcicleAxis = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new FruDiamondDustState(new Rng(0), settings));
    }

    [Test]
    public void RegisteredAsAnIncompleteScenarioAboveLightRampant()
    {
        var catalog = ScenarioCatalog.Create().ToList();
        var diamondDust = catalog.Single(s => s is FruDiamondDustScenario);
        var lightRampant = catalog.Single(s => s is AnoMech.Scenarios.Fru.LightRampant.FruLightRampantScenario);
        Assert.That(diamondDust.IsIncomplete, Is.True);
        Assert.That(diamondDust.Name, Is.EqualTo("Diamond Dust (Incomplete)"));
        Assert.That(catalog.IndexOf(diamondDust), Is.LessThan(catalog.IndexOf(lightRampant)));
        Assert.That(catalog.Any(s => s is FruArenaDiagnosticScenario), Is.False);
        Assert.That(diamondDust.SettingsOverrides, Is.TypeOf<FruDiamondDustStateOverrides>());
        Assert.That(AnoMech.Core.Game.Game.DisplayName(lightRampant), Is.EqualTo("P2 Light Rampant (Mana)"));
        Assert.That(AnoMech.Core.Game.Game.FullName(lightRampant), Does.EndWith("P2 Light Rampant (provisional)"));
    }

    [TestCase(false)] [TestCase(true)]
    public void WrongKickPositionFails(bool axe)
    {
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruDiamondDustScenario(),
            PlayerRole = PartyRole.MainTank,
            Overrides = o => ((FruDiamondDustStateOverrides)o).Axe = axe,
            Takeovers = [new(21f, axe ? Vector2.Zero : new(10f, 0f))],
            StopAt = 22.2f,
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == (axe ? FruConstants.ActionId.AxeKick : FruConstants.ActionId.ScytheKick)), Is.True, run.ToString);
    }

    [Test]
    public void StandingInFirstIcicleFails()
    {
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruDiamondDustScenario(),
            PlayerRole = PartyRole.MainTank,
            Overrides = o =>
            {
                var s = (FruDiamondDustStateOverrides)o;
                s.Axe = false; s.FirstIcicleAxis = 0; s.StonesOnSupports = false;
            },
            Takeovers = [new(24f, new(0f, -16f))],
            StopAt = 25f,
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == FruConstants.ActionId.IcicleImpact), Is.True, run.ToString);
    }

    [TestCase(1f)] [TestCase(16f)] [TestCase(27f)]
    [TestCase(31.7f)]
    public void LeavingDisposesAllPrefixActors(float leaveAt)
    {
        SimEnemy[] actors = [];
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruDiamondDustScenario(), StopAt = leaveAt,
            Probe = p => actors = p.World.Children.OfType<SimEnemy>().ToArray(),
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(actors, Is.Not.Empty);
        Assert.That(actors.Select(e => e.IsActive), Is.All.False);
    }

    [Test]
    public void LeavingBeforePacketSpawnCompletesReclaimsLateActors()
    {
        FakeGame? native = null;
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new ScenarioRunOptions
        {
            ConfigureNative = fake => { native = fake; fake.BattleCharas.PacketSpawnDelayFrames = 6; },
            StopAt = 0.02f,
        });
        Assert.That(run.Passed, run.ToString);
        for (var frame = 0; frame < 12; frame++)
        {
            native!.BattleCharas.Tick(ScenarioRun.FrameSeconds);
            native.BattleCharas.SweepOrphans();
        }
        Assert.That(native!.BattleCharas.PendingSpawns, Is.Zero);
        Assert.That(native.BattleCharas.OccupiedSlots, Is.Zero);
    }

    [Test]
    public void NativeSpawnTimeoutAbortsBeforeMechanics()
    {
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new ScenarioRunOptions
        {
            ConfigureNative = fake => fake.BattleCharas.PacketSpawnDelayFrames = 30,
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Failure, Is.EqualTo("mechanic mistake"));
        Assert.That(run.Deaths, Is.Empty);
        Assert.That(run.Warnings.Any(w => w.Contains("cancelling the prefix")), Is.True);
    }

    [TestCase(1)]
    [TestCase(6)]
    [TestCase(18)]
    public void DelayedNativeSpawnsStillReceiveEveryPrefixCast(int delay)
    {
        var captured = false;
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new ScenarioRunOptions
        {
            ConfigureNative = fake => fake.BattleCharas.PacketSpawnDelayFrames = delay,
            Probe = p =>
            {
                if (!p.Crossed(31.7f)) return;
                var actors = p.World.Children.OfType<SimEnemy>().ToArray();
                var effects = actors.SelectMany(e => ((FakeBattleChara)e.Proxy!).ReceivedEffects).ToArray();
                var casts = actors.SelectMany(e => ((FakeBattleChara)e.Proxy!).ReceivedCasts).ToArray();
                Assert.That(casts.Count(c => c.ActionId == 40198), Is.EqualTo(8));
                Assert.That(casts.Count(c => c.ActionId is 40200 or 40201), Is.EqualTo(12));
                Assert.That(effects.Count(e => e.ActionId == 40199), Is.EqualTo(4));
                Assert.That(effects.Count(e => e.ActionId == 40206), Is.EqualTo(4));
                Assert.That(effects.Count(e => e.ActionId == 40207), Is.EqualTo(1));
                captured = true;
            }
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(captured, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativePresentationUsesPreparedActorsAndRecordedTransitions(bool axe)
    {
        FakeBattleChara? boss = null, oracle = null;
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new ScenarioRunOptions
        {
            Overrides = o => ((FruDiamondDustStateOverrides)o).Axe = axe,
            Probe = p =>
            {
                var actors = p.World.Children.OfType<SimEnemy>().ToList();
                if (p.Crossed(1f))
                {
                    Assert.That(actors, Has.Count.EqualTo(30));
                    Assert.That(actors.Single(e => e.BNpcBaseId == FruConstants.BNpcBaseId.OraclesReflection).SpawnConfig.EnemyList, Is.EqualTo(EnemyListMode.OnlyWhenVisible));
                    boss = (FakeBattleChara)actors.Single(e => e.BNpcBaseId == FruConstants.BNpcBaseId.UsurperOfFrost).Proxy!;
                    oracle = (FakeBattleChara)actors.Single(e => e.BNpcBaseId == FruConstants.BNpcBaseId.OraclesReflection).Proxy!;
                    Assert.That(actors.Where(e => e.BNpcBaseId == FruConstants.BNpcBaseId.Helper).All(e => e.SpawnConfig.PacketSpawnEnableDraw), Is.True);
                    Assert.That(actors.Select(e => e.Casting.CastSeq), Is.All.Zero);
                    checks++;
                }
                if (p.Crossed(17.2f))
                {
                    Assert.That(oracle!.Timelines, Does.Contain((ushort)0x11D6));
                    Assert.That(oracle.AnimationStates, Does.Contain((0xF9, 0)));
                    var mirror = boss!.ReceivedEffects.Single(e => e.ActionId == 40180);
                    Assert.That(mirror.Targets!.Single(), Is.EqualTo(actors.Single(e => e.BNpcBaseId == FruConstants.BNpcBaseId.UsurperOfFrost).GameObjectId));
                    var raidwide = boss.ReceivedEffects.Single(e => e.ActionId == 40197);
                    Assert.That(raidwide.Targets, Has.Count.EqualTo(8));
                    Assert.That(raidwide.AnimationLock, Is.EqualTo(2.6f));
                    checks++;
                }
                if (p.Crossed(29f))
                {
                    Assert.That(oracle!.AnimationStates, Does.Contain((0xFC, 0)));
                    var knockback = boss!.ReceivedEffects.Single(e => e.ActionId == 40207);
                    Assert.That(knockback.Targets, Has.Count.EqualTo(8));
                    Assert.That(knockback.AnimationLock, Is.EqualTo(3.1f));
                    var cones = actors.SelectMany(e => ((FakeBattleChara)e.Proxy!).ReceivedEffects.Where(f => f.ActionId == 40206).Select(f => (Actor: e, Effect: f))).ToArray();
                    Assert.That(cones, Has.Length.EqualTo(4));
                    Assert.That(cones.All(c => c.Effect.AnimationTarget == c.Actor.GameObjectId && c.Effect.ActionTarget != c.Actor.GameObjectId && c.Effect.Targets is null), Is.True);
                    var icicles = actors.Where(e => e.BNpcBaseId == FruConstants.BNpcBaseId.Helper)
                        .SelectMany(e => ((FakeBattleChara)e.Proxy!).ReceivedCasts).Where(e => e.ActionId == 40198).ToArray();
                    Assert.That(icicles, Has.Length.EqualTo(8));
                    Assert.That(icicles.Select(c => c.CastTime), Is.All.EqualTo(8.7f));
                    checks++;
                }
                if (p.Crossed(31.7f))
                {
                    Assert.That(((FruDiamondDustScenario)p.Game.ActiveScenario!).PrefixCompleted, Is.True);
                    var needles = actors.SelectMany(e => ((FakeBattleChara)e.Proxy!).ReceivedEffects).Where(e => e.ActionId is 40200 or 40201).ToArray();
                    Assert.That(needles, Has.Length.EqualTo(12));
                    Assert.That(needles.Select(e => e.AnimationLock), Is.All.EqualTo(1.1f));
                    Assert.That(actors, Is.Not.Empty);
                    checks++;
                }
                if (p.Crossed(32.6f))
                {
                    Assert.That(actors, Is.Empty);
                    checks++;
                }
            }
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checks, Is.EqualTo(5));
    }

    [TestCase(0)]
    [TestCase(17)]
    [TestCase(123)]
    public void SameScenarioInstanceCanRunAgainAfterCleanup(int seed)
    {
        var scenario = new FruDiamondDustScenario();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, seed, new ScenarioRunOptions
            {
                ScenarioFactory = () => scenario,
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(scenario.PrefixCompleted, Is.True);
        }
    }

    public sealed class RecordedTimeline
    {
        public string Source { get; set; } = "";
        public int Pull { get; set; }
        public bool Axe { get; set; }
        public bool Supports { get; set; }
        public int Axis { get; set; }
        public int OracleSector { get; set; }
        public bool TwinStillnessFirst { get; set; }
        public Dictionary<string, float[]> Effects { get; set; } = [];
    }
    private static IEnumerable<TestCaseData> Recordings()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Scenarios/Fru/DiamondDust/RecordedTimelines.json");
        return System.Text.Json.JsonSerializer.Deserialize<RecordedTimeline[]>(File.ReadAllText(path))!
            .Select(r => new TestCaseData(r).SetName($"PrefixTimelineMatches{r.Source}Pull{r.Pull}"));
    }
    [TestCaseSource(nameof(Recordings))]
    public void PrefixMatchesRecordedEffects(RecordedTimeline recording)
    {
        var observed = new Dictionary<uint, List<float>>();
        var seen = new HashSet<(SimEnemy Actor, int Seq)>();
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, recording.Pull, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruDiamondDustScenario(),
            Overrides = o =>
            {
                var s = (FruDiamondDustStateOverrides)o;
                s.Axe = recording.Axe; s.StonesOnSupports = recording.Supports;
                s.FirstIcicleAxis = recording.Axis; s.OracleSector = recording.OracleSector;
            },
            Probe = p =>
            {
                foreach (var actor in p.World.Children.OfType<SimEnemy>())
                {
                    if (actor.Casting.EffectSeq == 0 || !seen.Add((actor, actor.Casting.EffectSeq))) continue;
                    var id = actor.Casting.EffectActionId;
                    if (!observed.TryGetValue(id, out var times)) observed[id] = times = [];
                    if (times.Count == 0 || p.Time - 3f - times[^1] > 0.2f) times.Add(p.Time - 3f);
                }
            }
        });
        Assert.That(run.Passed, run.ToString);
        foreach (var id in new uint[] { 40180, 40197, 40198, 40199, 40206, 40207, 40200, 40201, recording.Axe ? 40202u : 40203u })
        {
            Assert.That(observed.ContainsKey(id), Is.True, $"Missing {id}");
            var expected = recording.Effects[id.ToString()];
            var actual = observed[id];
            var count = id == 40198 ? 2 : 1;
            Assert.That(actual, Has.Count.EqualTo(count));
            for (var j = 0; j < count; j++) Assert.That(actual[j], Is.EqualTo(expected[j]).Within(0.2f), $"Pull {recording.Pull}: {id}");
        }
    }

    [Test]
    public void OverlappingStoneTargetsFail()
    {
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruDiamondDustScenario(), PlayerRole = PartyRole.MainTank,
            Overrides = o =>
            {
                var s = (FruDiamondDustStateOverrides)o;
                s.Axe = false; s.StonesOnSupports = true; s.FirstIcicleAxis = 0;
            },
            StopAt = 24.5f,
            Probe = p =>
            {
                if (!p.Crossed(24.2f)) return;
                p.Member(PartyRole.MainTank)!.StopMoving();
                p.Member(PartyRole.MainTank)!.SetPosition(p.Member(PartyRole.OffTank)!.Position);
            }
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == FruConstants.ActionId.FrigidStone), Is.True, run.ToString);
    }
    [Test]
    public void StandingInNeedleFails()
    {
        var run = ScenarioRun.Execute(typeof(FruDiamondDustScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruDiamondDustScenario(), PlayerRole = PartyRole.MainTank,
            StopAt = 31.4f,
            Probe = p =>
            {
                if (!p.Crossed(30f)) return;
                var needle = p.World.Children.OfType<SimEnemy>().First(e => e.Casting.ActionId == FruConstants.ActionId.FrigidNeedleCircle);
                p.Member(PartyRole.MainTank)!.StopMoving();
                p.Member(PartyRole.MainTank)!.SetPosition(needle.Position);
            }
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == FruConstants.ActionId.FrigidNeedleCircle), Is.True, run.ToString);
    }
}
