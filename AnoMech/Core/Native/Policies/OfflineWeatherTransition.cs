using System;

namespace AnoMech.Core.Native.Policies;

internal sealed class OfflineWeatherTransition
{
    public uint Epoch { get; private set; }
    private byte? requested;
    private nint generation;
    private bool complete;

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
    }

    public void Tick(nint readyLayout, Func<byte> current, Action<byte> queue, Action update)
    {
        if (requested is not { } wanted) return;
        if (readyLayout == 0)
        {
            generation = 0;
            complete = false;
            return;
        }
        if (generation != readyLayout)
        {
            generation = readyLayout;
            complete = false;
            queue(wanted);
        }
        if (complete) return;
        update();
        complete = current() == wanted;
    }
}
