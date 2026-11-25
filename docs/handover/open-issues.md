# Open issues

Known inaccuracies, unresolved source conflicts and approaches not to retry, as of the
2026-11-11 handover. Encounter-level detail and checklists remain in `docs/encounters/*/ROADMAP.md`,
`docs/research/fru-native-playtest.md`, `docs/research/dsr-progress.md`, `docs/tea/IMPLEMENTATION.md`
and `docs/verification/PLAYTEST-CORRECTIONS-2026-11-11.md`. Add new conflicts here; never resolve
one silently.

## 1. Open source conflicts

| # | Topic | Sources | Current code | What would settle it |
|---|---|---|---|---|
| C2 | **ActorCast byte 3**: omen delay or base cast time | Codex read it as omen delay (tenths). BossMod `ServerIPC.cs` names it `BaseCastTime100ms` | DSR Spiral Thrust `OmenDelay = 5.0f` (byte 50 in 123 casts, cast 5.7 s); DSR P3 Geirskogul `OmenDelay = 3.5f` (byte 35, cast 4.2 s → 0.7 s warning). TEA casts show 0 and no TEA value depends on it | Watch either telegraph in a Duty Recorder replay: does the omen appear ~0.7 s before cast end? |
| C13 | **TOP P2 Optimized Bladedance cone width** | BossMod guesses 90° (`AOEShapeCone(100, 45.Degrees())`). Retail (2026-11-17, pull 44): a tank 31° off the clone-to-centre line missed the party standing in the middle, so the cone is at most ~62° wide | Native Limitless Synergy uses a 60° cone, UNVERIFIED; the bots hold tethers 56° clockwise so either width misses the party | RaidTrace visualisation of a Bladedance, or a retail pull where someone stands 31–45° off its axis |

The same audit found no retail log for DSR P3–P7, so Hot Wing/Tail running instant (sheet 6.5 s)
and every other P3–P7 override are unchecked.

Resolved 2026-11-22 from every FRU capture with a Crystallize Time (ACT logs 2026-10-24 to 2026-11-21: 66 CT
starts inside territory 1238, 60 reaching Akh Morn, 51 reaching Morn Afah; 32 dragon tracks from the 11-11 to
11-21 RaidTraces, server-timed). Times are scenario time, where the Usurper's Crystallize Time cast starts at 1.0 s:

- **Crystallize Time timeline.** Debuffs (11.8), hourglasses, Edge of Oblivion, tethers and Speed (15.1–15.2),
  Quicken/Slow (20.8), the three Maelstroms (21.5/27.0/32.0), Dark Water (23.9), Eruption/Blizzard/Aero
  (25.9), Unholy Darkness (28.8), both Tidal Light waves (29.3/35.7), Quietus (42.9), Return (44.8), Spirit
  Taker (45.1), Hallowed Wings (47.5), the stun (51.8–60.8), both Wings knockbacks (53.7/58.3), Akh Morn's
  cast (64.0) and its four hits (69.0 + 1.05 s steps; retail 68.9/69.9/71.0/72.1) already matched the retail
  medians within about 0.15 s. Retail's 9D37/9D6E resolution at 68.0 s targets the bosses themselves and
  deals no damage.
