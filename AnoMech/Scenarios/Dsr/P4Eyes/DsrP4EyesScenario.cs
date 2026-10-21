using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Scenarios.Dsr.P4Eyes.DsrP4EyesConstants;

namespace AnoMech.Scenarios.Dsr.P4Eyes;
public sealed class DsrP4EyesScenario : IScenario
{
    public string DisplayNameOverride => "P4 Eyes";
    public string Name => "Eyes (Elemental)";
    public bool IsIncomplete => true;
    public bool EnablePhaseBgm => true;
    public DsrP4EyesState? LastState { get; private set; }
    public bool Completed => eyesCompleted && rescue.Completed;
    private bool eyesCompleted;
    public int BlueHealingHits { get; private set; }
    public int RedDamageHits { get; private set; }
    public IPhase Phase => DsrZone.P4;

    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP4EyesAi()];

    private readonly DsrP4EyesStateOverrides overrides = new();
    public object SettingsOverrides => overrides;

    public static readonly Vector3 LeftEyeSpot = new(10f, 0f, 0f);
    public static readonly Vector3 RightEyeSpot = new(-10f, 0f, 0f);
    public static readonly Vector3 AlphinaudSpot = new(0f, 0f, 10f);
    private const float EyeDriftSpeed = 2.4f;
    private static readonly Vector3 DiverWaitingSpot = new(23f, 0f, 4f);

    private const float SoulRadius = 5f;
    // UNVERIFIED: how close a red and a blue holder must stand for the tethers to trade.
    public const float PassRange = 1.5f;
    private const float BoundAndDeterminedSeconds = 3f;
    public static readonly Vector3[] GildedSpots = [new(-17f, 0f, 0f), new(17f, 0f, 0f)];
    public static readonly Vector3[] AzureSpots = [new(-10f, 0f, -7f), new(10f, 0f, -7f), new(-10f, 0f, 7f), new(10f, 0f, 7f)];
    private const float OrbBlastRadius = 6f;
    // UNVERIFIED: an orb pops when a player comes within its hitbox plus this.
    public const float OrbTouchMargin = 1f;
    private const float FireResistanceDownSeconds = 11.96f;
    private const float MirageDiveRadius = 4f;
    private const float PiercingResistanceDownSeconds = 12.96f;
    private const float BleedingSeconds = 6f;
    private const float EffectCarrierLinger = 3f;

    public const float OrbSpawnTime = 33.70f;
    public static readonly float[] DiveTimes = [59.87f, 65.00f, 70.13f, 75.22f];

    private SimWorld world = null!;
    private SimParty party = null!;
    private DsrP4EyesState state = null!;

    private SimEnemy? leftEye;
    private SimEnemy? rightEye;
    private SimEnemy? estinien;
    private SimEnemy? alphinaud;
    private SimEnemy? ysayle;
    private SimEnemy? haurchefant;
    private readonly List<SimEnemy> extras = [];
    private readonly Queue<SimEnemy> waitingDivers = new();
    private readonly List<Orb> orbs = [];
    private static readonly JsonSerializerOptions OrbTraceOptions = new() { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
    public sealed record OrbHit(float Time, Vector3 Position, bool Yellow, PartyRole Role, bool Red, bool Resistance);
    public List<OrbHit> OrbHits { get; } = [];
    public sealed record Transfer(float Time, PartyRole From, PartyRole To);
    public List<Transfer> Transfers { get; } = [];
    private readonly Dictionary<PartyRole, SimTether> boundTethers = [];
    private bool tethersPass;
    private float nextHealthUpdate;
    private DsrP4RescueSequence rescue = null!;
    public bool RescueCompleted => rescue.Completed;

    private sealed class Orb(SimEnemy enemy, Vector3 at, bool gilded)
    {
        public SimEnemy Enemy { get; } = enemy;
        public Vector3 At { get; } = at;
        public bool Gilded { get; } = gilded;
        public bool Popped { get; set; }
        // UNVERIFIED: the model is scaled by the hitbox radius; how the game sizes it was never seen.
        public float Scale { get; set; }
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = LastState = new DsrP4EyesState(world.Rng, overrides);
        eyesCompleted = false;
        nextHealthUpdate = 17.3f;
        BlueHealingHits = 0; RedDamageHits = 0;
        extras.Clear();
        waitingDivers.Clear();
        orbs.Clear(); OrbHits.Clear(); Transfers.Clear();
        boundTethers.Clear();
        tethersPass = false;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DsrP4EyesState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnWaitingDivers);
        world.Events.Add(4.85f, SpawnEyesAndEstinien);
        world.Events.Add(5.10f, TetherEyesToEstinien);
        world.Events.Add(5.40f, () => rightEye?.SetHealthFraction(.5f));
        world.Events.Add(5.17f, () => { leftEye?.PlayActionTimeline(TimelineId.EyeAppear); rightEye?.PlayActionTimeline(TimelineId.EyeAppear); estinien?.PlayActionTimeline(TimelineId.EyeAppear); });
        world.Events.Add(6.00f, SpawnAlphinaud);
        world.Events.Add(6.30f, () => alphinaud?.PlayActionTimeline(TimelineId.AllyAppear));
        world.Events.Add(11.90f, SpawnYsayleAndHaurchefant);
        world.Events.Add(12.36f, MarkSouls);
        world.Events.Add(12.45f, () => { ysayle?.PlayActionTimeline(TimelineId.AllyAppear); haurchefant?.PlayActionTimeline(TimelineId.AllyAppear); });
        world.Events.Add(14.46f, EyesDriftApart);
        world.Events.Add(17.30f, EyesEngage);
        world.Events.Add(17.49f, GrantSouls);
        world.Events.Add(19.30f, () => { ysayle?.FadeOut(); haurchefant?.FadeOut(); });
        world.Events.Add(24.70f, Resentment);

        world.Events.Add(30.02f, () => { CastSelf(leftEye, ActionId.Hatebound, 2.7f); CastSelf(rightEye, ActionId.Hatebound, 2.7f); });
        world.Events.Add(OrbSpawnTime, SpawnOrbs);
        world.Events.Add(33.75f, BindEveryone);
        world.Events.Add(OrbSpawnTime + Orbs.GildedBursts, () => BurstUnpopped(gilded: true));
        world.Events.Add(OrbSpawnTime + Orbs.AzureBursts, () => BurstUnpopped(gilded: false));

        world.Events.Add(56.10f, () => { CastSelf(leftEye, ActionId.MirageDiveWindup, 2.7f); CastSelf(rightEye, ActionId.MirageDiveWindup, 2.7f); });
        for (var i = 0; i < DiveTimes.Length; i++)
        {
            var dive = i;
            world.Events.Add(DiveTimes[i], () => MirageDive(dive));
        }
        world.Events.Add(76.00f, TryDefeatBlueEye);
        world.Events.Add(84.90f, TryDefeatBlueEye);
        world.Events.Add(79.10f, () => CastSelf(leftEye, ActionId.SteepInRage, 5.7f));
        world.Events.Add(85.05f, SteepInRage);
        world.Events.Add(86.30f, () => leftEye?.Defeat());
        world.Events.Add(89.20f, Unbind);
        world.Events.Add(92.00f, DespawnAll);
        rescue = new DsrP4RescueSequence(world);
        rescue.Schedule(92f);
    }

    public void Tick(float delta, float elapsed)
    {
        rescue.Tick();
        if (tethersPass) PassTethers();
        TouchOrbs(elapsed);
        if (elapsed >= nextHealthUpdate && elapsed < 89.2f && rightEye is { Dead: false })
        {
            nextHealthUpdate = elapsed + .5f;
            rightEye.SetHealthFraction(BlueHealth);
        }
    }

    private float BlueHealth => MathF.Max(0, .5f - MathF.Max(0, world.Events.Elapsed - 17.3f) * (.5f / 58.7f) + BlueHealingHits * .05f);

    private void TryDefeatBlueEye()
    {
        if (BlueHealth <= .001f) rightEye?.Defeat();
    }

    private void RecordColourHit(SimCharacter hit)
    {
        if (hit is not ISimPartyMember member) return;
        if (state.HoldsRed(member.Role)) RedDamageHits++;
        else BlueHealingHits++;
    }

    private void SpawnEyesAndEstinien()
    {
        leftEye = SpawnNpc(BNpcBaseId.LeftEye, BNpcNameId.LeftEye, new Placement(new Vector3(2f, 0f, 0f), -MathF.PI / 4f), EnemyListMode.Always);
        rightEye = SpawnNpc(BNpcBaseId.RightEye, BNpcNameId.RightEye, new Placement(new Vector3(-2f, 0f, 0f), MathF.PI / 4f), EnemyListMode.Always);
        estinien = SpawnNpc(BNpcBaseId.Estinien, BNpcNameId.Estinien, new Placement(Vector3.Zero, 0f), EnemyListMode.Never);
    }

    private void TetherEyesToEstinien()
    {
        if (estinien == null) return;
        if (leftEye != null) world.Tether(leftEye, estinien, TetherId.LeftEyeToEstinien);
        if (rightEye != null) world.Tether(rightEye, estinien, TetherId.RightEyeToEstinien);
    }

    private void SpawnAlphinaud() =>
        alphinaud = SpawnNpc(BNpcBaseId.Alphinaud, BNpcNameId.Alphinaud, new Placement(AlphinaudSpot, MathF.PI), EnemyListMode.Never);

    private void SpawnYsayleAndHaurchefant()
    {
        ysayle = SpawnNpc(BNpcBaseId.Ysayle, BNpcNameId.Ysayle, new Placement(new Vector3(3f, 0f, 2f), 0f), EnemyListMode.Never);
        haurchefant = SpawnNpc(BNpcBaseId.Haurchefant, BNpcNameId.Haurchefant, new Placement(new Vector3(-3f, 0f, 2f), 0f), EnemyListMode.Never);
    }

    private void MarkSouls()
    {
        if (party.Get(state.SoulMarked) is { } marked && haurchefant != null)
        {
            marked.AttachLockonVfx(LockonId.SoulOfFriendship);
            world.Tether(haurchefant, marked, TetherId.Soul, duration: 5.2f);
        }
        if (alphinaud != null && ysayle != null)
        {
            alphinaud.AttachLockonVfx(LockonId.SoulOfDevotion);
            world.Tether(ysayle, alphinaud, TetherId.Soul, duration: 5.2f);
        }
    }
    private void GrantSouls()
    {
        var friendshipAt = party.Get(state.SoulMarked) is { } marked && !marked.Dead ? marked.Position : AlphinaudSpot;
        PlayEffect(haurchefant, ActionId.SoulOfFriendship, 1.1f, at: friendshipAt);
        PlayEffect(ysayle, ActionId.SoulOfDevotion, 1.1f, at: AlphinaudSpot);
        foreach (var member in AliveMembers().ToList())
        {
            var friendship = FlatDistance(member.Position, friendshipAt) <= SoulRadius;
            var devotion = FlatDistance(member.Position, AlphinaudSpot) <= SoulRadius;
            if (friendship) member.AddStatus(StatusId.SoulOfFriendship, 89.96f);
            if (devotion) member.AddStatus(StatusId.SoulOfDevotion, 89.96f);
            if (!friendship || !devotion)
                Hit(estinien!, friendship ? ActionId.SoulOfDevotion : ActionId.SoulOfFriendship, [member], (ctx, hit) => ctx.Kill(hit, "missed a soul buff"));
        }
    }

    private void EyesDriftApart()
    {
        PlayEffect(leftEye, ActionId.EyesDrift, 0.6f, at: LeftEyeSpot);
        PlayEffect(rightEye, ActionId.EyesDrift, 0.6f, at: RightEyeSpot);
        leftEye?.MoveTo(LeftEyeSpot, EyeDriftSpeed, -MathF.PI / 2f);
        rightEye?.MoveTo(RightEyeSpot, EyeDriftSpeed, MathF.PI / 2f);
    }

    private void EyesEngage()
    {
        leftEye?.SetTargetable(true);
        rightEye?.SetTargetable(true);
    }

    private void Resentment()
    {
        Hit(estinien!, ActionId.Resentment, AliveMembers(), (ctx, member) => ctx.AddStatus(member, StatusId.Bleeding, BleedingSeconds));
    }

    private void BindEveryone()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (party.Get(role) is { } member && !member.Dead)
                Bind(role, member, state.FirstRed.Contains(role));
        tethersPass = true;
    }

    private void Bind(PartyRole role, SimCharacter member, bool red)
    {
        if (boundTethers.Remove(role, out var old)) old.Despawn();
        member.RemoveStatus(red ? StatusId.Fangbound : StatusId.Clawbound);
        member.AddStatus(red ? StatusId.Clawbound : StatusId.Fangbound);
        AnoMech.Core.DiagnosticLog.Info($"[Eyes colour] t={world.Events.Elapsed:F2} {role} red={red}");
        if (red) state.Red.Add(role);
        else state.Red.Remove(role);
        if ((red ? leftEye : rightEye) is { } eye)
            boundTethers[role] = world.Tether(member, eye, red ? TetherId.Clawbound : TetherId.Fangbound);
    }
    private void PassTethers()
    {
        var members = Enum.GetValues<PartyRole>()
            .Select(r => (Role: r, Member: party.Get(r)))
            .Where(x => x.Member is { } m && !m.Dead && boundTethers.ContainsKey(x.Role) && !m.HasStatus(StatusId.BoundAndDetermined))
            .ToList();
        var traded = new HashSet<PartyRole>();
        foreach (var red in members.Where(x => state.HoldsRed(x.Role)).ToArray())
        
        {
            if (traded.Contains(red.Role)) continue;
            var blue = members
                .Where(x => !state.HoldsRed(x.Role) && !traded.Contains(x.Role))
                .Where(x => FlatDistance(x.Member!.Position, red.Member!.Position) <= PassRange)
                .OrderBy(x => FlatDistance(x.Member!.Position, red.Member!.Position))
                .FirstOrDefault();
            if (blue.Member == null) continue;
            traded.Add(red.Role);
            traded.Add(blue.Role);
            Transfers.Add(new(world.Events.Elapsed, red.Role, blue.Role));
            AnoMech.Core.DiagnosticLog.Info($"[Eyes transfer] t={world.Events.Elapsed:F2} {red.Role}@{red.Member!.Position} red→blue; {blue.Role}@{blue.Member.Position} blue→red; lockout={BoundAndDeterminedSeconds}");
            Bind(red.Role, red.Member!, red: false);
            Bind(blue.Role, blue.Member, red: true);
            red.Member!.AddStatus(StatusId.BoundAndDetermined, BoundAndDeterminedSeconds);
            blue.Member.AddStatus(StatusId.BoundAndDetermined, BoundAndDeterminedSeconds);
        }
    }

    private void SpawnOrbs()
    {
        foreach (var at in GildedSpots)
            SpawnOrb(BNpcBaseId.GildedPrice, BNpcNameId.GildedPrice, at, gilded: true);
        foreach (var at in AzureSpots)
            SpawnOrb(BNpcBaseId.AzurePrice, BNpcNameId.AzurePrice, at, gilded: false);
    }

    private void SpawnOrb(uint baseId, uint nameId, Vector3 at, bool gilded)
    {
        var enemy = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: 90,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(at, RotationTowards(at, Vector3.Zero))));
        if (enemy == null) return;
        extras.Add(enemy);
        var orb = new Orb(enemy, at, gilded);
        orbs.Add(orb);
        TraceOrb(orb, "spawn");
        AnoMech.Core.DiagnosticLog.Info($"[Eyes spawn] t={world.Events.Elapsed:F2} player={party.PlayerRole} orb={baseId}@{at} yellow={gilded}");
    }
    private void TouchOrbs(float elapsed)
    {
        var age = elapsed - OrbSpawnTime;
        foreach (var orb in orbs.Where(o => !o.Popped).ToList())
        {
            var radius = Orbs.Radius(orb.Gilded, age);
            if (radius != orb.Scale)
            {
                orb.Scale = radius;
                orb.Enemy.SetModelScale(radius);
                TraceOrb(orb, "growth");
                AnoMech.Core.DiagnosticLog.Info($"[Eyes growth] t={elapsed:F2} at={orb.At} yellow={orb.Gilded} radius={radius}");

            }
            var touchers = AliveMembers().Where(m => FlatDistance(m.Position, orb.At) <= radius + OrbTouchMargin).ToList();
            if (touchers.Count > 0) Pop(orb, touchers, Orbs.Ripe(orb.Gilded, age));
        }
    }
    private void Pop(Orb orb, List<SimCharacter> touchers, bool ripe)
    {
        orb.Popped = true;
        TraceOrb(orb, "soaked");
        AnoMech.Core.DiagnosticLog.Info($"[Eyes orb] t={world.Events.Elapsed:F2} at={orb.At} yellow={orb.Gilded} radius={orb.Scale} ripe={ripe} touchers={string.Join(",", touchers.Select(m => ((ISimPartyMember)m).Role))}");
        var actionId = orb.Gilded ? ActionId.FlareNova : ActionId.FlareStar;
        var hits = AliveMembers().Where(m => FlatDistance(m.Position, orb.At) <= OrbBlastRadius).ToArray();
        Hit(orb.Enemy, actionId, hits, (ctx, hit) =>
        {
            if (hit is ISimPartyMember recipient)
            {
                OrbHits.Add(new(world.Events.Elapsed, orb.At, orb.Gilded, recipient.Role, state.HoldsRed(recipient.Role), hit.HasStatus(StatusId.FireResistanceDownII)));
                AnoMech.Core.DiagnosticLog.Info($"[Eyes hit] t={world.Events.Elapsed:F2} {recipient.Role}@{hit.Position} red={state.HoldsRed(recipient.Role)} fireDown={hit.HasStatus(StatusId.FireResistanceDownII)}");
            }
            RecordColourHit(hit);
            if (!ripe || (orb.Gilded && hits.Length < 2)) ctx.Kill(hit, "orb popped early or understacked");
            else if (hit is ISimPartyMember member && !state.HoldsRed(member.Role)) ctx.Kill(hit, "orb hit without red tether");
            else if (hit.HasStatus(StatusId.FireResistanceDownII)) ctx.Kill(hit, "second orb with Fire Resistance Down");
            else
            {
                ctx.AddStatus(hit, StatusId.FireResistanceDownII, FireResistanceDownSeconds);
                AnoMech.Core.DiagnosticLog.Info($"[Eyes resistance] {((ISimPartyMember)hit).Role} until={world.Events.Elapsed + FireResistanceDownSeconds:F2}");
            }
        });
        world.Events.Add(EffectCarrierLinger, orb.Enemy.FadeOut);
    }
    private void BurstUnpopped(bool gilded)
    {
        foreach (var orb in orbs.Where(o => o.Gilded == gilded && !o.Popped).ToList())
        {
            TraceOrb(orb, "expired");
            var assigned = state.OrbSoakers.GetValueOrDefault(orb.At) ?? [];
            var responsibility = string.Join("/", assigned.Select(r => r + (r == party.PlayerRole ? " (human)" : " (bot)")));
            world.Announce($"Missed {(gilded ? "yellow" : "blue")} orb at {orb.At.X}, {orb.At.Z}; assigned: {responsibility}.");
            orb.Popped = true;
            var actionId = gilded ? ActionId.FlareNovaUnpopped : ActionId.FlareStarUnpopped;
            Hit(orb.Enemy, actionId, AliveMembers(), (ctx, hit) => ctx.Kill(hit, "orb left to burst"));
            world.Events.Add(EffectCarrierLinger, orb.Enemy.FadeOut);
        }
    }

    private void TraceOrb(Orb orb, string phase)
    {
        var assigned = state.OrbSoakers.GetValueOrDefault(orb.At) ?? [];
        object Point(Vector3 at) => new { x = at.X, z = at.Z };
        var payload = new
        {
            time = world.Events.Elapsed, orb = orb.Enemy.GameObjectId.ObjectId,
            position = Point(orb.At), colour = orb.Gilded ? "yellow" : "blue", state = phase,
            radius = Orbs.Radius(orb.Gilded, world.Events.Elapsed - OrbSpawnTime), ripe = Orbs.Ripe(orb.Gilded, world.Events.Elapsed - OrbSpawnTime),
            human = party.PlayerRole.ToString(), assigned = assigned.Select(r => r.ToString()).ToArray(),
            humanResponsible = assigned.Contains(party.PlayerRole),
            failure = phase != "expired" ? null : assigned.Any(r => party.Get(r) is not { Dead: false })
                ? "assigned soaker unavailable" : assigned.Any(r => !state.HoldsRed(r))
                    ? "assigned soaker lacks red tether" : "assigned soaker did not reach orb",
            affected = (phase == "expired" ? AliveMembers() : phase == "soaked" ? AliveMembers().Where(m => FlatDistance(m.Position, orb.At) <= OrbBlastRadius) : Array.Empty<SimCharacter>())
                .Cast<ISimPartyMember>().Select(m => m.Role.ToString()).ToArray(),
            recentTransfers = Transfers.Where(t => world.Events.Elapsed - t.Time <= 5)
                .Select(t => new { time = t.Time, from = t.From.ToString(), to = t.To.ToString() }).ToArray(),
            players = Enum.GetValues<PartyRole>().Select(role =>
            {
                var member = party.Get(role);
                return new
                {
                    role = role.ToString(), red = state.HoldsRed(role), alive = member is { Dead: false },
                    position = member == null ? null : Point(member.Position),
                    destination = member?.MoveDestination is { } destination ? Point(destination) : null,
                    distance = member == null ? (float?)null : FlatDistance(member.Position, orb.At),
                    lockout = member?.FindStatus(StatusId.BoundAndDetermined)?.RemainingTime ?? 0,
                    fireResistanceDown = member?.FindStatus(StatusId.FireResistanceDownII)?.RemainingTime ?? 0,
                };
            }).ToArray(),
        };
        AnoMech.Core.DiagnosticLog.Info("[EyesOrb] " + JsonSerializer.Serialize(payload, OrbTraceOptions));
    }

    private void SpawnWaitingDivers()
    {
        for (var i = 0; i < DsrP4EyesState.Dives * 2; i++)
        {
            var diver = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.NidhoggDiver,
                NameId: BNpcNameId.Nidhogg,
                Level: 90,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(DiverWaitingSpot, 0f)));
            if (diver == null) continue;
            diver.SetModelHidden(true);
            extras.Add(diver);
            waitingDivers.Enqueue(diver);
        }
    }
    private void MirageDive(int dive)
    {
        var red = Enum.GetValues<PartyRole>().Where(r => state.HoldsRed(r) && party.Get(r) is { } m && !m.Dead).ToList();
        if (red.Count == 0) return;
        var (first, second) = state.DivePicks[dive];
        var picks = new[] { red[first % red.Count], red[second % red.Count] }.Distinct().ToList();
        state.DiveTargets[dive].Clear();
        state.DiveTargets[dive].AddRange(picks);
        foreach (var role in picks)
        {
            if (party.Get(role) is not { } target || !waitingDivers.TryDequeue(out var diver)) continue;
            diver.SetPosition(target.Placement());
            diver.SetModelHidden(false);
            Hit(diver, ActionId.MirageDive, party.Find.InsideCircle(target.Position, MirageDiveRadius), (ctx, hit) =>
            {
                RecordColourHit(hit);
                if (hit.HasStatus(StatusId.PiercingResistanceDown)) ctx.Kill(hit, "repeated Mirage Dive before swapping");
                else ctx.AddStatus(hit, StatusId.PiercingResistanceDown, PiercingResistanceDownSeconds);
            });
            world.Events.Add(EffectCarrierLinger, () => diver.SetModelHidden(true));
        }
    }
    private void SteepInRage()
    {
        var bothEyes = rightEye is { Dead: false };
        Hit(leftEye!, ActionId.SteepInRage, AliveMembers(), (ctx, hit) =>
        {
            if (bothEyes) ctx.Kill(hit, "both eyes alive at Steep in Rage");
        });
    }

    private void Unbind()
    {
        tethersPass = false;
        foreach (var tether in boundTethers.Values) tether.Despawn();
        boundTethers.Clear();
        state.Red.Clear();
        foreach (var member in AliveMembers())
            foreach (var status in new[] { StatusId.Clawbound, StatusId.Fangbound, StatusId.SoulOfFriendship, StatusId.SoulOfDevotion })
                member.RemoveStatus(status);
    }

    private void DespawnAll()
    {
        Unbind();
        eyesCompleted = true;
        foreach (var enemy in extras) enemy.Despawn();
        extras.Clear();
        foreach (var npc in new[] { leftEye, rightEye, estinien, alphinaud, ysayle, haurchefant })
            npc?.Despawn();
    }

    private SimEnemy? SpawnNpc(uint baseId, uint nameId, Placement placement, EnemyListMode enemyList) =>
        world.SpawnEnemy(DsrActors.WithNativeAppearance(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: 90,
            Targetable: false,
            EnemyList: enemyList,
            Placement: placement, IsHostile: baseId is BNpcBaseId.LeftEye or BNpcBaseId.RightEye)));

    private IEnumerable<SimCharacter> AliveMembers()
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && !member.Dead)
                yield return member;
    }

    private static void CastSelf(SimEnemy? caster, uint actionId, float castSeconds) =>
        caster?.Cast(new EnemyAction(actionId) { Cast = new() { CastSecondsOverride = castSeconds } });

    private static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, Vector3? at = null) =>
        caster?.Cast(new EnemyAction(actionId) { Cast = new() { CastSecondsOverride = 0, AnimationLock = animationLock }, Timing = new() { DamageDelay = 0 } }, at is { } spot ? (CastTarget)spot : default);

    private void Hit(SimEnemy caster, uint id, IEnumerable<SimCharacter> hits, Action<EnemyActionContext, SimCharacter> resolve)
    {
        var targets = hits.ToArray();
        caster.Cast(new EnemyAction(id)
        {
            Cast = new() { CastSecondsOverride = 0 },
            Area = new() { AdjustTargets = (_, _) => targets },
            Timing = new() { DamageDelay = 0 },
            Effects = [Resolve(ctx =>
            {
                foreach (var member in targets)
                {
                    ctx.ShowDamage(member, 30000, DamageKind.Magic);
                    resolve(ctx, member);
                }
            })],
        });
    }

    private static float RotationTowards(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    public static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));
}
public static class Orbs
{
    public const float GildedBursts = 9.39f;
    public const float AzureBursts = 15.36f;
    // UNVERIFIED: size before the first step.
    private const float Seed = 0.5f;
    private static readonly (float At, float Radius)[] Gilded = [(2.7f, 1.025f), (4.7f, 1.5f), (5.8f, 1.525f), (7.2f, 2f)];
    private static readonly (float At, float Radius)[] Azure = [(5.8f, 1.025f), (7.9f, 1.5f), (11.8f, 1.525f), (13.2f, 2f)];
    public static bool Ripe(bool gilded, float age) => age >= (gilded ? Gilded : Azure)[2].At;

    public static float Radius(bool gilded, float age)
    {
        var radius = Seed;
        foreach (var (at, r) in gilded ? Gilded : Azure)
            if (age >= at) radius = r;
        return radius;
    }
}
