using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Tea.TeaConstants;

namespace AnoMech.Scenarios.Tea.P4PerfectAlexander;

public abstract class TeaP4PerfectAlexanderAiBase : IScenarioAi<TeaP4PerfectAlexanderState>
{
    public virtual string Name => "NAUR";
    public virtual string? Group => null;

    private const float Jitter = 0.25f;
    private const float ShuffleSpeed = 3.25f;
    private const float ShuffleLength = 1.3f;
    private const float StillnessWatched = 2.4f;
    private const float ArenaEdge = 19.2f;

    private static readonly Vector2 HolderSpot = new(0.4f, -8.4f);

    private static readonly Vector2 LightBeaconSpot = new(0.1f, -14.4f);
    private static readonly Vector2 DarkBeaconSpot = new(0f, 14f);
    private static readonly Vector2[] LightFollowerSpots = [new(-0.5f, 10.8f), new(0.5f, 10.8f), new(0f, 10.2f)];
    private static readonly Vector2[] DarkFollowerSpots = [new(-0.3f, 12f), new(0.3f, 12f), new(0f, 12.6f)];

    private static readonly Vector2 WestStack = new(-7.5f, -1.5f);
    private static readonly Vector2 EastStack = new(7.5f, -1.5f);

    private static readonly Vector2 DefamationSpot = new(-6.7f, -16.3f);
    private static readonly Vector2 SharedSpot = new(4.3f, 17.1f);
    private static readonly Vector2[] NothingSpots = [new(3.6f, 16.3f), new(5.1f, 16.5f), new(4.1f, 17.9f)];
    private static readonly Vector2[] AggravatedSpots = [new(9.2f, 15.9f), new(8.6f, 15.1f), new(9.8f, 15.2f)];
    private static readonly Vector2 ThirdSafeDefamationSpot = new(7.0f, -16.1f);
    private static readonly Vector2 ThirdSafeSharedSpot = new(-9.2f, 15.4f);
    private static readonly Vector2[] ThirdSafeNothingSpots = [new(-8.5f, 14.6f), new(-10.0f, 14.8f), new(-9.4f, 16.2f)];
    private static readonly Vector2[] ThirdSafeAggravatedSpots = [new(-4.3f, 17.9f), new(-3.7f, 17.0f), new(-4.9f, 17.1f)];

    private static readonly Vector2 LightBetaBeaconSpot = new(-7f, -16.4f);
    private static readonly Vector2 LightBetaBeaconWait = new(-6.3f, -9.6f);
    private static readonly Vector2 DarkBetaBeaconSpot = new(13.8f, -0.1f);
    private static readonly Vector2 NorthStack = new(-3.4f, -14.9f);
    private static readonly Vector2 DarkCentreStack = new(0.4f, 2.2f);

    private static readonly Vector2 Centre = new(0f, 0f);
    private static readonly Vector2 EastTile = new(8f, 0f);
    private static readonly Vector2 SouthTile = new(0f, 8f);
    private static readonly Vector2 SouthEastTile = new(8f, 8f);

    private static readonly Vector2[] JailSpots =
        Enumerable.Range(0, 7).Select(k => Flat(Geometry.OnCompass(45f + 45f * k, 12f))).ToArray();
    private static readonly Vector2 PrisonWait = new(0f, 2.5f);
    internal static readonly PartyRole[] Prisoners = [RegenHealer, ShieldHealer, MainTank, OffTank, PhysRangedDps, CasterDps, MeleeDpsA];

    private TeaP4PerfectAlexanderState state = null!;
    private SimWorld world = null!;
    private Rng noise = null!;
    private float practiceStart;
    private readonly Dictionary<PartyRole, (float Time, Vector2 Spot)> startSpots = new();

