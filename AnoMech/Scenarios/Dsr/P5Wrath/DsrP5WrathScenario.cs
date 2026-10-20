using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Scenarios.Dsr.DsrP5Constants;

namespace AnoMech.Scenarios.Dsr.P5Wrath;

public sealed class DsrP5WrathScenario : IScenario
{
    public string Name => "Wrath of the Heavens (Elemental)";
    public IPhase Phase => DsrZone.P5;
    public bool IsIncomplete => true;
    public bool EnablePhaseBgm => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP5WrathAi()];
    private readonly DsrP5WrathStateOverrides overrides = new();
    public object SettingsOverrides => overrides;
    public DsrP5WrathState? LastState { get; private set; }
    public bool Completed { get; private set; }
    public const float Wrath = 29.10f;
    public const float CauterizeCast = 52.15f;
    public const float ThunderstruckEnds = 58.54f;
    public static readonly float[] EruptionCasts = [51.27f, 52.78f, 54.30f, 55.81f];
    public static readonly float[] LiquidHeavenDrops = [51.31f, 52.47f, 53.63f, 54.79f, 55.95f];
    private SimWorld world = null!;
    private DsrP5WrathState state = null!;
    private SimEnemy boss = null!;
    private SimEnemy dragon = null!;
    private SimEnemy dark = null!;
    private SimEnemy white = null!;
    private SimEnemy leap = null!;
    private SimEnemy mage = null!;
    private SimEnemy warrior = null!;
    private readonly SimEnemy[] dragoons = new SimEnemy[2];
    private readonly List<SimEnemy> actors = [];
    private readonly List<SimTether> tethers = [];
    private readonly List<(Vector3 At, SimEventObject? Object)> twisters = [];
    private readonly Queue<SimEnemy> eruptionHelpers = [];
    private readonly List<SimEnemy> mercyHelpers = [];
    private readonly Queue<SimEnemy> liquidHelpers = [];
    private bool twistersArmed;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam; state = LastState = new(world.Rng, overrides);
        actors.Clear(); tethers.Clear(); twisters.Clear(); eruptionHelpers.Clear(); mercyHelpers.Clear(); liquidHelpers.Clear();
        twistersArmed = false; Completed = false;
        world.Events.Add(0, SpawnOpening);
        world.Events.Add(4.27f, () => boss.Cast(ActionId.Incarnation));
        world.Events.Add(4.36f, () => { boss.SetTargetable(true); boss.SetTarget(world.Party.Get(PartyRole.MainTank), false); });
        world.Events.Add(11.41f, () => boss.Cast(ActionId.DragonsEye));
        world.Events.Add(14.20f, () => { dragon.FadeOut(); dark.FadeOut(); white.FadeOut(); });
        world.Events.Add(18.55f, Auto);
        world.Events.Add(21.68f, Auto);
        world.Events.Add(25.97f, () => boss.Cast(ActionId.KnightsOfTheRound));
        world.Events.Add(29.10f, () => boss.Cast(ActionId.WrathOfTheHeavens));
        world.Events.Add(36.17f, () => boss.SetTargetable(false));
        world.Events.Add(37.40f, SpawnTrio);
        world.Events.Add(38.89f, MarkPlayers);
        world.Events.Add(38.98f, () => dragon.Cast(Hit(ActionId.TwistingDive, Severity.Lethal)));
        world.Events.Add(42.56f, MarkLightning);
        world.Events.Add(43.60f, SpawnMageAndWarrior);
        world.Events.Add(44.97f, SnapshotTwisters);
        world.Events.Add(45.00f, () => world.Party.Get(state.Divebomb)?.AttachLockonVfx(LockonId.Divebomb));
        world.Events.Add(45.06f, SpiralPierce);
        world.Events.Add(45.10f, SkywardLeap);
        world.Events.Add(45.70f, SpawnTwisters);
        world.Events.Add(46.30f, () => { foreach (var t in twisters) t.Object?.EObjSetState(1, 0); });
        world.Events.Add(46.70f, () => twistersArmed = true);
        world.Events.Add(47.20f, () => { boss.Cast(ActionId.AscalonsMercyRevealed); SpawnHelpers(); });
        world.Events.Add(47.60f, () => { leap.FadeOut(); foreach (var actor in dragoons) actor.FadeOut(); });
        world.Events.Add(50.90f, ClearTwisters);
        world.Events.Add(51.27f, Mercy);
        foreach (var at in EruptionCasts) world.Events.Add(at, Eruption);
        foreach (var at in LiquidHeavenDrops) world.Events.Add(at, LiquidHeaven);
        world.Events.Add(52.15f, Cauterize);
        world.Events.Add(53.36f, () => warrior.Cast(Hit(ActionId.EmptyDimension, Severity.Lethal) with { Area = new() { Size = 6 } }));
        world.Events.Add(58.54f, ResolveLightning);
        world.Events.Add(59.35f, () => boss.SetTargetable(true));
        world.Events.Add(59.43f, () => boss.Cast(Hit(ActionId.AncientQuaga)));
        world.Events.Add(62.10f, () => { dragon.FadeOut(); dark.FadeOut(); white.FadeOut(); mage.FadeOut(); warrior.FadeOut(); });
        world.Events.Add(69.79f, Auto);
        world.Events.Add(71.84f, () => boss.Cast(new EnemyAction(ActionId.HeavenlyHeel)
        {
            Effects = [Damage(Severity.TankBuster), ApplyStatus(StatusId.SlashingResistanceDown, 15)], Timing = new() { DamageDelay = 0 },
        }, world.Party.Get(PartyRole.MainTank)));
        foreach (var at in new[] { 78.94f, 80.58f, 82.24f }) world.Events.Add(at, Might);
        world.Events.Add(86.20f, () => boss.Cast(ActionId.KnightsOfTheRound));
        world.Events.Add(88.10f, () => { ClearTwisters(); foreach (var actor in actors) actor.Despawn(); Completed = true; });
        if (selectedAi is { } index) ((IScenarioAi<DsrP5WrathState>)AiStrats[index]).Run(state, world);
    }

    public void Tick(float delta, float elapsed)
    {
        if (!twistersArmed) return;
        foreach (var twister in twisters.ToArray())
        {
            var touched = world.Party.Find.InsideCircle(twister.At, 1.5f).ToArray();
            if (touched.Length == 0) continue;
            var helper = Helper(twister.At);
            helper.Cast(Hit(ActionId.Twister, Severity.Lethal) with { Area = new() { AdjustTargets = (_, _) => touched } });
            twister.Object?.Despawn(); twisters.Remove(twister);
        }
    }

    private SimEnemy Spawn(uint id, uint name, Vector3 position, bool listed = false)
    {
        var actor = world.SpawnEnemy(new(id, name, Level: 90, Targetable: false,
            EnemyList: listed ? EnemyListMode.Always : EnemyListMode.Never,
            Placement: new(position, MathF.Atan2(-position.X, -position.Z)))) ?? throw new InvalidOperationException("DSR actor unavailable");
        actors.Add(actor); return actor;
    }
    private SimEnemy Helper(Vector3 position)
    {
        var actor = world.SpawnEnemy(new(9020, Level: 90, Visibility: SpawnVisibility.InvisibleHelper,
            EnemyList: EnemyListMode.Never, Placement: new(position, 0))) ?? throw new InvalidOperationException("DSR helper unavailable");
        actors.Add(actor); return actor;
    }
    private static EnemyAction Hit(uint id, Severity? severity = null) => new(id)
    { Effects = [Damage(severity)], Timing = new() { DamageDelay = 0 } };
    private static EnemyAction Instant(uint id, params IEnemyActionEffect[] effects) => new(id)
    { Cast = new() { CastSecondsOverride = 0 }, Effects = effects, Timing = new() { DamageDelay = 0 } };

    private void SpawnOpening()
    {
        boss = Spawn(BNpcBaseId.DarkThordan, BNpcNameId.Thordan, Vector3.Zero, true); boss.SetRotation(0);
        dark = Spawn(BNpcBaseId.Darkscale, BNpcNameId.Darkscale, new(-15, 0, 0));
        white = Spawn(BNpcBaseId.Vidofnir, BNpcNameId.Vidofnir, new(11, 0, 0));
        dragon = Spawn(BNpcBaseId.Vedrfolnir, BNpcNameId.Vedrfolnir, new(0, 0, 14));
    }
    private void Auto() => boss.Cast(Instant(ActionId.ThordanAttack, OnTarget(Damage()), OnOthers(Damage(Severity.Lethal))) with
        { Area = new() { Size = MathF.PI / 4 } }, world.Party.Get(PartyRole.MainTank));
    private void SpawnTrio()
    {
        dragon = Spawn(BNpcBaseId.Vedrfolnir, BNpcNameId.Vedrfolnir, state.DragonSpot);
        dragoons[0] = Spawn(BNpcBaseId.Ignasse, BNpcNameId.Ignasse, state.DragoonSpot(0));
        dragoons[1] = Spawn(BNpcBaseId.Vellguine, BNpcNameId.Vellguine, state.DragoonSpot(1));
        leap = Spawn(BNpcBaseId.Paulecrain, BNpcNameId.Paulecrain, AtBearing(state.DragonBearing + 180, 23));
        dark = Spawn(BNpcBaseId.DarkscaleDiver, BNpcNameId.Darkscale, AtBearing(state.DragonBearing + 90, 23));
        white = Spawn(BNpcBaseId.VidofnirDiver, BNpcNameId.Vidofnir, AtBearing(state.DragonBearing - 90, 23));
    }
    private void MarkPlayers()
    {
        for (var i = 0; i < 2; i++) tethers.Add(world.Tether(dragoons[i], world.Party.Get(state.Tethered[i])!, TetherId.SpiralPierce));
        world.Party.Get(state.Defamation)?.AttachLockonVfx(LockonId.WrathDefamation);
    }
    private void MarkLightning()
    {
        foreach (var role in state.Lightning) world.Party.Get(role)?.AddStatus(StatusId.Thunderstruck, ThunderstruckEnds - world.Events.Elapsed);
    }
    private void SpawnMageAndWarrior()
    {
        mage = Spawn(BNpcBaseId.Charibert, BNpcNameId.Charibert, state.MageSpot);
        warrior = Spawn(BNpcBaseId.Grinnaux, BNpcNameId.Grinnaux, state.WarriorSpot);
    }
    private void SnapshotTwisters()
    {
        foreach (var member in world.Party.ActiveMembers()) twisters.Add((member.Position, null));
    }
    private void SpawnTwisters()
    {
        for (var i = 0; i < twisters.Count; i++) twisters[i] = (twisters[i].At, world.SpawnEventObject(new() { EObjId = EObjId.Twister, Placement = new(twisters[i].At, 0) }));
    }
    private void ClearTwisters()
    {
        twistersArmed = false; foreach (var item in twisters) item.Object?.Despawn(); twisters.Clear();
    }
    private void SpiralPierce()
    {
        foreach (var tether in tethers) tether.Despawn();
        for (var i = 0; i < 2; i++) dragoons[i].Cast(Instant(ActionId.SpiralPierce,
            OnTarget(Damage()), OnOthers(Damage(Severity.Lethal)), ApplyStatus(StatusId.MagicVulnerabilityUp, 11.96f)), world.Party.Get(state.Tethered[i]));
    }
    private void SkywardLeap() => leap.Cast(Instant(ActionId.WrathSkywardLeap,
        Damage(VulnerableTo(StatusId.MagicVulnerabilityUp)), ApplyStatus(StatusId.MagicVulnerabilityUp, 11.96f)), world.Party.Get(state.Defamation));
    private void SpawnHelpers()
    {
        for (var i = 0; i < 8; i++) mercyHelpers.Add(Helper(boss.Position));
        for (var i = 0; i < 4; i++) eruptionHelpers.Enqueue(Helper(Vector3.Zero));
        for (var i = 0; i < 5; i++) liquidHelpers.Enqueue(Helper(Vector3.Zero));
    }
    private void Mercy()
    {
        var hitCounts = new Dictionary<SimCharacter, int>();
        foreach (var (member, i) in world.Party.ActiveMembers().Select((m, i) => (m, i)).ToArray())
        {
            var helper = mercyHelpers[i]; helper.SetRotation(helper.Placement().Face(member.Position).Rotation);
            helper.Cast(Instant(ActionId.AscalonsMercyRevealedCone, Damage(), Resolve(ctx =>
            {
                foreach (var hit in ctx.Hits)
                {
                    hitCounts[hit] = hitCounts.GetValueOrDefault(hit) + 1;
                    if (hitCounts[hit] > 1) ctx.Kill(hit, "overlapped proteans");
                }
            })) with
                { Area = new() { Size = 7.5f * MathF.PI / 180 } });
        }
    }
    private void Eruption()
    {
        var at = world.Party.Get(state.Eruptions)!.Position;
        var helper = eruptionHelpers.Dequeue(); helper.SetPosition(at);
        helper.Cast(Hit(ActionId.AltarFlareEruption, Severity.Lethal), at);
    }
    private void LiquidHeaven()
    {
        var at = world.Party.Get(state.LiquidHeaven)!.Position;
        var helper = liquidHelpers.Dequeue(); helper.SetPosition(at);
        helper.Cast(Instant(ActionId.LiquidHeaven, Damage(VulnerableTo(StatusId.MagicVulnerabilityUp))));
        world.Events.Add(.8f, () =>
        {
            var obj = world.SpawnEventObject(new() { EObjId = EObjId.LiquidHeaven, Placement = new(at, 0) });
            obj?.EObjSetState(1, 0);
            for (var delay = 1f; delay < 10; delay++) world.Events.Add(delay, () => helper.Cast(Instant(ActionId.LiquidHeaven, Damage(VulnerableTo(StatusId.MagicVulnerabilityUp)))));
            world.Events.Add(10f, () => obj?.EObjSetState(0, 0));
            world.Events.Add(12.1f, () => { obj?.Despawn(); helper.Despawn(); });
        });
    }
    private void Cauterize()
    {
        var aim = world.Party.Get(state.Divebomb)!.Position;
        foreach (var (actor, action) in new[] { (dark, ActionId.CauterizeDarkscale), (white, ActionId.CauterizeVidofnir) })
        { actor.SetRotation(actor.Placement().Face(aim).Rotation); actor.Cast(Hit(action, Severity.Lethal)); }
    }
    private void ResolveLightning()
    {
        var hitCounts = new Dictionary<SimCharacter, int>();
        foreach (var role in state.Lightning)
        {
            var struck = world.Party.Get(role)!; var helper = Helper(struck.Position);
            helper.Cast(Instant(ActionId.ChainLightningHit, Damage(), Resolve(ctx =>
            {
                foreach (var hit in ctx.Hits)
                {
                    hitCounts[hit] = hitCounts.GetValueOrDefault(hit) + 1;
                    if (hitCounts[hit] > 1) ctx.Kill(hit, "overlapped lightning bursts");
                }
            })));
            struck.RemoveStatus(StatusId.Thunderstruck);
        }
    }
    private void Might() => boss.Cast(Instant(ActionId.AscalonsMight,
        Damage(VulnerableTo(StatusId.SlashingResistanceDown, 1), Severity.TankBuster with { ForceMitigation = true })), world.Party.Get(PartyRole.MainTank));
}
