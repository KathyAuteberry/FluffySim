using AnoMech.Core.Native.Implementations.Pointers;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Graphics.Vfx;

namespace AnoMech.Core.Native.Implementations;

internal sealed unsafe class ActorVfxProxy(VfxData* vfx) : IActorVfxProxy
{
    public void Remove()
    {
        var owned = vfx;
        vfx = null;
        if (owned == null) return;
        DiagnosticLog.Debug("[ActorVfxProxy] remove begin");
        VfxDataPointers.Dtor(owned, 0);
        DiagnosticLog.Debug("[ActorVfxProxy] remove complete");
    }
}
