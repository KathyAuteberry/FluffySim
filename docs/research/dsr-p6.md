# P6 Double Dragons

Implemented two isolated entries: Wyrmsbreath to Wroth Flames (100s) and Hallowed Wings 2 to Cauterize (70s). Continuous P6 is intentionally deferred. P7 is excluded.

## Evidence and adaptation

Primary strategy: https://ffxiv.tuufless.com/elemental/dsr/06_double_dragons/ . AsuMech b7a31b630ebae5e1076f65ed069f3a50dda64272 supplies assignment state, initial route structure and reference clocks. Its legacy packet/DamageSolver implementation was replaced with EnemyActions and normal world-owned actors. Preserve AGPL attribution in THIRD-PARTY-NOTICES.md. BossMod P6WrothFlames corroborates 6y Akh Morn stacks/voidzones and 44x3 Flame Blast crosses. These are reference values, not local capture measurements.

Elemental changes: WB1 H1/H2/D4 anchors, flex D1/D2/D3; four-person light parties; MT at north/south edge and ST equator for first Wings; Mortal Vow DPS→MT→ST→D1 (D2 if D1 first)→D3; Wroth spreads west, pairs east, MT/ST/H1/H2/D1/D2/D3/D4 center-to-edge priority; dynamic WB2 5-1 with D3 alone south toward glowing dragon, center selected H1/H2/D4 from the larger north element group. Final opposite-element dives retain front tank invulnerability requirements.

## Native/lifecycle contract

Territory 968, weather 3, BGM 926, readiness-gated existing Nidhogg environment finalizer. Director layout arguments 1,0x2D, then 1,0x3C for WB2; map 758. Both native dragon actors (12612/12613), 13238 Scarlet Prices, pooled invisible effect carriers, native breath tethers with slack state, statuses and Akh Morn EObj 0x1EB683. A square 22y practice boundary permits the valid corner routes, replacing the P3 circular fence. The native square walkable geometry still needs owner verification; fake tests cannot establish collision fidelity.

Helpers are created at entry before effects, reused only after release, and owned by the world. Action observer is unsubscribed on teardown. Seven bots use normal AI movement; all human seats, Easy/Hard and hidden party modes use the same mechanics. Pause/Restart/Leave/God Mode use shared lifecycle and packet isolation. No server-bound synthetic actions.

## Limits requiring native checks

No local P6 IINACT capture was available. Reference clocks, breath 21y stretch threshold, 6-degree half-angle, glow 15-degree cone, 20y Blizzard safe hole, 1.1s puddle activation, 1y Pyretic drift and 10y Touchdown lethal radius remain UNVERIFIED practice values. Native sheets provide presentation shapes; hand-authored breath/cross/wing checks supplement custom geometry. HP starts balanced at 70%; targeted actions decrement 0.2% as a qualitative practice budget, not a potency model. Optional initial HP difference demonstrates the 3% Akh Afah failure and native HP-link tether. There is no continuous DPS/enrage simulator or complete Hraesvelgr enrage escalation. Deep Freeze uses shared status movement locking. Element expiry uses 12s strategy reference rather than AsuMech's ~11s estimate.

## Validation

57 focused Debug checks passed, 12.87s wall time: 48 bounded randomized executions, all eight seats on Easy/Hard, hidden-party alternation, delayed actor creation, repeated entry/teardown, pause, square corners, missing Vow receiver, HP imbalance and both human tank invulnerability failures. Catalog/integrated and firewall results are recorded in dsr-progress.md.

## Native playtest

- Enter each half directly from inn and title screen; verify square floor/collision, weather, dragon size/facing and director scenery, no P1 residue or flicker.
- Check two simultaneous casts, glow mouth tells, six taut/slack tethers and actual cone visuals.
- Verify triangle and 5-1 from each role; deliberately overlap/shorten a tether.
- Wings near/far tank targeting, simultaneous dive/hot lanes and actual hit geometry.
- Every Vow handoff, atonement expiration before final D3 pass, wrong/missing receiver failure.
- Wroth crosses, four stack/puddle snapshots, safe routes around persistent puddles, center-to-edge allocations.
- Pyretic stillness/actions, Deep Freeze, opposite-dive cleansing, front tank invulnerability, Touchdown.
- Pause during tethers/statuses, Restart during Wroth and closing dives, Leave and enter P2/P3/P4/P5; visible/hidden party and God Mode. No native claim until owner playtests.
