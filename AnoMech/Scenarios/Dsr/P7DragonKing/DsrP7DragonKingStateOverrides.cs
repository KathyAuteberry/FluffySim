using System.Collections.Generic;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

public sealed class DsrP7DragonKingStateOverrides
{
    public IReadOnlyList<IReadOnlyList<int>>? ExaflareHeadings { get; set; }
    public AscalonElement? ForcedElement { get; set; }
    public IReadOnlyList<AscalonElement>? Elements { get; set; }
    public IReadOnlyList<IReadOnlyList<int>>? GigaflareOrder { get; set; }
}
