using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.Upstream.P2DiamondDust.FruP2DiamondDustState;

namespace AnoMech.Scenarios.Fru.Upstream.P2DiamondDust;

public sealed class FruP2DiamondDustAi(bool mana = false, bool continuationOnly = false, float offset = 0f) : IScenarioAi<FruP2DiamondDustState>
{
    public string Name => mana ? "Mana Nukemaru" : "NA · Partner Swap";
    public string Group => mana ? "Mana" : "NA";

    private static readonly int[] Clocks = [0, 2, 6, 4, 5, 3, 7, 1];
    private const float SlideDistance = 32f;
    private const float SlideSeconds = 1f;
    private const float PuddleRadius = 6f;
    private const float NeedleCircleRadius = 5f;
    private const float NeedleLineHalfWidth = 2.5f;
    private const float EdgeRadius = 19f;
    private const float EdgeSearchOctants = 25f / 45f;

    private FruP2DiamondDustState state = null!;
    private SimWorld world = null!;
    private readonly List<(int Wave, Vector3 Position)> puddles = [];

    public void Run(FruP2DiamondDustState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        puddles.Clear();
        var ai = new AiManager(world);
        void Move(float at, Func<PartyRole, Vector3?> destination, float arrival)
            => ai.Move(at + offset, () => ByRole(destination), jitter: 0f, arrivalTime: arrival + offset);
        void At(float at, Action action) => world.Events.Add(at + offset, action);
        if (!continuationOnly)
        {
            Move(15.3f, KickSpot, 20.6f);
            Move(20.7f, StoneSpot, 23.3f);
            Move(23.35f, KnockbackSpot, 27.4f);
        }
        Move(29.05f, NeedleSafeEdgeSpot, 30.05f);
        Move(30.1f, role => HolySpot(role, 0), 31.4f);
        for (var wave = 0; wave < HolyWaves; wave++)
        {
            var w = wave;
            At(HolyAt(w) - FruHolyPuddles.SnapshotBeforeHit, () => RememberHolyPuddles(w));
            Move(HolyAt(w), role => HolySpot(role, w + 1), HolyAt(w + 1));
        }
        Move(HolyAt(HolyWaves), IceSpot, 39.9f);
        At(39.9f, FaceAwayFromGaze);
        At(IceStart + 0.2f, () => SlideBots(behind: false, recovery: true));
        At(45.05f, () => SlideBots(behind: state.Stillness));
        At(47.4f, () => SlideBots(behind: !state.Stillness));
        At(IceEnd, puddles.Clear);
        Move(IceEnd + 0.3f, _ => new Vector3(0, 0, 4), IceEnd + 2f);
    }

    private bool Cursed => (state.MirrorOctant - state.FirstIcicle + 8) % 4 == 0;

    private static bool GroupOne(PartyRole role) => ((int)role & 1) == 0;

    private int Clock(PartyRole role)
    {
        var original = Clocks[(int)role];
        var requiredParity = (state.FirstIcicle + (state.Marked(role) ? 1 : 0)) & 1;
        return (original + ((original & 1) == requiredParity ? 0 : (int)role < 4 ? 7 : 1)) % 8;
    }

    private Vector3? KickSpot(PartyRole role) => Point(Clock(role), (state.Axe ? 16 : 0) + (state.Marked(role) ? 3 : 1));

    private Vector3? StoneSpot(PartyRole role)
        => Point(Clock(role), state.Marked(role) ? state.Axe ? 19 : 8 : state.Axe ? 4 : 1);

    private int KnockbackOctant(PartyRole role)
    {
        var groupOne = mana ? (state.FirstIcicle >= 2 ? state.FirstIcicle + 4 : state.FirstIcicle) : state.FirstIcicle == 0 ? 0 : state.FirstIcicle + 4;
        return (groupOne + (GroupOne(role) ? 0 : 4)) % 8;
    }

    private Vector3? KnockbackSpot(PartyRole role) => Point(NeedleSafeEdgeOctant(role), 3);

    private Vector3? NeedleSafeEdgeSpot(PartyRole role) => Point(NeedleSafeEdgeOctant(role), EdgeRadius);

    private float NeedleSafeEdgeOctant(PartyRole role)
    {
        var holyOctant = KnockbackOctant(role) + (Cursed ? 1f / 3 : 0);
        var stars = Enum.GetValues<PartyRole>().Where(state.Marked).Select(r => StoneSpot(r)!.Value).ToList();
        return Enumerable.Range(-50, 101)
            .Select(step => holyOctant + step / 50f * EdgeSearchOctants)
            .OrderByDescending(octant => NeedleClearance(Point(octant, EdgeRadius), stars))
            .ThenBy(octant => MathF.Abs(octant - holyOctant))
            .First();
    }

