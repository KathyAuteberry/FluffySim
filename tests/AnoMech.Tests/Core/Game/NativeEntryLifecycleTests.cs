#pragma warning disable CA1416
using System.Reflection;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Fru;
using AnoMech.Scenarios.Dsr;
using Dalamud.Plugin.Services;

namespace AnoMech.Tests;

public class NativeEntryLifecycleTests
{
    public static IEnumerable<TestCaseData> CustomEntries()
    {
        foreach (var scenario in ScenarioCatalog.Create().Scenarios.Where(s => s.Phase.Zone is FruZone or DsrZone))
        foreach (var offline in new[] { false, true })
        foreach (var hard in new[] { false, true })
        foreach (var visible in new[] { false, true })
        foreach (var role in Enum.GetValues<PartyRole>())
            yield return new TestCaseData(scenario.GetType(), offline, hard, visible, role);
    }

    [TestCase(false)] [TestCase(true)]
    public void UpstreamDutyRegistersTextBeforeLayoutReadiness(bool offline)
    {
        var fake = Install(offline, false, true);
        fake.MapEffects.TerritoryReady = false;
        using var game = CreateGame();
        IZone zone = AnoMech.Scenarios.Umad.UmadZone.Instance;
        var data = zone.DutyInitData!;
        var record = data.Rsv.First();
        var key = System.Text.Encoding.ASCII.GetString(record, 4, 0x30).Split('\0')[0];
        var text = System.Text.Encoding.UTF8.GetString(record, 4 + 0x30, BitConverter.ToInt32(record, 0));
        Assert.That(((FakeRsvFunctions)Natives.Rsv).Resolve(key), Is.EqualTo(key));
        game.ZoneIn(zone, SessionMode.Local);
        Frames(fake, game, 2);
        Assert.That(((FakeRsvFunctions)Natives.Rsv).Resolve(key), Is.EqualTo(text));
        Assert.That(fake.BattleCharas.Player.Position, Is.EqualTo(data.EntranceBarrier.Position));
        Frames(fake, game, 2);
        Assert.That(game.ActiveScenario, Is.Null);
        Assert.That(fake.EventObjects.OccupiedSlots, Is.Zero);
        game.Leave();
        Frames(fake, game, 2);
        Assert.That(fake.Zone.IsActive, Is.False);
    }

    [TestCase(SessionMode.Host)]
    [TestCase(SessionMode.Peer)]
    public void MultiplayerPreparationWaitsForLayoutWithoutStartingTheScenario(SessionMode mode)
    {
        var fake = Install(false, false, true);
        fake.MapEffects.TerritoryReady = false;
        using var game = CreateGame();
        var scenario = new AnoMech.Scenarios.Dsr.Strength.DsrStrengthScenario();
        string? refusal = "not resolved";
        game.ZoneIn(DsrZone.Instance, mode);
        Frames(fake, game, 1);
        game.PrepareRun(Parameters(scenario, PartyRole.MainTank), result => refusal = result);
        Frames(fake, game, 120);
        Assert.That(refusal, Is.EqualTo("not resolved"));
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
        fake.MapEffects.TerritoryReady = true;
        Frames(fake, game, 180);
        Assert.That(refusal, Is.Null);
        Assert.That(game.ActiveScenario, Is.Null);
        Assert.That(game.World.Party.AllMembers().Count(), Is.EqualTo(8));
        Assert.That(game.IsPreparingScenario, Is.False);
    }

