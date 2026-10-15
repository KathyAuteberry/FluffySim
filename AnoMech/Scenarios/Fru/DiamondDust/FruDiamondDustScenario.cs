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

namespace AnoMech.Scenarios.Fru.DiamondDust;

public sealed class FruDiamondDustScenario : IScenario
{
    public string Name => "Diamond Dust (Incomplete)";
    public bool IsIncomplete => true;
    public IPhase Phase => FruZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruDiamondDustAi()];
    private readonly FruDiamondDustSettingsWindow settings = new();
    public FruDiamondDustStateOverrides Overrides => settings.Overrides;
    public void DrawSettings() => settings.Draw();
    public object SettingsOverrides => Overrides;
    public FruDiamondDustState? LastState { get; private set; }
    public bool PrefixCompleted { get; private set; }
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
        helpers.Clear(); stonePositions.Clear();
        world.Events.Add(0f, () =>
        {
            boss = Spawn(BNpcBaseId.UsurperOfFrost, BNpcNameId.UsurperOfFrost, new(0f, 0f, 5f), true);
            oracle = Spawn(BNpcBaseId.OraclesReflection, BNpcNameId.OraclesReflection, Vector3.Zero, true);
            // Packet spawning needs framework ticks before a native cast can be delivered.
            for (var i = 0; i < 28; i++) helpers.Add(Spawn(BNpcBaseId.Helper, BNpcNameId.UsurperOfFrost, Vector3.Zero, false));
        });
        world.Events.Add(3f, BeginMirrorImage);
        world.Events.Add(8.1f, () =>
        {
            world.Map.AddEffect(0x00020001, 0);
            boss.Cast(FruDiamondDustActions.Raidwide);
        });
        world.Events.Add(9.12f, () => oracle.PlayActionTimeline(0x11D6));
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
        world.Events.Add(32.52f, CleanupActors);
        if (selectedAi is { } index) ((IScenarioAi<FruDiamondDustState>)AiStrats[index]).Run(state, world);
    }

    private void BeginMirrorImage()
    {
        if (helpers.Append(boss).Append(oracle).Any(a => a.PacketSpawnFailed || a.Proxy is not { Exists: true, HasDrawObject: true }))
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
    private void EndPrefix()
    {
        foreach (var actor in helpers.Append(oracle).Append(boss)) actor.CancelCast(CastCancelReason.Cancelled);
        PrefixCompleted = true;
    }
    private void CleanupActors()
    {
        foreach (var actor in helpers.Append(oracle).Append(boss)) actor.Despawn();
        helpers.Clear(); stonePositions.Clear();
    }
}
