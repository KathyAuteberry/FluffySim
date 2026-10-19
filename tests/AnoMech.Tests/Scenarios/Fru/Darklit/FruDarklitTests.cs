using AnoMech.Core.SimObjects;
#pragma warning disable CA1416
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.Darklit;

namespace AnoMech.Tests;

public class FruDarklitTests
{
    [TestCase(false)] [TestCase(true)]
    public void NativeConeSourcesAndPacketsFaceTheAuthoritativeTargets(bool incorrectTowerBait)
    {
        var scenario = new FruDarklitScenario();
        var observed = false;
        ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, PlayerRole = PartyRole.RegenHealer, StopAt = 19.17f,
            Overrides = o => { var x = (FruDarklitStateOverrides)o; x.Advanced = true; x.ExactPattern = 0; },
            Takeovers = incorrectTowerBait ? [new(18.7f, new Vector2(0, -4))] : [],
            Probe = p =>
            {
                if (!p.Crossed(19.13f)) return;
                var requests = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>()
                    .SelectMany(e => ((FakeBattleChara)e.Proxy!).EffectSourceSnapshots)
                    .Where(r => r.Effect.ActionId == 40190).ToArray();
                Assert.That(requests.Length, Is.EqualTo(4));
                Assert.That(scenario.PathNativeSources.Count, Is.EqualTo(4));
                Assert.That(scenario.PathSnapshotCandidates.Count, Is.EqualTo(8));
                foreach (var cone in scenario.PathCones)
                {
                    var target = p.World.Party.Get(cone.Target)!;
                    var request = requests.Single(r => r.Effect.Targets?.Any(t => t.Id == target.GameObjectId) == true);
                    Assert.That(MathF.Atan2(MathF.Sin(request.Rotation - cone.Source.Rotation), MathF.Cos(request.Rotation - cone.Source.Rotation)), Is.EqualTo(0).Within(.0001f));
                    Assert.That(request.Effect.Rotation, Is.EqualTo(cone.Source.Rotation).Within(.0001f));
                    Assert.That(request.Position, Is.EqualTo(cone.Source.Position + p.World.ScenarioOrigin));
                }
                Assert.That(scenario.PathTargets.Contains(PartyRole.RegenHealer), Is.EqualTo(incorrectTowerBait));
                observed = true;
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(observed, Is.True);
        Assert.That(scenario.PathNativeSources, Is.Empty);
        Assert.That(scenario.PathCones, Is.Empty);
        Assert.That(scenario.PathSnapshotCandidates, Is.Empty);
    }

    [Test]
    public void DisplayLabelsDoNotChangeSavedScenarioIdentity()
    {
        var scenario = new FruDarklitScenario();
        Assert.That(AnoMech.Core.Game.Game.DisplayName(scenario), Is.EqualTo("P4 Darklit Dragonsong"));
        Assert.That(AnoMech.Core.Game.Game.FullName(scenario), Is.EqualTo("Futures Rewritten (Ultimate) — P4 Darklit Dragonsong (Mana)"));
        Assert.That(scenario.Phase.Zone.DisplayName, Is.EqualTo("Futures Rewritten"));
        AnoMech.Scenarios.IZone dsr = AnoMech.Scenarios.Dsr.DsrZone.Instance;
        Assert.That(dsr.DisplayName, Is.EqualTo("Dragonsong's Reprise"));
        Assert.That(dsr.Name, Is.EqualTo("Dragonsong's Reprise (Ultimate)"));
    }

    [Test]
    public void PathTargetsAreTheActualClosestFourToMovedShivaAtResolution()
    {
        var scenario = new FruDarklitScenario();
        PartyRole[] expected = [];
        ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, StopAt = 19.15f,
            Probe = p =>
            {
                if (p.Crossed(18.9f))
                {
                    var shiva = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Single(e => e.BNpcBaseId == 17833);
                    shiva.SetPosition(new Vector3(10, 0, 0));
                    expected = p.World.Party.ActiveMembers().OrderBy(m => FruDarklitScenario.PathDistance(shiva.Position, m.Position, shiva.HitboxRadius, m.HitboxRadius))
                        .Take(4).Select(m => ((AnoMech.Core.SimObjects.ISimPartyMember)m).Role).ToArray();
                }
                if (p.Crossed(19.12f)) Assert.That(scenario.PathTargets, Is.EqualTo(expected));
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(scenario.PathTargets.Count, Is.EqualTo(4));
    }

    [Test]
    public void LegacyTowerPreferenceCannotDisableConfirmedNativeTowers()
    {
        var scenario = new FruDarklitScenario();
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 1, new()
        {
            ScenarioFactory = () => scenario, StopAt = 11,
            Overrides = o => ((FruDarklitStateOverrides)o).TowerRendering = DarklitTowerRendering.PracticeOverlay,
            Probe = p =>
            {
                if (!p.Crossed(10.9f)) return;
                Assert.That(scenario.ActiveTowerPositions.Count, Is.EqualTo(2));
                var effects = ((FakeMapEffects)AnoMech.Core.Native.Interfaces.Natives.MapEffects).Applied;
                Assert.That(effects, Does.Contain((0x00020001u, (byte)0x2A)));
                Assert.That(effects, Does.Contain((0x00020001u, (byte)0x2B)));
            },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void TowerCornersAreComfortablyInsideAndOutsideTheConeBaiters()
    {
        FakeGame.Install();
        foreach (var pattern in FruDarklitState.Patterns)
        {
            var assignments = new DarklitManaAssignments(pattern);
            var nearest = FruDarklitState.Roles.OrderBy(r => assignments.Formation(r).LengthSquared()).Take(4);
            Assert.That(nearest.All(r => !pattern.IsTethered(r)), Is.True);
            foreach (var role in pattern.Tethered)
            {
                var position = assignments.Formation(role);
                Assert.That(Vector3.Distance(position, FruDarklitScenario.TowerPositions[assignments.South(role) ? 1 : 0]), Is.LessThan(3.5f));
                Assert.That(MathF.Abs(position.Z), Is.GreaterThan(8));
            }
            for (var i = 0; i < 4; i++)
                Assert.That(FruDarklitRules.FateFails(assignments.Formation(pattern.ChainRing[i]), assignments.Formation(pattern.ChainRing[(i + 1) % 4])), Is.False);
        }
    }

    [Test]
    public void PathDistanceUsesSourceGroundPlaneAndHitboxes()
    {
        Assert.That(FruDarklitScenario.PathDistance(new(10, 20, 5), new(13, 90, 9), 2, .5f), Is.EqualTo(2.5f));
        Assert.That(FruDarklitScenario.PathDistance(new(10, 0, 5), new(10, 0, 5), 2, .5f), Is.Zero);
    }

    [Test]
    public void LocalPracticeInvulnerabilityProtectsTheHumanWithoutAnAbilityPress()
    {
        var scenario = new FruDarklitScenario();
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, ScenarioFactory = () => scenario,
            Overrides = o => ((FruDarklitStateOverrides)o).BaitTank = PartyRole.MainTank,
            Probe = p =>
            {
                if (p.Crossed(29.1f))
                {
                    p.World.Party.Player!.RemoveStatus(82);
                    p.World.Party.Player.RemoveStatus(409);
                    p.World.Party.Player.RemoveStatus(810);
                    p.World.Party.Player.RemoveStatus(1836);
                    var proxy = (FakeBattleChara)p.World.Party.Player.Proxy!;
                    var effectsBefore = proxy.ReceivedEffects.Count;
                    var statusesBefore = p.World.Party.Player.ActiveStatuses.ToArray();
                    Assert.That(p.World.PracticeInvulnerability.Activate(true), Is.True);
                    Assert.That(proxy.ReceivedEffects.Count, Is.EqualTo(effectsBefore));
                    Assert.That(p.World.Party.Player.ActiveStatuses, Is.EqualTo(statusesBefore));
                }
                if (p.Crossed(30)) Assert.That(p.World.PracticeInvulnerability.Active, Is.True);
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Completed, Is.True);
    }

    [Test]
    public void EveryLegitimateGraphAndWaterPairHasACompleteManaAssignment()
    {
        FakeGame.Install();
        Assert.That(FruDarklitState.Patterns.Length, Is.EqualTo(1152));
        foreach (var p in FruDarklitState.Patterns)
        {
            var a = new DarklitManaAssignments(p);
            Assert.That(p.Tethered.Distinct().Count(), Is.EqualTo(4));
            Assert.That(p.Tethered.Count(r => r.IsTank()), Is.EqualTo(1));
            Assert.That(p.Tethered.Count(r => r.IsDps()), Is.EqualTo(2));
            Assert.That(p.IsTethered(p.TetherWater), Is.True);
            Assert.That(p.IsTethered(p.UntetherWater), Is.False);
            Assert.That(a.South(p.TetherWater), Is.Not.EqualTo(a.South(p.UntetherWater)));
            Assert.That(a.Corner(p.Healer), Is.Zero);
            Assert.That(p.Tethered.Count(a.South), Is.EqualTo(2));
            for (var i = 0; i < 4; i++) Assert.That(a.South(p.ChainRing[i]), Is.Not.EqualTo(a.South(p.ChainRing[(i + 1) % 4])));
            foreach (var r in FruDarklitState.Roles)
            {
                Assert.That(a.SpiritSpread(r).Length(), Is.LessThan(20));
                Assert.That(FruDarklitRules.SpiritHitsFragment(a.SpiritSpread(r), FruDarklitScenario.FragmentPosition), Is.False);
            }
        }
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    [TestCase(37)] [TestCase(100)] [TestCase(200)] [TestCase(350)] [TestCase(500)] [TestCase(900)] [TestCase(1151)]
    public void CompleteRun(int index)
    {
        var scenario = new FruDarklitScenario();
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, index, new()
        {
            ScenarioFactory = () => scenario,
            Overrides = o => { var x = (FruDarklitStateOverrides)o; x.Advanced = true; x.ExactPattern = index; },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(scenario.Completed, Is.True);
        Assert.That(scenario.FragmentCompromised, Is.False);
        Assert.That(scenario.ResolvedTowers, Is.EqualTo(2));
        Assert.That(scenario.ResolvedCones, Is.EqualTo(4));
        Assert.That(scenario.ResolvedSomberHits, Is.EqualTo(2));
        Assert.That(scenario.SomberTargets, Is.EqualTo(new[] { scenario.LastState!.BaitTank, scenario.LastState.BaitTank }));
    }
    private static IEnumerable<TestCaseData> FullPatterns() => from index in Enumerable.Range(0, 1152)
        from east in new[] { false, true } select new TestCaseData(index, east);
    [TestCaseSource(nameof(FullPatterns))]
    public void EveryPatternAndWingCompletes(int index, bool east)
    {
        var scenario = new FruDarklitScenario();
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, index, new()
        {
            ScenarioFactory = () => scenario,
            Overrides = o => { var x = (FruDarklitStateOverrides)o; x.Advanced = true; x.ExactPattern = index; x.EastWing = east; x.SpiritTarget = (PartyRole)(index % 8); },
            WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString); Assert.That(scenario.Completed, Is.True);
        Assert.That(scenario.FragmentHealth, Is.EqualTo(100));
        Assert.That(scenario.ResolvedSomberHits, Is.EqualTo(2));
    }
    [Test]
    public void PracticeFiltersWorkForEverySeatWithoutChangingAssignmentValidity()
    {
        foreach (var role in FruDarklitState.Roles)
        foreach (var choice in Enum.GetValues<DarklitPracticeAssignment>())
        foreach (var shape in Enum.GetValues<DarklitTetherPattern>())
        foreach (var water in new[] { false, true })
        {
            var settings = new FruDarklitStateOverrides { PracticeAssignment = choice, TetherPattern = shape, WaterAdjustment = water };
            var state = new FruDarklitState(new Rng(71), settings, role, PartyRole.MainTank);
            var p = state.Pattern;
            Assert.That(p.Shape, Is.EqualTo(shape)); Assert.That(new DarklitManaAssignments(p).WaterAdjusted, Is.EqualTo(water));
            if (choice is DarklitPracticeAssignment.Tethered or DarklitPracticeAssignment.TetheredWater) Assert.That(p.IsTethered(role), Is.True);
            if (choice is DarklitPracticeAssignment.Untethered or DarklitPracticeAssignment.UntetheredWater) Assert.That(p.IsTethered(role), Is.False);
            if (choice is DarklitPracticeAssignment.TetheredWater or DarklitPracticeAssignment.UntetheredWater) Assert.That(p.HasWater(role), Is.True);
        }
    }
    [Test]
    public void DpsSideAdjustmentsAndDistinctGraphCorrectionsAreVisibleResponsibilities()
    {
        foreach (var p in FruDarklitState.Patterns)
        {
            var a = new DarklitManaAssignments(p);
            Assert.That(a.SideAdjustedLineup(PartyRole.MeleeDpsB) == a.Lineup(PartyRole.PhysRangedDps), Is.EqualTo(p.RequiresDpsSideSwap));
            Assert.That(a.SideAdjustedLineup(PartyRole.PhysRangedDps) == a.Lineup(PartyRole.MeleeDpsB), Is.EqualTo(p.RequiresDpsSideSwap));
            Assert.That(a.Corner(p.Tank), Is.EqualTo(p.Shape == DarklitTetherPattern.Bowtie ? 1 : p.Shape == DarklitTetherPattern.Box ? 2 : 3));
            Assert.That(a.Corner(p.Healer), Is.Zero);
            var neighbors = new[] { p.ChainRing[1], p.ChainRing[3] };
            Assert.That(neighbors.All(a.South), Is.True);
            if (a.WaterAdjusted) Assert.That(a.South(p.UntetherWater), Is.Not.EqualTo(a.South(a.WaterPartner!.Value)));
        }
    }
    [TestCase(false)] [TestCase(true)]
    public void SpiritTakerChecksTheActualLandingAgainstFragmentHp(bool badBait)
    {
        var scenario = new FruDarklitScenario(); var checkedLanding = false;
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, ScenarioFactory = () => scenario,
            Overrides = o => { var x = (FruDarklitStateOverrides)o; x.Advanced = true; x.ExactPattern = 0; x.SpiritTarget = PartyRole.PhysRangedDps; },
            Takeovers = badBait ? [new(21.24f, new Vector2(0, -16))] : [],
            StopAt = 22.1f, WriteArtifactsOnFailure = false,
            Probe = p => { if (p.Crossed(22f)) { checkedLanding = true; Assert.That(scenario.FragmentHealth, Is.EqualTo(badBait ? 75 : 100)); Assert.That(scenario.FragmentCompromised, Is.EqualTo(badBait)); } },
        });
        Assert.That(run.Passed, run.ToString); Assert.That(checkedLanding, Is.True);
    }
    [Test]
    public void MovingAfterSpiritSnapshotDoesNotMoveItsImpactOntoTheFragment()
    {
        var scenario = new FruDarklitScenario();
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.PhysRangedDps, ScenarioFactory = () => scenario,
            Overrides = o => { var x = (FruDarklitStateOverrides)o; x.Advanced = true; x.ExactPattern = 0; x.SpiritTarget = PartyRole.PhysRangedDps; },
            Takeovers = [new(21.5f, new Vector2(0, -16))], StopAt = 22.1f, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString); Assert.That(scenario.FragmentCompromised, Is.False);
        Assert.That(Vector3.Distance(scenario.SpiritLanding, FruDarklitScenario.FragmentPosition), Is.GreaterThan(8.5));
    }
    [TestCase(17f, 0f, 0f, 40213u)]
    [TestCase(15f, 0f, 5f, 40215u)]
    [TestCase(15f, 0f, -19f, 40215u)]
    [TestCase(26.27f, 7f, 7f, 40228u)]
    [TestCase(25.99f, 0f, 0f, 40271u)]
    public void WrongHumanPositionsFailTheRealMechanic(float at, float x, float z, uint action)
    {
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            PlayerRole = action is 40228 or 40271 ? PartyRole.PhysRangedDps : PartyRole.RegenHealer,
            Overrides = o => { var s = (FruDarklitStateOverrides)o; s.Advanced = true; s.ExactPattern = 0; s.SpiritTarget = PartyRole.PhysRangedDps; s.EastWing = true; },
            Takeovers = [new(at, new Vector2(x, z))], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False); Assert.That(run.Deaths.Any(d => d.ActionId == action), Is.True, run.ToString);
    }
    [Test]
    public void UnmitigatedHumanTankCannotPassSomberDance()
    {
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.MainTank, Takeovers = [new(28.99f, null)], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.Role == PartyRole.MainTank && d.ActionId == 40284), Is.True, run.ToString);
    }
    [TestCase(.05f)] [TestCase(12f)] [TestCase(16f)] [TestCase(22f)] [TestCase(31f)]
    public void CancellationAndRestartReleaseNativeOwnership(float stop)
    {
        FakeGame? fake = null;
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 17, new() { StopAt = stop, ConfigureNative = f => fake = f, WriteArtifactsOnFailure = false });
        Assert.That(run.Passed, run.ToString); Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
        CompleteRun(17);
    }
    [Test]
    public void ExactSettingsAndSeedsDoNotUseIndependentVisualOrBotRandomness()
    {
        var o = new FruDarklitStateOverrides { Advanced = true, ExactPattern = 42, SpiritTarget = PartyRole.CasterDps,
            TetherPattern = DarklitTetherPattern.Bowtie, PracticeAssignment = DarklitPracticeAssignment.TetheredWater, WaterAdjustment = false, EastWing = true };
        var a = new FruDarklitState(new Rng(71), o, PartyRole.RegenHealer, PartyRole.MainTank);
        var b = new FruDarklitState(new Rng(71), o, PartyRole.RegenHealer, PartyRole.MainTank);
        Assert.That(a.Pattern, Is.EqualTo(b.Pattern)); Assert.That(a.PatternIndex, Is.EqualTo(42));
        Assert.That(a.SpiritTarget, Is.EqualTo(PartyRole.CasterDps)); Assert.That(a.EastWing, Is.True);
    }

    [Test]
    public void NativeOpeningMatchesTheRealRecordingAndOwnsVisibleActors()
    {
        var scenario = new FruDarklitScenario(); FakeGame? fake = null;
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 17, new()
        {
            ScenarioFactory = () => scenario, ConfigureNative = f => { fake = f; f.BattleCharas.PacketSpawnDelayFrames = 5; },
            Probe = p =>
            {
                if (p.Crossed(2.2f))
                {
                    var bosses = p.World.Children.OfType<AnoMech.Core.SimObjects.SimEnemy>().Where(a => (a.SpawnConfig.Visibility == SpawnVisibility.Visible)).ToArray();
                    Assert.That(bosses.Select(a => a.BNpcBaseId).Order(), Is.EqualTo(new uint[] { 17833, 17835, 17841 }));
                    Assert.That(bosses.All(a => a.Proxy!.IsDrawObjectVisible), Is.True);
                    Assert.That(bosses.Single(a => a.BNpcBaseId == 17835).SpawnConfig.Appearance!.BodyModel, Is.EqualTo(65537));
                    Assert.That(scenario.Phase.Weather, Is.EqualTo(105)); Assert.That(scenario.Phase.Bgm, Is.EqualTo(801));
                }
                if (p.Crossed(11f))
                {
                    Assert.That(scenario.ActiveTowerPositions, Is.EqualTo(FruDarklitScenario.TowerPositions));
                    var maps = ((FakeMapEffects)AnoMech.Core.Native.Interfaces.Natives.MapEffects).Applied;
                    foreach (var slot in new byte[] { 0x2A, 0x2B }) Assert.That(maps, Does.Contain((0x00020001u, slot)));
                }
                if (p.Crossed(20f))
                {
                    Assert.That(p.World.Party.ActiveMembers().Select(m => m.FindStatus(2257)?.Stacks), Is.All.EqualTo(4));
                    Assert.That(scenario.ActiveTowerPositions, Is.Empty);
                }
                if (p.Crossed(30f)) Assert.That(scenario.SomberVulnerable, Does.Contain(scenario.LastState!.BaitTank));
            }, WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, run.ToString); Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
        var tower = scenario.Resolutions.Single(r => r.Action == 40213).Time - 2.1f;
        var cones = scenario.Resolutions.Single(r => r.Action == 40190).Time - 2.1f;
        Assert.That(tower, Is.EqualTo(16.149).Within(.1)); Assert.That(cones, Is.EqualTo(16.995).Within(.1));
        Assert.That(scenario.SomberVulnerable, Is.Empty);
    }
    [Test]
    public void CurrentProvisionalTetherIntervalRejectsBothDistanceFailures()
    {
        Assert.That(FruDarklitRules.FateFails(Vector3.Zero, new Vector3(7.99f, 0, 0)), Is.True);
        Assert.That(FruDarklitRules.FateFails(Vector3.Zero, new Vector3(26.01f, 0, 0)), Is.True);
        Assert.That(FruDarklitRules.FateFails(Vector3.Zero, new Vector3(16, 0, 0)), Is.False);
        Assert.That(FruDarklitRules.FateFails(Vector3.Zero, new Vector3(16, 0, 0), false), Is.True);
    }
    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.OffTank)]
    public void BothTankBaitChoicesCanClearWithoutImmunityExemptions(PartyRole tank)
    {
        for (var index = 0; index < 1152; index += 97)
        {
            var scenario = new FruDarklitScenario();
            var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, index, new()
            { ScenarioFactory = () => scenario, Overrides = o => { var x = (FruDarklitStateOverrides)o; x.Advanced = true; x.ExactPattern = index; x.BaitTank = tank; }, WriteArtifactsOnFailure = false });
            Assert.That(run.Passed, run.ToString); Assert.That(scenario.SomberTargets, Is.EqualTo(new[] { tank, tank }));
        }
    }
    [Test]
    public void IncorrectConeBaitOverloadsLightsteepedAndFailsTheParty()
    {
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.RegenHealer,
            Overrides = o => { var x = (FruDarklitStateOverrides)o; x.Advanced = true; x.ExactPattern = 0; },
            Takeovers = [new(18.7f, new Vector2(0, -4))], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Deaths.Any(d => d.ActionId == 40216), Is.True, run.ToString);
    }
    [Test]
    public void CatalogPlacesTheFullProvisionalP4FragmentAfterApocalypse()
    {
        var catalog = ScenarioCatalog.Create().Scenarios;
        Assert.That(catalog[6], Is.TypeOf<FruDarklitScenario>()); Assert.That(catalog[6].IsIncomplete, Is.True);
        Assert.That(PhaseMusic.TrackFor(catalog[6]), Is.EqualTo(801));
        Assert.That(catalog[6].SettingsOverrides, Is.TypeOf<FruDarklitStateOverrides>());
        Assert.That(catalog[6].Phase.Zone, Is.SameAs(catalog[5].Phase.Zone));
    }

    [Test]
    public void FragmentDamageByABotTargetPreventsClearAndStillCleansUp()
    {
        var scenario = new FruDarklitScenario(); FakeGame? fake = null;
        var run = ScenarioRun.Execute(typeof(FruDarklitScenario), 0, 0, new()
        {
            PlayerRole = PartyRole.CasterDps, ScenarioFactory = () => scenario, ConfigureNative = f => fake = f,
            Overrides = o => { var x = (FruDarklitStateOverrides)o; x.Advanced = true; x.ExactPattern = 0; x.SpiritTarget = PartyRole.PhysRangedDps; },
            Takeovers = [new(21.24f, new Vector2(0, -16), Bot: PartyRole.PhysRangedDps)], WriteArtifactsOnFailure = false,
        });
        Assert.That(run.Passed, Is.False); Assert.That(run.Failure, Is.EqualTo("mechanic mistake"));
        Assert.That(run.Deaths, Is.Empty); Assert.That(scenario.FragmentCompromised, Is.True);
        Assert.That(scenario.Completed, Is.False); Assert.That(scenario.FragmentHealth, Is.EqualTo(75));
        Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero); Assert.That(scenario.ActiveTowerPositions, Is.Empty);
        scenario.Tick(.1f, 99); Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
    }

}
