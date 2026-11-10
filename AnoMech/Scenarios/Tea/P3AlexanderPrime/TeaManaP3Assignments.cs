using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Scenarios.Tea.P3AlexanderPrime;

public static class TeaManaP3Assignments
{
    public static Vector2 InitialNumberSpot(int number) => number switch
    {
        1 => new(-13f, -13f),
        2 => new(13f, -13f),
        3 => new(-12.4f, 12.3f),
        4 => new(12.4f, 12.3f),
        5 => new(-15.8f, -8.6f),
        6 => new(14.8f, -7.2f),
        7 => new(-17.3f, -6.0f),
        8 => new(16.6f, -5.6f),
        _ => throw new ArgumentOutOfRangeException(nameof(number)),
    };

    public static Vector2 FlameSpot(PartyRole role) => role switch
    {
        MainTank => new(-14f, 0f),
        OffTank => new(-15.5f, -7.8f),
        _ => new Vector2(-9f, 15f) + (role switch
        {
            RegenHealer => new Vector2(0f, 0f),
            ShieldHealer => new Vector2(-.6f, .3f),
            MeleeDpsA => new Vector2(.5f, .2f),
            MeleeDpsB => new Vector2(-.3f, -.4f),
            PhysRangedDps => new Vector2(.1f, .6f),
            _ => new Vector2(-.6f, -.3f),
        }),
    };

    public static Vector2 SwordSpot(PartyRole role, TeaP3AlexanderPrimeState state)
    {
        if (role == MainTank) return new(18.5f, 0f);
        if (role == RegenHealer) return new(-1.2f, 0f);
        if (role == ShieldHealer) return new(0f, 2.3f);
        if (role == state.RestrainingOrderDps) return new(0f, -3f);
        var stack = new[] { OffTank, state.SharedSentence }.Concat(state.AggravatedAssault).ToList();
        return new Vector2[] { new(7.5f, -.6f), new(8.4f, 0f), new(7.5f, .6f), new(8.6f, -1.1f) }[stack.IndexOf(role)];
    }

    public static Vector2 EnumerationSpot(PartyRole role, IReadOnlyList<PartyRole> marked)
    {
        var ordered = marked.OrderBy(r => r).ToArray();
        if (role == ordered[0]) return new(0f, -5.2f);
        if (role == ordered[1]) return new(0f, 5.2f);
        if (role == MainTank) return new(-.8f, -6f);
        if (role == OffTank) return new(.8f, -6f);
        if (role == RegenHealer) return new(-.8f, 6f);
        if (role == ShieldHealer) return new(.8f, 6f);
        var free = TeaP3AlexanderPrimeState.Dps.Except(marked).OrderBy(r => r).ToArray();
        return role == free[0] ? new(-6f, 0f) : new(6f, 0f);
    }
}
