using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using Constants = AnoMech.Scenarios.Fru.Upstream.FruConstants;

namespace AnoMech.Scenarios.Fru.Pandora;

public sealed class FruPandorasBoxScenario : IScenario
{
    public string Name => "Pandora's Box";
    public IPhase Phase => FruZone.P5;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruPandorasBoxAi()];
    public const float CastAt = 1f;
    public const float BotLimitBreakAt = 10f;
    public const uint ActionId = 40326;
    private SimEnemy? pandora;
    internal SimEnemy? SharedPandora { get; set; }

    private static readonly ushort[] TankLimitBreakThree = [196, 863, 864, 1931];
    private static readonly EnemyAction Box = new(ActionId)
    {
        Effects = [Resolve(ctx =>
        {
            foreach (var member in ctx.Hits)
            {
                if (!HasTankLimitBreakThree(member)) ctx.Kill(member, "Pandora's Box requires active tank LB3");
                ctx.ShowDamage(member, HasTankLimitBreakThree(member) ? 60000u : 999999u, DamageKind.Magic);
            }
        })],
        Timing = new() { DamageDelay = 0.3f },
    };

    internal static bool HasTankLimitBreakThree(SimCharacter member)
        => System.Array.Exists(TankLimitBreakThree, member.HasStatus);

    public void Run(SimWorld world, int? selectedAi)
    {
        if (selectedAi is not null) new FruPandorasBoxAi().Run(world);
        world.Events.Add(0f, () =>
        {
            world.Party.LimitBreak.Set(3f);
            pandora = SharedPandora ?? world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: Constants.BNpcBaseId.Pandora, NameId: Constants.BNpcNameId.Pandora,
                Level: 100, Targetable: true, EnemyList: EnemyListMode.Always,
                Placement: new Placement(Vector3.Zero, System.MathF.PI), LayoutId: Constants.LayoutId.PandoraP5));
        });
        world.Events.Add(CastAt, () => pandora?.Cast(Box));
    }
}
