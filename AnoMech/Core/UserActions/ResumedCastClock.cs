using System;

namespace AnoMech.Core.UserActions;

internal sealed class ResumedCastClock
{
    public float? Remaining { get; private set; }
    public void Preserve(float total, float progress) => Remaining ??= MathF.Max(0, total - progress);
    public bool Tick(float delta)
    {
        if (Remaining is not { } remaining) return false;
        Remaining = MathF.Max(0, remaining - delta);
        return Remaining == 0;
    }
    public void Clear() => Remaining = null;
}
