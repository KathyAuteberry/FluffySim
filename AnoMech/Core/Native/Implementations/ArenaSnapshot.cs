using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AnoMech.Core.Native.Implementations.Interop;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Node;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;

namespace AnoMech.Core.Native.Implementations;

internal static unsafe class ArenaSnapshot
{
    internal static bool IsAllowedLabel(string label) => label.Length is > 0 and <= 64
        && label.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    internal static bool IsReady(uint territory, uint layoutTerritory, int initState, uint loadState) =>
        territory != 0 && territory == layoutTerritory && initState == 7 && loadState == 2;

    internal static string FileName(string label, DateTime utc) => $"{utc:yyyyMMdd-HHmmss-fffffff}-{label}-{Guid.NewGuid():N}.json";

    internal static string Save(string label)
    {
        if (!IsAllowedLabel(label)) throw new ArgumentException("Use a label of 1–64 ASCII letters, digits, hyphens or underscores.");
        var game = GameMain.Instance();
        var world = LayoutWorld.Instance();
        if (game == null || world == null || world->ActiveLayout == null)
            throw new InvalidOperationException("No active layout.");
        var layout = world->ActiveLayout;
        if (!IsReady(game->CurrentTerritoryTypeId, layout->TerritoryTypeId, layout->InitState, game->TerritoryLoadState))
            throw new InvalidOperationException("Wait for the active territory and layout to finish loading.");

        var filters = new List<object>();
        foreach (var entry in layout->Filters)
        {
            if (filters.Count >= 256) throw new InvalidOperationException("Unexpected filter count.");
            var filter = entry.Item2.Value;
            if (filter != null) filters.Add(new { TableKey = entry.Item1, Key = filter->Key, TerritoryTypeId = filter->TerritoryTypeId, CfcId = filter->CfcId });
        }
        var layers = new List<object>();
        var budget = 20000;
        foreach (var entry in layout->Layers)
        {
            if (layers.Count >= 256) throw new InvalidOperationException("Unexpected layer count.");
            var layer = entry.Item2.Value;
            if (layer == null) continue;
            var instances = new List<object>();
            foreach (var item in layer->Instances)
            {
                if (item.Item2.Value != null) instances.Add(ReadInstance(item.Item2.Value, 0, ref budget));
            }
            layers.Add(new { Key = entry.Item1, LayerGroupId = layer->LayerGroupId, FestivalId = layer->FestivalId, FestivalSubId = layer->FestivalSubId, Flags = layer->Flags, Instances = instances });
        }
        var slots = new List<object>();
        var framework = EventFramework.Instance();
        var director = framework != null ? framework->DirectorModule.ActiveContentDirector : null;
        if (director != null && director->MapEffects != null)
        {
            var map = director->MapEffects;
            if (map->ItemCount > 128) throw new InvalidOperationException("Unexpected map slot count.");
            for (var i = 0; i < map->ItemCount; i++)
            {
                var item = map->Items[i];
                var binding = item.LayoutId == 0 ? null : world->GetLayoutInstance(InstanceType.SharedGroup, item.LayoutId);
                slots.Add(new { Index = i, item.LayoutId, item.State, item.Flags, Binding = binding == null ? null : new { LayerKey = binding->Id.LayerKey, InstanceKey = binding->Id.InstanceKey, SubId = binding->SubId } });
            }
        }
        var env = EnvManager.Instance();
        var weather = WeatherManager.Instance();
        var weathers = new List<object>();
        if (weather != null)
            for (var i = 0; i < weather->Weathers.Length; i++)
            {
                var value = weather->Weathers[i];
                weathers.Add(new { Index = i, value.CurrentWeatherId, value.NextWeatherId, value.IsCurrentWeatherForced, value.IsNextWeatherForced });
            }
        var document = new
        {
            SchemaVersion = 1, Label = label, CapturedUtc = DateTime.UtcNow,
            PluginVersion = typeof(Plugin).Assembly.GetName().Version?.ToString(),
            ClientStructsVersion = typeof(LayoutWorld).Assembly.GetName().Version?.ToString(),
            Game = new { CurrentTerritoryTypeId = game->CurrentTerritoryTypeId, CurrentTerritoryFilterKey = game->CurrentTerritoryFilterKey, TransitionTerritoryFilterKey = game->TransitionTerritoryFilterKey, CurrentContentFinderConditionId = game->CurrentContentFinderConditionId, TerritoryLoadState = game->TerritoryLoadState, ActiveFestivals = Festivals(game->ActiveFestivals), QueuedFestivals = Festivals(game->QueuedFestivals) },
            Layout = new { InitState = layout->InitState, Type = layout->Type, TerritoryTypeId = layout->TerritoryTypeId, CfcId = layout->CfcId, LayerFilterKey = layout->LayerFilterKey, LevelId = layout->LevelId, FestivalStatus = layout->FestivalStatus, InsideFestivalTransitionLayerUpdate = layout->InsideFestivalTransitionLayerUpdate, SkipAddingTerrainCollider = layout->SkipAddingTerrainCollider, ResourcePath = ResourcePath(layout->LvbResourceHandle), ActiveFestivals = Festivals(layout->ActiveFestivals), NewFestivals = Festivals(layout->NewFestivals), Filters = filters, Layers = layers },
            MapDirectorPresent = director != null, MapSlots = slots,
            Environment = env == null ? null : new { ActiveWeather = env->ActiveWeather, TransitionProgress = env->TransitionProgress, ScenePresent = env->EnvScene != null, SpacePresent = env->EnvSpace != null, LocationCount = env->EnvScene != null ? env->EnvScene->LocationCount : 0, WeatherIds = env->EnvScene != null ? env->EnvScene->WeatherIds.ToArray() : Array.Empty<byte>(), EnvironmentResource = env->EnvSpace != null ? ResourcePath((ResourceHandle*)env->EnvSpace->EnvSetResourceHandle) : null, SoundResource = env->EnvSpace != null ? ResourcePath((ResourceHandle*)env->EnvSpace->SoundSetResourceHandle) : null, AmbientResource = env->EnvSpace != null && env->EnvSpace->EnvLocation != null ? ResourcePath((ResourceHandle*)env->EnvSpace->EnvLocation->AmbientSetResource) : null },
            Weather = weather == null ? null : new { WeatherId = weather->WeatherId, WeatherOverride = weather->WeatherOverride, WeatherIndex = weather->WeatherIndex, Slots = weathers },
            Unavailable = new[] { "Selected environment scene index and forced-scene selector: no validated typed field", "Director update downstream managers: not captured", "Physics broadphase membership: not captured; collider getter and prefab flag are separate observations" }
        };
        var directory = Path.Combine(Plugin.PluginInterface.GetPluginConfigDirectory(), "arena-snapshots");
        var path = Path.Combine(directory, FileName(label, DateTime.UtcNow));
        WriteDocument(path, document);
        return path;
    }

