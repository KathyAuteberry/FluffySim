using AnoMech.Core.EnemyActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad;
using AnoMech.Scenarios.Umad.P3BlackHole;

namespace AnoMech.Scenarios.Dmu.LimitCut;

using AnoMech.Core.Native.Interfaces;
using static AnoMech.Scenarios.Umad.UmadConstants;
using static DmuLimitCutState;

// Dancing Mad P3 "Limit Cut". Scenario time 0 is 8.0s before Chaos starts casting Umbra Smash,
// the earliest start inside this mechanic (the second elements' Cyclones land 7.65s before).
public sealed class DmuLimitCutScenario : IScenario
{
    public string Name => "Limit Cut";
    public IPhase Phase => DmuZone.P3;
    public float BgmSecondsAtStart => 108.60f;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DmuLimitCutAi()];
    public void DrawSettings() => settingsWindow.Draw();
    public bool HasPerPlayerSettings => true;
    public void DrawPerPlayerSettings() => settingsWindow.DrawPerPlayer();
    public object SettingsOverrides => settingsWindow.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settingsWindow.Overrides.Validate().Problems;
    public void DrawMultiplayerSettings() => settingsWindow.DrawThunderIIIPlan();

    private readonly DmuLimitCutSettingsWindow settingsWindow = new();
    private SimWorld world = null!;
    private SimParty party = null!;
    private DmuLimitCutState state = null!;
    private readonly SimEnemy?[] cycloneHelpers = new SimEnemy?[8];
    private SimEnemy? thunderHelper;
    private SimEventObject? windCrystal;
    private Vector3 umbraImpact;
    private EnemyActionCast? vacuumWave;

    // Clone k is teleported to its spot, then fires its appearance ~0.1s later, ~2.0s apart.
    private static readonly float[] PlaceCloneAt = [8.806f, 10.818f, 12.830f, 14.842f, 16.854f, 18.866f, 20.878f, 22.890f];
    private static readonly float[] CloneAppearAt = [8.895f, 10.907f, 12.919f, 14.931f, 16.943f, 18.955f, 20.967f, 22.979f];
    // Charge k: teleport to the charge spot facing its number, then the rect ~0.1s later.
    private static readonly float[] PrepareChargeAt = [30.933f, 31.156f, 31.380f, 31.602f, 31.825f, 32.049f, 32.272f, 32.496f];
    private static readonly float[] ChargeAt = [31.033f, 31.256f, 31.480f, 31.702f, 31.925f, 32.149f, 32.372f, 32.596f];
    private const float NumbersAt = 18.83f;
    // Both bosses stop swinging for their own casts.
    private static readonly float[] ChaosAutoAt = [2.987f, 6.028f, 14.039f, 18.109f, 21.153f, 26.253f, 29.292f, 32.214f, 37.285f, 40.311f, 43.335f, 49.381f, 52.407f];
    private static readonly float[] ExdeathAutoAt = [1.064f, 4.107f, 7.149f, 20.257f, 23.299f, 26.342f, 29.381f, 32.417f, 41.603f, 47.647f, 50.671f];

    public DmuLimitCutState? LastState { get; private set; }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new DmuLimitCutState(world.Rng, party, settingsWindow.Overrides);
        LastState = state;
        vacuumWave = null;
        DiagnosticLog.Info(
            $"[DmuLimitCut] Roll: clones start {SpotName(state.StartSpot)} going {(state.Clockwise ? "clockwise" : "counter-clockwise")}, "
            + $"bosses held {SpotName(state.BossSpot)}, bait {state.BaitRole}, numbers "
            + string.Join(" ", state.Numbers.Select((r, k) => $"{k + 1}={r}")) + ", winds "
            + string.Join(" ", state.Winds.Select(kv => $"{kv.Key}={kv.Value}")) + ".");

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DmuLimitCutState>)AiStrats[idx]).Run(state, world);

        SpawnActors();
        ApplyStartingStatuses();

        world.Events.Add(1.0f, () =>
        {
            state.Objects.Chaos?.Follow(party.Get(state.ChaosTank));
            state.Objects.Exdeath?.Follow(party.Get(state.ExdeathTank));
        });
        foreach (var at in ChaosAutoAt)
            world.Events.Add(at, () => state.Objects.Chaos?.Cast(UmadActions.AutoAttack, party.Get(state.ChaosTank)));
        foreach (var at in ExdeathAutoAt)
            world.Events.Add(at, () => state.Objects.Exdeath?.Cast(UmadActions.ExdeathAutoAttack, party.Get(state.ExdeathTank)));
        // The real packet refreshes Kefka's trance aura in place; AddStatusParam adds a slot, so
        // drop the first or Kefka wears two auras at once.
        world.Events.Add(6.783f, () =>
        {
            state.Objects.Kefka?.ActorControl.Transform(BNpcStateId.KefkaLimitCut);
            state.Objects.Kefka?.RemoveStatus(StatusId.KefkaTrance);
            state.Objects.Kefka?.AddStatusParam(StatusId.KefkaTrance, 0x22B);
        });

        // Both bosses stand still for their casts, which lets the tanks step into the stack (at
        // every real wave both tanks were inside it, Exdeath frozen 5.5y further out).
        world.Events.Add(7.8f, () => state.Objects.Chaos?.Follow());
        world.Events.Add(7.928f, () => state.Objects.Exdeath?.Follow());
        world.Events.Add(8.0f, StartUmbraSmash);
        world.Events.Add(8.128f, () => vacuumWave = state.Objects.Exdeath?.Cast(UmadActions.VacuumWave));
        for (var k = 0; k < 8; k++)
        {
            var clone = k;
            world.Events.Add(PlaceCloneAt[k], () => PlaceClone(clone));
            world.Events.Add(CloneAppearAt[k], () => CloneAppear(clone));
        }
        world.Events.Add(14.526f, ChaosLands);
        world.Events.Add(17.075f, () => state.Objects.Chaos?.Follow(party.Get(state.ChaosTank)));
        world.Events.Add(17.56f, () => state.Objects.Exdeath?.Follow(party.Get(state.ExdeathTank)));
        world.Events.Add(NumbersAt, AttachNumbers);
        world.Events.Add(19.856f, ResolveCyclones);
        world.Events.Add(21.020f, () => windCrystal?.Despawn());
        world.Events.Add(21.189f, () => state.Objects.Chaos?.Cast(ActionId.Aetherlink_Chaos, animationLock: 3.1f));
        world.Events.Add(21.242f, () => state.Objects.Exdeath?.Cast(ActionId.Aetherlink_Exdeath, animationLock: 3.1f));
        for (var k = 0; k < 8; k++)
        {
            var clone = k;
            world.Events.Add(PrepareChargeAt[k], () => PrepareCharge(clone));
            world.Events.Add(ChargeAt[k], () => ResolveCharge(clone));
        }
        world.Events.Add(33.29f, () => state.Objects.Exdeath?.Follow());
        world.Events.Add(33.49f, () => state.Objects.Exdeath?.Cast(ActionId.ThunderIII_Cast, animationLock: 3.1f));
        world.Events.Add(38.57f, ResolveThunder);
        world.Events.Add(41.60f, ResolveThunder);
        world.Events.Add(42.10f, () => state.Objects.Exdeath?.Follow(party.Get(state.ExdeathTank)));
        world.Events.Add(43.30f, () =>
        {
            state.Objects.Chaos?.Follow();
            state.Objects.Exdeath?.Follow();
        });
        world.Events.Add(43.50f, () =>
        {
            state.Objects.Chaos?.Cast(ActionId.DecisiveBattle_Chaos, animationLock: 3.1f);
            state.Objects.Exdeath?.Cast(ActionId.DecisiveBattle_Exdeath, animationLock: 3.1f);
        });
        world.Events.Add(46.49f, ResolveDecisiveBattle);
        world.Events.Add(46.69f, () =>
        {
            state.Objects.Chaos?.Follow(party.Get(state.ChaosTank));
            state.Objects.Exdeath?.Follow(party.Get(state.ExdeathTank));
        });
        world.Events.Add(50.581f, () => state.Objects.Kefka?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(50.746f, () =>
        {
            state.Objects.Exdeath?.Follow();
            state.Objects.Exdeath?.Cast(ActionId.ThunderIII_Cast, animationLock: 3.1f);
        });
    }

    public void RunInstanceEvents(SimWorld instanceWorld)
    {
        Natives.TimelinePreload.Preload(CloneTimelines, "DmuLimitCut");
        instanceWorld.Party.LimitBreak.Set(3f);
        instanceWorld.Events.Add(1.873f, () => instanceWorld.Map.DirectorUpdate(0x80000027U, 0x18U, 0x2U, 0x1BDBU, 0x4000EFB9U));
        instanceWorld.Events.Add(8.905f, () => instanceWorld.Map.DirectorUpdate(0x80000027U, 0x2FU, 0x2U, 0x1BDBU, 0x4000EFB9U));
        instanceWorld.Events.Add(31.033f, () => instanceWorld.Map.DirectorUpdate(0x80000027U, 0x19U, 0x2U, 0x1BDBU, 0x4000EFB9U));
        instanceWorld.Events.Add(52.629f, () => instanceWorld.Map.DirectorUpdate(0x80000027U, 0x1AU, 0x2U, 0x1BDBU, 0x4000EFB9U));
    }

    // The "hide" set's mon_sp003/mon_sp004 materialise the clone out of its dissolve; a timeline
    // that never comes up leaves it frozen white and translucent. Both key spellings, as Flood needed.
    private static readonly (ushort Id, string Key)[] CloneTimelines =
    [
        (4575, "mon_sp/m0462/hide/mon_sp003"),
        (4576, "mon_sp/m0462/hide/mon_sp004"),
    ];

    // The bosses are held at the wind crystal, and the cyclone and thunder helpers sit on it.
    private Vector3 WindCrystalPos => OnCircle(SpotHeading(state.BossSpot), 14f);

    private uint WindCrystalLayoutId => state.BossSpot switch
    {
        1 => EObjId.WindCrystalSeLayoutId,
        3 => EObjId.WindCrystalNeLayoutId,
        5 => EObjId.WindCrystalNwLayoutId,
        _ => EObjId.WindCrystalSwLayoutId,
    };

    private void SpawnActors()
    {
        var objects = state.Objects;
        var hold = SpotHeading(state.BossSpot);
        objects.Kefka = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.KefkaP3, NameId: BNpcNameId.Kefka, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            LayoutId: BNpcLayoutId.KefkaP3,
            Placement: new Placement(new Vector3(0f, 0f, -10f), 0f)));
        objects.Chaos = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.ChaosP3, NameId: BNpcNameId.Chaos, Level: 100,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            LayoutId: BNpcLayoutId.ChaosP3,
            Placement: new Placement(OnCircle(hold, 9f), hold + MathF.PI)));
        objects.Exdeath = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Exdeath, NameId: BNpcNameId.Exdeath, Level: 100,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            LayoutId: BNpcLayoutId.Exdeath,
            Placement: new Placement(
                OnCircle(hold, 10.6f) + OnCircle(hold + MathF.PI / 2f, 1.6f),
                hold + MathF.PI)));
        for (var k = 0; k < 8; k++)
            objects.Clones[k] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.KefkaCloneP3, NameId: BNpcNameId.Kefka, Level: 100,
                Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.HiddenUntilShown,
                LayoutId: BNpcLayoutId.KefkaCloneP3,
                Placement: new Placement(Vector3.Zero, 0f)));
        // Cyclone's caster-side VFX needs a real skeleton; the real 9020 helpers have none.
        for (var i = 0; i < 8; i++)
            cycloneHelpers[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Chaos, NameId: BNpcNameId.Chaos, Level: 1,
                Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.HiddenUntilShown,
                Placement: new Placement(WindCrystalPos, 0f)));
        // The strike's VFX sits on the target, so the helper's model never matters.
        thunderHelper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.KefkaHelper, NameId: BNpcNameId.Exdeath, Level: 1,
            Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
            Placement: new Placement(WindCrystalPos, 0f)));
        // The fire and water crystals faded when their elements resolved, before this window.
        windCrystal = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = EObjId.WindCrystal, LayoutId = WindCrystalLayoutId, TargetableStatus = 5, Arg2 = EObjId.WindCrystalArg2,
            Placement = new Placement(WindCrystalPos, hold + MathF.PI),
        });

        // Set once the draw objects exist.
        world.Events.Add(0.5f, () =>
        {
            objects.Kefka?.SetAnimationState(0, 1);
        });
    }

    // No Epic/Fated sides: stripped 44.3s before the Umbra cast, back with the Decisive Battle.
    // The winds land 68s long at Bowels of Agony, 48.49s before scenario start.
    private void ApplyStartingStatuses()
    {
        world.Events.Add(0.1f, () =>
        {
            state.Objects.Kefka?.AddStatusParam(StatusId.KefkaTrance, 0x1FF);
            foreach (var role in Enum.GetValues<PartyRole>())
            {
                if (party.Get(role) is not { } member) continue;
                member.AddStatus(state.Winds[role] == Wind.Headwind ? StatusId.Headwind : StatusId.Tailwind, 19.41f);
            }
        });
    }

    // Black Hole's buster, on whoever is closest to Exdeath.
    private void ResolveThunder()
    {
        if (state.Objects.Exdeath is not { } exdeath) return;
        thunderHelper?.Cast(DmuP3Actions.ThunderIII(state.ThunderPlan), party.Find.Closest(exdeath.Position));
    }

    private void ResolveDecisiveBattle()
    {
        state.Objects.Chaos?.AddStatus(StatusId.EpicVillain);
        state.Objects.Exdeath?.AddStatus(StatusId.FatedVillain);
        foreach (var role in Enum.GetValues<PartyRole>())
            if (party.Get(role) is { } member && member.IsAlive())
                member.AddStatus(role == state.ChaosTank || role is PartyRole.ShieldHealer or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB
                    ? StatusId.EpicHero : StatusId.FatedHero);
    }

    private void StartUmbraSmash()
    {
        if (state.Objects.Chaos is not { } chaos) return;
        var bait = party.Find.Farest(chaos.Position);
        umbraImpact = bait?.Position ?? chaos.Position;
        DiagnosticLog.Info($"[DmuLimitCut] Umbra Smash: bait {(bait as ISimPartyMember)?.Role.ToString() ?? "none"} at ({umbraImpact.X:F1},{umbraImpact.Z:F1}), {Vector3.Distance(umbraImpact, chaos.Position):F1}y from Chaos.");
        chaos.Cast(UmadActions.UmbraSmash, umbraImpact);
    }

    private void ChaosLands()
    {
        if (state.Objects.Chaos is not { } chaos) return;
        chaos.SetPosition(new Placement(umbraImpact, chaos.Rotation));
    }

    private void PlaceClone(int k)
    {
        if (state.Objects.Clones[k] is not { } clone) return;
        var spot = state.PlacementSpot(k);
        clone.SetPosition(new Placement(SpotPosition(spot), SpotHeading(spot) + MathF.PI));
        if (k == 0) state.Objects.Kefka?.SetAnimationState(0, 0);
    }

    private void CloneAppear(int k)
    {
        if (state.Objects.Clones[k] is not { } clone) return;
        clone.Cast(UmadActions.UltimaBlaster);
    }

    private void AttachNumbers()
    {
        for (var k = 0; k < 8; k++)
        {
            if (party.Get(state.Numbers[k]) is not { } member || !member.IsAlive()) continue;
            member.AttachLockonVfx(LockonId.LimitCutNumbers[k]);
        }
    }

    // One Cyclone per player the Vacuum Wave reached: everyone carries a wind into it.
    private void ResolveCyclones()
    {
        windCrystal?.FadeOut();
        if (vacuumWave is null) return;
        var centres = vacuumWave.Hits.Select(h => h.Who).Where(t => t.IsAlive()).ToList();
        for (var i = 0; i < centres.Count; i++)
            cycloneHelpers[i % cycloneHelpers.Length]?.Cast(DmuP3Actions.LimitCutCyclone, centres[i]);
    }

    // A dead number's clone still charges someone; none of the 8 real retargets fit a rule
    // (nearest, farthest, next slot, next number, enmity, or excluding a vuln carrier), so
    // uniformly random.
    private SimCharacter? ChargeTarget(int k)
    {
        if (party.Get(state.Numbers[k]) is { } numbered && numbered.IsAlive()) return numbered;
        var alive = party.ActiveMembers().Where(m => m.IsAlive()).ToArray();
        return alive.Length == 0 ? null : state.PickRandom(alive);
    }

    private void PrepareCharge(int k)
    {
        if (state.Objects.Clones[k] is not { } clone) return;
        var target = ChargeTarget(k);
        var spot = SpotPosition(state.ChargeSpot(k));
        var facing = target != null ? MathF.Atan2(target.Position.X - spot.X, target.Position.Z - spot.Z) : SpotHeading(state.ChargeSpot(k)) + MathF.PI;
        clone.SetPosition(new Placement(spot, facing));
    }

    // Self-targeted along the facing PrepareCharge gave the clone, so a misplaced number drags
    // the rect across whoever is between.
    private void ResolveCharge(int k)
    {
        state.Objects.Clones[k]?.Cast(UmadActions.UltimaBlasterCharge);
    }
}
