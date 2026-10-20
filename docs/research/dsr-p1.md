# P1 The Holy See

Independent Elemental practice entry from pull through Pure of Heart; no P2 drill prerequisite. Seven bots, all seats and Easy/Hard use shared AI/lifecycle. P7 excluded.

Primary strategy: https://ffxiv.tuufless.com/elemental/dsr/01_the_holy_see/ . Assignment state, initial AI structure and reference clocks adapted from AsuMech b7a31b630ebae5e1076f65ed069f3a50dda64272/P1Knights (AGPL). BossMod DSW1 corroborates 6y Empty/Full Dimension, six Bladedance hits, 4y Heavensblaze stack requiring seven, 5y Execution and 3y/2.5s Skyblind. No new local IINACT timing capture.

## Implemented

- Native Adelphel/Grinnaux, MT Grinnaux and ST Adelphel; MT intercepts random passable Adelphel tether, invulnerability requirement and six frontal Bladedance hits. Actual farthest Heavensblaze target is locked at cast start and must share with seven.
- South-first random Hyperdimensional marks, complementary north second set, four-person cone, native wall tears with overlap/approach failures.
- Random cardinal Adelphel arrival/hourglass direction, native Shining Blade casts/movement, Brightspheres and portal detonations, ST Execution invulnerability/separation.
- Playstation role-constrained random pairs; D1>D2>D3>D4 west-to-east circles, initial-length-relative chains, Faith Unmoving knockback, outward follow-through, eight Heavensflame spreads.
- ST/D3/ST interrupts; human uses actual Interject/Head Graze targeted at Adelphel; missed interrupt heals bosses and fails practice.
- Random Empty/Full Dimension, Planar Prison, Charibert/Zephirin, friendly Haurchefant and spear, shockwaves; HMRT Brightwing nearest-pair baits, resistance lifecycle, alternating Skyblind drops, Pure of Heart.
- Territory968/weather2/BGM920, readiness-gated Holy See environment, layout1/map758, prison layout0x14 and native EObj0x1EB681. Square22y practice boundary, independent P1 waymarks. Shared pause/God Mode/restart/leave and isolated local actions.

## Explicit limitations

Reference clocks are not locally native-confirmed. Third interrupt and prison-tail clocks inherit Asu's extended scripted sequence. Knights' HP push is scripted, Charibert is placed at29% before Pure of Heart; this is mechanics practice, not a DPS/health simulator. Missed Hallowing and unbroken chains are immediate practice failures rather than a complete recovery/healing model. Haurchefant's doomed P1 sequence has native actors/effects but no full health/death animation simulation; it is distinct from P4's rescue. P1 ends before P2 transition, preserving independent entries. There is no complete auto-attack/party-healing budget. Portal5y proximity/8y pair/7y orb thresholds, chain30y stretch and9y prison radius are inherited UNVERIFIED practice geometry. Shining Blade orb spawn clocks, knockback16y/20speed and Skyblind drop+5.48s need native corroboration. Actual sheet effect shapes and casts use native presentation; no approximate scenery or synthetic server traffic.

## Validation and native checklist

31 focused Debug checks passed in11.98s:24 randomized assignments, all eight seats Easy/Hard with delayed packet creation, repeated entry/cleanup, circle priorities and human missed-interrupt/invulnerability failures. Integrated contract/safety results in dsr-progress.md.

Owner checks required:

- Inn and title-screen offline direct entry, correct P1 square floor/collision/weather, native boss models/names, waymarks and facing.
- MT tether interception and north-facing Bladedance while other seven share Heavensblaze; human MT/ST invulnerability.
- South-first slashes, visible wall portals and realistic portal/orb interaction; every landing/dash direction and ST Execution.
- Symbol presentation, knockback, chain breaking and no flame overlap; human ST/D3 targeted interrupts.
- Prison geometry/environment transition, Haurchefant/spear presentation, HMRT cone/buff/puddle timing, Pure of Heart.
- Visible/hidden party, pause, God Mode, Restart during portals/chains/prison; Leave→P2/P3/P4/P5/P6 and repeat entry. Native visuals/collision remain unverified until owner playtests.

## Native playtest repair follow-up

Demihuman knight appearance payloads restored; explicit local knockback and creation-distance-relative chains implemented. See [root causes, validation and retest checklist](dsr-native-repairs.md). Native rendering/results await owner confirmation.

## Final refinement — 2026-10-21

Owner playtesting confirms the previous native boss visibility, Faith Unmoving displacement and distance-based chains now work. Those paths are unchanged.

Empty/Full Dimension now delay only the native floor omen beyond the cast lifetime (6s versus the sheet's 5s, adjusted to 4.7s by the shared action pipeline). Cast bars, action IDs, impact effects, collision geometry and absolute damage events remain intact. Pinned AsuMech P1 explicitly uses `CastWithoutOmen` for both casts. Current Action rows25306/25307 contain omen references360/1; that establishes available shapes, **not** whether the live encounter displays them. BossMod corroborates donut/circle damage geometry but is not proof of native floor presentation. No new native capture was available: this correction follows the reference implementation and reported behavior; precise live presentation remains an acceptance check. No other P1 telegraphs were suppressed without evidence.

The prison transition previously applied imprisonment but never forced movement; bots walked in early and concealed the omission. At112.31 Grinnaux now emits Planar Prison25313 from the prison centre, consistent with AsuMech and BossMod's pull-to-Grinnaux definition. At112.71 the party receives tether0x35, one-second stun0x95 and existing imprisonment. Members beyond7y are carried to(-11,0,1). The bot pre-walk is removed. The existing local CarryTo path emits ActorControl0xF1 for the human and a bounded eased carry trajectory for simulated members; no instantaneous teleport or new server-bound movement path. The native ring/layout events and114.26 boundary activation are preserved. Exact native trajectory and inherited7y eligibility threshold remain unmeasured; this uses the existing supported carry contract, not a claim of capture-perfect motion.

Retest both Dimension casts/impacts, prison pull with a stationary human outside the ring, hidden/visible party, Pause/Restart during the pull, Leave/reentry and subsequent HMRT baits in inn/title-screen modes. Working Faith Unmoving/chains require a brief regression check.
