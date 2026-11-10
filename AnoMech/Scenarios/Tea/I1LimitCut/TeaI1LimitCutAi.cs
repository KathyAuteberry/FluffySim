using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using static AnoMech.Scenarios.Tea.TeaConstants;

namespace AnoMech.Scenarios.Tea.I1LimitCut;

public sealed class TeaI1LimitCutAi : IScenarioAi<TeaI1LimitCutState>
{
    public string Name => "1211";
    public string Group => "Mana";

    private static readonly float[] WarpAt = [11.55f, 16.16f, 20.80f, 25.50f];
    private static readonly float[] HitAt = [12.64f, 14.14f, 17.31f, 18.80f, 21.95f, 23.42f, 26.58f, 28.05f];
    private static readonly float[] ClaimAt = [9.4f, 13.9f, 18.1f, 20.3f];
    private static readonly float[] BaitLines = [45, 135, 180, 225];
    private static readonly (float At, float Angle, float Radius)[] Pack =
    [
        (3.5f, -45, 14), (5.65f, -45, 14), (7f, 0, 14), (7.75f, 0, 14),
        (9.2f, 45, 14), (9.95f, 45, 14), (11.4f, 90, 14), (12.15f, 90, 14),
        (13.6f, 135, 14), (16.5f, 135, 14), (18f, 180, 14), (18.75f, 180, 14),
        (20.2f, 225, 14), (20.95f, 225, 14), (22.4f, 270, 14), (23.1f, 270, 14),
        (24.55f, 315, 14), (25.4f, 315, 14), (27f, 360, 14), (29f, 360, 14), (32f, 360, 1),
    ];

    private TeaI1LimitCutState state = null!;
    private SimWorld world = null!;

    public void Run(TeaI1LimitCutState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);
        ai.Move(.3f, () => AiMove.Create(Enumerable.Repeat<Vector2?>(Vector2.Zero, 8).ToArray()).NaturalOrder(), jitter: 0f);
        for (var i = 0; i < 8; i++)
        {
            var k = i;
            ai.UseAction(HitAt[k] - 3f, state.Numbers[k], KnockbackImmunity.ActionId);
            world.Events.Add(k % 2 == 0 ? WarpAt[k / 2] - .25f : HitAt[k] - .3f, () => FaceForHit(k));
        }
        world.Events.Add(3.3f, () =>
        {
            foreach (var member in world.Party.ActiveMembers())
                if (world.Party.IsBotDriven(member)) (member as ISimPartyMember)?.UseSprint(20f);
        });
        world.Events.Add(3.5f, Steer);
    }

    private void Steer()
    {
        var now = world.Events.Elapsed;
        if (now > 32f) return;
        world.Events.Add(.1f, Steer);
        for (var k = 0; k < 8; k++)
        {
            var member = world.Party.Get(state.Numbers[k]);
            if (member is null || !world.Party.IsBotDriven(member) || !member.IsAlive()) continue;
            if (now >= WarpAt[k / 2] - .3f && now <= HoldUntil(k))
            {
                member.StopMoving();
                continue;
            }
            var (angle, radius) = Destination(k, now + .25f);
            var currentRadius = new Vector2(member.Position.X, member.Position.Z).Length();
            if (currentRadius > 2f && radius > 10f)
            {
                var currentAngle = float.RadiansToDegrees(MathF.Atan2(member.Position.X, -member.Position.Z));
                var advance = (angle - currentAngle + 540f) % 360f - 180f;
                angle = currentAngle + Math.Clamp(advance, -12f, 12f);
            }
            var target = Geometry.OnCompass(angle, radius);
            var distance = Vector3.Distance(member.Position, target);
            if (distance < .08f) continue;
            var speed = now < 23.3f ? AiManager.SprintSpeed : AiManager.RunSpeed;
            member.MoveTo(target, MathF.Min(speed, distance / .15f));
        }
    }

    private (float Angle, float Radius) Destination(int k, float at)
    {
        var pair = k / 2;
        if (at >= ClaimAt[pair] && at <= HoldUntil(k))
            return (StartHeading(state.FirstBlasterAxis) + BaitLines[pair], k % 2 == 0 ? 18f : 12f);
        for (var i = 1; i < Pack.Length; i++)
        {
            if (at > Pack[i].At) continue;
            var outerLane = k % 2 == 0 && at < HitAt[k + 1] + .65f && at > HitAt[k];
            var a = Pack[i - 1];
            var b = Pack[i];
            var f = Math.Clamp((at - a.At) / (b.At - a.At), 0, 1);
            return (StartHeading(state.FirstBlasterAxis) + a.Angle + (b.Angle - a.Angle) * f,
                outerLane ? 18f : a.Radius + (b.Radius - a.Radius) * f);
        }
        return (StartHeading(state.FirstBlasterAxis) + Pack[^1].Angle, Pack[^1].Radius);
    }

    private static float HoldUntil(int k) => HitAt[k] + (k % 2 == 0 ? .4f : .65f);

    private void FaceForHit(int k)
    {
        if (world.Party.Get(state.Numbers[k]) is not { } member || !world.Party.IsBotDriven(member)) return;
        member.StopMoving();
        member.Face(member.Position * 2f);
    }

    internal static float StartHeading(int axis) => axis switch
    {
        0 => 0, 1 => 45, 2 => 90, 3 => 315,
        _ => throw new ArgumentOutOfRangeException(nameof(axis)),
    };
}
