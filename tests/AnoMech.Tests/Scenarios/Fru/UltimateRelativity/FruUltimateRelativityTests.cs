using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.UltimateRelativity;

namespace AnoMech.Tests;

public class FruUltimateRelativityTests
{
    [TestCase(false)] [TestCase(true)]
    public void FullManaSequenceClears(bool ice)
    {
        var complete = false;
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(),
            Overrides = o => ((FruUltimateRelativityStateOverrides)o).DpsIce = ice,
            Probe = p =>
            {
                if (!p.Crossed(65.1f)) return;
                var scenario = (FruUltimateRelativityScenario)p.Game.ActiveScenario!;
                complete = scenario.Completed;
                Assert.That(scenario.ReturnPositions, Has.Count.EqualTo(8));
                Assert.That(scenario.Resolutions.Count(r => r.Action == 40235), Is.EqualTo(3));
                Assert.That(scenario.Resolutions.Count(r => r.Action == 40292), Is.EqualTo(27));
                Assert.That(p.World.SoftMarkers.Count, Is.Zero);
                Assert.That(p.World.Children.OfType<SimEnemy>(), Is.Empty);
                foreach (var m in p.World.Party.ActiveMembers()) foreach (var id in FruUltimateRelativityScenario.PlayerStatuses) Assert.That(m.HasStatus(id), Is.False);
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(complete, Is.True);
    }

    [Test]
    public void SameSeedReproducesAssignmentsAndMarkers()
    {
        var a = new FruUltimateRelativityState(new Rng(43), new());
        var b = new FruUltimateRelativityState(new Rng(43), new());
        Assert.That(a.Roles, Is.EqualTo(b.Roles));
        Assert.That(a.StackTargets, Is.EqualTo(b.StackTargets));
        Assert.That(a.ClockwiseMask, Is.EqualTo(b.ClockwiseMask));
        foreach (var role in a.Roles) Assert.That(a.Marker(role), Is.EqualTo(b.Marker(role)));
    }

    [Test]
    public void AssignmentConstraintsAndHumanEligibilityHoldAcrossSeeds()
    {
        var eligible = new HashSet<(PartyRole, Sign)>();
        for (var seed = 0; seed < 256; seed++)
        {
            var s = new FruUltimateRelativityState(new Rng(seed), new());
            Assert.That(s.Roles.Distinct().Count(), Is.EqualTo(8));
            Assert.That(s.Roles.Count(s.IsIce), Is.EqualTo(1));
            Assert.That(s.Roles.Count(r => s.FireGroup(r) == 1), Is.EqualTo(3));
            Assert.That(s.Roles.Count(r => s.FireGroup(r) == 2), Is.EqualTo(2));
            Assert.That(s.Roles.Count(r => s.FireGroup(r) == 3), Is.EqualTo(3));
            Assert.That(s.StackTargets.Distinct().Count(), Is.EqualTo(3));
            for (var order = 1; order <= 3; order++) Assert.That(s.FireOrder(s.StackTargets[order - 1]), Is.Not.EqualTo(order));
            foreach (var role in s.Roles)
            {
                if (s.Marker(role) is not { } mark) continue;
                eligible.Add((role, mark));
                if (mark is Sign.Attack1 or Sign.Attack2 or Sign.Bind1 or Sign.Bind2)
                    Assert.That(s.FireOrder(role), Is.EqualTo(role.IsDps() ? 1 : 3));
                Assert.That(role.IsDps(), Is.EqualTo(mark is Sign.Attack1 or Sign.Attack2 or Sign.Ignore2 or Sign.Bind3));
                Assert.That(s.FireGroup(role), Is.EqualTo(mark switch
                {
                    Sign.Attack1 or Sign.Attack2 or Sign.Attack3 => 1,
                    Sign.Ignore1 or Sign.Ignore2 => 2,
                    _ => 3,
                }));
            }
        }
        Assert.That(eligible.Count, Is.EqualTo(32));
    }

    private static IEnumerable<TestCaseData> MarkerOrientations() => from north in Enumerable.Range(0, 8)
        from ice in new[] { false, true } select new TestCaseData(north, ice);

    [TestCaseSource(nameof(MarkerOrientations))]
    public void EveryManaSlotKeepsItsDistinctMarkerAcrossIceBranchesAndOrientations(int north, bool ice)
    {
        var state = new FruUltimateRelativityState(new Rng(19), new() { RelativeNorth = north, DpsIce = ice });
        Sign[] signs = [Sign.Attack1, Sign.Attack2, Sign.Ignore2, Sign.Bind3, Sign.Attack3, Sign.Ignore1, Sign.Bind1, Sign.Bind2];
        int[] clocks = [7, 1, 2, 0, 4, 6, 5, 3];
        Assert.That(state.Roles.Select(state.Marker).Distinct().Count(), Is.EqualTo(8));
        for (var slot = 0; slot < 8; slot++)
        {
            var role = state.Roles[slot];
            Assert.That(state.Marker(role), Is.EqualTo(signs[slot]));
            Assert.That(state.Clock(role), Is.EqualTo(clocks[slot]));
            var theta = (north + clocks[slot]) * MathF.PI / 4f;
            Assert.That(Vector3.Distance(state.Direction(state.Clock(role)), new Vector3(MathF.Sin(theta), 0f, -MathF.Cos(theta))), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(FruUltimateRelativityAi.BaitPosition(state, role), state.Hourglass(clocks[slot])), Is.EqualTo(1.5f).Within(0.0001f));
        }
        Assert.That(state.Marker(state.Roles.Single(state.IsIce)), Is.EqualTo(ice ? Sign.Bind3 : Sign.Attack3));
    }

    private static IEnumerable<TestCaseData> Spins() => from mask in Enumerable.Range(0, 256)
        from ice in new[] { false, true } select new TestCaseData(mask, ice);

    [TestCaseSource(nameof(Spins))]
    public void AllIndependentHourglassSpinCombinationsClear(int mask, bool ice)
    {
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, mask, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(),
            Overrides = o =>
            {
                var settings = (FruUltimateRelativityStateOverrides)o;
                settings.ClockwiseMask = mask;
                settings.DpsIce = ice;
                settings.RelativeNorth = mask % 8;
            },
        });
        Assert.That(run.Passed, run.ToString);
    }

    private static IEnumerable<TestCaseData> Seats() => from role in Enumerable.Range(0, 8)
        from assignment in Enumerable.Range(role < 4 ? 4 : 0, 4)
        from ice in new[] { false, true } select new TestCaseData((PartyRole)role, (RelativityAssignment)assignment, ice);

    [TestCaseSource(nameof(Seats))]
    public void HumanCanPracticeEveryEligibleAssignment(PartyRole role, RelativityAssignment assignment, bool ice)
    {
        var assigned = false;
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, (int)assignment, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(),
            PlayerRole = role,
            Overrides = o =>
            {
                var settings = (FruUltimateRelativityStateOverrides)o;
                settings.PracticeRole = role; settings.PracticeAssignment = assignment; settings.DpsIce = ice;
            },
            Probe = p =>
            {
                if (!p.Crossed(15f)) return;
                var state = ((FruUltimateRelativityScenario)p.Game.ActiveScenario!).LastState!;
                Assert.That(state.Assignment(role), Is.EqualTo(assignment));
                Assert.That(p.World.SoftMarkers.Count, Is.EqualTo(8));
                var expected = state.Roles.Where(r => state.Marker(r) != null)
                    .Select(r => (state.Marker(r)!.Value, p.Member(r)!.Position + p.World.ScenarioOrigin + new Vector3(0f, 2.8f, 0f))).ToArray();
                Assert.That(p.World.SoftMarkers.Snapshot(p.World.ScenarioOrigin).Select(m => (m.Sign, m.Position)), Is.EquivalentTo(expected));
                assigned = true;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(assigned, Is.True);
    }

    [TestCase(false)] [TestCase(true)]
    public void FireCollisionKillsHumansAndBotsEqually(bool bot)
    {
        var role = PartyRole.MeleeDpsA;
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(), PlayerRole = PartyRole.MeleeDpsA,
            Overrides = o =>
            {
                var settings = (FruUltimateRelativityStateOverrides)o;
                settings.PracticeRole = role; settings.PracticeAssignment = RelativityAssignment.ShortDps1;
            },
            Takeovers = [new(24.8f, Vector2.Zero, Bot: bot ? role : null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Deaths.Any(d => d.ActionId == 40276), Is.True);
    }

    [TestCase(0.02f)] [TestCase(15f)] [TestCase(30f)] [TestCase(55f)]
    public void CancellationClearsActorsStatusesAndLocalMarkers(float leaveAt)
    {
        FakeGame? native = null;
        SoftMarkers? markers = null;
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 1, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(), StopAt = leaveAt,
            ConfigureNative = fake => { native = fake; fake.BattleCharas.PacketSpawnDelayFrames = 6; },
            Probe = p => markers = p.World.SoftMarkers,
        });
        Assert.That(run.Passed, run.ToString);
        for (var frame = 0; frame < 20; frame++) { native!.BattleCharas.Tick(ScenarioRun.FrameSeconds); native.BattleCharas.SweepOrphans(); }
        Assert.That(native!.BattleCharas.PendingSpawns, Is.Zero);
        Assert.That(native.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(markers!.Count, Is.Zero);
    }

    [Test]
    public void ScenarioInstanceCanBeRestartedAfterCancellation()
    {
        var scenario = new FruUltimateRelativityScenario();
        for (var pass = 0; pass < 3; pass++)
        {
            var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, pass, new ScenarioRunOptions
            {
                ScenarioFactory = () => scenario, StopAt = pass == 0 ? 15f : null,
            });
            Assert.That(run.Passed, run.ToString);
        }
        Assert.That(scenario.Completed, Is.True);
    }

    [Test]
    public void NativeActorsStatusesSpinsAndPresentationTargetsMatchTheirAssignments()
    {
        var inspected = false;
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 3, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(),
            Overrides = o => ((FruUltimateRelativityStateOverrides)o).ClockwiseMask = 85,
            Probe = p =>
            {
                var scenario = (FruUltimateRelativityScenario)p.Game.ActiveScenario!;
                var state = scenario.LastState!;
                var actors = p.World.Children.OfType<SimEnemy>().ToArray();
                if (p.Crossed(15f))
                {
                    var boss = actors.Single(a => a.SpawnConfig.BNpcBaseId == 17831);
                    Assert.That(boss.SpawnConfig.IsVisible, Is.True);
                    Assert.That(boss.SpawnConfig.Appearance, Is.EqualTo(new NpcAppearance(65537, 4295237459)));
                    Assert.That(boss.SpawnConfig.NameId, Is.EqualTo(9832));
                    Assert.That(boss.SpawnConfig.Targetable, Is.True);
                    Assert.That(boss.Position, Is.EqualTo(Vector3.Zero));
                    Assert.That(Natives.Data.BNpcBase(17831)!.ModelChara, Is.EqualTo(3057));
                    Assert.That(Natives.Data.BNpcBase(17832)!.ModelChara, Is.EqualTo(476));
                    var hourglasses = actors.Where(a => a.SpawnConfig.BNpcBaseId == 17832).ToArray();
                    Assert.That(hourglasses, Has.Length.EqualTo(8));
                    foreach (var glass in hourglasses)
                    {
                        Assert.That(glass.SpawnConfig.IsVisible, Is.True);
                        Assert.That(glass.SpawnConfig.Targetable, Is.False);
                    }
                    for (var i = 0; i < 8; i++) Assert.That(Vector3.Distance(hourglasses[i].Position, state.Hourglass(i)), Is.LessThan(0.005f));
                    foreach (var role in state.Roles)
                    {
                        var member = p.Member(role)!;
                        Assert.That(member.HasStatus(state.IsIce(role) ? (ushort)2462 : (ushort)2455), Is.True);
                        Assert.That(member.FindStatus(state.FinalStatus(role))!.RemainingTime, Is.EqualTo(41.83f).Within(0.04f));
                    }
                }
                if (p.Crossed(26.1f))
                {
                    var glass = actors.Where(a => a.SpawnConfig.BNpcBaseId == 17832).ToArray();
                    for (var i = 0; i < 8; i++)
                    {
                        var status = glass[i].FindStatus(2970);
                        if (i is 0 or 3 or 5) Assert.That(status!.Stacks, Is.EqualTo(state.Clockwise(i) ? 0x15C : 0x10D));
                        else Assert.That(status, Is.Null);
                    }
                    var effects = actors.SelectMany(a => ((FakeBattleChara)a.Proxy!).ReceivedEffects).ToArray();
                    Assert.That(effects.Single(e => e.ActionId == 40266).Targets, Has.Count.EqualTo(8));
                    Assert.That(effects.Single(e => e.ActionId == 40293).Targets, Has.Count.EqualTo(1));
                    var stack = effects.Single(e => e.ActionId == 40277);
                    Assert.That(stack.Targets, Has.Count.EqualTo(state.DpsIce ? 5 : 6));
                    Assert.That(stack.AnimationLock, Is.EqualTo(1.1f));
                    inspected = true;
                }
                if (p.Crossed(55.4f))
                {
                    foreach (var member in p.World.Party.ActiveMembers()) Assert.That(member.MovementLocked, Is.True);
                }
                if (p.Crossed(56.4f))
                {
                    foreach (var role in state.Roles) Assert.That(Vector3.Distance(p.Member(role)!.Position, scenario.ReturnPositions[role]), Is.LessThan(0.05f));
                }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(inspected, Is.True);
    }

    [TestCase(false)] [TestCase(true)]
    public void LookingInwardAtRewindGazesFailsForHumansAndBots(bool bot)
    {
        var role = PartyRole.MeleeDpsA;
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(), PlayerRole = role,
            Overrides = o =>
            {
                var settings = (FruUltimateRelativityStateOverrides)o;
                settings.RelativeNorth = 0; settings.PracticeRole = role; settings.PracticeAssignment = RelativityAssignment.ShortDps1;
            },
            Takeovers = [new(53.82f, null, Facing: MathF.PI / 4f, Bot: bot ? role : null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Deaths.Any(d => d.ActionId == 40278), Is.True);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void EveryRelativeNorthClears(int north)
    {
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, north + 50, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(),
            Overrides = o => ((FruUltimateRelativityStateOverrides)o).RelativeNorth = north,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void NormalCatalogContainsManaAfterLightRampantAndKeepsDiamondDustShelved()
    {
        var catalog = ScenarioCatalog.Create();
        Assert.That(catalog[0], Is.TypeOf<AnoMech.Scenarios.Fru.LightRampant.FruLightRampantScenario>());
        Assert.That(catalog[1], Is.TypeOf<FruUltimateRelativityScenario>());
        Assert.That(catalog.Any(s => s is AnoMech.Scenarios.Fru.DiamondDust.FruDiamondDustScenario), Is.False);
        Assert.That(Game.DisplayName(catalog[1]), Is.EqualTo("P3 Ultimate Relativity"));
        Assert.That(catalog[1].SettingsOverrides, Is.TypeOf<FruUltimateRelativityStateOverrides>());
    }

    [Test]
    public void TimelineMatchesFiveCompleteRecordedSequencesWithinReceiveTolerance()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Scenarios/Fru/UltimateRelativity/RecordedTimelines.json");
        using var records = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var recordings = records.RootElement.EnumerateArray().Where(r => r.GetProperty("complete").GetBoolean()).ToArray();
        Assert.That(recordings.Length, Is.GreaterThanOrEqualTo(5));
        IReadOnlyList<(float Time, uint Action)>? timeline = null;
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(),
            Probe = p => timeline = ((FruUltimateRelativityScenario)p.Game.ActiveScenario!).Resolutions.ToArray(),
        });
        Assert.That(run.Passed, run.ToString);
        foreach (var recording in recordings)
        {
            foreach (var id in new uint[] { 40276,40277,40279,40235,40274,40271,40278,40287 })
            {
                if (!recording.GetProperty("effects").TryGetProperty(id.ToString(), out var effects)) continue;
                var expected = effects.EnumerateArray().Select(t => t.GetSingle()).ToArray();
                var actual = timeline!.Where(e => e.Action == id).Select(e => e.Time - 3f).ToArray();
                Assert.That(actual.Length, Is.EqualTo(expected.Length), $"action {id}");
                for (var i = 0; i < expected.Length; i++) Assert.That(actual[i], Is.EqualTo(expected[i]).Within(0.25f), $"action {id}, event {i}");
            }
        }
    }

    [TestCase(false)] [TestCase(true)]
    public void CrossingAnActiveRotatingLaserKillsHumanAndBot(bool bot)
    {
        var heading = -17f * MathF.PI / 24f;
        var spot = new Vector2(MathF.Sin(heading) * 5f, -9.5f + MathF.Cos(heading) * 5f);
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(), PlayerRole = PartyRole.MainTank,
            Overrides = o =>
            {
                var settings = (FruUltimateRelativityStateOverrides)o;
                settings.RelativeNorth = 0; settings.ClockwiseMask = 255;
                settings.PracticeRole = PartyRole.MainTank; settings.PracticeAssignment = RelativityAssignment.ShortSupport;
            },
            Takeovers = [new(32f, spot, Bot: bot ? PartyRole.MainTank : null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Deaths.Any(d => d.ActionId == 40292), Is.True);
    }

    [TestCase(false)] [TestCase(true)]
    public void StandingInIceDonutKillsHumanAndBot(bool bot)
    {
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(), PlayerRole = PartyRole.MainTank,
            Overrides = o =>
            {
                var settings = (FruUltimateRelativityStateOverrides)o;
                settings.RelativeNorth = 0; settings.DpsIce = true;
                settings.PracticeRole = PartyRole.MainTank; settings.PracticeAssignment = RelativityAssignment.ShortSupport;
            },
            Takeovers = [new(34.8f, new(5f, 0f), Bot: bot ? PartyRole.MainTank : null)],
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Deaths.Any(d => d.ActionId == 40279), Is.True);
    }

    [TestCase(RelativityAssignment.ShortSupport, PartyRole.MainTank, 40274u)]
    [TestCase(RelativityAssignment.MidDps, PartyRole.CasterDps, 40271u)]
    public void BadReturnRecordCausesFinalEruptionOrWaterFailure(RelativityAssignment assignment, PartyRole role, uint action)
    {
        var spot = assignment == RelativityAssignment.MidDps ? new Vector2(8f, 0f) : new Vector2(0f, 1f);
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(), PlayerRole = PartyRole.OffTank,
            Overrides = o =>
            {
                var settings = (FruUltimateRelativityStateOverrides)o;
                settings.RelativeNorth = 0; settings.DpsIce = true;
                settings.PracticeRole = role; settings.PracticeAssignment = assignment;
            },
            Takeovers = [new(29.78f, spot, Bot: role)], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Deaths.Any(d => d.ActionId == action), Is.True);
    }

    [Test]
    public void LeavingFinalStackCausesShellCrusherFailure()
    {
        var run = ScenarioRun.Execute(typeof(FruUltimateRelativityScenario), 0, 0, new ScenarioRunOptions
        {
            ScenarioFactory = () => new FruUltimateRelativityScenario(),
            Takeovers = [new(61f, new(14f, 0f))], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False);
        Assert.That(run.Deaths.Any(d => d.ActionId == 40287), Is.True);
    }
}
