# FRU retail audit — 2026-11-13

Every custom FRU scenario (Fall of Faith, Diamond Dust, Mirror Mirror, Light Rampant, Ultimate
Relativity, Apocalypse, Darklit Dragonsong, the Pandora scenarios) and the shared `FruZone` were
compared with the November captures. Times below are relative to a named boss cast, so receive-time
offsets cancel out.

## Evidence

| Source | Content |
|---|---|
| `Logy/Network_30305_20261111.log` | 4 pulls (to P2 Light Rampant) |
| `Logy/Network_30305_20261112.log` | 3 pulls, one to P4 Crystallize Time |
| `Logy/Network_30305_20261113.log` | 5 pulls: two to P3, one to P5 enrage (wipe), one **clear** |
| `Trace/20261111-161244`, `20261112-151401`, `20261113-052813`, `20261113-115137` `-duty1006.rtrace` | RaidTrace 0.3.1 of the same sessions; the last is the clear |
| `replay/autorenamed/Futures Rewritten (Ultimate) 2026.11.13 {00.32.56,15.17.24,21.17.53}.dat` | Duty Recorder of the same sessions (not needed for this audit) |

RaidTrace records were decoded with the C3 remap from `FluffySim-evidence/dmu-p1-2026-11-12/analysis/rtfix.py`
(see `open-issues.md` C3). `actor_move` positions are packed u16: `value / 65535 * 2000 - 1000`.

## Findings that changed code

### Music (DirectorUpdate `0x80000001`, param = BGM row)

Retail plays FRU's own arrangements. Codex had picked the original tracks by name.

| Phase | Retail | Was |
|---|---|---|
| P1 | 20134 `BGM_EX5_Ban_13` (sent at every wipe reset, not at the pull) | 800 `BGM_EX3_Raid_10` |
| P2 | 20135 `BGM_EX5_Ban_14`, at the P2 platform MapEffect | 230 `BGM_Ban_Shiva1` |
| Intermission | 20136 `BGM_EX5_Ban_15`, ~1 s before Endless Ice Age | — |
| P3 | 20105 `BGM_EX5_Ban_12`, at the end of the intermission | 802 `BGM_EX3_Raid_12` |
| P4 | 1 (silence), then 20137 `BGM_EX5_Ban_16` 2 s later | 801 `BGM_EX3_Raid_11` |
| P5 | 1 at the P5 transition, 20099 `BGM_EX5_Ban_11` ~39.5 s later | 20099 |

Seconds into the track at each scenario's start (spread across captures ≤ 0.25 s):
Diamond Dust 22.94, Mirror Mirror 89.05, Light Rampant 124.82, Ultimate Relativity 32.85,
Apocalypse 115.59, Darklit 23.22, Fulgent Blade / Pandora sequence 15.52, Paradise Regained 52.84,
Polarizing Strikes 82.17, Pandora's Box 110.87. P1's track starts at the previous reset, so Fall of
Faith cannot be synced (offsets 78–247 s across pulls).

### Arena

- Retail sends one MapEffect snapshot with all 54 slots in state 4 at zone-in and at every reset
  (14 snapshots across the four traces). `FruZone` now declares that snapshot and per-phase
  overrides instead of 54 individual AddEffect calls per phase: P2 slot 23 = 1; P3/P4 slots 40, 41 = 1;
  P5 slot 47 = 1.
- **P3 time-stop.** Retail enters time-stop at the Ultimate Relativity cast (weather 106, slot 40
  `0x00200010`, 0.09 s before the cast) and leaves it 61.25 s after the cast (weather 105, slot 40
  `0x00400001`, hourglasses `0x00080004`). `FruZone.P3` had weather 106 and the time-stop floor for
  every P3 scenario, so Apocalypse ran in time-stop. P3 is now weather 105; Ultimate Relativity
  switches in and out itself.
- **Hourglass sand.** Relative to the UR cast: all eight slots `0x00200010` at +18.84; per wave
  `0x00800040` at +20.84 / +25.88 / +30.87 and `0x02000100` at +22.85 / +32.92 / +43.88 (just
  before each wave's Sinbound Meltdown). Hourglass slots are 26 + octant from true north. The
  scenario used the relative clock, which only went unnoticed because all eight are shown at once.
- **Tower occupancy.** Light Rampant outer towers (slots 9–14 clockwise from north) go `0x00200010`
  as a soaker steps in and `0x00400001` when empty; the centre tower (21) counts up through
  `0x00200010/0x00800040/0x02000100/0x08000400`. The slot order was confirmed from player positions
  in three traces. Darklit towers (42 N, 43 S) use one/two occupants and `0x01000001` when empty;
  the old code showed "one occupant" on an empty tower. Both now use `Core/Map/MapEffectTower`.
- **Pandora's Box.** Slot 47 goes `0x00100008` 13.99 s and `0x00200001` 19.08 s after the cast.

### Presentation and timing

- Darklit: retail puts head marker 62 on both Dark Water targets 0.09 s before the Darklit cast (all
  four pulls). Added; a Codex test had asserted there was none.
- Small timing corrections (all ≤ 0.3 s): Diamond Dust Twin Silence/Stillness +0.1 s and Hallowed
  Ray +0.12 s; Light Rampant second orb group −0.1 s; UR hourglass spawn −0.25 s and Shell Crusher
  hit +0.12 s; Apocalypse Dark Eruption −0.08 s; Darklit Spirit Taker −0.24 s, Somber Dance cast
  +0.06 s, second hit +0.06 s, Water +0.09 s; Pandora sequence stage starts (Box −0.1, second Fulgent
  −0.12, second Paradise −0.24, third Fulgent −0.2, Paradise Lost −0.28 s).

## Confirmed unchanged

Fall of Faith (reveals, Fetters, charges, impacts, cones, vulnerability 3.96 s), Diamond Dust,
Mirror Mirror (mirror slots, red pairs two octants apart, protean and Banish timing), Light
Rampant (assignments, hammers every 1.56 s, chains→curse, towers, Burst, Banish, House of Light),
Ultimate Relativity (tethers, Speed, fires/stacks, lasers at +2.05 s then 1.07 s, rewind, final
debuffs, Shell Crusher), Apocalypse (waters, Spirit Taker, six waves every 2.0 s, Darkest Dance,
knockback) and Darklit (chains, fate, cones, wings) all match within ~0.1 s.

## Still to check in game

- The new music: correct tracks, and that the offsets land on the retail position.
- Snapshot-based phase init in all five phases, from cold entry and phase switches (Upstream FRU
  already used this path in the same territory).
- Apocalypse in weather 105 with the normal stage floor; UR time-stop entering and leaving.
- Hourglass sand animation per wave; tower lights in Light Rampant and Darklit.
- Darklit head marker 62 on the water targets.
- Pandora's Box slot 47 transitions (what they draw is unknown).
