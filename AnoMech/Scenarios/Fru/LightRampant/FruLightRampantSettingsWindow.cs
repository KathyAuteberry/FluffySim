using System.Linq;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.LightRampant;

public sealed class FruLightRampantSettingsWindow
{
    public FruLightRampantStateOverrides Overrides { get; } = new();
    private static readonly string[] Pairs = new[] { "Random" }.Concat(
        FruLightRampantState.PuddlePairs.Select(p => $"{p.A} + {p.B}")).ToArray();
    public void Draw()
    {
        ImGui.TextWrapped("Provisional: native arena/tower/orb appearance needs in-game validation. Ring starting formation only; chain cutoff uses BossMod's 25y rule. Puddle entry is a practice KO; Bleed tick damage and the precise House of Light cone edge are unverified.");
        var pair = (Overrides.PuddlePair ?? -1) + 1;
        if (ImGui.Combo("Puddle targets", ref pair, Pairs, Pairs.Length)) Overrides.PuddlePair = pair == 0 ? null : pair - 1;
        var banish = Overrides.Spread is { } spread ? (spread ? 2 : 1) : 0;
        if (ImGui.Combo("Banish III", ref banish, "Random\0Pairs\0Spread\0")) Overrides.Spread = banish == 0 ? null : banish == 2;
        var orbs = Overrides.NorthOrbFirst is { } north ? (north ? 1 : 2) : 0;
        if (ImGui.Combo("First orb trio", ref orbs, "Random\0North + SW + SE\0South + NW + NE\0")) Overrides.NorthOrbFirst = orbs == 0 ? null : orbs == 1;
    }
}