    [TestCaseSource(nameof(CustomEntries))]
    public void ColdEntryWaitsForLayoutThenOpensBarrierBeforePartyAndPhase(Type type, bool offline, bool hard, bool visible, PartyRole role)
    {
        var fake = Install(offline, hard, visible);
        fake.MapEffects.TerritoryReady = false;
        fake.BattleCharas.PacketSpawnDelayFrames = 5;
        using var game = CreateGame();
        var scenario = (IScenario)Activator.CreateInstance(type)!;
        game.EnterAndStart(Parameters(scenario, role), SessionMode.Local);
        Frames(fake, game, 240);
        Assert.That(game.IsPreparingScenario, Is.True);
        Assert.That(game.ActiveScenario, Is.Null);
        Assert.That(game.EventClockNow, Is.Zero);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(fake.EventObjects.OccupiedSlots, Is.Zero);
        Assert.That(fake.MapEffects.Applied, Is.Empty);
        Assert.That(((FakePartyHud)Natives.PartyHud).Members, Is.Empty);

        fake.MapEffects.TerritoryReady = true;
        Frames(fake, game, 1);
        var barrier = fake.EventObjects.Objects.Single();
        Assert.That(barrier.SpawnConfig.LayoutId, Is.EqualTo(scenario.Phase.Zone.EntranceBarrier.EObjLayoutId));
        Assert.That(barrier.SpawnConfig.GimmickId, Is.EqualTo(scenario.Phase.Zone.EntranceBarrier.WallLayoutId));
        Assert.That(barrier.BarrierCollisionEnabled, Is.True);
        Frames(fake, game, 90);
        Assert.That(barrier.BarrierCollisionEnabled, Is.True);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(game.ActiveScenario, Is.Null);
        for (var i = 0; i < 120 && game.ActiveScenario is null; i++) Frames(fake, game, 1);
        Assert.That(game.ActiveScenario, Is.SameAs(scenario));
        Assert.That(barrier.BarrierCollisionEnabled, Is.False);
        Assert.That(game.EventClockNow, Is.LessThan(.1f));
        Frames(fake, game, 30);
        Assert.That(game.World.Party.AllMembers().Count(), Is.EqualTo(8));
        Assert.That(game.World.Party.PlayerRole, Is.EqualTo(role));
        var bots = game.World.Party.AllMembers().OfType<SimPartyNpc>().ToArray();
        Assert.That(bots, Has.Length.EqualTo(7));
        Assert.That(bots.All(b => b.Proxy is { Exists: true }), Is.True);
        Assert.That(bots.All(b => b.PresentationVisible == visible), Is.True);
        Assert.That(bots.All(b => b.Proxy!.IsDrawObjectVisible == visible), Is.True);
        Assert.That(((FakePartyHud)Natives.PartyHud).Members.Select(m => m.Actor).Distinct().Count(), Is.EqualTo(8));
        Assert.That(game.World.BotDifficulty, Is.EqualTo(hard ? BotDifficulty.Hard : BotDifficulty.Easy));
        AssertPhase(scenario, fake);
        game.Leave();
        Frames(fake, game, 2);
        Assert.That(fake.Zone.IsActive, Is.False);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(fake.EventObjects.OccupiedSlots, Is.Zero);
        Assert.That(((FakePartyHud)Natives.PartyHud).Members, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BarrierSpawnFailureRetriesWithoutStartingOrLosingTheParty(bool offline)
    {
        var fake = Install(offline, false, true);
        fake.EventObjects.RefuseSpawns = true;
        using var game = CreateGame();
        var scenario = ScenarioCatalog.Create().Scenarios.First(s => s.Phase == FruZone.P3);
        game.EnterAndStart(Parameters(scenario, PartyRole.MainTank), SessionMode.Local);
        Frames(fake, game, 180);
        Assert.That(game.IsPreparingScenario, Is.True);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
        fake.EventObjects.RefuseSpawns = false;
        Frames(fake, game, 150);
        Assert.That(game.ActiveScenario, Is.SameAs(scenario));
        Assert.That(fake.EventObjects.Objects.Single(o => o.SpawnConfig.GimmickId != 0).BarrierOpened, Is.True);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void StopOrLeaveCancelsAWaitingStart(bool offline, bool leave)
    {
        var fake = Install(offline, false, true);
        fake.MapEffects.TerritoryReady = false;
        using var game = CreateGame();
        var scenario = ScenarioCatalog.Create().Scenarios.First(s => s.Phase.Zone is DsrZone);
        game.EnterAndStart(Parameters(scenario, PartyRole.MeleeDpsB), SessionMode.Local);
        Frames(fake, game, 3);
        if (leave) game.Leave(); else game.Reset();
        Frames(fake, game, 2);
        fake.MapEffects.TerritoryReady = true;
        Frames(fake, game, 180);
        Assert.That(game.ActiveScenario, Is.Null);
        Assert.That(game.IsPreparingScenario, Is.False);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ChangingPhaseAndRestartingKeepsTheEntranceOpenAndReplacesPhaseHolds(bool offline)
    {
        var fake = Install(offline, false, true);
        using var game = CreateGame();
        var catalog = ScenarioCatalog.Create().Scenarios;
        var first = catalog.Single(s => s.Phase == FruZone.P1);
        var next = catalog.First(s => s.Phase == FruZone.P3);
        game.EnterAndStart(Parameters(first, PartyRole.MeleeDpsA), SessionMode.Local);
        Frames(fake, game, 160);
        Assert.That(game.ActiveScenario, Is.SameAs(first));
        var barrier = fake.EventObjects.Objects.Single(o => o.SpawnConfig.GimmickId != 0);
        Assert.That(barrier.BarrierOpened, Is.True);
        Assert.That(((FakeLayoutFunctions)Natives.Layout).HeldLayerStates[(1238, 0x5F88)], Is.True);
        game.Reset(); Frames(fake, game, 1);
        game.EnterAndStart(Parameters(next, PartyRole.PhysRangedDps), SessionMode.Local);
        Frames(fake, game, 20);
        Assert.That(game.ActiveScenario, Is.SameAs(next));
        Assert.That(barrier.BarrierOpened, Is.True);
        var holds = ((FakeLayoutFunctions)Natives.Layout).HeldLayerStates;
        Assert.That(holds[(1238, 0x5F88)], Is.False);
        Assert.That(holds.ContainsKey((1238, 0x5FCA)), Is.False);
        Assert.That(game.World.Party.AllMembers().Count(), Is.EqualTo(8));
        Assert.That(game.World.Party.PlayerRole, Is.EqualTo(PartyRole.PhysRangedDps));
    }

    public static IEnumerable<Type> OriginalControls() => ScenarioCatalog.Create().Scenarios
        .Where(s => s.Phase.Zone is not FruZone and not DsrZone).Select(s => s.GetType()).Take(3);

    public static IEnumerable<TestCaseData> OriginalEntries() => OriginalControls()
        .SelectMany(type => new[] { false, true }.Select(offline => new TestCaseData(type, offline)));

    [TestCaseSource(nameof(OriginalEntries))]
    public void OriginalScenariosUseTheSameReadyBarrierTransition(Type type, bool offline)
    {
        var fake = Install(offline, false, true);
        using var game = CreateGame();
        var scenario = (IScenario)Activator.CreateInstance(type)!;
        game.EnterAndStart(Parameters(scenario, PartyRole.MainTank), SessionMode.Local);
        Frames(fake, game, 160);
        Assert.That(game.ActiveScenario, Is.SameAs(scenario));
        Assert.That(fake.EventObjects.Objects.Single(o => o.SpawnConfig.GimmickId == scenario.Phase.Zone.EntranceBarrier.WallLayoutId).BarrierOpened, Is.True);
    }

    [TestCase(false)] [TestCase(true)]
    public void MeteorsRestartAfterIdleStartsAtZeroAndReleasesTowerVisuals(bool offline)
    {
        var fake = Install(offline, true, true);
        using var game = CreateGame();
        game.ZoneIn(DsrZone.Instance, SessionMode.Local);
        Frames(fake, game, 180);
        var scenario = new AnoMech.Scenarios.Dsr.Sanctity2.DsrSanctity2Scenario();
        for (var run = 0; run < 3; ++run)
        {
            Frames(fake, game, 1800);
            Assert.That(game.Events.Elapsed, Is.GreaterThan(29));
            game.EnterAndStart(Parameters(scenario, PartyRole.MeleeDpsA), SessionMode.Local);
            Frames(fake, game, 2);
            Assert.That(game.Events.Elapsed, Is.LessThan(.1));
            Frames(fake, game, 60);
            Assert.That(scenario.ActiveTowerVisuals, Has.Count.EqualTo(8));
            Assert.That(game.World.Party.AllMembers().Count(), Is.EqualTo(8));
            game.Reset();
            Frames(fake, game, 1);
            Assert.That(scenario.ActiveTowerVisuals, Is.Empty);
            Assert.That(fake.BattleCharas.OccupiedSlots, Is.EqualTo(7), "Stop retains the idle party but removes mechanic actors");
            Assert.That(((FakePartyHud)Natives.PartyHud).Members, Has.Count.EqualTo(8));
        }
        game.Leave();
        Frames(fake, game, 2);
        Assert.That(fake.BattleCharas.OccupiedSlots, Is.Zero);
        Assert.That(((FakePartyHud)Natives.PartyHud).Members, Is.Empty);
    }

    private static FakeGame Install(bool offline, bool hard, bool visible)
    {
        var fake = FakeGame.Install();
        var client = DispatchProxy.Create<IClientState, EntryClientState>();
        ((EntryClientState)(object)client).LoggedIn = !offline;
        DalamudServices.Install(nameof(Plugin.ClientState), client);
        DalamudServices.Install(nameof(Plugin.Config), new Configuration { BotDifficulty = hard ? BotDifficulty.Hard : BotDifficulty.Easy, ShowSimulatedParty = visible });
        PerRole.ForceSeats = true;
        return fake;
    }

    private static Game CreateGame()
    {
        var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        return game;
    }
    private static RunScenarioParams Parameters(IScenario scenario, PartyRole role) => new(scenario, new LocalPartyProvider { RoleOverride = role }, 0, 0, 123);
    private static void Frames(FakeGame fake, Game game, int count)
    {
        for (var i = 0; i < count; i++) fake.Frame(1f / 60f, game.Tick);
    }
    private static void AssertPhase(IScenario scenario, FakeGame fake)
    {
        if (scenario.Phase.Zone is DsrZone)
        {
            for (byte i = 0; i < 9; i++) Assert.That(fake.MapEffects.Applied.Last(e => e.Index == i).Flags, Is.EqualTo(i == 8 && (scenario.Phase == DsrZone.P3 || scenario.Phase == DsrZone.P4) ? 0x00020001u : 0x00040004u));
            Assert.That(((FakeLayoutFunctions)Natives.Layout).HeldLayerStates, Is.Empty);
            Assert.That(((FakeLayoutFunctions)Natives.Layout).HeldInstanceStates, Is.Empty);
            return;
        }
        var holds = ((FakeLayoutFunctions)Natives.Layout).HeldLayerStates;
        Assert.That(holds[(1238, 0x5F88)], Is.EqualTo(scenario.Phase == FruZone.P1));
        Assert.That(holds[(1238, 0x5FC9)], Is.EqualTo(scenario.Phase == FruZone.P2));
        var expected = scenario.Phase == FruZone.P1 ? 0x00040004u : scenario.Phase == FruZone.P2 ? 0x00020001u : scenario.Phase == FruZone.P3 ? 0x00080004u : 0x00040004u;
        Assert.That(fake.MapEffects.Applied.Last(e => e.Index == 0x17).Flags, Is.EqualTo(expected));
        if (scenario.Phase == FruZone.P3 || scenario.Phase == FruZone.P4)
            Assert.That(fake.MapEffects.Applied.Last(e => e.Index == 0x29).Flags, Is.EqualTo(0x00020001));
    }

    public class EntryClientState : DispatchProxy
    {
        public bool LoggedIn;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => targetMethod?.Name == "get_IsLoggedIn" ? LoggedIn : targetMethod is { ReturnType.IsValueType: true } && targetMethod.ReturnType != typeof(void) ? Activator.CreateInstance(targetMethod.ReturnType) : null;
    }
}
