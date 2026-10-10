using System;

namespace AnoMech.Core.Native.Implementations;

internal readonly record struct BgmSceneState(ushort Track, uint Territory);

internal sealed class BgmOverride(Func<BgmSceneState?> read, Action<ushort> play, Action release)
{
    private BgmSceneState? owned;
    private bool yielded;

    public bool Play(ushort track)
    {
        if (track == 0 || yielded || read() is not { } state) return false;
        if (owned is { } previous)
        {
            if (state != previous)
            {
                Poll();
                return false;
            }
            if (previous.Track == track) return true;
        }
        else if (state.Track != 0) return false;
        play(track);
        owned = new(track, state.Territory);
        return true;
    }

    public bool Poll()
    {
        if (owned is not { } previous) return false;
        var state = read();
        if (state is { } current && current == previous) return true;
        // A territory change ends the lease, but never restores a stale song.
        if (state is { } changed && changed.Track == previous.Track) release();
        owned = null;
        yielded = true;
        return false;
    }

    public void Reset()
    {
        if (owned is { } previous && read() is { } current && current.Track == previous.Track) release();
        owned = null;
        yielded = false;
    }
}
