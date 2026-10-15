using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Core.Native.Implementations.Interop;

internal static class PartyModelVisibility
{
    // The native representation update suppresses non-local PCs without the online representation
    // context. Only our offline doppels may bypass that suppression; other hide reasons survive.
    internal const ulong RepresentationSuppressed = 1UL << 30;
    private const ulong ModelAndNameplate = 2 | 2048;

    internal static ulong Apply(ulong flags, bool hidden, bool offline, bool ownedDoppel)
    {
        if (offline && ownedDoppel) flags &= ~RepresentationSuppressed;
        return hidden ? flags | ModelAndNameplate : flags & ~ModelAndNameplate;
    }

    internal static unsafe void Reconcile(BattleChara* actor, int slot, bool hidden, bool offline)
    {
        if (actor == null) return;
        var obj = (GameObject*)actor;
        var owned = OwnsDoppel(slot, obj->EntityId, obj->ObjectKind == ObjectKind.Pc);
        obj->RenderFlags = (VisibilityFlags)Apply((ulong)obj->RenderFlags, hidden, offline, owned);
        if (offline && owned && actor->DrawObject != null)
        {
            // Match the native render-bit clear path without overriding the culling bit.
            ref var flags = ref actor->DrawObject->Flags;
            flags = (byte)(obj->RenderFlags == VisibilityFlags.None ? flags | 1 : flags & ~1);
        }
    }

    internal static bool OwnsDoppel(int slot, uint entityId, bool playerCharacter)
        => playerCharacter && slot >= 0 && slot < 100 && entityId == 0x10FFFE00u + (uint)slot;
}
