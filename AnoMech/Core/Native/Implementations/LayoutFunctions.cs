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
        if (heldLayerStates.Count == 0) return;
        var world = LayoutWorld.Instance();
        var game = GameMain.Instance();
        if (world == null || game == null || world->ActiveLayout == null
            || world->ActiveLayout->InitState != 7 || game->TerritoryLoadState != 2) return;
        foreach (var (key, active) in heldLayerStates)
        {
            if (world->ActiveLayout->TerritoryTypeId != key.Territory || game->CurrentTerritoryTypeId != key.Territory) continue;
            foreach (var ptr in LayoutQuery.CollectLayerInstances(key.Layer))
            {
                var instance = (ILayoutInstance*)ptr;
                if (instance->IsActive != active) instance->SetActive(active);
            }
        }
    }

    public void ClearSuppressedLayers()
    {
        suppressedLayerInstances.Clear();
        heldLayerStates.Clear();
    }


    public string DescribeActiveLayers() => LayoutQuery.DescribeActiveLayers();

    public string DescribeLiveEffects() => LayoutQuery.DescribeLiveEffects();
}
