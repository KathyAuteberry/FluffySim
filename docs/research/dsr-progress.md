# Operation Dragonsong progress

Updated 2026-10-20. First expanded native playtest found P1/P5 invisible actors, naked P4 NPCs, P1 knockback failure and P6 instant puddle death. Repairs applied; native re-acceptance remains pending. P7 excluded. No release request has been made.

## Current topology and recovery

One jj workspace, working change `xwtnow`, exact description `chore: integration commit`. All six feature tips are verified ancestors of it:

| Change | Work |
| --- | --- |
| `ktmmtz` | P2 facing correction; retains inventory `wlkspx` |
| `xzvvss` | P3 presentation pose; retains choreography `suprns`, arena correction `zrtumy`, original P3 `zmrxpo` |
| `kltull` | P4 Eyes and Haurchefant rescue |
| `koonkp` | P5 Wrath and Death |
| `okkrmu` | P6 two isolated halves |
| `vxkypp` | Independent P1 Holy See, implemented last |

P4/P5 depend on shared native infrastructure `ozrvwr`. P6/P1 independently depend on square-boundary core `rzltut`, child of `ozrvwr`; no P5→P6→P1 encounter chain. `jj simplify-parents -r xwtnow` found nothing redundant. Integration holds intentional catalog, registration, attribution and report conflict resolutions. Preserve every parent when adding follow-ups.

Published `master`, `master@origin` and tag2.2.0.0 remain `235f704c` (`xpxtrw`). Historical `plkqvn` and published integrations untouched. No pushes/tags/version changes/publication/plogons/FRU encounter edits. Debug only.

At resume, `koonkp` was empty: no partial P5 code existed to recover. P5 was implemented there, followed by P6 and P1. Current source is saved in jj, not just temporary files. Reference archive `/tmp/dragonsong-asumech/Asunnya-AsuMech-b7a31b6` can expire; restore exact pinned public source from inventory rather than using the older local Asu checkout. No unresolved automated failures.

## Delivered catalog and evidence

Eleven DSR entries: existing Strength1/2, Sanctity1/2; P3 Nidhogg; P4 Eyes; P5 Wrath/Death; P6 Wyrmsbreath→Wroth and Wings2→Cauterize; P1 Holy See. Continuous P6 is deferred. No P7.

- [Source inventory](dsr-inventory.md): actual Asu master `b7a31b63` includes registered standalone P3 despite ambiguous README; upstream AnoMech `6aec4a59` has no DSR. Asu is reference, not ground truth. AGPL attribution preserved.
- P2: Sacred Severs knights now face along their first charge. Existing measured clocks/Elemental assignments preserved. No encounter rewrite.
- [P3 report/checklist](dsr-nidhogg.md): Easthogg, native environment correction, MT-only NW bait, varied independent random-lance preparation, safe post-tether party. Owner confirmed corrected P3 arena including title-screen offline. Other native fidelity is not certified.
- [P4 report/checklist](dsr-eyes.md): T/H red, DPS blue, Elemental allocations and North-relative Mirage priorities, rescue continuation/LB/healing/spear deadline. Native playtest pending.
- [P5 report/checklist](dsr-p5.md): Elemental Wrath and Dooms North/2-2/anchored Death, modern actions, persistent Liquid Heaven, native weather46/layout finalization, actionable meteor deadline. Native playtest pending.
- [P6 report/checklist](dsr-p6.md): triangle H1/H2/D4 anchors, dynamic5-1 D3 south, correct Wroth and Vow priorities, dual native actors/tethers/debuffs, square boundary. Native playtest pending.
- [P1 report/checklist](dsr-p1.md): MT Grinnaux/opening tether, ST Adelphel/Execution, south-first slashes, Playstation priorities, targeted interrupts, HMRT/prison/Haurchefant. Native playtest pending.

## Latest P3 visual follow-up

`xzvvss` corrects an objective action-layer conversion mismatch: coincident-target facing discarded the captured dive pose. It preserves pose, restores1.1s reference animation lock and center-facing tower emitters. Mechanics, snapshots, assignments, tower coordinates and visibility schedule unchanged. Existing pooled native emitters already send the dive/tower actions; no independent Action VFX row was found. This does NOT prove missing jump/tower visuals fully resolved. Retest all three jump types and towers in inn/offline with visible/hidden party; if still missing, capture native actor-state/ActionEffect sequences before changing visibility or clocks.

## Validation

All builds/tests Debug. Latest combined run: **769 passed,65.9s** (`/tmp/dsr-final-integrated.txt`). Covers P1/P3/P4/P5/P6, focused lifecycle/environment/catalog and fake inn/offline startup contracts. No native-rendering proof implied.

