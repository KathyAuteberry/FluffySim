using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakePlayerConditions : IPlayerConditions
{
    public bool StatusAffliction { get; private set; }
    public bool ControlAffliction { get; private set; }
    public bool ThrowOnNextSet { get; set; }
    public void EndClientLoad() { }

    public void SetAfflictions(bool status, bool control)
    {
        if (ThrowOnNextSet) { ThrowOnNextSet=false; throw new InvalidOperationException("Pause condition test"); }
        StatusAffliction = status;
        ControlAffliction = control;
    }
}
