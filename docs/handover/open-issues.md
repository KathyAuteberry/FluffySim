# Open issues

Known inaccuracies, unresolved source conflicts and approaches not to retry, as of the
2026-11-11 handover. Encounter-level detail and checklists remain in `docs/encounters/*/ROADMAP.md`,
`docs/research/fru-native-playtest.md`, `docs/research/dsr-progress.md`, `docs/tea/IMPLEMENTATION.md`
and `docs/verification/PLAYTEST-CORRECTIONS-2026-11-11.md`. Add new conflicts here; never resolve
one silently.

## 1. Open source conflicts

| # | Topic | Sources | Current code | What would settle it |
|---|---|---|---|---|
| C1 | **FRU P5 Paradise Regained, Wings Dark and Light cone** | Tuufless: 225°. BossMod and BMR: half-angle 120°, offset ±60° (240°; related code, not measurements). Kitten: no angle ("half-moon"). Owner's reading of the diagrams (half of one tower, all of one, about a third of the last) fits **~232°** in the sim layout: 240° → ½/1/½, symmetric 225° → ⅓/1/⅓ | 240° (`FruActions.WingsCleaveLight/Dark`, `AreaSpec.Size = 2π/3` half-angle, rotation ±π/3) | The actions have no Omen, so the sheet can't help. The 2026-10-30/31 clears (8 hits) only bound the cone at ≲259°. A replay or log where someone stands about 55–65° off Pandora's facing at the hit would settle it. The logged heading also suggests the cone may be centred behind Pandora rather than offset from the tank, but heading at effect time ≠ snapshot. Unconfirmed |
| C2 | **ActorCast byte 3**: omen delay or base cast time | Codex read it as omen delay (tenths). BossMod `ServerIPC.cs` names it `BaseCastTime100ms` | DSR Spiral Thrust `OmenDelay = 5.0f` (byte 50 in 123 casts, cast 5.7 s); DSR P3 Geirskogul `OmenDelay = 3.5f` (byte 35, cast 4.2 s → 0.7 s warning). TEA casts show 0 and no TEA value depends on it | Watch either telegraph in a Duty Recorder replay: does the omen appear ~0.7 s before cast end? |
| C3 | **`tools/rtrace.py` row alignment** for keyed kinds (`actor`/`caster` column) | `tools.md` §5 calls the encoding validated by exact byte consumption. Against the 2026-11-11 DMU ACT log only 5/780 casts match unfixed; with value rows taken as key-sorted and the key column and clocks in arrival order, all 780 casts, 2954 actions, 421 headmarkers, 495 tethers, 36 MapEffects and 547 EObj states match (`docs/encounters/dmu/p1-evidence.md` §2.1) | Decoder unchanged; DMU work used a scratch remap | Fix the decoder, add an ACT-aligned test fixture, recheck earlier RaidTrace conclusions |
| C4 | **ACT timestamps**: receive time or not | `workflow.md` §3.2: receive times. In the DMU capture ACT times equal RaidTrace `server_time` (packet header time) to the ms across about 4,700 records | Nothing depends on it | Reword §3.2: packet header time, still not the server's snapshot |
| C5 | **DMU Revolting Ruin III cone** | Yan: 「90度扇範囲」. BossMod: 120°. Kitten: "wide cone". The sheet has no angle | 90° (owner decision), UNVERIFIED | A capture with a player clipped at a known angle |
| C7 | **DMU 11-04 evidence** | The owner listed `Network_30304_20261104.log` and `DMU-Roost_…` as in-game evidence; both are FluffySim/AnoMech runs in The Roost (bot party names) | Excluded from DMU evidence | Owner confirmation |
| C8 | **Upstream FRU zone-in snapshot** | `Upstream/FruZone.ArenaMapEffectSnapshot` has slot 23 = 1. Every retail snapshot in the 2026-11-11/12/13 traces (14 of them, zone-in and resets) has all 54 slots = 4 | Left as is: the upstream zone only hosts P2+ scenarios, where slot 23 shown is the right state; custom `FruZone` uses the retail snapshot | Decide whether to align the upstream file or leave it for upstream to fix |

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
  - Crystallize Time:
    - No complete CT sequence exists in the local captures.
    - The Wings mitigation model approximates the Mana plan.
    - Wind/Return/Tidal visual onset is unverified.
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
- The FRU arena-diagnostic scenarios were removed on 2026-11-13; `Dsr/DsrArenaDiagnosticScenario.cs`
  remains as unreferenced dead code.
- RaidTrace captures are readable with `tools/rtrace.py` (`tools.md`), but **only tracer 0.3.1+
  captures are trustworthy**. The owner reports older captures fail on raidtrace.com. Local checks agree:
  tracer 0.2.6 captures hold no actions; most 0.3.0 captures hold none either; and 0.3.0 has
  `server_time` = 0. The one FRU capture (`20261110-141323-duty1006`, 0.3.0) was checked against the same
  pull's ACT log. Individual field values match (cleave facings 3.14/1.02 vs ACT 3.12/1.02), but
  timing and grouping are scrambled (four cleaves over two minutes in ACT become two pairs 2.4 s
  apart). **Conflict C1 needs a fresh live FRU capture with tracer 0.3.1 or later.** As of 2026-11-12
  the only reliable capture is Dancing Mad (`20261111-112002-duty1094`).
- `docs/encounters/dsr-sanctity-meteors-events.json` lacks the Eternal Conviction (25568) rows the
  current analyzer adds. **Owner decision (2026-11-12): leave it.** The Meteors implementation is
  considered essentially accurate and needs no further evidence work.
- Evidence attrition: logs and replays age out (`workflow.md` §3.1). On 2026-11-12 every then-existing
  ACT log, the FRU replays in `replay/deleted/` and the ultimate replays in `autorenamed/` were copied
  to `~/Dokumenty/Final Fantasy XIV/FluffySim-evidence/{logs,replays}/`. Older sources were already gone.
