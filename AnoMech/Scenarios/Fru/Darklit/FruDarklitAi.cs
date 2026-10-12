using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.Darklit;

public sealed class DarklitManaAssignments
{
    private readonly DarklitPattern pattern;
    private readonly PartyRole[] towers;
    private readonly PartyRole[] initialBaits;
    private readonly PartyRole[] baits;
    public bool WaterAdjusted { get; }
    public PartyRole? WaterPartner { get; }
    public DarklitManaAssignments(DarklitPattern pattern)
    {
        this.pattern = pattern;
        towers = [pattern.Healer, pattern.Tank, pattern.EastDps, pattern.WestDps];
        if (pattern.Shape == DarklitTetherPattern.Box) (towers[1], towers[2]) = (towers[2], towers[1]);
        if (pattern.Shape == DarklitTetherPattern.Hourglass) (towers[1], towers[3]) = (towers[3], towers[1]);
        var others = FruDarklitState.Roles.Except(pattern.Tethered).ToArray();
        var dps = others.Where(r => r.IsDps()).Order().ToArray();
        initialBaits = [others.Single(r => r is PartyRole.RegenHealer or PartyRole.ShieldHealer),
            others.Single(r => r.IsTank()), dps[1], dps[0]];
        baits = (PartyRole[])initialBaits.Clone();
        WaterAdjusted = IsSouth(Array.IndexOf(towers, pattern.TetherWater)) == IsSouth(Array.IndexOf(baits, pattern.UntetherWater));
        if (WaterAdjusted)
        {
            var index = Array.IndexOf(baits, pattern.UntetherWater);
            var partner = 3 - index;
            WaterPartner = baits[partner];
            (baits[index], baits[partner]) = (baits[partner], baits[index]);
        }
    }
    private static bool IsSouth(int corner) => corner >= 2;
    public int Corner(PartyRole role, bool beforeWater = false) => Array.IndexOf(pattern.IsTethered(role) ? towers : beforeWater ? initialBaits : baits, role);
    public bool South(PartyRole role) => IsSouth(Corner(role));
    public Vector3 Lineup(PartyRole role)
    {
        Vector3[] positions = [new(2.12f, 0, -1.91f), new(4.79f, 0, -.085f),
            new(-4.79f, 0, -.085f), new(-2.12f, 0, -1.91f),
            new(-4.79f, 0, 8.99f), new(-2.12f, 0, 10.81f),
            new(2.12f, 0, 10.81f), new(4.79f, 0, 8.99f)];
        return positions[(int)role];
    }
    public Vector3 SideAdjustedLineup(PartyRole role)
        => pattern.RequiresDpsSideSwap && role is PartyRole.MeleeDpsB or PartyRole.PhysRangedDps
            ? Lineup(role == PartyRole.MeleeDpsB ? PartyRole.PhysRangedDps : PartyRole.MeleeDpsB) : Lineup(role);
    public Vector3 Formation(PartyRole role, bool beforeWater = false)
    {
        var corner = Corner(role, beforeWater);
        var west = corner is 0 or 3;
        return new((west ? -1 : 1) * (pattern.IsTethered(role) ? 2.2f : 6.2f), 0,
            (IsSouth(corner) ? 1 : -1) * (pattern.IsTethered(role) ? 10.5f : 2.84f));
    }
    public Vector3 SpiritSpread(PartyRole role)
    {
        var corner = Corner(role);
        return corner switch
        {
            0 => pattern.IsTethered(role) ? new(-4.28f, 0, -6.95f) : new(-8.05f, 0, 0),
            1 => pattern.IsTethered(role) ? new(4.28f, 0, -6.95f) : new(8.05f, 0, 0),
            2 => pattern.IsTethered(role) ? new(3.82f, 0, 13.32f) : new(8.05f, 0, 6.36f),
            _ => pattern.IsTethered(role) ? new(-3.82f, 0, 13.32f) : new(-8.05f, 0, 6.36f),
        };
    }
    public Vector3 WaterStack(PartyRole role, bool eastWing) => new(eastWing ? -7.071f : 7.071f, 0, South(role) ? 7.337f : -7.337f);
    public Vector3 DanceParty(PartyRole role) => new(0, 0, South(role) ? 6.36f : -6.36f);
    public Vector3 DanceBait(bool eastWing) => new(eastWing ? -15.3f : 15.3f, 0, 0);
}

public sealed class FruDarklitAi : IScenarioAi<FruDarklitState>
{
    public string Name => "Mana Nukemaru / Bowtie / 扇前";
    public string Group => "JP";
    public void Run(FruDarklitState state, SimWorld world)
    {
        var assignment = new DarklitManaAssignments(state.Pattern);
        void Move(Func<PartyRole, Vector3> target)
        { foreach (var role in FruDarklitState.Roles) world.Party.Get(role)?.MoveTo(target(role)); }
        world.Events.Add(.1f, () => Move(assignment.Lineup));
        world.Events.Add(9.5f, () => Move(assignment.SideAdjustedLineup));
        world.Events.Add(10.6f, () => Move(r => assignment.Formation(r, true)));
        world.Events.Add(12f, () => Move(r => assignment.Formation(r)));
        world.Events.Add(19.2f, () => Move(assignment.SpiritSpread));
        world.Events.Add(21.9f, () => Move(r => assignment.WaterStack(r, state.EastWing)));
        world.Events.Add(26.5f, () =>
        {
            Move(assignment.DanceParty);
            world.Party.Get(state.BaitTank)!.MoveTo(assignment.DanceBait(state.EastWing));
        });
        world.Events.Add(29f, () =>
        {
            var member = world.Party.Get(state.BaitTank)!;
            if (!world.Party.IsBotDriven(member)) return;
            var (action, status) = member.Proxy?.ClassJob switch
            { 19 => (30u, (ushort)82), 21 => (43u, (ushort)409), 32 => (3638u, (ushort)810), _ => (16152u, (ushort)1836) };
            if (member is SimPartyNpc npc) npc.PlayAction(action);
            member.AddStatus(status, 10);
        });
        world.Events.Add(30.2f, () => world.Party.Get(state.BaitTank)!.MoveTo(((FruDarklitScenario)Plugin.Game.ActiveScenario!).OraclePosition));
    }
}
