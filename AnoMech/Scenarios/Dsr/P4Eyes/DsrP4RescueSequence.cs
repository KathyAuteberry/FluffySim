using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Scenarios.Dsr.P4Eyes;

public sealed class DsrP4RescueSequence(SimWorld world)
{
    private readonly List<SimEnemy> actors = [];
    private SimEnemy charibert = null!;
    private SimEnemy knight = null!;
    private SimEnemy spear = null!;
    private SimEnemy friend = null!;
    private float friendHealth;
    private float spearHealth = 1;
    private bool protectedFriend;
    private bool active;
    private bool prisonActive;
    private SimEventObject? prison;
    private float start;
    public bool Completed { get; private set; }
    public bool FriendSaved => protectedFriend && friendHealth > 0;
    public static readonly float[] BrightwingTimes = [35.3f, 40.3f, 45.3f, 50.3f];
    private static readonly HashSet<uint> Heals = [120, 135, 140, 185, 190, 189, 3594, 3610, 3614, 3570, 7434, 16556, 24284, 24291, 24296, 24303];

    public void Schedule(float at)
    {
        start = at;
        world.ActionResolved += OnAction;
        world.OnDespawn(() => world.ActionResolved -= OnAction);
        world.Events.Add(at, PrepareArena);
        world.Events.Add(at + 19.9f, SpawnCharibert);
        world.Events.Add(at + 20.3f, SpawnPrison);
        world.Events.Add(at + 21.85f, () => prisonActive = true);
        world.Events.Add(at + 55.6f, ReleasePrison);
        world.Events.Add(at + 20f, () => charibert.Cast(25316));
        world.Events.Add(at + 26.6f, SpawnFriend);
        world.Events.Add(at + 26.9f, BotLimitBreak);
        world.Events.Add(at + 27.5f, SpearImpact);
        for (var t = 29f; t < 55f; t += 1.1f)
            world.Events.Add(at + t, () => spear.Cast(new EnemyAction(protectedFriend ? 25368u : 25315u) { Effects = [Damage()], Timing = new() { DamageDelay = 0 } }));
        foreach (var t in BrightwingTimes) world.Events.Add(at + t, Brightwing);
        for (var t = 30f; t < 54f; t += 2.5f) world.Events.Add(at + t, BotHealing);
        world.Events.Add(at + 55.5f, PureOfHeart);
        world.Events.Add(at + 57.5f, () => { spear.SetTargetable(true); spear.SetHealthFraction(spearHealth); });
        world.Events.Add(at + 60.2f, () => spear.Cast(26971, friend));
        for (var t = 61f; t <= 68f; t++) world.Events.Add(at + t, BotSpearDamage);
        world.Events.Add(at + 71.2f, ResolvePierce);
        world.Events.Add(at + 73f, () =>
        {
            active = false;
            prison?.Despawn();
            foreach (var actor in actors) actor.Despawn();
            Completed = FriendSaved && spearHealth <= 0;
        });
    }

    public void Tick()
    {
        if (!prisonActive) return;
        foreach (var member in world.Party.ActiveMembers().ToArray())
            if (Vector2.Distance(new(member.Position.X, member.Position.Z), new(-11, 0)) > 9)
                member.Die(25313, "left the Planar Prison");
    }

    private void SpawnPrison()
    {
        prison = world.SpawnEventObject(new() { EObjId = 0x1EB681, Placement = new(new(-11, 0, 0), MathF.PI / 2) });
        prison?.EObjSetState(1, 0);
        var grinnaux = Spawn(12602, 3639, new(-11, 0, 0));
        grinnaux.Cast(25313);
        foreach (var member in world.Party.ActiveMembers())
        {
            world.Tether(grinnaux, member, 0x35, duration: 2);
            member.AddStatus(0x95, 1);
            member.AddStatus(2662);
            if (Vector3.Distance(member.Position, new Vector3(-11, 0, 0)) > 7)
                member.CarryTo(new(-11, 0, 1));
        }
    }

    private void ReleasePrison()
    {
        prisonActive = false;
        prison?.EObjSetState(0, 0);
        foreach (var member in world.Party.ActiveMembers()) member.RemoveStatus(2662);
    }

    private SimEnemy Spawn(uint id, uint name, Vector3 position, bool hostile = true, bool targetable = false)
    {
        var enemy = world.SpawnEnemy(DsrActors.WithNativeAppearance(new(id, name, Level: 90, Targetable: targetable,
            EnemyList: hostile ? EnemyListMode.Always : EnemyListMode.Never,
            IsHostile: hostile, Placement: new(position, 0)))) ?? throw new InvalidOperationException("Rescue actor unavailable");
        actors.Add(enemy);
        return enemy;
    }

    private void PrepareArena()
    {
        world.Map.SetWeather(2);
        for (byte slot = 0; slot < 9; slot++) world.Map.AddEffect(0x00040004, slot);
        world.Map.DirectorUpdate(0x80000016, 0, 0x14);
        world.Map.DirectorUpdate(0x8000001F, 758);
    }

