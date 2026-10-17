using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.Sanctity2;

public enum MeteorPractice { Random, Meteor, NoMeteor }
public sealed class DsrSanctity2StateOverrides
{
    public bool AdvancedMode { get; set; }
    public MeteorPractice PracticeAssignment { get; set; }
    public bool? Cursed { get; set; }
    public int? TargetPair { get; set; }
    public int? TowerPattern { get; set; }
    public int? InnerPattern { get; set; }
}

public sealed record MeteorTowerPattern(int[] Outer)
{
    public int OutsideCount => Outer.Sum(mask => System.Numerics.BitOperations.PopCount((uint)mask));
    public bool ForcedCursed => (Outer[0] == 1 && Outer[2] == 4) || (Outer[0] == 4 && Outer[2] == 1);
    public IEnumerable<int> Towers => Enumerable.Range(0,12).Where(i => (Outer[i/3] & (1 << (i%3))) != 0);
}

public sealed class DsrSanctity2State
{
    public static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();
    public static readonly int[] BaseQuadrants = [0,2,3,1,3,1,0,2];
    public static readonly (PartyRole One, PartyRole Two)[] TargetPairs =
        (from a in Roles from b in Roles where (int)a < (int)b && a.IsDps() == b.IsDps() select (a,b)).ToArray();
    public static readonly MeteorTowerPattern[] Patterns = BuildPatterns();
    public bool MeteorOnDps { get; }
    public PartyRole[] Targets { get; }
    public int PatternIndex { get; }
    public int InnerMask { get; }
    public MeteorTowerPattern Pattern => Patterns[PatternIndex];
    public int[] Quadrants { get; }
    public int[] FirstTowers { get; } = new int[8];
    public IReadOnlyList<int> TowerIndices => Pattern.Towers.Concat(Enumerable.Range(0,4).Where(q => (InnerMask & (1<<q)) != 0).Select(q => q+12)).ToArray();
    public bool IsMeteor(PartyRole role) => Targets.Contains(role);
    public bool IsMeteorRole(PartyRole role) => role.IsDps() == MeteorOnDps;
    public int Quadrant(PartyRole role) => Quadrants[(int)role];