Focused milestones (overlapping, not additive):

| Work | Result |
| --- | --- |
| P2 prior existing suite | 3,804 passed,2m28s; exhaustive Meteor matrix subsequently moved to opt-in Extended |
| P2 bounded/facing |64 passed2s; eight facing regressions passed287ms after failing before fix |
| P3 choreography |37 passed12s; additional human-MT/target variation2 passed487ms |
| P3 presentation/lifecycle |41 passed14.07s, includes visible/hidden native delivery assertions and delayed creation |
| P4 prior integration |289 passed; stale safety artifacts rebuilt rather than guard weakened |
| P5 integration |212 passed13.6s; focused48 passed6s plus bounded48 randomized executions |
| P6 focused |57 passed12.87s, includes48 randomized executions, all seats/Easy/Hard, delayed actors, hidden party, pause/restart and deliberate failures |
| P6 integration |398 passed27s |
| P1 focused |31 passed11.98s; native interruptible-bar regression1 passed455ms |
| P1/P6 integration |296 passed24.85s |

Final ordinary+isolated optimized Debug safety: **default308 passed/3Windows-only skips18s; safetyhook308 passed/3skips18s**. Fresh isolated optimized Debug build succeeded3.25s, zero warnings/errors. Logs `/tmp/dsr-final-safety-default.txt`, `/tmp/dsr-final-safety-safetyhook.txt`; build `/tmp/dsr-final-safety-build.txt`. Firewall and stale-artifact guard unchanged. No relay changes, relay suite not rerun. No giant stress matrix added to routine tests.

## Reproduce / native acceptance

Use `DALAMUD_HOME=/home/kath/.xlcore/dalamud/Hooks/dev`. Standard `dotnet test tests/AnoMech.Tests -c Debug` with focused filters above. Safety requires ordinary `AnoMech/bin/Debug/FluffySim.dll` and isolated optimized Debug `/tmp/dragonsong-final-safety/FluffySim.dll` (Optimize=true, DefineConstants=TRACE, IntermediateOutputPath=obj/safety-Debug). See tests/SafetyTests/README.md. A jj checkout refreshes source timestamps: rebuild both artifacts after checkout before safety tests. Never weaken the stale guard or write Release output.

Native acceptance is the next work. For each phase: inn/title-screen cold entry; floor/scenery/weather/collision and native actors; each strategy variation and human role; visible/hidden party; native cast/tether/status/VFX snapshots; deliberate failures; God Mode/Pause/Restart/Leave/reentry and cross-phase handoff. Detailed ordered lists are in each phase report.

No new native snapshot/capture was available here. Some P5/P6/P1 clocks/radii remain inherited or inferred; P1/P6 HP/DPS budgets and several recovery models are deliberate practice abstractions, listed explicitly in reports. P4/P5/P6/P1 are marked incomplete pending acceptance. Do not call them native-verified or publish a release before owner playtesting.

## Native repair checkpoint

Repair `qvoykv` uses all six existing phase tips as genuine dependencies and preserves the previous integration resolutions. [Repair evidence/menu/root causes/playtest checklist](dsr-native-repairs.md). No new scenarios, stable Name/FullName keys retained, UI-only labels and chronological ordering. Shared DSR appearance path fixes missing demihuman gear and story-NPC customize/equipment; packet-before-draw initialization, loaded-model-slot readiness checks, meshless-helper exemption. P1 explicit local shove, anti-knockback and baseline-relative chains. P6 per-player timed bleed exposure with provisional timing, no frame/overlap multiplication.

Validation so far: main affected-phase/entry/FRU presentation622 passed50.04s; final focused25 passed2.75s; final P1/repair53 passed13.03s; incorrect pair-direction test passed505ms. Native visuals unconfirmed; no real P6 timing capture found. Final integration/safety results follow.

Final repair integration: `xwtnow` now has repair `qvoykv` as its parent; `jj simplify-parents` removed six redundant direct edges, with all six original feature tips explicitly verified as ancestors through the repair's six-parent merge. No published history or concurrent feature was discarded. Repair baseline exactly retained the old integration resolutions. Integrated focused/lifecycle/catalog/appearance95 passed4.92s (`/tmp/dsr-repairs-integrated.txt`). Fresh isolated optimized Debug build3.02s, zero warnings/errors. Both safety backends308 passed/3Windows-only skips18s each, against ordinary Debug and `/tmp/dragonsong-native-repairs-safety/FluffySim.dll`; logs `/tmp/dsr-native-repairs-safety-default.txt` and `...-safetyhook.txt`. No firewall/stale-guard edits. No Release builds, publication, version/configuration changes, plogons or FRU encounter edits. Retest native visuals/P1 shove and calibrate provisional puddle cadence before acceptance. Working integration retains its exact description `chore: integration commit`.

