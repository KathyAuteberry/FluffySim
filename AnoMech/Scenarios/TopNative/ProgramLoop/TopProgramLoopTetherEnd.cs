using System;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.TopNative.ProgramLoop;

// Passable like End.Passable(exclusive: true), but a bot only intercepts when it is its turn to
// take a tether; the human takes it whenever they step in, right or wrong. Bots walking across
// the beams on their way to towers would otherwise steal it, which retail players avoid by eye.
public sealed class TopProgramLoopTetherEnd(SimCharacter? initial, Func<SimCharacter, bool> mayTake) : DynamicEnd
{
    private const float HalfWidth = 0.5f;
    private const float MinInterceptForward = 0.1f;

    public override SimCharacter? Resolve(SimCharacter? self, SimCharacter? other, TetherContext ctx)
    {
        if (self is null) return initial ?? ctx.Party.RandomMember(ctx.Rng);
        if (!self.IsAlive() || other is null || !other.IsAlive()) return self;
        var holderPos = self.Position;
        var anchorPos = other.Position;
        var dx = anchorPos.X - holderPos.X;
        var dz = anchorPos.Z - holderPos.Z;
        var len = MathF.Sqrt(dx * dx + dz * dz);
        if (len < 0.01f) return self;
        var fwdX = dx / len;
        var fwdZ = dz / len;
        SimCharacter? best = null;
        var bestDistSq = float.MaxValue;
        foreach (var c in ctx.Party.InsideRect(new Placement(holderPos, MathF.Atan2(dx, dz)), HalfWidth, len))
        {
            if (ReferenceEquals(c, self) || !mayTake(c)) continue;
            if (c.HasTetherInSlot0(ctx.TetherId) || ctx.HoldsParallelTether?.Invoke(c) == true) continue;
            var ddx = c.Position.X - holderPos.X;
            var ddz = c.Position.Z - holderPos.Z;
            if (ddx * fwdX + ddz * fwdZ < MinInterceptForward) continue;
            var d = ddx * ddx + ddz * ddz;
            if (d < bestDistSq) { bestDistSq = d; best = c; }
        }
        return best ?? self;
    }
}
