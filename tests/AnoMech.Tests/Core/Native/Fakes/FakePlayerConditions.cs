using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakePlayerConditions(SimTimeline timeline) : IPlayerConditions
{
    public bool StatusAffliction { get; private set; }
    public bool ControlAffliction { get; private set; }
    public bool ThrowOnNextSet { get; set; }
    public void EndClientLoad() { }

    public void SetAfflictions(bool status, bool control)
    {
        if (ThrowOnNextSet) { ThrowOnNextSet=false; throw new InvalidOperationException("Pause condition test"); }
        if (status != StatusAffliction || control != ControlAffliction)
            timeline.Add("afflictions", detail: $"status={(status ? 1 : 0)} control={(control ? 1 : 0)}");
        StatusAffliction = status;
        ControlAffliction = control;
    }
}
