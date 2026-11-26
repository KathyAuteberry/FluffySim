using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.Elements;

// P3 from Definition of Insanity to the second elements' Cyclones. Bowels of Agony starts at 27 s.
public sealed class DmuElementsScenario : IScenario
{
    private const float BowelsStart = 27f;
    private const float ElementsApplied = 31.96f;
    private const float FirstExpiry = 51.0f;
    private const float SecondExpiry = 78.0f;
    private const float FirstElementSeconds = FirstExpiry - ElementsApplied;
    private const float SecondElementSeconds = SecondExpiry - ElementsApplied;
    private const float WindSeconds = 68f;
    private const float HolderBurstDelay = 0.07f;
    private const float CrystalShotDelay = 1.1f;
    // UNVERIFIED: retail's Cyclones followed the clearing shot by 1.0 to 3.5 s.
    private const float CycloneDelay = 1.7f;
    private const float WindClearedBy = 0.8f;
    private const float AutoAttackInterval = 3.03f;
    private const int HelperCount = 16;
    private const int CycloneHelperCount = 8;

    public string Name => "Elements";
    public IPhase Phase => DmuZone.P3;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DmuElementsAi()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    public float BgmSecondsAtStart => 28f;

    public DmuElementsState? LastState { get; private set; }

    private readonly DmuElementsSettingsWindow settingsWindow = new();
    private readonly List<SimEnemy> helpers = [];
    private readonly List<SimEnemy> cycloneHelpers = [];
    private readonly List<SimEventObject> crystals = [];
    private SimWorld world = null!;
    private SimParty party = null!;
    private DmuElementsState state = null!;
    private SimEnemy? kefka;
    private SimEnemy? chaos;
    private SimEnemy? exdeath;
    private SimEnemy? gravenImage;
    private int nextHelper;
    private int nextCycloneHelper;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new DmuElementsState(world.Rng, settingsWindow.Overrides);
        LastState = state;
        helpers.Clear();
        cycloneHelpers.Clear();
        crystals.Clear();
        nextHelper = nextCycloneHelper = 0;
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<DmuElementsState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnKefkaAndHelpers);
        world.Events.Add(2.4f, () => kefka?.Cast(DmuElementsActions.DefinitionOfInsanity));
        world.Events.Add(7.4f, SpawnChaosAndExdeath);
        world.Events.Add(9.6f, () =>
        {
            chaos?.Cast(DmuElementsActions.DecisiveBattleChaos);
            exdeath?.Cast(DmuElementsActions.DecisiveBattleExdeath);
        });
        world.Events.Add(10.9f, () => kefka?.SetModelHidden(true));
        world.Events.Add(11.6f, () => kefka?.SetPosition(new Placement(new Vector3(0f, 0f, -10f), 0f)));
        world.Events.Add(11.8f, () => kefka?.SetModelHidden(false));
        world.Events.Add(12.6f, DecisiveBattle);
        world.Events.Add(13.8f, () => kefka?.Cast(DmuElementsActions.StatueGlance));
        AutoAttacks(15.7f, () => chaos, state.ChaosTank, DmuElementsActions.ChaosAutoAttack);
        AutoAttacks(16.7f, () => exdeath, state.ExdeathTank, DmuElementsActions.ExdeathAutoAttack);

        world.Events.Add(BowelsStart, () => chaos?.Cast(DmuElementsActions.BowelsOfAgony));
        world.Events.Add(ElementsApplied, ApplyElements);
        world.Events.Add(32.2f, SpawnCrystals);

        world.Events.Add(43.9f, () => world.Map.AddEffect(packetFlags: 0x00020001U, index: 0x22));
        world.Events.Add(44.2f, ThunderIIICircle);
        world.Events.Add(51.4f, () => exdeath?.Follow(party.Get(state.ExdeathTank), 6f, 0f));
        Elements(FirstExpiry, state.First);
        world.Events.Add(55.2f, () => world.Map.AddEffect(packetFlags: 0x00080004U, index: 0x22));
        world.Events.Add(55.3f, () => exdeath?.Cast(DmuElementsActions.ThunderIIIBusterCast));
        world.Events.Add(60.3f, ThunderIIIBuster);
        world.Events.Add(63.3f, ThunderIIIBuster);

        world.Events.Add(68.2f, Implosion);
        Elements(SecondExpiry, state.Second);
        world.Events.Add(86f, () => DiagnosticLog.Info("[DmuElements] End after the second elements."));
    }

    private void SpawnKefkaAndHelpers()
    {
        kefka = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Kefka, NameId: BNpcNameId.Kefka, Level: Level,
            Targetable: false, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.Kefka,
            Placement: new Placement(Vector3.Zero, 0f)));
        gravenImage = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.GravenImage, NameId: BNpcNameId.GravenImage, Level: Level,
            Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.GravenImage,
            Placement: new Placement(new Vector3(0f, 10.7f, -47.5f), 0f)));
        for (var i = 0; i < HelperCount; i++)
            if (world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.Chaos, Level: 1,
                    Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
                    Placement: new Placement(Vector3.Zero, 0f))) is { } helper)
                helpers.Add(helper);
        for (var i = 0; i < CycloneHelperCount; i++)
            if (world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.ChaosVfxHelper, NameId: BNpcNameId.Chaos, Level: 1,
                    Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.HiddenUntilShown,
                    Placement: new Placement(Vector3.Zero, 0f))) is { } helper)
                cycloneHelpers.Add(helper);
    }

    private void SpawnChaosAndExdeath()
    {
        chaos = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.ChaosP3, NameId: BNpcNameId.Chaos, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.ChaosP3,
            Placement: new Placement(new Vector3(-8f, 0f, 0f), 0f)));
        exdeath = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Exdeath, NameId: BNpcNameId.Exdeath, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.Exdeath,
            Placement: new Placement(new Vector3(8f, 0f, 0f), 0f)));
    }

    private SimEnemy? Helper(Vector3 at, float rotation = 0f)
    {
        if (helpers.Count == 0) return null;
        var helper = helpers[nextHelper++ % helpers.Count];
        helper.SetPosition(new Placement(at, rotation));
        return helper;
    }

    private SimCharacter? Alive(PartyRole role) => party.Get(role) is { } m && m.IsAlive() ? m : null;

    private IReadOnlyList<SimCharacter> LivingByDistance(Vector3 from) => party.ActiveMembers().Where(m => m.IsAlive())
        .OrderBy(m => Vector3.DistanceSquared(m.Position, from)).ToList();

    private void AutoAttacks(float first, System.Func<SimEnemy?> boss, PartyRole role, EnemyAction action)
    {
        for (var at = first; at < 86f; at += AutoAttackInterval)
            world.Events.Add(at, () =>
            {
                if (boss() is { IsActive: true, AnimationLock: false } enemy && Alive(role) is { } tank) enemy.Cast(action, tank);
            });
    }

    // The four nearest Chaos become his Epic Heroes, the rest Exdeath's Fated Heroes.
    private void DecisiveBattle()
    {
        if (chaos == null || exdeath == null) return;
        chaos.AddStatus(StatusId.EpicVillain);
        exdeath.AddStatus(StatusId.FatedVillain);
        var byChaos = party.ActiveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, chaos.Position)).ToList();
        for (var i = 0; i < byChaos.Count; i++)
            byChaos[i].AddStatus(i < byChaos.Count / 2 ? StatusId.EpicHero : StatusId.FatedHero);
        chaos.Follow(party.Get(state.ChaosTank), 6f, 0f);
        exdeath.Follow(party.Get(state.ExdeathTank), 6f, 0f);
    }

    private void ApplyElements()
    {
        foreach (var role in PerRole.All)
        {
            if (Alive(role) is not { } member) continue;
            member.AddStatus(state.Headwind[role] ? StatusId.Headwind : StatusId.Tailwind, WindSeconds);
            if (state.FireHolders.Contains(role))
                member.AddStatus(StatusId.Entropy, state.First == Element.Fire ? FirstElementSeconds : SecondElementSeconds);
            if (state.WaterHolders.Contains(role))
                member.AddStatus(StatusId.DynamicFluid, state.First == Element.Water ? FirstElementSeconds : SecondElementSeconds);
        }
    }

    private void SpawnCrystals()
    {
        Crystal(EObjId.FireCrystal, FireCrystalArg2, state.FireCorner);
        Crystal(EObjId.WaterCrystal, WaterCrystalArg2, state.WaterCorner);
        Crystal(EObjId.WindCrystal, WindCrystalArg2, state.WindCorner);
    }

    private void Crystal(uint eobjId, uint arg2, int corner)
    {
        var at = DmuElementsCorners.Crystal(corner);
        if (world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = eobjId,
                LayoutId = LayoutId.Crystal[corner],
                EventId = EventId.StatueDirector,
                TargetableStatus = 5,
                Arg2 = arg2,
                Placement = new Placement(new Vector3(at.X, 0f, at.Y), System.MathF.Atan2(-at.X, -at.Y)),
            }) is { } crystal)
            crystals.Add(crystal);
    }

    private void ThunderIIICircle()
    {
        if (exdeath == null) return;
        exdeath.Follow(null);
        chaos?.RemoveStatus(StatusId.EpicVillain);
        exdeath.RemoveStatus(StatusId.FatedVillain);
        if (gravenImage != null) world.Tether(exdeath, gravenImage, TetherId.ExdeathVoid, duration: 7f);
        exdeath.Cast(DmuElementsActions.ThunderIIICircle);
    }

    private void ThunderIIIBuster()
    {
        if (exdeath == null || LivingByDistance(exdeath.Position).FirstOrDefault() is not { } closest) return;
        Helper(exdeath.Position)?.Cast(DmuElementsActions.ThunderIIIBuster, closest);
    }

    // The holders burst, then the element's crystal fires at the two players nearest it.
    private void Elements(float expiry, Element element)
    {
        world.Events.Add(expiry + HolderBurstDelay, () =>
        {
            foreach (var role in state.Holders(element))
                if (Alive(role) is { } holder)
                    Helper(holder.Position)?.Cast(element == Element.Fire ? DmuElementsActions.StrayFlames : DmuElementsActions.StraySpray, holder);
        });
        world.Events.Add(expiry + CrystalShotDelay, () => CrystalShot(element));
    }

    private void CrystalShot(Element element)
    {
        var crystal = DmuElementsCorners.Crystal(state.Corner(element));
        var crystalAt = new Vector3(crystal.X, 0f, crystal.Y);
        var windBefore = party.ActiveMembers().Where(m => m.IsAlive() && DmuElementsActions.HasWind(m)).ToList();
        foreach (var target in LivingByDistance(crystalAt).Take(2))
            Helper(target.Position)?.Cast(element == Element.Fire ? DmuElementsActions.Inferno : DmuElementsActions.Tsunami, target);
        world.Events.Add(WindClearedBy, () =>
        {
            var cleared = windBefore.Count(m => m.IsAlive() && !DmuElementsActions.HasWind(m));
            if (cleared == 0) return;
            DiagnosticLog.Info($"[DmuElements] {element} crystal cleared {cleared} wind(s); Cyclones follow.");
            world.Events.Add(CycloneDelay - WindClearedBy, () => Cyclones(cleared));
        });
    }

    private void Cyclones(int count)
    {
        var wind = DmuElementsCorners.Crystal(state.WindCorner);
        var windAt = new Vector3(wind.X, 0f, wind.Y);
        foreach (var target in LivingByDistance(windAt).Take(count))
        {
            if (cycloneHelpers.Count == 0) return;
            var helper = cycloneHelpers[nextCycloneHelper++ % cycloneHelpers.Count];
            helper.SetPosition(new Placement(windAt, 0f));
            helper.Cast(DmuElementsActions.Cyclone, target);
        }
    }

    // Self-cast: the shockwaves run from the facing Chaos has as the cast starts.
    private void Implosion()
    {
        if (chaos == null) return;
        chaos.Follow(null);
        if (Alive(state.ChaosTank) is { } tank)
            chaos.SetRotation(System.MathF.Atan2(tank.Position.X - chaos.Position.X, tank.Position.Z - chaos.Position.Z));
        var facing = chaos.Rotation;
        chaos.Cast(state.Latitudinal ? DmuElementsActions.LatitudinalImplosion : DmuElementsActions.LongitudinalImplosion);
        var first = state.Latitudinal ? new[] { System.MathF.PI / 2f, -System.MathF.PI / 2f } : [0f, System.MathF.PI];
        var second = state.Latitudinal ? new[] { 0f, System.MathF.PI } : [System.MathF.PI / 2f, -System.MathF.PI / 2f];
        world.Events.Add(5.77f, () => Shockwaves(facing, first));
        world.Events.Add(7.77f, () => Shockwaves(facing, second));
        world.Events.Add(8.2f, () => chaos?.Follow(party.Get(state.ChaosTank), 6f, 0f));
    }

    private void Shockwaves(float facing, float[] offsets)
    {
        if (chaos == null) return;
        foreach (var offset in offsets) Helper(chaos.Position, facing + offset)?.Cast(DmuElementsActions.Shockwave);
    }
}