    internal static void WriteDocument(string path, object document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(file, document, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string? ResourcePath(ResourceHandle* resource) => resource == null ? null : resource->FileName.ToString();

    private static object[] Festivals(ReadOnlySpan<GameMain.Festival> values)
    {
        var result = new object[values.Length];
        for (var i = 0; i < values.Length; i++) result[i] = new { values[i].Id, values[i].Phase };
        return result;
    }

    private static object ReadInstance(ILayoutInstance* instance, int depth, ref int budget)
    {
        if (--budget < 0) throw new InvalidOperationException("Instance traversal limit exceeded.");
        var children = new List<object>();
        string? path = null;
        uint? prefabFlags = null;
        byte? timeline = null;
        float[]? position = null;
        var truncated = false;
        if (instance->Id.Type == InstanceType.SharedGroup)
        {
            var group = (SharedGroupLayoutInstance*)instance;
            path = LayoutQuery.GetSgbPath(group);
            prefabFlags = group->PrefabFlags2;
            timeline = group->PlayingTimelineIndex;
            var p = group->Transform.Translation;
            position = new[] { p.X, p.Y, p.Z };
            var count = group->Instances.Instances.Count;
            if (count < 0 || count > 10000) throw new InvalidOperationException("Unexpected child count.");
            truncated = depth >= 4 && count != 0;
            if (depth < 4)
                for (var i = 0; i < count; i++)
                {
                    var child = (ChildNodeInstance*)group->Instances.Instances[i].Value;
                    if (child != null && child->Instance != null) children.Add(ReadInstance(child->Instance, depth + 1, ref budget));
                }
        }
        bool? colliderActive = null;
        bool? colliderLoaded = null;
        if (instance->Id.Type is InstanceType.SharedGroup or InstanceType.BgPart or InstanceType.CollisionBox)
        {
            colliderLoaded = instance->IsColliderLoaded();
            colliderActive = instance->IsColliderActive();
        }
        return new { Type = instance->Id.Type.ToString(), instance->Id.LayerKey, instance->Id.InstanceKey, SubId = instance->SubId, Flags1 = instance->Flags1, Flags2 = instance->Flags2, Flags3 = instance->Flags3, IsActive = instance->IsActive, Path = path, PrefabFlags2 = prefabFlags, PlayingTimelineIndex = timeline, Position = position, ColliderLoaded = colliderLoaded, ColliderActive = colliderActive, ChildrenTruncated = truncated, Children = children };
    }
}
