using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace AnoMech.Core.SimObjects;

// A caster's server-shaped cast packets: the ActorCast that raises the bar and omen, the
// ActionEffect that plays the release, and the interrupt. Holds the animation lock that roots
// the caster after a release. Positions in and out are scenario-local; they go world-space only
// at the proxy.
public sealed class SimCast : ISimObject
{
    private readonly SimCharacter parent;
    private readonly Coordinates coordinates;

    private float remainingAnimationLock;
    private float remainingCastTime;
    // Packet-spawned helpers have no native slot contents until a later client frame.
    private ActorCastData? pendingCast;
    private ActorCastData? delayedOmenCast;
    private bool presentationPaused;

    public bool IsCasting => parent.Proxy?.IsCasting ?? false;

    // Read live from CastInfo against the bar's own length.
    public float Progress
    {
        get
        {
            if (parent.Proxy is not { Exists: true } chara || Total <= 0f) return 0f;
            return Math.Clamp(chara.CurrentCastTime / Total, 0f, 1f);
        }
    }

    public int CastSeq { get; private set; }
    public int EffectSeq { get; private set; }
    public uint EffectActionId { get; private set; }
    public uint ActionId { get; private set; }
    public float Total { get; private set; }
    public float OmenDelay { get; private set; }
    // Absolute, with any omen offset already folded in.
    public float Rotation { get; private set; }
    public Vector3? TargetLocation { get; private set; }
    public GameObjectId? TargetId { get; private set; }

    // True while the cast bar is up or the release animation is still playing. A following boss
    // roots itself while busy so the action animation finishes in place instead of sliding.
    public bool IsBusy => remainingCastTime > 0f || remainingAnimationLock > 0f;

    // Owned and ticked by its SimCharacter, never reaped by liveness.
    public bool IsActive => true;

    internal SimCast(SimCharacter parent, Coordinates coordinates)
    {
        this.parent = parent;
        this.coordinates = coordinates;
    }

    public void NativeCast(uint actionId, ActionType actionType, float omenDelay, float castTime, bool interruptible, float? rotation = null, Vector3? position = null, GameObjectId? targetId = null, GameObjectId? ballistaId = null)
    {
        var castRotation = rotation ?? parent.Rotation;
        var cast = new ActorCastData(
            actionId, actionType, castTime, omenDelay, interruptible,
            castRotation, coordinates.ToGlobal(position ?? parent.Position), targetId, ballistaId);
        pendingCast = null;
        delayedOmenCast = omenDelay > 0f ? cast : null;
        presentationPaused = false;
        if (parent is SimNpc { PacketSpawnPending: true }) pendingCast = cast;
        else parent.Proxy?.ReceiveActorCast(cast);

        ActionId = actionId;
        Total = castTime;
        remainingCastTime = castTime;
        OmenDelay = omenDelay;
        Rotation = castRotation;
        TargetLocation = position;
        TargetId = targetId;
        CastSeq++;
    }

    // Returns the global sequence the targets' EffectResults name; 0 when nothing was sent.
    public uint NativeActionEffect(uint actionId, float animationLock, ushort spellId, byte animationVariation, ActionType actionType, byte flags, float? rotation = null, Vector3? position = null, GameObjectId? animationTargetId = null, IReadOnlyList<ActionEffectTarget>? targets = null, GameObjectId? ballistaId = null, GameObjectId? actionTargetId = null)
    {
        pendingCast = null;
        delayedOmenCast = null;
        if (parent.Proxy is not { Exists: true } chara) return 0;
        targets ??= actionTargetId is { } target ? [new ActionEffectTarget(target)] : null;

        var effectRotation = rotation ?? parent.Rotation;
        var sequence = chara.ReceiveActionEffect(new ActionEffectData(
            actionId, actionType, animationLock, spellId, animationVariation, flags,
            effectRotation, coordinates.ToGlobal(position ?? Vector3.Zero),
            animationTargetId, targets, ballistaId));

        EffectActionId = actionId;
        EffectSeq++;
        remainingAnimationLock = animationLock;
        remainingCastTime = 0;
        return sequence;
    }

    public void Cancel(CastCancelReason reason)
    {
        pendingCast = null;
        delayedOmenCast = null;
        remainingCastTime = 0;
        parent.ActorControl.CancelCast(ActionId, reason);
    }

    internal void PausePresentation(bool paused)
    {
        if (presentationPaused == paused) return;
        presentationPaused = paused;
        if (delayedOmenCast is not { } cast || remainingCastTime <= 0f) return;
        // Native omen countdowns keep running while the simulation clock is stopped.
        if (paused) parent.ActorControl.CancelCast(ActionId, CastCancelReason.Silent);
        else pendingCast = cast;
    }

    public void Tick(float deltaSeconds)
    {
        if (presentationPaused) return;
        remainingCastTime = MathF.Max(0, remainingCastTime - deltaSeconds);
        if (remainingCastTime <= 0f) pendingCast = null;
        if (pendingCast is { } cast && parent is not SimNpc { PacketSpawnPending: true }
            && parent.Proxy is { Exists: true } chara)
        {
            pendingCast = null;
            chara.ReceiveActorCast(cast with
            {
                CastTime = remainingCastTime,
                OmenDelay = MathF.Max(0f, cast.OmenDelay - (Total - remainingCastTime)),
            });
        }
        if (remainingAnimationLock > 0f)
            remainingAnimationLock = MathF.Max(0f, remainingAnimationLock - deltaSeconds);
    }

    // Character::Terminate's scheduler teardown crashes on a still-live cast timeline
    // (C0000005 at TimelineGroup.PlayAction), so the cast is called off before the despawn.
    public void Despawn()
    {
        pendingCast = null;
        delayedOmenCast = null;
        remainingCastTime = remainingAnimationLock = 0;
        presentationPaused = false;
        if (parent.Proxy is { IsCasting: true } chara)
            parent.ActorControl.CancelCast(chara.CastActionId, CastCancelReason.Silent);
    }
}