- **Dragons.** RaidTrace `actor_move`: they start moving at 20.1 s (20.06–20.14), from 0.1526 rad off north,
  on a 12.55 y radius, at 0.2275 rad/s, reaching south at 33.2 s. The scenario used 20.2 s and 0.237 rad/s,
  which put the heads about 1 y ahead of retail by the end of the run. Now 20.1 s and 0.2275 rad/s; the
  radius stays 12.6 y. (ACT's own position polls lag by about 0.45 s and would suggest 20.56 s.)
- **Morn Afah.** Retail starts the casts at 72.2 s and the stack hits at 79.0 s; the scenario had 72.9 s and
  79.8 s. Moved to match.

Resolved 2026-11-16:

- **C11, DSR visual cast plus carrier.** Retail sends one effect per cast. Owner, 2026-11-17: fix it.
  Heavensblaze, Pure of Heart (P1 and the P4 rescue), P5 Faith Unmoving and Steep in Rage are now a
  single cast that carries its own effects; Spear of the Fury no longer sends a second effect.
  Who is hit is now decided at bar end, 0.25–0.3 s earlier than the old carrier; damage, deaths and
  the Faith Unmoving knockback still show at the effect. The helper-cast carriers (Empty/Full
  Dimension, P1 Faith Unmoving, Cauterize, Akh Morn, Hot Wing/Tail) are unchanged. Not checked in game.
- **C12, DSR Spear of the Fury timing.** Retail (3 intermission pulls): the cast starts 0.54 s before
  Pure of Heart and hits at Pure of Heart + 9.44 s. Owner, 2026-11-17: fix it. P1 now starts the cast
  at 112.30 instead of at the hit time. The P4 rescue starts it at +19.46 and lands the impact at
  +29.44 (was +27.5). Haurchefant's appearance and the bot tank LB moved with it (+28.55, +28.84),
  keeping P1's 0.89 s lead, and the first Spear follow-up tick at +29.0 was dropped. UNVERIFIED for
  the rescue itself: no retail capture of that sequence exists; its times are carried over from P1.

- **C10, FRU Fall of Faith cast length.** Retail: bar 8.70 s, hit 8.94–9.02 s after cast start (472
  casts, 27 logs). The scenario counted its bar down from 9.0 s, so the effect landed at reveal +
  9.3 s. Owner, 2026-11-17: fix it. The bar now counts down from the sheet's cast time less the
  0.3 s effect offset (8.7 s). The tethers, Fetters, charges and impacts run on their own timeline
  and did not move. Not checked in game.

- **C9, `CastSecondsOverride = 0`.** Retail sends no cast for sheet-instant actions and shows the
  effect at the hit, while an overridden bar of 0 still sent the ActionEffect 0.3 s later. Owner,
  2026-11-16: fix it. `EnemyActionHandler` now treats a 0 override as an instant, so every such site
  (DSR, DMU) sends its effect at once. This includes the sites that suppress a real cast bar or
  carry the effects of a separate visual cast (C11); their effect also moved 0.3 s earlier.
  Not checked in game.

- **C4, ACT timestamps.** They are the packet header time, not receive time: in the DMU capture they
  equal RaidTrace `server_time` to the millisecond across about 4,700 records. They are still not the
  server's snapshot time, so positions and headings in `21/22` rows can be stale. `workflow.md` §3.2
  and §3.3 are reworded.

Resolved 2026-11-14 from the 2026-11-13 DMU capture (ACT log and RaidTrace `20261113-150916-duty1094`):

- **C5, Revolting Ruin III cone. Conflicts with Yan's 「90度扇範囲」.** With positions from the trace's
  server-timed `actor_move` and the facing in the hit's own `action` record, a player standing still 58°
  off the axis at 4.2 y was hit, and players standing still 78–79° off it (5–8 y) were missed in three
  pulls. A dancer walking in was hit 36–54° off it. Treating players as points bounds the cone at
  116–156°; with a 0.5 y hitbox, 102–146°. 90° is ruled out either way. RaidTrace's drawing of the
  hit measures about 130°. The code uses BossMod's 120°, inside both bounds. ACT's client-side
  positions lag behind for players who are moving (ACT put the same dancer at 61°), so use the trace
  for edge cases like this.
- **C7, simulator captures.** Owner: ignore every log, trace, replay or recording made at the inn
  or in The Roost; they are FluffySim/AnoMech runs, not retail.

Resolved 2026-11-14:

- **C3, `tools/rtrace.py` row alignment.** For kinds with an `actor`/`caster` key the key column is
  in arrival order and the other columns are stably sorted by key. The decoder now realigns them;
  its output equals the ACT-validated scratch remap on every record of the 2026-11-13 DMU trace. The
  DMU and FRU RaidTrace analyses already used that remap, so no conclusion changes.

Resolved by the owner on 2026-11-14 from RaidTrace's visualisation of a retail hit:

- **C1, Wings Dark and Light.** The drawn cleave measures 225° (edges 9.3° and 35.8° above the
  horizontal through Pandora, ±1°), as Tuufless says. Shape as the owner describes it: the full half
  on the wing's side plus 45° past the main-threat side. `WingsCleaveLight/Dark` now use a 112.5°
  half-angle rotated ±67.5° off the facing. The axis is not re-checked against traces; in the
  2026-11-10/13 traces the cleave's caster is not where Pandora stands, so its facing cannot be
  read off directly.

Resolved from the 2026-11-11 DMU RaidTrace (2026-11-12, scratch analysis in the evidence snapshot's
`analysis/`, decoded with the C3 remap):

- **C6, DMU knockback distances.** The hit packets name Knockback rows 72 and 240, so the sheet's 13 y
  and 14 y are what the server applies. Standing trap targets travel 13.9–14.05 y. Pulse Wave targets
  show 10–14 y only because they were still walking when it hit. The push starts about 0.8 s after
  the hit packet and ends about 1.5 s after it. The Pulse Wave caster stands at (100, 56).
- **GI3 Tele-portent teleports.** A paddle warps its rider with ActorControl 241 to exactly 6 y from
  the paddle's centre in its direction (31 warps: 5.97–6.03 y), wherever the rider entered. Each warp
  applies Bind (2518) for about 1 s. A landing inside another paddle chains to it as the Bind ends,
  every 0.98–1.04 s. A confused rider whose chain ends stands still about 2.6 s after the last warp
  (2.45–3.00 s, the 4 rides that ended mid-confusion). The warp comes at least 0.3 s after the rider
  is first seen inside; position updates are too sparse to pin the exact delay. `DmuGravenImage3Scenario`
  now uses 6 y, a 1.6 s hold after the Bind and a 0.3 s entry delay (the last still UNVERIFIED).

Resolved from the 2026-11-11/12/13 FRU captures (2026-11-13, `docs/research/fru-retail-audit-2026-11-13.md`):

- **FRU phase music.** Codex chose 800/230/802/801 by track name. Retail's director sends the FRU
  arrangements 20134/20135/20105/20137 (plus 20136 for the intermission and 1 for silence before P4/P5).
- **FRU P3 weather and stage.** Time-stop (weather 106, stage slot 40 `0x00200010`) lasts only from the
  Ultimate Relativity cast to 61.25 s after it; Apocalypse runs in weather 105 with the normal stage.
- **Darklit water head marker.** A Codex test asserted none; retail shows head marker 62 on both water
  targets just before the Darklit cast.
- **C8, upstream FRU zone-in snapshot.** Every retail snapshot has all 54 slots hidden; the platform
  (slot 23) appears with P2. Owner, 2026-11-14: trust our evidence over upstream. The upstream zone now
  starts all hidden and its P2 phase shows the platform, as the custom zone does.

Resolved by the owner at handover: FRU Ultimate Relativity has nothing at true south. Everything is
relative to the hourglasses and their tethers, so the guide macro's "South" for Bind 3 is wrong and
the tether-relative code is correct. DSR P7 environment 45 vs 47 was already fixed (`5912cef5`).
DSR Lash/Gnash uses BossMod's 8 y, not AsuMech's 6 y (not yet checked against a log).

## 2. Known inaccuracies and unverified behaviour

Headless tests prove the practice model, not what the client draws. Almost every native
presentation detail added since October still awaits an in-game check. The highest-value items:

- **FRU**
  - Light Rampant:
    - Tangled topology is unsupported.
    - Chain/Bleed cutoffs and the House cone edge are unverified.
    - Hammer puddle grace (+0.83 s appear, +1.5 s hazard) is based on the owner's observation.
    - Missed-tower damage-down is not simulated.
  - Ultimate Relativity: ice donut inner radius is provisional (3); the Return ground trace is unresolved.
  - Apocalypse: lights/trails are a local overlay; jump, landing offset and knockback speed are unverified.
  - Darklit: built from a single partial recording; tether bounds, Somber vulnerability and
    Fragment memory poses are unresolved. Native cones are validated.
  - Diamond Dust: Thin Ice sliding for the human and the earliest puddle damage are inferred.
  - Mirror: colours and orb tells are unverified.
  - Crystallize Time (timeline checked against retail on 2026-11-22, see §1):
    - The Wings mitigation model approximates the Mana plan.
    - Wind/Return/Tidal visual onset is unverified.
    - Rewind: ACT's position polls see players settle at their Return spots at about 53.6 s (scenario time),
      which fits the scenario's carry from 52.8 s; ACT cannot time the carry's start more closely.
    - Retail shows the Usurper's two Return II timers ending at 51.8 s (the one applied at 28.8 s) and 56.8 s
      (the one applied at 34.8 s); the scenario has the same end times on the opposite applications.
      Display only.
  - P5: no full threat swaps, mitigation budgets, exaline resistance or DPS race.
  - Native particles keep animating during Pause (logical time is frozen).
- **DSR**
  - Strength P2: secondary Bash cone geometry is unknown (visual only); no verified Lightning head-marker ID.
  - Sanctity P1: jump falloff is a practice model.
  - Meteors:
    - Retail hazard cutoff, activation frames and comet capture time are inferred.
    - The fire visual lead (5.5 s) is provisional.
    - Tower pillar height `(3,6,3)` is uncalibrated.
  - P3: dive animation unverified; a tiny stationary flash before the slam remains (see §3).
  - P1: Empty/Full Dimension omen suppression is supported only by AsuMech.
  - P4: Eye environment, Mirage and rescue presentation unverified.
  - P5: human ST Provoke from the physical hotbar is unconfirmed; the 20% minimum mitigation is a practice abstraction.
  - P6: puddle bleed timing is provisional.
  - P7:
    - No retail P7 events in the local logs.
    - Gigaflare reveal spacing is inferred from action durations.
    - Exaflare cadence, the 18 y Gigaflare and Trinity thresholds are inherited.
    - Rotation lock and Exaflare presentation need a check.
- **TEA**
  - Hand of Pain 35% threshold is unverified.
  - P2 Nisi contact model (~1 y), Stasis House Arrest threshold (5 y) and P3 Flarethrower width (90°, inherited) are unconfirmed.
  - P4 Optical target distribution (one support / one DPS) is inferred.
  - P4 late jail cadence is uncertain.
  - The fixed barrier budget and level-80 defensive upgrades are practice choices.
  - Prison order is a fixed practice order, **not established as universal Mana**.
  - Deliberate guide-over-capture choices: marker 1 at cardinal north, inner B for the third water.
  - Native warp, jump, omen and impact timing need client observation. Pending playtest items:
    P1 buster facing, Limit Cut outward facing, P3 baits, P4 Motion visibility.
  - Reproducibility: `tools/tea_evidence.py` does not regenerate `docs/tea/evidence-summary.json`;
    part of it came from scratch scripts (recovered, see `tools.md` §3).
- **DMU**
  - P2 Forsaken (native, Mana/Yan): checked on 2026-11-21 against the 2026-11-20 session (ACT
    `Network_30305_20261120.log`, ten Forsaken pulls; RaidTraces `20261120-120759` and `-144808`).
    Upstream's tower, lockon, End and All Things Ending times, the weather (79 → 89 → 79), the BGM
    (20292), the P2 arena slot and the shapes all match retail within about 0.1 s. Fixed from the logs:
    clones land 4 y short of the player they jump on (they used to stay at the centre); each All Things
    Ending aims at one party member's spot at cast start (random per caster, not the End target and not
    always the human); the hidden shape statuses 0x13DC/0x13DD/0x13DE follow each player's lockon
    (everyone used to get 0x13DC); two auto-attacks moved. With clones landing off-centre, two Yan bot
    spots near the centre were inside the shifted cleave; they moved about 1.5 y deeper. **Check against
    Kitten's figures** that the moved helper spot (−1.0, −3.6) and the second stack soaker (−5.0, −3.6)
    still read as the strat.
    - Kefka's autos target the off-tank (upstream's choice); in retail they hit whoever holds aggro.
    - The lineup and the final-tower self-marks still follow Kitten and the owner as before.
  - P2 Trine (native, Yan Flash = Elemental since 2026-11-19): built 2026-11-21 from three retail Trines
    (pulls at 22:27, 22:42 and 23:56 JST on 2026-11-20). Observed: the seven telegraph positions never
    change (six outer triangles alternating direction plus a centre one), waves of 3/1/3 triangles burst
    15.87/17.88/19.88 s after Trine's cast starts, the centre triangle bursts in wave 3 and points either
    way, Wings of Destruction (left, Kefka facing north) resolves at 10.11 s, the closest and farthest
    players take r7 busters at 20.37 s, Ultimate Embrace follows at 22.55 s. Bots follow BossMod Reborn's
    and the owner's rule: party to the first wave-1 triangle clockwise from north, tanks to the first one
    counter-clockwise from north-west, off-tank at the edge. Main tank (owner, 2026-11-25): out from Kefka
    at right angles to a side of the centre triangle, the first such direction counter-clockwise from
    north-west (SW when it points west, W when it points east), at the closest spot within 45° clear of
    the third wave and the busters, never clockwise of north-west; the bot waits beside Kefka until the
    second wave bursts.
    - UNVERIFIED: when the third wave covers that whole side (centre pointing west with S and W in wave
      3), the main tank takes the next direction counter-clockwise; the owner's rule names only the
      first direction, and no retail pull has shown this pattern.
    - UNVERIFIED: the wave distribution (any split is rolled; three pulls showed three different ones),
      the right-wing cleave (only the left was seen), Ultimate Embrace's damage split, and the arena
      map-effect states before Trine (slot 0x13 turns off 6.5 s before Trine in retail; not reproduced).
    - Native presentation needs an in-game look: the telegraph EObjs (2015154/2015155) and their beats,
      Kefka's wing animation state (ActorControl 0x3E 0/2/3), burst VFX.
