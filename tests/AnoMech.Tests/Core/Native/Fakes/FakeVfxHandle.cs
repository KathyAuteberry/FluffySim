using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeVfxHandle : IActorVfxProxy, IStaticVfxProxy
{
    public int RemoveCalls { get; private set; }
    public void Remove() => ++RemoveCalls;
}
