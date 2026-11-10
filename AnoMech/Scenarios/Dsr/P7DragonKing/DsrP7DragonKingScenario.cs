using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static AnoMech.Scenarios.Dsr.DsrP7Constants;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

public sealed class DsrP7DragonKingScenario : IScenario
{
    public string DisplayNameOverride => "P7 Dragon-king Thordan";
    public string Name => "Dragon-king Thordan (Elemental)";
    public bool SupportsMultiplayer => true;
    public bool IsIncomplete => true;
    public bool EnablePhaseBgm => true;
    public DsrP7DragonKingState? LastState { get; private set; }
    public bool Completed { get; private set; }
    public IPhase Phase => DsrZone.P7;

    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP7DragonKingAi()];

    private readonly DsrP7DragonKingStateOverrides overrides = new();
    public object SettingsOverrides => overrides;
    public void DrawSettings()
    {
        Dalamud.Bindings.ImGui.ImGui.TextWrapped("All Akh Morns: H1/D1/D3 left, H2/D2/D4 right, tanks rear. Trinity baits: D1 → D2, D3 → D4, H1 → H2. Tanks Provoke after each pair of Trinities. Practice ends before Morn Afah's Edge resolves.");
        var element = overrides.ForcedElement is { } forced ? (int)forced + 1 : 0;
        if (Dalamud.Bindings.ImGui.ImGui.Combo("Ascalon", ref element, "Random\0Flames (out)\0Ice (in)\0"))
            overrides.ForcedElement = element == 0 ? null : (AscalonElement)(element - 1);
    }

    public const float StartAt = 0f;
    public const float AlternativeEnd = 2.91f;
    public const float Targetable = 11.95f;
    public static readonly float[] ExaflareCasts = [16.23f, 95.10f, 175.05f];
    public static readonly float[] ExaflareFirstHits = [23.12f, 101.96f, 181.91f];
    public static readonly float[] AkhMornCasts = [37.41f, 116.30f, 196.22f];
    public static readonly float[] AkhMornFirstHits = [44.10f, 122.98f, 202.90f];
    public static readonly int[] AkhMornFollowUps = [4, 5, 6];
    public const float AkhMornSecondHitDelay = 2.18f;
    public const float AkhMornHitInterval = 1.07f;
    public static readonly float[] GigaflareCasts = [61.86f, 141.81f];
    public static readonly float[][] GigaflareHits = [[70.82f, 74.83f, 78.84f], [150.81f, 154.77f, 158.78f]];
    private static readonly uint[] GigaflareActions = [ActionId.GigaflaresEdgeFirst, ActionId.GigaflaresEdgeSecond, ActionId.GigaflaresEdgeThird];
    public static readonly float[] AscalonResolves = [23.34f, 44.18f, 71.00f, 102.23f, 123.07f, 150.94f, 182.17f, 202.99f];
    private static readonly float[] EdgeCasts = [16.23f, 37.41f, 61.86f, 95.10f, 116.30f, 141.81f, 175.05f, 196.22f];
    private const float GlowAfterCast = 0.93f;
    public static readonly float[] TrinityHits =
        [31.40f, 35.45f, 55.85f, 59.91f, 89.09f, 93.14f, 110.29f, 114.34f, 135.80f, 139.85f, 169.03f, 173.09f, 190.25f, 194.26f, 216.90f, 220.95f];
    private const float TrinitySwingLead = 1.07f;
    public const float MornAfahsEdgeCast = 222.91f;
    public const float MornAfahsEdgeFirstTowers = 233.60f;
    public const float End = MornAfahsEdgeFirstTowers - 0.1f;

    private static readonly Vector3 ThordanHome = Vector3.Zero;
    private static float AscalonRadius => Natives.Data.Action(ActionId.FlamesOfAscalon)?.EffectRange ?? 8;
    private static float AkhMornRadius => Natives.Data.Action(ActionId.AkhMornsEdgePartyFirst)?.EffectRange ?? 4;
    // UNVERIFIED: the lethal proximity threshold; distance-based damage is simplified for practice.
    public const float GigaflareLethalWithin = 18f;
    private static float TrinityRadius => Natives.Data.Action(ActionId.TrinityNearest)?.EffectRange ?? 3;
    private const float TrinityResistanceSeconds = 37f;
    private const float TrinityVulnSeconds = 60f;
    // UNVERIFIED: the third same-element Trinity is treated as lethal.
    private const int TrinityLethalStacks = 2;
    private const float EffectCarrierLinger = 3f;
    private const int HelperPoolSize = 32;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DsrP7DragonKingState state = null!;
    private SimEnemy? thordan;
    public PartyRole AggroTank => LastState?.AggroTank ?? PartyRole.MainTank;
    private readonly List<SimEnemy> spawned = [];
    private readonly Queue<SimEnemy> helperPool = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        Completed = false;
        state = LastState = new DsrP7DragonKingState(world.Rng, overrides);
        akhMornCasters.Clear();
        gigaflareCasters.Clear();
        spawned.Clear();
        helperPool.Clear();
        thordan = null;
        world.ActionResolved += OnAction;
        world.OnDespawn(() => world.ActionResolved -= OnAction);

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DsrP7DragonKingState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(StartAt, () => world.EnforceArenaBoundary(21f, "Touched the arena wall"));
        world.Events.Add(StartAt, SpawnThordan);
        world.Events.Add(StartAt, FillHelperPool);
        world.Events.Add(AlternativeEnd, () => PlayFromHelper(ActionId.AlternativeEnd, new Vector3(0f, 0f, -12f)));
        world.Events.Add(Targetable, () =>
        {
            thordan?.SetTargetable(true);
            thordan?.SetTarget(party.Get(AggroTank), false);
            thordan?.KeepFacing(party.Get(AggroTank));
        });

        for (var i = 0; i < EdgeCasts.Length; i++)
        {
            var mechanic = i;
            world.Events.Add(EdgeCasts[i] + GlowAfterCast, () => Glow(state.Elements[mechanic]));
            world.Events.Add(AscalonResolves[i], () => Ascalon(state.Elements[mechanic]));
        }

        for (var cycle = 0; cycle < DsrP7DragonKingState.Cycles; cycle++)
        {
            var c = cycle;
            world.Events.Add(ExaflareCasts[c], () => CastExaflares(c));
            foreach (var spot in Enum.GetValues<EdgeSpot>())
                foreach (var explosion in state.ExaflareExplosions(c, spot))
                {
                    var (s, e) = (spot, explosion);
                    world.Events.Add(ExaflareFirstHits[c] + e.Delay, () => Exaflare(c, s, e));
                }

            world.Events.Add(AkhMornCasts[c], () => CastAkhMorn(c));
            world.Events.Add(AkhMornFirstHits[c], () => AkhMorn(c, 0));
            for (var hit = 1; hit <= AkhMornFollowUps[c]; hit++)
            {
                var h = hit;
                var at = AkhMornHitAt(c, h);
                world.Events.Add(at - 0.8f, () => PlayEffect(thordan, h == AkhMornFollowUps[c] ? ActionId.AkhMornsEdgeLastSwing : ActionId.AkhMornsEdgeSwing, 1.1f));
                world.Events.Add(at, () => AkhMorn(c, h));
            }

            if (c >= DsrP7DragonKingState.GigaflareCycles) continue;
            world.Events.Add(GigaflareCasts[c], () => CastGigaflares(c));
            for (var blast = 0; blast < 3; blast++)
            {
                var b = blast;
                world.Events.Add(GigaflareHits[c][b], () => Gigaflare(c, b));
            }
        }

        for (var i = 0; i < TrinityHits.Length; i++)
        {
            world.Events.Add(TrinityHits[i] - TrinitySwingLead, () => PlayEffect(thordan, ActionId.Trinity, 1.1f));
            world.Events.Add(TrinityHits[i], Trinity);
        }

        world.Events.Add(MornAfahsEdgeCast, CastMornAfahsEdge);
        world.Events.Add(End, DespawnAll);
    }

    public void RunInstanceEvents(SimWorld world)
    {
        world.Events.Add(3.84f, () => world.Map.DirectorUpdate(0x8000001F, 765));
        world.Events.Add(11.99f, () => world.Map.DirectorUpdate(0x80000016, 1, 0x50));
    }

    public static float AkhMornHitAt(int cycle, int hit)
        => hit == 0 ? AkhMornFirstHits[cycle] : AkhMornFirstHits[cycle] + AkhMornSecondHitDelay + (hit - 1) * AkhMornHitInterval;

    public static float AkhMornLastHit(int cycle) => AkhMornHitAt(cycle, AkhMornFollowUps[cycle]);

    private void SpawnThordan()
    {
        thordan = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.DragonKingThordan,
            NameId: BNpcNameId.DragonKingThordan,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(ThordanHome, MathF.PI)));
        if (thordan != null) spawned.Add(thordan);
    }

    private void Glow(AscalonElement element)
        => thordan?.AddStatusParam(StatusId.AscalonGlow, element == AscalonElement.Flames ? StatusId.FlamesGlow : StatusId.IceGlow);

    private void Ascalon(AscalonElement element)
    {
        thordan?.RemoveStatus(StatusId.AscalonGlow);
        var flames = element == AscalonElement.Flames;
        PlayFromHelper(flames ? ActionId.FlamesOfAscalon : ActionId.IceOfAscalon, ThordanHome);
        var hits = flames
            ? party.Find.InsideCircle(ThordanHome, AscalonRadius)
            : party.Find.OutsideCircle(ThordanHome, AscalonRadius);
        foreach (var hit in hits.Where(m => m.IsAlive()).ToList())
            hit.Die(flames ? ActionId.FlamesOfAscalon : ActionId.IceOfAscalon,
                flames ? "stood near the Dragon-king for Flames of Ascalon (red sword: get out)" : "stood away from the Dragon-king for Ice of Ascalon (blue sword: get in)");
    }

    private void CastExaflares(int cycle)
    {
        state.SnapshotExaflareFacing(cycle, thordan?.Rotation ?? MathF.PI);
        CastSelf(thordan, ActionId.ExaflaresEdge);
        foreach (var spot in Enum.GetValues<EdgeSpot>())
        {
            var at = ToWorld(state.ExaflarePoint(cycle, DsrP7DragonKingState.Spot(spot)));
            var rotation = state.ExaflareRotation(cycle, spot);
            if (TakeHelper(new Placement(at, rotation)) is not { } helper) continue;
            helper.Casting.NativeCast(ActionId.ExaflaresEdgeFirst, ActionType.Action, 0f, CastSeconds(ActionId.ExaflaresEdgeFirst), false, rotation: rotation, position: at);
            world.Events.Add(ExaflareFirstHits[cycle] - ExaflareCasts[cycle] + EffectCarrierLinger, () => ReturnHelper(helper));
        }
    }

    private void Exaflare(int cycle, EdgeSpot spot, Explosion explosion)
    {
        var action = explosion.Delay == 0f ? ActionId.ExaflaresEdgeFirst : ActionId.ExaflaresEdgeStep;
        // The scheduler builds explosions before the cast selects its facing.
        var at = ToWorld(state.ExaflarePoint(cycle, explosion.At));
        var radius = Natives.Data.Action(action)?.EffectRange ?? DsrP7DragonKingState.ExaflareRadius;
        PlayFromHelper(action, at, state.ExaflareRotation(cycle, spot));
        foreach (var hit in party.Find.InsideCircle(at, radius).Where(m => m.IsAlive()).ToList())
            hit.Die(action, $"stood in an Exaflare's Edge explosion ({Vector3.Distance(hit.Position, at):F1}y from its center, danger radius {radius:F1}y)");
    }

    private readonly Dictionary<EdgeSpot, SimEnemy> akhMornCasters = [];

    private void CastAkhMorn(int cycle)
    {
        state.SnapshotAkhMornFacing(cycle, thordan?.Rotation ?? MathF.PI);
        CastSelf(thordan, ActionId.AkhMornsEdge);
        foreach (var spot in Enum.GetValues<EdgeSpot>())
        {
            var at = ToWorld(state.AkhMornPoint(cycle, DsrP7DragonKingState.Spot(spot)));
            if (TakeHelper(new Placement(at, 0f)) is not { } helper) continue;
            helper.Casting.NativeCast(AkhMornFirstAction(spot), ActionType.Action, 0f, CastSeconds(AkhMornFirstAction(spot)), false, targetId: helper.GameObjectId, position: at);
            akhMornCasters[spot] = helper;
        }
        world.Events.Add(AkhMornLastHit(cycle) - AkhMornCasts[cycle] + EffectCarrierLinger, () =>
        {
            foreach (var helper in akhMornCasters.Values) ReturnHelper(helper);
            akhMornCasters.Clear();
        });
    }

    private static uint AkhMornFirstAction(EdgeSpot spot) => spot switch
    {
        EdgeSpot.Party => ActionId.AkhMornsEdgePartyFirst,
        EdgeSpot.SecondParty => ActionId.AkhMornsEdgeSecondPartyFirst,
        _ => ActionId.AkhMornsEdgeTanksFirst,
    };

    // UNVERIFIED: solo and empty tower failures are simplified practice checks.
    private void AkhMorn(int cycle, int hit)
    {
        foreach (var spot in Enum.GetValues<EdgeSpot>())
        {
            var at = ToWorld(state.AkhMornPoint(cycle, DsrP7DragonKingState.Spot(spot)));
            var action = hit == 0 ? AkhMornFirstAction(spot) : spot == EdgeSpot.Tanks ? ActionId.AkhMornsEdgeTanks : ActionId.AkhMornsEdgeParty;
            if (akhMornCasters.TryGetValue(spot, out var helper)) PlayEffect(helper, action, 1.1f, at: at);
            var hits = party.Find.InsideCircle(at, AkhMornRadius).Where(m => m.IsAlive()).ToList();
            if (hit == 0 && hits.Count == 0)
            {
                foreach (var member in AliveMembers().ToList())
                    member.Die(action, "nobody took an Akh Morn's Edge spot");
                return;
            }
            if (hits.Count != 1) continue;
            var lone = hits[0];
            if (!IsTank(lone)) lone.Die(action, "took Akh Morn's Edge alone (stack with your group)");
            else if (hit > 0 && Core.UserActions.Mitigation.DamageTaken(lone.ActiveStatusSnapshot.Select(s => s.StatusId), DamageKind.Magic) > 0f)
                lone.Die(action, "held Akh Morn's Edge alone without an invuln");
        }
    }

    private readonly List<SimEnemy> gigaflareCasters = [];

    private void CastGigaflares(int cycle)
    {
        CastSelf(thordan, ActionId.GigaflaresEdge);
        for (var blast = 0; blast < 3; blast++)
        {
            var at = ToWorld(state.GigaflareSpot(cycle, blast));
            if (TakeHelper(new Placement(at, 0f)) is not { } helper) continue;
            // UNVERIFIED: equal warning lengths inferred from the sheet's staggered cast durations.
            var omenDelay = CastSeconds(GigaflareActions[blast]) - CastSeconds(GigaflareActions[0]);
            helper.Casting.NativeCast(GigaflareActions[blast], ActionType.Action, omenDelay, CastSeconds(GigaflareActions[blast]), false, targetId: helper.GameObjectId, position: at);
            gigaflareCasters.Add(helper);
        }
    }

    private void Gigaflare(int cycle, int blast)
    {
        var at = ToWorld(state.GigaflareSpot(cycle, blast));
        if (blast < gigaflareCasters.Count) PlayEffect(gigaflareCasters[blast], GigaflareActions[blast], 1.1f, at: at);
        foreach (var hit in party.Find.InsideCircle(at, GigaflareLethalWithin).Where(m => m.IsAlive()).ToList())
            hit.Die(GigaflareActions[blast], "too close to Gigaflare's Edge (it falls off with distance; go to the far side)");
        if (blast < 2) return;
        var used = gigaflareCasters.ToList();
        gigaflareCasters.Clear();
        world.Events.Add(EffectCarrierLinger, () => { foreach (var helper in used) ReturnHelper(helper); });
    }

    private void OnAction(SimCharacter actor, uint action, ulong target)
    {
        if (action != 7533 || thordan == null || target != thordan.GameObjectId.ObjectId || !actor.IsAlive()
            || actor is not ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank } member) return;
        state.AggroTank = member.Role;
        thordan.SetTarget(actor, false);
        thordan.KeepFacing(actor);
    }

    private void Trinity()
    {
        var darkTank = AggroTank;
        var lightTank = darkTank == PartyRole.MainTank ? PartyRole.OffTank : PartyRole.MainTank;
        var nearest = party.Find.Closest(ThordanHome);
        foreach (var (target, action) in new[]
                 {
                     (party.Get(darkTank), ActionId.TrinityFirstEnmity),
                     (party.Get(lightTank), ActionId.TrinitySecondEnmity),
                     (nearest, ActionId.TrinityNearest),
                 })
        {
            if (target is not { } t || !t.IsAlive()) continue;
            PlayFromHelper(action, t.Position, RotationTowards(ThordanHome, t.Position), t.GameObjectId);
            foreach (var hit in party.Find.InsideCircle(t.Position, TrinityRadius).Where(m => m.IsAlive()).ToList())
                TrinityHit(hit, action);
        }
    }

    private void TrinityHit(SimCharacter hit, uint action)
    {
        var dark = action != ActionId.TrinitySecondEnmity;
        var light = action != ActionId.TrinityFirstEnmity;
        if (action == ActionId.TrinityNearest && hit.HasStatus(StatusId.PhysicalVulnerabilityUp))
        {
            hit.Die(action, "nearest to the Dragon-king for Trinity twice in a row (let the next pair take it)");
            return;
        }
        if ((dark && Stacks(hit, StatusId.DarkResistanceDown) >= TrinityLethalStacks) || (light && Stacks(hit, StatusId.TrinityLightResistanceDown) >= TrinityLethalStacks))
        {
            hit.Die(action, "took a third Trinity of the same element (tanks swap each pair)");
            return;
        }
        if (dark) hit.AddStatus(StatusId.DarkResistanceDown, TrinityResistanceSeconds);
        if (light) hit.AddStatus(StatusId.TrinityLightResistanceDown, TrinityResistanceSeconds);
        if (action == ActionId.TrinityNearest) hit.AddStatus(StatusId.PhysicalVulnerabilityUp, TrinityVulnSeconds);
    }

    private static int Stacks(SimCharacter member, ushort statusId) => member.FindStatus(statusId)?.Stacks ?? 0;

    private void CastMornAfahsEdge()
    {
        state.SnapshotMornAfahFacing(thordan?.Rotation ?? MathF.PI);
        CastSelf(thordan, ActionId.MornAfahsEdge);
        foreach (var (spot, action) in new[]
                 {
                     (EdgeSpot.Party, ActionId.MornAfahsEdgePartyFirst),
                     (EdgeSpot.SecondParty, ActionId.MornAfahsEdgeSecondPartyFirst),
                     (EdgeSpot.Tanks, ActionId.MornAfahsEdgeTanksFirst),
                 })
        {
            var at = ToWorld(state.MornAfahPoint(DsrP7DragonKingState.Spot(spot)));
            if (TakeHelper(new Placement(at, 0f)) is { } helper)
                helper.Casting.NativeCast(action, ActionType.Action, 0f, CastSeconds(action), false, targetId: helper.GameObjectId, position: at);
        }
    }

    private void DespawnAll()
    {
        helperPool.Clear();
        akhMornCasters.Clear();
        gigaflareCasters.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
        Completed = true;
    }

    // Helpers must exist before acting so asynchronous packet spawns do not drop telegraphs.
    private void FillHelperPool()
    {
        for (var i = 0; i < HelperPoolSize; i++)
            if (SpawnHelper(new Placement(Vector3.Zero, 0f)) is { } helper) helperPool.Enqueue(helper);
    }

    private SimEnemy? TakeHelper(Placement placement)
    {
        if (!helperPool.TryDequeue(out var helper)) return SpawnHelper(placement);
        helper.SetPosition(placement);
        return helper;
    }

    private void ReturnHelper(SimEnemy helper) => helperPool.Enqueue(helper);

    private SimEnemy? SpawnHelper(Placement placement)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Dummy,
            NameId: BNpcNameId.DragonKingThordan,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Visibility: SpawnVisibility.InvisibleHelper,
            Placement: placement));
        if (helper != null) spawned.Add(helper);
        return helper;
    }

    // An actor can carry only one action effect per frame.
    private void PlayFromHelper(uint actionId, Vector3 at, float rotation = 0f, GameObjectId? target = null)
    {
        if (TakeHelper(new Placement(at, rotation)) is not { } helper) return;
        PlayEffect(helper, actionId, 1.1f, rotation, target, at);
        world.Events.Add(EffectCarrierLinger, () => ReturnHelper(helper));
    }

    private bool IsTank(SimCharacter member) => member is ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank };

    private IEnumerable<SimCharacter> AliveMembers()
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }

    private static Vector3 ToWorld(Vector2 at) => new(at.X, 0f, at.Y);

    private static float CastSeconds(uint actionId) => (Natives.Data.Action(actionId)?.CastSeconds ?? 0f) - CastSpec.ActionEffectOffset;

    private static void CastSelf(SimEnemy? caster, uint actionId) =>
        caster?.Cast(new EnemyAction(actionId) { Cast = new() { FaceTarget = false } });

    private static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, float? rotation = null, GameObjectId? target = null, Vector3? at = null) =>
        caster?.Casting.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
            rotation: rotation, position: at ?? caster.Position, animationTargetId: target ?? caster.GameObjectId);

    private static float RotationTowards(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);
}
