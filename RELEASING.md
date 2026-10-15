# FluffySim releases

`FluffySim.Version.props` is the authoritative plugin version. The inherited AnoMech source version was `0.4.2.0`; the first FluffySim major release is `1.0.0.0` (SemVer `1.0.0`).

Edit only `FluffySimVersion` for a future release:

- Major: major milestone, significant feature set or breaking change.
- Minor: a new scenario or meaningful simulator functionality.
- Patch (assembly build component): fixes and small improvements.
- Assembly/file revision remains zero. Debug builds never automatically increment versions.

The plugin project imports this file. Assembly/file version, informational version, dependency metadata and Dalamud's generated manifest all derive from it. Main UI reads the loaded assembly version. RootNamespace, project/solution names, relay assembly and existing scenario IDs remain AnoMech for source/protocol compatibility. DLL/assembly/InternalName are FluffySim; typed legacy configuration import bridges the changed Dalamud config identity.

## Development verification

**Never build Release during development.** Dalamud watches the user's installed Release output; touching it can freeze the running game. Build/test Debug only, never publish/copy into installed plugin directories. Debug and Release output/intermediate trees stay separate.

```sh
dotnet build -c Debug
python tools/package_release.py --input AnoMech/bin/x64/Debug --output /tmp/fluffysim-package-check
```

For direct project builds use `AnoMech/bin/Debug`. The ZIP above contains Debug binaries and is only a packaging check, not a public release. Never register it alongside the installed plugin.

## User-operated release build and ZIP

Only the owner performs the Release build, after leaving/handling their running installed plugin:

```sh
dotnet build -c Release
python tools/package_release.py --input AnoMech/bin/x64/Release --output dist
```

The script **does not build, publish, mutate the input, register plugins, bump versions, commit, tag or upload**. It outputs `dist/FluffySim-1.2.0.0.zip` at the current version, refuses overwriting an existing ZIP, and refuses ZIP destinations inside Release/build input directories. It packages runtime assets from the dependency manifest, excluding stale AnoMech plugin DLLs, test hosts, Dalamud-shipped libraries, logs and private snapshots. Missing dependencies or mismatched manifest/dependency versions abort packaging. The custom DalamudPackager target generates a manifest only, including on owner-operated Release builds; it avoids the upstream packager’s unfiltered latest.zip. Use a clean input build when adding runtime resources; update the script/tests for assets not represented by dependency metadata.

Before distributing: manually test the Release build, inspect ZIP contents, include the matching complete source under AGPL-3.0-or-later (including the changes), and publish source and ZIP together. The inherited upstream URL credits AnoMech; it is not a source location for unpublished FluffySim changes. No repository release is created automatically. The manual CI verification workflow builds Debug and uploads a check artifact only.

## Manual acceptance check

1. Disable/remove the old developer registration, select only FluffySim.dll and confirm `FluffySim v1.2.0.0` and saved preferences.
2. For each of the nine scenarios, run the same seed/role once Easy and once Hard. Confirm unchanged assignments and later positioning in the documented eligible phases.
3. Check tower communication and tight dodges remain correct. Try Hard + invisible bots, then God Mode and both local practice actions in their eligible scenarios.
4. Leave, re-enter and verify BGM restoration, model visibility, markers, arena and practice-action readiness. No development workflow reloads the running plugin automatically.

## Loader/configuration compatibility evidence

DalamudPackager derives InternalName and AssemblyVersion from the assembly and looks for a manifest sharing its filename. Dalamud uses InternalName for configuration lookup, but the developer-plugin loading preferences use the registered DLL path. Therefore this rename changes the plugin identity to FluffySim, imports typed legacy settings explicitly, and requires manual developer-registration migration. Sources: [packager implementation](https://github.com/goatcorp/DalamudPackager/blob/master/DalamudPackager/DalamudPackager.cs), [configuration storage](https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Configuration/PluginConfigurations.cs), [developer plugin lifecycle](https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Plugin/Internal/Types/LocalDevPlugin.cs). Namespaces, relay protocol headers, canonical scenario identifiers and ImGui window IDs remain unchanged. Diagnostic archive filenames/header parsing remain AnoMech to keep historical build/log recognition compatible.
