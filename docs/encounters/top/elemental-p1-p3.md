# TOP native Elemental scenarios, P1–P3

Seven scenarios under `AnoMech/Scenarios/TopNative/` (zone `TopNativeZone`, sidebar "FluffySim Native"),
following the Tuufless Elemental guide (`ffxiv.tuufless.com/elemental/top/`, mirrored locally in
`~/Projekty/Tuufless/FFXIV-Elemental-raid-macros/_elemental/top/`; the mirror is newer than every TOP
commit upstream). Built 2026-11-18; the owner's first in-game pass the same day drove the fixes noted
below as "in-game feedback".

## Evidence

A friend running Elemental captured two lockouts on 2026-11-17 (timestamps UTC+8):

| File (`~/Stažené/top/`) | What |
|---|---|
| `Network_30305_20261117.log` | IINACT log, 53 pulls in territory 1122 |
| `20261117-130520-duty908.rtrace`, `20261117-144120-duty908.rtrace` | RaidTrace 0.3.1, one per lockout |
| `the Omega Protocol (Ultimate) 2026.11.17 22.30.51.dat`, `… 2026.11.18 00.07.57.dat` | Duty Recorder replays (not decoded yet) |

Pulls reaching each mechanic: Program Loop 53, Pantokrator 45, Party Synergy ~22, Limitless Synergy 7,
Hello World 5, Oversampled Wave Cannon 1 (a wipe). Party in the captures, mapped to Elemental roles by
the P1 wave-cannon spread spots: H1 SCH, H2 SGE, D1 SAM, D2 VPR, D3 DNC/BRD, D4 BLM, tanks WAR and DRK.
Positions come from the traces' server-timed `actor_move`/`position_report`; events from the ACT log.
Analysis scripts were scratch only (not committed).

Zone-in: retail parties appear around (100, 115), the entrance barrier's centre; its EObj sits 5 y east at
(105, 115), so `TopNativeZone.PlayerSpawn` is set explicitly (in-game feedback: spawning on the EObj put the
player outside the barrier ring).

Phase environment: weather 77 / 78 / 79 (176 during Hello World), BGM 962 / 963 / 950; P1 floor is
MapEffect slot 9 (shown from Program Loop + 5.9 s), P2 floor slot 10.

## Program Loop

Times from the Program Loop cast: numbers +4.74 s (Looper 16/25/34/43 s); towers spawn +7.94, +17.08,
+25.96, +34.90 and resolve +18.16 + 9 s·k; Blaster tethers +10.06, explosions 0.13 s after each tower set.
Tower spots are 12.37 y out, 14° either side of the cardinals, each with its own layout ID; a set takes two
spots on the same side of their cardinals (90° or 180° apart) and every spot is used once. Omega faces south,
the party stacks south, 3s step in front. Retail holders stand 16.5 y out on the empty cardinals; towers and
tethers follow "CW from NW: H1 MT ST D1 D2 D3 D4 H2".

Simplification: bots only intercept a tether on their turn (`TopProgramLoopTetherEnd`); the human always can.

In-game feedback (owner): bots re-aim at the live tether line every 0.2 s from the blast, cutting it near the
centre, because a human holder starts moving as soon as their defamation goes off; the next grabbers idle
between the towers (towers 90° apart) or just behind a tower (180°), never beside the centre, where greedy
melee may drop a defamation. Old bot holders leave 0.5 s after their blast.

## Pantokrator

Pantokrator cast, numbers +4.96 s; Flame Thrower pairs cast every 2.0 s from +7.11 (14 pairs, 30° steps,
direction and first bearing random, relative to Omega facing north); puddles every 2.0 s from +14.63
(10 waves); resolves +17.09 + 6 s·k, missile on number k+1, beams on the numbers whose Condensed Wave
Cannon runs out (3, 4, 1, 2). Wave cannons: markers +38.69/+47.72, lines +43.78/+52.82, Diffuse cones on
the two farthest every ~2.08 s from +43.78.