    public void Run(TeaP4PerfectAlexanderState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        noise = world.Stream("tea-p4-ai");
        practiceStart = TeaP4PerfectAlexanderPractice.Of(state.Practice).Start;
        startSpots.Clear();
        if (practiceStart > 0f) world.Events.Add(practiceStart, PlaceBotsForPractice);

        RegroupUnder(0.5f, MainTank);
        TakeTheFinalWord();
        TakeOpticalSights();
        TakeFateCalibrationAlpha();
        TakeCapitalPunishment(113.21f, MainTank, shared: true);
        TakeFateCalibrationBeta();
        TakeCapitalPunishment(213.15f, OffTank, shared: false);
        TakeAlmightyJudgment(232.49f, 0, MainTank);
        TakeCapitalPunishment(258.82f, MainTank, shared: false);
        TakeAlmightyJudgment(277.94f, 1, OffTank);
        TakeTemporalPrison(TeaP4PerfectAlexanderState.TemporalPrisonCast);
    }

    private static Vector2 UptimeSpot(PartyRole role, PartyRole holder) => role switch
    {
        _ when role == holder => HolderSpot,
        MainTank or OffTank => new(0.6f, 2f),
        RegenHealer => new(2.2f, 5.1f),
        ShieldHealer => new(0.6f, 7.3f),
        MeleeDpsA => new(-2f, 7.3f),
        MeleeDpsB => new(-6.6f, 4.9f),
        PhysRangedDps => new(-4.1f, 3.2f),
        _ => new(-0.9f, 8.7f),
    };

    private void RegroupUnder(float time, PartyRole holder)
    {
        foreach (var role in PerRole.All) Go(time, role, UptimeSpot(role, holder));
    }

    private void TakeTheFinalWord()
    {
        At(16.95f, () => WalkToFinalWordSpots(firstStillness: !state.FinalWordMotionFirst));
        if (state.FinalWordMotionFirst) ShuffleEveryoneThrough(23.21f, FinalWordSpot);
        else At(23.4f, () => { foreach (var role in PerRole.All) Walk(role, FinalWordSpot(role), AiManager.RunSpeed, jitter: false); });
        if (!state.FinalWordMotionFirst) ShuffleEveryoneThrough(34.32f, null);
        RegroupUnder(34.6f, MainTank);
    }

    private FinalWordDebuff? ShownFinalWord(PartyRole role)
    {
        if (Member(role) is not { } member) return null;
        if (member.ShowsStatus(StatusId.FinalWordContactRegulation)) return FinalWordDebuff.LightBeacon;
        if (member.ShowsStatus(StatusId.FinalWordContactProhibition)) return FinalWordDebuff.LightFollower;
        if (member.ShowsStatus(StatusId.FinalWordEscapeDetection)) return FinalWordDebuff.DarkBeacon;
        if (member.ShowsStatus(StatusId.FinalWordEscapeProhibition)) return FinalWordDebuff.DarkFollower;
        return null;
    }

    private Vector2 FinalWordSpot(PartyRole role)
    {
        var debuff = ShownFinalWord(role) ?? state.FinalWordOf(role);
        var same = PerRole.All.Where(r => (ShownFinalWord(r) ?? state.FinalWordOf(r)) == debuff).ToList();
        var index = Math.Max(0, same.IndexOf(role));
        return FinalWordPosition(debuff, index);
    }

    protected virtual Vector2 FinalWordPosition(FinalWordDebuff debuff, int index) => debuff switch
        {
            FinalWordDebuff.LightBeacon => LightBeaconSpot,
            FinalWordDebuff.DarkBeacon => DarkBeaconSpot,
            FinalWordDebuff.LightFollower => LightFollowerSpots[index % LightFollowerSpots.Length],
            _ => DarkFollowerSpots[index % DarkFollowerSpots.Length],
        };

    private void WalkToFinalWordSpots(bool firstStillness)
    {
        var stillFrom = 23.21f - StillnessWatched - 0.2f;
        foreach (var role in PerRole.All)
        {
            if (Member(role) is not { } member) continue;
            var spot = FinalWordSpot(role);
            var leave = Reaction(0.25f, 0.6f);
            var arrive = world.Events.Elapsed + leave + Vector2.Distance(Flat(member.Position), spot) / AiManager.RunSpeed;
            var r = role;
            world.Events.Add(leave, () => Walk(r, spot, AiManager.RunSpeed, jitter: false));
            if (firstStillness && arrive > stillFrom)
                world.Events.Add(stillFrom - world.Events.Elapsed, () => Member(r)?.StopMoving());
        }
    }

