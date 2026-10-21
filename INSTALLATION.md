# Install FluffySim

Add this custom repository URL under Dalamud settings →
Experimental → Custom Plugin Repositories, then find FluffySim in the plugin installer:

https://raw.githubusercontent.com/KathyAuteberry/plogons/refs/heads/master/mostly-harmless.json

Leave the simulation and disable any
old developer registration or AnoMech installation before installing through the repository.
Never load both copies. Repository-installed updates are managed by Dalamud.

## Manual developer installation

1. Download `FluffySim-<version>.zip` from [FluffySim releases](https://github.com/KathyAuteberry/FluffySim/releases) and extract **all files** into a dedicated folder. Keep the DLL, manifest, dependency JSON and dependency DLLs together.
2. Run `/xlsettings`, open **Experimental**, and use **Select Dev Plugin DLL** to select `FluffySim.dll`.
3. Run `/xlplugins` → **Dev Tools** → **Installed Dev Plugins** and enable **FluffySim**.
4. Use the first green power-symbol button to disable automatic plugin loading. Automatic loading and automatic reload are separate controls; disable reload-on-file-change too if you want to update files without interrupting the running client.
5. Go to an inn and open `/fluffysim`. `/anomech` and `/ano` remain supported. The title shows the actual assembly release version.

Do not select `AnoMech.Relay.dll`: it is a dependency, not another plugin. Do not register a development Debug output beside an installed Release output.

# Upgrade from AnoMech

1. Leave any simulation. Back up Dalamud's `pluginConfigs/AnoMech.json` and `pluginConfigs/AnoMech/` directory.
2. Disable the old AnoMech plugin, then remove its **developer DLL registration** before selecting FluffySim. If also installed through a repository, disable that copy too. Keep the old files and configuration backup; deleting configuration is unnecessary.
3. Extract FluffySim into a **new folder**, not over the old output, and register only `FluffySim.dll`.
4. On first startup, if no `FluffySim.json` configuration exists, FluffySim reads the adjacent legacy `AnoMech.json`. It does not overwrite or delete the legacy file. The next normal settings save uses `FluffySim.json`. Existing FluffySim settings always take precedence.
5. Verify your selected scenario, role, difficulty, visibility, God Mode and audio settings. The namespace, configuration property names, scenario identities and multiplayer protocol remain unchanged. Numeric scenario overrides retain their existing meaning and persistence behavior.

Legacy snapshots/log directories are not automatically copied or removed. Keep `AnoMech/arena-snapshots` for comparisons; new snapshots go into `FluffySim/arena-snapshots`. Developer-plugin loading/reload preferences are tied to the selected DLL registration, so configure them again after migration. A startup guard refuses to load while an AnoMech or FluffySim instance is already loaded; it never uninstalls another plugin.

# Update an existing FluffySim installation

Leave the simulation and disable FluffySim before replacing files. Extract the new archive into a clean folder, update the developer DLL registration if its path changed, then enable it manually. Never keep both registrations active.

# Experimental offline mode

FluffySim must already be loaded for its title-screen entry to exist. With autoload disabled, load it manually if Dalamud offers that UI at title, or load it in a normal session and log out to title. Wait at least ten seconds after logout. Choose **FluffySim offline mode**, select a local character/loadout and review the warnings before entering its virtual inn. No live login is required. Do not enable automatic loading just for this feature without understanding that change.

Imported character configuration is read from disk into local state; the original is not intentionally rewritten. Back it up before the first experimental test. Reviewed file/connection guards have limits; see [the integration audit](UPSTREAM-INTEGRATION.md). Native Windows/Wine startup and restoration still require validation. Unexpected login attempts and unloading during a tainted offline session may intentionally close the game. Return through the explicit offline controls, and leave a running scenario before attempting to return to title.
