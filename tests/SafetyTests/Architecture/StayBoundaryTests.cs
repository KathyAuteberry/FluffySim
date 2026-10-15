using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;
using Assert = Xunit.Assert;

namespace AnoMech.SafetyTests.Architecture;

// The real character may only be moved by code that runs during a stay, while the filter holds
// whatever the move would tell the server. The firewall tests never run scenario or engine code,
// so this follows every call path in the built plugin instead: from every place code can start
// running (Dalamud callbacks, UI, timers, tasks, anything a delegate is handed to), no path may
// reach a move of the character, or put something in the sim world, without passing a check
// that a stay is running.
public class StayBoundaryTests
{
    private const string Game = "AnoMech.Core.Game.Game";
    private const string Map = "AnoMech.Core.Map.MapController";
    private const string World = "AnoMech.Core.SimObjects.SimWorld";

    // Calls that move the real character, or may: any SimCharacter can be the SimPlayer, which
    // always resolves to the live character.
    private static readonly HashSet<string> Moves =
    [
        "AnoMech.Core.SimObjects.SimCharacter::SetPosition",
        "AnoMech.Core.SimObjects.SimCharacter::SetRotation",
        "AnoMech.Core.Native.Implementations.ForcedMovement::CarryTo",
    ];

    // Native position writes elsewhere are moves too, except in the move methods' own bodies,
    // in these, which only ever touch objects the sim spawned, and in the zone session's own
    // (whose every write the firewall tests judge).
    private static readonly HashSet<string> SimObjectWriters =
    [
        "AnoMech.Core.SimObjects.SimEventObject::SetPosition",
        "AnoMech.Core.SimObjects.SimTower::Spawn",
        "AnoMech.Core.SimObjects.SimEnemy::Spawn",
        "AnoMech.Core.Game.Party.PartyCreator::SpawnNative",
        "AnoMech.Core.SimObjects.SimCast::FaceTarget",
    ];

    private const string ZoneControlWriter = "AnoMech.Core.Native.Implementations.ZoneControl::SetLocalPlayerPosition";

    // Checks that a stay is running. Each is true only between a verified arm and the lift:
    // IsInInstance and IsActive by the zone session's own design (the firewall tests check "in
    // instance implies both filters up" every frame), PeerInRun includes IsInInstance, TryLoad
    // succeeding is the arm, and Game.activeScenario is pinned by the tests below.
    private static bool IsStayCheck(Instruction i) => i.Operand switch
    {
        FieldReference f => i.OpCode.Code == Code.Ldfld && PluginIL.Name(f) == $"{Game}::activeScenario",
        MethodReference m => PluginIL.Name(m) is $"{Map}::get_IsInInstance" or "AnoMech.Core.Native.Implementations.ZoneControl::get_IsActive"
            or "AnoMech.Multiplayer.MultiplayerManager::get_PeerInRun" or $"{Map}::TryLoad" or $"{Game}::get_ActiveScenario" or $"{Game}::get_IsScenarioActive" or "AnoMech.Core.Game.InstanceSession::get_IsLoaded",
        _ => false,
    };

    // Work handed to these runs no later than the stay it was added in: every EventScheduler is
    // either the game's own, cleared when a run ends, or a scenario's, ticked only while that
    // scenario is the active one.
    private static bool IsStayBoundScheduler(string name) => name == "AnoMech.Core.Game.EventScheduler::Add";

    // Everything the world ticks, it holds; it holds the player only during a stay (the party is
    // made only after the zone loaded and dropped before it unloads) and nothing else outside
    // one but the reviewed entries below, none of them the player. Pinned by the tests below; so
    // the world's own tick counts as inside a stay.
    private const string WorldTick = $"{World}::Tick";

