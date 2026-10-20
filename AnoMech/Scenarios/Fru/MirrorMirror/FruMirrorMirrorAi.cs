using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.MirrorMirror;

public sealed class FruMirrorMirrorAi : IScenarioAi<FruMirrorMirrorState>
{
    public string Name => "Nukemaru";
    public string Group => "Mana";
    public static bool MeleeGroup(PartyRole role) => role.IsTank() || role is PartyRole.MeleeDpsA or PartyRole.MeleeDpsB;
    public static int MeleeRed(FruMirrorMirrorState state)
    {
        var opposite = (state.Blue + 4) % 8;
        int Distance(int red) => Math.Min((red - opposite + 8) % 8, (opposite - red + 8) % 8);
        var a = Distance(state.Red);
        var b = Distance(state.OtherRed);
        return a == b ? ((state.Red - opposite + 8) % 8 < 4 ? state.Red : state.OtherRed) : a < b ? state.Red : state.OtherRed;
    }
    public static int RangedRed(FruMirrorMirrorState state) => MeleeRed(state) == state.Red ? state.OtherRed : state.Red;
    public static Vector3 Pocket(Vector3 source, float octant, PartyRole role)
    {
        var angle = role switch
        {
            PartyRole.MainTank or PartyRole.RegenHealer => -72f,
            PartyRole.OffTank or PartyRole.ShieldHealer => 72f,
            PartyRole.MeleeDpsA or PartyRole.PhysRangedDps => -22f,
            _ => 22f,
        };
        return source + FruMirrorMirrorState.Point(octant + 4 + angle / 45f, 3f);
    }
    private static Vector3 FirstPocket(FruMirrorMirrorState state, Vector3 bossPosition, PartyRole role)
    {
        var mirror = FruMirrorMirrorState.Point(state.Blue, 20f);
        var source = MeleeGroup(role) ? bossPosition : mirror;
        var target = MeleeGroup(role) ? mirror : bossPosition;
        var direction = target - source;
        var facing = MathF.Atan2(direction.X, -direction.Z) / (MathF.PI / 4f);
        return Pocket(source, facing - (MeleeGroup(role) ? 0f : 4f), role);
    }
    public static Vector3 RedPocket(FruMirrorMirrorState state, PartyRole role)
    {
        var source = MeleeGroup(role) ? MeleeRed(state) : RangedRed(state);
        var other = MeleeGroup(role) ? RangedRed(state) : MeleeRed(state);
        var first = role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.PhysRangedDps;
        var angle = role.IsTank() || role is PartyRole.RegenHealer or PartyRole.ShieldHealer ? 72f : 22f;
        var sign = (other - source + 8) % 8 == 2 ? 1f : -1f;
        return FruMirrorMirrorState.Point(source, 20f) + FruMirrorMirrorState.Point(source + 4 + angle * sign * (first ? 1f : -1f) / 45f, 3f);
    }
    public static Vector3 BanishSpot(FruMirrorMirrorState state, PartyRole role)
    {
        var anchor = state.Pairs ? role switch
        {
            PartyRole.MeleeDpsA => PartyRole.MainTank,
            PartyRole.MeleeDpsB => PartyRole.OffTank,
            PartyRole.PhysRangedDps => PartyRole.RegenHealer,
            PartyRole.CasterDps => PartyRole.ShieldHealer,
            _ => role,
        } : role;
        var p = anchor switch
        {
            PartyRole.MainTank => new Vector3(-7, 0, -15),
            PartyRole.OffTank => new Vector3(7, 0, -15),
            PartyRole.RegenHealer => new Vector3(-7, 0, 2),
            PartyRole.ShieldHealer => new Vector3(7, 0, 2),
            PartyRole.MeleeDpsA => new Vector3(-14, 0, -7),
            PartyRole.MeleeDpsB => new Vector3(14, 0, -7),
            PartyRole.PhysRangedDps => new Vector3(-7, 0, 10),
            _ => new Vector3(7, 0, 10),
        };
        var a = MeleeRed(state) * MathF.PI / 4f;
        return new(p.X * MathF.Cos(a) - p.Z * MathF.Sin(a), 0, p.X * MathF.Sin(a) + p.Z * MathF.Cos(a));
    }
    public void Run(FruMirrorMirrorState state, SimWorld world) => Run(state, world, () => null);
    internal void Run(FruMirrorMirrorState state, SimWorld world, Func<SimEnemy?> boss)
    {
        var ai = new AiManager(world);
        IAiMove ByRole(Func<PartyRole, Vector3> spot) => AiMove.Create(Enum.GetValues<PartyRole>().Select(r => { var p = spot(r); return (Vector2?)new Vector2(p.X, p.Z); }).ToArray()).NaturalOrder();
        var meleeRed = MeleeRed(state);
        ai.Move(6.05f, () => ByRole(r => r == PartyRole.MainTank ? FruMirrorMirrorState.Point(state.Blue + 4, 19f)
            : Pocket(FruMirrorMirrorState.Point(MeleeGroup(r) ? state.Blue + 4 : state.Blue, MeleeGroup(r) ? 14f : 20f), MeleeGroup(r) ? state.Blue + 4 : state.Blue, r)), jitter: 0);
        ai.Move(12.06f, () => ByRole(r => FirstPocket(state, boss()?.Position ?? FruMirrorMirrorState.Point(state.Blue + 4, 14f), r)), jitter: 0, arrivalTime: world.BotDifficulty == BotDifficulty.Easy ? 17.3f : 17.9f);
        ai.Move(19f, () => ByRole(r => r == PartyRole.MainTank ? FruMirrorMirrorState.Point(meleeRed, 19f)
            : RedPocket(state, r)), jitter: 0);
        ai.Move(26.1f, () => ByRole(r => RedPocket(state, r)), jitter: 0, arrivalTime: world.BotDifficulty == BotDifficulty.Easy ? 28f : 28.5f);
        ai.Move(29f, () => ByRole(r => BanishSpot(state, r)), jitter: 0, arrivalTime: world.BotDifficulty == BotDifficulty.Easy ? 33.6f : 34f);
    }
}
