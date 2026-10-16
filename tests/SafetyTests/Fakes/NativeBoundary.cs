using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using AnoMech.Core.Native.Implementations;
using AnoMech.SafetyTests.Harness;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;

namespace AnoMech.Core.Native.Interfaces
{
    internal static class Natives
    {
        public static ZoneControl Zone => ZoneControl.Current!;
        public static FakeInput PlayerInput { get; } = new();
        public static FakeData Data { get; } = new();
        public static MapEffects MapEffects { get; } = new();
        public static FakeLayout Layout { get; } = new();
        public static FakeDirector Director { get; } = new();
        public static FakeBattleCharas BattleCharas { get; } = new();
    }
    internal class FakeInput { public bool TransitionInput { get; set; } }
    internal class FakeData { public ClassJob? ClassJob(uint id) => VirtualGame.Current.Sheet<ClassJob>().GetRowOrDefault(id); public string ActionName(uint id) => AnoMech.Core.ActionLookup.Name(id); }
    internal class FakeLayout
    {
        public void ClearSuppressedLayers() { }
        public void ReassertSuppressedLayers() { }
        public int SuppressLayer(ushort layer) => 0;
        public void HoldLayerActive(uint territory,ushort layer,bool active) { }
        public void HoldInstanceActive(uint territory,ushort layer,uint instance,bool active) { }
        public string DescribeActiveLayers() => "fake";
        public string DescribeLiveEffects() => "fake";
    }
    internal class FakeDirector
    {
        public bool ProcessDirectorUpdate(uint category,uint a,uint b,uint c,uint d,uint e,uint f)
            => VirtualGame.Current.NativeDirectorUpdate(category);
    }
    internal class FakeBattleCharas { public FakeChara LocalPlayer => new(); }
    internal class FakeChara
    {
        public bool Exists => Plugin.ObjectTable.LocalPlayer != null;
        public Vector3 Position => Plugin.ObjectTable.LocalPlayer?.Position ?? default;
    }
}
namespace AnoMech.Core.Native.Implementations
{
    // Level sync is outside this transport harness; the production zone/filter source is linked unchanged.
    internal sealed unsafe class LevelSync
    {
        public void Capture() { }
        public void Apply(ContentFinderCondition cfc,Character* player,byte job) => VirtualGame.Current.NativeUpdateClassInfo(cfc.ClassJobLevelSync);
        public void Reset(Character* player,byte job) => VirtualGame.Current.NativeUpdateClassInfo(0);
    }
    internal class BattleCharaProxy { public static object LocalPlayer => new(); }
}
namespace AnoMech.Core.Game
{
    internal enum CastCancelReason { SelfCancelled }
    internal class ActorControl
    {
        public ActorControl(Func<object> actor) { _ = actor; }
        public void CancelCast(uint id, CastCancelReason reason) => VirtualGame.Current.CancelCast("local cancellation");
    }
}
namespace AnoMech.Offline { internal static class OfflineSession { public static bool InWorld => false; } }
namespace FFXIVClientStructs.FFXIV.Client.Game
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RecastDetail { public bool IsActive; public uint ActionId; public float Elapsed,Total; }
    public unsafe struct ActionManager
    {
        private static readonly ActionManager* instance = (ActionManager*)NativeMemory.AllocZeroed((nuint)sizeof(ActionManager));
        private static readonly RecastDetail* recasts = (RecastDetail*)NativeMemory.AllocZeroed(80,(nuint)sizeof(RecastDetail));
        public ushort LastUsedActionSequence,LastHandledActionSequence;
        public bool ActionQueued, AreaTargetingExecuteAtCursor;
        public float AnimationLock;
        public uint AreaTargetingActionId;
        public static ActionManager* Instance() => instance;
        public int GetRecastGroup(int type,uint action) => (int)(action % 80);
        public RecastDetail* GetRecastGroupDetail(int group) => group is >= 0 and < 80 ? recasts+group : null;
    }
}
