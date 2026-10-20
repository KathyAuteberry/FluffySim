using System;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Map;

internal sealed class SimArenaBoundary : ISimObject
{
    private readonly SimParty party;
    private readonly float radiusSq;
    private readonly string cause;
    private readonly bool square;

    public bool IsAlive => true;
    public bool IsActive => true;

    internal SimArenaBoundary(SimParty party, float radius, string cause, bool square = false)
    {
        this.party = party;
        this.square = square;
        this.radiusSq = radius * radius;
        this.cause = cause;
    }

    internal bool IsOutside(Vector3 local) => square ? MathF.Max(local.X * local.X, local.Z * local.Z) > radiusSq : local.X * local.X + local.Z * local.Z > radiusSq;

    public void Tick(float deltaSeconds)
    {
        // Member positions are scenario-local; the boundary is centered on local zero.
        foreach (var member in party.ActiveMembers())
        {
            if (!IsOutside(member.Position)) continue;
            member.Die(SimCharacterDeathExtensions.Environment, cause);
        }
    }

    public void Despawn() { }
}
