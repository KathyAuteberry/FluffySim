# Dancing Mad (Ultimate) P1: evidence extraction

Phase 1 report for the native FluffySim P1 scenarios (Graven Image 1, 2, 3). It contains no strategy
and no design. Every value is labelled with its basis:

- **Observed**: measured in the in-game captures below.
- **Sheet**: read from the game data (pinned datamining CSVs: Action, Omen, Knockback).
- **Inferred**: deduced from observations. The rule and the number of consistent cases are given.
- **Tier-2**: from a strategy guide, listed in §8 for a future capture.

Upstream AnoMech `Scenarios/Umad/` and BossMod were used only to name IDs. Nothing here takes a
value from them.

## 0. Owner decisions (2026-11-12 review)

- **GI1 ends after Mystery Magic 2 resolves (53.5 s)**, not at the trap pop (49.5 s).
- **Final Mystery Magic spreads** (unobserved) reuse Mystery Magic 1's behaviour: same mechanic,
  same IDs, same truth/lie rule. Marked UNVERIFIED for MM3.
- **Thin variants** (final-MM lies, Tele-portent permutations) are extrapolated from the observed
  rules, shipped for practice and corrected when more captures arrive.
- **Revolting Ruin III cone: 90°** (owner, agreeing with Yan 「90度扇範囲」; superseded on 2026-11-14 by
  the retail trace: 120°, see `docs/handover/open-issues.md`). Settles C-DMU-4
  provisionally. Still UNVERIFIED by a measurement.
- Knockbacks use the sheet values (13 / 14 / 50 y).
- Safety is non-negotiable: shipping half-finished mechanics is fine; weakening the firewall or the
  packet filter is not.

## 1. Evidence base

All inputs are snapshotted, with a `SHA256SUMS` manifest, in
`~/Dokumenty/Final Fantasy XIV/FluffySim-evidence/dmu-p1-2026-11-12/`. The analysis scripts and
their (unsanitised) outputs are in `analysis/` beside them. They are not in the repository because
they contain player actor IDs.

| File | What it is | Used |
|---|---|---|
| `20261111-112002-duty1094.rtrace` | RaidTrace, tracer 0.3.1, game 2026.11.03, territory 1363, CFC 1094, 6087 s | Yes, after the decoder fix in §2.1 |
| `Network_30305_20261111.log` | IINACT log. 33 DMU pulls, 20:31–22:00 JST, same session as the trace | Yes, primary timeline source |
| `Dancing Mad (Ultimate) 2026.11.11 22.01.29.dat` | Duty Recorder, build 14251429, recorded 11:20:02 UTC, 6087 s | Yes, cross-check |
| `DancingMadUltimate_DRK100_Ka_Au_2026_11_11_20_20_02.log` | BossMod Reborn replay (BLCB, VER 31), same session | Count cross-check only |
| `Network_30304_20261104.log` | IINACT log. Its 2 "DMU" pulls are in **The Roost** with bot party names | **Excluded: simulator runs, not retail** (§9 C-DMU-2) |
| `DMU-Roost_DRK100_Ka_Au_2026_11_04_17_45_15.log` | BMR replay of the same Roost session | **Excluded**, same reason |

All retail evidence is therefore **one session**: the static's 33 progression pulls on 2026-11-11.
How far the pulls got:

- 32 pulls reach Graven Image 1.
- 25 reach Graven Image 2 (the second Graven Image cast).
- 7 reach the Gravitas resolution.
- 6 reach Tele-trouncing.
- 4 reach the enrage Light of Judgment (BABB).
- None reaches P2.

Party: MT = DRK (the owner), ST = PLD, H1 = WHM, H2 = SGE, D1 = SAM, D2 = RPR, D3 = BRD, D4 = RDM.
These labels are this document's aliases. They are not a claim about the static's own assignments.
The MT pulls, and after the opening swap the ST holds aggro. Times are seconds from the pull's
InCombat flag (ACT `260`). Coordinates are world x/y with the arena centre at (100, 100); +y is
south. Heading h follows the game's convention: facing vector (sin h, cos h), so 0 = south,
±π = north, +π/2 = east and −π/2 = west.

## 2. RaidTrace validation against the ACT log

### 2.1 Decoder defect found: `tools/rtrace.py` misaligns keyed kinds

As committed, `rtrace.py` decodes every byte (the "exact byte consumption" claim in
`docs/handover/tools.md` holds). But for every kind with an `actor` or `caster` key column, it
pairs that column with the wrong rows:

