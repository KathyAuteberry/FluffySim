using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained.FruP5ParadiseRegainedState;

namespace AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained;

public sealed class FruP5ParadiseRegainedAi : IScenarioAi<FruP5ParadiseRegainedState>
{
    public string Name => "Nukemaru";
    public string Group => "Mana";

    private readonly bool polarizingOnly;
    private readonly float polarizingOffset;
    public FruP5ParadiseRegainedAi(bool polarizingOnly = false, float polarizingOffset = 0f)
    {
        this.polarizingOnly = polarizingOnly;
        this.polarizingOffset = polarizingOffset;
    }

    private FruP5ParadiseRegainedState state = null!;

    public void Run(FruP5ParadiseRegainedState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        if (!polarizingOnly)
        {
            if (state.RequireInvulnerability)
                world.Events.Add(13f, () =>
                {
                    BotActions.UseInvulnerability(world, PartyRole.MainTank);
                    BotActions.UseInvulnerability(world, PartyRole.OffTank);
                });
            ai.Move(2f, () => ByRole(StackUnderFirstTower), jitter: 0f, arrivalTime: 8.3f);
            ai.Move(10f, () => ByRole(role => state.Rotate(FirstWingSpot(role))), jitter: 0f, arrivalTime: TowerHitAt(0));
            ai.Move(16.5f, () => ByRole(role => state.Rotate(SecondWingSpot(role))), jitter: 0f, arrivalTime: TowerHitAt(1));
        }
        var offset = (polarizingOnly ? -29.35f : 0f) + polarizingOffset;
        ai.Move(polarizingOnly ? 0f : MathF.Max(23.4f, 25.5f + offset), () => ByRole(PolarizingPreposition), jitter: 0f, arrivalTime: 30f + offset);
        ai.Move(30f + offset, () => ByRole(role => LineSpot(role, 0, dodged: false)), jitter: 0f, arrivalTime: LinesAimedAt(0) + offset);
        for (var round = 0; round < 4; round++)
        {
            var r = round;
            if (r > 0) ai.Move(LinesAt(r) - 1.4f + offset, () => ByRole(role => LineSpot(role, r, dodged: false)), jitter: 0f, arrivalTime: LinesAimedAt(r) + offset);
            ai.Move(LinesAt(r) + 1.2f + offset, () => ByRole(role => LineSpot(role, r, dodged: true)), jitter: 0f, arrivalTime: EchoAt(r) + offset);
        }
    }

    private static float LinesAimedAt(int round) => LinesAt(round) - 0.1f;

    private Vector3? StackUnderFirstTower(PartyRole role) => state.Rotate(new Vector3(((int)role - 3.5f) * 0.35f, 0f, 7f));

    private Vector3 FirstWingSpot(PartyRole role) => role switch
    {
        PartyRole.MainTank => new(0f, 0f, -7f),
        PartyRole.OffTank => state.DarkFirst ? new(-1f, 0f, 1f) : new(10f, 0f, 10f),
        PartyRole.RegenHealer => new(state.DarkFirst ? -1f : 1f, 0f, 7f),
        PartyRole.ShieldHealer => new(state.DarkFirst ? -2f : 2f, 0f, 7f),
        PartyRole.MeleeDpsA => new(state.DarkFirst ? -8f : 8f, 0f, 0f),
        PartyRole.PhysRangedDps => new(state.DarkFirst ? -8.5f : 8.5f, 0f, 0f),
        PartyRole.MeleeDpsB => new(state.DarkFirst ? -8f : 8f, 0f, -0.5f),
        _ => new(state.DarkFirst ? -8.5f : 8.5f, 0f, -0.5f),
    };

    private Vector3 SecondWingSpot(PartyRole role) => role switch
    {
        PartyRole.MainTank => new(0f, 0f, state.DarkFirst ? -14f : -1f),
        PartyRole.OffTank => new(state.DarkFirst ? -6.0622f : 6.0622f, 0f, 3.5f),
        PartyRole.RegenHealer => new(-0.65f, 0f, -7.5f),
        PartyRole.ShieldHealer => new(0.65f, 0f, -7.5f),
        PartyRole.MeleeDpsA => new(state.DarkFirst ? 5f : -5f, 0f, -4.5f),
        PartyRole.MeleeDpsB => new(state.DarkFirst ? 5.2f : -5.2f, 0f, -4.7f),
        PartyRole.PhysRangedDps => new(state.DarkFirst ? -5f : 5f, 0f, -4.5f),
        _ => new(state.DarkFirst ? -5.2f : 5.2f, 0f, -4.7f),
    };

    public static int? AssignedTower(FruP5ParadiseRegainedState state, PartyRole role) => role switch
    {
        PartyRole.RegenHealer or PartyRole.ShieldHealer => 0,
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => state.EastSecond == state.DarkFirst ? 1 : 2,
        PartyRole.PhysRangedDps or PartyRole.CasterDps => state.EastSecond == state.DarkFirst ? 2 : 1,
        _ => null,
    };

    private static int BaitOrder(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.OffTank => 0,
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => 1,
        PartyRole.PhysRangedDps or PartyRole.CasterDps => 2,
        _ => 3,
    };

    private static bool StartsWest(PartyRole role)
        => role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.PhysRangedDps;

    private static Vector3? PolarizingPreposition(PartyRole role)
        => BaitOrder(role) == 0 ? new Vector3(0f, 0f, -3.5f) : LineSpot(role, 0, dodged: false);

    private static Vector3? LineSpot(PartyRole role, int round, bool dodged)
    {
        var order = BaitOrder(role);
        var crossed = dodged ? order <= round : order < round;
        var sign = StartsWest(role) != crossed ? -1f : 1f;
        if (!dodged)
        {
            var depth = order == round ? 4f : 6f;
            return new Vector3(sign * depth, 0f, depth);
        }
        var next = order == Math.Min(round + 1, 3);
        return new Vector3(sign * (next ? 0.6f : 1.8f), 0f, next ? 6.2f : 8.5f);
    }

    private static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();
}
