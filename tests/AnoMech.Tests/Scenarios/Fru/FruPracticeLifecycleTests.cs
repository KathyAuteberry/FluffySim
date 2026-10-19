#pragma warning disable CA1416
using System.Reflection;
using Dalamud.Plugin.Services;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru.Pandora;
using AnoMech.Scenarios.Fru.Upstream.P4CrystallizeTime;
using AnoMech.Scenarios.Fru.Upstream.P5FulgentBlade;
using AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained;

namespace AnoMech.Tests;

public class FruPracticeLifecycleTests
{
    public static IEnumerable<TestCaseData> Entries()
    {
        yield return new(typeof(AnoMech.Scenarios.Fru.DiamondDust.FruDiamondDustScenario), 0, 42f);
        yield return new(typeof(AnoMech.Scenarios.Fru.MirrorMirror.FruMirrorMirrorScenario), 0, 20f);
        yield return new(typeof(FruP4CrystallizeTimeScenario), 1, 12f);
        yield return new(typeof(FruP5FulgentBladeScenario), 0, 20f);
        yield return new(typeof(FruP5ParadiseRegainedScenario), 0, 15f);
        yield return new(typeof(FruPolarizingStrikesScenario), 0, 10f);
        yield return new(typeof(FruPandorasBoxScenario), 0, 10.5f);
        yield return new(typeof(FruPandoraSequenceScenario), 0, 115f);
    }

