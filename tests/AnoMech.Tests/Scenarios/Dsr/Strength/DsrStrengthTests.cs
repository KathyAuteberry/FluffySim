using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.Strength;

namespace AnoMech.Tests;

public class DsrStrengthTests
{
    private static IEnumerable<TestCaseData> Variants() =>
        from axis in Enumerable.Range(0, 4)
        from sides in Enumerable.Range(0, 8)
        from heavy in Enumerable.Range(0, 4)
        select new TestCaseData(axis, sides, heavy);

    [TestCaseSource(nameof(Variants))]
    public void ElementalClearsAllChargeAndHeavyPatterns(int axis, int sides, int heavy)
    {
        var completed = false;
        var run = ScenarioRun.Execute(typeof(DsrStrengthScenario), 0, sides * 4 + axis, new()
        {
            ScenarioFactory = () => new DsrStrengthScenario(),
            Overrides = settings =>
            {
                var o = (DsrStrengthStateOverrides)settings;
                o.SafeAxis = axis; o.KnightSides = sides; o.HeavyCardinal = heavy;
                o.AdditionalPattern = sides;
            },
            Probe = p =>
            {
                if (p.Crossed(30.1f)) completed = ((DsrStrengthScenario)p.Game.ActiveScenario!).Completed;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(completed, Is.True);
    }

    [Test]
    public void SeedReproducesAllRolls()
    {
        for (var seed = 0; seed < 100; ++seed)
        {
            var a = new DsrStrengthState(new Rng(seed), new());
            var b = new DsrStrengthState(new Rng(seed), new());
            Assert.That(b.KnightClocks, Is.EqualTo(a.KnightClocks));
            Assert.That((b.SafeAxis,b.KnightSides,b.HeavyCardinal,b.AdditionalPattern),
                Is.EqualTo((a.SafeAxis,a.KnightSides,a.HeavyCardinal,a.AdditionalPattern)));
        }
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void TrianglesHaveCorrectGroupsAndLightningSeparation(int axis)
    {
        var s = new DsrStrengthState(new Rng(0), new() { SafeAxis = axis });
        var points = Enumerable.Range(0, 8).Select(i => DsrStrengthAi.Triangle(s, (PartyRole)i)).ToArray();
        for (var i = 0; i < 8; ++i)
        {
            Assert.That(points[i].Length(), Is.LessThan(21));
            Assert.That(Vector3.Dot(points[i], DsrStrengthState.Direction(s.MainGroupClock)),
                i % 2 == 0 ? Is.GreaterThan(0) : Is.LessThan(0));
            for (var j = i + 1; j < 8; ++j)
                Assert.That(Vector3.Distance(points[i],points[j]), Is.GreaterThan(5));
        }
    }

    [Test]
    public void TuuflessWaymarksPreserveProvidedCoordinates()
    {
        var marks = DsrZone.Instance.WaymarkPresets.Single().Markers;
        Assert.That(marks.Count, Is.EqualTo(8));
        Assert.That(marks[0].Offset + DsrZone.Instance.Origin, Is.EqualTo(new Vector3(100,0,87)));
        Assert.That(marks[4].Offset + DsrZone.Instance.Origin, Is.EqualTo(new Vector3(109.192f,0,90.807f)));
        Assert.That(marks[7].Offset + DsrZone.Instance.Origin, Is.EqualTo(new Vector3(90.807f,0,90.807f)));
    }

    [Test]
    public void NativeEffectTimelineMatchesFortySixCompleteRecordings()
    {
        var emitted = new Dictionary<uint, float>();
        var run = ScenarioRun.Execute(typeof(DsrStrengthScenario), 0, 0, new()
        {
            ScenarioFactory = () => new DsrStrengthScenario(),
            Probe = p =>
            {
                foreach (var enemy in p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>())
                    if (enemy.Proxy is FakeBattleChara proxy)
                        foreach (var effect in proxy.ReceivedEffects)
                            emitted.TryAdd(effect.ActionId, p.World.Events.Elapsed - 3f);
            },
        });
        Assert.That(run.Passed, run.ToString);
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Scenarios/Dsr/Strength/RecordedTimelines.json")));
        var complete = doc.RootElement.GetProperty("sequences").EnumerateArray()
            .Where(r => r.GetProperty("effects").TryGetProperty("25562", out _) && r.GetProperty("effects").TryGetProperty("25545", out _)).ToArray();
        Assert.That(complete.Length, Is.EqualTo(46));
        foreach (var record in complete)
        foreach (var effect in record.GetProperty("effects").EnumerateObject())
        {
            var id = uint.Parse(effect.Name);
            Assert.That(emitted.ContainsKey(id), Is.True, $"missing native effect {id}");
            Assert.That(emitted[id], Is.EqualTo(effect.Value[0].GetSingle()).Within(.18f), $"native effect {id}");
        }
    }

    [TestCase(false)] [TestCase(true)]
    public void MissingMercyDodgeKillsHumanAndBot(bool bot)
    {
        var state = new DsrStrengthState(new Rng(0), new() { SafeAxis = 0 });
        var point = DsrStrengthAi.Bait(state, PartyRole.MainTank);
        var run = ScenarioRun.Execute(typeof(DsrStrengthScenario), 0, 0, new()
        {
            ScenarioFactory = () => new DsrStrengthScenario(), PlayerRole = PartyRole.MainTank,
            Overrides = o => ((DsrStrengthStateOverrides)o).SafeAxis = 0,
            Takeovers = [new(25.5f, new Vector2(point.X, point.Z), Bot: bot ? PartyRole.MainTank : null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == 25545), Is.True, run.ToString);
    }

    [TestCase(false)] [TestCase(true)]
    public void OverlappingLightningKillsHumanAndBot(bool bot)
    {
        var state = new DsrStrengthState(new Rng(0), new() { SafeAxis = 0 });
        var point = DsrStrengthAi.Triangle(state, PartyRole.RegenHealer);
        var run = ScenarioRun.Execute(typeof(DsrStrengthScenario), 0, 0, new()
        {
            ScenarioFactory = () => new DsrStrengthScenario(), PlayerRole = PartyRole.MainTank,
            Overrides = o => ((DsrStrengthStateOverrides)o).SafeAxis = 0,
            Takeovers = [new(19.75f, new Vector2(point.X, point.Z), Bot: bot ? PartyRole.MainTank : null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == 25549), Is.True, run.ToString);
    }

    [TestCase(1, 19.2f, 0f)] [TestCase(2, 21.4f, 9f)] [TestCase(3, 23.3f, 15f)]
    [TestCase(4, 25.1f, 21f)] [TestCase(5, 27f, 27f)]
    public void StandingInEachHeavyPulseFails(int pulse, float time, float radius)
    {
        var run = ScenarioRun.Execute(typeof(DsrStrengthScenario), 0, 0, new()
        {
            ScenarioFactory = () => new DsrStrengthScenario(), PlayerRole = PartyRole.MainTank,
            Overrides = o => { ((DsrStrengthStateOverrides)o).HeavyCardinal = 0; ((DsrStrengthStateOverrides)o).SafeAxis = 0; },
            Takeovers = [new(time, new Vector2(0, pulse == 1 ? -12 : radius - 7))], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == 25557 + pulse), Is.True, run.ToString);
    }

    [TestCase(0)] [TestCase(11.1f)] [TestCase(22f)] [TestCase(30.1f)]
    public void LeavingAtDifferentLifecycleStagesReleasesNativeActors(float time)
    {
        FakeGame? fake = null;
        var run = ScenarioRun.Execute(typeof(DsrStrengthScenario), 0, 0, new()
        {
            ScenarioFactory = () => new DsrStrengthScenario(), StopAt = time,
            ConfigureNative = f => { fake = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
    }

    [TestCase(12604u, 3632u, 3269u)] [TestCase(12633u, 3636u, 3268u)]
    [TestCase(12634u, 3637u, 3268u)] [TestCase(12635u, 3638u, 3268u)]
    [TestCase(12637u, 3641u, 3268u)] [TestCase(12601u, 3634u, 3268u)]
    [TestCase(12632u, 3635u, 3268u)] [TestCase(12602u, 3639u, 3268u)]
    [TestCase(12636u, 3640u, 3268u)]
    public void NativeActorsUseVisibleAuthenticatedModelsAndEquipment(uint id, uint name, uint model)
    {
        var config = DsrActors.Config(id, default);
        Assert.That(config.NameId, Is.EqualTo(name));
        Assert.That(config.ModelCharaId, Is.EqualTo(model));
        Assert.That(config.IsVisible, Is.True);
        Assert.That(config.Appearance!.BodyModel, Is.Not.Zero);
        Assert.That(config.Appearance.MainhandWeaponModel, Is.Not.Zero);
        if (id != 12604)
        {
            Assert.That(config.Appearance.HeadModel, Is.Not.Null.And.Not.Zero);
            Assert.That(config.Appearance.LegsModel, Is.Not.Null.And.Not.Zero);
        }
        if (id is 12601 or 12632) Assert.That(config.Appearance.OffhandWeaponModel, Is.Not.Null.And.Not.Zero);
    }

    [Test]
    public void CatalogExposesElementalSettingsAndKeepsFruFirst()
    {
        var scenarios = ScenarioCatalog.Create();
        Assert.That(scenarios[1], Is.TypeOf<AnoMech.Scenarios.Fru.LightRampant.FruLightRampantScenario>());
        Assert.That(scenarios[2], Is.TypeOf<AnoMech.Scenarios.Fru.UltimateRelativity.FruUltimateRelativityScenario>());
        Assert.That(scenarios[5], Is.TypeOf<DsrStrengthScenario>());
        Assert.That(scenarios[5].Phase.Name, Is.EqualTo("P2 King Thordan"));
        Assert.That(scenarios[5].SettingsOverrides, Is.TypeOf<DsrStrengthStateOverrides>());
        Assert.That(scenarios.OfType<DsrArenaDiagnosticScenario>(), Is.Empty);
    }

    [TestCase(0)] [TestCase(8)] [TestCase(32)]
    public void ArenaDiagnosticHasNoActorsOrPresetWrites(int seed)
    {
        var inspected = false;
        var run = ScenarioRun.Execute(typeof(DsrArenaDiagnosticScenario), 0, seed, new()
        {
            ScenarioFactory = () => new DsrArenaDiagnosticScenario(), Solo = true, StopAt = 1f,
            Probe = p =>
            {
                inspected = true;
                Assert.That(p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>(), Is.Empty);
                Assert.That(((FakeMapEffects)AnoMech.Core.Native.Interfaces.Natives.MapEffects).Applied, Is.Empty);
                Assert.That(((FakeLayoutFunctions)AnoMech.Core.Native.Interfaces.Natives.Layout).HeldLayerStates, Is.Empty);
            },
        });
        Assert.That(inspected, Is.True);
        Assert.That(run.Passed, run.ToString);
    }

    [TestCase(false)] [TestCase(true)]
    public void EnteringAChargeLaneKillsHumanAndBot(bool bot)
    {
        var run = ScenarioRun.Execute(typeof(DsrStrengthScenario), 0, 0, new()
        {
            ScenarioFactory = () => new DsrStrengthScenario(), PlayerRole = PartyRole.MainTank,
            Overrides = o => { ((DsrStrengthStateOverrides)o).SafeAxis = 0; ((DsrStrengthStateOverrides)o).HeavyCardinal = 0; },
            Takeovers = [new(19.2f, new Vector2(8,0), Bot: bot ? PartyRole.MainTank : null)], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == 25556), Is.True, run.ToString);
    }

    [Test]
    public void ReusingTheScenarioAfterCancellationAndCompletionHasNoStateLeak()
    {
        var scenario = new DsrStrengthScenario();
        foreach (var stop in new float?[] { 11.2f, null, 22f, null })
        {
            FakeGame? fake = null;
            var run = ScenarioRun.Execute(typeof(DsrStrengthScenario), 0, 7, new()
            {
                ScenarioFactory = () => scenario, StopAt = stop, ConfigureNative = f => fake = f,
            });
            Assert.That(run.Passed, run.ToString);
            Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
            Assert.That(scenario.Completed, Is.EqualTo(stop is null));
        }
    }

    [Test]
    public void KnightIdentitiesCanOccupyEveryValidPermutation()
    {
        var permutations = Enumerable.Range(0, 100).Select(seed => new DsrStrengthState(new Rng(seed),
            new() { SafeAxis = 0, KnightSides = 0, HeavyCardinal = 0, AdditionalPattern = 0 }).KnightClocks)
            .Select(clocks => string.Join(",", clocks)).Distinct().ToArray();
        Assert.That(permutations.Length, Is.EqualTo(6));
    }

    [Test]
    public void BotsReadTheArrivingKnightsBeforeMovingToTheirTriangles()
    {
        var checkedBefore = false;
        var checkedAfter = false;
        var run = ScenarioRun.Execute(typeof(DsrStrengthScenario), 0, 0, new()
        {
            ScenarioFactory = () => new DsrStrengthScenario(),
            Probe = p =>
            {
                var state = ((DsrStrengthScenario)p.Game.ActiveScenario!).LastState!;
                if (p.Crossed(11.3f))
                {
                    checkedBefore = true;
                    Assert.That(Vector3.Distance(p.Member(PartyRole.MainTank)!.Position,
                        DsrStrengthAi.Triangle(state, PartyRole.MainTank)), Is.GreaterThan(1));
                }
                if (p.Crossed(18f))
                {
                    checkedAfter = true;
                    for (var role = 0; role < 8; ++role)
                        Assert.That(Vector3.Distance(p.Member((PartyRole)role)!.Position,
                            DsrStrengthAi.Triangle(state, (PartyRole)role)), Is.LessThan(.05f));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(checkedBefore && checkedAfter, Is.True);
    }
}
