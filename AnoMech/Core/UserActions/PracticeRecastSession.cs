using System;
using System.Collections.Generic;

namespace AnoMech.Core.UserActions;

internal readonly record struct LocalRecast(bool Active, uint ActionId, float Elapsed, float Total)
{
    public LocalRecast After(double seconds)
    {
        if (!Active) return this;
        var elapsed = Math.Min(Total, Elapsed + (float)Math.Max(0, seconds));
        return this with { Active = elapsed < Total, Elapsed = elapsed };
    }
}

internal interface IPracticeRecastStore
{
    int Group(uint action);
    LocalRecast? Read(int group);
    void Write(int group, LocalRecast value);
}

internal sealed class PracticeRecastSession(IPracticeRecastStore store, Func<bool> isolated, Func<double> seconds)
{
    private readonly Dictionary<int, LocalRecast> original = new();
    private readonly HashSet<int> changed = new();
    private readonly Dictionary<int, LocalRecast> paused = new();
    private bool suspended;
    private double started;
    public bool Captured { get; private set; }
    public static readonly uint[] Actions = [3, 30, 43, 3638, 16152, 7548, 7559,
        36920, 3540, 22, 36923, 7388, 40, 36927, 16471, 3634, 36935, 16160, 16140, 7531, 7535];

    public void Capture()
    {
        if (Captured) throw new InvalidOperationException("Recast session already captured");
        if (!isolated()) return;
        original.Clear();
        changed.Clear();
        foreach (var action in Actions)
        {
            var group = store.Group(action);
            if (group < 0 || original.ContainsKey(group)) continue;
            if (store.Read(group) is { } value) original.Add(group, value);
        }
        started = seconds();
        Captured = true;
    }

    public void ForgetPause()
    {
        suspended = false;
        paused.Clear();
    }

    public void SetPaused(bool value)
    {
        if (!Captured || !isolated() || value == suspended) return;
        if (value)
        {
            paused.Clear();
            foreach (var group in changed)
                paused.Add(group, store.Read(group) ?? throw new InvalidOperationException("Owned recast unavailable at pause"));
            suspended = true;
        }
        else
        {
            foreach (var (group, record) in paused) store.Write(group, record);
            ForgetPause();
        }
    }

    public void Track(uint action)
    {
        if (!Captured || !isolated() || Array.IndexOf(Actions, action) < 0) return;
        var group = store.Group(action);
        if (original.ContainsKey(group)) changed.Add(group);
    }

    public bool Reset(uint action)
    {
        if (!Captured || !isolated() || Array.IndexOf(Actions, action) < 0) return false;
        var group = store.Group(action);
        if (!original.ContainsKey(group) || store.Read(group) is not { } value) return false;
        // Shared groups must not clear a cooldown belonging to an unrelated action.
        if (value.ActionId != 0 && Array.IndexOf(Actions, value.ActionId) < 0) return false;
        changed.Add(group);
        store.Write(group, value with { Active = false, Elapsed = 0 });
        return true;
    }

    public void Restore()
    {
        if (!Captured) return;
        if (!isolated()) throw new InvalidOperationException("Cannot restore recasts without the firewall");
        foreach (var group in changed) store.Write(group, original[group].After(seconds() - started));
        changed.Clear();
        original.Clear();
        ForgetPause();
        Captured = false;
    }
}
