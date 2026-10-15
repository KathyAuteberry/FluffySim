using System;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.Native.Implementations.Interop;

internal static unsafe class OfflinePartyIdentity
{
    internal static bool Owns(uint entity, ulong content)
        => (entity == 0x10FFA001 || entity >= 0x10FFFE00 && entity < 0x10FFFE64)
           && (content == 0x00FFA0FF00000001UL || content == (0xFF00000000000000UL | entity));

    internal static void Invalidate(Span<NameCache.EntityCacheEntry> entities, Span<NameCache.ContentIdCacheEntry> contents, uint entity, ulong content)
    {
        if (!Owns(entity, content)) return;
        foreach (ref var entry in entities) if (entry.EntityId == entity) entry = default;
        foreach (ref var entry in contents) if (entry.ContentId == content) entry = default;
    }

    internal static void Store(uint entity, ulong content, string name, ushort world, byte sex)
    {
        if (!Owns(entity, content)) return;
        var cache = NameCache.Instance();
        if (cache == null) return;
        // Native setters retain existing entries, including names left by a previous virtual run.
        Invalidate(cache->EntityCache, cache->ContentIdCache, entity, content);
        var bytes = Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* ptr = bytes)
        {
            var info = new NameCache.CharacterInfo { Name = ptr, HomeWorldId = world, Sex = sex };
            cache->SaveCharacterInfoForEntityId(entity, &info);
            cache->SetNameForContentId(content, ptr);
        }
    }
}