    private void ShuffleEveryoneThrough(float check, Func<PartyRole, Vector2>? home)
    {
        At(check - 2.05f, () =>
        {
            foreach (var role in PerRole.All)
                if (Member(role) is { } member)
                    Shuffle(role, home?.Invoke(role) ?? Flat(member.Position), FinalWordShuffleDirection(role), check);
        });
    }

    protected virtual Vector2 FinalWordShuffleDirection(PartyRole role)
        => new((int)role % 2 == 0 ? 1f : -1f, 0f);

    private void Shuffle(PartyRole role, Vector2 home, Vector2 direction, float check)
    {
        var now = world.Events.Elapsed;
        var leg = 2f * ShuffleLength / ShuffleSpeed;
        for (var step = 0; step < 3; step++)
        {
            var destination = home + direction * (step % 2 == 0 ? ShuffleLength : -ShuffleLength);
            world.Events.Add(MathF.Max(0f, check - 1.95f + step * leg - now), () => Walk(role, destination, ShuffleSpeed, jitter: false));
        }
        world.Events.Add(MathF.Max(0f, check + 0.45f - now), () => Walk(role, home, ShuffleSpeed, jitter: false));
    }

    private void TakeOpticalSights()
    {
        At(41.6f, () => TakeOpticalSight(state.OpticalSpreadFirst));
        At(47.7f, () => TakeOpticalSight(!state.OpticalSpreadFirst));
        RegroupUnder(52.85f, MainTank);
    }

    private void TakeOpticalSight(bool spread)
    {
        foreach (var role in PerRole.All)
        {
            var r = role;
            world.Events.Add(Reaction(0.2f, 0.6f), () => Walk(r, spread ? SpreadSpot(r) : StackSpot(r), AiManager.RunSpeed, jitter: true));
        }
    }

    protected virtual Vector2 SpreadSpot(PartyRole role) => role switch
    {
        MainTank => new(0f, -12f),
        OffTank => new(9.4f, -7.5f),
        MeleeDpsB => new(11.7f, 2.7f),
        CasterDps => new(5.2f, 10.8f),
        PhysRangedDps => new(-5.2f, 10.8f),
        MeleeDpsA => new(-11.7f, 2.7f),
        ShieldHealer => new(-9.4f, -7.5f),
        _ => Centre,
    };

    protected virtual bool InFirstStack(PartyRole role) => role is MainTank or RegenHealer or MeleeDpsA or PhysRangedDps;

    protected virtual PartyRole Counterpart(PartyRole role) => role switch
    {
        MainTank => OffTank,
        OffTank => MainTank,
        RegenHealer => ShieldHealer,
        ShieldHealer => RegenHealer,
        MeleeDpsA => MeleeDpsB,
        MeleeDpsB => MeleeDpsA,
        PhysRangedDps => CasterDps,
        _ => PhysRangedDps,
    };

    private Vector2 StackSpot(PartyRole role)
    {
        var marked = state.OpticalStackTargets;
        var swapped = marked.Count == 2 && InFirstStack(marked[0]) == InFirstStack(marked[1]) ? marked.Max() : (PartyRole?)null;
        var first = InFirstStack(role);
        if (swapped is { } mover && (role == mover || role == Counterpart(mover))) first = !first;
        return StackPosition(first) + RingOffset(role, 0.7f);
    }

    protected virtual Vector2 StackPosition(bool first) => first ? WestStack : EastStack;

    private void TakeFateCalibrationAlpha()
    {
        At(85.2f, WalkToAlphaSpots);
        if (state.AlphaFirstMotion) At(96f, () => ShuffleAlpha(97.97f));
        if (state.AlphaSecondMotion) At(100f, () => ShuffleAlpha(102.00f));
        RegroupUnder(103.4f, MainTank);
    }

    private bool ThirdCloneSafe => state.AlphaSafeClone == 2;

    private Vector2 AlphaSpot(PartyRole role) => state.AlphaSentenceOf(role) switch
    {
        AlphaSentence.Defamation => ThirdCloneSafe ? ThirdSafeDefamationSpot : DefamationSpot,
        AlphaSentence.SharedSentence => ThirdCloneSafe ? ThirdSafeSharedSpot : SharedSpot,
        AlphaSentence.AggravatedAssault => (ThirdCloneSafe ? ThirdSafeAggravatedSpots : AggravatedSpots)[Rank(role, AlphaSentence.AggravatedAssault)],
        _ => (ThirdCloneSafe ? ThirdSafeNothingSpots : NothingSpots)[Rank(role, AlphaSentence.Nothing)],
    };