    // Code outside a stay that puts objects in the world, each reviewed: none of it is the player
    // or moves it.
    private static readonly Dictionary<string, string> ReviewedWorldEntries = new()
    {
        ["AnoMech.Core.Native.Implementations.TimelineDebug::SpawnChaosNearPlayer"] = "the debug timeline bench's test enemy (its controls are Debug-only)",
        ["AnoMech.Windows.DebugMenu::DrawDebugContent"] = "the debug menu's manual enemy and event-object spawns (Debug builds only)",
        ["AnoMech.Scenarios.Umad.P1TeleTrouncing.UmadP1TeleTrouncingScenario::FireAppearNow"] = "a settings button replaying the statue-appear tell: statue beats and effects",
        ["AnoMech.Scenarios.Umad.P1TeleTrouncing.UmadP1TeleTrouncingScenario::FireWindUpNow"] = "a settings button replaying the gaze wind-up tell: statue beats and effects",
    };

    [Fact]
    public void TheCharacterIsOnlyMovedFromCodeThatRunsDuringAStay()
    {
        var failures = PluginIL.Builds.SelectMany(b => Paths(b, (i, m) => IsMove(i, m), new Dictionary<string, string>()).Select(f => $"[{b}] {f}")).ToList();
        Assert.True(failures.Count == 0,
            "Code that can run outside a stay reaches a move of the real character with no check that a stay is running. Outside a stay nothing holds the move from the server: "
            + "move it into the scenario's timeline, or check IsInInstance first.\n\n" + string.Join("\n\n", failures));
    }

    [Fact]
    public void TheSimWorld_OnlyGainsObjectsDuringAStay()
    {
        var failures = PluginIL.Builds.SelectMany(b =>
        {
            var entries = WorldEntryMethods(b);
            return Paths(b, (i, m) => i.Operand is MethodReference r && entries.Contains(PluginIL.Name(r)) && PluginIL.TypeName(m.DeclaringType) != World, ReviewedWorldEntries)
                .Select(f => $"[{b}] {f}");
        }).ToList();
        Assert.True(failures.Count == 0,
            "Code that can run outside a stay puts objects into the sim world, whose tick then runs them outside a stay. Spawn from the scenario's timeline instead, "
            + "or, for a tool whose objects can never be or move the player, add the method to ReviewedWorldEntries with the reason and have it reviewed.\n\n" + string.Join("\n\n", failures));
    }

    // ---- the search ----------------------------------------------------------------------------

    private static IEnumerable<string> Paths(PluginIL il, Func<Instruction, MethodDefinition, bool> isSink, Dictionary<string, string> reviewed)
    {
        var graph = new CallGraph(il, IsStayBoundScheduler);
        var ungatedCache = new Dictionary<MethodDefinition, HashSet<Instruction>>();
        HashSet<Instruction> Ungated(MethodDefinition m)
        {
            if (ungatedCache.TryGetValue(m, out var set)) return set;
            if (PluginIL.Owner(m) == WorldTick) return ungatedCache[m] = new HashSet<Instruction>();
            var good = m.Body.Instructions.Where(IsStayCheck).Select(ControlFlow.OutcomeOf).Where(o => o != null).Select(o => (o!.Value.Branch, o.Value.WhenTruthy)).ToList();
            return ungatedCache[m] = new ControlFlow(m).FeasiblyReachableAvoiding(good);
        }

        var previous = new Dictionary<MethodDefinition, (MethodDefinition? From, string Root)>();
        var queue = new Queue<MethodDefinition>();
        foreach (var r in graph.Roots)
            if (previous.TryAdd(r.Method, (null, r.Why)))
                queue.Enqueue(r.Method);
        while (queue.Count > 0)
        {
            var m = queue.Dequeue();
            if (reviewed.ContainsKey(PluginIL.Owner(m))) continue;
            var ungated = Ungated(m);
            foreach (var e in graph.Callees(m))
                if (ungated.Contains(e.At) && previous.TryAdd(e.To, (m, previous[m].Root)))
                    queue.Enqueue(e.To);
        }

        foreach (var m in previous.Keys)
        {
            if (reviewed.ContainsKey(PluginIL.Owner(m))) continue;
            var ungated = Ungated(m);
            var sinks = m.Body.Instructions.Where(i => ungated.Contains(i) && isSink(i, m)).ToList();
            if (sinks.Count == 0) continue;
            var path = new List<string>();
            for (MethodDefinition? c = m; c != null; c = previous[c].From) path.Add(PluginIL.Owner(c));
            path.Reverse();
            path = path.Distinct().ToList();
            yield return $"{string.Join("\n  -> ", path)}\n  calls {string.Join(", ", sinks.Select(i => $"{PluginIL.Name((MethodReference)i.Operand)} (IL_{i.Offset:X4})").Distinct())}"
                         + $"\n  ({path[0]} {previous[m].Root})";
        }
    }