    [TestCaseSource(nameof(Entries))]
    public void PauseFreezesMechanicClockAndStatusesThenBotsFinish(Type type, int strat, float pauseAt)
    {
        FakeGame native = null!;
        var paused = false;
        var run = ScenarioRun.Execute(type, strat, 47, new()
        {
            ConfigureNative = fake => native = fake,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (paused || !p.Crossed(pauseAt)) return;
                paused = true;
                var elapsed = p.Game.EventClockNow;
                var statuses = Statuses(p.World);
                var positions = p.World.Party.AllMembers().Select(m => m.Position).ToArray();
                p.Game.Paused = true;
                Frames(native, p.Game, 180);
                Assert.That(p.Game.EventClockNow, Is.EqualTo(elapsed));
                p.Game.Paused = false;
                Assert.That(Statuses(p.World), Is.EqualTo(statuses));
                Assert.That(p.World.Party.AllMembers().Select(m => m.Position), Is.EqualTo(positions));
            },
        });
        Assert.That(paused, Is.True, run.ToString);
        Assert.That(run.Passed, Is.True, run.ToString);
    }

    [TestCaseSource(nameof(Entries))]
    public void SameSeedRestartsDiscardRunStateAndIdleTime(Type type, int strat, float resetAt)
    {
        FakeGame native = null!;
        var scenario = (IScenario)Activator.CreateInstance(type)!;
        var restarted = false;
        uint[] baseline = [];
        var gauge = 0;
        var run = ScenarioRun.Execute(type, strat, 47, new()
        {
            ScenarioFactory = () => scenario,
            PlayerRole = PartyRole.MeleeDpsA,
            ConfigureNative = fake => native = fake,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (restarted) return;
                if (p.Crossed(1f))
                {
                    baseline = Actors(p.World);
                    gauge = p.World.Party.LimitBreak.FilledBars;
                }
                if (!p.Crossed(resetAt)) return;
                restarted = true;
                for (var repeat = 0; repeat < 2; repeat++)
                {
                    p.Game.Reset();
                    native.Frame(0f, p.Game.Tick);
                    Assert.That(p.World.Children.OfType<SimEnemy>().Any(e => e.IsActive), Is.False);
                    Assert.That(p.World.Party.AllMembers().SelectMany(m => m.ActiveStatuses), Is.Empty);
                    Frames(native, p.Game, 180);
                    var mapEffectStart = native.MapEffects.Applied.Count;
                    p.Game.EnterAndStart(new RunScenarioParams(scenario,
                        new LocalPartyProvider { RoleOverride = PartyRole.MeleeDpsA }, strat, 0, 47), SessionMode.Local);
                    native.Frame(ScenarioRun.FrameSeconds, p.Game.Tick);
                    Assert.That(p.Game.ActiveScenario, Is.SameAs(scenario));
                    Assert.That(p.Game.EventClockNow, Is.LessThan(.1f));
                    while (p.Game.EventClockNow < 1f) native.Frame(ScenarioRun.FrameSeconds, p.Game.Tick);
                    Assert.That(Actors(p.World), Is.EqualTo(baseline));
                    Assert.That(p.World.Party.AllMembers().Count(), Is.EqualTo(8));
                    Assert.That(p.World.Party.LimitBreak.FilledBars, Is.EqualTo(gauge));
                    if (type == typeof(FruP5ParadiseRegainedScenario))
                    {
                        while (p.Game.EventClockNow < 5.8f) native.Frame(ScenarioRun.FrameSeconds, p.Game.Tick);
                        Assert.That(native.MapEffects.Applied.Skip(mapEffectStart)
                            .Where(effect => effect.Index >= 51 && effect.Index <= 53 && effect.Flags != 0x00040004), Is.Empty,
                            "Old tower occupancy must not be emitted before new towers spawn");
                    }
                    if (repeat == 0)
                        while (p.Game.EventClockNow < resetAt) native.Frame(ScenarioRun.FrameSeconds, p.Game.Tick);
                }
            },
        });
        Assert.That(restarted, Is.True, run.ToString);
        Assert.That(run.Passed, Is.True, run.ToString);
    }

    [TestCaseSource(nameof(Entries))]
    public void OfflineColdEntryWaitsForLayoutAndDelayedPartyThenLeavesCleanly(Type type, int strat, float unused)
    {
        var fake = FakeGame.Install();
        fake.MapEffects.TerritoryReady = false;
        fake.BattleCharas.PacketSpawnDelayFrames = 5;
        DalamudServices.Install(nameof(Plugin.ClientState), DispatchProxy.Create<IClientState, DalamudServices.NullService>());
        DalamudServices.Install(nameof(Plugin.Config), new Configuration());
        using var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        var scenario = (IScenario)Activator.CreateInstance(type)!;
        game.EnterAndStart(new RunScenarioParams(scenario,
            new LocalPartyProvider { RoleOverride = PartyRole.MeleeDpsA }, strat, 0, 47), SessionMode.Local);
        Frames(fake, game, 240);
        Assert.That(game.IsPreparingScenario, Is.True);
        Assert.That(game.ActiveScenario, Is.Null);
        Assert.That(game.EventClockNow, Is.Zero);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(fake.MapEffects.Applied, Is.Empty);
        fake.MapEffects.TerritoryReady = true;
        for (var i = 0; i < 240 && game.ActiveScenario is null; i++) Frames(fake, game, 1);
        Assert.That(game.ActiveScenario, Is.SameAs(scenario));
        Assert.That(game.EventClockNow, Is.LessThan(.1f));
        Frames(fake, game, 30);
        Assert.That(game.World.Party.AllMembers().Count(), Is.EqualTo(8));
        Assert.That(game.World.Party.AllMembers().OfType<SimPartyNpc>().All(m => m.Proxy is { Exists: true }), Is.True);
        game.Leave();
        Frames(fake, game, 2);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(fake.BattleCharas.PendingSpawns, Is.Zero);
        Assert.That(fake.EventObjects.OccupiedSlots, Is.Zero);
        Assert.That(game.World.Party.LimitBreak.FilledBars, Is.Zero);
        Assert.That(((FakePartyHud)Natives.PartyHud).Members, Is.Empty);
    }

    private static object[] Statuses(SimWorld world) => world.Party.AllMembers()
        .SelectMany((member, seat) => member.ActiveStatuses.Select(status =>
            (object)(seat, status.StatusId, status.Stacks, status.Snapshot.Elapsed, status.RemainingTime))).ToArray();

    private static uint[] Actors(SimWorld world) => world.Children.OfType<SimEnemy>()
        .Where(enemy => enemy.IsActive).Select(enemy => enemy.SpawnConfig.BNpcBaseId).Order().ToArray();

    private static void Frames(FakeGame fake, Game game, int frames)
    {
        for (var i = 0; i < frames; i++) fake.Frame(ScenarioRun.FrameSeconds, game.Tick);
    }
}
