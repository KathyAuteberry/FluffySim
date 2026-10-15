using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru.UltimateRelativity;

public sealed class FruUltimateRelativityScenario : IScenario
{
    public string Name => "Ultimate Relativity (Mana)";
    public string DisplayNameOverride => "P3 Ultimate Relativity";
    public IPhase Phase => FruZone.P3;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruUltimateRelativityAi()];
    private readonly FruUltimateRelativitySettingsWindow settings = new();
    public object SettingsOverrides => settings.Overrides;
    public void DrawSettings() => settings.Draw();
    public FruUltimateRelativityState? LastState { get; private set; }
    public bool Completed { get; private set; }
    public IReadOnlyDictionary<PartyRole, Vector3> ReturnPositions => returns;
    public IReadOnlyList<(float Time, uint Action)> Resolutions => resolutions;
    private readonly Dictionary<PartyRole, Vector3> returns = [];
    private readonly List<(float, uint)> resolutions = [];
    private readonly List<SimTether> tethers = [];
    private readonly SimEnemy[] glasses = new SimEnemy[8];
    private readonly SimEnemy[] lasers = new SimEnemy[8];
    private readonly SimEnemy[] helpers = new SimEnemy[8];
    private readonly Dictionary<int, (PartyRole Target, float Heading)> laserSnapshots = [];
    private SimEnemy boss = null!;
    private SimEnemy stackHelper = null!;
    private SimEnemy iceHelper = null!;
    private SimWorld world = null!;
    private FruUltimateRelativityState state = null!;
    public static readonly ushort[] PlayerStatuses = [2454, 2455, 2456, 2460, 2461, 2462, 2464, 2452, 4163];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new(world.Rng, settings.Overrides);
        LastState = state;
        Completed = false;
        returns.Clear(); resolutions.Clear(); tethers.Clear(); laserSnapshots.Clear();
        Array.Clear(glasses); Array.Clear(lasers); Array.Clear(helpers);
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(3f, StartRelativity);
        world.Events.Add(13.83f, AssignStatuses);
        world.Events.Add(14.38f, SpawnHourglasses);
        world.Events.Add(17.05f, ShowTethers);
        world.Events.Add(17.13f, () => boss.Cast(FruUltimateRelativityActions.Speed));
        world.Events.Add(22.63f, SpeedEffects);
        world.Events.Add(24.90f, () => ResolveFireAndStack(1));
        world.Events.Add(25.88f, () => RotateHourglasses(1));
        world.Events.Add(25.97f, () => CastHourglasses(1));
        world.Events.Add(29.80f, () => RecordReturn(1));
        ScheduleLasers(1, 30.03f);
        world.Events.Add(34.90f, () => ResolveFireAndStack(2));
        world.Events.Add(35.94f, () => RotateHourglasses(2));
        world.Events.Add(36.03f, () => CastHourglasses(2));
        world.Events.Add(39.83f, () => RecordReturn(2));
        ScheduleLasers(2, 40.07f);
        world.Events.Add(44.90f, () => ResolveFireAndStack(3));
        world.Events.Add(46.93f, () => RotateHourglasses(3));
        world.Events.Add(47.02f, () => CastHourglasses(3));
        ScheduleLasers(3, 51.06f);
        world.Events.Add(53.84f, Stun);
        world.Events.Add(55.30f, Rewind);
        world.Events.Add(56.90f, FinalResolution);
        world.Events.Add(57.83f, () => boss.Cast(FruUltimateRelativityActions.ShellVisual));
        world.Events.Add(61.10f, ResolveShell);
        world.Events.Add(65f, Cleanup);
        if (selectedAi is { } index) ((IScenarioAi<FruUltimateRelativityState>)AiStrats[index]).Run(state, world);
    }

    public void Tick(float delta, float elapsed)
    {
        if (world.Party.ActiveMembers().Count() < 8) world.SoftMarkers.Clear();
    }

    private SimEnemy Spawn(uint baseId, uint nameId, Vector3 position, bool visible, float heading = MathF.PI) =>
        world.SpawnEnemy(new EnemySpawnConfig(baseId, nameId, Level: 100,
            Targetable: visible && baseId == BNpcBaseId.OracleOfDarkness,
            EnemyList: baseId == BNpcBaseId.OracleOfDarkness ? EnemyListMode.Always : EnemyListMode.Never,
            Visibility: visible ? SpawnVisibility.Visible : SpawnVisibility.InvisibleHelper, Placement: new(position, heading),
            Appearance: baseId == BNpcBaseId.OracleOfDarkness ? new NpcAppearance(65537, 4295237459) : null))
        ?? throw new InvalidOperationException($"Could not spawn Ultimate Relativity actor {baseId}.");

    private void SpawnActors()
    {
        boss = Spawn(BNpcBaseId.OracleOfDarkness, BNpcNameId.OracleOfDarkness, Vector3.Zero, true);
        stackHelper = Spawn(BNpcBaseId.Helper, BNpcNameId.OracleOfDarkness, Vector3.Zero, false);
        iceHelper = Spawn(BNpcBaseId.Helper, BNpcNameId.OracleOfDarkness, Vector3.Zero, false);
        for (var i = 0; i < 8; i++)
        {
            helpers[i] = Spawn(BNpcBaseId.Helper, BNpcNameId.OracleOfDarkness, Vector3.Zero, false);
            lasers[i] = Spawn(BNpcBaseId.Helper, BNpcNameId.OracleOfDarkness, state.Hourglass(i), false);
        }
    }

    private void StartRelativity()
    {
        if (helpers.Concat(lasers).Append(stackHelper).Append(iceHelper).Append(boss).Any(a => a.PacketSpawnFailed || a.Proxy is not { Exists: true }))
        {
            Plugin.Game.RecordMechanicMistake(40266, "Native actor setup incomplete; leave and retry.");
            world.Events.Clear();
            Cleanup();
            Completed = false;
            return;
        }
        boss.Cast(FruUltimateRelativityActions.Relativity);
    }

    private void AssignStatuses()
    {
        foreach (var role in state.Roles)
        {
            var member = world.Party.Get(role)!;
            member.AddStatus(state.IsIce(role) ? (ushort)2462 : (ushort)2455, state.IsIce(role) ? 21f : state.FireGroup(role) * 10f + 1f);
            member.AddStatus(2464, state.ReturnOrder(role) == 1 ? 16f : 26f);
            member.AddStatus(state.FinalStatus(role), 43f);
            if (state.Marker(role) is { } marker) world.SoftMarkers.Set(marker, member);
        }
        for (var i = 0; i < 3; i++) world.Party.Get(state.StackTargets[i])!.AddStatus(2454, 11f + i * 10f);
    }

    private void SpawnHourglasses()
    {
        for (var i = 0; i < 8; i++)
        {
            var d = state.Direction(i);
            glasses[i] = Spawn(BNpcBaseId.DelightsHourglass, BNpcNameId.DelightsHourglass, state.Hourglass(i), true, MathF.Atan2(-d.X, -d.Z));
            world.Map.AddEffect(0x00020001, (byte)(0x1A + i));
        }
    }

    private int Wave(int clock) => clock is 0 or 3 or 5 ? 1 : clock is 1 or 4 or 7 ? 2 : 3;
    private void ShowTethers()
    {
        for (var i = 0; i < 8; i++)
            if (Wave(i) != 2) tethers.Add(world.Tether(glasses[i], boss, Wave(i) == 1 ? (ushort)134 : (ushort)133, 5.6f));
    }
    private void SpeedEffects()
    {
        for (var i = 0; i < 8; i++)
            if (Wave(i) != 2) helpers[i].Cast(Wave(i) == 1 ? 40294u : 40295u, glasses[i], animationLock: 1.1f);
        foreach (var tether in tethers) tether.Despawn();
        tethers.Clear();
    }
    private void RotateHourglasses(int wave)
    {
        for (var i = 0; i < 8; i++)
            if (Wave(i) == wave) glasses[i].AddStatusParam(2970, state.Clockwise(i) ? 0x15C : 0x10D, 4f);
    }
    private void CastHourglasses(int wave)
    {
        for (var i = 0; i < 8; i++) if (Wave(i) == wave) glasses[i].Cast(FruUltimateRelativityActions.Hourglass);
    }
    private void ScheduleLasers(int wave, float first)
    {
        world.Events.Add(first, () => FireLasers(wave, 0));
        for (var shot = 1; shot < 10; shot++)
        {
            var n = shot;
            world.Events.Add(first + 2.04f + (shot - 1) * 1.07f, () => FireLasers(wave, n));
        }
    }
    private void FireLasers(int wave, int shot)
    {
        for (var i = 0; i < 8; i++)
        {
            if (Wave(i) != wave) continue;
            if (glasses[i].PacketSpawnFailed || glasses[i].Proxy is not { Exists: true })
            {
                Plugin.Game.RecordMechanicMistake(40291, "Hourglass actor unavailable.");
                return;
            }
            var origin = state.Hourglass(i);
            if (shot == 0)
            {
                var nearest = world.Party.ActiveMembers().MinBy(m => Vector3.DistanceSquared(m.Position, origin));
                if (nearest is null) return;
                var d = nearest.Position - origin;
                laserSnapshots[i] = (((ISimPartyMember)nearest).Role, MathF.Atan2(d.X, d.Z));
            }
            if (!laserSnapshots.TryGetValue(i, out var snapshot)) continue;
            var rotation = snapshot.Heading + shot * (state.Clockwise(i) ? -1f : 1f) * MathF.PI / 12f;
            lasers[i].SetPosition(new Placement(origin, rotation));
            if (shot == 0) lasers[i].Cast(FruUltimateRelativityActions.LaserFirst, world.Party.Get(snapshot.Target));
            else lasers[i].Cast(FruUltimateRelativityActions.LaserRepeat);
        }
        resolutions.Add((world.Events.Elapsed, shot == 0 ? 40235u : 40292u));
    }
    private void ResolveFireAndStack(int order)
    {
        foreach (var role in state.Roles.Where(r => state.FireOrder(r) == order))
        {
            var member = world.Party.Get(role)!;
            helpers[(int)role].SetPosition(member.Position);
            helpers[(int)role].Cast(FruUltimateRelativityActions.Fire, member);
            member.RemoveStatus(2455);
        }
        if (order == 2)
        {
            var ice = world.Party.Get(state.Roles.Single(state.IsIce))!;
            iceHelper.SetPosition(ice.Position);
            iceHelper.Cast(FruUltimateRelativityActions.Ice, ice);
            ice.RemoveStatus(2462);
            resolutions.Add((world.Events.Elapsed, 40279));
        }
        var target = world.Party.Get(state.StackTargets[order - 1])!;
        stackHelper.SetPosition(target.Position);
        stackHelper.Cast(FruUltimateRelativityActions.Stack, target);
        target.RemoveStatus(2454);
        resolutions.Add((world.Events.Elapsed, 40276));
        resolutions.Add((world.Events.Elapsed, 40277));
    }
    private void RecordReturn(int order)
    {
        foreach (var role in state.Roles.Where(r => state.ReturnOrder(r) == order))
        {
            var member = world.Party.Get(role)!;
            returns[role] = member.Position;
            member.RemoveStatus(2464);
            member.AddStatus(2452, order == 1 ? 24f : 14f);
        }
    }
    private void Stun()
    {
        foreach (var member in world.Party.ActiveMembers())
        {
            member.StopMoving();
            member.AddStatus(4163, 4f);
        }
    }
    private void Rewind()
    {
        foreach (var (role, position) in returns)
            if (world.Party.Get(role) is { } member && member.IsAlive()) member.CarryTo(position);
    }
    private void FinalResolution()
    {
        foreach (var role in state.Roles)
        {
            var member = world.Party.Get(role)!;
            helpers[(int)role].SetPosition(member.Position);
            helpers[(int)role].Cast(state.FinalStatus(role) switch
            {
                2460 => FruUltimateRelativityActions.Eruption,
                2461 => FruUltimateRelativityActions.Water,
                _ => FruUltimateRelativityActions.Eye,
            }, member);
            member.RemoveStatus(state.FinalStatus(role));
            member.RemoveStatus(2452);
        }
        resolutions.Add((world.Events.Elapsed, 40274));
        resolutions.Add((world.Events.Elapsed, 40271));
        resolutions.Add((world.Events.Elapsed, 40278));
        world.SoftMarkers.Clear();
    }
    private void ResolveShell()
    {
        boss.Cast(FruUltimateRelativityActions.Shell, world.Party.Get(state.ShellTarget));
        resolutions.Add((world.Events.Elapsed, 40287));
    }
    private void Cleanup()
    {
        Completed = world.Party.ActiveMembers().Count(m => m.IsAlive()) == 8;
        world.SoftMarkers.Clear();
        foreach (var member in world.Party.ActiveMembers()) foreach (var id in PlayerStatuses) member.RemoveStatus(id);
        foreach (var actor in glasses.Concat(lasers).Concat(helpers).Append(stackHelper).Append(iceHelper).Append(boss)) actor?.Despawn();
        foreach (var tether in tethers) tether.Despawn();
        tethers.Clear();
        for (byte i = 0x1A; i <= 0x21; i++) world.Map.AddEffect(0x00040004, i);
    }
}