    public DsrSanctity2State(Rng rng, DsrSanctity2StateOverrides o, PartyRole human)
    {
        var pairs = TargetPairs.Where((pair,index) => o.AdvancedMode
            ? o.TargetPair is null || o.TargetPair == index
            : o.PracticeAssignment == MeteorPractice.Random || (new[]{pair.One,pair.Two}.Contains(human) == (o.PracticeAssignment == MeteorPractice.Meteor))).ToArray();
        if (pairs.Length == 0) throw new ArgumentException("No valid Meteor target pair.");
        var pair = pairs[rng.Next(pairs.Length)]; Targets = [pair.One,pair.Two]; MeteorOnDps = pair.One.IsDps();
        Quadrants = AdjustQuadrants(Targets);
        var patterns = Enumerable.Range(0,Patterns.Length).Where(i =>
            (!o.AdvancedMode || o.TowerPattern is null || o.TowerPattern == i) && (o.Cursed is null || Patterns[i].ForcedCursed == o.Cursed) &&
            (!o.AdvancedMode || o.InnerPattern is null || System.Numerics.BitOperations.PopCount((uint)o.InnerPattern.Value) == 8-Patterns[i].OutsideCount)).ToArray();
        if (patterns.Length == 0) throw new ArgumentException("Tower pattern conflicts with the cursed filter.");
        PatternIndex = patterns[rng.Next(patterns.Length)];
        var masks = Enumerable.Range(1,15).Where(m => System.Numerics.BitOperations.PopCount((uint)m) == 8-Pattern.OutsideCount &&
            (!o.AdvancedMode || o.InnerPattern is null || m == o.InnerPattern)).ToArray();
        if (masks.Length == 0) throw new ArgumentException("Inner tower mask must contain the remaining two or three towers.");
        InnerMask = masks[rng.Next(masks.Length)];
        AssignTowers(rng);
    }
    public static int[] AdjustQuadrants(IReadOnlyList<PartyRole> targets)
    {
        if (targets.Count != 2 || targets[0] == targets[1] || targets[0].IsDps() != targets[1].IsDps()) throw new ArgumentException("Two distinct targets from the same role category are required.");
        var result = (int[])BaseQuadrants.Clone();
        var category = Roles.Where(r => r.IsDps() == targets[0].IsDps()).ToArray();
        if (targets.All(r => BaseQuadrants[(int)r] % 2 == 1)) { Swap(3,0); Swap(1,2); }
        else foreach (var target in targets.Where(r => BaseQuadrants[(int)r] % 2 == 1))
            Swap(BaseQuadrants[(int)target], Enumerable.Range(0,2).Select(i => i*2).Single(q => !targets.Any(r => result[(int)r] == q)));
        return result;
        void Swap(int a,int b)
        {
            var one = category.Single(r => result[(int)r] == a); var two = category.Single(r => result[(int)r] == b);
            result[(int)one] = b; result[(int)two] = a;
        }
    }
    private void AssignTowers(Rng rng)
    {
        int[] priority = [1,0,2];
        var north = priority.First(b => (Pattern.Outer[0] & (1<<b)) != 0);
        var south = priority.First(b => (Pattern.Outer[2] & (1<<b)) != 0);
        if (Math.Abs(north-south) == 2)
        {
            var alternatives = (from n in priority from s in priority
                where (Pattern.Outer[0] & (1<<n)) != 0 && (Pattern.Outer[2] & (1<<s)) != 0 && Math.Abs(n-s) < 2 select (n,s)).ToArray();
            if (alternatives.Length > 0) (north,south) = alternatives[0];
        }
        var innerRoles = new List<PartyRole>();
        foreach (var q in Enumerable.Range(0,4))
        {
            var meteorRole = Roles.Single(r => Quadrant(r) == q && IsMeteorRole(r));
            var other = Roles.Single(r => Quadrant(r) == q && !IsMeteorRole(r));
            var selected = q == 0 ? north : q == 2 ? south : priority.First(b => (Pattern.Outer[q] & (1<<b)) != 0);
            FirstTowers[(int)meteorRole] = 3*q+selected;
            var second = priority.Where(b => b != selected && (Pattern.Outer[q] & (1<<b)) != 0).ToArray();
            if (second.Length > 0) FirstTowers[(int)other] = 3*q+second[0]; else innerRoles.Add(other);
        }
        var remaining = Enumerable.Range(0,4).Where(q => (InnerMask & (1<<q)) != 0).ToList();
        foreach (var role in innerRoles.ToArray())
            if (remaining.Remove(Quadrant(role))) { FirstTowers[(int)role] = 12+Quadrant(role); innerRoles.Remove(role); }
        foreach (var role in innerRoles)
        {
            var index = rng.Next(remaining.Count); FirstTowers[(int)role] = 12+remaining[index]; remaining.RemoveAt(index);
        }
    }
    public static Vector3 Direction(float angle) => new(MathF.Sin(angle),0,-MathF.Cos(angle));
    public static Vector3 TowerPosition(int index) => index < 12
        ? Direction((index/3*90+(index%3-1)*30)*MathF.PI/180)*18f
        : Direction(((index-12)*90+45)*MathF.PI/180)*6f;
    public Vector3 PairPosition(PartyRole role) => Direction(Quadrant(role)*MathF.PI/2)*10;
    public Vector3 FirstPosition(PartyRole role)
    {
        var index = FirstTowers[(int)role]; var position = TowerPosition(index);
        if (index >= 12) return Vector3.Normalize(position)*3.3f;
        if (IsMeteor(role))
        {
            var cardinal = Direction(Quadrant(role)*MathF.PI/2)*18f;
            if (Vector3.Distance(position,cardinal) > 0.1f) position += Vector3.Normalize(cardinal-position)*2.6f;
        }
        return position;
    }
    public Vector3 CommunicationPosition(PartyRole role) => PairPosition(role)+Vector3.Normalize(FirstPosition(role)-PairPosition(role))*3;
    public Vector3 FinalPosition(PartyRole role) => Direction((Quadrant(role)*90+(IsMeteorRole(role) ? IsMeteor(role) ? 180 : 0 : 45))*MathF.PI/180)*18f;
    public Vector3 KnockbackPosition(PartyRole role) => Vector3.Normalize(FinalPosition(role))*2f;
    public bool TakesKnockback(PartyRole role) => FirstTowers[(int)role] >= 12;
    private static MeteorTowerPattern[] BuildPatterns()
    {
        int[] masks = [1,2,4,3,5,6];
        return (from n in masks from e in masks from s in masks from w in masks
                let p = new MeteorTowerPattern([n,e,s,w]) where p.OutsideCount is 5 or 6 select p).ToArray();
    }
}
