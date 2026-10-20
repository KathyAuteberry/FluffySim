using System;
using AnoMech.Core;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using Upstream = AnoMech.Scenarios.Fru.Upstream;
using AnoMech.Scenarios.Fru.Upstream.P2DiamondDust;

namespace AnoMech.Scenarios.Fru.DiamondDust;

public sealed class FruDiamondDustScenario : IScenario
{
    public string Name => "Diamond Dust";
    public bool IsIncomplete => false;
    public IPhase Phase => FruZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruDiamondDustAi()];
    private readonly FruDiamondDustSettingsWindow settings = new();
    public FruDiamondDustStateOverrides Overrides => settings.Overrides;
    public void DrawSettings() => settings.Draw();
    public object SettingsOverrides => Overrides;
    public FruDiamondDustState? LastState { get; private set; }
    public bool PrefixCompleted { get; private set; }
    private readonly Upstream.FruHolyPuddles puddles = new();
    private const float ContinuationOffset = 1.03f;
    // UNVERIFIED: earliest puddle damage is not constrained by clean recordings.
    private const float PuddleArmsAfterShowing = 1.5f;
    private SimWorld world = null!;
    private FruDiamondDustState state = null!;
    private SimEnemy boss = null!, oracle = null!;
    private readonly List<SimEnemy> helpers = [];
    private readonly List<Vector3> stonePositions = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new(world.Rng, Overrides);
        LastState = state;
        PrefixCompleted = false;
        helpers.Clear(); stonePositions.Clear(); puddles.Clear();
        world.Events.Add(0f, () =>
        {
            boss = Spawn(BNpcBaseId.UsurperOfFrost, BNpcNameId.UsurperOfFrost, new(0f, 0f, 5f), true);
            oracle = Spawn(BNpcBaseId.OraclesReflection, BNpcNameId.OraclesReflection, Vector3.Zero, true);
            oracle.SetModelHidden(true);
            // Packet spawning needs framework ticks before a native cast can be delivered.
            for (var i = 0; i < 28; i++) helpers.Add(Spawn(BNpcBaseId.Helper, BNpcNameId.UsurperOfFrost, Vector3.Zero, false));
        });
        world.Events.Add(3f, BeginMirrorImage);
        world.Events.Add(8.1f, () =>
        {
            world.Map.AddEffect(0x00020001, 0);
            boss.Cast(FruDiamondDustActions.Raidwide);
        });
        world.Events.Add(9.12f, () => { oracle.SetModelHidden(false); oracle.PlayActionTimeline(0x11D6); });
        world.Events.Add(12.2f, () => world.Map.AddEffect(0x00080004, 0));
        world.Events.Add(15.78f, () =>
        {
            oracle.SetPosition(Vector3.Zero);
            CastIcicles(0);
            if (!state.Axe) oracle.Cast(FruDiamondDustActions.Scythe);
        });
        world.Events.Add(16.04f, () => { if (state.Axe) oracle.Cast(FruDiamondDustActions.Axe); });
        world.Events.Add(16.26f, () =>
        {
            boss.SetTargetable(false);
            boss.PlayActionTimeline(0x11E5);

            foreach (var role in Enum.GetValues<PartyRole>().Where(state.HasStone))
                world.Party.Get(role)!.AttachLockonVfx(345, persistent: false);
        });
        world.Events.Add(16.77f, () => { if (!state.Axe) oracle.SetAnimationState(0xF9, 0); });
        world.Events.Add(17.04f, () => { if (state.Axe) oracle.SetAnimationState(0xF9, 0); });
        world.Events.Add(18.30f, () => boss.SetModelHidden(true));
        world.Events.Add(19.79f, () => CastIcicles(1));
        world.Events.Add(22.58f, () => { if (!state.Axe) ResolveCones(); });
        world.Events.Add(22.84f, () => { if (state.Axe) ResolveCones(); });
        world.Events.Add(23.10f, () => { if (!state.Axe) oracle.SetAnimationState(0xFC, 0); });
        world.Events.Add(23.35f, () => { if (state.Axe) oracle.SetAnimationState(0xFC, 0); });
        world.Events.Add(23.8f, () => CastIcicles(2));
        world.Events.Add(24.34f, ResolveStones);
        world.Events.Add(26.42f, CastNeedles);
        world.Events.Add(26.55f, () =>
        {
            boss.SetPosition(Vector3.Zero);
            boss.PlayActionTimeline(0x1E43);
            boss.SetModelHidden(false);
            oracle.SetPosition(new Placement(FruDiamondDustState.At(state.OracleSector, 10f),
                new Placement(FruDiamondDustState.At(state.OracleSector, 10f), 0f).Face(Vector3.Zero).Rotation));
        });
        world.Events.Add(28.63f, () => boss.Cast(FruDiamondDustActions.Knockback));
        world.Events.Add(31.6f, EndPrefix);
        ScheduleContinuation(selectedAi);
        world.Events.Add(64f, CleanupActors);
        if (selectedAi is { } index) ((IScenarioAi<FruDiamondDustState>)AiStrats[index]).Run(state, world);
    }

    private void BeginMirrorImage()
    {
        if (!NativeActorReadiness.Check(helpers.Append(boss).Append(oracle), "FRU Diamond Dust / Mirror Image"))
        {
            DiagnosticLog.Warn("[FRU Diamond Dust] Native actors were not ready before Mirror Image; cancelling the prefix.");
            Plugin.Game.RecordMechanicMistake(ActionId.MirrorImage, "Native actor setup incomplete; leave and retry.");
            world.Events.Clear();
            CleanupActors();
            return;
        }
        boss.Cast(new EnemyAction(ActionId.MirrorImage) { Cast = new() { TargetsCasterForPresentation = true, AnimationLock = 2.1f } });
    }

    private SimEnemy Spawn(uint baseId, uint nameId, Vector3 position, bool visible)
    {
        var actor = world.SpawnEnemy(new EnemySpawnConfig(baseId, nameId, Level: 100,
            Targetable: baseId == BNpcBaseId.UsurperOfFrost,
            EnemyList: baseId == BNpcBaseId.UsurperOfFrost ? EnemyListMode.Always
                : baseId == BNpcBaseId.OraclesReflection ? EnemyListMode.OnlyWhenVisible : EnemyListMode.Never,
            Visibility: visible ? SpawnVisibility.Visible : SpawnVisibility.InvisibleHelper, Placement: new(position, MathF.PI)))
            ?? throw new InvalidOperationException($"Could not spawn Diamond Dust actor {baseId}.");
        return actor;
    }
    private SimEnemy Helper(int index, Vector3 position, float rotation = 0f)
    {
        var actor = helpers[index];
        actor.SetPosition(new Placement(position, rotation));
        return actor;
    }
    private void CastIcicles(int wave)
    {
        var count = wave == 1 ? 4 : 2;
        for (var i = 0; i < count; i++)
        {
            var position = state.IciclePosition(wave, i);
            Helper((wave == 0 ? 0 : wave == 1 ? 2 : 6) + i, position,
                new Placement(position, 0f).Face(Vector3.Zero).Rotation).Cast(FruDiamondDustActions.Icicle, position);
        }
    }
    private void ResolveCones()
    {
        var index = 8;
        foreach (var member in Enumerable.Range(0, 8).Select(i => world.Party.Get(i)!)
            .Where(m => m.IsAlive()).OrderBy(m => m.Position.LengthSquared()).Take(4))
        {
            var actor = Helper(index++, Vector3.Zero);
            actor.SetPosition(new Placement(Vector3.Zero, new Placement(Vector3.Zero, 0f).Face(member.Position).Rotation));
            actor.Cast(FruDiamondDustActions.Cone, member);
        }
    }
    private void ResolveStones()
    {
        var index = 12;
        foreach (var role in Enum.GetValues<PartyRole>().Where(state.HasStone))
        {
            var member = world.Party.Get(role)!;
            stonePositions.Add(member.Position);
            var actor = Helper(index++, member.Position);
            actor.Cast(FruDiamondDustActions.Stone, member);
        }
    }
    private void CastNeedles()
    {
        var index = 16;
        foreach (var position in stonePositions)
        {
            Helper(index++, position).Cast(FruDiamondDustActions.NeedleCircle);
            Helper(index++, position).Cast(FruDiamondDustActions.NeedleCross);
            var diagonal = Helper(index++, position);
            diagonal.SetPosition(new Placement(position, -MathF.PI / 4f));
            diagonal.Cast(FruDiamondDustActions.NeedleCross);
        }
    }
    private void ScheduleContinuation(int? selectedAi)
    {
        var continuation = new FruP2DiamondDustState(world.Rng, new()
        {
            Axe = state.Axe, SupportsMarked = state.StonesOnSupports, FirstIcicle = state.FirstIcicleAxis,
            MirrorOctant = state.OracleSector, Stillness = state.Stillness, GazeOctant = state.GazeSector,
        });
        if (selectedAi is not null) new FruP2DiamondDustAi(mana: true, continuationOnly: true, offset: ContinuationOffset).Run(continuation, world);
        void At(float time, Action action) => world.Events.Add(time + ContinuationOffset, action);
        At(25.8f, () => oracle.Cast(Upstream.FruConstants.ActionId.SinboundHolyCast, animationLock: 3.1f));
        At(30.65f, () => boss.PlayActionTimeline(Upstream.FruConstants.TimelineId.UsurperHide));
        for (var wave = 0; wave < FruP2DiamondDustState.HolyWaves; wave++)
        {
            var w = wave;
            var hit = FruP2DiamondDustState.HolyAt(w);
            var show = hit + FruP2DiamondDustState.PuddleShowsAfterHoly;
            At(hit - Upstream.FruHolyPuddles.SnapshotBeforeHit, SnapshotHealers);
            At(hit, () =>
            {
                var i = 8;
                foreach (var role in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer })
                {
                    if (world.Party.Get(role) is not { } healer || !healer.IsAlive()) continue;
                    Helper(i++, healer.Position).Cast(Upstream.FruActions.SinboundHoly, healer);
                    puddles.Drop(w, healer, show + ContinuationOffset + PuddleArmsAfterShowing);
                }
            });
            At(show, () => puddles.Show(world, w));
            At(FruP2DiamondDustState.PuddleFadesAt(w), () => puddles.Fade(w));
            At(show + 14.1f, () => puddles.Remove(w));
        }
        At(32.75f, () => boss.SetPosition(new Placement(continuation.GazePosition, 0f).Face(Vector3.Zero)));
        At(32.87f, () => { boss.SetModelHidden(false); boss.PlayActionTimeline(ActionTimelineId.WarpEnd); });
        At(34.9f, () =>
        {
            oracle.ActorControl.Transform(Upstream.FruConstants.MirrorIceBNpcState);
            oracle.AddStatusParam(Upstream.FruConstants.TransfigurationStatus, Upstream.FruConstants.MirrorIceTransformation);
            boss.SetModeAttributeFlags(0xFC);
            boss.ActorControl.Transform(Upstream.FruConstants.UsurperLightBNpcState);
            boss.AddStatusParam(Upstream.FruConstants.TransfigurationStatus, Upstream.FruConstants.UsurperLightTransformation);
        });
        At(40.05f, () =>
        {
            Helper(0, continuation.GazePosition).Cast(Upstream.FruActions.ShiningArmor);
            Helper(1, continuation.MirrorPosition).Cast(Upstream.FruConstants.ActionId.FrostArmor, animationLock: 2.1f);
        });
        At(FruP2DiamondDustState.IceStart, () =>
        {
            oracle.SetModeAttributeFlags(0xF0);
            world.Map.AddEffect(Upstream.FruConstants.MapEffect.FreezeFloor, Upstream.FruConstants.MapEffect.P2Platform);
            foreach (var member in world.Party.ActiveMembers())
                member.AddStatusParam(Upstream.FruConstants.StatusId.ThinIce, Upstream.FruConstants.ThinIceSlideParam,
                    FruP2DiamondDustState.IceEnd - FruP2DiamondDustState.IceStart);
        });
        At(42.9f, () => oracle.SetModeAttributeFlags(0xF3));
        At(43f, () => boss.PlayActionTimeline(Upstream.FruConstants.TimelineId.UsurperHide));
        At(43f, () => oracle.VoiceLine(state.Stillness ? Upstream.FruConstants.VoiceLineId.TwinStillness : Upstream.FruConstants.VoiceLineId.TwinSilence));
        At(43f, () => oracle.Cast(state.Stillness ? Upstream.FruActions.TwinStillnessFirst : Upstream.FruActions.TwinSilenceFirst));
        At(47.28f, () => oracle.SetModelState(5));
        At(48.57f, () => oracle.SetModeAttributeFlags(0xF0));
        At(48.65f, () => oracle.Cast(state.Stillness ? Upstream.FruActions.TwinStillnessSecond : Upstream.FruActions.TwinSilenceSecond));
        At(49.54f, () => oracle.SetModelState(0));
        At(51.74f, () => oracle.PlayActionTimeline(Upstream.FruConstants.TimelineId.MirrorHide));
        At(52.17f, () =>
        {
            world.Map.AddEffect(Upstream.FruConstants.MapEffect.ThawFloor, Upstream.FruConstants.MapEffect.P2Platform);
            boss.SetPosition(new Placement(Vector3.Zero, MathF.PI));
            boss.SetModelHidden(false);
            boss.SetTargetable(true);
            boss.PlayActionTimeline(ActionTimelineId.WarpEnd);
        });
        At(54.3f, () => { boss.Cast(40210, animationLock: 2.1f); world.Party.Get(state.RayTarget)?.AttachLockonVfx(525); });
        At(59.9f, () =>
        {
            if (world.Party.Get(state.RayTarget) is not { } player) return;
            Helper(0, boss.Position, new Placement(boss.Position, 0).Face(player.Position).Rotation).Cast(new EnemyAction(40211)
            {
                Effects = [EnemyActionEffects.Damage(split: Distribution.Stack(8))],
                Cast = new() { AnimationLock = 2.1f },
            }, player);
        });
    }

    private void SnapshotHealers()
    {
        foreach (var role in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer })
            if (world.Party.Get(role) is { } healer && healer.IsAlive()) puddles.Snapshot(healer);
    }

    public void Tick(float delta, float elapsed)
        => puddles.KillWhoeverStandsIn(world.Party, elapsed, Upstream.FruConstants.ActionId.SinboundHoly, "stepped in a Sinbound Holy puddle");

    private void EndPrefix()
    {
        PrefixCompleted = true;
    }
    private void CleanupActors()
    {
        foreach (var actor in helpers.Append(oracle).Append(boss)) actor.Despawn();
        helpers.Clear(); stonePositions.Clear();
    }
}
