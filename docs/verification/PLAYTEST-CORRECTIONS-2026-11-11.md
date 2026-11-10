# TEA Mana and DSR P7 playback corrections

This pass addresses the November 11 playback review. Temporal Stasis feedback
was withdrawn by the owner: the simulator had been manually paused. No Stasis
assignment, timing or presentation change was made.

## Corrections and source decisions

| Entry | Result | Evidence and limits |
|---|---|---|
| TEA P1 | MT pulls to center, crosses north before the opening cleave; ST takes the Hand north of the southern party after the kite | Both supplied clear poses and Fuccdayo diagrams support tanks ahead of the party. Eight arena orientations check actual attack targets and native source facing. [Ledger](../tea/P1.md). |
| TEA 1211 | Odd and even baiters face outward | The recordings support separate odd/even radial lanes; those lanes remain. No unsupported conversion to one wall radius. [Ledger](../tea/LIMIT-CUT.md). |
| TEA P3 opener | MT takes the first invulnerability set south at C; ST takes the second | Nukemaru 04:27–04:52 explicitly describes south-facing Prime. Regional choices stay in Mana AI policies. |
| TEA P3 flames | Every flame selects the nearest living player, including a previous baiter; both tanks really withdraw | Removed the artificial previous-bait exclusion. Repeat hits still punish vulnerability. The party remains in its guide-defined sector and naturally becomes nearest for flame three. [Ledger](../tea/P3.md). |
| TEA P4 Motion | Sustained real back-and-forth bot movement across Final Word and Calibration α verdicts | Tests inspect native-proxy travel and reversals, not just verdict success. Final Word movement preserves west/east beacon separation. Actual client visibility needs another playback. |
| TEA P4 healer spread | Retained H1/H2 on the boss's center row | Existing assignments match the macro; dedicated position coverage added. |
| TEA P4 prison | Retained the explicit practice default; did not invent a universal H2→ST→MT→H1 priority | The primary narration says lowest remaining damage contribution first and illustrates healer/tank/ranged/melee groups. Exact job/burst ordering is party-dependent; live DPS is not modeled. [Source discussion](../tea/P4.md). |
| DSR P7 | Boss tracks actual threat; Exaflare, Akh Morn and enrage-tower frames snapshot facing at cast start | Telegraphs, explosions and towers share the frozen frame. Bots prepare the current threat holder before casts; human Provoke remains required. Gigaflares remain arena-relative without evidence to rotate them. [Report](../encounters/dsr/P7-PORT.md). |

The P7 playtest diagnostics identify four deaths: a bot ST alone in an Akh Morn
tower without invulnerability on a follow-up hit; two MT fourth-wave Exaflare
clips; and MT's third same-element Trinity. These are simulator diagnostics,
not retail measurements. Exaflare deaths now report center distance and the
collision radius. The six-yalm radius, seven-yalm propagation and existing
cadence were retained; no arbitrary timing or radius nerf was applied.

The first P7 facing fix exposed a bot setup command occurring after its release
command. Setup now happens before the cast. Preparing the actual aggro holder
also prevents a delayed human Provoke from rotating the tower frame merely
because the AI expected a swap that had not happened.

## Verification

The complete non-Extended Debug regression passed **9,589 tests**, zero failures.
Focused verification passed **532 cases**, plus all **52 legacy P4 cases**.
Both fresh Debug plugin builds passed with zero warnings/errors. Both packet-safety
backends passed against ordinary and optimized TRACE Debug artifacts: **319 passed,
four platform-dependent skips each**. Relay tests passed **67/67**; Python tooling
passed **86/86**, including packaged-document link checks. All 139 checked local
Markdown destinations resolve across 94 documents.

| Correction | Follow-up change | Existing branch parent |
|---|---|---|
| Living Liquid pull/busters | `pwzytt` | `tmyyov` |
| 1211 outward facing | `youyuy` | `wvylpr` |
| Mana P3 opener/fire baits | `owukoz` | `wktzsn` |
| P4 visible Motion | `trmwou` | `yulpul` |
| DSR P7 actual-threat facing | `utkwxp` | `vlnvmv` |

Each follow-up replaces its existing branch tip as a parent of `wxyvwv`;
the shared integration retains its exact description `chore: integration commit`
and all twelve concurrent workstreams. Extracting these follow-ups preserved
all 1,083 tracked paths, content hashes and file modes exactly. Subsequent edits
update verification notes, documentation links and the explicit packaged-document
entry. The original encounter code was not flattened or discarded.

Extended suites remain opt-in. No installed DLL, release version, master
bookmark, tag or publication is changed.

Native retests should check P1 pull and both buster facings, all LC numbers'
outward poses, P3 south-facing opener and three genuine fire baits, and visible
Motion reversals with both verdict orders. For P7, move the threat holder before
casts, verify rotated omens and floor alignment, then move during the cast and
confirm the locked mechanic frame. Compare visible Exaflare impact/omen timing
against a retail recording before claiming its remaining feel is exact.
