# Spectator garage development batch

All 28 currently selectable FRU/DSR catalog entries (30 strategies) accept JESUS TAKE THE
WHEEL. The full entry-by-entry responsibility and coverage matrix is in
[SPECTATOR-COVERAGE.md](SPECTATOR-COVERAGE.md). This includes both P6 practice halves,
upstream alternatives, existing incomplete-labeled entries and the continuous Pandora sequence.
Existing practice approximations and intentionally lethal terminal options remain unchanged.

## Encounter behavior

Support is inherited from the three FRU/DSR zone implementations instead of a three-scenario
allowlist. Every watched role remains an ordinary autonomous participant, subject to normal
assignments, damage and failure. Eight bots replace seven bots plus a human; the parked native
player is absent from simulation membership. The approved camera implementation is unchanged.

Bot Provoke now has a simulated action presentation alongside the existing aggro transfer.
Strict Paradise wing checks now examine every hit tank, and bot tanks execute their job's
invulnerability action and receive its normal simulated status. Continuous Pandora repeats
this across both Paradise stages. Existing bot LB3, gauge consumption, protection, healing,
rescue, tether handling and movement infrastructure were retained. No synthetic hotbar inputs
or server action requests were introduced.

## Party displacement

Eight simulated actors were correct, but the native HUD's separately rebuilt self-first view
retained the real player and displaced the last bot. Dalamud reads shared group records;
installed DelvUI also reads the HUD identity table. Fixing only visible nodes cannot address
both. See [SPECTATOR-PARTY-PROJECTION.md](SPECTATOR-PARTY-PROJECTION.md) for the traced data paths,
projection, restoration and native limitations.

The watched bot occupies presentation row zero; the other seven follow in role order. Matching
actor identities are projected into existing group records, HUD member records and native
party arrays, including HP/MP, jobs and statuses. Exactly eight entries represent eight actors.
The real player actor and ownership remain intact. The authentic party snapshot is restored
on cleanup. A scoped HUD update hook keeps the projection correct even when DelvUI hides the
native party addon. Normal-practice presentation is unchanged.

## Validation

| Debug validation | Result |
|---|---|
| Solution build | Passed; 14 existing NUnit analyzer warnings, no errors |
| Full spectator clears | 480: 30 strategies × 8 roles × Easy/Hard |
| Catalog restart/failure/leave cases | 60: every strategy, normal and intentional-failure paths |
| Final focused suite | 670 passed, including the full matrix, party mapping and mechanic checks |
| Bounded ordinary FRU/DSR, spectator, GCD, offline and catalog regressions | 6,788 passed |
| Relay | 67 passed |
| Packet safety, ordinary and optimized TRACE Debug, default backend | 316 passed, 3 skipped |
| Packet safety, ordinary and optimized TRACE Debug, SafetyHook backend | 316 passed, 3 skipped |
| DelvUI 2.8.0.0 installed assembly contract | Passed within safety suite; read-only inspection |
| Deliberately stale Debug artifact | Correctly rejected by freshness check |

Focused checks cover real simulated Provoke/aggro recipient changes, strict invulnerability
statuses through continuous stages, meaningful LB3/gauge/protection, all watched-role slot
mappings, D4 retention, identity consistency, status bounds, death refresh and selective cleanup.
Headless lifecycle tests cover pause/resume, restart, intentional failure, role/scenario changes,
normal↔spectator transitions, inn/offline restoration and actor/camera/party cleanup. Combined
catalog tests assert eight unique presentation identities, all roles, watched-first ordering and
absence of the real player's identity. Test totals overlap; they are not additive.

The broad regression run excluded Extended and Integration categories. Final focused and packet
checks were rebuilt after the final HUD-instance guard. No Release build or installed binary
modification occurred.

## Jujutsu topology

```text
xopvqt (original spectator)
  ├─ nopmvn (full catalog support) ─┐
  └─ zzpnxq (party projection) ─────┤
okoqxr (P3 randomization) ──────────┴─ tkzqks
                                     chore: integration commit
```

The existing unpublished shared integration `tkzqks` was reused. Both spectator follow-ups
branch independently from `xopvqt`; P3 randomization merges directly into `tkzqks`.
Neither follow-up is an ancestor of the other. The topology correction preserves the exact
validated source/test tree; only this topology documentation changes. Published master,
upstream ancestry, previous feature branches,
P3 randomization/visual fixes, GCD improvements and historical six-parent `plkqvn` remain intact.
No release preparation, version/changelog change, master advancement, push, tag or plogons edit.

## Remaining native verification

The maintainer has approved the existing native DSR P3 camera experience. This batch does not
claim native HUD or all-encounter visual verification from headless results.

Retest native and DelvUI displays with every watched role, especially a tank real player watching
D4: exactly two tanks, two healers and four DPS; no parked-player entry or displaced participant.
Check row clicks, status hover, HP/MP, icons and provisional party cast-progress scaling. Repeat
with DelvUI hiding the native addon, in inn and offline sessions, across restart, wipe/completion,
mode/role/scenario changes, Leave and unload. Sample FRU/DSR Provoke, rescue/LB3 and continuous
phase transitions visually. Teardown may briefly leave an empty HUD until the next native
refresh; synthetic enmity and social/ready-check self behavior are intentionally not emulated.
