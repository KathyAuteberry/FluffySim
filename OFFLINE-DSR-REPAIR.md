# Offline DSR environment and identity repair

## Evidence

The supplied `dsr-p2-online-correct` and `dsr-p2-offline-flickering` JSON files were read
without alteration. The comparison report at
`/home/kath/Stažené/dsr_p2_snapshot_comparison.md` was subsequently located and read. Its
structural findings match the supplied snapshots and the findings pasted in the request.
Both snapshots have territory
968, layout initialization 7, 61 layers and the same 1,980 top-level object identities.
The problem is activation, not missing resources: 60988 is P1-active offline and inactive
online; 61049/61050 have the reverse state, with additional differences in 60991/62172.
Raw layer/map-slot flags are evidence, not values to write back into memory.

The decisive weather difference is current 45/next 45/forced current online versus current
2/next 45/unforced current offline. The runtime `WeatherDiag` checkpoints independently
show online committing 45 within 250 ms, while offline retains current 2 for ten seconds.
The previous repair continuously held P2 instances against the native environment owner;
that was an incorrect reconciliation and could produce the observed alternation. As the
report emphasizes, single-frame snapshots do not identify the native callback responsible
for a later reset. The stalled weather and managed layer writes are established; attribution
of the observed flicker to their interaction remains a runtime hypothesis pending the new
change-only trace and client playtest. Raw map-slot flag differences are not independently
decoded or treated as writable phase instructions.

Read-only inspection of this installed game's ServerWeather virtual table identifies
Update at image VA 140E13AC0. It subtracts the framework delta from its local fade timer,
then copies NextWeatherId and IsNextWeatherForced into the current fields. There are no
calls or network operations in that function. Addresses are diagnostic evidence for this
binary, not offsets used by the implementation. FFXIVClientStructs already exposes this
method as `ServerWeather.Update()`.

## Implementation

Only offline territory 968 opts into the missing local weather commit. After territory load
2, matching layout territory and initialization 7, the existing WeatherChange handler queues
the phase weather with zero fade and the typed native Update commits it. Completion stops
all further queue/update calls for that layout. A different active layout pointer or a real
readiness loss permits one new finalization. Phase changes supersede the request, and leave
clears it and commits the saved original weather locally. There are no delayed callbacks
holding stale layout-object pointers.

DSR's per-frame layer/individual-instance holds have been removed. The native environment
selector now owns its P1/P2 scenery; mixed layers are never overwritten. The nine existing
P2 MapEffects, waymarks, collision and mechanics remain unchanged. Inn weather uses its
existing path. FRU's separately established presets/holds remain unchanged.

The optional **Trace offline environment and party identity** checkbox in Offline config
logs only observed state changes for layers 60988, 61049, 61050, 60991 and 62172, including
layout pointer, load/init state, monotonic time and simulation time. Weather commit logs
include request epoch, source, before/request/result. It observes native changes without
hooking every native SetActive call; it cannot attribute unrelated native writes by stack.

## Virtual party identity

Local gearset files contain neither a character name nor a home world. The configured name
was already used in PlayerState and the spawn packet, but the virtual player spawn omitted
both world fields. Bots inherit those fields, so all eight had world zero. Offline config
now asks for the user's **actual home world**, selected from public rows of the installed
World sheet. Preflight rejects missing/unresolved choices before creating the virtual actor.
Existing configurations still deserialize; their original files are not changed. Users must
supply the name/world metadata once. Neither a world nor an authenticated content ID is
invented.

Offline actor publication and roster presentation now populate the native entity and
content-ID display-name caches before spawn and before the existing local UpdateParty
handler. The native setters do not replace an existing entry; stale entries belonging only
to our reserved virtual IDs are invalidated first, so repeated runs/imports cannot retain
old names. Native name formatting has a separate cache; having visible actors alone does not
populate every consumer. Only the existing virtual actor/content IDs are used offline.
Read-only native inspection verifies that the setters perform bounded cache scans and
string copies, with no transport calls. The content-ID *getter* can issue a name request
and is deliberately never called here.
The setters only populate local display caches. Native roster names, actor associations,
jobs, worlds and status machinery continue to use the existing packet path. Diagnostics
compare actor and actual native roster identities after that handler.

This addresses missing metadata/cache initialization. Whether a particular DelvUI release
uses additional authenticated-session gates still needs a client test. Dalamud PartyMember
reads Name/HomeWorld from the native group and resolves GameObject by EntityId; those
associations are retained. Invisible models do not remove party membership or statuses. The public
DelvUI PartyManager also reads the native party addon/HUD-agent rows and keys an internal
map by displayed name before joining it to Dalamud party members. Eight identical Unknown
labels therefore collapse that map; fixing only actor visibility cannot fix those frames.
This repair supplies the underlying names/caches rather than writing DelvUI's private HUD
offsets. Its actual installed build and status rendering still need the manual check.

