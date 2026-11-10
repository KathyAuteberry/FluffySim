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

The first repair used only ServerWeather.Update. The subsequent 3308CD runtime trace
shows why that was insufficient: its slot becomes 45 while WeatherManager.WeatherId stays
0, selector stays 3, EnvManager.ActiveWeather stays 2, and the P1 objects remain active.
A committed server slot is not a completed rendered environment transition.

Offline DSR P2 now queues the phase weather through the existing handler and invokes the
complete native WeatherManager.Update once after layout readiness. Read-only inspection
of the installed executable identifies this routine at image VA 140E140E0: it updates the
weather interfaces, selects the active interface, notifies the event system when it changes,
and publishes the rendered weather/fade and manager cache. The signature uniquely matches
this installed binary; no hardcoded address or raw flag write is used. This routine is not
exposed by the installed ClientStructs, so it is resolved with the existing signature-based
native pointer facility. It is called only inside the offline DSR lifecycle and restoration.
Normal inn initialization and FRU restoration keep their previous paths.

Completion now requires rendered weather 45 **and** matching actual object activation in
all thirteen differing layers. The verifier checks instance IDs and activity, including the
mixed layers, against the known-good snapshot. It is read-only. After the one transition,
subsequent ticks only observe completion; they never requeue or rerun the transition for
the same ready layout generation. Readiness loss/a new layout permits a new finalization.
Stop/Leave invalidate ownership through the existing phase/session lifecycle.

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
plugin assumption. The compatibility workaround below avoids exposing the offline
identity to Bridge; it does not change Bridge or suppress Dalamud events.

FluffySim temporarily unloads loaded incompatible plugins before bootstrap begins, using
Dalamud's internal LocalPlugin lifecycle through reflection. Discord Chat Bridge is the
initial allowlist entry. The full reflection contract is resolved before any unload;
missing members or unload failures refuse entry and restore previously suspended plugins.
The plugin's saved manifest/profile preferences are unchanged. Already disabled plugins
remain disabled. Restoration waits until offline state is put back and guards released at
title, and failures identify the need to enable the plugin manually. Pending operations and
installation IDs survive FluffySim reloads in process-wide state. No external plugin binary
is modified and no authenticated lobby identity is fabricated.

Headless regression coverage exercises pending unloads, selective suspension, failed entry
rollback and restoration failure/retry. A client check must confirm Bridge unloads before
the first territory event, remains absent across scenario changes, and returns after title
cleanup; then verify an ordinary login and repeat with Bridge initially disabled.

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

## Previous repair headless validation

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

## Follow-up: real 3308CD traces and HUD publication

The user's traces were found in:
`/home/kath/Projekty/anomek/AnoMech/AnoMech/bin/x64/Release/anomech-logs/`.
The 08:24 session is preserved in `AnoMech-Offline-20261016-082720-3308CD.log.gz`;
the preceding 08:17 session is in `AnoMech-Offline-20261016-082226-3308CD.log.gz`.
These files were read only. Offline config now shows both the exact current active-file
path and the archive directory, and offers Flush while a session is active. Active data is
also written to Dalamud logging and the bounded in-memory diagnostic buffer. Rotations
produce `AnoMech-Offline-*.log.gz`; the active path can have a stamped fallback filename.

The trace establishes eight correct, distinct actor/entity and native group identities,
names and home world 72. It does **not** record the old HUD row contents or actor content
IDs. Source inspection finds a further real mismatch: doppel spawn packets left content
and account IDs zero, while PartyHud substituted reserved virtual values in the group.
The repaired offline spawn packet now uses those same reserved IDs from the outset;
the virtual human also has consistent local packet metadata. No authenticated identities
are invented and no packet is sent to a server. Inn doppel creation is unchanged.

Name caches alone do not publish native HUD rows or its StringArrayData. The offline
bridge now maps every native group entry to its actual actor, entity/content ID and stable
name buffer in AgentHUD.PartyMembers. It publishes names through the existing managed
StringArrayData.SetValue API before the _PartyList addon consumes them. It uses the typed
PartyListStringArray member locations, not DelvUI's private offsets. Status/cast arrays and
model visibility are untouched. Listener disposal and Clear remove owned HUD pointers;
managed strings remain owned by the native UI allocator.