- The key column (and the block's per-row clock) is stored in **arrival order**.
- Every other column of the section is stored **stably sorted by key**.
- The decoder zips them positionally, so actors and local clocks are attached to the wrong records.

Unfixed, only 5/780 casts match the ACT log, and tethers report the wrong statue.

Remapped (value row *j* belongs to arrival row `argsort_stable(keys)[j]`, scratch implementation
`analysis/rtfix.py`), every comparison matches:

| Kind | ACT | RaidTrace | Matched (actor, ID, ±1 s) | ACT − `server_time` |
|---|---|---|---|---|
| NPC cast start (ACT `20`) | 780 | 780 | 780 | 0 ms (p5–p95: 0 ms) |
| NPC action, unique packets (ACT `21/22`) | 2954 | 2954 | 2954 | 0 ms |
| Headmarker (`27`) | 421 | 421 | 421 | 0 ms |
| Tether (`35`) | 495 | 495 | 495 | 0 ms |
| MapEffect (`257`) | 36 | 36 | 36 | 0 ms |
| EObj state (`273` category 0x19D) | 547 | 547 | 547 | 0 ms |

- Per-pull NPC cast counts are identical in all 33 pulls.
- The trace has no `dropped`, `incomplete` or `pause` records.
- After the fix, the trace's local receive clock agrees with `server_time` within ±27 ms (p5–p95).

**Consequence:** in this IINACT build, ACT line timestamps *are* the packet header timestamps
(RaidTrace `server_time`), to the millisecond. They are not local receive times, but they are still
not the server's internal snapshot time of a mechanic. `workflow.md` §3.2 says otherwise (§9 C-DMU-3).

The fix is not applied. This phase allows no code changes. Fixing `rtrace.py` (and re-checking
anything already concluded from a keyed RaidTrace kind) is listed as follow-up work.

### 2.2 Duty Recorder and BossMod replay

- **Duty Recorder:** ActorCast is opcode 352 in build 14251429 (32 bytes; action ID at +0, cast
  time f32 at +8, byte 3 = 0 for every P1 boss cast). The replay holds 2935 casts, as does the
  trace. 2905 align uniquely at a single offset (replay ms 0 = 11:20:02.265 UTC); residuals are
  −0.056 to +0.059 s (p1–p99).
- **BossMod replay** (brotli body, not decoded further). Record counts match the trace:
  - `ICON` 421 = headmarkers;
  - `ENVC` 36 = MapEffects;
  - `CST!` 27 575 = trace `action` records;
  - `CST+` 2 923 against 2 935 casts.

The three in-game sources describe the same session consistently. Everything below comes from the
ACT log, with positions and EObj data from the remapped trace.

## 3. Actors, statues and coordinates

### 3.1 Battle NPCs (Observed)

| Actor | BNpcBase / Name | Spawn | Role |
|---|---|---|---|
| Kefka | 19504 / 7131 | (100, 90). At the first Revolting Ruin cast, (99.99, 99.60), h = π (facing north) | Boss |
| Kefka helpers ×~32 | 9020 / 7131 | (100, 90) | Invisible casters: cones, lines, towers, puddles, Tele-trouncing drops |
| Graven Image ×9 | 19505 / 7132 | (100, 100) | Invisible statue casters. Their ability rows report the source position of the statue they act for |

Kefka teleports with the unnamed ability **C3FD** (no cast) to exactly (99.99, 99.99):

- at 24.52 ± 0.15, before GI1;
- at 75.74 ± 0.2, before GI2;
- at 179.86, inside GI3, before the final Mystery Magic.

He faces north (h ≈ ±π) for every cast except Graven Image 3 and its follow-up C554 (h ≈ −1.0 in
the recorded pulls).

### 3.2 Statue EObjs (Observed, trace `object_spawn`)

The same 13 EObjs spawn at every pull reset, with fixed layout IDs. The z column is height. Each
role is identified by which EObj changes state when the mechanic fires (§3.3).

| Base | Layout | Position (x, y, z) | Role |
|---|---|---|---|
| 2015156 | 0xBC7C91 | (100, 100, 0) | Arena centre object. State 1 at the first cast (10.0), 0x10 at LoJ 1 (59.5), 0x40 at LoJ 2 (127.3) |
| 2015163 | 0xBC178D | (100, 56, 18.5) | GI1 knockback statue (Pulse Wave) |
| 2015163 | 0xBC178E | (100, 65, 0) | GI1 Wave Cannon statue |
| 2015164 | 0xBC178F | (102.5, 27, 22.5) | GI2 "middle" statue: Gravitas tethers |
| 2015165 | 0xBC1790 | (126, 41.5, 7) | GI2 "east" statue: Vitrophyre tethers |
| 2015165 | 0xBC1792 | (116, 43, 6.5) | Half-room cleave indicator, **east** (Intemperate Will) |
| 2015164 | 0xBC1791 | (92, 27, 15) | Half-room cleave indicator, **west** (Gravitational Wave) |
| 2015167 | 0xBC1795 | (95, 25, 27) | GI3 Indulgent Will statue (confusion) |
| 2015166 | 0xBC1793 | (107, 43, 8.5) | GI3 Idyllic Will statue (sleep) |
| 2015167 | 0xBC1796 | (95, 25, 12.5) | GI3 gaze statue for **Ave Maria** |
| 2015166 | 0xBC1794 | (105.25, 34, 13.5) | GI3 gaze statue for **Indolent Will** |
| 2000182 | 0xBC174E | (100.41, 115.75, 0) | Not a P1 mechanic (likely entrance/exit). Unchanged in P1 |
| 2000139 | 0xBC174D | (100, 85, 0) | Same (spawn flags 4 instead of 5) |

Two kinds of mechanic EObjs spawn during the fight:

- **2015266 Gravitas puddle**, at each Gravitas stack position:
  - set 1 at 88.2 (south, y ≈ 108.8–109.7);
  - set 2 at 107.0 (north, y ≈ 89.9–90.9).
- **2015267 Tele-portent arrow**: it spawns where the player stands when their portent expires, and
  its facing is the arrow direction (0 = S, π = N, ±π/2 = E/W).

### 3.3 EObj state sequence (ACT `273` category 0x19D = trace `object_state`)

Every statue uses the same (state, timelines) pairs:

1. (1, 2) when it tethers or activates;
2. (0x10, 0x20) when it resolves a tether mechanic;
3. (0x40, 0x80) to telegraph;
4. (0x100, 0x200) when it fires.

Gravitas puddles use (0x10, 0x20) when they arm and (4, 8) when they are soaked or expire.

Observed sequence in a full pull, with medians:

| t | EObj | State | Paired event |
|---|---|---|---|
| 29.86 | knockback statue | 1 | GI1 tethers |
| 34.83 | knockback statue | 0x10 | Pulse Wave 34.91 |
| 37.16 | Wave Cannon statue | 0x40 | (Mystery Magic resolves 37.25) |
| 42.18 | Wave Cannon statue | 0x100 | Wave Cannon 42.27 |
| 80.92 | middle + east statues | 1 | GI2 set-1 tethers |
| 96.08 | cleave indicator (E or W) | 0x40 | (RR III hit 1 at 97.25) |
| 101.14 | same | 0x100 | half-room cleave 1 at 101.25 |
| 109.22 | cleave indicator (E or W) | 0x40 | |
| 113.81 | middle + east statues | 0x10 | |
| 114.25 | same indicator | 0x100 | half-room cleave 2 at 114.2–114.35 |
| 117.83 | 8 Gravitas puddles | 0x10 | |
| 120.8 | puddles | 4 | Gravity III |
| 164.41 | GI3 confusion + sleep statues | 1 | GI3 tethers |
| 173.37 | same | 0x10 | Indulgent / Idyllic Will 173.45 |
| 176.66 | gaze statue (Ave Maria or Indolent) | 0x40 | |
| 186.48 | same | 0x100 | gaze 186.58 |

The cleave indicator that animates is the east one (0xBC1792) for Intemperate Will and the west
one (0xBC1791) for Gravitational Wave. This held in every observed cleave (E and W, sets 1 and 2).

### 3.4 Map effects (Observed)

Director 0x800375D2, slot 0, two values:

- flags 0x00080004 at 59.49 (Light of Judgment 1 cast + 1.9 s), in all 29 pulls that reached it;
- flags 0x00200010 at 127.26 (Light of Judgment 2 cast start), in all 7 pulls.

Both coincide with the centre object's state change. These are the only MapEffects in P1.

## 4. P1 timeline (Observed)

Medians and full min–max ranges across the pulls that reached each event. Cast times are the
server cast bars (ACT shows 4.7 s for the sheet's 5.0 s and 2.7 s for 3.0 s, as for every
encounter). Spreads are about ±0.3 s, so the timeline is fixed relative to InCombat.

### 4.1 Opening and Graven Image 1

| t (median) | Range | Event | IDs |
|---|---|---|---|
| 10.14 | 10.09–10.22 | **Revolting Ruin III** cast (5.0 s) on the enmity-1 tank. Tank marker **00DA** on that tank, same tick | C403 (cone, sheet range 100, no angle) |
| 15.21 | 15.14–15.27 | RR III hit 1: on the **marked tank** | C403 |
| 18.34 | 18.26–18.49 | RR III hit 2 (no cast): on **enmity #2** at that moment | C4E1 |
| 24.52 | | Kefka teleports to the centre | C3FD |
| 26.12 | 25.86–26.25 | **Graven Image** cast (3.0 s) | BCF2 |
| 29.86 | 29.61–30.00 | 4 tethers 002D from 4 Graven Image casters, all on **one role group** | — |
| 32.18 | 31.94–32.33 | Boss icons, one fire + one ice (§5.1). Player markers: 007F on all 8 *or* 0080 on 1 support + 1 DPS | 02A1–02A4, 007F/0080 |
| 32.27 | 32.03–32.42 | **Mystery Magic** cast (5.0 s) plus helper Blizzard III Blowout casts at the centre (truth: 2 omened real; lie: 2 omened fake + 2 un-omened real) | BA94; BA98 / BA9B + BA9E |
| 34.91 | 34.67–35.06 | **Pulse Wave** on the 4 tethered players: knockback from the statue at (100, 56) | BAA9 |
| 37.25 | 36.99–37.39 | Blizzard III Blowout resolves (two opposite quadrant cones) | as above |
| ~38.05 | 37.80–38.15 | **Flagrant Fire III**: spreads (r5) on all 8, or 2 stacks (r6) | BAA2 / BAA3 |
| 41.42 | 41.16–41.58 | **Double-trouble Trap** cast (3.0 s) | BAA6 |
| 42.27 | 42.05–42.44 | **Wave Cannon** from the statue at (100, 65): 4 lines (width 6) on 2 supports + 2 DPS | BAA8 |
| 42.91 | 42.68–43.07 | 4 **Explosion** towers (r4, 3.0 s) at the 4 beam-hit spots | BAAA |
| 44.41 | 44.16–44.58 | Double-trouble Trap debuff (5 s) on 1 support + 1 DPS | status 0x13D6 |
| 45.89 | 45.68–46.07 | Towers resolve. Unsoaked tower → Unmitigated Explosion 47.07 (seen in 9 pulls) | BAAA / BAAB |
| 48.41 | 48.18–48.57 | Boss icons, one ice + one thunder (§5.1) | 02A3/02A4 + 02A5/02A6 |
| 48.49 | 48.27–48.66 | **Mystery Magic 2** (5.0 s): Blizzard III Blowout + Thrumming Thunder III | BA94; BA98/BA9B/BA9E; BA9F/BAA0/BAA1 |
| 49.51 | 49.26–49.68 | **Trap expires**: holder-centred stack r6 + knockback | BAA7 |
| 50.09 | 49.84–50.26 | Trap debuff passed: **68 s** on 1 support + 1 DPS (random among those hit) | 0x13D6 |
| 53.46 | 53.24–53.63 | Mystery Magic 2 resolves (cones + lines) | |
| 57.56 / 62.54 | | Light of Judgment cast / raidwide (to be skipped) | C622 |
| 65.74, 67.84, 69.93 | | Hyperdrive ×3 on the enmity-1 tank, no cast (to be skipped) | C24B |

### 4.2 Graven Image 2

| t (median) | Range | Event | IDs |
|---|---|---|---|
| 75.74 | | Kefka teleports to the centre | C3FD |
| 77.18 | 76.94–77.35 | **Graven Image** (3.0 s) | BCF2 |
| 80.92 | 80.65–81.10 | Set-1 tethers: **all 8**. One role group to the middle statue (Gravitas), the other to the east statue (Vitrophyre) | 002D |
| 82.21 | 81.95–82.40 | Boss ice icon only | 02A3/02A4 |
| 82.30 | 82.04–82.49 | Boss **Blizzard III Blowout** (5.0 s) plus helper cones at the centre | BA95; BA98 / BA9B + BA9E |
| 87.28 | 87.00–87.46 | Cones resolve | |
| 87.42 | 87.18–87.62 | **Gravitas** ×4 (r5 stacks) from the middle statue's casters, targeted on the middle-tethered players. Puddle EObjs spawn at those spots at +0.8 s (south cluster) | BAAC |
| 91.42 | 91.20–91.61 | **Vitrophyre** ×4 (r5) on the east-tethered players. Its knockback on bystanders is Knockback row 86 (50 y) | BAB0 |
| 92.17 | 91.88–92.38 | **Revolting Ruin III** (5.0 s), marker 00DA on the current enmity-1 tank | C403 |
| 96.08 | | Cleave indicator telegraph (0x40) | |
| 97.25 | 96.94–97.44 | RR III hit 1 on the marked tank | C403 |
| 97.37 | 97.14–97.53 | Set-2 tethers, all 8, re-rolled | 002D |
| 100.38 | 100.07–100.57 | RR III hit 2 on enmity #2 | C4E1 |
| 101.25 | 100.92–101.33 | **Half-room cleave 1** from the centre: Intemperate Will h = +π/2 (east half) or Gravitational Wave h = −π/2 (west half) | BAB2 / BAB1 |
| 105.86 | 105.64–106.03 | Set-2 Gravitas. Puddles spawn at +1.1 s (north cluster) | BAAC |
| 109.89 | 109.68–110.05 | Set-2 Vitrophyre | BAB0 |
| 114.19–114.35 | 113.97–114.49 | **Half-room cleave 2** | BAB2 / BAB1 |
| 118.18 | 117.99–118.24 | **Trap expires** (68 s debuff): stack + knockback | BAA7 |
| 118.75 | 118.58–118.86 | Trap passed: **49 s** on 1 support + 1 DPS | 0x13D6 |
| 120.82 | 120.64–120.95 | **Gravity III** puddles detonate. One hit per cluster, one per puddle actor, on the players standing in it | BAAF |
| 127.35 / 132.33 | | Light of Judgment (to be skipped) | C622 |
| 135.49, 137.60, 139.70 | | Hyperdrive ×3 (to be skipped) | C24B |

### 4.3 Graven Image 3

| t (median) | Range | Event | IDs |
|---|---|---|---|
| 146.59 | 146.38–146.77 | **Tele-trouncing** cast (5.0 s) | BAB9 |
| 152.34 | 152.11–152.50 | Two Tele-portent statuses per player, one 7 s and one 10 s (§5.3) | 0x130C–0x130F, 0x13D7–0x13DA |
| 159.41 | 159.18–159.56 | 7 s portents expire. A helper hit (r2) under each player, then an arrow EObj at that spot | BABA, EObj 2015267 |
| 160.66 | 160.43–160.81 | **Graven Image** (3.0 s) | BCF2 |
| 162.39 | 162.18–162.58 | 10 s portents expire | BABA |
| 164.41 | 164.19–164.54 | Tethers, all 8: one group to the confusion statue, the other to the sleep statue | 002D |
| 165.78 | 165.58–165.96 | Unnamed 3.0 s cast (resolves 168.78) | C554 |
| 167.81 | 167.64–167.91 | **Trap expires** (49 s): stack + knockback. No further trap debuff follows | BAA7 |
| 173.45 | 173.28–173.60 | **Indulgent Will** (Confused 6 s) on one group, **Idyllic Will** (r5, Sleep 6 s) on the other | BAB5 / BAB6 |
| 177.77 | | Unnamed ability, no cast | C555 |
| 179.86 | | Kefka teleports to the centre | C3FD |
| 181.31 | 181.02–181.50 | Boss icons, one fire + one thunder. Player markers 007F on all, or 0080 on 1 support + 1 DPS | 02A1/02A2 + 02A5/02A6 |
| 181.41 | 181.10–181.59 | **Mystery Magic 3** (5.0 s) + Thrumming Thunder III lines | BA94; BA9F / BAA0 + BAA1 |
| 186.40 | 186.07–186.57 | Lines resolve | |
| 186.58 | 186.34–186.75 | **Gaze**: Ave Maria (look at; range 100) or Indolent Will (look away) | BAB3 / BAB4 |
| 187.18 | 186.88–187.37 | **Flagrant Fire III** | BAA2 / BAA3 |
| 197.67 | 197.43–197.84 | Kefka untargetable (197.7), then the **enrage** Light of Judgment cast (BABB) because HP was above 14.9 % | BABB |

## 5. Mechanic details

### 5.1 Truth/lie and element icons (Inferred, 160 checks, 0 violations)

| Boss icon | Meaning | Casts that appear |
|---|---|---|
| 02A2 | Fire, **truth** | Flagrant Fire matches the player markers |
| 02A1 | Fire, **lie** | Flagrant Fire is the opposite of the player markers |
| 02A4 | Ice, **truth** | 2× BA98 (real, with omen) |
| 02A3 | Ice, **lie** | 2× BA9B (omen, no effect) on the shown quadrants + 2× BA9E (real, **no omen**) on the other two |
| 02A6 | Thunder, **truth** | 2× BA9F (real, with omen) |
| 02A5 | Thunder, **lie** | 2× BAA0 (omen, no effect) + 2× BAA1 (real, **no omen**) on the other lines of the same orientation |

- Player marker **007F** on everyone = "spread" shown. **0080** on one support and one DPS =
  "stack" shown.
- The result is the shown mechanic on a fire truth and the opposite on a fire lie. This held in all
  37 fire resolutions.
- Ice held in 87 checks and thunder in 36.
- The fake casts BA9B/BAA0 list targets but carry only the animation effect: no damage, no status
  (303 and 84 target rows, 0 damage entries).
- The Lockon sheet is not in the datamining cache, so the icons' VFX names are unconfirmed.
  Kitten says the lie shows as question-mark orbs around Kefka.

**Geometry** (Sheet; omens m0462fan / m0462lay):

- Blizzard III Blowout:
  - cone, range 40; the sheet has no angle, so 90° is inferred from "quadrants";
  - two cones always opposite each other, at headings {−45°, 135°} or {−135°, 45°}, both centred
    on (100, 100).
- Thrumming Thunder III: rect 40 × 10, cast from fixed origins.
  - Heading +45° (lines running SE):
    - set A = (96.45, 75.24) + (82.29, 89.37);
    - set B = (89.37, 82.29) + (75.24, 96.45).
  - Heading −45° (lines running SW):
    - set C = (103.53, 75.24) + (117.66, 89.37);
    - set D = (110.58, 82.29) + (124.74, 96.45).
  - Each orientation's four lines sit at perpendicular offsets of ±5 and ±15 from the centre.
    A/B (or C/D) are complementary, so a lie swaps to the other set of the same orientation.
- Flagrant Fire III: spread r5 (BAA2) or stack r6 (BAA3), from two helpers.
- Magic Vulnerability Up (0xB7D) is applied for 0.96 s by each hit; 3.96 s by Wave Cannon.

### 5.2 Graven Image 1 specifics (Observed)

- **Tether group:** the 4 Graven Image tethers go to all supports or all DPS. They come from four
  Graven Image actors that cast Pulse Wave from (100, 56).
- **Pulse Wave knockback:** Knockback row 72: distance 13, speed 25, direction mode 0.
  Effect type 31 (param 72) on every target. The movement samples (10–16 y including the player's
  own running) are consistent with this, but the distance is from the sheet, not measured.
- **Wave Cannon:**
  - targets are 2 supports + 2 DPS, random;
  - each beam is a 100 × 6 rect from (100, 65) toward its target, so hits on other players are
    possible;
  - each target's position 0.6 s later becomes a tower (BAAA, r4, 3.0 s).
- **Double-trouble Trap:**
  - Debuff 0x13D6 on 1 support + 1 DPS, for 5 s (first), 68 s (after the GI1 pop) and 49 s (after
    the GI2 pop).
  - On expiry: BAA7 stack r6 + knockback (Knockback row 240: distance 14, speed 20, Direction field
    6 — that enum's meaning is UNVERIFIED).
  - The pass goes to one support and one DPS among those hit.
  - Pops at 49.5, 118.2 and 167.8. The third pop gives no new debuff.

### 5.3 Graven Image 3 Tele-portent (Observed, 6 pulls)

Direction IDs, from BossMod's names, checked against the arrow EObj facings in P32:

| Direction | Set 1 | Set 2 |
|---|---|---|
| N | 0x130C | 0x13D7 |
| S | 0x130D | 0x13D8 |
| E | 0x130E | 0x13D9 |
| W | 0x130F | 0x13DA |

- One role group gets **identical arrows** (two statuses with the same 0x130x ID, one 7 s and one
  10 s). Its four players hold N, S, E and W.
- The other group gets **two different arrows**:
  - one 0x130x and one 0x13Dx;
  - the 0x13Dx direction is **always the 0x130x direction rotated 90° clockwise** (6/6 pulls,
    24/24 players);
  - first directions are again N/S/E/W;
  - **which of the two is the 7 s timer is per player**: both orders occur within a single pull.
- The arrow EObj spawns at the player's position at expiry, facing the arrow direction.
- **Teleports (measured 2026-11-12 from the remapped trace):** an arrow warps its rider with
  ActorControl 241 (destination packed in params 0/1 as quantised x and z) to 6.00 y from the arrow
  EObj's centre in its direction (31 warps in 6 pulls: 5.97–6.03 y, sideways ≤ 0.03 y). Each warp
  applies Bind (status 2518) for about 1 s; a landing inside another arrow (about 2 y) chains to it as
  the Bind ends, every 0.98–1.04 s. Confused riders' warps: 174.79 (n=9), 175.79, 176.89, 178.00
  (full rides). After the last warp of a ride ending mid-confusion the rider stands still 2.45–3.00 s.
  The warp follows the first sample inside the arrow by at least 0.3 s.

## 6. Provoke windows (Observed)

### 6.1 Revolting Ruin III #1 (cast 10.14, hit 1 at 15.21, hit 2 at 18.34)

- In the opening the MT holds aggro and gets the 00DA marker.
- The ST provoked during the cast bar in 30 of 33 pulls:
  - provoke at 10.75–13.81, i.e. **+0.6 to +3.7 s after cast start**;
  - the MT Shirked at 14.09–14.60.
- In all 30, both hits landed on the MT:
  - hit 1 follows the marker;
  - hit 2 goes to enmity #2, which the MT became after the ST's provoke.
- The ST then held aggro (auto-attacks and Hyperdrive 1–3 on the ST).
- Without an ST provoke (P1, P6, P23), hit 2 went to the ST's cone and hit other players.
- In P6 and P23, the 00DA marker and hit 1 were on the ST or on both tanks. The early MT provoke
  timing differed in those pulls.

### 6.2 Revolting Ruin III #2 in GI2 (cast 92.17, hit 1 at 97.25, hit 2 at 100.38)

- The marker goes to the current enmity-1 tank, the "ST" player.
- The MT provoked at 94.49–97.28 (+2.3 to +5.1 s) in 14 pulls. Both hits landed on the marked tank
  every time.
- **P9 provoked 0.18 s after hit 1** (97.28 vs 97.10), and hit 2 still went to the marked tank. So
  hit 2 is decided at its own resolution, not at cast start.
- In P24 the MT provoked early (87.16, before the cast). The marker went to the MT, and hit 2 then
  hit 7 players.
- The MT then held aggro (Hyperdrive 4–6 on the MT).

### 6.3 Strict deadline (not established)

The evidence shows that a provoke anywhere in the cast bar works. It also shows that hit 2's target
is enmity #2 when hit 2 resolves. It does not show the latest working moment for RR #1. Grading a
"provoke during the cast bar" window (cast start → hit 1) is consistent with every pull. The owner
asked for that window.

## 7. Randomized variants: observed vs not observed

Cells give the pull numbers in which each variant was seen.

### 7.1 Graven Image 1 (31 executions)

| Variable | Values observed |
|---|---|
| GI1 tether group | Supports: 2,4,5,7,8,11,12,13,15,18,19,20,21,25,26,27,28,32,33. DPS: 6,9,10,14,16,17,22,24,29,30,31 |
| MM1 ice | Truth (02A4): 3,7,10,13,16,20,24,28,29,30,31,32. Lie (02A3): the rest |
| MM1 cone diagonal | {−45°,135°} and {−135°,45°}: both, under both truth and lie |
| MM1 fire × markers | Truth + spread markers: 1,2,5,6,9,12,14,16,29. Truth + stack markers: 8,10,11,15,17,18,21,26,28,30. Lie + spread markers (→ stacks): 4,7,13,19,20,22,24,25,27,32,33. Lie + stack markers (→ spreads): **31 only** |
| MM1 ice × fire | All four combinations |
| Stack-marker pairs | (D2,H2), (D4,H2), (D2,H1), (D1,H2), (D1,H1), (D3,H1): always 1 support + 1 DPS |
| Wave Cannon targets | Always 2 supports + 2 DPS, varying members |
| Trap holders | Always 1 support + 1 DPS. Every role was a holder at least once |
| MM2 ice × thunder | Lie/lie: 2,24,30,31. Lie/truth: 5,7,11,12,13,16,17,18,20,32,33. Truth/lie: 4,8,15,19,21,25,26,29. Truth/truth: 6,9,10,14,22,27,28 |
| MM2 line sets | A, B, C and D all observed. Lies swap within an orientation |

### 7.2 Graven Image 2 (25 executions, 7 to the resolution)

| Variable | Values observed |
|---|---|
| GI2 Blizzard | Truth: 7,8,14,19,20,22,25–30,32,33. Lie: 6,9,11,13,15,16,17,18,21,24,31. Both diagonals under both |
| Set-1 spread group (east statue) | Supports: 9,14,15,17,18,19,20,21,29,33. DPS: 6,7,8,11,13,16,22,24,25,26,27,28,30,31,32 |
| Spread group set 1 → set 2 | SUP→SUP: 18,29. DPS→DPS: 22,32. SUP→DPS: 17,20,33. DPS→SUP: 26,27,30. **Not forced to alternate**, as the owner said |
| Cleave 1 → cleave 2 | E→W: 9,18,22,24,30. E→E: 17,20,27,32,33. W→W: 26. W→E: 29. Single cleave seen: E (16,19), W (21,25) |
| GI2 never casts thunder or fire | 25/25 (boss BA95 + ice icon only) |

### 7.3 Graven Image 3 (6 executions)

| Variable | Values observed |
|---|---|
| Identical-arrow group | DPS: 26,29,32,33. Supports: 27,30 |
| Arrow direction assignment | Several permutations. Different-arrow rule constant (90° cw) |
| 7 s/10 s order in the different-arrow group | Per player, both orders |
| Confused group | Supports: 26,27,30,32,33. DPS: 29 |
| Gaze | Ave Maria: 26,27,29,30,32. Indolent Will: 33 |
| Final thunder | Truth: 26 (B), 27 (C), 30 (B), 32 (D). Lie: 29 (shown A / real B), 33 (shown B / real A) |
| Final fire × markers | Lie + spread markers (→ stacks): 26,27,29,32,33. Truth + stack markers (→ stacks): 30 |

### 7.4 Not observed (must come from a tier-2 source or a future capture)

1. **Final Mystery Magic resulting in spreads**: fire truth + spread markers, or fire lie + stack
   markers. In MM1 these combinations exist (§7.1), so they are expected here too.
2. Final-MM line sets are thin: A, B, C and D all appear, but each in only 1–3 pulls, and only
   two final-MM lies were seen (29: shown A, real B; 33: shown B, real A). No final-MM lie on the
   −45° orientation.
3. Tele-portent: only 6 samples. Every N/S/E/W permutation and timer pattern is plausible but
   unverified. Teleport distance is measured (§5.3).
4. GI2 and GI3 with a failed or late provoke for RR #1, and the latest working provoke moment
   (§6.3).
5. Knockback distances measured directly (sheet values used: Pulse Wave 13 y, Trap 14 y,
   Vitrophyre bystander 50 y).
6. Cone angles: Revolting Ruin III (no sheet angle; §9 C-DMU-4), Blizzard III Blowout (90°
   inferred) and the half-room cleaves (180° inferred).
7. Lockon VFX names for 007F, 0080, 00DA and 02A1–02A6.
8. What C554 / C555 do and what the boss's facing during GI3 means (h ≈ −1.0 in P32).
9. P2 transition: never reached (needs Kefka ≤ 14.9 % before 197.7). The scenarios end before it,
   so this does not block them.

## 8. Items for the owner to capture next time

With RaidTrace 0.3.1+, IINACT and the Duty Recorder all running:

- More GI3 pulls, especially final Mystery Magic spread outcomes (§7.4 items 1–2) and
  Tele-portent variety.
- One pull where a GI2 cleave hits someone standing about 85° off its facing, and one where an RR III
  hit clips a player at a known angle: settles the cone angles.
- A paused Duty Recorder view (and `/fluffysim snapshot`) during each statue telegraph, to confirm
  which model animates and the arena's resting MapEffect state for a mid-fight start.

## 9. Conflicts and corrections for `docs/handover/open-issues.md`

Not yet written to `open-issues.md`. Phase 1 changes nothing tracked. These entries go in with the
first DMU work commit:

| # | Topic | Sources | What would settle it |
|---|---|---|---|
| C-DMU-1 | `tools/rtrace.py` attaches the key column and clocks to the wrong rows for keyed kinds | `tools.md` §5 ("validated by exact byte consumption"). This report §2.1: 5/780 casts match unfixed, 780/780 remapped | Fix the decoder (value rows are key-sorted) and add a test against an ACT-aligned fixture. Recheck earlier RaidTrace conclusions (e.g. tracer-version completeness claims) |
| C-DMU-2 | `Network_30304_20261104.log` and `DMU-Roost_…` are listed as in-game evidence | The log shows zone The Roost and bot names: simulator runs | Owner confirms. Treat as non-evidence |
| C-DMU-3 | "All ACT times are receive times" | `workflow.md` §3.2. Here ACT times equal RaidTrace `server_time` (packet header time) to the ms in 4,700+ records | Note it in `workflow.md`: header time, not receive time, and not the server's snapshot |
| C-DMU-4 | Revolting Ruin III cone width | Yan: 「90度扇範囲」. Kitten: "wide cone". BossMod: 120°. Sheet: no angle | A capture with a known clipped angle (§8) |
| C-DMU-5 | Revolting Ruin III hit-1 target | Yan: 「1回目はヘイト1位」 (enmity 1). Logs: hit 1 follows the cast marker (enmity 1 *at cast start*), even after the ST's provoke | Not a contradiction in practice. Recorded so the sim does not re-target hit 1 at resolution |