    private int Rank(PartyRole role, AlphaSentence sentence)
        => PerRole.All.Where(r => state.AlphaSentenceOf(r) == sentence).ToList().IndexOf(role) % 3;

    private void WalkToAlphaSpots()
    {
        foreach (var role in PerRole.All)
        {
            var r = role;
            world.Events.Add(Reaction(0.2f, 0.7f), () => Walk(r, AlphaSpot(r), AiManager.RunSpeed, jitter: true));
        }
    }

    private void ShuffleAlpha(float check)
    {
        var away = new Vector2(ThirdCloneSafe ? -1f : 1f, 0f);
        foreach (var role in PerRole.All) Shuffle(role, AlphaSpot(role), away, check);
    }

    private void TakeCapitalPunishment(float cast, PartyRole holder, bool shared)
    {
        var other = holder == MainTank ? OffTank : MainTank;
        if (shared)
        {
            At(cast + 5f, () => UseTankDefense(holder, MajorDefense));
            At(cast + 5f, () => UseTankDefense(other, MajorDefense));
        }
        else
        {
            At(cast + 5f, () => UseTankDefense(holder, job => PracticeDefensiveActions.TankAction(job)));
            At(cast + 10.5f, () =>
            {
                UseTankDefense(other, _ => 7531);
                UseTankDefense(other, ShortDefense);
            });
        }
        Go(cast + 0.3f, holder, shared ? new Vector2(0f, -9.3f) : holder == MainTank ? new Vector2(-0.1f, -9f) : new Vector2(0f, -9.2f));
        Go(cast + 0.3f, other, shared ? new Vector2(0f, -9.2f) : holder == MainTank ? new Vector2(10.4f, -4.8f) : new Vector2(11f, -2.3f));
        Go(cast + 8.4f, holder, shared ? new Vector2(-11.1f, -4.5f) : holder == MainTank ? new Vector2(-9.8f, -5.8f) : new Vector2(-6.7f, -4.9f));
        if (shared) Go(cast + 8.4f, other, new Vector2(5.4f, -8.2f));
        At(cast + (shared ? 9.38f : holder == MainTank ? 10.41f : 8.91f), () => Provoke(other));
        RegroupUnder(cast + 12.5f, other);
    }

    private void UseTankDefense(PartyRole role, Func<byte, uint> actionForJob)
    {
        if (Member(role) is { } tank && tank is ISimPartyMember member && world.Party.IsBotDriven(tank))
            member.UseAction(actionForJob(member.ClassJob));
    }

    private static uint MajorDefense(byte job) => job switch
    {
        19 => 17,
        21 => 44,
        32 => 3636,
        37 => 16148,
        _ => 0,
    };

    private static uint ShortDefense(byte job) => job switch
    {
        19 => 3542,
        21 => 40,
        32 => 3634,
        37 => 16161,
        _ => 0,
    };

    private void Provoke(PartyRole tank)
    {
        if (Member(tank) is ISimPartyMember member && state.Objects.PerfectAlexander is { } boss) member.UseAction(Enmity.ProvokeActionId, boss);
    }

    private void TakeFateCalibrationBeta()
    {
        At(156.3f, TakePreMarchSpots);
        At(178.8f, LightBetaBeaconTakesItsSpot);
        At(188.8f, () => WalkToBetaSpots(AfterTheMarch));
        At(192.2f, () => WalkToBetaSpots(state.BetaOpticalStack ? ForTheStacks : ForTheSpreads));
        At(198.05f, RunUnderTheDonut);
        RegroupUnder(203.1f, OffTank);
    }

    private void TakePreMarchSpots()
    {
        foreach (var role in PerRole.All)
        {
            var r = role;
            var spot = state.BetaRoleOf(r) == BetaRole.LightBeacon ? LightBetaBeaconWait : BeforeTheMarch(state.BetaRoleOf(r));
            world.Events.Add(Reaction(0.1f, 5.5f), () => Walk(r, spot, AiManager.RunSpeed, jitter: false));
        }
    }

