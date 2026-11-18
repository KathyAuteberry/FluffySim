using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.TopNative.Transition.TopTransitionScenario;

namespace AnoMech.Scenarios.TopNative.Transition;

public sealed class TopTransitionAi : IScenarioAi<TopTransitionState>
{
    public string Name => "Elemental";
    public string Group => "Elemental";

    public static readonly PartyRole[] WestToEast =
    [
        PartyRole.RegenHealer, PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA,
        PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.ShieldHealer,
    ];

    private static readonly float[] PairBearings = [340f, 20f];
    private static readonly float[] SniperBearings = [270f, 215f, 145f, 90f];
    private const float Edge = 19f;
    private const float Inside = 16.3f;

    private TopTransitionState state = null!;
    private readonly Dictionary<PartyRole, float> home = [];

    public void Run(TopTransitionState stateParam, SimWorld world)
    {
        state = stateParam;
        AssignHomes();
        var ai = new AiManager(world);
        ai.Move(0.5f, LineUpHorizontally, jitter: 0.2f);
        ai.Move(DebuffsAt + 0.3f, () => Each(r => Polar(home[r], Edge)), jitter: 0f);
        ai.Move(WaveRepeaterCasts[0] + RingOffsets[2] - 1.5f, () => Each(r => Polar(home[r], Inside)), jitter: 0f);
        ai.Move(WaveRepeaterCasts[0] + RingOffsets[2] + 0.25f, () => Each(r => Polar(home[r], Edge)), jitter: 0f);
        ai.Move(HelloWorldCast - 13.5f, () => Each(r => Polar(BesideSecondArms(home[r]), Edge)), jitter: 0f);
        ai.Move(WaveRepeaterCasts[1] + RingOffsets[1] + 0.15f, () => Each(r => Polar(InsideFirstArms(home[r]), Inside)), jitter: 0f);
    }

    private void AssignHomes()
    {
        home.Clear();
        var highPowered = WestToEast.Where(r => state.Debuffs[r] == SniperDebuff.HighPowered).ToArray();
        var partners = WestToEast.Where(r => state.Debuffs[r] == SniperDebuff.None).ToArray();
        var snipers = WestToEast.Where(r => state.Debuffs[r] == SniperDebuff.Sniper).ToArray();
        for (var i = 0; i < 2; i++)
        {
            home[highPowered[i]] = PairBearings[i];
            home[partners[i]] = PairBearings[i];
        }
        for (var i = 0; i < 4; i++) home[snipers[i]] = SniperBearings[i];
    }

    private float BesideSecondArms(float bearing) => Beside(bearing, state.SecondArmsBearing, 22f);

    private float InsideFirstArms(float bearing) => Beside(bearing, state.FirstArmsBearing, 15f);

    private static float Beside(float bearing, Func<int, float> arms, float offset)
    {
        var nearest = Enumerable.Range(0, 3).Select(arms).OrderBy(a => MathF.Abs(Delta(a, bearing))).First();
        return nearest + MathF.Sign(Delta(nearest, bearing)) * offset;
    }

    private static float Delta(float from, float to) => ((to - from) % 360f + 540f) % 360f - 180f;

    private static IAiMove LineUpHorizontally()
        => Each(r => new Vector2(-10.5f + 3f * Array.IndexOf(WestToEast, r), 6f));

    private static IAiMove Each(Func<PartyRole, Vector2> spot)
    {
        var coords = new Vector2?[8];
        foreach (var role in WestToEast) coords[(int)role] = spot(role);
        return AiMove.Create(coords).NaturalOrder();
    }

    private static Vector2 Polar(float bearing, float radius)
    {
        var p = TopNativeConstants.Polar(bearing, radius);
        return new Vector2(p.X, p.Z);
    }
}