- **TOP (native Elemental, P1–P4)**: built 2026-11-18 from a friend's Elemental lockouts on 2026-11-17
  (ACT log, two RaidTraces, two replays in `~/Stažené/top`); evidence, simplifications and the in-game
  checklist are in `docs/encounters/top/elemental-p1-p3.md`. Owner's first in-game pass 2026-11-18:
  Party Synergy, Limitless Synergy and Hello World looked right; bot movement fixes followed. Pantokrator
  showed telegraphs only on the first run (helper lists were never cleared between runs; also fixed in
  Limitless Synergy and the P3 transition) and needs another pass. Zone-in flashes every phase's arena for a
  second or two, as FRU does.
  - Program Loop: a bot only intercepts a Blaster tether when its number is due; the human always can.
  - Hello World: a Nisi passes only to a player without one, bots accept it only in their strat's
    receiving window, and touching is 1.2y. One retail sample per loop shape; tether break distance 10y
    is upstream's P5 value.
  - Oversampled Wave Cannon: five retail samples since the 2026-11-20 log; spots follow its four pulls.
  - P4 Blue Screen (2026-11-21, three retail pulls from 2026-11-20): ends with Blue Screen's cast, the DPS
    check is not simulated. UNVERIFIED: the delayed Wave Cannon lines are lethal; the line-stack marker is
    retail's helper action 22393 and its look in the sim is unchecked; MapEffect slot 20's look; P3's BGM
    continuing.
  - UNVERIFIED: Flame Thrower and Ballistic Impact damage, the Sagittarius Arrow aiming at the closest
    player at cast start, Bladedance width (C13), and every native presentation detail.
