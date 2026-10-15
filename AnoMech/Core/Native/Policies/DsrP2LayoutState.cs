using System;
using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Core.Native.Policies;

internal static class DsrP2LayoutState
{
    private static readonly (ushort Layer, int Count, ulong State)[] Expected =
    [
        (2995, 44, 0xA67ED4E18A128697UL),
        (3997, 12, 0xB38B1D10DF18B9CDUL),
        (5023, 1, 0xC60E714E5F85C7BBUL),
        (5644, 1, 0xC70EC64E6139582AUL),
        (60988, 578, 0xC610DC6E66FF3957UL),
        (60990, 23, 0x30FE80DD2B82B2E5UL),
        (60991, 46, 0x8E8D640915048A97UL),
        (60992, 4, 0x63212CC4C0D43B0DUL),
        (61049, 262, 0xBDA14EF98C23392DUL),
        (61050, 19, 0x34A30DB0BC3CC554UL),
        (62172, 39, 0x1C72BD5CEDBD6BF1UL),
        (62176, 3, 0xD78E8345E28C20FEUL),
        (65362, 1, 0xB2A3374E3E86942DUL),
    ];

    internal static bool Matches(Func<ushort, IReadOnlyList<(uint Id, bool Active)>> read)
    {
        foreach (var expected in Expected)
        {
            var objects = read(expected.Layer);
            if (objects.Count != expected.Count) return false;
            var state = 14695981039346656037UL;
            foreach (var obj in objects.OrderBy(o => o.Id))
                state = unchecked((state ^ (((ulong)obj.Id << 1) | (obj.Active ? 1UL : 0UL))) * 1099511628211UL);
            if (state != expected.State) return false;
        }
        return true;
    }
}