    private static float NeedleClearance(Vector3 spot, List<Vector3> stars)
    {
        var clearance = float.PositiveInfinity;
        foreach (var star in stars)
        {
            var offset = spot - star;
            clearance = MathF.Min(clearance, offset.Length() - NeedleCircleRadius);
            for (var line = 0; line < 4; line++)
            {
                var along = Point(line, 1);
                var across = MathF.Abs(offset.X * along.Z - offset.Z * along.X);
                clearance = MathF.Min(clearance, across - NeedleLineHalfWidth);
            }
        }
        return clearance;
    }

    private int RotationAroundRim(PartyRole role)
    {
        if (Cursed) return 1;
        var clockwiseDestination = Point(KnockbackOctant(role) + 2, 1);
        return Vector3.Dot(clockwiseDestination, state.MirrorPosition) < 0 ? 1 : -1;
    }

    private Vector3 HolySpot(PartyRole role, int step)
    {
        var start = KnockbackOctant(role);
        if (!mana) return Point(start + (Cursed ? 1f / 3 : 0) + RotationAroundRim(role) * step * 0.5f, 18);
        var relative = (state.MirrorOctant - start + 8) % 8;
        var rate = (relative % 4) switch
        {
            0 => 0.75f,
            2 => 0.5f,
            3 => 0.75f,
            _ => relative == 1 ? -0.5f : 0.5f,
        };
        return Point(start + (Cursed ? 1f / 3 : 0f) + step * rate, 18);
    }

    private Vector3? IceSpot(PartyRole role) => HolySpot(role, !mana && Cursed ? 5 : 4) * (17f / 18);

    private void RememberHolyPuddles(int wave)
    {
        foreach (var healer in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer })
            if (world.Party.Get(healer) is { } member && member.IsAlive()) puddles.Add((wave, member.Position));
    }

    private IEnumerable<Vector3> LivePuddles()
        => puddles.Where(p => PuddleFadesAt(p.Wave) + offset > world.Events.Elapsed).Select(p => p.Position);

    private IEnumerable<SimCharacter> Bots()
        => world.Party.ActiveMembers().Where(world.Party.IsBotDriven).ToList();

    private void FaceAwayFromGaze()
    {
        foreach (var bot in Bots())
        {
            bot.StopMoving();
            bot.Face(2 * bot.Position - state.GazePosition);
        }
    }

    private void SlideBots(bool behind, bool recovery = false)
    {
        foreach (var bot in Bots())
        {
            var skip = recovery
                ? mana ? BehindMirror(bot.Position) || SlideDestination(bot.Position, true) != null : !Cursed || Vector3.Dot(bot.Position, state.MirrorPosition) <= 0
                : BehindMirror(bot.Position) == behind;
            if (skip || SlideDestination(bot.Position, behind, prepareNextSlide: recovery) is not { } destination) continue;
            bot.IceSlideTo(destination, SlideSeconds);
        }
    }

    private bool BehindMirror(Vector3 position)
    {
        var offset = position - state.MirrorPosition;
        return offset.LengthSquared() > 0.001f
            && Vector3.Dot(Vector3.Normalize(offset), Vector3.Normalize(state.MirrorPosition)) > MathF.Sqrt(0.5f);
    }

    private Vector3? SlideDestination(Vector3 from, bool behind, bool prepareNextSlide = false)
    {
        Vector3? best = null;
        var bestMargin = float.NegativeInfinity;
        var live = LivePuddles().ToList();
        for (var degree = 0; degree < 720; degree++)
        {
            var end = from + Point(degree / 90f, SlideDistance);
            if (end.Length() > 19 || BehindMirror(end) != behind) continue;
            var clearance = live.Select(p => DistanceToSegment(p, from, end) - PuddleRadius).DefaultIfEmpty(20).Min();
            if (clearance < 0.35f) continue;
            var relative = end - state.MirrorPosition;
            var backDot = Vector3.Dot(Vector3.Normalize(relative), Vector3.Normalize(state.MirrorPosition));
            var coneMargin = (behind ? backDot - MathF.Sqrt(0.5f) : MathF.Sqrt(0.5f) - backDot) * relative.Length();
            var margin = MathF.Min(MathF.Min(20 - end.Length(), clearance), coneMargin);
            if (margin > bestMargin && (!prepareNextSlide || SlideDestination(end, true) != null))
            {
                bestMargin = margin;
                best = end;
            }
        }
        return best;
    }

    private static float DistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
    {
        var line = to - from;
        var t = line.LengthSquared() > 0 ? Math.Clamp(Vector3.Dot(point - from, line) / line.LengthSquared(), 0, 1) : 0;
        return Vector3.Distance(point, from + t * line);
    }

    private static Vector3 Point(float octant, float radius) => FruP2DiamondDustState.Point(octant, radius);

    private static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();
}
