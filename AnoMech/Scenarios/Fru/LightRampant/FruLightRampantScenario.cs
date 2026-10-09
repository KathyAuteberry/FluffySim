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
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Fru.LightRampant;

public sealed class FruLightRampantScenario : IScenario
{
    public string Name => "Light Rampant (provisional)";
    public IPhase Phase => FruZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruLightRampantAi()];
    public object SettingsOverrides => settings.Overrides;
    public void DrawSettings() => settings.Draw();
    private readonly FruLightRampantSettingsWindow settings = new();
    public FruLightRampantState? LastState { get; private set; }
    public bool Completed { get; private set; }
    private SimWorld world = null!;
    private FruLightRampantState state = null!;
    private SimEnemy boss = null!;
    private readonly List<SimEnemy> helpers = [];
    private readonly Dictionary<int, SimEnemy> orbs = [];
    private readonly List<SimTether> chains = [];
    private readonly List<(Vector3 Position, float From, float Until)> puddles = [];
    private readonly List<SimEventObject> puddleVisuals = [];
    private bool chainsActive;
    private bool chainFailed;

    public static IReadOnlyList<Vector3> OuterTowers { get; } =
    [
        new(-13.8564f, 0f, -8f), new(0f, 0f, 16f), new(13.8564f, 0f, -8f),
        new(-13.8564f, 0f, 8f), new(0f, 0f, -16f), new(13.8564f, 0f, 8f),
    ];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new(world.Rng, settings.Overrides);
        LastState = state;
        Completed = false;
        chainsActive = chainFailed = false;
        helpers.Clear(); orbs.Clear(); chains.Clear(); puddles.Clear(); puddleVisuals.Clear();
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(3f, () => boss.Cast(FruActions.LightRampant));
        world.Events.Add(8.81f, Assign);
        world.Events.Add(11.03f, ShowOuterTowers);
        world.Events.Add(11.12f, () => boss.PlayActionTimeline(0x11E5));
        world.Events.Add(13.16f, () => boss.SetModelHidden(true));
        world.Events.Add(15.89f, DropPuddles);
        world.Events.Add(17.45f, DropPuddles);
        world.Events.Add(18.80f, ActivateChains);
        world.Events.Add(19.01f, DropPuddles);
        world.Events.Add(19.16f, ResolveOuterTowers);
        world.Events.Add(19.57f, () => ShowOrbs(first: true));
        world.Events.Add(20.57f, DropPuddles);
        world.Events.Add(22.13f, DropPuddles);
        world.Events.Add(22.33f, () => CastOrbs(first: true));
        world.Events.Add(22.47f, () => ShowOrbs(first: false));
        world.Events.Add(24.87f, ResolvePowerfulLight);
        world.Events.Add(25.36f, () => CastOrbs(first: false));
        world.Events.Add(25.71f, () =>
        {
            world.Map.AddEffect(0x00020001, 0x15);
            world.Map.AddEffect(0x00010004, 0x16);
        });
        world.Events.Add(27.80f, ClearChains);
        world.Events.Add(29.02f, () => HideOrbs(first: true));
        world.Events.Add(29.81f, () => boss.PlayActionTimeline(0x1E43));
        world.Events.Add(29.87f, () => boss.SetModelHidden(false));
        world.Events.Add(31.96f, () => boss.Cast(state.Spread ? ActionId.BanishSpread : ActionId.BanishPairs));
        world.Events.Add(32.06f, () => HideOrbs(first: false));
        world.Events.Add(33.84f, ResolveCentralTower);
        world.Events.Add(37.04f, ResolveBanish);
        world.Events.Add(40.12f, () =>
        {
            boss.SetTargetable(true);
            boss.Cast(ActionId.HouseOfLight);
        });
        world.Events.Add(46.00f, ResolveHouseOfLight);
        world.Events.Add(49.00f, Cleanup);
        if (selectedAi is { } index) ((IScenarioAi<FruLightRampantState>)AiStrats[index]).Run(state, world);
    }

    private void SpawnActors()
    {
        boss = Spawn(BNpcBaseId.UsurperOfFrost, BNpcNameId.UsurperOfFrost, Vector3.Zero, true);
        for (var i = 0; i < 8; ++i) helpers.Add(Spawn(BNpcBaseId.Helper, BNpcNameId.UsurperOfFrost, Vector3.Zero, false));
    }

    private SimEnemy Spawn(uint baseId, uint nameId, Vector3 position, bool visible) =>
        world.SpawnEnemy(new EnemySpawnConfig(baseId, nameId, Level: 100, Targetable: visible,
            EnemyList: visible ? EnemyListMode.Always : EnemyListMode.Never,
            Visibility: visible ? SpawnVisibility.Visible : SpawnVisibility.InvisibleHelper, Placement: new(position, MathF.PI)))
        ?? throw new InvalidOperationException($"Could not spawn FRU actor {baseId}.");

    private void Assign()
    {
        state.BindChains(Enumerable.Range(0, 8).Select(i => ((PartyRole)i, world.Party.Get(i)!.Position)));
        for (var i = 0; i < 8; i++)
        {
            var member = world.Party.Get(i)!;
            member.AddStatus(StatusId.Lightsteeped, 36f, state.InitialLightsteeped[i], overrideStacks: true);
            if (state.PuddleTargets.Contains((PartyRole)i)) member.AttachLockonVfx(PuddleLockon, persistent: false);
            else member.AddStatus(StatusId.Chains, 10f);
        }
        foreach (var role in state.WeightTargets) world.Party.Get(role)!.AddStatus(StatusId.WeightOfLight, 16f);
        CreateChains(ChainsTether);
        boss.SetTargetable(false);
    }

    private void CreateChains(ushort id)
    {
        for (var i = 0; i < 6; i++)
            chains.Add(world.Tether(world.Party.Get(state.ChainRing[i]), world.Party.Get(state.ChainRing[(i + 1) % 6]), id));
    }

    private void ActivateChains()
    {
        foreach (var chain in chains) chain.Despawn();
        chains.Clear();
        foreach (var role in state.ChainRing)
        {
            world.Party.Get(role)!.RemoveStatus(StatusId.Chains);
            world.Party.Get(role)!.AddStatus(StatusId.Curse, 9f);
        }
        CreateChains(CurseTether);
        chainsActive = true;
    }

    private void ClearChains()
    {
        chainsActive = false;
        foreach (var chain in chains) chain.Despawn();
        chains.Clear();
        foreach (var role in state.ChainRing) world.Party.Get(role)!.RemoveStatus(StatusId.Curse);
    }

    private void ShowOuterTowers()
    {
        for (byte i = 9; i <= 14; ++i)
        {
            world.Map.AddEffect(0x00020001, i);
            world.Map.AddEffect(0x00010004, (byte)(i + 6));
        }
    }

    private void ResolveOuterTowers()
    {
        for (var i = 0; i < 6; i++)
        {
            ResolveTower(i, OuterTowers[i], 1);
            world.Map.AddEffect(0x00080004, (byte)(9 + i));
        }
    }

    private void ResolveTower(int helperIndex, Vector3 position, int required)
    {
        helpers[helperIndex].SetPosition(position);
        helpers[helperIndex].Cast(new EnemyAction(ActionId.BrightHunger)
        {
            Effects = [Damage(), new ResolveEffect(ctx =>
            {
                if (ctx.Hits.Count != required)
                {
                    Plugin.Game.RecordMechanicMistake(ActionId.BrightHunger, $"{ctx.Hits.Count}/{required} tower soakers");
                    helpers[7].Cast(ActionId.InescapableIllumination);
                }
                AddLightsteeped(ctx.Hits);
            })],
        });
    }

    private void DropPuddles()
    {
        for (var i = 0; i < 2; i++)
        {
            var member = world.Party.Get(state.PuddleTargets[i])!;
            var position = member.Position;
            helpers[i].SetPosition(position);
            helpers[i].Cast(FruActions.LuminousHammer, member);
            var from = world.Events.Elapsed + FruLightRampantRules.PuddleHazardActivationDelay;
            var until = world.Events.Elapsed + FruLightRampantRules.PuddleSpawnDelay + FruLightRampantRules.PuddleLifetime;
            puddles.Add((position, from, until));
            world.Events.Add(FruLightRampantRules.PuddleSpawnDelay, () =>
            {
                var visual = world.SpawnEventObject(new EventObjectSpawnConfig
                {
                    EObjId = PuddleEObjId, Placement = new(position, 0f), Lifetime = FruLightRampantRules.PuddleVisualLifetime,
                });
                if (visual is not null) puddleVisuals.Add(visual);
            });
        }
    }

    private void ShowOrbs(bool first)
    {
        for (var i = 0; i < 6; i++)
            if (IsFirstOrb(i) == first)
                orbs.Add(i, world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId.HolyLight, BNpcNameId.HolyLight,
                    Level: 100, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.Visible,
                    Placement: new(OuterTowers[i], 0f)))
                    ?? throw new InvalidOperationException("Could not spawn Holy Light orb."));
    }

    private bool IsFirstOrb(int index) => (index >= 3) == state.NorthOrbFirst;

    private void CastOrbs(bool first)
    {
        for (var i = 0; i < 6; i++)
            if (IsFirstOrb(i) == first) orbs[i].Cast(FruActions.Burst);
    }

    private void HideOrbs(bool first)
    {
        for (var i = 0; i < 6; i++)
            if (IsFirstOrb(i) == first && orbs.Remove(i, out var orb)) orb.Despawn();
    }

    private void ResolvePowerfulLight()
    {
        for (var i = 0; i < 2; i++)
        {
            var target = world.Party.Get(state.WeightTargets[i])!;
            helpers[i].SetPosition(target.Position);
            helpers[i].Cast(new EnemyAction(ActionId.PowerfulLight)
            {
                Effects = [Damage(), new ResolveEffect(ctx =>
                {
                    if (ctx.Hits.Count < 4)
                    {
                        Plugin.Game.RecordMechanicMistake(ActionId.PowerfulLight, $"{ctx.Hits.Count}/4 players in stack");
                        foreach (var hit in ctx.Hits) hit.AddStatus(StatusId.MarkOfMortality);
                    }
                    AddLightsteeped(ctx.Hits);
                })],
            }, target);
        }
    }

    private void ResolveCentralTower()
    {
        ResolveTower(6, Vector3.Zero, 4);
        world.Map.AddEffect(0x00080004, 0x15);
    }

    private void ResolveBanish()
    {
        var targets = Enumerable.Range(0, 8).Select(i => (PartyRole)i)
            .Where(r => state.Spread || r.IsDps() == state.PairTargetsDps).ToArray();
        for (var i = 0; i < targets.Length; i++)
        {
            var target = world.Party.Get(targets[i])!;
            helpers[i].SetPosition(target.Position);
            helpers[i].Cast(state.Spread ? FruActions.BanishSpread : FruActions.BanishPairs, target);
        }
    }

    private void ResolveHouseOfLight()
    {
        for (var i = 0; i < 8; i++)
        {
            var target = world.Party.Get(i)!;
            helpers[i].SetPosition(new Placement(Vector3.Zero, new Placement(Vector3.Zero, 0f).Face(target.Position).Rotation));
            helpers[i].Cast(FruActions.HouseOfLight with
            {
                Effects = FruActions.HouseOfLight.Effects.Append(new ResolveEffect(ctx => AddLightsteeped(ctx.Hits))).ToArray(),
            }, target);
        }
    }

    private void AddLightsteeped(IEnumerable<SimCharacter> members)
    {
        foreach (var member in members.Where(m => m.IsAlive()))
        {
            var count = (member.FindStatus(StatusId.Lightsteeped)?.Stacks ?? 0) + 1;
            member.AddStatus(StatusId.Lightsteeped, 36f);
            if (FruLightRampantRules.LightsteepedOverloads(count))
                world.Party.WipeAllPlayers(ActionId.Lightsteep, "Five Lightsteeped stacks");
        }
    }

    public void Tick(float delta, float elapsed)
    {
        if (Completed) return;
        if (chainsActive && !chainFailed && chains.Any(t =>
            t.A is { } a && t.B is { } b && FruLightRampantRules.ChainFails(a.Position, b.Position, a.IsAlive(), b.IsAlive())))
        {
            chainFailed = true;
            world.Party.WipeAllPlayers(ActionId.RefulgentFate, "Active chain broke");
        }
        foreach (var puddle in puddles.Where(p => world.Events.Elapsed >= p.From && world.Events.Elapsed < p.Until))
            foreach (var member in world.Party.Find.InsideCircle(puddle.Position, FruLightRampantRules.PuddleRadius))
                member.Die(ActionId.LuminousHammer, "Stayed in a persistent puddle");
    }

    private void Cleanup()
    {
        ClearChains();
        foreach (var actor in helpers.Concat(orbs.Values).Append(boss)) actor.Despawn();
        foreach (var visual in puddleVisuals) visual.Despawn();
        helpers.Clear(); orbs.Clear(); puddleVisuals.Clear(); puddles.Clear();
        for (var i = 0; i < 8; i++)
            foreach (var status in new[] { StatusId.Chains, StatusId.Curse, StatusId.WeightOfLight, StatusId.Lightsteeped, StatusId.MarkOfMortality })
                world.Party.Get(i)?.RemoveStatus(status);
        Completed = true;
    }

    private sealed class ResolveEffect(Action<EnemyActionContext> resolve) : IEnemyActionEffect
    {
        public void Apply(EnemyActionContext ctx, System.Collections.Generic.IReadOnlyList<SimCharacter> hits) => resolve(ctx);
    }
}
