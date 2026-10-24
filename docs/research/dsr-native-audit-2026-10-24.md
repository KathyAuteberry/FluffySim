# DSR native audit: October 24–25, 2026 JST

## Sources and coverage

Read-only analysis used `Network_30304_20261024.log` (IINACT 2.10.3.11 /
ACT parser 3.0.3.4), filtering territory 968 and the evening session rather
than using the filename as a session boundary. The log contains 51 in-game
combat segments from October 24 22:09:43.879 through October 25 00:18:38.134 JST.
These segment numbers are the output of `tools/parser.py` for this file and
territory, not a claim that all 51 are full boss attempts. Checkpoint resets,
death and end-of-duty activity also produce combat boundaries.

Two native format-5 content-788 recordings were inspected:

- `Dragonsong's Reprise (Ultimate) 2026.10.24 22.06.27.dat`: header start
  22:06:01 JST, 26.069 seconds, one chapter; initial short entry, not P3 evidence.
- `Dragonsong's Reprise (Ultimate) 2026.10.25 00.18.38.dat`: header start
  October 24 22:08:54 JST, 7,783.796 seconds, 64 chapters, 527,693 segments,
  game build 14155597. This is the night's long DSR recording, even though
  its filename is the final timestamp.

The matching two `DragonsongsRepriseUltimate` files in the separate `Replaye`
folder start with binary `BLCB` magic. They are not ACT text or format-5 Duty
Recorder input; this audit inventories them but does not decode or claim
additional visual evidence from them. No independent video, rendered playback,
or packet-dump capture with a separately verified schema was available in the
inspected source directories. The IINACT text and native recorder are two
views of the same client's incoming events, not independent server clocks.

P1 action records are present, as are P2 Strength and Sanctity. The existing
Strength extractor finds 47 sequences, 46 complete by its limited criterion
(final Heavy Impact and Mercy effects); this does not mean 46 phase clears.
The Sanctity extractor finds 24 Sacred Severs sequences with four jumps each.
Only combat segment 33 reaches P3: 23:36:32.727–23:40:26.010 JST. It records the
first numbered dive, first towers and three first-set Geirskogul casts; the
second numbered dive follows during wipe processing. The group does not
supply a clean third dive, later towers, tether sequence or P3 completion.
Older recordings, synthetic `DSW2-Roost` practice, and the 2024 archive used by
the previous presentation audit were excluded as tonight's evidence.

## Method and clock limitations

`tools/parser.py`, `tools/replay_inspector.py`, `tools/dsr_strength_evidence.py`
and `tools/dsr_sanctity_evidence.py` were used without writing to originals.
Only sanitized aggregates are recorded here. Names, actor IDs and raw packets
are deliberately omitted. Extractor JSON remained outside the repository.

Tables use seconds; delta is **simulation minus observed**, in milliseconds.
ACT timestamps represent receive/parser events, not server execution or first
rendered pixels. Native replay milliseconds are receive-time segment clocks.
P3 uses the stable local anchor Dive from Grace cast minus 22.450 seconds:
23:39:38.832 JST in ACT, replay millisecond 5,444,601. It is an alignment
convention, not an independently observed phase-spawn timestamp. For example,
native towers are +41.345 whereas ACT reports +41.362: a 17 ms difference
between capture paths. We do not adjust mechanics to chase these residuals.

P2 sequences align to their named opening cast. Ranges are cross-pull
receive-time variation and include parser scheduling/network arrival jitter.
High confidence below concerns identification and encoded parameters; it does
not imply millisecond knowledge of the real server snapshot.

## P1 opening cross-check

The first two combat segments are the door-boss attempts. Opening cast-to-cast
intervals avoid treating the player's combat-flag transition as a boss anchor.

| Interval from first Holiest of Holy cast | ACT observed | Simulation | Delta ms | Confidence |
|---|---:|---:|---:|---|
| Empty Dimension start | 12.123 / 12.118 | 12.120 | −3 / +2 | Two attempts, moderate timing |
| Heavensblaze start | 17.155 / 17.151 | 17.150 | −5 / −1 | Two attempts, moderate timing |
| Hyperdimensional Slash start | 32.572 | 32.570 | −2 | Second attempt only, moderate timing |
| Heavensflame start | 74.875 | 74.850 | −25 | Second attempt only, moderate timing |

The door-boss opening is consistent at this precision. This does not validate
every later cast, target selection, interrupt, prison or phase-transition cue.

## P2 comparison

| Mechanic / event | Native/ACT relative time | Current simulation | Delta ms | Evidence / confidence |
|---|---:|---:|---:|---|
| Strength: Spiral, first Heavy and Lightning cast starts | 10.623–10.711 (median 10.652) | 10.640 | −12 median; −71 to +17 | 47 sequences, ACT; high event identity, moderate timing |
| Strength: Mercy boss cast | 18.684–18.793 (median 18.729) | 18.710 | −19 median; −83 to +26 | 47 sequences, ACT; moderate timing |
| Strength: Mercy helper cast | 21.754–21.870 (median 21.803) | 21.780 | −23 median; −90 to +26 | 368 target-helper casts; moderate timing |
| Sanctity: gaze cast | 12.553–12.662 (median 12.609) | 12.620 | +11 median; −42 to +67 | 24 sequences, ACT; moderate timing |
| Sanctity: first orb pair, example first recorded sequence | 19.021 | 19.040 | +19 | ACT, one illustrative sequence; moderate |
| Sanctity: last orb pair, same sequence | 22.851 | 22.875 | +24 | ACT, same sequence; moderate |

