# P4 Eyes — implementation and native playtest gates

Updated 2026-10-20. New catalog entry: **Eyes (Elemental)** under **P4 Eyes**. Native verification pending; P3's successful arena playtest does not verify this phase.

## Strategy and provenance

Primary: [Elemental Eyes](https://ffxiv.tuufless.com/elemental/dsr/04_eyes/). Pure of Heart uses the linked HMRT sequence: [Elemental Holy See](https://ffxiv.tuufless.com/elemental/dsr/01_the_holy_see/#pure-of-heart). This implements only the P4 rescue continuation; the independent P1 door boss remains last in the mission.

Adapted AsuMech's actual live source at `b7a31b630ebae5e1076f65ed069f3a50dda64272`, P4Eyes and the Pure of Heart portion of P1Knights (AGPL; THIRD-PARTY-NOTICES.md). The cached local Asu checkout is older. Asu P4 is NA, uses legacy native packet methods, and ends without rescuing Haurchefant; it was not imported wholesale. BossMod's DSW2 P4Hatebound/P4Intermission/DSW2States and DSW1 PureOfHeart corroborate shapes, nearest-two Brightwing baits, status handling and relative intermission order. Neither reference is a local native timing capture.

## Implemented

- Native left/right eyes, Estinien, Alphinaud and the two soul ghosts, appearance timelines, soul lockons/tethers/buffs, eye separation and targetability.
- Eight delayed-spawn-safe hidden native Nidhogg divers. Random valid initial 4/4 colours and two-red-holder choices per Mirage pair.
- Actual proximity colour transfers, 3s Bound and Determined lockout. Fixed a reference-loop defect that could reconsider a new red holder during the same update.
- T/H obtain red at Estinien. Yellow west MT/H1, east ST/H2; DPS swap after yellow and take blue at D1 NW, D4 NE, D2 SW, D3 SE. Yellow requires two soakers; early, missing, blue-tether and repeated-fire-vulnerability failures resolve through EnemyActions.
- Mirage baits around the west eye. H1 ccw/H2 cw from true North first; MT ccw/ST cw second; first-hit DPS in D1>D2>D3>D4 priority third. Actual red holders determine random targets, with 4y splash and piercing vulnerability.
- Colour damage events track red-eye damage and blue-eye healing. Eye HP/DPS is a practice model, not a combat rotation simulator. Blue starts half HP and scripted DPS is offset by erroneous blue hits; both eyes alive at Steep in Rage wipes.
- P4 rescue continuation: weather-2 rewind, native prison event object, 9y prison boundary and status, Charibert/Zephirin/Haurchefant/spear actors, LB3 window after arrival/before spear hit, friendly NPC healing, HMRT nearest-two Brightwings and timed Skyblind puddles, Pure of Heart healing threshold, targetable spear/Pierce deadline. Bots perform LB/healing/spear damage; local resolved actions can protect/heal the ally and damage the spear. Tank puddle drop waits for ranged puddles to resolve.
- All seats, Easy/Hard movement, native late-spawn visibility/scale replay, normal world teardown and reused scenario objects. Existing engine Pause/God Mode/Restart/Leave apply.

## Native environment contract

Eyes uses territory968, origin(100,0,100), weather3/BGM923, director layout0x5A/map758 and slot8. Clears prior phase slots. It shares P3's readiness-gated native WeatherManager.Update and game-data scenery/collision signature, with one finalization per request/layout generation. DSR MapEffects and director messages now also queue while the target layout is cold. No held/suppressed layers, hand-built scenery or server-bound synthetic traffic.

The rescue explicitly selects weather2/director0x14. Weather2 now uses the same readiness pipeline, with a separate Holy See signature derived from the game's OBSB selectors and existing sanitized instance IDs: bg_02 active, bg_04/bg_05 inactive, native collision selections retained. This is a game-data-derived expectation, not an in-game snapshot.

## Fidelity limits

- No local P4 IINACT capture was found. Eyes absolute times, orb growth steps, 1.5y pass distance, touch margin and growth-to-model scale are inherited estimates. Orb radius >1.501 is corroborated by BossMod with its own verification caveat. These values need native comparison.
- Rescue timing combines BossMod relative measurements with inherited Pure of Heart mechanics; rewind layout/ring VFX and actual LB/heal presentation need native checking. It does not replay all visual time-reversal cinematics.
- Eye, Charibert and spear damage rates are scripted practice budgets, not player potency/HP calculations. Haurchefant heal amounts are simplified; a curated set of direct heals including Benediction is supported. Unsupported heals/HoTs/shields are not a full healing simulation. Blue healing penalty is a fixed practice increment. Displayed max HP comes from the native spawn pipeline, not Asu's hardcoded HP guesses.
- Unpopped or wrong-colour orbs deliberately fail the drill; retail may instead apply a large hit, damage down and eye healing. This conservative practice failure needs tuning from captures if it obstructs useful recovery practice.
- Dynamic orb scale is local native presentation; multiplayer scale replication remains outside this mission's solo-practice guarantee.
- No native correctness claim until owner playtesting. Keep IsIncomplete until the remaining fidelity gates are reviewed.

## Native playtest checklist

1. Enter from inn and title-screen offline: correct Eyes arena/scenery/collision, no P1 flash or P3 leftovers; both eyes face and drift correctly, all allies use the right models.
2. Verify soul marker/tether origins and both buffs. Deliberately miss one.
3. Check red/blue tether orientation, actual center swaps and 3s lockout. Repeat with tank/healer and every DPS seat.
4. Yellow shared pops, growth readability and orb disappearance; DPS transfer/blue pops at Elemental positions. Try early, missed, wrong-colour and double-orb hits.
5. Four Mirage pairs, clone appearance, exact splash/VFX and piercing lifecycle. Verify first/second/third swap priorities against North, especially two targets on one side.
6. Steep in Rage, eye defeat and colour damage/healing readouts. Check targetability and enemy list.
7. Rewind to Holy See scenery/ring. Haurchefant appears, tank LB3 protects only inside its window, friendly targeting/heals work. Check HMRT cones and puddles, final tank wait, Pure of Heart and spear/Pierce.
8. Pause during orb growth, swap lockout, Mirage and rescue. Restart/Leave at those points; re-enter P2, P3 and P4. Check stale tethers, statuses, actors, collision, scenery flicker and real-world return.

## Validation checkpoint

Focused Eyes/environment/NPC tests119 passed12s; North-relative destinations and pause subset19 passed4s. Integrated P2/P3/P4 run289 passed28s with one stale catalog-count assertion; corrected count test passes9ms, giving all290 covered checks passing across those runs. Both packet-isolation backends pass308 checks/3 Windows-only skips each in19s using ordinary and isolated optimized Debug artifacts. An intervening safety run rejected a stale artifact before analysis; rebuilt and resolved. No Release builds, pushes, tags, version changes or FRU/plogons edits. Feature `kltull`, shared infrastructure `ozrvwr`, integrated into `xwtnow`. Native checklist above remains open.

## Native playtest repair follow-up

Native NPC customization/equipment omission repaired for all four story NPCs and rescue actors. See [root causes, validation and retest checklist](dsr-native-repairs.md). Native rendering/results await owner confirmation.

## Orb handoff gameplay repair — 2026-10-21

Owner confirms story-NPC appearances; this follow-up addresses actual gameplay, not native model initialization. Elemental's centre handoff and North-relative Mirage priorities were rechecked against the primary guide. BossMod corroborates6y Flare Nova/Star and Bound and Determined's3s lockout; Asu's side handoff is not suitable for independent human timing.

Deterministic pre-fix reproduction: seed0, human D2, initially correct colours, wait at centre after41s and approach assigned SW blue orb at46.8s. H1 first takes yellow Flare Nova26817 at40.92 at(-14.4,1), gaining11.96s Fire Resistance Down (expires52.88). H1 subsequently waits at(-10,2.9) for D2. The blue orb at(-10,7) grows to radius2 and resolves at46.92; H1 is4.1y from its centre and receives Flare Star26815 inside the unchanged6y blast, dying to resistance. D2 at(-10,4.4000015) also fails red-tether eligibility: the two positions are marginally beyond the1.5y transfer limit. This is a real bot staging/transfer-margin defect, not evidence of an orb firing twice, and not attributed to incorrect human play. The first fatal orb for H1 was yellow, the second blue. Pre-fix log `/tmp/dsr-eyes-repro.log`; synthetic diagnostic trace remains ignored in TestResults, not committed.

Supports and DPS now meet at centre after yellow resolution. Supports that have handed off move down the central lane to the blue-eye huddle, outside both rows of6y blasts. Each red DPS goes only to its own blue orb, then its Mirage corner. An unswapped support/DPS continues meeting at centre rather than abandoning a delayed human at47s. This keeps the human's orb available through its normal deadline. Orb growth/ripe times,1.5y transfer distance,3s lockout,6y blasts, pair requirements, resistance duration/failure and burst deadlines are unchanged.

Per-run orb-hit and tether-transfer snapshots record actual damage recipients, time/position, colour and pre-hit resistance. Diagnostics include initial colours, bot allocations, native orb identities/positions, growth, transfers/lockout and resistance expiry. Tests cover all eight manually controlled human roles, delayed D2 (the original reproducer), two yellow support pairs, four distinct DPS blue hits, no repeated transfer during lockout, all four Mirage Dive target pairs, and deliberate support double soaking that still dies. Existing complete rescue/lifecycle/role/difficulty tests also run; no additional complete-Eyes failure was found synthetically.

Native acceptance: human DPS delay its centre handoff within the orb deadline; confirm its blue orb is not stolen and H1/H2 retreat safely. Play each yellow pair, then all four blue allocations; verify3s lockout, correct colours, deliberate double-soak death and all four Mirage swaps. Repeat inn/offline, hidden/visible party and Restart/Leave. Synthetic survival is not client-rendering or live position proof.

Validation results and final topology are recorded in [DSR progress](dsr-progress.md): integrated affected395 passed35.15s, final tank-target14 passed4.35s, non-Extended routine7,122 passed9m28s, and both packet-safety backends313 passed with3Windows-only skips each. Counts overlap. Native client playtesting is still required for these new gameplay repairs.

## Native missed-orb regression — 2026-10-21

Actual local execution history **was recorded** in `~/.xlcore/logs/dalamud.log`; no raw log/config/account information is committed. The previously inspected09:15:03 attempt (seed548184525, human MT) lost red from MT to **bot ST** at46.06 near(-12.34,0.017)/(-11,-0.6). D4 remained blue at centre and its NE orb(10,-7) burst at49.07. A newer completed attempt beginning09:20:34 (seed1585846992, human MT) shows the same defect more clearly: yellow pairs resolved40.90, other three DPS received red42.77, MT's red was intercepted by ST45.77, ST's3s lockout expired48.77, and D4 only received red48.78. At the49.07 burst D4 was still near(0.4,-0.2), with only0.29s to cover roughly11y. Three other blue orbs had resolved46.90. The missed orb was assigned to a **bot**, not the human. The earlier seed also fails deterministically headless at49.07 with the same uncollected NE orb before this repair.

The previous correction introduced two related choreography defects: DPS waited exclusively at centre instead of approaching a red T/H who legitimately remained at the yellow soak; already-blue supports retreated into that support's westward path and could intercept the outstanding red tether after their lockouts expired. It successfully prevented H1's second hit, but stranded the last DPS. This was not a blue hitbox, orb-respawn, movement-speed or resistance-state defect.

Blue DPS now approach available red supports after their yellow hit, preferring normal pairs and deterministically rematching remaining eligible colours when a physical exchange changes the pairing. Their target is safely within the existing transfer radius, not on its edge. Human T/H may remain at their valid yellow soak; no forced human movement or synthetic colour assignment is performed. Already-blue supports wait north/south of the central approach lane until all DPS have red, then retreat **via centre** to the blue eye. This avoids both tether interception and diagonal travel through an uncollected blue orb's6y blast. Each red DPS still approaches only its own blue orb. No radius, damage, growth/ripe time, burst deadline, lockout or resistance rule changed; reservation data is diagnostic, never enforced by silently ignoring collisions.

Why previous tests missed it: sunny-day runs drove the human with the same AI; manual human cases moved directly to centre on a fixed schedule. They never left a red human support on the real west-side route while blue bots retreated past it. The new native-seed regressions keep MT/H1 at yellow, reproduce the later west-side approach, verify the DPS actually comes for red before45s, reject support→support transfers during the orb window, verify all four real blue recipients before49.06, and finish all four Mirage pairs. Both bot difficulties and the two observed native seeds are covered with a small case set. A separate deliberately stationary human D4 still fails its own NE orb, and the diagnostic explicitly reports human responsibility.

Local event-level `[EyesOrb]` JSON records spawn/growth/soak/expiry with relative time, synthetic orb ID/position/colour/radius/ripe state, human role, intended soakers, actual party positions/destinations, tether colours, remaining lockout/resistance, recent transfers, recipients and explicit expiry reason. JSON numbers are culture-independent. Existing detailed transfer/hit logs remain. No per-frame trace, personal names/account IDs or network upload was added. Chat identifies the human's assignment at spawn and the assigned human/bot responsibility for a missed orb.

Native acceptance: remain at a correct yellow soak and let the assigned DPS approach; then move west without catching another blue support. Confirm D4's NE orb resolves, all four blue soakers are distinct, H1 stays safe and Mirage follows. Also deliberately omit a human DPS soak to verify clear failure reporting. Repeat inn/title-screen, Easy/Hard, Pause/Restart/Leave. The new choreography still requires in-game confirmation; retained thresholds/timings have not been recalibrated.

## Final rescue pull repair (3.0 preparation)

P4 spawned the ring and applied Imprisonment but never emitted Planar Prison's physical carry. A 110-second AI walk concealed this omission in headless runs. P1 already used Grinnaux's action 25313, tether 0x35, one-second stun and `CarryTo` outside the seven-yalm inner area. P4 now uses the same existing local ActorControl 0xF1 carry path, with Grinnaux's native appearance, at 112.3 seconds. Boundary enforcement waits until 113.85 seconds, after the carry settles, instead of killing players during displacement. The premature AI walk is removed. Orb, Mirage, LB3 and rescue deadlines are unchanged.

Focused P1/P4 tests assert actual stationary-human displacement, local carry emission, party positions and cleanup; the initial focused run passed 98 checks in 27.805 seconds. Existing offline/inn initialization contracts remain applicable. Fake-client movement is evidence of the local movement contract, not native visual proof: playtest the pull from both entry modes, including Restart and Leave during the carry.
