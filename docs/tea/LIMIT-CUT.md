# Mana 1211 Limit Cut

The scenario identity is `The Epic of Alexander (Ultimate) — I1 Limit Cut`;
its separately saved strategy key is `Mana\n1211`. Existing group/name preference
storage is sufficient for a future Elemental AI. The timeline, native actors,
number rolls, random starting axis, collision and failure logic are shared.
Only `TeaI1LimitCutAi` owns Mana paths and priorities. The inherited 1256 AI is
retained for explicit legacy regression fixtures and is not a selectable strategy.

## Assignment and route authority

Nukemaru [P1 and Limit Cut](https://www.youtube.com/watch?v=AMdtMyvbPNs),
24:14–25:16, explains the single clockwise pack and the 1/2/1/1 radial advances;
25:18–28:40 supplies the numbered responsibilities. The Japanese macro selects
east in an east/west start. Starting directions are N, NE, E and NW for the four
possible axes. All eight share that start: there is no 1256/3478 split.

Pairs 1/2, 3/4, 5/6 and 7/8 stop on perimeter blast lines 2, 4, 6 and 7.
Odd numbers take the outer lane and face out before Cruise Chaser appears behind
them. Even numbers take the inner lane and also face outward for the charge.
The lanes stay separate: the supplied clear target poses put odds roughly 18–19y
from center and evens roughly 12–16y out. Facing the wall does not put both
players on the same radius; only the odd player's facing chooses the warp side.
Completed numbers rejoin clockwise; they stay outside the final central blast
and clear the last charge before regrouping. Both captured clears corroborate
this one-pack convention, with successive odd targets advancing 45°, 90°, 45°
and 45° from the first blast axis.

AI waypoint radii and movement margins are implementation choices fitted to the
shared real collision shapes, not surveyed requirements of the Japanese guide.
The route checks ordinary action collisions and vulnerability statuses for every
seat. It does not mark a number successful or exempt bots from damage. Sprint
and knockback immunity are actual simulated statuses; the human must supply their
own facing, movement and actions when bot control is off.

ACT261 changes are partial updates. A missing position key must retain the last
value rather than discard the sample. Sparse samples and long periods without
position packets do not reveal a continuous route or exact stopping times. The
committed evidence tool accumulates those fields. The playable bot route is a
coherent implementation of the guide, not a claim that interpolation reproduces
every captured footstep.

## Timing ledger

The fragment stages actors before numbers. For comparison, align the first
number marker at scenario 3.19 with pull 4's 65.472 (subtract 62.282 from that
pull's timestamps). This normalization does not prove the exact Living Liquid
death time. Receive clocks agree closely; server execution and frame rendering
have additional uncertainty.

| Event | TEA-L01 pull 4 | Normalized observed | Implemented | Difference, s |
|---|---:|---:|---:|---:|
| First number | 65.472 | 3.190 | 3.190 | 0.000, anchor |
| First Hawk Blaster ability | 67.518 | 5.236 | 5.270 | +0.034 |
| Alpha Sword 1 | 74.955 | 12.673 | 12.640 | −0.033 |
| Charge 2 | 76.425 | 14.143 | 14.140 | −0.003 |
| Alpha Sword 3 | 79.583 | 17.301 | 17.310 | +0.009 |
| Charge 4 | 81.049 | 18.767 | 18.800 | +0.033 |
| Alpha Sword 5 | 84.213 | 21.931 | 21.950 | +0.019 |
| Charge 6 | 85.685 | 23.403 | 23.420 | +0.017 |
| Alpha Sword 7 | 88.860 | 26.578 | 26.580 | +0.002 |
| Charge 8 | 90.334 | 28.052 | 28.050 | −0.002 |
| J Kick | 93.562 | 31.280 | 31.260 | −0.020 |

These compare native ability events, not visible impact frames. The inherited
0.13 s turn lead, 0.1 s warp-position lead, and 0.63/0.31/0.53 s impact delays
for Blaster/Sword/Charge remain presentation approximations pending native retest.
Castless actions have no fabricated cast bar or added ground omen. Hawk Blaster
helpers are staged one second early, with their explosion timeline preloaded;
they are retained through the next wave and cleared by J Kick.

## Native checks still required

Verify all eight numbered markers, Cruise Chaser's model visibility at every warp,
model facing before each sword, authentic explosion and charge animations, enemy
list reveal at transition, and actual impact lead times. Check Pause/Resume around
a warp and an explosion, Restart during active markers, then Leave in both offline
and inn sessions. Headless packet and draw-state tests do not establish that these
animations render in FFXIV.

## Playback correction — 2026-11-11

Playback review identified inward-facing even baiters. The AI now faces every
number outward before its own hit, while preserving the captured inner/outer
lanes and the guide-defined starting-axis priority. In clear 12, number 6's
received target pose is (99.84, 112.41), rotation −0.09: a south-facing player
in the inner southern lane. This corroborates outward facing without relocating
evens to the wall. Other receive-time poses can include movement or stale
headings, so these are not treated as exact facing-lock timestamps.

The focused regression probes every number before its hit across all four axes,
checks outward model facing and distinct lane radii, then requires the real
collisions and vulnerability checks to complete without deaths. Native warp and
charge presentation still need a client retest.
