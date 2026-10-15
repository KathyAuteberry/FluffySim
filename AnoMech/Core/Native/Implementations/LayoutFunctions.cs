using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Native.Implementations.Interop;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.Native.Implementations;

// Some zones' client-side load activates every layer at once (there is no real duty director
// selecting the current phase's), so geometry from different phases renders in the same space
// and z-fights; the losing layers are held inactive.
internal sealed unsafe class LayoutFunctions : ILayoutFunctions
{
    private readonly List<nint> suppressedLayerInstances = new();
    private readonly Dictionary<(uint Territory, ushort Layer), bool> heldLayerStates = new();

    private readonly Dictionary<(uint Territory, ushort Layer, uint Instance), bool> heldInstanceStates = new();

    public void HoldInstanceActive(uint territoryId, ushort layerKey, uint instanceKey, bool active)
        => heldInstanceStates[(territoryId, layerKey, instanceKey)] = active;

    public void HoldLayerActive(uint territoryId, ushort layerKey, bool active)
        => heldLayerStates[(territoryId, layerKey)] = active;

    public int SuppressLayer(ushort layerKey)
    {
        var instances = LayoutQuery.CollectLayerInstances(layerKey);
        suppressedLayerInstances.AddRange(instances);
        return instances.Count;
    }

    public void ReassertSuppressedLayers()
    {
        foreach (var ptr in suppressedLayerInstances)
            ((ILayoutInstance*)ptr)->SetActive(false);
        TraceOfflineDsr();
        if (heldLayerStates.Count == 0 && heldInstanceStates.Count == 0) return;
        var world = LayoutWorld.Instance();
        var game = GameMain.Instance();
        if (world == null || game == null || world->ActiveLayout == null
            || world->ActiveLayout->InitState != 7 || game->TerritoryLoadState != 2
            || world->ActiveLayout->TerritoryTypeId != game->CurrentTerritoryTypeId) return;
        ReassertHeldLayers(game->CurrentTerritoryTypeId, true, LayoutQuery.CollectLayerInstances,
            static p => ((ILayoutInstance*)p)->Id.InstanceKey,
            static p => ((ILayoutInstance*)p)->IsActive,
            static (p, active) => ((ILayoutInstance*)p)->SetActive(active));
    }

    internal void ReassertHeldLayers(uint territory, bool ready,
        Func<ushort, IReadOnlyList<nint>> instances,
        Func<nint, uint> instanceKey, Func<nint, bool> isActive, Action<nint, bool> setActive)
    {
        if (!ready) return;
        foreach (var (key, active) in heldLayerStates)
        {
            if (key.Territory != territory) continue;
            foreach (var ptr in instances(key.Layer))
                if (isActive(ptr) != active) setActive(ptr, active);
        }
        foreach (var (key, active) in heldInstanceStates)
        {
            if (key.Territory != territory) continue;
            foreach (var ptr in instances(key.Layer))
                if (instanceKey(ptr) == key.Instance && isActive(ptr) != active) setActive(ptr, active);
        }
    }

    private string? lastOfflineDsrState;

    private void TraceOfflineDsr()
    {
        if (!AnoMech.Offline.OfflineSession.InWorld || !AnoMech.Offline.OfflineProbe.TraceEnvironment) return;
        var game = GameMain.Instance();
        var world = LayoutWorld.Instance();
        var layout = world == null ? null : world->ActiveLayout;
        if (game == null || game->CurrentTerritoryTypeId != 968 || layout == null) return;
        var state = $"layout=0x{(nint)layout:X} init={layout->InitState} load={game->TerritoryLoadState} requested=P2 verified={LayoutQuery.MatchesDsrP2()}";
        foreach (ushort key in new ushort[] {60988, 61049, 61050, 60991, 62172})
        {
            var instances = LayoutQuery.CollectLayerInstances(key);
            var active = 0;
            foreach (var ptr in instances) if (((ILayoutInstance*)ptr)->IsActive) active++;
            state += $" layer={key}:{active}/{instances.Count}";
        }
        if (state == lastOfflineDsrState) return;
        DiagnosticLog.Info($"[OfflineDsrLayout] monotonic={System.Diagnostics.Stopwatch.GetTimestamp()} sim={Plugin.Game?.EventClockNow:F3} observer=LayoutFunctions previous=[{lastOfflineDsrState ?? "none"}] actual=[{state}]");
        lastOfflineDsrState = state;
    }

    public void ClearSuppressedLayers()
    {
        lastOfflineDsrState = null;
        suppressedLayerInstances.Clear();
        heldLayerStates.Clear();
        heldInstanceStates.Clear();
    }


    public string DescribeActiveLayers() => LayoutQuery.DescribeActiveLayers();

    public string DescribeLiveEffects() => LayoutQuery.DescribeLiveEffects();
}
