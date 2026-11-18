using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.TopNative.HelloWorld.TopHelloWorldScenario;

namespace AnoMech.Scenarios.TopNative.HelloWorld;

public sealed class TopHelloWorldAi : IScenarioAi<TopHelloWorldState>
{
    public string Name => "Elemental";
    public string Group => "Elemental";

    public static readonly PartyRole[] Priority =
    [
        PartyRole.RegenHealer, PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA,
        PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.ShieldHealer,
    ];

    private const float OverflowOutside = 18.8f;
    private const float Marker = 14f;
    private const float LocalBeside = 14.5f;
    private const float SyncInside = 12.3f;
    private const float RemoteBetween = 11.7f;
    private const float LocalLastLoop = 9.3f;
    private const float RemoteClear = 14.5f;
    private const float LocalMeet = 10.4f;
    private const float TetherRing = 12f;
    private const float DebuffRing = 6f;
    private const float ArcStep = 20f;
    private static readonly (float Bearing, float Radius) Spawn = (180f, 15f);

    private TopHelloWorldState state = null!;
    private AiManager ai = null!;
    private readonly Dictionary<PartyRole, (float Bearing, float Radius)> plan = [];

    public void Run(TopHelloWorldState stateParam, SimWorld world)
    {
        state = stateParam;
        ai = new AiManager(world);
        plan.Clear();
        foreach (var role in Priority) plan[role] = Spawn;
        for (var i = 0; i < Priority.Length; i++) Go(0.5f, Priority[i], 22.5f + 45f * i, 10f, DebuffRing);
        for (var loop = 0; loop < TopHelloWorldState.Loops; loop++) Loop(loop);
    }

    private static Vector2 Polar(float bearing, float radius)
    {
        var p = TopNativeConstants.Polar(bearing, radius);
        return new Vector2(p.X, p.Z);
    }

    private void Tether(float at, PartyRole role, float bearing, float radius) => Go(at, role, bearing, radius, TetherRing);
    private void Debuff(float at, PartyRole role, float bearing, float radius) => Go(at, role, bearing, radius, DebuffRing);

    private void Go(float at, PartyRole role, float bearing, float radius, float ring)
    {
        var (fromBearing, fromRadius) = plan[role];
        var sweep = AngleDelta(fromBearing, bearing);
        var steps = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(sweep) / ArcStep));
        var previous = Polar(fromBearing, fromRadius);
        var time = at;
        for (var k = 1; k <= steps; k++)
        {
            var t = (float)k / steps;
            var r = fromRadius + (radius - fromRadius) * t;
            var point = k == steps ? Polar(bearing, radius) : Polar(fromBearing + sweep * t, MathF.Max(r, ring));
            ai.Move(time, () => AiMove.Single(role, point), jitter: 0f);
            time += Vector2.Distance(previous, point) / AiManager.RunSpeed;
            previous = point;
        }
        plan[role] = (bearing, radius);
    }

    private static float AngleDelta(float from, float to) => ((to - from) % 360f + 540f) % 360f - 180f;

    private void Loop(int loop)
    {
        var towers = state.Towers[loop];
        var last = loop == TopHelloWorldState.Loops - 1;
        var overflow = state.WithRole(HelloRole.Overflow, loop, Priority);
        var local = state.WithRole(HelloRole.Local, loop, Priority);
        var sync = state.WithRole(HelloRole.Synchronization, loop, Priority);
        var remote = state.WithRole(HelloRole.Remote, loop, Priority);
        var setUp = TowerCasts[loop] + 0.5f;
        var resolve = ResolveTime(loop);
        for (var i = 0; i < 2; i++)
        {
            var away = i == 0 ? -1f : 1f;
            var toward = -away;
            var own = towers.Bearing(i);
            var stack = towers.Bearing(2 + i);
            Debuff(setUp, overflow[i], own, OverflowOutside);
            Debuff(setUp, sync[i], stack + 18f * toward, SyncInside);
            Tether(setUp, remote[i], stack + 30f * toward, RemoteBetween);
            if (last) Tether(setUp, local[i], stack + 30f * toward, LocalLastLoop);
            else Tether(setUp, local[i], own + 34f * away, LocalBeside);

            if (last)
            {
                Tether(resolve + 0.15f, remote[i], stack - 25f * toward, RemoteClear);
                Tether(resolve + 1.2f, local[i], stack + 30f * toward, 1.5f);
                continue;
            }
            Debuff(resolve + 0.15f, overflow[i], own, Marker);
            Tether(resolve + 0.15f, remote[i], stack + 18f * toward, SyncInside);
            Tether(resolve + 1.5f, remote[i], stack - 25f * toward, RemoteClear);
            Tether(resolve + 1.8f, local[i], own, Marker);
            Tether(resolve + 3.2f, local[i], towers.Bearing(0) + 45f, LocalMeet);
        }
    }
}
