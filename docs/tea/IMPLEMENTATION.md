# TEA Mana implementation and verification

This work integrates Evernow's `TEA-work` encounter foundation while preserving
its ancestry and the current unpublished FRU/DSR work. It implements Mana first;
Elemental is an extension point, not a selectable or implemented strategy.
No plugin version, release metadata, installed binary or publication is changed.

## Source coverage

The complete supplied package was found and analyzed. It contains one ACT network
log (12 pulls, two clears) and two native format-5 Duty Recorder files, covering
P1–P4. Both native files were structurally decoded and correlated with log casts;
this is not an assertion of successful in-game playback. Originals were read-only.
See [the complete inventory and limitations](EVIDENCE.md) and
[the sanitized source ledger](evidence-summary.json).

The complete accessible Notion body was read in the browser, including its
waymark image, macro links and five video cards. There were no hidden linked phase
pages to invent. All five Japanese automatic caption tracks were inspected and
retained with timestamps outside the repository. Selected primary video frames
and the eleven Fuccdayo P1 diagrams were read where necessary.

| Phase | Strategy authority | Implementation and evidence notes |
|---|---|---|
| Living Liquid | Fuccdayo Doll Skip; JP macro | Ignored dolls, relative north at the lone spout, Proteans, Parting, MT double buster, unequal-HP Pain exercise and orb kite. [P1 ledger](P1.md). |
| Limit Cut | Nukemaru 1211; JP east priority | One clockwise pack, pairs on blast lines 2/4/6/7. [Limit Cut ledger](LIMIT-CUT.md). |
| BJ/CC | Updated JP macro; Nukemaru | Four Nisi transfers, healer-first third, water/lightning groups and fixed support pairing. [P2 ledger](P2.md). |
| Temporal Stasis | JP fixed-position macro | None left, green right; support north/DPS south; blue support left/DPS right; lightning on BJ's side. [Stasis ledger](STASIS.md). |
| Alexander Prime | Nukemaru/Hamukatsu 34 fixed; JP macro | Mana Inception baits, fixed 3/4, 56→78→12 soaks and north/south Enumeration. [P3 ledger](P3.md). |
| Perfect Alexander | Nukemaru; JP macro | W/E Final Word, TH north/DPS south Optical, Calibration α/β and Trine handling. [P4 ledger](P4.md). |

