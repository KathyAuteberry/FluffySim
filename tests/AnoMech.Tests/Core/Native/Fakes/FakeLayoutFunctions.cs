using System.Numerics;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeLayoutFunctions : ILayoutFunctions
{
    public readonly Dictionary<(uint Territory, ushort Layer), bool> HeldLayerStates = new();
    public void HoldLayerActive(uint territoryId, ushort layerKey, bool active)
        => HeldLayerStates[(territoryId, layerKey)] = active;
    public int SuppressLayer(ushort layerKey) => 0;
    public void ReassertSuppressedLayers() { }
    public void ClearSuppressedLayers() => HeldLayerStates.Clear();

    // Any non-zero count: the zone is fully streamed in from the first frame.
    public int DisableSpawnAreaColliders(Vector3 center, float radius) => 1;

    public string DescribeActiveLayers() => "fake";
    public string DescribeLiveEffects() => "fake";
}
