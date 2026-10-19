using System;
using System.Collections.Generic;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Motion;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.UserActions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.SimObjects;

public sealed class SimPartyNpc : SimNpc, ISimPartyMember
{
    public PartyRole Role { get; set; }
    public bool PresentationVisible { get; private set; } = true;
    public void SetPresentationVisible(bool visible)
    {
        PresentationVisible = visible;
        if (Proxy is { Exists: true } proxy) proxy.SetModelHidden(!visible);
    }
    public byte ClassJob { get; }
    public string DisplayName { get; }

    internal SimPartyNpc(IBattleCharaProxy proxy, Coordinates coordinates, PartyRole role, byte classJob, string name, ObstacleField obstacles)
        : base(proxy, coordinates,
            self => new SimulatedMotion(() => self.Proxy, coordinates, self.ActorControl, obstacles, turnSpeed: null, npc: false))
    {
        Role = role;
        ClassJob = classJob;
        DisplayName = name;
    }

    // Level 1-3; zero when the actor cannot act or the job has no limit break at that level.
    internal uint UseLimitBreak(int level)
    {
        if (!this.IsAlive() || ActionsLocked) return 0;
        var actionId = LimitBreakHandler.ActionId(ClassJob, level);
        if (actionId == 0) return 0;
        PlayAction(actionId);
        JobActions.ApplyEffects(this, actionId, (ulong)GameObjectId, Random.Shared);
        DiagnosticLog.Info($"[SimPartyNpc] {Role} (job {ClassJob}) uses LB{level} {Natives.Data.ActionName(actionId)}.");
        return actionId;
    }

    public void UseSprint(float duration)
    {
        if (!ActionsLocked) SprintHandler.Apply(this, duration);
    }

    public override void Despawn()
    {
        base.Despawn();
    }

    protected override void OnPacketSpawned(IBattleCharaProxy chara)
    {
        ActorControl.ZoneIn();
        chara.SetModelHidden(!PresentationVisible);
    }

    public override void Tick(float deltaSeconds)
    {
        base.Tick(deltaSeconds);
        if (PacketSpawnFrames is 30 or 180 && Proxy is { Exists: true } proxy)
            DiagnosticLog.Info($"[SimPartyNpc] {Role}, presentation visible={PresentationVisible}, {proxy.DescribeDrawState()}");
    }

    public void OnKilled(bool byEnvironment)
    {
        GoDown(byEnvironment);
    }
}