## Discord Chat Bridge: separate API limitation

Installed Dalamud IL establishes that Login is raised on a framework update when conditions
become nonempty and a local actor exists. Its IPlayerState.HomeWorld/CurrentWorld getters,
however, return default RowRefs whenever AgentLobby.IsLoggedIn is false. Offline mode must
keep that flag false. Character identity is staged before actor publication, but a later
Login notification cannot make HomeWorld authenticated. Faking a lobby login would violate
the offline guard and is not an acceptable compatibility repair.

The public DiscordBridgePlugin source uses unchecked HomeWorld.Value in its constructor
cache refresh, OnTerritoryChanged and async-void OnLoginEvent. An empty reference therefore
explains the reported RowRef.Value exception; a captured live RowId at the exact crash is
not available. This is both missing FluffySim actor metadata (fixed above) and an external
plugin assumption (still requires its own fix). No installed plugin was patched, disabled,
or unloaded, and no Dalamud events were suppressed.

`docs/discord-bridge-offline-guard.patch` is a proposed, unpublished upstream patch: use
ValueNullable, skip online presence without a resolvable identity, and contain exceptions
inside async-void login. It has been checked against the fetched source text, not built as
Discord Bridge. Until that plugin handles offline identities, **manually disable Discord
Chat Bridge before entering offline mode**. The Offline config screen displays this warning.

Sources:
- https://raw.githubusercontent.com/reiichi001/Dalamud.DiscordBridge/master/Dalamud.DiscordBridge/DiscordBridgePlugin.cs
- https://raw.githubusercontent.com/aers/FFXIVClientStructs/main/FFXIVClientStructs/FFXIV/Client/Game/WeatherManager.cs
- https://raw.githubusercontent.com/aers/FFXIVClientStructs/main/FFXIVClientStructs/FFXIV/Client/Game/NameCache.cs
- https://github.com/DelvUI/DelvUI/blob/develop/DelvUI/Interface/Party/PartyManager.cs

## Validation boundaries and client checklist

Headless tests exercise the production bounded transition policy, readiness loss/rebuild,
request replacement, clear/restart, spawn identity, all eight party packet entries and cache
requests. Existing all-scenario entry tests retain phase weather, MapEffects, readiness and
clock gating. They do not execute FFXIV's layout selector, native drawing or DelvUI.

1. Disable Discord Chat Bridge manually; select the actual name/home world in Offline config.
2. Enable the optional trace. Cold-enter DSR P2; wait before Start, then run Strength and
   Meteors. Check stable P2 scenery, correct floor/walls and weather, no P1 flicker.
3. Stop/Restart; switch among all four DSR entries; Leave and cold-enter again. Check tower
   windows and Pause/Resume remain correct. Try immediate and delayed Start.
4. Check eight named native party entries, DelvUI names/statuses, seven models and tethers.
   Repeat with hidden models: roster/statuses should remain populated.
5. Check one original offline encounter and FRU P1/P3/P4, then inn DSR as controls.
6. If anything remains wrong, capture a fresh snapshot plus the change-only trace; distinguish
   actor names from roster names and current weather from merely pending weather.

Native stability and third-party UI compatibility remain pending the user's Release playtest.
Version stays 1.2.0.0. Development builds and tests use Debug only.

## Completed headless validation

- Solution `dotnet build -c Debug --no-restore`: succeeds; no Release output touched.
- Final optimized Debug plugin suite: 13,658 passed, zero failed (4m26s). DEBUG remains
  defined; this is not a Release build. This includes all scenario/lifecycle regressions.
- Relay: 44 passed. Python tooling: 65 passed.
- Safety: 303 passed, three Windows-native cases skipped on Linux, with each hook backend.
  The backend runs and focused subsets overlap; their totals are not additive.
- Proposed Discord Bridge patch: dry-run applies to the fetched public source; not built,
  installed, submitted or published.
- Existing warnings: unavailable NuGet vulnerability feed and nullable warnings in
  DsrSanctity2Ai/PauseInvestigationTests. No packages installed.

History: the functional change is `roszql`, after `soxsns` and before the empty four-parent
`plkqvn` merge. Evernow heads 440eec337fed, aa96ab9d800b and 4a1f8c2d93d5 remain parents;
upstream 1babdfbe3615 remains an ancestor. The empty working area is `xznrxk` above the merge.
The pre-task recovery operation is `515d7fc54c14`: inspect with
`jj --at-operation 515d7fc54c14 log` before selecting a recovery operation. Do not blindly
restore an old operation over concurrent work. No push or remote history changes occurred.
