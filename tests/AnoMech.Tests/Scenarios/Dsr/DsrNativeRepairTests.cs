#pragma warning disable CA1416
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.P1Knights;
using AnoMech.Scenarios.Dsr.P4Eyes;
using AnoMech.Scenarios.Dsr.P5Wrath;
using AnoMech.Scenarios.Dsr.P5Death;
using System.Numerics;

namespace AnoMech.Tests;
public class DsrNativeRepairTests
{
    [Test]
    public void MenuNamesAndChronologyDoNotChangeSavedIdentities()
    {
        var scenarios = ScenarioCatalog.Create().Scenarios.Where(s => s.Phase.Zone is DsrZone).ToArray();
        Assert.That(scenarios.Select(s => s.DisplayName()), Is.EqualTo(new[] {
            "P1 The Holy See", "P2 Strength of the Ward 1", "P2 Strength of the Ward 2", "P2 Sanctity of the Ward 1", "P2 Sanctity of the Ward 2", "P3 Dive from Grace", "P4 Eyes", "P5 Wrath of the Heavens", "P5 Death of the Heavens", "P6 Wyrmsbreath 1 to Wroth Flames", "P6 Hallowed Wings 2 to Cauterize" }));
        Assert.That(scenarios[0].FullName(), Is.EqualTo("Dragonsong's Reprise (Ultimate) — P1 The Holy See The Holy See — South-first / HMRT (Elemental)"));
        Assert.That(scenarios[7].Name, Is.EqualTo("Wrath of the Heavens (Elemental)"));
        Assert.That(scenarios.Select(s => s.FullName()), Is.Unique);
        var catalog = ScenarioCatalog.Create();
        Assert.That(catalog.PhasesOf(DsrZone.Instance).SelectMany(catalog.ScenariosOf).Select(s => s.DisplayName()), Is.EqualTo(scenarios.Select(s => s.DisplayName())));
    }
    [TestCase(12601u, 131073u)] [TestCase(12602u, 65538u)] [TestCase(12611u, 131073u)]
    public void DemihumansHaveTheirNativeBodyAndWeapon(uint id, uint body)
    {
        var config = DsrActors.WithNativeAppearance(new(id));
        Assert.That(config.ModelCharaId, Is.EqualTo(id == 12611 ? 3269 : 3268));
        Assert.That(config.Appearance!.BodyModel, Is.EqualTo(body));
        Assert.That(config.Appearance.MainhandWeaponModel, Is.Not.Zero);
    }
    [TestCase(13117u, 3289u, 10223631u, 2)] [TestCase(13118u, 3290u, 74620u, 2)]
    [TestCase(13119u, 3291u, 74651u, 2)] [TestCase(13769u, 3499u, 74639u, 2)]
    public void StoryNpcAppearanceIncludesEquipmentAndElezenCustomization(uint id, uint model, uint body, int race)
    {
        var config = DsrActors.Config(id, default);
        Assert.That(config.ModelCharaId, Is.EqualTo(model));
        Assert.That(config.Appearance!.Equipment![1], Is.EqualTo(body));
        Assert.That(config.Appearance.Customize, Has.Length.EqualTo(26));
        Assert.That(config.Appearance.Customize![0], Is.EqualTo(race));
        Assert.That(config.Appearance.MainhandWeaponModel, Is.Not.Zero);
        Assert.That(config.IsHostile, Is.False);
    }
    [TestCase(typeof(DsrP1KnightsScenario), 1f, 12601u)]
    [TestCase(typeof(DsrP5WrathScenario), .2f, 12611u)]
    [TestCase(typeof(DsrP5DeathScenario), .2f, 12611u)]
    [TestCase(typeof(DsrP4EyesScenario), 13f, 13119u)]
    public void AllocatedActorsAreNotReadyUntilVisibleModelsAreLoaded(Type type, float at, uint id)
    {
        FakeGame? fake = null; var checkedActor = false;
        var run = ScenarioRun.Execute(type, 0, 0, new()
        {
            StopAt = at + .1f, ConfigureNative = f => fake = f,
            Probe = p =>
            {
                if (!p.Crossed(at)) return;
                var enemy = p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == id);
                var actor = fake!.BattleCharas.ActorAt(enemy.Proxy!.Slot)!;
                Assert.That(enemy.SpawnConfig.Appearance, Is.Not.Null);
                Assert.That(NativeActorReadiness.Check([enemy], "test"), Is.True);
                actor.HasLoadedModel = false;
                Assert.That(NativeActorReadiness.Check([enemy], "test"), Is.False);
                actor.HasLoadedModel = true; actor.HasDrawObject = false;
                Assert.That(NativeActorReadiness.Check([enemy], "test"), Is.False);
                actor.HasDrawObject = true; actor.IsDrawObjectVisible = false;
                Assert.That(NativeActorReadiness.Check([enemy], "test"), Is.False);
                actor.IsDrawObjectVisible = true; checkedActor = true;
            },
        });
        Assert.That(run.Passed, run.ToString); Assert.That(checkedActor, Is.True);
    }
    [TestCase(PartyRole.MainTank)] [TestCase(PartyRole.RegenHealer)] [TestCase(PartyRole.CasterDps)]
    public void HumanAndBotsArePhysicallyPushedBeforeOutwardMovement(PartyRole role)
    {
        Vector3 before = default; var checkedPush = false;
        var botBefore = new Dictionary<PartyRole, Vector3>();
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 0, new()
        {
            PlayerRole = role, Takeovers = [new(81.8f, null)], StopAt = 83.25f,
            Probe = p =>
            {
                if (p.Crossed(81.9f))
                {
                    before = p.Member(role)!.Position;
                    foreach (var member in p.World.Party.ActiveMembers()) botBefore[((ISimPartyMember)member).Role] = member.Position;
                }
                if (!p.Crossed(83.2f)) return;
                var after = p.Member(role)!.Position;
                Assert.That(Vector3.Distance(before, after), Is.EqualTo(16).Within(.1));
                Assert.That(Vector3.Dot(Vector3.Normalize(before), Vector3.Normalize(after)), Is.GreaterThan(.99));
                Assert.That(p.World.Children.OfType<SimTether>().Where(t => t.IsActive && t.TetherId == 9), Is.Empty);
                foreach (var member in p.World.Party.ActiveMembers().Where(m => ((ISimPartyMember)m).Role != role))
                {
                    var start = botBefore[((ISimPartyMember)member).Role];
                    Assert.That(Vector3.Distance(start, member.Position), Is.InRange(15.9f, 20f));
                }
                checkedPush = true;
            },
        });
        Assert.That(run.Passed, run.ToString); Assert.That(checkedPush, Is.True);
    }

    [Test]
    public void AntiKnockbackPreventsDisplacementAndLeavesChainsUnbroken()
    {
        var before = new Dictionary<PartyRole, Vector3>();
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 0, new()
        {
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (p.Crossed(81.9f))
                    foreach (var member in p.World.Party.ActiveMembers())
                    { member.AddStatus(1209, 6); before[((ISimPartyMember)member).Role] = member.Position; }
                if (!p.Crossed(82.4f)) return;
                foreach (var member in p.World.Party.ActiveMembers())
                    Assert.That(Vector3.Distance(member.Position, before[((ISimPartyMember)member).Role]), Is.LessThan(.1f));
                Assert.That(p.World.Children.OfType<SimTether>().Count(t => t.IsActive && t.TetherId == 9), Is.EqualTo(4));
            },
        });
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("Chain not broken")), Is.True, run.ToString);
    }
    [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
    public void PuddleBleedAllowsEscapeButKillsSustainedExposure(bool lateEntry, bool leave)
    {
        var pool = Vector3.Zero; var checkedStack = false;
        var run = ScenarioRun.Execute(typeof(AnoMech.Scenarios.Dsr.P6DoubleDragons.DsrP6DoubleDragonsScenario), 0, 0, new()
        {
            StopAt = 91.7f, PlayerRole = PartyRole.CasterDps, Takeovers = [new(84.35f, null)],
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (p.Crossed(84.4f))
                {
                    var objects = p.World.Children.OfType<SimEventObject>().Where(o => o.EObjRowId == 0x1EB683).ToArray();
                    Assert.That(objects.Length, Is.EqualTo(4)); pool = objects[^1].Position;
                    Assert.That(p.World.Party.ActiveMembers().All(m => !m.Dead), Is.True);
                    p.Member(PartyRole.CasterDps)!.SetPosition(!leave || lateEntry ? Vector3.Zero : pool);
                    checkedStack = true;
                }
                if (!leave && p.Crossed(lateEntry ? 88.4f : 88f)) p.Member(PartyRole.CasterDps)!.SetPosition(pool);
                if (leave && lateEntry && p.Crossed(86.3f)) p.Member(PartyRole.CasterDps)!.SetPosition(pool);
                if (leave && p.Crossed(lateEntry ? 86.9f : 85.1f)) p.Member(PartyRole.CasterDps)!.SetPosition(Vector3.Zero);
            },
        });
        Assert.That(checkedStack, Is.True);
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("bleed exposure")), Is.EqualTo(!leave), run.ToString);
        if (leave) Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void RestartDuringActiveBleedRemovesStatusPoolsAndExposure()
    {
        var scenario = new AnoMech.Scenarios.Dsr.P6DoubleDragons.DsrP6DoubleDragonsScenario();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            FakeGame? fake = null; var checkedBleed = false;
            var run = ScenarioRun.Execute(scenario.GetType(), 0, 0, new()
            {
                ScenarioFactory = () => scenario, PlayerRole = PartyRole.CasterDps, StopAt = 89.3f,
                Takeovers = [new(87.9f, null)], ConfigureNative = f => fake = f,
                Probe = p =>
                {
                    if (p.Crossed(88f)) p.Member(PartyRole.CasterDps)!.SetPosition(p.World.Children.OfType<SimEventObject>().Last(o => o.EObjRowId == 0x1EB683).Position);
                    if (!p.Crossed(89.2f)) return;
                    Assert.That(p.Member(PartyRole.CasterDps)!.HasStatus(0xB87), Is.True); checkedBleed = true;
                },
            });
            Assert.That(run.Passed, run.ToString); Assert.That(checkedBleed, Is.True);
            Assert.That(fake!.BattleCharas.OccupiedSlots, Is.Zero);
            Assert.That(fake.BattleCharas.Player.Statuses, Is.Empty);
        }
    }

    [Test]
    public void IncorrectPairDirectionsRemainChainedAfterRealKnockback()
    {
        var scenario = new DsrP1KnightsScenario();
        SimCharacter? first = null, second = null; var baseline = 0f; var checkedChain = false;
        var run = ScenarioRun.Execute(typeof(DsrP1KnightsScenario), 0, 0, new()
        {
            ScenarioFactory = () => scenario, WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (p.Crossed(80.8f))
                {
                    var pair = scenario.LastState!.ChainPairs().First();
                    first = p.Member(pair.A)!; second = p.Member(pair.B)!;
                    first.StopMoving(); second.StopMoving();
                    first.SetPosition(new Vector3(1, 0, -2)); second.SetPosition(new Vector3(2, 0, -1));
                }
                if (p.Crossed(81f)) baseline = Vector3.Distance(first!.Position, second!.Position);
                if (!p.Crossed(82.9f)) return;
                Assert.That(first!.Position.Length(), Is.GreaterThan(15));
                Assert.That(second!.Position.Length(), Is.GreaterThan(15));
                Assert.That(Vector3.Distance(first.Position, second.Position), Is.LessThan(baseline + 30));
                Assert.That(p.World.Children.OfType<SimTether>().Any(t => t.IsActive && (t.A == first || t.B == first) && t.TetherId == 9), Is.True);
                checkedChain = true;
            },
        });
        Assert.That(checkedChain, Is.True);
        Assert.That(run.Deaths.Any(d => d.Cause.Contains("Chain not broken")), Is.True, run.ToString);
    }
}

