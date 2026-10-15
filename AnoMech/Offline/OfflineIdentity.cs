using System;
using AnoMech.Core.Native.Implementations.Interop;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.Game.Network;

namespace AnoMech.Offline;

internal static unsafe class OfflineIdentity
{
    internal static bool HasHomeWorld(ushort world, Func<ushort, bool> resolvesPublicWorld)
        => world != 0 && resolvesPublicWorld(world);

    internal static void ConfigureSpawn(ref SpawnPlayerPacket packet, string name, ushort homeWorld)
    {
        packet.HomeWorldId = homeWorld;
        packet.CurrentWorldId = homeWorld;
        fixed (byte* field = packet.Common.Name)
            GameObjectHelper.WriteName(field, 32, name);
    }
}
