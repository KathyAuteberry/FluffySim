using System;
using System.Collections.Generic;

namespace AnoMech.Core.Game;

internal sealed class PeriodicExposure<T>(float graceSeconds, float intervalSeconds) where T : notnull
{
    private readonly Dictionary<T, (float Next, int Ticks)> occupants = [];

    public int Advance(T occupant, bool inside, float elapsed)
    {
        if (!inside) { occupants.Remove(occupant); return 0; }
        if (!occupants.TryGetValue(occupant, out var state)) state = (elapsed + graceSeconds, 0);
        if (elapsed < state.Next) { occupants[occupant] = state; return 0; }
        var ticks = 1 + (int)MathF.Floor((elapsed - state.Next) / intervalSeconds);
        state = (state.Next + ticks * intervalSeconds, state.Ticks + ticks);
        occupants[occupant] = state;
        return state.Ticks;
    }

    public void Clear() => occupants.Clear();
}
