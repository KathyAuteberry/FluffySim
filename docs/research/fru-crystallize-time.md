# Crystallize Time: Mana adaptation and evidence register

This change adapts the existing upstream CT implementation rather than importing a second encounter engine. The preserved source baseline is AnoMech `6aec4a5940df56ebc4f70a2fdddeceb31c7848b8`; AsuMech `d3f4f3e07a9af49d66a2637ef3f329c76fd174d9` carries identical CT files. See [comparison](asumech-comparison.md). Native rendering has **not** been verified in this development session.

## Strategy changes

[Tuufless Mana P4](https://ffxiv.tuufless.com/mana/fru/04_gaia_and_shiva/) specifies Nukemaru. Red Ice and Aero pairs use **H1 MT ST D1 D2 D3 D4 H2**, west first. Eruption goes to the northern slowed corner; the other blues initially join the southern slowed corner. Existing FRU-Sim-derived early routes already implement that broad geometry, so those routes are retained while replacing role priority. Blue puddle allocation remains deterministic by debuff subtype, a valid default for the guide's player-selected 1–4 ordering; automatic overhead numbers are not added.

The new Mana option captures a central Y, rotates fixed role spreads with the second Tidal Light origin, uses bot anti-knockback before stun, and finishes with ST solo / seven on MT. Easy bots expose later movement earlier (0.75s arrival margin after the cleanse phase), while Hard retains0.25s; early constrained routes keep the same safe deadlines. Individual Blue Ice, Water and Eruption settings supplement the existing combined random choice. The original NA option remains available at its original index and saved identity. Humans use their native Arm’s Length/Surecast or the existing once-per-run six-second practice button, now enabled for CT; press before stun. The human practice flag applies only to the human, while bots use their own timed statuses.

The party stack sits1.5yalms towards the second wave origin, with tanks4.5yalms towards it; this avoids the final Tidal strip and gives the later20-yalm knockback a safe landing margin. Central stack positions have sub-yalm offsets to make front-four wing order deterministic. Tanks lead; no non-tank takes both vulnerable front-four hits. This is a simulator ordering convention, not a measured native placement tolerance. The guide allows tanks to survive the overlapping wing with heavy mitigation; CT now models that through the existing mitigation system instead of unconditional tank death. The 56% requirement approximates 40% personal plus three independent 10% reductions; exact HP, gear, shields and damage rolls are not simulated. Unprotected non-tank double hits remain lethal. Optional mitigation checks follow the existing global practice setting; bots pass modeled mitigation requirements as other scenarios do.

## Encounter and native evidence

Times are relative to this drill's scheduler start. Unless stated otherwise these are preserved upstream executable values, **not locally measured combat-log timestamps**. Action cast data and shapes continue to come from the pinned game sheets. Local BMR `BossMod/Modules/Dawntrail/Ultimate/FRU/P4CrystallizeTime.cs`, `FRUEnums.cs`, and `FRUStates.cs` corroborate the mechanic families, dragon catches, stack sizes, Tidal Light geometry, and Akh Morn→Morn Afah ordering. They do not prove every inherited timestamp.

| Event | Scheduler time | ID / geometry / presentation | Evidence and confidence | Outstanding verification |
|---|---:|---|---|---|
| CT casts | 1.0 | 40240 / 40298; native boss casts | Upstream; cast IDs corroborated BMR | Native cast/impact offsets |
| Assignments | 11.8 | 2 red Aero, 2 red Ice, 4 distinct blues; 3 Quietus | Upstream + BMR + strategy; corroborated composition | Assignment/status publication timing |
| Hourglass scenery / actors | 11.8 / 12.0 | Six radius-11 positions; native MapEffects and tether actors | Upstream, inferred presentation | Rendered clock alignment and tether endpoints |
| Yellow / purple tethers | 15.1 | N/S fast; NW/SE or NE/SW slow | Strategy + BMR + upstream; corroborated pattern | Native speed animation |
| Dragons spawn / begin movement | 15.3 / 20.2 | Opposed semicircles radius12.6; shrink after first catch | Upstream + BMR hitbox shrink; geometry inferred | Model scale, motion and collision tolerance |
| Maelstrom cast starts | 21.5 / 27.0 / 31.95 | 40299; sheet radius12 circles | Upstream; corroborated shape | Exact snapshots and lingering visuals |
| Water / remaining debuffs | 23.9 / 25.9 / 28.8 | Native actions, Aero push and five-person Darkness stack | Upstream + BMR; corroborated rules, inferred time | Knockback arrival and native expiry |
| Dragon contact / puddle | Dynamic | 40241 radius12; native event object, consumable fang cleanse | Strategy + BMR + upstream; corroborated behavior | Puddle grace/radius/lifetime and director state |
| Tidal Light | 29.3 / 35.7 | 40251/40252/40253; travelling 10×40 strips | BMR + upstream; corroborated shapes/order | Native travelling effects and exact damage offset |
| Quietus | 42.9 | 40281, three carriers, raidwide | Upstream; corroborated mechanic | Effect overlap |
| Record / Spirit Taker | 44.8 / 48.5 | Native Return markers, per-player stored positions; spread failure | Upstream; strategy corroborates required formation | Actual native marker/rewind and fragment clipping |
| Stun / rewind | 51.8 / 52.8 | Native carry/Return, nine-second stun | Upstream, inferred timing | Input suppression and pause |
| Wings casts | 53.7 / 58.3 | Existing native ID/shape/timing, front-four ordering, native knockback row | Strategy corroborates survivable tank overlap; threshold inferred | First KB immune, second KB active; mitigation experience |
| Akh Morn | 64.0 cast; hits69.0 + 1.05n | 40247/40302 bars, 40248/40303 hits; ST solo option | Upstream + strategy + BMR; corroborated order | Target presentation, actual hit intervals |
| Morn Afah | 72.9 bars; 79.8 resolution | 40249/40304, 40250 radius4 eight-player stack | BMR6s cast and0.9s effect interval; inferred scenario alignment | Native offset; boss HP balance is out of scope because bosses do not model DPS |

A hungry dragon now reports Joyless Dragonsong40242 rather than the previously unverified Longing of the Lost40241; the guide explicitly identifies the failed-catch wipe and BMR corroborates the action ID.

No native IDs were invented for a substitute model or effect. CT uses the existing **main FluffySim P4 phase** and its arena readiness/layer behavior. Per-run actor spawning, scheduler ownership and world cleanup remain in the existing lifecycle. No network/firewall or installed-output changes are involved.

## Validation and remaining limits

`FruP4CrystallizeTimeTests` covers all eight roles × four Tidal corners × both slow patterns × Easy/Hard; deterministic deck constraints and forced debuffs across128 seeds; red-pair Mana priority; Y front-four ordering; and a deliberately missed red cleanse. The original NA catalog regression must also pass. Executed validation: ordinary Debug passed the initial139 CT cases; the final optimized Debug run passed144 test cases (142 CT unit/matrix cases plus both CT catalog strata). The command supplied `TestRunParameters.Parameter(name="SunnySeeds", value="100")`, overriding the checked-in40-seed default, so each catalog stratum executed100 random full runs (200 additional runs total). Output was isolated in `tests/AnoMech.Tests/bin/fast-Debug` with per-project `obj/fast-tests-Debug` intermediates. The human anti-knockback tests cover own practice flag timing, its expiry before the second wing, and a missed first-wing immunity causing a headless arena fall; actual native client presentation remains unverified. No Release artifacts were built.

The early routing retains measured-looking upstream constants whose local observation provenance was not re-established. No claim is made that these timings are newly observed. The native guide screenshot positions are not a complete specification of path interpolation. The central-Y party uses deterministic small offsets; verify actual visual placement and avoid reading these offsets as a player precision requirement.

Before release: test both inn and offline cold load; hourglass layers/tethers; heads and shrinking; correct and wrong catches; consumed puddles; Tidal Light snapshots; record and rewind; fixed relative spread with random Spirit Taker targets; anti-knockback before stun; tank mitigation on both wings; ST solo Akh Morn; eight-player Morn Afah; Stop/Restart/Leave during each stage; hidden bots; Pause/Resume. Confirm all native actors and pending effects disappear after leaving. Headless tests cannot verify these native visuals.

The [complete local capture inventory](fru-native-capture-inventory.md) observes four simultaneous-pair entry cast records with a9.700s bar. All were interrupted after wipe, so this corroborates opening bar duration only; it does not establish a completed CT timeline.