    private void LightBetaBeaconTakesItsSpot()
    {
        var beacon = state.BetaHolder(BetaRole.LightBeacon);
        world.Events.Add(Reaction(0f, 0.8f), () => Walk(beacon, LightBetaBeaconSpot, AiManager.RunSpeed, jitter: false));
    }

    private void WalkToBetaSpots(Func<BetaRole, Vector2> spot)
    {
        foreach (var role in PerRole.All)
        {
            var r = role;
            world.Events.Add(Reaction(0.1f, 0.4f), () => Walk(r, spot(state.BetaRoleOf(r)), AiManager.RunSpeed, jitter: false));
        }
    }

    protected virtual Vector2 BeforeTheMarch(BetaRole role) => role switch
    {
        BetaRole.LightBeacon => LightBetaBeaconSpot,
        BetaRole.DarkBeacon => DarkBetaBeaconSpot,
        BetaRole.LightFar => new(12.5f, -1.3f),
        BetaRole.LightNear => new(12.4f, -1.8f),
        BetaRole.LightShared => new(12.2f, -1.3f),
        BetaRole.DarkNear => new(12.2f, -1.6f),
        BetaRole.DarkUntethered => new(11.5f, 0f),
        _ => new(12.7f, 1f),
    };

    protected static Vector2 AfterTheMarch(BetaRole role) => role switch
    {
        BetaRole.DarkUntethered => new(-15.9f, 0.3f),
        BetaRole.DarkFar => new(-0.3f, 17.2f),
        BetaRole.DarkBeacon => new(14.6f, -0.2f),
        BetaRole.LightBeacon => NorthStack,
        BetaRole.LightFar => new(-3.5f, -14.1f),
        BetaRole.LightNear => new(-4f, -15f),
        BetaRole.LightShared => new(-3f, -14.8f),
        _ => new(-3.7f, -15.3f),
    };

    protected virtual Vector2 ForTheStacks(BetaRole role) => role switch
    {
        BetaRole.DarkBeacon => DarkCentreStack + new Vector2(0.6f, -0.2f),
        BetaRole.DarkUntethered => DarkCentreStack + new Vector2(-1f, 0.2f),
        BetaRole.DarkFar => DarkCentreStack + new Vector2(-0.2f, 1.1f),
        BetaRole.DarkNear => DarkCentreStack + new Vector2(0.2f, -0.8f),
        _ => AfterTheMarch(role),
    };

    private static Vector2 ForTheSpreads(BetaRole role) => role switch
    {
        BetaRole.LightFar => new(2f, -2f),
        BetaRole.LightShared => new(12f, -13f),
        BetaRole.DarkFar => new(2f, 13f),
        BetaRole.LightNear => new(2f, -12f),
        BetaRole.DarkNear => new(-8f, -12f),
        BetaRole.LightBeacon => new(16f, -4f),
        BetaRole.DarkBeacon => new(16f, 5f),
        _ => new(-13f, 6f),
    };

    private static readonly Vector2 EastBait = new(14.6f, -0.2f);
    private static readonly Vector2 SouthBait = new(-0.3f, 17.2f);
    private static readonly Vector2 WestBait = new(-15.9f, 0.3f);

    private void RunUnderTheDonut()
    {
        var clone = state.Objects.Clones[state.BetaDonutClone];
        var landing = clone is { IsActive: true } ? Flat(clone.Position)
            : state.BetaDonutClone switch { 0 => EastBait, 3 => WestBait, _ => SouthBait };
        var outward = landing.LengthSquared() > 0.01f ? Vector2.Normalize(landing) : -Vector2.UnitY;
        foreach (var role in PerRole.All)
        {
            if (Member(role) is not ISimPartyMember member) continue;
            var spot = landing + outward * 1.8f + RingOffset(role, 1.2f);
            var r = role;
            world.Events.Add(Reaction(0f, 0.2f), () =>
            {
                if (Member(r) is not { } runner) return;
                member.UseSprint(Vector2.Distance(Flat(runner.Position), spot) / AiManager.SprintSpeed + 1f);
                Walk(r, spot, AiManager.SprintSpeed, jitter: false);
            });
        }
    }