- **Zone-in arena flicker (FRU, native TOP), open since 2026-11-18.** Every phase's arena shows and flashes
  for a second or two after zone-in. The cause is not confirmed.
  - Measured on TOP (two stays, Dalamud log of 2026-11-18): layout ready at +0.7 s, run start at +2.7 s, and
    11 arena layers still fully active at +3.7 to +4.3 s (layer `0x2304` 97/97). They settle by +5.5 to +6.0 s
    (19/97). The dumps are too sparse to show what triggers the change.
  - Ruled out: the `CanApplyMapEffects` territory gate (FRU has it and flickers; TOP's snapshot already waits for
    `IsTerritoryReady`) and FRU's layer holds (native TOP has none and flickers).
  - Retail (both TOP replays of 2026-11-17) sends one zone-in snapshot, header 5, 1.7 s into the recording, and
    its first reset, header 1, only after a wipe. On a cold start the simulator sent two more snapshots, from run
    preparation and run start, inside the flicker window. Local first runs now skip both when the client already
    rests in the same states.
    Host and peer runs keep it, because a peer's zone can finish loading after the zone-in snapshot.
  - Owner's in-game pass with that change (2026-11-18): FRU P1 under 0.5 s; FRU P2 several arenas for over 1 s;
    FRU P3 to P5 several arenas under 0.5 s, then a P1-like arena for about 1 s, then the right one; TOP P1
    unchanged at 1 to 2 s. The trace shows a single snapshot on every first run, so the resends were not the main cause.
  - FRU: the phase's layer holds were registered at zone-in, but only the world tick applied them, and the world does
    not tick during the roughly 2 s of run preparation. P1 hid this because its layers are the zone's default. Holds are
    now applied during preparation. UNVERIFIED in game.
    Owner's in-game pass: every FRU phase now clears in under 0.1 s, which the owner accepts.
  - TOP: the zone has no holds. In the traced stays nine layers drop at snapshot +4.6 s (`0x2304` 97 to 19) and two at
    +3.6 s, at different frame counts, so the change follows a timer. No scenario event is due then (Program Loop's
    first cast is at 10.4 s). The trigger is unknown. The temporary layer trace, removed before 3.4.5.0, could not see the first 2 s after the
    snapshot, because it ran from the world tick.
  - TOP now hides the 128 instances that are off once P1 has settled, from the first layout frame until 6 s after the
    layout is ready, then hands them back to the client. The list comes from one settled P1 arena snapshot.
    Owner's in-game pass (2026-11-18): the TOP P1 flash is much shorter but not gone, and not down to FRU's 0.1 s. The
    owner accepts this for now. UNVERIFIED: whether P2 and P3 entries settle to the same set.
