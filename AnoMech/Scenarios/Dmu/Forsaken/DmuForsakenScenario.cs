using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu.Forsaken;

// Scenario time 0 is Kefka's spawn, 2.46 s before Forsaken.
public sealed class DmuForsakenScenario : IMultiplayerReplayable
{
    private static readonly float[] TowerTimes = [22.66f, 32.67f, 43.71f, 53.72f, 64.76f, 74.77f, 85.81f, 95.82f];
    private static readonly float[] TowerShownTimes = [12.51f, 22.53f, 33.56f, 43.58f, 54.61f, 64.63f, 75.67f, 85.68f];
    private static readonly float[] KefkaEndTimes = [25.61f, 46.75f, 67.76f, 88.81f];
    private static readonly float[] CloneEndTimes = [32.35f, 53.59f, 74.61f, 95.51f];
    private const float CloneLandDelay = 0.67f;
    private const float CloneLandsShortOfTarget = 4f;

    public string Name => "Forsaken";
    public IPhase Phase => DmuZone.P2;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new DmuForsakenAi()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public DmuForsakenState? LastState { get; private set; }

    private readonly DmuForsakenSettingsWindow settingsWindow = new();
    private DmuForsakenState state = null!;
    private SimWorld world = null!;
    private SimParty party = null!;
    private SimEnemy? kefka;
    private IReadOnlyList<SimCharacter> endAttackTargets = [];
    private readonly List<SimEnemy?> spellHelpers = [];
    private string? lastBroadcastLockons;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new DmuForsakenState(world.Rng, settingsWindow.Overrides);
        LastState = state;
        lastBroadcastLockons = null;
        endAttackTargets = [];
        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<DmuForsakenState>)AiStrats[idx]).Run(state, world);

        SpawnKefka();
        SpawnTowers();
        SpawnClones();
        ArenaEvents();
        world.Events.Add(11.04f, () => party.ForEachActive(p => p.AddStatus(StatusId.SpellsTrouble, duration: 999f, stacks: 4, overrideStacks: true)));
        world.Events.Add(11.04f, () => party.ForEachActive(p => p.AddStatus(ShapeStatus(state.Lockons[((ISimPartyMember)p).Role]), duration: 9999f)));
        world.Events.Add(11.04f, () => party.ForEachActive(c => c.AttachLockonVfx(state.Lockons[((ISimPartyMember)c).Role])));
    }

    public void Tick(float delta, float elapsed) { }

    private void ArenaEvents()
    {
        world.Events.Add(2.37f, () => world.Map.AddEffect(packetFlags: 0x00080004U, index: 0x11));
        world.Events.Add(9.43f, () => world.Map.AddEffect(packetFlags: 0x00020001U, index: 0x11));
        world.Events.Add(11.04f, () => world.Map.AddEffect(packetFlags: 0x00040001U, index: 0x12));
        for (var i = 0; i < TowerShownTimes.Length; i++)
        {
            var (d1, d2) = state.GetTowers(i);
            ShowTower(TowerShownTimes[i], d1);
            ShowTower(TowerShownTimes[i], d2);
        }
    }

    private void ShowTower(float at, Direction direction)
    {
        var index = (byte)(direction.Index() + 1);
        world.Events.Add(at, () => world.Map.AddEffect(packetFlags: 0x00020001U, index: index));
        world.Events.Add(at + 3f, () => world.Map.AddEffect(packetFlags: 0x00080004U, index: index));
    }

    private void SpawnKefka()
    {
        kefka = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.GodKefka, NameId: BNpcNameId.Kefka, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: LayoutId.GodKefka,
            Placement: new Placement(Vector3.Zero, 0f)));
        foreach (var at in new[] { 1.30f, 11.35f, 14.39f, 17.43f, 20.47f, 23.43f, 109.73f })
            world.Events.Add(at, () => kefka?.Cast(DmuActions.AutoAttack, party.Get(PartyRole.OffTank)));
        world.Events.Add(2.46f, () => kefka?.Cast(DmuForsakenActions.Forsaken));
        world.Events.Add(10.38f, () => world.Map.SetWeather(89, transition: 2f));
        for (var i = 0; i < KefkaEndTimes.Length; i++) KefkaEndAttack(i, KefkaEndTimes[i]);
        world.Events.Add(110.84f, () => kefka?.Cast(DmuForsakenActions.LightOfJudgment));
        world.Events.Add(116.40f, () => world.Map.SetWeather(79, transition: 0f));
    }

    private void KefkaEndAttack(int index, float start)
    {
        var end = state.EndAttacks[index];
        world.Events.Add(start, () => kefka?.Cast(end.CastBarAction, animationLock: 0.2f));
        world.Events.Add(start + 6.7f, () =>
        {
            if (kefka == null) return;
            endAttackTargets = party.Find.ClosestN(kefka.Position, 4);
            if (endAttackTargets.Count > 0) kefka.Cast(end.KefkaHit, endAttackTargets[0]);
        });
        world.Events.Add(start + 12.8f, () => AimAllThingsEnding(kefka, index, 0));
        world.Events.Add(start + 12.9f, () => kefka?.Cast(end.AllThingsEnding));
        world.Events.Add(start + 18f, () => kefka?.Face(party.Get(PartyRole.OffTank)));
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.Kefka,
        Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(new Vector3(0f, 0f, -10f), 0f)));

    private void SpawnTowers()
    {
        var tower1 = SpawnHelper();
        var tower2 = SpawnHelper();
        for (var i = 0; i < TowerTimes.Length; i++) Tower(tower1, tower2, TowerTimes[i], i);
        spellHelpers.Clear();
        for (var i = 0; i < 4; i++) spellHelpers.Add(SpawnHelper());
    }

    private void Tower(SimEnemy? tower1, SimEnemy? tower2, float start, int index)
    {
        var (d1, d2) = state.GetTowers(index);
        world.Events.Add(start - 1f, () => tower1?.SetPosition(d1.Apply(new Placement(new Vector3(0f, 0f, -8f), 0))));
        world.Events.Add(start - 1f, () => tower2?.SetPosition(d2.Apply(new Placement(new Vector3(0f, 0f, -8f), 0))));
        world.Events.Add(start, () =>
        {
            if (tower1 == null || tower2 == null) return;
            var soakers1 = tower1.Cast(DmuForsakenActions.ThePathOfLight).Hits.Select(h => h.Who).ToList();
            var soakers2 = tower2.Cast(DmuForsakenActions.ThePathOfLight).Hits.Select(h => h.Who).ToList();
            if (soakers1.Count != 2 || soakers2.Count != 2) return;
            ResolveSpell(soakers1[0], 0);
            ResolveSpell(soakers1[1], 1);
            ResolveSpell(soakers2[0], 2);
            ResolveSpell(soakers2[1], 3);
            world.Events.Add(0.6f, () => ReassignLockons([soakers1[0], soakers1[1], soakers2[0], soakers2[1]], index));
        });
    }

    // After the seventh tower the stack moves to a cone holder of the towers' last soakers.
    private void ReassignLockons(SimCharacter[] soakers, int index)
    {
        var ordered = soakers.OrderBy(c => ((ISimPartyMember)c).Role).ToList();
        var deal = state.LockonDeals[index];
        uint[] lockons = index switch
        {
            6 => [LockonId.ForsakenStack, LockonId.ForsakenStack, LockonId.ForsakenCone, LockonId.ForsakenStack],
            _ when index % 2 == 0 => [LockonId.ForsakenCone, LockonId.ForsakenCone, LockonId.ForsakenChariot, LockonId.ForsakenChariot],
            _ => [LockonId.ForsakenStack, LockonId.ForsakenStack, LockonId.ForsakenCone, LockonId.ForsakenChariot],
        };
        for (var i = 0; i < 4; i++)
        {
            ordered[i].AddStatus(StatusId.SpellsTrouble, duration: 999f, stacks: -1);
            var role = ((ISimPartyMember)ordered[i]).Role;
            var lockon = ordered[i].HasStatus(StatusId.SpellsTrouble) ? lockons[deal[i]] : 0u;
            if (lockon != 0) ordered[i].AttachLockonVfx(lockon);
            var previous = state.Lockons[role];
            if (previous != lockon)
            {
                if (previous != 0) ordered[i].RemoveStatus(ShapeStatus(previous));
                if (lockon != 0) ordered[i].AddStatus(ShapeStatus(lockon), duration: 9999f);
            }
            state.Lockons[role] = lockon;
        }
        DiagnosticLog.Info($"[DmuForsaken] Tower {index + 1} lockons {string.Join(",", ordered.Select(c => $"{((ISimPartyMember)c).Role}={state.Lockons[((ISimPartyMember)c).Role]}"))}");
    }

    private static ushort ShapeStatus(uint lockon) => lockon switch
    {
        LockonId.ForsakenStack => StatusId.ForsakenStackShape,
        LockonId.ForsakenChariot => StatusId.ForsakenChariotShape,
        _ => StatusId.ForsakenConeShape,
    };

    private void ResolveSpell(SimCharacter character, int helperIndex)
    {
        var lockon = state.Lockons[((ISimPartyMember)character).Role];
        world.Events.Add(0.6f, () =>
        {
            var helper = spellHelpers[helperIndex];
            switch (lockon)
            {
                case LockonId.ForsakenStack:
                    helper?.Cast(DmuForsakenActions.Spelldriver, character);
                    break;
                case LockonId.ForsakenChariot:
                    helper?.Cast(DmuForsakenActions.Spellscatter, character);
                    break;
                case LockonId.ForsakenCone:
                    var position = character.Placement();
                    if (party.Find.Closest(character.Position, character) is { } closest) position = position.Face(closest.Position);
                    helper?.SetPosition(position);
                    helper?.Cast(DmuForsakenActions.Spellwave);
                    break;
            }
        });
    }

    private void SpawnClones()
    {
        for (var i = 0; i < 3; i++)
        {
            var clone = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.KefkaClone, NameId: BNpcNameId.Kefka, Level: Level,
                Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.HiddenUntilShown, LayoutId: LayoutId.KefkaClone,
                Placement: new Placement(Vector3.Zero, 0f)));
            for (var n = 0; n < CloneEndTimes.Length; n++) CloneEndAttack(clone, CloneEndTimes[n], n, i);
        }
    }

    private void CloneEndAttack(SimEnemy? clone, float start, int number, int index)
    {
        var end = state.EndAttacks[number];
        world.Events.Add(start - 0.5f, () => clone?.SetPosition(Vector3.Zero));
        world.Events.Add(start, () =>
        {
            if (clone == null || endAttackTargets.Count <= index + 1) return;
            var target = endAttackTargets[index + 1];
            clone.Cast(end.CloneHit, target);
            var landing = LandingShortOf(clone.Position, target.Position);
            world.Events.Add(CloneLandDelay, () => clone.SetPosition(landing));
        });
        world.Events.Add(start + 6f, () => AimAllThingsEnding(clone, number, index + 1));
        world.Events.Add(start + 6.1f, () => clone?.Cast(end.AllThingsEnding));
        world.Events.Add(start + 14.1f, () => clone?.PlayActionTimeline(ActionTimelineId.WarpStart));
    }

    // A clone lands short of the player it jumped on, facing them; one already within reach stays put.
    private static Placement LandingShortOf(Vector3 from, Vector3 target)
    {
        var offset = new Vector3(target.X - from.X, 0f, target.Z - from.Z);
        var distance = offset.Length();
        var landing = distance > CloneLandsShortOfTarget ? from + offset * ((distance - CloneLandsShortOfTarget) / distance) : from;
        return new Placement(landing with { Y = 0f }, 0f).Face(target);
    }

    // Each All Things Ending aims at one party member's spot as its cast starts: Future's End's half
    // covers them, Past's End's the other half.
    private void AimAllThingsEnding(SimEnemy? caster, int end, int source)
    {
        if (caster == null) return;
        var aimed = party.Get(state.AllThingsEndingAims[end][source]) is { } member && member.IsAlive()
            ? member
            : party.ActiveMembers().FirstOrDefault(m => m.IsAlive());
        caster.Face(aimed);
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s
            ? new DmuForsakenAiReplayStateMessage(s.EncodeEndAttacks(), s.NewNorth.RadiansFromNorth, s.Rotation,
                new Dictionary<PartyRole, uint>(s.Lockons), s.EncodeFlags())
            : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not DmuForsakenAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (DmuForsakenState.FromNetworkReplay(msg.EndAttacks, msg.NewNorthRadians, msg.Rotation, msg.Lockons, msg.Flags) is not { } shadow)
            return null;
        ((IScenarioAi<DmuForsakenState>)AiStrats[aiIndex]).Run(shadow, replayWorld);
        return shadow;
    }

    // Lockons are reassigned on the host only; a peer's stale lookup would throw inside its scheduler.
    public MpMessage? BuildMidRunUpdateMessage()
    {
        if (LastState is not { } s) return null;
        var key = string.Join(",", s.Lockons.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}"));
        if (lastBroadcastLockons == key) return null;
        lastBroadcastLockons = key;
        return new DmuForsakenLockonsUpdateMessage(new Dictionary<PartyRole, uint>(s.Lockons));
    }

    public void ApplyMidRunUpdate(object shadowState, MpMessage message)
    {
        if (shadowState is DmuForsakenState shadow && message is DmuForsakenLockonsUpdateMessage update)
            shadow.Lockons = update.Lockons;
    }
}
