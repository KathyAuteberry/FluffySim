using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.Native.Policies;
using AnoMech.Core.UserActions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AnoMech.Core.Native.Implementations;

public sealed unsafe partial class ZoneControl
{
    void IZoneControl.EndSpeedBuffs() => EndSpeedBuffs();

    private readonly PacketFilterPolicy outboundPolicy;
    private readonly PracticeRecastSession practiceRecasts;
    private readonly HashSet<uint> pendingPracticeRecasts = [];
    private byte recastJob;
    private long? revertedAt;
    public bool NativePracticeReady => IsActive && guardArmed && tripReason == null && practiceRecasts.Captured && FirewallIntact && Plugin.Config.SafeMode;
    private bool FirewallIntact => sendPacketHook.IsEnabled && !sendPacketHook.IsDisposed && receivePacketHook.IsEnabled && !receivePacketHook.IsDisposed;
    public string FirewallStatus => $"send={sendPacketHook.IsEnabled}, receive={receivePacketHook.IsEnabled}, session={IsActive}, restoring={guardArmed && !IsActive}, native practice={NativePracticeReady}; intercepted={outboundPolicy.Intercepted}, heartbeat allowed={outboundPolicy.Allowed}, non-heartbeat/unclassified blocked={outboundPolicy.Blocked}, unreadable={outboundPolicy.Invalid}";

    internal static void VerifyActionIsolation()
    {
        if (Current is { } session && (session.IsActive || session.guardArmed) && (!session.FirewallIntact || session.tripReason != null))
            Die("a native action was attempted without both session filters");
    }

    public void NotePracticeAction(uint action)
    {
        var am = ActionManager.Instance();
        if (am != null)
        {
            var group = am->GetRecastGroup((int)ActionType.Action, action);
            pendingPracticeRecasts.RemoveWhere(candidate => am->GetRecastGroup((int)ActionType.Action, candidate) == group);
        }
        if (IsActive && FirewallIntact && recastJob == Plugin.PlayerState.ClassJob.RowId) practiceRecasts.Track(action);
    }

    public void SetPracticeRecastsPaused(bool paused)
    {
        if (IsActive && FirewallIntact) practiceRecasts.SetPaused(paused);
        else practiceRecasts.ForgetPause();
    }

    public void ResetPracticeRecast(uint action)
    {
        if (!IsActive || !FirewallIntact || recastJob != Plugin.PlayerState.ClassJob.RowId) return;
        pendingPracticeRecasts.Add(action);
        FlushPracticeRecasts();
    }
    private void FlushPracticeRecasts()
    {
        if (!NativePracticeReady || recastJob != Plugin.PlayerState.ClassJob.RowId) return;
        foreach (var action in pendingPracticeRecasts.ToArray())
            if (practiceRecasts.Reset(action)) pendingPracticeRecasts.Remove(action);
    }
    private void QuiesceLocalActions()
    {
        var am = ActionManager.Instance();
        if (am == null) Die("action manager unavailable while stopping local actions");
        am->ActionQueued = false;
        am->AreaTargetingActionId = 0;
        am->AreaTargetingExecuteAtCursor = false;
        var ui = UIState.Instance();
        if (ui != null && ui->WeaponState.AutoAttackState.IsAutoAttacking)
            ui->WeaponState.AutoAttackState.SetImpl(false, false, true);
        if (Plugin.ObjectTable.LocalPlayer is { IsCasting: true } player)
            new Core.Game.ActorControl(() => BattleCharaProxy.LocalPlayer).CancelCast(player.CastActionId, Core.Game.CastCancelReason.SelfCancelled);
    }

}
