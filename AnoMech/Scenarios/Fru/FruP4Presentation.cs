using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.Upstream.FruConstants;

namespace AnoMech.Scenarios.Fru;

internal static class FruP4Presentation
{
    public static IReadOnlyList<SimEnemy> SpawnVisions(SimWorld world)
    {
        var actors = new List<SimEnemy>();
        Spawn(BNpcBaseId.VisionOfGaia, BNpcNameId.Gaia, Appearance.VisionOfGaia,
            new(new Vector3(-0.71f, 0, -16), 1.22f), 0x4020B, LayoutId.VisionOfGaia, 0x1F);
        Spawn(BNpcBaseId.VisionOfRyne, BNpcNameId.Ryne, Appearance.VisionOfRyne,
            new(new Vector3(0.71f, 0, -16), -0.84f), 0x4020A, LayoutId.VisionOfRyne, 0x05);
        return actors;

        void Spawn(uint baseId, uint name, NpcAppearance appearance, Placement placement, uint flags, uint layout, byte modelState)
        {
            if (world.SpawnEnemy(new EnemySpawnConfig(baseId, name, Level: Level,
                EnemyList: EnemyListMode.Never, Placement: placement, DisplayFlags: flags,
                LayoutId: layout, IsHostile: false, Appearance: appearance,
                Statuses: [new(StatusId.Levitation, 200)], ModelState: modelState)) is { } actor)
                actors.Add(actor);
        }
    }
}
