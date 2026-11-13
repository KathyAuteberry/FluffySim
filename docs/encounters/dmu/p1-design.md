# Dancing Mad (Ultimate) P1: scenario design

Phase 3 deliverable. It covers:

- how the three native scenarios start mid-fight;
- how skipped sections are bridged;
- how provoke timing is detected and graded;
- what can and cannot reuse upstream Umad code.

Facts come from [p1-evidence.md](p1-evidence.md); positions and rules from
[p1-strategy.md](p1-strategy.md). Pull time = seconds from InCombat in the 2026-11-11 pulls.

## 1. Identity and placement

| Item | Proposal |
|---|---|
| Folder / namespace | New native family `AnoMech/Scenarios/Dmu/` (`AnoMech.Scenarios.Dmu`). It contains `DmuConstants.cs` (IDs, every one checked against the evidence), `DmuActions.cs` (`EnemyAction` definitions, shapes from the Action sheet) and one folder per scenario: `GravenImage1/`, `GravenImage2/`, `GravenImage3/`. Each scenario folder has `*Scenario`, `*Ai` (Yan/Mana), `*State`, `*StateOverrides` and `*SettingsWindow` |
| Zone / phase | Reuse **`UmadZone`** and its `P1` phase, so the menu keeps a single "Dancing Mad" entry. Its territory (1363), weather (77 between pulls, 78 on pull, 4 s transition) and reset directors (`0x80000028` 4, `0x8000000D` 1) match the trace. Its `DutyInitData` is used as is, to be confirmed in game |
| BGM | P1 is not one track. Retail switches with director `0x80000001`: **20289** at the Revolting Ruin cast (10.09), **20290** at LoJ 1 (59.6) and **20291** at LoJ 2 (127.4). `UmadZone.P1`'s 20291 is right only for GI3. GI1 and GI2 switch their own track in `RunInstanceEvents` (`SwitchBgm`), as TEA I2 does |
| Menu names | "Graven Image 1", "Graven Image 2", "Graven Image 3" |
| Upstream `UmadP1TeleTrouncingScenario` | Keep the source. **Hide it from the menu** like the duplicate upstream Diamond Dust (owner decision D1) |
| Strategy | One `IScenarioAi` per scenario: Mana / Yan Flash |
| Docs | The evidence, strategy and design docs are gitignored under `/docs/encounters/*`. The first work commit needs an allowlist line, `!/docs/encounters/dmu/`, so they can be committed |

## 2. Timelines, start states and bridging

Every scenario keeps absolute `world.Events.Add(t, …)` literals, where **scenario time = pull time −
offset**. A fixed offset keeps every evidence timestamp a direct subtraction.

### 2.1 Graven Image 1: offset 0 (the pull), ends at 55.0

- **Start state:**
  - Fresh pull. Kefka at (100, 99.6), facing north.
  - The MT holds enmity and stands north at about (100, 93.5); the rest of the party is behind,
    south.
  - The 13 statue EObjs spawn at rest, in their pull state (evidence §3.2).
  - Weather 78. Auto-attacks every 3.03 s from 2.96.
- **Events:**
  - Revolting Ruin III at 10.14.
  - Graven Image at 26.12; Mystery Magic 1 at 32.27; Wave Cannon at 42.27.
  - Trap cast at 41.42, debuff at 44.41, pop at 49.51.
  - Mystery Magic 2 at 48.49, resolving at 53.46.
- **End:** about 1.5 s after MM2 resolves (owner decision), before Light of Judgment 1 (57.56).
  Nothing to bridge.

### 2.2 Graven Image 2: offset 73.0, runs 0–51 (pull 73.0–124.0)

**Skipped:** Light of Judgment 1 (57.56–62.54) and Hyperdrive ×3 (65.7–69.9).

Start state, i.e. the state the skipped section leaves behind:

| Element | Value at scenario 0 | Evidence |
|---|---|---|
| Kefka | Centre, facing north. His C3FD teleport plays at t = 2.74 (pull 75.74) | §3.1 |
| Enmity | **ST #1, MT #2** (the result of the GI1 swap) | §6.1 |
| Party | Full HP. ST north of Kefka at about (100, 93); the others at their post-Hyperdrive spots south of centre | Positions are an inference; the scenario only needs everyone near the south stack spot by 87 |
| Double-trouble Trap | Status 0x13D6 on one rolled support and one rolled DPS. **45.18 s** left, so it pops at t = 45.18 (pull 118.18) | §5.2 |
| Statues | GI1 knockback statue at its last beat (0x10). Wave Cannon statue at 0x100. Centre object at 0x10 | §3.3 |
| Arena | Replay MapEffect (0x00080004, slot 0), as at pull 59.49 | §3.4 |
| BGM | Track 20290, 13.4 s in (73.0 − 59.6) | §1 |

**End:** after Gravity III (pull 120.82) and the puddle cleanup beat (123.85). Light of Judgment 2
is skipped.

### 2.3 Graven Image 3: offset 145.0, runs 0–44 (pull 145.0–189.0)

**Skipped:** Light of Judgment 2 (127.35–132.33) and Hyperdrive ×3 (135.5–139.7).

Upstream's Tele-trouncing scenario used the same anchor: its cast at t = 1.59. Every upstream beat I
checked (Graven Image, trap pop, Wills, Mystery Magic, fire) sits within 0.1 s of this evidence. That
confirms the offset, but only the evidence is the source.

| Element | Value at scenario 0 | Evidence |
|---|---|---|
| Kefka | Centre, facing north | |
| Enmity | **MT #1, ST #2** (the result of the GI2 swap) | §6.2 |
| Double-trouble Trap | 0x13D6 on one rolled support and one rolled DPS. **22.81 s** left (pops at pull 167.81) | §5.2 |
| Statues | GI2 middle and east statues at 0x10. One cleave indicator at 0x100 (the rolled "last cleave" side; cosmetic). Centre object at 0x40. No Gravitas puddles | §3.3 |
| Arena | Replay MapEffect (0x00080004, 0), then (0x00200010, 0) | §3.4 |
| BGM | 20291, 17.6 s in (145.0 − 127.4); `UmadZone.P1`'s track | §1 |

**End:** about 2 s after Flagrant Fire III (pull 187.18). The enrage Light of Judgment (197.67) is
skipped. Optionally (D5), punish a failed P1 the way retail's "fake clear" path would; upstream ends
with its own LoJ punishment.

### 2.4 What is not carried between scenarios

Each scenario rolls its own state. Nothing from a previous scenario carries over:

- Damage Down, deaths and boss HP are not carried (P1 has no HP phase push within our segments).
- The trap holders are re-rolled. Retail picks them randomly at every pass, so a fresh roll is
  faithful.

## 3. Provoke detection and grading (Core)

Both Revolting Ruin swaps need the same logic, so it goes in **Core**, per AGENTS ("logic that more
than one scenario needs belongs in Core"):

- **Proposed:** a new class `Core/Game/MarkedTankbusterSwap.cs`.
- **Not reused:** the existing `TankSwapSequence`. It is shaped around DSR's Heavenly Heel (a
  vulnerability check and three cleaves), and changing it risks DSR.
- **Reused:** the provoke pipeline itself:
  - `LocalProvokeHandler` validates the human's Provoke (logs `[ProvokeInput]`);
  - `world.ActionResolved` reports Provoke 7533 from humans and bots;
  - Shirk is action 7537.