- **Cross-cutting**
  - Spectator camera POV and HUD/DelvUI are unverified.
  - Offline party consumers (native HUD names, Dalamud party list, DelvUI) are separate checkpoints and were never all confirmed.
  - Discord Chat Bridge is suspended through Dalamud internals by reflection (fragile).
  - Changing debug sim speed while a latency-delayed action is pending changes its wall-clock delay.

## 3. Do not retry

| Approach | Why it failed |
|---|---|
| Capturing FRU arena init through IINACT's OverlayPlugin WebSocket during replay playback | No MapEffect, Director or ImportedLogLines arrive. Use `.dat` parsing plus snapshot diffs |
| Running `tools/parser.py` (or any pipe-split) on `Replaye/*.log` | They are BossMod `BLCB` binary; the parser silently returns nothing |
| Holding DSR layers every frame (offline) | Competes with native environment selection and causes flicker. One verified weather-manager transition per layout generation works |
| A weather-slot write without the full weather-manager transition | Slot reaches 45 while the rendered weather stays 2 |
| Blanket "every actor has a draw object" readiness guards | Meshless helpers never get one, so the scenario aborts (Diamond Dust shelved on 10-10 for this) |
| Readiness checks on CharacterBase's transient loading flag | Blocked every DSR P3 dive; use draw readiness |
| DSR P3 dive: extending clone lifetime or preloading timelines | Model suppression left `DrawObject=null`. Load hidden and dispatch the real ActionEffect instead |
| DSR P3 dive: "keep hidden until the native timeline reveals it" (`msszqs`) | Worse in game; reverted. A reveal delay also risks hiding part of the dive |
| Tower visuals via cast/omen-only helpers (DSR Meteors) | No persistent visual. The native Conviction AVFX from Omen 90 is now used; the projected-ring overlay was removed |
| Pause through the old input lock | It invoked a real auto-attack cancel action. Pause now freezes sim time and lets native animation run |
| Faking a lobby login or patching other plugins for offline mode | Rejected. Bridge is suspended and restored instead |
| Wholesale conflict resolution (`jj restore --from` over integrations, broad `jj abandon`, loops rewriting many revisions) | Discards concurrent work; Codex's reviewer denied it 7 times. Resolve hunk by hunk |
| FFLogs as a raw-log source | No raw ACT log via the API; archived reports locked |
| Sparse pose interpolation for TEA 1211 Limit Cut routes | Unreliable; a procedural clockwise pack with measured lanes is used |
| Splatoon "Full Toolers" script as an Exaline timing source | Its action IDs drive Polarizing Strikes, not Exalines |
| Copying bot movement-command times into wall-clock cue schedules | Mixes AI wait time with encounter timing and drifts with replay speed |

