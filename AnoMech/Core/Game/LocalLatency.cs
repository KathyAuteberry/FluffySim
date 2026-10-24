using System;
using System.Collections.Generic;

namespace AnoMech.Core.Game;

// An estimated outgoing transport delay for the local participant.
public sealed class LocalLatency
{
    private readonly List<(double Time, Placement Pose)> history = [];
    private double elapsed;
    public int PingRttMs { get; private set; }
    public float OneWaySeconds => PingRttMs / 2000f;

    public void Begin(int pingRttMs, Placement pose)
    {
        Clear();
        PingRttMs = Math.Clamp(pingRttMs, 0, 300);
        ResetPose(pose);
    }

    public void Sample(float realDeltaSeconds, Placement pose)
    {
        if (PingRttMs == 0) return;
        elapsed += Math.Max(0, realDeltaSeconds);
        history.Add((elapsed, pose));
        var cutoff = elapsed - OneWaySeconds;
        // Retain the sample preceding the receipt horizon. Never invent a path through a teleport.
        while (history.Count > 1 && history[1].Time <= cutoff) history.RemoveAt(0);
    }

    public Placement ServerPose(Placement immediate)
        => PingRttMs == 0 || history.Count == 0 ? immediate : history[0].Pose;

    // A sim-authoritative teleport is already known to the server; it must not replay old input.
    public void ResetPose(Placement pose)
    {
        history.Clear();
        history.Add((elapsed, pose));
    }

    public void Accept(EventScheduler events, float encounterSpeed, Action effect)
    {
        if (PingRttMs == 0) effect();
        else events.Add(OneWaySeconds * Math.Max(0, encounterSpeed), effect);
    }

    public void Clear()
    {
        history.Clear();
        elapsed = 0;
        PingRttMs = 0;
    }
}
