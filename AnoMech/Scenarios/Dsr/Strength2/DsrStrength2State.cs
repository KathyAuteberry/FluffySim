using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.Strength;

namespace AnoMech.Scenarios.Dsr.Strength2;

public enum DsrStrength2PracticeAssignment { Random, SkywardLeapAny, SkywardLeapWest, SkywardLeapEast, SkywardLeapSouth, Unmarked }

public sealed class DsrStrength2StateOverrides
{
    public DsrStrength2PracticeAssignment PracticeAssignment { get; set; }
    public bool? AdvancedMode { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public bool UsesAdvancedPatterns => AdvancedMode ?? (KnightPattern.HasValue || LeapTrio.HasValue || LeapOrder.HasValue || TowerCommunication.HasValue || TetherTargets.HasValue);
    public int? KnightPattern { get; set; }
    public int? LeapTrio { get; set; }
    public int? LeapOrder { get; set; }
    public int? TowerCommunication { get; set; }
    public int? TetherTargets { get; set; }
}

public sealed class DsrStrength2State
{
    public static readonly PartyRole[] NonTanks = [PartyRole.RegenHealer, PartyRole.ShieldHealer,
        PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];
    public static readonly PartyRole[][] LeapTrios = (from a in Enumerable.Range(0,6)
        from b in Enumerable.Range(a+1,5-a) from c in Enumerable.Range(b+1,5-b)
        select new[] { NonTanks[a],NonTanks[b],NonTanks[c] }).ToArray();
    public static readonly int[][] Orders = [[0,1,2],[0,2,1],[1,0,2],[1,2,0],[2,0,1],[2,1,0]];
    public int KnightPattern { get; }
    public int ReferenceClock { get; }
    public PartyRole[] Leaps { get; }
    public PartyRole[] TowerCommunicators { get; }
    public PartyRole[] InitialTetherTargets { get; }
    public PartyRole RageTarget { get; }
    public Vector3 WestKnight { get; }
    public Vector3 EastKnight { get; }
    public uint WestKnightId { get; }
    public uint EastKnightId { get; }
    public IReadOnlyList<Vector3> AdditionalPositions { get; }
    public Vector3 StackPosition => Rotate(new(0,0,-20));
    public DsrStrength2State(Rng rng, DsrStrength2StateOverrides o, PartyRole? playerRole = null)
    {
        var advanced = o.UsesAdvancedPatterns;
        KnightPattern = Roll(rng,advanced ? o.KnightPattern : null,8);
        AdditionalPositions = DsrStrengthScenario.AdditionalPositions(KnightPattern);
        var sum = AdditionalPositions[0] + AdditionalPositions[1];
        ReferenceClock = ((int)MathF.Round(MathF.Atan2(-sum.X,sum.Z)*4/MathF.PI)+8)%8;
        var east = Rotate(Vector3.UnitX);
        var adelphelWest = Vector3.Dot(AdditionalPositions[0],east)<0;
        WestKnight = AdditionalPositions[adelphelWest ? 0 : 1];
        EastKnight = AdditionalPositions[adelphelWest ? 1 : 0];
        WestKnightId = adelphelWest ? 12601u : 12632u;
        EastKnightId = adelphelWest ? 12632u : 12601u;
        int trioIndex, orderIndex;
        if (!advanced && o.PracticeAssignment != DsrStrength2PracticeAssignment.Random && playerRole is { } role && !role.IsTank())
        {
            var candidates = PracticePatterns(role, o.PracticeAssignment);
            (trioIndex, orderIndex) = candidates[rng.Next(candidates.Count)];
        }
        else
        {
            trioIndex = Roll(rng, advanced ? o.LeapTrio : null, 20);
            orderIndex = Roll(rng, advanced ? o.LeapOrder : null, 6);
        }
        var trio = LeapTrios[trioIndex];
        Leaps = Orders[orderIndex].Select(i=>trio[i]).ToArray();
        var unmarked = NonTanks.Except(Leaps).ToArray();
        TowerCommunicators = Orders[Roll(rng,advanced ? o.TowerCommunication : null,6)].Select(i=>unmarked[i]).ToArray();
        var tethers = Orders[Roll(rng,advanced ? o.TetherTargets : null,6)].Select(i=>unmarked[i]).ToArray();
        InitialTetherTargets = tethers[..2]; RageTarget = tethers[2];
    }
    public static IReadOnlyList<(int Trio, int Order)> PracticePatterns(PartyRole role, DsrStrength2PracticeAssignment assignment)
    {
        if (!Enum.IsDefined(assignment)) throw new ArgumentOutOfRangeException(nameof(assignment));
        return (from trio in Enumerable.Range(0, LeapTrios.Length)
            from order in Enumerable.Range(0, Orders.Length)
            let index = Array.IndexOf(Orders[order].Select(i => LeapTrios[trio][i]).ToArray(), role)
            where assignment switch
            {
                DsrStrength2PracticeAssignment.Random => true,
                DsrStrength2PracticeAssignment.SkywardLeapAny => index >= 0,
                DsrStrength2PracticeAssignment.SkywardLeapWest => index == 0,
                DsrStrength2PracticeAssignment.SkywardLeapEast => index == 1,
                DsrStrength2PracticeAssignment.SkywardLeapSouth => index == 2,
                DsrStrength2PracticeAssignment.Unmarked => index < 0,
                _ => false,
            }
            select (trio, order)).ToArray();
    }
    public Vector3 Rotate(Vector3 position)
    {
        var a = ReferenceClock*MathF.PI/4;
        return new(position.X*MathF.Cos(a)-position.Z*MathF.Sin(a),position.Y,
            position.X*MathF.Sin(a)+position.Z*MathF.Cos(a));
    }
    public Vector3 LeapPosition(PartyRole role)
    {
        var index = Array.IndexOf(Leaps,role);
        return Rotate(index switch { 0=>new(-19.6962f,0,3.473f),1=>new(19.6962f,0,3.473f),2=>new(0,0,20),
            _=>throw new ArgumentOutOfRangeException(nameof(role)) });
    }
    public Vector3 TankPosition(PartyRole role) => Rotate(new(role == PartyRole.MainTank ? 6.1803f : -6.1803f,0,-19.0211f));
    public Vector3 CommunicationPosition(PartyRole role)
    {
        var slot = Array.IndexOf(TowerCommunicators, role);
        return Rotate(new((slot - 1) * 6f, 0, slot == 1 ? -18f : -20f));
    }
    public Vector3 TowerPosition(PartyRole role)
    {
        var leap=Array.IndexOf(Leaps,role);
        var clock=leap>=0 ? new[] {6,2,4}[leap] : new[] {7,0,1}[Array.IndexOf(TowerCommunicators,role)];
        return Rotate(DsrStrengthState.Direction(clock)*12);
    }
    public IReadOnlyList<Vector3> CollapsePositions => Enumerable.Range(0,8).Select(i =>
    {
        var outer = i>=4;
        var clock=((i%4)*2+(outer ? 1 : 0)+ReferenceClock)%8;
        var radius=outer ? 18f : clock==4 ? 8f : 9f;
        return DsrStrengthState.Direction(clock)*radius;
    }).ToArray();
    private static int Roll(Rng rng,int? value,int max)=> value is {} v ? v>=0&&v<max?v:throw new ArgumentOutOfRangeException(nameof(value)):rng.Next(max);
}
