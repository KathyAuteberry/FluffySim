using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.Apocalypse;

public enum ApocalypseWater { None, Short, Medium, Long }
public enum ApocalypseSwapPractice { Random, Swap, Stay }
public sealed class FruApocalypseStateOverrides
{
    public ApocalypseWater? PracticeWater { get; set; }
    public ApocalypseSwapPractice SwapPractice { get; set; }
    public bool? Clockwise { get; set; }
    public bool Advanced { get; set; }
    public int? WaterPermutation { get; set; }
    public int? InitialAxis { get; set; }
    public PartyRole? SpiritTarget { get; set; }
    public PartyRole? BaitTank { get; set; }
}

public sealed class FruApocalypseState
{
    public static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();
    public static readonly int[][] Distributions = BuildDistributions().ToArray();
    private static IEnumerable<int[]> BuildDistributions()
    {
        var values = new int[8]; var remaining = new[] { 2,2,2,2 };
        return Fill(0);
        IEnumerable<int[]> Fill(int seat)
        {
            if (seat == 8) { yield return (int[])values.Clone(); yield break; }
            for (var i=0;i<4;i++) if (remaining[i]>0)
            {
                remaining[i]--; values[seat]=i;
                foreach(var distribution in Fill(seat+1)) yield return distribution;
                remaining[i]++;
            }
        }
    }
    public int Permutation { get; }
    public bool Clockwise { get; }
    public int Axis { get; }
    public PartyRole SpiritTarget { get; }
    public PartyRole BaitTank { get; }
    public IReadOnlyList<int> Water => Array.AsReadOnly(Distributions[Permutation]);
    public static bool[] Swappers(IReadOnlyList<int> water)
    {
        var swaps = new bool[8];
        foreach(var start in new[] {0,4})
            foreach(var category in Enumerable.Range(0,4))
            {
                var duplicate=Enumerable.Range(start,4).Where(i=>water[i]==category).ToArray();
                if(duplicate.Length==2) swaps[duplicate[0]]=true;
            }
        return swaps;
    }
    public FruApocalypseState(Rng rng,FruApocalypseStateOverrides o,PartyRole human,PartyRole defaultBait)
    {
        if(o.Advanced && (o.WaterPermutation is <0 or >2519 || o.InitialAxis is <0 or >3 || o.SpiritTarget is { } target && !Roles.Contains(target) || o.BaitTank is { } tank && !tank.IsTank()))
            throw new ArgumentException("Invalid Apocalypse exact pattern.");
        if(o.Advanced && o.BaitTank is { } requested && requested!=defaultBait)
            throw new ArgumentException("Dance bait must follow PLD > ST for the current party.");
        var candidates=Enumerable.Range(0,Distributions.Length).Where(i=>
            o.Advanced ? o.WaterPermutation==null || i==o.WaterPermutation :
            (o.PracticeWater==null || Distributions[i][(int)human]==(int)o.PracticeWater) &&
            (o.SwapPractice==ApocalypseSwapPractice.Random || Swappers(Distributions[i])[(int)human]==(o.SwapPractice==ApocalypseSwapPractice.Swap))).ToArray();
        if(candidates.Length==0)throw new ArgumentException("No valid distribution for this practice assignment; H2 and D4 never swap.");
        Permutation=candidates[rng.Next(candidates.Length)];Clockwise=o.Clockwise??rng.NextBool();
        Axis=o.Advanced ? o.InitialAxis??rng.Next(4) : rng.Next(4);
        SpiritTarget=o.Advanced ? o.SpiritTarget??Roles[rng.Next(8)] : Roles[rng.Next(8)];
        BaitTank=o.Advanced ? o.BaitTank??defaultBait : defaultBait;
    }
    public int Order(PartyRole role)=>Distributions[Permutation][(int)role];
    public static Vector3 Direction(float angle)=>new(MathF.Sin(angle),0,-MathF.Cos(angle));
    public IReadOnlyList<Vector3> Wave(int wave)
    {
        var result=new List<Vector3>();
        if(wave<2) {result.Add(Vector3.Zero);result.Add(Vector3.Zero);}
        for(var offset=Math.Max(0,wave-2);offset<=wave;offset++)
        {
            var p=Direction((Axis+(Clockwise?offset:-offset))*MathF.PI/4)*14;
            result.Add(p);result.Add(-p);
        }
        return result;
    }
}