The Strength scenario starts its opening cast at scenario +3 seconds; that
three-second practice lead-in is removed before comparison. None of these
residuals supports a timing correction. The remaining P1/P2 events are
inventoried coverage, not a claim of exhaustive timing/geometry or native
visual validation. Meteor reconstruction, tower elevation and continuous
phase-transition presentation remain separate acceptance boundaries.

## P3 comparison

| Event | Observed relative time | Current simulation | Delta ms | Evidence / confidence |
|---|---:|---:|---:|---|
| Dive from Grace cast + numbers | 22.450 | 22.450 | 0 by alignment | ACT + native cast; high identity |
| Arrow/no-arrow applications, durations 9/19/30s | 28.200 | 28.150 | −50 | ACT status records; moderate timing |
| Lash and Gnash start | 29.579 | 29.600 | +21 | ACT; moderate |
| First arrow + number removals | 37.187 | arrow 37.150; number 37.280 (previously 69.600) | −37 arrow; +93 number (previously +32,413) | ACT; high ordering, moderate timing |
| First native jump effects 26382/26383/26384 | 37.277–37.278 | 37.280 | +2 to +3 | ACT action effects, all three variants; high identity |
| First Eye of the Tyrant effect | 37.411 | 37.450 | +39 | ACT; moderate |
| First Lashing Wheel effect | 40.828 | 40.890 | +62 | ACT; moderate |
| First towers start, cast duration 2.2s | ACT 41.362; recorder 41.345 | 41.380 | +18 ACT; +35 recorder | Three ACT/native casts; high encoded duration |
| First tower effects | 43.850 | approximately 43.880 | +30 | ACT; moderate, modeled 2.5s action duration |
| Second wheel effect | 43.938 | 44.010 | +72 | ACT; moderate |
| First Geirskogul cast | ACT 46.473; recorder 46.477 | 46.510 | +37 ACT; +33 recorder | Three casts; high identity |
| Geirskogul cast/snapshot duration | encoded 4.1999998s | 4.200 | approximately 0 | All three native ActorCast packets; high encoded parameter |
| Geirskogul omen delay | encoded byte 35 = 3.500s | 3.500 | 0 | All three native ActorCast packets; high encoded parameter |
| Geirskogul warning before encoded snapshot | derived 0.700s | 0.700 | 0 | Arithmetic from packet fields; not pixel measurement |
| Geirskogul impact after cast start | no clean effect before wipe cancellation | 4.500 | unavailable | Tonight does not independently validate impact |
| Second arrow + number removals | 47.185 | arrow 47.150; number 47.270 (previously 69.600) | −35 arrow; +85 number (previously +22,415) | ACT, wipe underway; high observed ordering |
| Second native jump effects 26382 | 47.274 | 47.270 | −4 | Two effects, wipe underway; moderate |

The three Geirskogul ActorCast packets occur at recorder millisecond 5,491,078,
opcode 127, length 32. The action ID at offsets 0 and 4 is 26378, action type
is 1, the delay byte at 3 is 35 and the float at 8 is 4.199999809. This
independently corroborates the existing 4.2-second snapshot / 3.5-second omen
configuration without reusing the old recording as new evidence. The current
4.5-second impact remains supported by the earlier audit and action data;
this night's wipe cannot add a successful impact sample. Do not shift it.

## Correction and remaining evidence

One narrow status-lifecycle correction removes a player's numbered status at
that numbered dive, rather than retaining every number until +69.600. The
first two native expirations clearly show the old lifetime was wrong. Existing
dive times are retained, so the correction clears about 90 ms after native
status removal while matching native action presentation within 4 ms. The
same per-wave rule is applied to the third wave for consistency; tonight
contains no clean third-wave expiration and does not validate its timestamp.
A three-wave deterministic regression verifies each number is present before
its dive, absent afterward, and later assignments remain intact. This does
not change arrow durations, bot routes, target snapshots, lockon effects,
tower placements, actor spawns, Geirskogul or the encounter timeline.

Native jump action identities agree with the current targeted ActionEffect
implementation. This confirms the event sequence, **not a rendered slam**.
The renderer/readiness corrections described in `DSR-P3-PRESENTATION-AUDIT.md`
still need native inn/offline playback acceptance: all three arrow variants,
human and bot targets, visible dive/impact and sound before the later tower,
late Geirskogul flash, Pause/Resume and Restart/Leave cleanup. Packet timestamps
cannot prove pixels, ground-omen first visibility, facing/position lock time,
closest-player bait choice, or the precise movement-safe instant. No synthetic
fallback animation or speculative global offset was introduced.

## Validation

Focused Debug `DsrP3NidhoggTests`: 60 passed, 0 failed/skipped; NUnit duration
15 seconds, whole successful build/test invocation 23 seconds (shell wall
measurement, rounded to seconds). This includes the three new status-lifetime
cases and existing dive presentation, placement, bot and lifecycle cases.
The first attempts exposed missing workspace datamining cache and an unset
`DALAMUD_HOME`; no encounter assertion failed. The pinned public CSV cache
was reused locally and the successful run used the installed dev SDK path.
Full integration validation and both packet-safety backends are recorded by
the integration workstream; this branch alone does not claim those results.
