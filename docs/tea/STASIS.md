# Mana fixed Temporal Stasis

The shared scenario rolls one support and one DPS for each debuff category,
freezes movement and status timers, resolves proximity conditions and delayed
boss attacks, then releases time. Mana choices live in `TeaI2TemporalStasisAi`:
no debuff goes to the left robot, green/House Arrest to the right; supports stand
north and DPS south. Blue/Restraining Order uses support left and DPS right,
independent of where those players happened to be standing. On Cruise Chaser's
side that bait stands closer to the boss. Lightning/Aggravated Assault goes to
Brute Justice's outer side. The encounter does not branch on a regional enum.

The Japanese macro is the assignment authority. Its left/right rules apply to
both boss arrangements. The inherited AI previously picked blue sides from the
players' relative X coordinates; that was replaced by the explicit support/DPS
rule. Distances and bait margins remain measured/practice approximations; the
inherited five-yalm House Arrest boundary is marked unverified.

## Timing ledger

TEA-L01 pull 4 receives Temporal Stasis at 281.745. The practice casts it at 7.55;
use 274.195 as the comparison offset. This is a cast anchor, not independent
proof of the preceding robot's disappearance time.

| Event | Implemented, s | Evidence/confidence and remaining verification |
|---|---:|---|
| Debuff assignment / cast start | 7.55 | Macro assignments; measured cast anchor |
| Frozen simulation / status-time lock | 16.70 | Inherited native sequence; verify status/animation boundary against replay playback |
| Proximity/lightning resolves | 18.58 | Inherited delayed action sequence; exact visible impact pending native retest |
| Three Alpha Swords | 20.62 / 21.69 / 22.76 | Real actions and nearest-bait selection; client-facing animation pending retest |
| Resume time | 25.60 | Status timers resume; verify visible time-stop/BGM transition |
| Alexander Prime return | 26.80 | Native warp presentation pending retest |

Existing negative coverage exercises distance failures, overlapping cleaves and
status vulnerabilities on the shared scenario. The final regression result is
recorded in [the implementation report](IMPLEMENTATION.md). Both boss layouts,
all four debuff categories, native status/tether rendering, Pause/Resume and
Restart still require an in-client check. Headless action/status emission does
not establish native freeze or jump visibility.