**Enmity model:** an ordered pair (enmity #1, enmity #2) of the two tanks per boss.

- A Provoke on the boss by tank X makes X #1 and pushes the old #1 to #2.
- A Shirk from X onto the other tank lowers X below them.
- Nothing else changes the order; DPS and healers never reach the tank band in P1.

**Revolting Ruin III (C403 → C4E1), as observed:**

| Step | Time from cast start | Behaviour |
|---|---|---|
| Cast start | 0 | Hit-1 target locked to enmity #1 *now*. Tank marker **00DA** on them |
| Hit 1 (C403) | +5.07 | 120° cone from Kefka toward the **locked** target, wherever enmity now stands |
| Hit 2 (C4E1, no cast) | +8.20 | 120° cone toward **enmity #2 at this moment** |

**Grading** (log tag `[DmuRevoltingRuin]` plus an on-screen `Announce`):

| Outcome | Condition | Consequence |
|---|---|---|
| **On time** | The other tank's Provoke lands between cast start and hit 1 | Both hits on the marked tank. Pass |
| **Late** | Provoke between hit 1 and hit 2 | Still works: retail P9 provoked 0.18 s after hit 1. Pass, with the announcement "late provoke: works in game, but practise it during the cast bar" |
| **Early** | Provoke before the cast starts | The marker goes to the provoker (retail P24), so the swap is reversed. Graded as a failure of the drill |
| **Missing** | No Provoke before hit 2 | Hit 2 goes to the other tank. **Fail** (consequence: owner decision D4) |
| Shirk | MT's Shirk in GI1 | Logged only. In retail it comes at +4.0–4.5 s, but the ordering above does not depend on it |

**Who acts:**

- **Human tank:** the scenario waits for real input.
- **Bot tank:** provokes at the observed median (GI1: ST at +1.7 s; GI2: MT at +3.5 s). The GI1 bot
  MT Shirks at +4.0 s.
- **Hard bot difficulty** may add the observed spread: GI1 +0.6…+3.7 s, GI2 +2.3…+5.1 s.

**Tests** (headless): on-time, late, early, missing, and human vs bot on each side.

## 4. Mechanic building blocks

| Mechanic | Implementation | Source of truth |
|---|---|---|
| Statues | 13 EObjs spawned from the evidence table (base, layout, position, height, `Arg2` = the `object_spawn` arg such as 0x420003, state 4). Beats via `SimEventObject.PlayBeat(state, timelines)` (ActorControl 413 = 0x19D, as logged) | Evidence §3.2–3.3 |
| Hidden Graven Image casters | BNpc 19505, placed at their statue and tethering with 0x2D. They cast Pulse Wave, Wave Cannon, Gravitas, Vitrophyre, Wills and gazes | §3.1 |
| Boss icons / player markers | `HeadMarker` 02A1–02A6 on Kefka. 007F on all or 0080 on 1 support + 1 DPS. 00DA on the RR target | §5.1 |
| Mystery Magic | Helper casts BA98/BA9B/BA9E (cone r40, 90° from the centre, at the two diagonals) and BA9F/BAA0/BAA1 (rect 40×10 from the four line sets). Lie = omened fake + un-omened real on the complementary set. Flagrant Fire BAA2 r5 / BAA3 r6 resolved by the truth table | §5.1 |
| Pulse Wave | `Knockback` row 72 from (100, 56) on the tethered group | §5.2 |
| Wave Cannon + towers | Rect 100×6 from (100, 65) toward 2 rolled supports + 2 rolled DPS. Towers BAAA r4, 3 s, at the targets' spots 0.6 s later. Unsoaked → Unmitigated Explosion (wipe) | §5.2 |
| Double-trouble Trap | Status 0x13D6. At expiry: BAA7 stack r6 + Knockback row 240 from the holder. Fewer than 3 soakers kill (Kitten; to be confirmed). Passes to 1 support + 1 DPS among those hit (68 s, then 49 s) | §5.2 |
| GI2 | Tethers from the middle and east statues. Gravitas 2-man r5 stacks → puddle EObj 2015266 with a hazard r5 (stepping in or a Vitrophyre hit = wipe). Vitrophyre r5 + Knockback row 86 (50 y) on bystanders. Cleaves BAB1/BAB2 180° from the centre at h = ∓π/2, with the indicator beats. Gravity III soak | §4.2, §3.3 |
| Tele-portent | Statuses by direction (§5.3), 7 s / 10 s. Drop arrow EObj 2015267 with the facing. Stepping in (r2, Tele-trouncing range) teleports along the arrow. Distance 6 y from the arrow's centre, then Bind 1 s; a landing inside another arrow chains to it (measured, evidence §5.3) | §5.3; owner |
| Confusion / sleep | Core `ActorControl.SetConfused` + `SimParty` nearest-player chase (already in Core, built for upstream UMAD). Sleep 0x131E, 6 s, no movement | §4.3 |
| Gazes | Ave Maria BAB3 (look at) / Indolent Will BAB4 (look away) via Core's `Gaze` effect, from the two gaze statues. Failure → Damage Down 0xB5F, 180 s | §4.3 |

**Shared across the three scenarios:** Mystery Magic resolution, the statue set, the trap pass and
Kefka/helper spawning.

- AGENTS discourages "a family's helper class".
- The repository already has family-level mechanic files (`Tea/TeaDotPuddles.cs`,
  `Fru/FruHolyPuddles.cs`).
- Proposal (D2): `Dmu/DmuMysteryMagic.cs`, `Dmu/DmuStatues.cs` and `Dmu/DmuDoubleTroubleTrap.cs` as
  family mechanic files. Only `MarkedTankbusterSwap` goes to Core.

## 5. Randomness and settings

`*State` rolls everything from `world.Rng`. `*StateOverrides` can force each roll. Defaults are
uniform over everything the evidence allows, including variants seen only in tier-2 sources:

- **GI1:**
  - tether group;
  - MM1 ice truth/lie and diagonal;
  - fire truth/lie and marker type, with the stack-marker pair;
  - Wave Cannon targets;
  - trap holders;
  - MM2 ice/thunder truth/lie, diagonal and line set.
- **GI2:**
  - Blizzard truth/lie and diagonal;
  - set 1 and set 2 spread group, independent of each other;
  - cleave 1 and cleave 2 sides;
  - trap holders.
- **GI3:**
  - identical-arrow group;
  - directions, with the 90° clockwise rule;
  - per-player 7 s / 10 s order;
  - confused group;
  - gaze type;
  - fire/thunder truth/lie, markers and line set.

  Final-MM spreads use MM1's rule (owner decision), marked UNVERIFIED for MM3.

## 6. Upstream Umad: what can and cannot be reused

| Item | Status |
|---|---|
| Core features built for upstream UMAD: `ActorControl.SetConfused` + `SimParty` chase, `SimEventObject.PlayBeat`/`EObjSetState`, `SpawnEventObject`, sheet knockbacks, the `Gaze` effect, `Tether`, `HeadMarker`, `MapController.AddEffect`/`DirectorUpdate`, `BattleTalk`, `SwitchBgm` | **Reused.** They are Core, already shared, with no strategy in them |
| `UmadZone` (territory, weather, reset directors, `DutyInitData`, `P1` phase) | **Reused as identity**, after checking it against the trace (§1). Its `P1` BGM fits GI3 only |
| `UmadConstants` IDs | **Reference only.** Re-declared in `DmuConstants` with evidence values. Those I checked agree: BNpc 19504/19505/9020; EObj 2015156/63–67/266/267; layouts 0xBC178D–96 and 0xBC7C91; Graven `Arg2`s; status 0x13D6. Names differ (upstream calls Kefka "KefkaP3") |
| `UmadActions` / `UmadP1TeleTrouncing*` (scenario, AI, state) | **Not reused** (owner: not extending upstream). Their timings agree with the evidence within 0.1 s and their statue spawn packets match the trace. Useful as a reading aid for how the client needs the props spawned (two gaze-statue instances, `EventId`, entity IDs) |
| Upstream arrow constants (trigger r2, collide 4 y, push 6 y, dwell 0.35 s) | Trigger r2 = sheet (BABA). The rest is **UNVERIFIED**, like ours. Take them as starting values and mark them UNVERIFIED |
| `TankSwapSequence` (DSR) | Not reused (§3). Could later be rebuilt on `MarkedTankbusterSwap`; out of scope |

## 7. Safety

- Nothing here touches the packet filter, firewall, zone or entry guards, or server traffic.
- Everything is client-side simulation through the existing `Natives` boundary, as in every other
  scenario.
- New sim-to-client calls reuse existing implementations: EObj spawns, beats, MapEffects, BGM,
  headmarkers, tethers and confusion ActorControls.

## 8. Testing and delivery

- `ScenarioCatalogTests`: the Mana AI clears each scenario with no deaths over random seeds.
- New unit tests:
  - `MarkedTankbusterSwap` grading;
  - the truth/lie table;
  - the Tele-portent quadrant/spot assignment;
  - the trap pass (needs 3+ soakers; next holders are one support and one DPS).
- Debug build only. One work commit per scenario (GI1, then GI2, then GI3), each merged into the
  shared integration commit. Evidence and docs under `docs/encounters/dmu/`.
- In-game checks for the owner after each one: statue animations and props, arena MapEffect state at
  the GI2/GI3 start, BGM offsets, the RR cone and marker, provoke feedback, and the lie omens.

## 9. Owner decisions (2026-11-12)

| # | Decision |
|---|---|
| D1 | Upstream Dancing Mad stays under **Upstream**. A second zone entry, **Dancing Mad under "FluffySim Native"** (like Futures Rewritten's two entries), holds the three P1 scenarios and grows as more phases land. So a native `DmuZone` (territory 1363) is added; `UmadZone` and its scenarios are left untouched |
| D2 | Family mechanic files under `Dmu/`; only the swap grading goes to Core |
| D3 | Multiplayer during initial development, where it fits |
| D4 | A missing or early provoke: hit 2 kills the wrong tank and anyone in the cone, and **the scenario fails** |
| D5 | GI3 end: announce earlier failures only |
| D6 | Allowlist `docs/encounters/dmu/` in `.gitignore` |

BGM follow-up: each scenario needs its own track, so Core gains an optional per-scenario track
(`IScenario.Bgm`, falling back to the phase's).

## 10. Original proposals (for reference)

| # | Question | My proposal |
|---|---|---|
| D1 | Hide upstream "Tele-trouncing" from the menu once GI3 ships? | Yes. Keep the source; hide the entry (Diamond Dust precedent) |
| D2 | Shared DMU P1 mechanics as family files (`Dmu/DmuMysteryMagic.cs` etc.) rather than Core? | Yes (TEA/FRU precedent). Only the swap grading goes to Core |
| D3 | Multiplayer in the first release of each scenario, or solo first? | Solo + bots first. Add multiplayer (`IMultiplayerReplayable`) as a follow-up per scenario, so your static can practise together |
| D4 | Consequence of a missing provoke | Hit 2 on the wrong tank kills it if unmitigated, and kills anyone else in the cone, with an announcement. Same for an early provoke |
| D5 | GI3 end when a P1 mechanic failed earlier in the run | Announce the failure only. No fake-clear or enrage replay |
| D6 | Allowlist `docs/encounters/dmu/` in `.gitignore` in the GI1 commit | Yes |

## 11. Implementation notes

### Graven Image 2

- **Trap knockback.** In the static's GI2 pass (pulls 26–33), the knocked group moved about
  11.6–13.8 y (median ≈ 12) from about (95.5, 98) to the north puddles, against the sheet's 14 y
  (Knockback row 240). Pulse Wave showed the same shortfall: about 10–11 y against 13. Core keeps
  the sheet distance. The bots stand **13 y** from their puddle so either distance lands inside
  its 5 y radius, and they walk into the puddle afterwards as Yan says. UNVERIFIED which distance
  is right. If the in-game knockback looks long, this is the first suspect.
- **Puddle rules, all UNVERIFIED:**
  - A Vitrophyre whose 5 y circle reaches a puddle (centres within 10 y) wipes. Yan's spread spots
    keep about 11 y.
  - A puddle nobody stands in at Gravity III wipes.
  - Stepping into a puddle before Gravity III is not modelled. Everyone stands in them as they
    drop, so any such rule must have a grace period, and no source states one.
- **Stack spot.** The south stack sits 0.8 y inside the safe south quadrant. The static's puddles
  were 0.2–0.7 y off the C line.
- **Trap pass** (settled by the owner):
  - Supports west, knocked north-east into the north puddles.
  - DPS east, knocked south-west into the south puddles.
  - Each holder stands 1.5 y behind its group.

### Provoke routes (fix after the owner's first GI1/GI2 test)

A solo or host player's Provoke reaches the sim as `world.ActionResolved`: `UserActions` hands it
to `LocalProvokeHandler`, which validates it without a stance and notifies. It never calls
`SimEnemy.Provoke`, which only bots (`JobActions` → `ProvokeEffect`) and peers (host handling of
`PeerProvokedEnemyMessage`) raise. `MarkedTankbusterSwap` first heard only `Provoked`, so every
human Provoke graded as Missing. The in-game log showed `local-validation … result=eligible` and
then `graded grade=Missing`. The swap now listens to both and ignores a Provoke by the current
holder, which also drops the duplicate. The tests drive the human through `LocalProvokeHandler`
and fail without the fix. Stance is never required.

### Graven Image 3

- **Tele-portent paddles:**
  - Each paddle drops where its owner stands when the 7 s or 10 s status expires, facing its arrow.
  - It arms once nobody stands on it.
  - Anyone who steps within 2 y (Tele-trouncing's radius), awake and not bound, is carried
    **6.5 y** along the arrow (UNVERIFIED: Yan's box spacing), bound for 1 s (Bind 0x9D6, 1.00 s,
    as logged), and the paddle is spent.
  - Landing on the next paddle chains, as the Pinoy box intends.
- **Paddle rules, all UNVERIFIED:**
  - Two paddles dropped within 4 y cancel and count as a mistake.
  - A paddle left at the end counts as a mistake ("P3 would stay locked"). It is not a wipe
    (owner decision D5).
- **Confused chase:** Core's own chase follows the nearest living ally and swings (cone, 2 y) at
  anyone in reach.
  - A full four-paddle chain takes about 4 s, which still leaves time to reach a partner. Clean
    retail pulls never showed that.
  - So a rider whose chain ends (lands on no paddle) holds **2 s** past the bind (UNVERIFIED, as
    upstream did).
  - A short chain, from a misplaced paddle, still leaves the partner exposed.
- **Teleport spread** (Yan): inner spots at Yan's exact coordinates, MT (−4.2, −4.6),
  ST (3.7, −4.6), D1 (−4.4, 3.7), D2 (3.4, 3.7). Outer players edge toward their box, to
  (±8.5, ±8.5), per Kitten's "edge close to the teleporter circle".
  - Each diagonal partner is then the clearly nearest player.
  - With Yan's box-centre outer spots and any destination jitter, inner players chased each other,
    so the bots move there without jitter.
- **Gazes:**
  - A failed gaze gives Damage Down (0xB5F, 180 s) and a recorded mistake, as retail does. Core's
    gaze effect kills instead, so it is not used.
  - Ave Maria fails when facing away from its statue; Indolent Will fails when facing it.
- **Final Mystery Magic:** role anchors (Yan) nudged to the nearest point clear of the real lines,
  with a 1.5 y margin.
- **Arena at the start:** MapEffects for both Light of Judgments replayed; centre statue at its
  wind-up beat. One cleave indicator is left at its fire beat; that choice is cosmetic.
- **Multiplayer:** off for GI3 for now (`SupportsMultiplayer => false`; the replay message and
  state encoding stay in place).
  - In a multiplayer run, peers reach their spread spots by movement input and ride paddles with
    network delay.
  - The confused chase then caught partners: `ResultMarkTests` recorded deaths.
  - GI1 and GI2 support multiplayer.
- **Gaze facing:** bots turn with `TurnTo`. `SetRotation` never reaches a bot-controlled local
  player.

### Graven Image 3 fix: a confused player rode only one paddle

The owner's first GI3 test: as a confused MT, they rode the North paddle onto the West one and then
walked off it and killed D3.
- The log: carried to (−5.7, −13.6), Bind gained; at Bind's end they were at (−7.7, −10.0), and
  their swing at D3 resolved 14 ms later.
- `TeleportTo` only sets the engine-side position, so the client's in-flight forced approach toward
  D3 kept walking them. The release sends only a forced-follow end, not a forced stop.
- The chase also resumed the moment Bind lifted, before the paddle check.

Fix:
- Riders are moved with `CarryTo`, a move the client executes itself, as upstream did.
- While a confused player stands on an armed paddle, the chase is re-held every frame, so the
  paddle decides when they move on.

The headless player test passes on the old code too, because the fake client doesn't model the
drift. **Only an in-game check verifies this fix.**
