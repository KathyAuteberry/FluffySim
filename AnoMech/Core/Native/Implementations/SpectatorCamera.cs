using System;
using AnoMech.Core.Native.Interfaces;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Core.Native.Implementations;

internal sealed unsafe class SpectatorCamera(IGameInteropProvider interop) : ISpectatorCamera
{
    private delegate GameObject* CameraTargetDelegate(Camera* camera);
    private Hook<CameraTargetDelegate>? targetHook;
    private IBattleCharaProxy? target;
    private Camera* ownedCamera;
    private uint targetEntityId;
    private CameraSnapshot? snapshot;
    private IBattleCharaProxy? parkedPlayer;
    private bool playerVisible;

    public bool Available
    {
        get
        {
            var manager = CameraManager.Instance();
            return manager != null && manager->Camera != null && manager->ActiveCameraIndex == 0;
        }
    }

    public void Begin(IBattleCharaProxy actor)
    {
        End();
        if (!Natives.Zone.IsActive || !Available || actor.Slot < 0)
            throw new InvalidOperationException("An isolated stay and a native actor-follow camera are required.");
        ownedCamera = CameraManager.Instance()->Camera;
        // UNVERIFIED in the native client: redirect only the normal camera's actor lookup,
        // leaving its mouse input, collision, zoom and yaw/pitch calculation in the engine.
        targetHook ??= interop.HookFromAddress<CameraTargetDelegate>(
            (nint)ownedCamera->VirtualTable->GetCameraTargetObject, TargetDetour);
        snapshot = CameraSnapshot.Capture(ownedCamera);
        target = actor;
        targetEntityId = actor.Exists ? actor.EntityId : 0;
        parkedPlayer = Natives.BattleCharas.LocalPlayer;
        playerVisible = parkedPlayer.IsDrawObjectVisible;
        parkedPlayer.IsDrawObjectVisible = false;
        Natives.PlayerInput.Steer(null, 0);
        Natives.PlayerInput.SpectatorInput = true;
        try { targetHook.Enable(); }
        catch { End(); throw; }
    }

    private GameObject* TargetDetour(Camera* camera)
    {
        if (camera == ownedCamera && Natives.Zone.IsActive && target is { Exists: true, Slot: >= 0 } actor)
        {
            if (CharacterManager.Instance() != null)
            {
                var ptr = BattleCharaProxy.PtrAt(actor.Slot);
                if (ptr != null)
                {
                    if (targetEntityId == 0) targetEntityId = actor.EntityId;
                    if (ptr->EntityId == targetEntityId) return (GameObject*)ptr;
                }
            }
        }
        return targetHook!.Original(camera);
    }

    public void End()
    {
        target = null;
        try { targetHook?.Disable(); }
        finally
        {
            Natives.PlayerInput.SpectatorInput = false;
            var manager = CameraManager.Instance();
            if (snapshot is { } saved && manager != null && manager->Camera == ownedCamera)
            {
                saved.Restore(ownedCamera);
            }
            if (parkedPlayer is { Exists: true } player) player.IsDrawObjectVisible = playerVisible;
            parkedPlayer = null;
            snapshot = null;
            ownedCamera = null;
        }
    }

    public void Dispose()
    {
        End();
        targetHook?.Dispose();
        targetHook = null;
    }
}
