using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeActionTimelinePreload : IActionTimelinePreload
{
    public List<(ushort Id, string Key)> Rows { get; } = [];
    public void Preload(IEnumerable<(ushort Id, string Key)> rows, string tag) => Rows.AddRange(rows);
}