    private static bool IsMove(Instruction i, MethodDefinition m)
    {
        if (i.Operand is not MethodReference r || i.OpCode.Code is not (Code.Call or Code.Callvirt)) return false;
        var name = PluginIL.Name(r);
        var owner = PluginIL.Owner(m);
        if (Moves.Contains(name)) return !Moves.Contains(owner) && !MovesItselfAsAnNpc(i, m);
        return !Moves.Contains(owner) && !SimObjectWriters.Contains(owner) && owner != ZoneControlWriter && ArchitectureTests.IsNativeTransformCall(name);
    }

    // A move whose target is statically an NPC (SimNpc and below: an index into the game's
    // character array the sim claimed), which is never the player.
    private static bool MovesItselfAsAnNpc(Instruction call, MethodDefinition m)
    {
        var receivers = m.Body.Instructions.Where(p => CallGraph.Consumer(p, 0) is var (consumer, argument) && consumer == call && argument == -1)
            .Select(p => StaticType(p, m)).ToList();
        return receivers.Count > 0 && receivers.All(t => t != null && IsNpcType(t));
    }

    private static TypeReference? StaticType(Instruction p, MethodDefinition m) => p.OpCode.Code switch
    {
        Code.Ldarg_0 when m.HasThis => m.DeclaringType,
        Code.Ldarg_0 or Code.Ldarg_1 or Code.Ldarg_2 or Code.Ldarg_3 or Code.Ldarg_S or Code.Ldarg => Parameter(p, m)?.ParameterType,
        Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 => m.Body.Variables[p.OpCode.Code - Code.Ldloc_0].VariableType,
        Code.Ldloc_S or Code.Ldloc => ((VariableDefinition)p.Operand).VariableType,
        Code.Ldfld or Code.Ldsfld => ((FieldReference)p.Operand).FieldType,
        Code.Call or Code.Callvirt => ((MethodReference)p.Operand).ReturnType,
        Code.Newobj => ((MethodReference)p.Operand).DeclaringType,
        Code.Castclass or Code.Isinst => (TypeReference)p.Operand,
        _ => null,
    };

    private static ParameterDefinition? Parameter(Instruction p, MethodDefinition m)
    {
        var index = p.OpCode.Code switch
        {
            Code.Ldarg_0 => 0,
            Code.Ldarg_1 => 1,
            Code.Ldarg_2 => 2,
            Code.Ldarg_3 => 3,
            _ => ((ParameterDefinition)p.Operand).Index + (m.HasThis ? 1 : 0),
        } - (m.HasThis ? 1 : 0);
        return index >= 0 && index < m.Parameters.Count ? m.Parameters[index] : null;
    }

    private static bool IsNpcType(TypeReference t)
    {
        TypeDefinition? c;
        try
        {
            c = t.Resolve();
        }
        catch (AssemblyResolutionException)
        {
            return false;
        }
        while (c != null)
        {
            if (PluginIL.TypeName(c) == "AnoMech.Core.SimObjects.SimNpc") return true;
            try
            {
                c = c.BaseType?.Resolve();
            }
            catch (AssemblyResolutionException)
            {
                return false;
            }
        }
        return false;
    }

    // SimWorld methods that add to the objects it ticks.
    private static HashSet<string> WorldEntryMethods(PluginIL il)
        => il.Module.GetTypes().Where(t => PluginIL.TypeName(t) == World).SelectMany(t => t.Methods).Where(m => m.HasBody
                && m.Body.Instructions.Any(i => i.Operand is FieldReference f && f.Name == "children")
                && m.Body.Instructions.Any(i => i.Operand is MethodReference r && PluginIL.Name(r) == "System.Collections.Generic.List::Add"))
            .Select(m => $"{World}::{m.Name}").ToHashSet();

