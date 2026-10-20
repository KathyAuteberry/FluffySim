using System;
using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Core.Native.Policies;

internal static class DsrEnvironmentState
{
    internal static bool NeedsFinalization(uint territory, byte weather, bool offline)
        => territory == 968 && (weather is 2 or 3 || offline && weather == 45);

    private static readonly (ushort Layer, int Count, ulong State)[] Nidhogg =
    [
        (5210, 1, 0xC64FA84E5FF49830UL),
        (5212, 1, 0xC64FAB4E5FF49D49UL),
        (5644, 1, 0xC70EC74E613959DDUL),
        (61049, 262, 0xB93E75F36A7E5B4FUL),
        (61060, 53, 0x2EE8F9D022EF8298UL),
        (61063, 47, 0xE47AC018B7D2AFE4UL),
        (61064, 2, 0x8D4715F2F62A3415UL),
        (61065, 351, 0x211D35B67FFBBCECUL),
        (61068, 6, 0x92390F6FD68B6ACDUL),
        (61379, 13, 0xDE25C00AC38CE656UL),
        (61380, 6, 0x862C40838BA17599UL),
        (62172, 39, 0x1C72BD5CEDBD6BF1UL),
        (65362, 1, 0xB2A3364E3E86927AUL),
    ];

    private static readonly (ushort Layer, int Count, ulong State)[] HolySee =
    [
        (5210, 1, 0xC64FA84E5FF49830UL),
        (5212, 1, 0xC64FAB4E5FF49D49UL),
        (5644, 1, 0xC70EC74E613959DDUL),
        (61049, 262, 0xB93E75F36A7E5B4FUL),
        (61060, 53, 0xA6B50EB054E3752BUL),
        (61063, 47, 0x87959A3FEF5424C5UL),
        (61064, 2, 0x8D4672F2F6263BF3UL),
        (61065, 351, 0x8323BA56AD598221UL),
        (61068, 6, 0xA8DC9A53BFE9D157UL),
        (61379, 13, 0xFDBE3F28A381C525UL),
        (61380, 6, 0xDD0E766EA735B793UL),
        (62172, 39, 0xDE50A87C75B9C74CUL),
        (65362, 1, 0xB2A3364E3E86927AUL),
    ];

    internal static bool MatchesHolySee(Func<ushort, IReadOnlyList<(uint Id, bool Active, bool ColliderLoaded, bool ColliderActive)>> read)
        => Matches(HolySee, read);

    internal static bool MatchesNidhogg(Func<ushort, IReadOnlyList<(uint Id, bool Active, bool ColliderLoaded, bool ColliderActive)>> read)
        => Matches(Nidhogg, read);

    private static bool Matches((ushort Layer, int Count, ulong State)[] expectedLayers, Func<ushort, IReadOnlyList<(uint Id, bool Active, bool ColliderLoaded, bool ColliderActive)>> read)
    {
        foreach (var expected in expectedLayers)
        {
            var objects = read(expected.Layer);
            if (objects.Count != expected.Count) return false;
            var state = 14695981039346656037UL;
            foreach (var obj in objects.OrderBy(o => o.Id))
            {
                state = unchecked((state ^ (((ulong)obj.Id << 1) | (obj.Active ? 1UL : 0UL))) * 1099511628211UL);
                if ((expected.Layer is 65362 or 5210 or 5212 or 5644) && (!obj.ColliderLoaded || obj.ColliderActive != obj.Active))
                    return false;
            }
            if (state != expected.State) return false;
        }
        return true;
    }
}