Primary links: [Notion guide](https://app.notion.com/p/The-Epic-of-Alexander-Ultimate-TEA-226507c362a280e39a49c7406fcafe3c),
[Japanese macro](https://jp.finalfantasyxiv.com/lodestone/character/34120564/blog/5274750/),
[English macro](https://na.finalfantasyxiv.com/lodestone/character/40730370/blog/5524788/),
[Fuccdayo P1](https://www.youtube.com/watch?v=mxy_ZYEYwlE),
[Nukemaru P1/LC](https://www.youtube.com/watch?v=AMdtMyvbPNs),
[P2](https://www.youtube.com/watch?v=yuLjtpm2ti8),
[P3](https://www.youtube.com/watch?v=3wtfeXognIY),
[P4](https://www.youtube.com/watch?v=9YgDXyUWbiQ).

The JP macro controls ambiguous translation: it has four Nisi transfers, D3's
mine at five o'clock (the EN says southwest), and explicit west/east Final Word
marks. The captures put marker 1 northwest rather than the Notion diagram's north,
and third water at outer 2 rather than the macro's inner B. Neither discrepancy
was silently relabeled as agreement. The implemented waymarks follow the guide's
cardinal diagram with approximate radii informed by the capture; P2 uses inner B.

## Shared mechanics and regional extension points

| Shared encounter behavior | Mana-specific behavior |
|---|---|
| `TeaZone`, real NPC/action/status IDs, map state and native proxy calls | `TeaManaWaymarks` and the Mana waymark preset |
| Phase timelines, casts, snapshots, hit geometry and vulnerability rules | Per-mechanic assignment/position modules and AI movement stages |
| Nisi contact, letter matching, expiry and Gavel failure | Pairing, four transfer routes and healer-first third priority |
| Random numbered/debuff rolls from the run RNG | 1211 start priority; fixed 3/4 and Enumeration priorities |
| Stasis freeze, delayed attacks and proximity rules | Fixed debuff placements and baits |
| Projection mechanics, forced motion, stack/spread resolution | Final Word/Optical/Calibration placements |
| Parent-owned actors, statuses, effects and teardown | Role-appropriate bot actions and movement margins |

A scenario's `FullName()` is its encounter/phase/mechanic identity. Regional
selection is the existing separately persisted `Group + newline + Name` key in
`Configuration.ScenarioStrategies`. Human role and forced assignment preferences
remain per-seat settings, outside both identities. Seeds reproduce shared rolls;
AI action proc randomness now uses named run streams as well.

The cohesive `*Ai`/assignment modules are the extension points: add an actual
Elemental AI or positioning policy to the same scenario only after its sources
and tests exist. Shared P3/P4 choreography accepts strategies rather than copying
the encounter timeline. Legacy APT/1256/NAUR implementations remain explicitly
injected regression fixtures; none is exposed under a Mana name. No global
regional-strategy framework or speculative Elemental rules were added.

Potential future costs are explicit: regional differences in phase skip windows,
waymarks or mitigation plans may require another policy parameter. They should
not require another actor system or a second whole encounter timeline. The P1
short Doll Skip practice window is a practice simplification; its end does not
model actual player DPS or establish a measured death timestamp.

## Fidelity limits

Native IDs and action shapes come from game data and the existing encounter
foundation. Captured cast/ability timestamps are the authority for comparison;
the per-phase ledgers state clock normalization and differences. Snapshot, action
receipt, visible omen, animation and delayed impact are not interchangeable.
Headless tests establish emitted requests and collision behavior, not visibility
or exact VFX timing in the FFXIV client.

Remaining approximations include the P1 Pain mitigation threshold (not an inferred
raw HP-gap formula), barriers' fixed practice budget, native movement/turn lead
margins, P2's frontal shield-DPS stand-in and contact radius, Photon/healing,
Optical's inferred support/DPS target distribution, and existing upgraded defensive
statuses at level 80. P4's late jail cadence has inherited unverified values;
see its ledger before treating that segment as an exact timing reference.

TEA opts into real mitigation checks for required bot hits, leaving FRU/DSR defaults
unchanged. Its negative tests remove real shields, invulnerability or tank LB
statuses and expect failures. Geometry, Nisi, soaks and vulnerability checks also
apply to bots. This is a coarse practice damage model, not a retail HP/DPS simulator.

All six entries advertise ordinary practice and eight-bot spectator support.
Headless coverage includes every human role, deterministic assignment rolls and
complete bot routes. The existing Easy/Hard setting is consumed, although Mana
routes do not invent different regional assignments for Hard. TEA uses the shared
local simulated-latency path already present in `Game`: configured round-trip
latency applies to ordinary local practice; spectator mode starts without a human
latency queue. Native input and rendering still need the checks below.

## Validation and remaining native checks

The initial integration results below precede the November 11 playback
corrections. See the [correction report](../verification/PLAYTEST-CORRECTIONS-2026-11-11.md)
for the latest changes, verification and retest scope.

Extended suites remain opt-in. Ordinary Debug output and the optimized TRACE
Debug safety artifact use separate output/intermediate trees. Both fresh plugin
builds passed with zero warnings and errors. Parallel builds initially contended
for the generated manifest; the final builds ran sequentially.

The complete non-Extended scenario/engine regression passed all **9,559 tests**
with zero failures (21 minutes 18 seconds). This includes the existing FRU/DSR
catalog, local latency, ordinary/spectator role matrices, multiplayer lifecycle
and TEA positive/negative mechanics. The focused P2 campaign additionally passed
all 118 cases, including a bounded 128-seed full-phase loop and saved failure seeds.

Both packet-safety backends passed against both fresh artifacts: 319 passed,
four skipped, zero failures per backend. The skips cover native Windows hook
execution and the optional installed DelvUI contract; those are not native
signoffs. The relay suite passed all 67 cases. Python tooling passed all 86 cases
after the documentation/package-link update. Both Debug builds and both safety
backends were rerun after the history restructuring with the same clean results.

Verification date: 2026-11-10. The full scenario suite was compiled with
`Optimize=true` and isolated `bin/tea-focused-Debug` / `obj/tea-focused-Debug`
paths, then run using `dotnet test tests/AnoMech.Tests -c Debug --no-build
--no-restore --filter 'TestCategory!=Extended'` with those same paths.
Safety runs use `ANOMECH_DLLS` containing ordinary Debug and isolated optimized
TRACE Debug binaries, with `ANOMECH_HOOK_BACKEND=default` and `safetyhook`.
No stale-binary override is used. See `tests/SafetyTests/README.md` for the
artifact preparation and platform-dependent checks.

An earlier full run was stopped after apparently stalled progress without a
usable final result. The completed run used per-test progress and a 90-second
hang timeout with dump generation disabled; no test timed out. The separate
27-case FRU lifecycle diagnostic also passed.

Native playtesting is still required for all six entries: title-screen/inn entry,
arena layers, boss/add models and animations, targetability/enemy list, cast bars,
markers/tethers/status overheads, omen and impact lead times, human local actions,
Pause/Resume during active VFX, Restart, Leave and eight-bot spectator camera/input
cleanup. Check every Limit Cut warp and P3/P4 jump explicitly; a successful fake
packet test cannot prove that the renderer played it.

## Local integration topology

The local `codex/tea-mana` branch contains these independent changes above the
shared native/engine foundation. The integration description is exactly
`chore: integration commit`.

| Workstream | Jujutsu change | Commit |
|---|---|---|
| Shared Evernow foundation and native integration | `vysoup` | `cd2328d` |
| Mana Living Liquid | `tmyyov` | `fe15357` |
| Mana 1211 Limit Cut | `wvylpr` | `91b830b` |
| Mana BJ/CC | `wlnuqy` | `f8f3fd5` |
| Fixed Temporal Stasis | `zulunm` | `8c944e1` |
| Mana Alexander Prime | `wktzsn` | `b2e4493` |
| Mana Perfect Alexander | `yulpul` | `59e37f5` |
| Source extraction and evidence | `slqsmm` | `b50684e` |
| P2 live Nisi routing follow-up | `zznxlz` | `356e011` |
| Explicit stay guards and duty review | `yrpwms` | `1c42ed4` |
| FRU second-clear evidence | `xztryy` | `321e01d` |
| DSR P7 implementation | `mzwrrz` | `67186c1` |
| DSR P7 environment fix | `uzmyoy` | `3e189b7` |
| DSR P7 marker timing fix | `vlnvmv` | `aca3ec7` |
| Local Jujutsu build fix | `xpnkmx` | `20cfac6` |
| Documentation organization | `owvnqu` | `740007f` |
| P1 pull and buster correction | `pwzytt` | `920ac93` |
| 1211 outward-facing correction | `youyuy` | `a32255a` |
| P3 opener and nearest-flame correction | `owukoz` | `12d2fba` |
| P4 visible Motion correction | `trmwou` | `028e071` |
| DSR P7 threat-facing correction | `utkwxp` | `e941e2c` |
| Correction report and documentation links | `wnlplq` | `df69908` |
| Twelve-parent shared integration | `wxyvwv` | `d4b1d16` |

Catalog identity checks and verification notes are above that integration.
Explicit stay checks were added to the two per-frame punishment paths identified
by the safety audit. Territory 887 was reviewed against the reciprocal
TerritoryType/ContentFinderCondition 694 rows and the native recording header.
The safety rules were retained; the Debug branch structure makes the stay check
explicit rather than relying on a compound boolean expression.

FRU evidence, the local build fix, the DSR P7 chain and documentation cleanup
are independent branches from `tnlmky`. P7's environment and marker fixes remain
on the P7 implementation branch because they depend on its definitions. TEA's
shared foundation starts from `tnlmky` plus the original Evernow source tip;
phase implementations and the safety follow-up depend on that foundation.
Five playback follow-ups sit on their existing phase branches; each replaces
that branch tip as a merge parent. All twelve workstream tips merge into
`wxyvwv`; catalog verification and this
report remain above the integration.

The initial TEA split preserved all 1,078 tracked files byte-for-byte. The later
owner-authorized history restructuring was checked against a frozen copy of all
1,081 tracked files after documentation cleanup: every path, content hash and
file mode matched exactly, with no missing or added files. Integration conflict
resolutions retain the relocated documentation and remove obsolete root copies.
At that milestone, only this topology/verification report was updated after
that equality check. The subsequent playback corrections have their own
file-preservation check and validation in the linked correction report.

Original Evernow TEA commits, `plkqvn`, published release ancestry and
`master@origin` remain ancestors. The unpublished FRU/DSR changes retain their
change IDs under new commit hashes. Local `master` remains at the published
3.2.2.0 preparation change; it was not advanced.