## 4. Repository and environment state at handover

- `v3.3.0.0` points at a commit that failed packaging. By design it stays unreleased forever;
  `v3.3.1.0` superseded it.
- `docs/NEXT-UPSTREAM-AUDIT.md` is a local, git-ignored file; its public version is
  `docs/integration/POST-3.0.2-UPSTREAM-AUDIT.md`.
- `~/Projekty/PunishXIV/Splatoon` holds the owner's local Fulgent Blade script change (jj
  `unkuny`, script v9; not upstreamed). FluffySim does not depend on it.
- The earliest FluffySim work (2026-10-09 to 10-12) was done inside `~/Projekty/anomek/AnoMech`,
  which is now treated as a read-only reference; that checkout is clean.
- Codex built Release locally on 2026-10-09/10 before the Debug-only rule existed (2026-10-11).
- The FRU arena-diagnostic scenarios were removed on 2026-11-13 and `Dsr/DsrArenaDiagnosticScenario.cs`
  on 2026-11-16.
- RaidTrace captures are readable with `tools/rtrace.py` (`tools.md`), but **only tracer 0.3.1+
  captures are trustworthy**. The owner reports older captures fail on raidtrace.com. Local checks agree:
  tracer 0.2.6 captures hold no actions; most 0.3.0 captures hold none either; and 0.3.0 has
  `server_time` = 0. The one FRU capture (`20261110-141323-duty1006`, 0.3.0) was checked against the same
  pull's ACT log. Individual field values match (cleave facings 3.14/1.02 vs ACT 3.12/1.02), but
  timing and grouping are scrambled (four cleaves over two minutes in ACT become two pairs 2.4 s
  apart). C1 was settled on 2026-11-14 from a tracer 0.3.1 capture (section 1).
- `docs/encounters/dsr-sanctity-meteors-events.json` lacks the Eternal Conviction (25568) rows the
  current analyzer adds. **Owner decision (2026-11-12): leave it.** The Meteors implementation is
  considered essentially accurate and needs no further evidence work.
- Evidence attrition: logs and replays age out (`workflow.md` §3.1). On 2026-11-12 every then-existing
  ACT log, the FRU replays in `replay/deleted/` and the ultimate replays in `autorenamed/` were copied
  to `~/Dokumenty/Final Fantasy XIV/FluffySim-evidence/{logs,replays}/`. Older sources were already gone.
