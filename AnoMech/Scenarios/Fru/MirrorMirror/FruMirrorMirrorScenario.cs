using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Distribution;
using Upstream = AnoMech.Scenarios.Fru.Upstream;

namespace AnoMech.Scenarios.Fru.MirrorMirror;

public sealed class FruMirrorMirrorScenario : IScenario
{
    public string Name => "Mirror, Mirror";
    public IPhase Phase => FruZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruMirrorMirrorAi()];
    private readonly FruMirrorMirrorStateOverrides settings = new();
    public object SettingsOverrides => settings;
    public FruMirrorMirrorState? LastState { get; private set; }
    private SimWorld world = null!;
    private FruMirrorMirrorState state = null!;
    private SimEnemy? boss, blue;
    private readonly SimEnemy?[] red = new SimEnemy?[2];
    private readonly SimEnemy?[] helpers = new SimEnemy?[8];

    public void DrawSettings()
    {
        var blueIndex = (settings.Blue ?? -1) + 1;
        if (ImGui.Combo("Blue mirror", ref blueIndex, "Random\0N\0NE\0E\0SE\0S\0SW\0W\0NW\0"))
        {
            settings.Blue = blueIndex == 0 ? null : blueIndex - 1;
            settings.Red = null;
        }
        var markers = settings.ShowMirrorMarkers;
        if (ImGui.Checkbox("Mirror marker fallback", ref markers)) settings.ShowMirrorMarkers = markers;
        var pairs = settings.Pairs is { } p ? p ? 1 : 2 : 0;
        if (ImGui.Combo("Banish III", ref pairs, "Random\0Pairs\0Spreads\0")) settings.Pairs = pairs == 0 ? null : pairs == 1;
    }
    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new(world.Rng, settings);
        LastState = state;
        boss = blue = null;
        Array.Clear(red);
        Array.Clear(helpers);
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(1f, BeginMirrorMirror);
        world.Events.Add(6.05f, () =>
        {
            world.Map.AddEffect(0x00020001, FruMirrorMirrorState.MirrorSlot(state.Blue));
            world.Map.AddEffect(0x02000100, FruMirrorMirrorState.MirrorSlot(state.Red));
            world.Map.AddEffect(0x02000100, FruMirrorMirrorState.MirrorSlot(state.OtherRed));
            if (settings.ShowMirrorMarkers)
            {
                if (blue is { } blueSource) world.SoftMarkers.Set(Sign.Bind1, blueSource);
                if (red[0] is { } redA) world.SoftMarkers.Set(Sign.Attack1, redA);
                if (red[1] is { } redB) world.SoftMarkers.Set(Sign.Attack2, redB);
            }
            boss?.SetTarget(world.Party.Get(PartyRole.MainTank));
        });
        world.Events.Add(12.05f, () => { boss?.Follow(null); boss?.StopMoving(); });
        world.Events.Add(12.14f, () => boss?.Cast(Upstream.FruActions.ScytheKick));
        world.Events.Add(18.11f, () =>
        {
            foreach (var octant in new[] { state.Red, state.OtherRed }) world.Map.AddEffect(0x08000001, FruMirrorMirrorState.MirrorSlot(octant));
        });
        world.Events.Add(20.2f, () => world.Map.AddEffect(0x00080004, FruMirrorMirrorState.MirrorSlot(state.Blue)));
        world.Events.Add(30.2f, () =>
        {
            foreach (var octant in new[] { state.Red, state.OtherRed }) world.Map.AddEffect(0x00080004, FruMirrorMirrorState.MirrorSlot(octant));
        });
        world.Events.Add(18.2f, () =>
        {
            blue?.Cast(Donut(40204));
            foreach (var mirror in red) mirror?.Cast(Donut(40205));
        });
        world.Events.Add(18.82f, () => { Proteans(boss, 0); Proteans(blue, 4); });
        world.Events.Add(19f, () => boss?.SetTarget(world.Party.Get(PartyRole.MainTank)));
        world.Events.Add(26f, () => { boss?.Follow(null); boss?.StopMoving(); });
        world.Events.Add(28.8f, () => { for (var i = 0; i < red.Length; i++) Proteans(red[i], i * 4); });
        world.Events.Add(29.3f, () => boss?.Cast(state.Pairs ? 40220u : 40221u, animationLock: 3.1f));
        world.Events.Add(34.36f, Banish);
        world.Events.Add(37f, () =>
        {
            foreach (var mirror in red.Append(blue)) mirror?.Despawn();
        });
        if (selectedAi is not null) new FruMirrorMirrorAi().Run(state, world, () => boss);
    }
    private void BeginMirrorMirror()
    {
        var actors = helpers.Concat(red).Append(blue).Append(boss).ToArray();
        if (!NativeActorReadiness.Check(actors, "FRU Mirror Mirror / opening cast"))
        {
            Plugin.Game.RecordMechanicMistake(40179, "Native mirror setup incomplete; leave and retry.");
            world.Events.Clear();
            foreach (var actor in actors) actor?.Despawn();
            return;
        }
        boss?.Cast(40179, animationLock: 2.1f);
    }
    private static EnemyAction Donut(uint id) => new(id)
    {
        Area = new() { Size = 4f }, Effects = [Damage(Severity.Lethal)],
        Cast = new() { AnimationLock = 2.1f },
    };
    private void SpawnActors()
    {
        boss = world.SpawnEnemy(new EnemySpawnConfig(Upstream.FruConstants.BNpcBaseId.UsurperP2,
            Upstream.FruConstants.BNpcNameId.Usurper, Level: 100, Targetable: true,
            Placement: new Placement(Vector3.Zero, 0), InitialModeAttributeFlags: 0xF0));
        blue = SpawnMirror(state.Blue);
        red[0] = SpawnMirror(state.Red);
        red[1] = SpawnMirror(state.OtherRed);
        for (var i = 0; i < helpers.Length; i++) helpers[i] = world.SpawnEnemy(new EnemySpawnConfig(
            Upstream.FruConstants.BNpcBaseId.Helper, Upstream.FruConstants.BNpcNameId.Usurper,
            Level: 1, Visibility: SpawnVisibility.InvisibleHelper, EnemyList: EnemyListMode.Never));
    }
    private SimEnemy? SpawnMirror(int octant) => world.SpawnEnemy(new EnemySpawnConfig(
        Upstream.FruConstants.BNpcBaseId.FrozenMirror, Upstream.FruConstants.BNpcNameId.FrozenMirror,
        Level: 100, Visibility: SpawnVisibility.InvisibleHelper, EnemyList: EnemyListMode.Never,
        Placement: new Placement(FruMirrorMirrorState.Point(octant, 20f), 0).Face(Vector3.Zero),
        InitialModeAttributeFlags: 0xFC));
    private void Proteans(SimEnemy? source, int offset)
    {
        if (source is null) return;
        var baits = world.Party.ActiveMembers().OrderBy(m => Vector3.DistanceSquared(source.Position, m.Position)).Take(4).ToArray();
        for (var i = 0; i < baits.Length; i++)
        {
            helpers[i + offset]?.SetPosition(new Placement(source.Position, 0).Face(baits[i].Position));
            helpers[i + offset]?.Cast(Upstream.FruActions.HouseOfLightDiamondDust);
        }
    }
    private void Banish()
    {
        var i = 0;
        foreach (var member in world.Party.ActiveMembers().Where(m => !state.Pairs || m is ISimPartyMember p && !p.Role.IsDps()))
        {
            helpers[i]?.Cast(state.Pairs ? Upstream.FruActions.BanishPairs : Upstream.FruActions.BanishSpread, member);
            i++;
        }
    }
}