## Final native polish checkpoint — 2026-10-21

Owner confirms previous repairs natively: P1 visible bosses/knockback/chains, P4 dressed NPCs, P5 visible Thordan in both entries, P6 survivable puddles and working arenas. P6 provisional puddle cadence is unchanged.

Anonymous feature `vmsloq` builds on repair `qvoykv`, retaining all six completed phase ancestors and prior integration report updates. Scoped work: P1 Dimension omen-only suppression and actual prison pull; P5 persisted valid assignment preferences; central job-specific retry recasts and charge/late-readiness handling. Evidence: [P1](dsr-p1.md), [P5](dsr-p5.md), [cooldown engineering/native checklist](dsr-practice-cooldowns.md). No new encounters, release work or FRU encounter edits.

Focused checks so far: P1 baseline32 passed10s; P5/recast243 passed12s; new focused74 passed12s (overlapping totals). Isolated optimized **Debug** build succeeded5.385s wall/5.24s build, zero warnings/errors. Default safety313 passed/3Windows-only skips17s test/21.08s wall. Final integration and bounded regression results to follow. Native acceptance remains required for the new omens/pull and hotbar reset behavior.

Final integration: `xwtnow` → `vmsloq` → `qvoykv` (all six original phase tips verified ancestors). Published `master@origin`/local master remain `xpxtrw`235f70, version2.2.0.0; historical release ancestry untouched. Integration preserves exact `chore: integration commit` description; one report-only rebase conflict resolved by retaining the complete feature progress report. Cooldown engineering report and its documentation allowlist are recorded in integration.

Final validation: integrated focused **102 passed15.3178s test/19.957s wall**; fresh isolated optimized Debug build **2.72s build/2.858s wall**. Final safety against ordinary plus isolated optimized Debug: **default313 passed/3Windows-only skips17.6085s; safetyhook313 passed/3skips17.8804s**. Logs `/tmp/dsr-polish-final-focused.log`, `/tmp/dsr-polish-safety-default.log`, `/tmp/dsr-polish-safety-safetyhook.log`.

Broader existing regression exposed four stale pre-repair catalog assertions: **3244 passed,4 failed,3m38s test/3m40.176s wall**. Detailed diagnostic subset **3174 passed,3 failed,3.5898min test/3m36.307s wall**, plus Sanctity2 **72 passed,1 stale-label failure,6.4354s** identified all four as old absolute menu indices/display labels. Those assertions now check correct identities/adjacency/current short labels while retaining stable FullName checks; all four pass in the final102. No mechanical failure was found. Legacy large bounded matrices were not added or expanded; Extended remains opt-in. No unresolved automated failures remain. Earlier broad command was canceled24.305s after detecting missing Extended exclusion; no result claimed for that canceled run.

Native acceptance checklist: P1 both Dimension cast bars/impact without floor warnings; actual pull from outside the prison with human/all bots, inn/offline, Pause/Restart/Leave; every P5 preference and persistence, valid late bait/Lightning exclusions, Doom/non-Doom cleanup; each tank invulnerability plus charged Oblation, short mitigation, anti-knockback and P1 interrupts on entry/retry, meaningful within-attempt cooldowns and original timers after Leave. Exact pull trajectory and live omen policy remain reference-supported rather than newly captured. See phase/cooldown reports. No Release build, push, version bump, publication, plogons edit, P7 or FRU encounter modification.

## Final P4/P5 gameplay repair checkpoint — 2026-10-21

Native acceptance received for P1 omens/pull and previous bosses/chains; P3 arena/mechanics; P4 NPC appearances; P5 presentation/preferences; P6 puddle survival; central cooldown restoration. No release work authorized. Anonymous repair `tnqkoz` builds on `vmsloq`, retaining the integration's three documentation files. Shared integration remains `xwtnow`; published master2.2.0.0 untouched.

P4 exact synthetic reproduction now matches native H1 error (yellow40.92→blue46.92, unsafe handoff inside6y blast plus marginal1.5y transfer). Fixed centre handoff and safe support retreat; no orb/damage/lockout weakening. P5 hardcoded MT hits replaced by mandatory MT→ST local Provoke state in both entries, proper post-Heel positions/facing and actual-recipient diagnostics. Human ST is not auto-assisted; seven bots use ordinary local mitigation and the bot ST swaps. All four jobs share the same policy. Phase evidence/reproduction/native checklists updated.

Focused run391 passed31.461s before the final strategy-position refactor/additional job cases. Earlier18 recipient/failure checks passed3.6576s. Routine suite excluding Extended is in progress (`/tmp/dsr-gameplay-routine.log`); final integration and both safety backend results will follow. Remaining uncertainty: native centre routing, actual hotbar Provoke, client-facing prompt/target behavior and deliberately coarse20% practice mitigation threshold. P7/release/plogons remain untouched.

