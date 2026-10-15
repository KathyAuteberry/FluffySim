using System;

namespace AnoMech.Core.Native.Policies;

internal sealed class OfflineWeatherTransition
{
    internal bool Complete => complete;
    public uint Epoch { get; private set; }
    private byte? requested;
    private nint generation;
    private bool complete;
    private bool applied;

    public void Request(byte weather)
    {
        Clear();
        requested = weather;
    }

    public void Clear()
    {
        ++Epoch;
        requested = null;
        generation = 0;
        complete = false;
        applied = false;
    }

    public void Tick(nint readyLayout, Func<byte> current, Action<byte> queue, Action update)
    {
        if (requested is not { } wanted) return;
        if (readyLayout == 0)
        {
            generation = 0;
            complete = false;
            applied = false;
            return;
        }
        if (generation != readyLayout)
        {
            generation = readyLayout;
            complete = false;
            applied = false;
            queue(wanted);
        }
        if (complete) return;
        if (!applied)
        {
            update();
            applied = true;
        }
        complete = current() == wanted;
    }
}