Retail movement, normalised to clockwise flames: each stack stands 101° ahead of the first flame axis at the
first puddle (just ahead of the approaching cone's leading edge) and steps 30° per puddle; the missile player
goes centre (+41°, 6 y) → front (+88°, 11.5 y, missile resolves) → back and out (+61°, 17.5 y) → rejoin
(+11°, 16.5 y), the guide's slower path. Starting sectors follow the guide's NW/SE rule.

In-game feedback (owner): before the numbers the party waits loosely spread 3 y around the centre ("the rest
of the party can stay near the center", Omega facing north), and only reacts 0.8 s after the debuffs land;
each stack is spread 1.2 y along its radius, which keeps everyone inside the shared beam. A 2.4 y spread let a
rejoining missile player cross a puddle.

Flame Thrower timing checked against 36 retail pulls: the bar is 3.7 s and the hit lands 3.97 s after the cast
starts, as simulated. Retail players stay inside the next cone's telegraph for a median 2.7 s, leave it with 1.27 s
left and stand in some telegraph 88% of the time, so standing in yellow is correct here. As each flame fires the
party is 41° ahead of its axis, 11° past its front edge, then steps one 30° slice within about 1.2 s and waits 71°
ahead of that axis, 11° clear of the next flame. UNVERIFIED: the retail omen delay, which the captures do not expose; the simulator shows the omen
when the cast starts.

Settings: your number (solo) and, per number, whether that pair adjusts sides. A pair adjusts when both players
start in the same half of the priority line (H1 MT ST D1 / D2 D3 D4 H2); adjusting pairs always come in twos.

## Party Synergy

Upstream's scenario matched retail to within 0.05 s on every key event, so the native one copies it and adds
an Elemental AI: light parties N→S (healer, tank, melee, ranged), lower duplicate swaps; spreads with the eye
as north in 〇×▽□ order at x = ±11.25 (Mid) or the Remote layout with the right group's 〇/□ swapped; stack
swap by the one farther from the eye; knockback west / south (Mid) or east (Remote), Remote stacks at ±19 y.

## Limitless Synergy

Native, from retail: clones at 22 y (Bladedance pair on opposite intercardinals, others on random 30° steps);
tethers +10.23, Sagittarius cast +10.36 aimed at the party, Bladedance +18.46, flares +24.27, Beyond Defense
jump +29.50, Pile Pitch +32.63, Cosmo Memory +41.43. Tanks take tethers 56° clockwise of their clone at 19 y
(adjusted to clear the arrow), party dodges the arrow inside the target circle; the hit tank leaves sideways.

Simplification (owner's request): 1 s after Cosmo Memory, Omega-F's Laser Shower bar is recast as a 3 s
cast; retail's runs on for ~30 s with nothing left to do.

## P3 transition

From the Hello World cast: debuffs −27.3 (19 s), Wave Repeater casts −27.23 and −19.09 (rings at +7.07,
+9.12, +11.18), arms reveal −24.19/−21.19 and cast −12.16/−9.66 (r11 at 14 y, sets 60° apart). Retail weave:
edge (19 y), step in to 16.3 y for each 18–24 y ring, sit 22° beside a second-set arm before the first arms,
then 15° inside a first-set arm crater. Stack pairs at 340°/20°, Sniper spreads at 270/215/145/90°.

## Hello World

From the cast: debuffs +4.98, bugs and Nisis +8.01; towers cast +19.14, +40.18, +61.27, +82.30 and resolve
9.99 s later (four towers 14 y out, all cardinal or all intercardinal, each colour on two adjacent spots);
regression tethers 1.12 s before each resolve; Critical Error +105.45. Touching copies a Nisi (the giver keeps
theirs); the four pairs hand roles on as `TopHelloWorldState.RoleInLoop`. Retail positions relative to the
towers: Overflow 18.8 y on its tower, Local 34° outside at 14.5 y, Synchronization 18° inward at 12.3 y,
Remote 30° inward at 11.7 y; the last loop puts Local with the stacks.

Simplifications: Nisis pass only to a player without one; bots accept one only in their strat's receiving
window; touch distance 1.2 y; tether break at 10 y (upstream's P5 value).

In-game feedback (owner's static rule, common in PF): never cut through the centre, where touching another
player can swap a Nisi. Bots move along arcs, tether players (Local, Remote) on 12 y or more and debuff players
(Overflow, Synchronization) on 6 y or more; only the last loop's Locals meet in the middle on purpose.

## Oversampled Wave Cannon

Five retail samples (cast, monitors −0.09 s, fire +10.07, Ion Efflux +16.11; all five agree to 0.01 s). Astoh
spots follow the four 2026-11-20 pulls, normalised to a west cleave: north and south players about 19 y out,
centre (−4, 0), west (−15.5, 0), south-middle (2, 10.5), monitors (9.4, −11.5), (14, −3.4) and (13.5, 6.8)
(line-up H1 H2 MT ST D1 D2 D3 D4); monitors turn to face. Each monitor picks two on its side, as upstream's P5
monitors do.

## Check in game

Still open after the first pass: arenas from every phase flash for a second or two at zone-in, the same as
FRU (a shared zone-load issue, not TOP-specific); Pantokrator past the telegraph fix, which blocked it.

- Tower EObj visuals, layout IDs and director args (Program Loop); flame and puddle omens (Pantokrator).
- Clone looks and warps in Limitless Synergy; Omega-M's shield look.
- Transition arms and rings; Hello World tower omens, tethers and statuses; monitor VFX.
- Bot movement looks sane at every step; the human-only rules above feel right.