Integrated repair: `xwtnow` → `tnqkoz` → `vmsloq` → `qvoykv`; all six original DSR phase tips explicitly verified as ancestors. A report-only rebase conflict retained the complete updated progress report. The final MT-reclaim edge case was folded back into the anonymous repair while keeping the integration revision and description. No published history, FRU encounter, P2/P3/P6 mechanics or native arena/appearance pipeline changed.

Final affected integration run **395 passed35.1504s test/41.275s wall**, including all eight manual orb roles, every seat/difficulty, all four tank jobs, both P5 entries, failures and lifecycle. Final expanded tank-swap check **14 passed4.3466s** additionally verifies actual wrong-holder retargeting after deliberate MT reclaim (overlaps earlier counts). Ordinary Debug build succeeded3.50s build/3.662s wall. Initial integrated safety both313 passed/3Windows-only skips19.8896s/19.9996s, before the final reclaim adjustment; fresh final safety and routine results follow. No native rendering/Provoke execution proof inferred from these results.

Final results: ordinary non-Extended routine feature snapshot **7,122 passed9.4724min test (9m28.344s)/9m34.533s wall**, `/tmp/dsr-gameplay-routine.log` and ignored `TestResults/dsr-gameplay-routine.trx`. The subsequent narrow P4/P5 positioning/bot-mitigation refactor and MT-reclaim edge case are covered by the final integrated395 and final14 checks above; the entire routine was not repeated after those scoped changes. No new stress matrix or Extended run was introduced.

Fresh final isolated optimized **Debug** build succeeded3.22s build/3.376s wall. Both safety backends against fresh ordinary and optimized Debug DLLs passed: **default313 passed/3Windows-only skips20.0936s test/20.687s wall; safetyhook313 passed/3skips19.8168s test/20.436s wall**. Logs `/tmp/dsr-gameplay-final-safety-default.log` and `/tmp/dsr-gameplay-final-safety-safetyhook.log`; artifacts `/tmp/dragonsong-gameplay-safety/FluffySim.dll` and `AnoMech/bin/Debug/FluffySim.dll`. Firewall and stale-artifact protections unchanged. No unresolved automated failures.

Recovery: feature `tnqkoz`efa3b8, parent `vmsloq`; current shared integration `xwtnow` contains that feature and final reports. All six original DSR feature tips remain ancestors. Published `master`/`master@origin`235f70 and historical `plkqvn`9810be remain intact. No Release build, release preparation, push/tag/publication/version bump, plogons edit or new scenario. No FRU encounter source changes. Native acceptance remains required: both yellow support pairs and each human DPS centre→own blue orb, delayed handoff with no bot deaths, deliberate double-soak/lockout and full Mirage progression; both P5 entries on all tank jobs, MT mitigated Heel→bot ST's three cleaves, human ST actual hotbar Provoke after prompt, no bot reclaim, wrong/late Provoke and expired mitigation, inn/offline/Pause/Restart/Leave with cooldown restoration. See phase reports for coarse mitigation/model limitations.

## P4 missed collection + supplied logo checkpoint — 2026-10-21

Actual native Dalamud log identifies D4 bot's NE blue orb(10,-7) as missed in human-MT attempts548184525 and1585846992. Latest inspected complete attempt has red intercepted by ST45.77, only transferred to D448.78, then burst49.07. Root: centre-only waiting DPS plus blue-support retreat intersecting outstanding red support. Previous tests scripted the human into centre and missed that route. Exact old-code deterministic reproduction fails49.07. See dsr-eyes.md for sanitized evidence and native checklist; raw native logs are not committed.

Anonymous repair `yyutvt` builds on `tnqkoz`, preserving previous integration reports. DPS now approach eligible red supports (including a human staying at yellow); completed supports hold clear until DPS have red and retreat through centre. Physics, damage, lockouts, ownership collisions, native arenas and P5 swaps unchanged. Structured local event-only JSON and human/bot missed-orb announcements added. Initial corrected P4 run127 passed17.9649s; final native-seed variations and integration validation follow.

Independent branding feature `upllop`, parent published master@origin, replaces only images/icon.png and keeps README display at64px. Artwork is the supplied1254×1254 RGBA PNG, Lanczos downscaled to512×512, optimized PNG with alpha0–255. The original remains untouched at its supplied local path. Existing manifest URL, embedded resource path and packaging references remain; no installed binary, version or plogons metadata changed. Integrate both features into existing xwtnow without serially chaining branding to encounter code.
