using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game.Ai;

public enum BotDifficulty { Easy, Hard }

public sealed class BotMovement(SimWorld world)
{
    private readonly Dictionary<SimCharacter, int> versions = [];
    public int DelayedMoves { get; private set; }
    public IReadOnlyList<BotDeparture> Departures => departures;
    private readonly List<BotDeparture> departures = [];
    public static float DepartureDelay(float distance, float speed, float available)
        => MathF.Max(0, available - distance / speed - .45f);

    public void Move(SimCharacter? member, Vector3 destination, float? arriveBy = null, float speed = 6, float? facing = null, float? leaveBy = null)
    {
        if (member == null) return;
        var version = versions.GetValueOrDefault(member) + 1;
        versions[member] = version;
        var delay = world.BotDifficulty == BotDifficulty.Hard && arriveBy is { } deadline && world.Party.IsBotDriven(member) && world.Obstacles.All.Count == 0
            ? DepartureDelay(Vector3.Distance(member.Position, destination), speed, deadline - world.Events.Elapsed) : 0;
        if (member.MoveDestination is not null || member.ActionsLocked) delay = 0;
        if (leaveBy is { } latest) delay = MathF.Min(delay, MathF.Max(0, latest - world.Events.Elapsed));
        if (Vector3.Distance(member.Position, destination) < .01f) delay = 0;
        departures.Add(new(world.Events.Elapsed, world.Events.Elapsed + delay, Vector3.Distance(member.Position, destination), speed, arriveBy, member, member.Position, destination));
        if (delay <= 0) { member.MoveTo(destination, speed, facing); return; }
        ++DelayedMoves;
        world.Events.Add(delay, () =>
        {
            if (member.IsActive && member.IsAlive() && versions.GetValueOrDefault(member) == version)
                member.MoveTo(destination, speed, facing);
        });
    }
    public void Clear() { versions.Clear(); departures.Clear(); DelayedMoves = 0; }
}

public readonly record struct BotDeparture(float RequestedAt, float DepartAt, float Distance, float Speed, float? Deadline, SimCharacter Member, Vector3 From, Vector3 Destination);
