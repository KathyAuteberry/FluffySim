using System.Globalization;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeVfxFunctions : IVfxFunctions
{
    public sealed record StaticSpawn(string Path, Placement Placement, Vector3 Scale, FakeVfxHandle Handle);
    public List<StaticSpawn> StaticSpawns { get; } = [];
    public bool FailStaticSpawns { get; set; }
    public IStaticVfxProxy? SpawnStatic(string path, Placement placement, Vector3 scale)
    {
        if (FailStaticSpawns) return null;
        var handle = new FakeVfxHandle();
        StaticSpawns.Add(new(path,placement,scale,handle));
        return handle;
    }
    public string? LockonIconName(uint lockonId) => lockonId.ToString(CultureInfo.InvariantCulture);
}