    private void SpawnCharibert()
    {
        charibert = Spawn(12603, 3642, new(-11, 0, 5), targetable: true);
        knight = Spawn(12592, 3633, new(22, 0, 0));
        knight.Cast(25314);
        spear = Spawn(11810, 3633, new(3, 0, 0));
        world.Party.LimitBreak.Set(3);
    }

    private void SpawnFriend()
    {
        friend = Spawn(13117, 1455, new(2, 0, 0), hostile: false, targetable: true);
        friend.PlayActionTimeline(0x11D3);
        friendHealth = 1;
        active = true;
    }

    private void BotLimitBreak()
    {
        var role = world.Party.PlayerRole == PartyRole.OffTank ? PartyRole.OffTank : PartyRole.MainTank;
        if (world.Party.UseLimitBreak(role)) protectedFriend = true;
    }

    private void SpearImpact()
    {
        knight.Cast(new EnemyAction(25314) { Cast = new() { CastSecondsOverride = 0 } }, friend);
        friendHealth = protectedFriend ? .2f : 0;
        friend.SetHealthFraction(friendHealth);
        if (!protectedFriend) Fail(25314, "Haurchefant was not protected by tank LB3");
    }

    private void OnAction(SimCharacter actor, uint id, ulong target)
    {
        if (!active || actor.Dead) return;
        var role = (actor as ISimPartyMember)?.Role;
        if (role is PartyRole.MainTank or PartyRole.OffTank && Natives.Data.Action(id)?.ActionCategory == 9 &&
            world.Events.Elapsed < start + 27.5f && world.Party.LimitBreak.FilledBars == 3)
            protectedFriend = true;
        if (target == (ulong)friend.GameObjectId && Heals.Contains(id) && protectedFriend && friendHealth > 0)
        {
            friendHealth = id == 140 ? 1 : MathF.Min(1, friendHealth + .2f);
            friend.SetHealthFraction(friendHealth);
        }
        if (target == (ulong)spear.GameObjectId && !Heals.Contains(id) && world.Events.Elapsed >= start + 57.5f)
            DamageSpear(.08f);
    }

    private void BotHealing()
    {
        if (!active || !protectedFriend || friendHealth <= 0) return;
        foreach (var role in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer })
        {
            if (world.Party.Get(role) is not { Dead: false } healer || !world.Party.IsBotDriven(healer)) continue;
            var heal = healer.Proxy?.ClassJob switch { 28 => 190u, 40 => 24284u, 33 => 3594u, _ => 120u };
            world.Party.Act(healer, new EnemyAction(heal) { Cast = new() { CastSecondsOverride = 0 } }, friend);
            friendHealth = MathF.Min(1, friendHealth + .12f);
        }
        friend.SetHealthFraction(friendHealth);
    }

    private void Brightwing()
    {
        foreach (var bait in world.Party.Find.ClosestN(charibert.Position, 2).ToArray())
        {
            var helper = Spawn(9020, 0, charibert.Position);
            helper.SetModelHidden(true);
            helper.SetRotation(helper.Placement().Face(bait.Position).Rotation);
            helper.Cast(new EnemyAction(25369)
            {
                Area = new() { Size = MathF.PI / 12 },
                Effects = [Damage(VulnerableTo(2278)), ApplyStatus(2278, 15f), ApplyStatus(2661, 5f)],
                Timing = new() { DamageDelay = 0 },
            });
            world.Events.Add(5.48f, () => DropPuddle(bait));
        }
    }

    private void DropPuddle(SimCharacter bait)
    {
        if (bait.Dead) return;
        var helper = Spawn(9020, 0, bait.Position);
        helper.SetModelHidden(true);
        helper.Cast(new EnemyAction(25370) { Effects = [Damage(Severity.Lethal)], Timing = new() { DamageDelay = 0 } }, bait.Position);
    }

    private void PureOfHeart()
    {
        charibert.SetHealthFraction(.25f);
        charibert.Cast(new EnemyAction(25316) { Cast = new() { CastSecondsOverride = 0 }, Effects = [Damage()], Timing = new() { DamageDelay = 0 } });
        if (friendHealth < .65f) { friendHealth = 0; Fail(25316, "Haurchefant needed healing before Pure of Heart"); }
        else friendHealth -= .64f;
        friend.SetHealthFraction(friendHealth);
    }

    private void BotSpearDamage()
    {
        if (world.Party.ActiveMembers().Any(world.Party.IsBotDriven)) DamageSpear(.16f);
    }

    private void DamageSpear(float fraction)
    {
        if (spearHealth <= 0) return;
        spearHealth = MathF.Max(0, spearHealth - fraction);
        spear.SetHealthFraction(spearHealth);
        if (spearHealth <= 0) spear.Defeat();
    }

    private void ResolvePierce()
    {
        if (spearHealth > 0) { friendHealth = 0; Fail(26971, "Spear was not destroyed before Pierce"); }
    }

    private void Fail(uint action, string reason) => world.Party.WipeAllPlayers(action, reason);
}