    // ---- what makes Game.activeScenario a stay check ------------------------------------------

    [Fact]
    public void TheActiveScenario_IsOnlySetAfterTheZoneLoaded()
    {
        foreach (var il in PluginIL.Builds)
        {
            var writers = il.Methods.Where(m => m.Body.Instructions.Any(i => i.OpCode.Code == Code.Stfld
                && i.Operand is FieldReference f && PluginIL.Name(f) == $"{Game}::run" && i.Previous?.OpCode.Code != Code.Ldnull));
            Assert.Equal(new[] { $"{Game}::StartScenarioNow" }, writers.Select(PluginIL.Owner).ToArray());
            var start = il.Method(Game,"StartScenarioNow");
            var create = start.Body.Instructions.Single(i => i.Operand is MethodReference m && PluginIL.Name(m) == "AnoMech.Core.Game.ScenarioSession::Create");
            var store = start.Body.Instructions.Single(i => i.OpCode.Code == Code.Stfld && i.Operand is FieldReference f && PluginIL.Name(f) == $"{Game}::run");
            Assert.True(create.Offset < store.Offset);
            var refusal = il.Method("AnoMech.Core.Game.ScenarioSession", "Refusal");
            Assert.Contains(refusal.Body.Instructions, i => i.Operand is MethodReference m && PluginIL.Name(m) == "AnoMech.Core.Game.InstanceSession::get_IsLoaded");
        }
    }

    [Fact]
    public void TheActiveScenarioAndTheWorld_AreClearedBeforeTheZoneUnloads()
    {
        foreach(var il in PluginIL.Builds)
        {
            var end = il.Method(Game,"EndRun");
            var clear = end.Body.Instructions.Single(i => i.OpCode.Code == Code.Stfld && i.Operand is FieldReference f && PluginIL.Name(f) == $"{Game}::run");
            var stop = end.Body.Instructions.Single(i => i.Operand is MethodReference m && PluginIL.Name(m) == "AnoMech.Core.Game.ScenarioSession::End");
            Assert.True(clear.Offset < stop.Offset);
            Assert.Contains(il.Method("AnoMech.Core.Game.ScenarioSession","End").Body.Instructions, i => i.Operand is MethodReference m && PluginIL.Name(m) == $"{World}::Despawn");
            var exit=il.Method("AnoMech.Core.Game.InstanceSession","Exit");
            var drop=exit.Body.Instructions.Single(i=>i.Operand is MethodReference m && PluginIL.Name(m)==$"{World}::DespawnParty");
            var revert=exit.Body.Instructions.Single(i=>i.Operand is MethodReference m && PluginIL.Name(m)=="AnoMech.Core.Native.Interfaces.IZoneControl::Revert");
            Assert.True(new ControlFlow(exit).EveryPathTakes(revert,drop,drop.Next));
        }
    }

    [Fact]
    public void TheSimPlayer_IsOnlyMadeForARun()
    {
        foreach(var il in PluginIL.Builds)
        {
            Assert.Equal(new[] { "AnoMech.Core.SimObjects.SimParty::Apply" }, il.CallersOf(@"^AnoMech\.Core\.SimObjects\.SimPlayer::\.ctor$").ToArray());
            Assert.Equal(new[] { $"{World}::SeatParty" }, il.CallersOf(@"^AnoMech\.Core\.SimObjects\.SimParty::Apply$").ToArray());
            var callers=il.CallersOf($"^{Regex.Escape(World)}::SeatParty$");
            Assert.Equal(new[] { $"{Game}::SeatParty", $"{World}::ApplyParty" }, callers.OrderBy(x=>x).ToArray());
            Assert.Equal(new[] { "AnoMech.Core.Game.ScenarioSession::Start" }, il.CallersOf($"^{Regex.Escape(World)}::ApplyParty$").ToArray());
        }
    }
}
