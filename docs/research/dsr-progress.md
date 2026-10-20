# Operation Dragonsong progress

Updated 2026-10-20. Mission active; P7 excluded. Baseline published 2.2.0.0 `235f704c` (`xpxtrw`), working change `wlkspx`. Clean baseline verified against live public API. One workspace; no unpublished integration found. Local master stays at release. Historical integrations unchanged.

## Order and state

1. Source inventory written: [inventory](dsr-inventory.md). Live Asu `b7a31b63` (27 commits newer than cached comparison); upstream `6aec4a59`, no DSR.
2. P2 audit in progress; no implementation changes yet. Preserve measured clocks and Elemental rules.
3. P3 pending.
4. P4 pending.
5. P5 Wrath/Death pending.
6. P6 pending.
7. P1 pending (last).

## Validation

No build/tests executed yet in this mission. No native checks performed. Inventory has per-phase playtest gates. No release builds, publication, version changes, plogons or FRU edits.

## Recovery

Reference source archive: `/tmp/dragonsong-asumech/Asunnya-AsuMech-b7a31b6`; exact pinned GitHub source is linked in inventory if temporary files expire. Local reference Asu checkout remains at older `d3f4f3`; do not mistake it for live head. SSH fetch fails on host system config permissions; read-only public API succeeds with network permission. Native evidence for phases beyond P2 still requires extraction/corroboration. Next: finish P2 defect review and focused regressions before P3.
