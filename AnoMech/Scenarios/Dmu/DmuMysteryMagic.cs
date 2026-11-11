using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu;

// Blizzard III Blowout: two opposite quadrant cones from the centre. A lie shows the cones on one
// diagonal and fires them, unshown, on the other.
public sealed record IceRoll(bool Lie, bool ShownSouthwestNortheast)
{
    public bool RealSouthwestNortheast => ShownSouthwestNortheast != Lie;
    public static float[] Headings(bool southwestNortheast)
        => southwestNortheast ? [-MathF.PI / 4f, 3f * MathF.PI / 4f] : [-3f * MathF.PI / 4f, MathF.PI / 4f];
}

// Thrumming Thunder III: two 10y-wide lines out of four parallel ones. A lie shows one pair and
// fires the other pair of the same orientation.
public sealed record ThunderRoll(bool Lie, bool RunsSoutheast, bool ShownFirstPair)
{
    public bool RealFirstPair => ShownFirstPair != Lie;

    // Line origins, local; heading +45° (south-east) or -45° (south-west).
    public static Vector3[] Origins(bool runsSoutheast, bool firstPair) => (runsSoutheast, firstPair) switch
    {
        (true, true) => [new(-3.55f, 0f, -24.76f), new(-17.71f, 0f, -10.63f)],
        (true, false) => [new(-10.63f, 0f, -17.71f), new(-24.76f, 0f, -3.55f)],
        (false, true) => [new(3.53f, 0f, -24.76f), new(17.66f, 0f, -10.63f)],
        (false, false) => [new(10.58f, 0f, -17.71f), new(24.74f, 0f, -3.55f)],
    };

    public static float Heading(bool runsSoutheast) => runsSoutheast ? MathF.PI / 4f : -MathF.PI / 4f;
}

// Flagrant Fire III. The players are marked for spreads (everyone) or stacks (one support, one DPS);
// a lie resolves the other. A lie's stacks land on a support and a DPS of the fight's own choosing.
public sealed record FireRoll(bool Lie, bool StackMarkers, PartyRole StackSupport, PartyRole StackDps)
{
    public bool Stacks => StackMarkers != Lie;
}

public static class DmuMysteryMagic
{
    public const float LineHalfWidth = 5f;
    public const float LineLength = 40f;

    public static void ShowIcons(SimEnemy? kefka, params uint[] lockons)
    {
        foreach (var lockon in lockons) kefka?.ActorControl.HeadMarker(lockon);
    }

    public static uint IceIcon(IceRoll roll) => roll.Lie ? LockonId.IceLie : LockonId.IceTruth;
    public static uint ThunderIcon(ThunderRoll roll) => roll.Lie ? LockonId.ThunderLie : LockonId.ThunderTruth;
    public static uint FireIcon(FireRoll roll) => roll.Lie ? LockonId.FireLie : LockonId.FireTruth;

    public static void MarkPlayers(SimParty party, FireRoll roll)
    {
        if (roll.StackMarkers)
        {
            foreach (var role in new[] { roll.StackSupport, roll.StackDps })
                party.Get(role)?.ActorControl.HeadMarker(LockonId.Stack);
            return;
        }
        foreach (var member in party.ActiveMembers().Where(m => m.IsAlive()))
            member.ActorControl.HeadMarker(LockonId.Spread);
    }

    public static void CastIce(DmuArena arena, IceRoll roll)
    {
        foreach (var heading in IceRoll.Headings(roll.ShownSouthwestNortheast))
            arena.Helper(Vector3.Zero, heading)?.Cast(roll.Lie ? DmuActions.BlizzardFake : DmuActions.BlizzardShown);
        if (!roll.Lie) return;
        foreach (var heading in IceRoll.Headings(roll.RealSouthwestNortheast))
            arena.Helper(Vector3.Zero, heading)?.Cast(DmuActions.BlizzardHidden);
    }

    public static void CastThunder(DmuArena arena, ThunderRoll roll)
    {
        var heading = ThunderRoll.Heading(roll.RunsSoutheast);
        foreach (var origin in ThunderRoll.Origins(roll.RunsSoutheast, roll.ShownFirstPair))
            arena.Helper(origin, heading)?.Cast(roll.Lie ? DmuActions.ThunderFake : DmuActions.ThunderShown);
        if (!roll.Lie) return;
        foreach (var origin in ThunderRoll.Origins(roll.RunsSoutheast, roll.RealFirstPair))
            arena.Helper(origin, heading)?.Cast(DmuActions.ThunderHidden);
    }

    // The fire comes from two helpers over Kefka's head.
    public static void ResolveFire(DmuArena arena, SimParty party, FireRoll roll)
    {
        var from = new Vector3(0f, 0f, -10f);
        if (roll.Stacks)
        {
            foreach (var role in new[] { roll.StackSupport, roll.StackDps })
                if (party.Get(role) is { } target && target.IsAlive())
                    arena.Helper(from, 0f)?.Cast(DmuActions.FlagrantFireStack, target);
            return;
        }
        foreach (var target in party.ActiveMembers().Where(m => m.IsAlive()).ToList())
            arena.Helper(from, 0f)?.Cast(DmuActions.FlagrantFireSpread, target);
    }

    // Local XZ point safe from the real cones.
    public static bool SafeFromIce(Vector2 p, IceRoll roll)
    {
        if (p == Vector2.Zero) return false;
        var southwestOrNortheast = MathF.Sign(p.X) != MathF.Sign(p.Y);
        return southwestOrNortheast != roll.RealSouthwestNortheast;
    }

    public static bool SafeFromThunder(Vector2 p, ThunderRoll roll, float margin = 0f)
    {
        var heading = ThunderRoll.Heading(roll.RunsSoutheast);
        var dir = new Vector2(MathF.Sin(heading), MathF.Cos(heading));
        var across = new Vector2(dir.Y, -dir.X);
        foreach (var origin in ThunderRoll.Origins(roll.RunsSoutheast, roll.RealFirstPair))
        {
            var o = new Vector2(origin.X, origin.Z);
            var along = Vector2.Dot(p - o, dir);
            var side = MathF.Abs(Vector2.Dot(p - o, across));
            if (along >= -margin && along <= LineLength + margin && side <= LineHalfWidth + margin) return false;
        }
        return true;
    }

    // The safe point nearest `from` among candidates no further than `reach` from the centre.
    public static Vector2 NearestSafe(Vector2 from, Func<Vector2, bool> safe, float reach = 17f, float step = 0.5f)
    {
        var best = from;
        var bestDistance = float.MaxValue;
        for (var x = -reach; x <= reach; x += step)
        for (var y = -reach; y <= reach; y += step)
        {
            var p = new Vector2(x, y);
            if (p.Length() > reach || !safe(p)) continue;
            var d = Vector2.DistanceSquared(p, from);
            if (d < bestDistance) { bestDistance = d; best = p; }
        }
        return best;
    }
}
