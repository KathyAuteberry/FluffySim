using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.TopNative.HelloWorld;

// What each player starts Hello World resolving; every loop the four shift on.
public enum HelloRole { Overflow, Synchronization, Local, Remote }

public sealed class TopHelloWorldStateOverrides
{
    public PerRoleSetting<HelloRole> FirstRole { get; set; } = new();
    public bool? OverflowWithBlue { get; set; }

    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;
        foreach (var role in new[] { HelloRole.Overflow, HelloRole.Synchronization, HelloRole.Local, HelloRole.Remote })
            conflicts.AtMost(2, PerRole.All.Where(r => FirstRole[r] == role).ToList(), role.ToString());
        return conflicts;
    }
}

public sealed record HelloTowers(bool Intercardinal, int Rotation)
{
    // The two towers of the Overflows' colour first, clockwise; then the other colour's.
    public float Bearing(int i) => (Intercardinal ? 45f : 0f) + 90f * ((Rotation + i) % 4);
}

public sealed class TopHelloWorldState
{
    public const int Loops = 4;

    // Pair p's role in loop k is RoleInLoop[k][p]: the pairs hand the roles on in this order.
    public static readonly HelloRole[][] RoleInLoop =
    [
        [HelloRole.Overflow, HelloRole.Synchronization, HelloRole.Local, HelloRole.Remote],
        [HelloRole.Remote, HelloRole.Local, HelloRole.Overflow, HelloRole.Synchronization],
        [HelloRole.Synchronization, HelloRole.Overflow, HelloRole.Remote, HelloRole.Local],
        [HelloRole.Local, HelloRole.Remote, HelloRole.Synchronization, HelloRole.Overflow],
    ];

    public IReadOnlyDictionary<PartyRole, int> Pair { get; }
    public bool OverflowWithBlue { get; }
    public IReadOnlyList<HelloTowers> Towers { get; }

    public TopHelloWorldState(Rng rng, SimParty party, TopHelloWorldStateOverrides overrides)
    {
        var slots = overrides.FirstRole.Resolve(party.PlayerRole).ToDictionary(r => r.Role, r => new[] { 2 * (int)r.Value, 2 * (int)r.Value + 1 });
        var order = new RoleListBuilder { Slots = slots }.Build(rng, party);
        Pair = Enumerable.Range(0, 8).ToDictionary(i => order[i], i => i / 2);
        OverflowWithBlue = overrides.OverflowWithBlue ?? rng.NextBool();
        Towers = Enumerable.Range(0, Loops).Select(_ => new HelloTowers(rng.NextBool(), rng.Next(4))).ToArray();
        DiagnosticLog.Info($"[TopHelloWorld] pairs {string.Join(",", Pair.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"))}; overflow blue {OverflowWithBlue}; towers {string.Join(" ", Towers)}");
    }

    private TopHelloWorldState(Dictionary<PartyRole, int> pair, bool overflowWithBlue, HelloTowers[] towers)
    {
        Pair = pair;
        OverflowWithBlue = overflowWithBlue;
        Towers = towers;
    }

    public static TopHelloWorldState? FromNetworkReplay(Dictionary<PartyRole, int> pair, bool overflowWithBlue, bool[] intercardinal, int[] rotation)
        => intercardinal.Length == Loops && rotation.Length == Loops
            ? new(pair, overflowWithBlue, Enumerable.Range(0, Loops).Select(i => new HelloTowers(intercardinal[i], rotation[i])).ToArray())
            : null;

    public HelloRole RoleOf(PartyRole role, int loop) => RoleInLoop[loop][Pair[role]];

    public IReadOnlyList<PartyRole> WithRole(HelloRole helloRole, int loop, IReadOnlyList<PartyRole> priority)
        => priority.Where(r => RoleOf(r, loop) == helloRole).ToArray();
}