The public DelvUI raw-info offset E18 corresponds to the installed typed HUD row base E10
plus its Name field at +8; this is not evidence of an outdated offset. Its map uses HUD
names as keys and then resolves Dalamud members. Eight correct group names therefore do
not establish eight correct DelvUI inputs. Zero/shared actor IDs can make content-based
lookups ambiguous; duplicated/missing HUD names can collapse its map. The exact old values
that made only Paladin survive were not captured, so that last causal detail is not claimed
as observed. The new bounded per-slot table includes actor and roster content IDs, names,
world/job, HUD-before values, addon text, Dalamud names/IDs and actor association.

Dalamud's installed IPartyList reads the native group count and requires loaded, nonzero
PlayerState.ContentId to construct members. The offline bootstrap supplies its existing
sandbox ID; the actual trace confirms PlayerState loaded. No lobby login or HomeWorld API
semantics were changed. The user's confirmed Discord Bridge compatibility is preserved by
leaving its integration and external plugin untouched.

Native environment/UI success remains subject to a fresh client test. The full-manager
call and HUD adapter are headlessly tested through policies, real native struct projections,
recorded object-state fixtures and production IL safety checks; the game engine itself is
not executed by those tests.

The follow-up functional change is inserted after `roszql` and before the same four-parent
`plkqvn` merge. Pre-task operation checkpoint: `c2869e0f2f24`. The accepted upstream and
Evernow histories are retained; no remote operations or version change are required.

Follow-up manual checks:

1. Open Offline config and enable Trace. Note the exact Active trace path; Flush is available
   without leaving the active session. Confirm the new checksum in diagnostics after your
   own Release build.
2. Cold-enter DSR; try both immediate Start and waiting before Start. Trace should show
   rendered environment 2→45, cached weather 45/selector 2, and `requested=P2 verified=True`
   for recorded instance activation. Check the actual floor, lighting and collision.
3. Run each Strength/Sanctity entry, Stop/Restart, switch, Leave and re-enter. Confirm stable
   P2 and normal Meteors tower windows; pause during a tower wave.
4. Confirm eight distinct native names and eight DelvUI entries with debuffs. The table
   should show matching actor/roster content IDs and correct per-slot HUD/Dalamud identities.
5. Repeat with invisible bots, an original offline scenario and FRU P4, then inn DSR. No
   installed UI plugins need patching by FluffySim. Existing Discord Bridge/login handling
   is unchanged; retain the user's already-working compatibility setup.
6. If incomplete, retain the trace and a fresh post-initialization snapshot. A weather slot
   alone is no longer considered success, nor are populated actors alone evidence of HUD
   publication. The trace explicitly distinguishes these stages.

Follow-up validation completed:
- Solution Debug build: succeeds, zero errors. Existing NU1900 and two nullable warnings.
- Final isolated optimized Debug suite (DEBUG still defined): 13,668 passed, zero failed,
  4m52s. This includes all nine scenarios, recorded P1/P2 layer-state comparisons, mixed
  instance activation, native eight-row name/identity projection, invisibility and cleanup.
- Relay 44 passed; tooling 65 passed.
- Safety 305 passed per hook backend, with three Windows-native cases skipped on Linux.
  Both ordinary Debug and optimized TRACE-only Debug plugin artifacts were inspected.
- Full weather-manager signature: exactly one match in the installed game executable.
  This is static validation, not execution of the new native transition in a live client.
- No Release build/output writes, external plugin changes, remote pushes or version changes.

The exact old HUD cache contents were not logged, so an explanation that all seven raw HUD
rows specifically reused Paladin is not presented as an observation. The first bot is
Paladin in the actual roster trace, all bot spawn packets had zero identity fields in the
old source, and DelvUI consumes a different HUD identity table from the verified group.
The repaired path removes those inconsistencies and exposes every stage for client checking.
