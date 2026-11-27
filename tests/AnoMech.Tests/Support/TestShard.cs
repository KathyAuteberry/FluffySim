using System.Text;
using NUnit.Framework.Interfaces;

[assembly: AnoMech.Tests.TestShard]

namespace AnoMech.Tests;

// CI splits the suite across jobs with ANOMECH_TEST_SHARD=<index>/<count> (1-based). Tests share
// static game state, so they cannot run in parallel inside one process; separate jobs can. The hash
// is FNV-1a because string.GetHashCode is randomized per process.
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class TestShardAttribute : Attribute, ITestAction
{
    private static readonly (int Index, int Count)? Shard = Parse(Environment.GetEnvironmentVariable("ANOMECH_TEST_SHARD"));

    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test)
    {
        if (Shard is { } shard && Bucket(test.FullName, shard.Count) != shard.Index - 1)
            Assert.Ignore($"Runs in another shard than {shard.Index}/{shard.Count}.");
    }

    public void AfterTest(ITest test) { }

    public static int Bucket(string name, int count)
    {
        var hash = 2166136261u;
        foreach (var b in Encoding.UTF8.GetBytes(name)) hash = (hash ^ b) * 16777619u;
        return (int)(hash % (uint)count);
    }

    private static (int, int)? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split('/');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var index) || !int.TryParse(parts[1], out var count)
            || count < 1 || index < 1 || index > count)
            throw new InvalidOperationException($"ANOMECH_TEST_SHARD must be <index>/<count>, got '{value}'.");
        return (index, count);
    }
}
