using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Top;
using static AnoMech.Scenarios.TopNative.TopNativeConstants;
using Actions = AnoMech.Scenarios.TopNative.TopNativeActions;

namespace AnoMech.Scenarios.TopNative.LimitlessSynergy;

public sealed class TopLimitlessSynergyScenario : IMultiplayerReplayable
{
    public const float LimitlessCast = 7f;
    public const float CloneReveal = LimitlessCast + 8.13f;
    public const float TethersAt = LimitlessCast + 10.23f;
    public const float SagittariusCast = LimitlessCast + 10.36f;
    public const float BladedanceHit = LimitlessCast + 18.46f;
    public const float FlaresAt = LimitlessCast + 24.27f;
    public const float BeyondDefenseCast = LimitlessCast + 24.40f;
    public const float BeyondDefenseJump = LimitlessCast + 29.50f;
    public const float MeteorHit = LimitlessCast + 32.40f;
    public const float PilePitchAt = LimitlessCast + 32.63f;
    public const float CosmoMemoryHit = LimitlessCast + 41.43f;
    public const float CloneRadius = 22f;

    public string Name => "Limitless Synergy";
    public IPhase Phase => TopNativeZone.P2;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new TopLimitlessSynergyAi()];
    public object SettingsOverrides => overrides;
    public TopLimitlessSynergyState? LastState { get; private set; }

    private readonly TopLimitlessSynergyStateOverrides overrides = new();
    private SimWorld world = null!;
    private TopLimitlessSynergyState state = null!;
    private SimEnemy? omegaM;
    private SimEnemy? omegaF;
    private SimEnemy? sagittarius;
    private SimEnemy? shieldClone;
    private readonly SimEnemy?[] bladedancers = new SimEnemy?[2];
    private readonly SimTether?[] tethers = new SimTether?[2];
    private readonly List<SimEnemy?> helpers = [];
    private SimCharacter? jumped;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = LastState = new TopLimitlessSynergyState(world.Rng, world.Party, overrides);
        jumped = null;
        Array.Clear(tethers);
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<TopLimitlessSynergyState>)AiStrats[idx]).Run(state, world);

        SpawnOmegas();
        SpawnClones();
        helpers.Clear();
        for (var i = 0; i < 6; i++) helpers.Add(SpawnHelper());
        world.Events.Add(TethersAt, SpawnTethers);
        world.Events.Add(SagittariusCast, AimSagittarius);
        world.Events.Add(BladedanceHit, Bladedance);
        world.Events.Add(FlaresAt, () => { foreach (var r in state.Flares) world.Party.Get(r)?.AttachLockonVfx(LockonId.OptimizedMeteor); });
        world.Events.Add(BeyondDefenseCast, () => shieldClone?.Cast(ActionId.BeyondDefense, animationLock: 0.3f));
        world.Events.Add(BeyondDefenseJump, JumpOnTank);
        world.Events.Add(MeteorHit, Flares);
        world.Events.Add(PilePitchAt, PilePitch);
    }

    public void Tick(float delta, float elapsed) { }

    private void SpawnOmegas()
    {
        omegaM = world.SpawnEnemy(new EnemySpawnConfig(InitialModeAttributeFlags: 0x10, BNpcBaseId: BNpcBaseId.OmegaM, NameId: BNpcNameId.OmegaP2,
            Level: Level, Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.OmegaM,
            Placement: new Placement(new Vector3(-0.5f, 0f, 0f), MathF.PI)));
        omegaF = world.SpawnEnemy(new EnemySpawnConfig(InitialModeAttributeFlags: 0x10, BNpcBaseId: BNpcBaseId.OmegaF, NameId: BNpcNameId.OmegaP2,
            Level: Level, Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.OmegaF,
            Placement: new Placement(new Vector3(0.5f, 0f, 0f), MathF.PI)));
        world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OpticalUnit, NameId: BNpcNameId.OpticalUnit, Level: Level, Targetable: false,
            EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.OpticalUnit,
            Placement: new Placement(new Vector3(0f, 0f, -45f), 0f)));
        world.Events.Add(LimitlessCast - 6.26f, () => omegaM?.Cast(ActionId.SyntheticShield, animationLock: 0.3f));
        world.Events.Add(LimitlessCast - 5.28f, () => OmegaAttack.Shield.Reshape(omegaM));
        world.Events.Add(LimitlessCast, () => omegaM?.Cast(ActionId.LimitlessSynergyM, animationLock: 0.6f));
        world.Events.Add(LimitlessCast, () => omegaF?.Cast(ActionId.LimitlessSynergyF, animationLock: 0.6f));
        world.Events.Add(CloneReveal, () =>
        {
            omegaM?.SetPosition(new Placement(Vector3.Zero, MathF.PI));
            omegaF?.SetPosition(new Placement(new Vector3(0f, 0f, -10f), 0f));
        });
        world.Events.Add(LimitlessCast + 10.01f, () => omegaM?.Cast(ActionId.OptimizedPassageOfArms, animationLock: 0.3f));
        world.Events.Add(LimitlessCast + 10.01f, () => omegaF?.Cast(ActionId.LaserShower, animationLock: 0.3f));
        world.Events.Add(CosmoMemoryHit + 1f, HurryLaserShower);
    }

    // Simplification: retail's Laser Shower bar runs ~30 s past Cosmo Memory with nothing left to do.
    private void HurryLaserShower()
    {
        omegaF?.CancelCast(CastCancelReason.Cancelled);
        omegaF?.Cast(Actions.LaserShowerHurried);
    }

    private SimEnemy? SpawnClone(bool female, float bearing, float radius, float despawn)
    {
        var position = Polar(bearing, radius);
        SimEnemy? clone = world.SpawnEnemy(new EnemySpawnConfig(InitialModeAttributeFlags: OmegaAttack.SpawnAttributeFlags,
            BNpcBaseId: female ? BNpcBaseId.OmegaFClone : BNpcBaseId.OmegaMClone, NameId: female ? BNpcNameId.OmegaF : BNpcNameId.OmegaM,
            Level: Level, Targetable: false, EnemyList: EnemyListMode.OnlyWhenVisible, Visibility: SpawnVisibility.HiddenUntilShown,
            LayoutId: female ? LayoutId.OmegaFClone : LayoutId.OmegaMClone, Placement: new Placement(position, 0f).Face(Vector3.Zero)));
        world.Events.Add(CloneReveal, () => clone?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(despawn - 2.2f, () => clone?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(despawn, () => clone?.Despawn());
        return clone;
    }

    private void SpawnClones()
    {
        var cosmoF = SpawnClone(true, BearingDegrees(new Vector3(4f, 0f, -14f)), MathF.Sqrt(212f), LimitlessCast + 46.45f);
        var cosmoM = SpawnClone(false, BearingDegrees(new Vector3(-4f, 0f, -14f)), MathF.Sqrt(212f), LimitlessCast + 46.45f);
        bladedancers[0] = SpawnClone(state.BladedanceNorthIsF, state.BladedanceNorthBearing, CloneRadius, LimitlessCast + 25.52f);
        bladedancers[1] = SpawnClone(!state.BladedanceNorthIsF, state.BladedanceSouthBearing, CloneRadius, LimitlessCast + 25.52f);
        sagittarius = SpawnClone(false, state.SagittariusBearing, CloneRadius, LimitlessCast + 30.40f);
        var meteor = SpawnClone(true, state.MeteorBearing, CloneRadius, LimitlessCast + 39.12f);
        shieldClone = SpawnClone(false, state.BeyondDefenseBearing, CloneRadius, LimitlessCast + 37.63f);
        world.Events.Add(CloneReveal, () => { cosmoF?.SetRotation(0f); cosmoM?.SetRotation(0f); });
        world.Events.Add(LimitlessCast + 10.27f, () => shieldClone?.Cast(ActionId.SyntheticShield, animationLock: 0.3f));
        world.Events.Add(LimitlessCast + 11.26f, () => OmegaAttack.Shield.Reshape(shieldClone));
        for (var i = 0; i < 2; i++)
        {
            var clone = bladedancers[i];
            var female = i == 0 ? state.BladedanceNorthIsF : !state.BladedanceNorthIsF;
            world.Events.Add(LimitlessCast + 10.32f, () => clone?.Cast(female ? ActionId.OptimizedBladedanceF : ActionId.OptimizedBladedanceM, animationLock: 0.3f));
        }
        world.Events.Add(LimitlessCast + 24.36f, () => meteor?.Cast(ActionId.OptimizedMeteor, animationLock: 0.3f));
        world.Events.Add(LimitlessCast + 36.38f, () => cosmoF?.Cast(ActionId.CosmoMemoryF, animationLock: 0.3f));
        world.Events.Add(LimitlessCast + 36.38f, () => cosmoM?.Cast(ActionId.CosmoMemoryM, animationLock: 0.3f));
        world.Events.Add(CosmoMemoryHit, () => helpers[5]?.Cast(Actions.CosmoMemoryAoe));
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.OmegaP2,
        Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private void SpawnTethers()
    {
        for (var i = 0; i < 2; i++)
            tethers[i] = world.Tether(bladedancers[i], End.Passable(world.Party.Get(state.TetherSeeds[i]), exclusive: true), TetherId.OptimizedBladedance);
    }

    // The arrow turns on whoever is closest as its cast starts, and keeps that line.
    private void AimSagittarius()
    {
        if (sagittarius == null || world.Party.Find.Closest(sagittarius.Position) is not { } closest) return;
        sagittarius.SetRotation(new Placement(sagittarius.Position, 0f).Face(closest.Position).Rotation);
        sagittarius.Cast(Actions.OptimizedSagittariusArrow);
    }

    private void Bladedance()
    {
        for (var i = 0; i < 2; i++)
        {
            if (tethers[i]?.B is not { } holder || bladedancers[i] is not { } clone) continue;
            helpers[i]?.SetPosition(new Placement(clone.Position, 0f).Face(holder.Position));
            helpers[i]?.Cast(Actions.OptimizedBladedanceAoe, holder);
            tethers[i]?.Despawn();
        }
    }

    private SimCharacter? OneOfTwoClosest(Vector3 from, bool second, SimCharacter? excluded)
    {
        var closest = world.Party.ActiveMembers().Where(m => m.IsAlive() && m != excluded)
            .OrderBy(m => Vector3.DistanceSquared(m.Position, from)).Take(2).ToList();
        return closest.Count == 0 ? null : closest[second && closest.Count > 1 ? 1 : 0];
    }

    private void JumpOnTank()
    {
        if (shieldClone == null || OneOfTwoClosest(shieldClone.Position, state.BeyondDefenseOnSecondClosest, null) is not { } target) return;
        jumped = target;
        shieldClone.SetPosition(new Placement(target.Position, shieldClone.Rotation));
        shieldClone.Cast(Actions.BeyondDefenseAoe, target);
    }

    private void Flares()
    {
        for (var i = 0; i < state.Flares.Count; i++)
            if (world.Party.Get(state.Flares[i]) is { } flare && flare.IsAlive())
                helpers[2 + i]?.Cast(Actions.OptimizedMeteorAoe, flare);
    }

    private void PilePitch()
    {
        if (shieldClone == null || OneOfTwoClosest(shieldClone.Position, state.PilePitchOnSecondClosest, jumped) is not { } target) return;
        shieldClone.Cast(Actions.PilePitch, target);
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new TopLimitlessSynergyAiReplayStateMessage(s.EncodeBearings(), s.EncodeFlags(), s.TetherSeeds.ToArray(), s.Flares.ToArray()) : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not TopLimitlessSynergyAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (TopLimitlessSynergyState.FromNetworkReplay(msg.Bearings, msg.Flags, msg.TetherSeeds, msg.Flares) is not { } shadow) return null;
        ((IScenarioAi<TopLimitlessSynergyState>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }
}
