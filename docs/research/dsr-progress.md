# Operation Dragonsong progress

Updated 2026-10-20. Mission active; P7 excluded. Baseline published 2.2.0.0 `235f704c` (`xpxtrw`), inventory change `wlkspx`, P2 change `ktmmtz`. Clean baseline verified against live public API. One workspace; no unpublished integration found. Local master stays at release. Historical integrations unchanged.

## Order and state

1. Source inventory written: [inventory](dsr-inventory.md). Live Asu `b7a31b63` (27 commits newer than cached comparison); upstream `6aec4a59`, no DSR.
2. P2 audit complete: corrected Sacred Severs knights’ initial facing to follow their first charge. No clocks, assignments or damage rules changed. Added eight facing regressions (all failed before, all pass after), 64 bounded all-seat/both-difficulty/delayed-native tests, moved the 1,620-case Meteor matrix to opt-in Extended. Ready for integration after two-artifact safety passed.
3. P3 pending.
4. P4 pending.
5. P5 Wrath/Death pending.
6. P6 pending.
7. P1 pending (last).

## Validation

Debug build succeeded. P2 existing mechanics/lifecycle: 3,804 passed, 2m28s (includes exhaustive Meteor matrix, before marking Extended). New bounded set: 64 passed, 2s. Facing regression: eight failed before / eight passed after, 287ms. Ordinary-only safety: 307 passed, three Windows skips, one artifact-coverage failure because optimized Debug input was omitted; rerun with both artifacts: 308 passed, three Windows skips, 16s. Isolated optimized Debug build: zero warnings/errors, 4.72s. No native checks performed. Inventory has per-phase playtest gates. No release builds, publication, version changes, plogons or FRU edits.

## Recovery

Reference source archive: `/tmp/dragonsong-asumech/Asunnya-AsuMech-b7a31b6`; exact pinned GitHub source is linked in inventory if temporary files expire. Local reference Asu checkout remains at older `d3f4f3`; do not mistake it for live head. SSH fetch fails on host system config permissions; read-only public API succeeds with network permission. Native evidence for phases beyond P2 still requires extraction/corroboration. Next: P3 after integration. P3 diagrams were reviewed from local Tuufless images. BossMod corroboration exposes Asu’s incorrect 6y donut hole and minimum-only tower check; use 8y boundary and exact occupancy. No local P3 action capture found.
