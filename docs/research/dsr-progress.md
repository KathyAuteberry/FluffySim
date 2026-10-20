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
