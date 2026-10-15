using System;
using System.IO;
using Newtonsoft.Json;

namespace AnoMech;

internal static class ConfigurationMigration
{
    internal static Configuration Load(FileInfo currentFile, Func<Configuration?> loadCurrent)
    {
        if (currentFile.Exists) return loadCurrent() ?? new Configuration();
        var legacyFile = Path.Combine(currentFile.DirectoryName!, "AnoMech.json");
        if (!File.Exists(legacyFile)) return new Configuration();
        // Explicitly typed deserialization ignores old assembly-qualified $type metadata.
        return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(legacyFile),
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None })
            ?? throw new InvalidDataException("The existing AnoMech settings could not be read. They have not been overwritten.");
    }
}