    private void TakeAlmightyJudgment(float cast, int judgment, PartyRole holder)
    {
        foreach (var role in PerRole.All.Where(r => r != holder)) Go(cast + 0.3f, role, SouthTile + RingOffset(role, 0.8f));
        At(cast + 8.3f, () =>
        {
            var start = StartingPointBesideTheFirstSet(state.JudgmentOrders[judgment][2], state.JudgmentOrders[judgment][0]);
            foreach (var role in PerRole.All)
            {
                var r = role;
                world.Events.Add(Reaction(0.1f, 0.5f), () => Walk(r, start + RingOffset(r, 0.8f), AiManager.RunSpeed, jitter: false));
            }
        });
        At(cast + 14.14f, () =>
        {
            var first = FirstSetPointNextTo(state.JudgmentOrders[judgment][0], StartingPointBesideTheFirstSet(state.JudgmentOrders[judgment][2], state.JudgmentOrders[judgment][0]));
            foreach (var role in PerRole.All)
            {
                var r = role;
                world.Events.Add(Reaction(0.2f, 0.35f), () => Walk(r, first + RingOffset(r, 0.8f), AiManager.RunSpeed, jitter: false));
            }
        });
        RegroupUnder(cast + 19.5f, holder);
    }

    private static Vector2 StartingPointBesideTheFirstSet(int lastSet, int firstSet) => lastSet switch
    {
        2 => SouthTile,
        1 => SouthEastTile,
        _ => firstSet == 2 ? Centre : EastTile,
    };

    private static Vector2 FirstSetPointNextTo(int firstSet, Vector2 from) => firstSet switch
    {
        2 => SouthTile,
        1 => SouthEastTile,
        _ => from == SouthEastTile ? EastTile : Centre,
    };

    private void TakeTemporalPrison(float prison)
    {
        foreach (var role in PerRole.All.Where(r => r != OffTank)) Go(prison - 7f, role, PrisonWait + RingOffset(role, 1.5f));
        for (var k = 0; k < Prisoners.Length; k++)
            Go(prison + TeaP4PerfectAlexanderState.FirstJail + TeaP4PerfectAlexanderState.JailEvery * k - 4f, Prisoners[k], JailSpots[k]);
    }

    private void At(float time, Action action, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (TeaP4PerfectAlexanderPractice.Covers(state.Practice, time)) world.Events.Add(time, action, file, line);
    }

    private void Go(float time, PartyRole role, Vector2 spot, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (TeaP4PerfectAlexanderPractice.Covers(state.Practice, time))
            world.Events.Add(time + Reaction(0.15f, 0.55f), () => Walk(role, spot, AiManager.RunSpeed, jitter: true), file, line);
        else if (time < practiceStart && (!startSpots.TryGetValue(role, out var known) || time >= known.Time))
            startSpots[role] = (time, spot);
    }

    private void PlaceBotsForPractice()
    {
        foreach (var (role, (_, spot)) in startSpots)
            if (Member(role) is { } bot && world.Party.IsBotDriven(bot))
                bot.SetPosition(new Placement(new Vector3(spot.X, 0f, spot.Y), bot.Rotation));
    }

    private void Walk(PartyRole role, Vector2 spot, float speed, bool jitter)
    {
        if (Member(role) is not { } member) return;
        var at = jitter ? spot + RandomOffset(Jitter) : spot;
        if (at.Length() > ArenaEdge) at = Vector2.Normalize(at) * ArenaEdge;
        member.MoveTo(new Vector3(at.X, 0f, at.Y), speed);
    }

    private float Reaction(float from, float to) => from + (to - from) * noise.NextSingle();

    private Vector2 RandomOffset(float radius)
    {
        var angle = MathF.Tau * noise.NextSingle();
        var length = radius * MathF.Sqrt(noise.NextSingle());
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * length;
    }

    private static Vector2 RingOffset(PartyRole role, float radius) => Flat(Geometry.OnCompass(45f * (int)role, radius));

    private SimCharacter? Member(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private static Vector2 Flat(Vector3 v) => new(v.X, v.Z);
}
