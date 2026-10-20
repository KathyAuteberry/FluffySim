using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game;

public sealed record SoftMarkerView(Sign Sign, uint IconId, Vector3 Position);

public sealed class SoftMarkers
{
    private readonly Dictionary<Sign, SimCharacter> markers = [];
    public int Count => markers.Count;
    public void Set(Sign sign, SimCharacter character)
    {
        IconId(sign);
        markers[sign] = character;
    }
    public void Clear() => markers.Clear();
    public void RemoveInactive()
    {
        foreach (var sign in markers.Where(m => !m.Value.IsActive || !m.Value.IsAlive()).Select(m => m.Key).ToArray()) markers.Remove(sign);
    }
    public IReadOnlyList<SoftMarkerView> Snapshot(Vector3 origin, bool showBots = true) => markers
        .Where(m => m.Value.IsActive && m.Value.IsAlive() && (showBots || m.Value is not SimPartyNpc))
        .Select(m => new SoftMarkerView(m.Key, IconId(m.Key), origin + m.Value.Position + new Vector3(0f, 2.8f, 0f))).ToArray();
    public static uint IconId(Sign sign) => sign switch
    {
        Sign.Attack1 => 61201, Sign.Attack2 => 61202, Sign.Attack3 => 61203, Sign.Attack4 => 61204,
        Sign.Bind1 => 61211, Sign.Bind2 => 61212, Sign.Bind3 => 61213,
        Sign.Ignore1 => 61221, Sign.Ignore2 => 61222,
        _ => throw new ArgumentOutOfRangeException(nameof(sign)),
    };
}
