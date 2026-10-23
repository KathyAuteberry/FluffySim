# Spectator and Easthogg development batch

## History and scope

The live baseline was published `vnqupy` (3.0.3.0), above completed integration `xpxqro`.
`xpxqro` retains the confirmed P3 dive implementation in `xskups`. The alternate clone-reveal
experiment `msszqs` was explicitly rejected by native retest and remains excluded.

Independent sibling features from `vnqupy`:

- `xopvqt`: JESUS TAKE THE WHEEL.
- `okoqxr`: Easthogg claim randomization.

`tkzqks` is their shared two-parent `chore: integration commit`. Neither feature is an ancestor
of the other. Parent simplification changes nothing. Published `plkqvn` still has six parents;
master, origin, published integrations and releases are unchanged. All baseline GCD, P3 visual,
Wrath Twister and upstream changes remain ancestors. No release preparation or publication.

## Spectator behavior and limits

See [SPECTATOR-MODE.md](SPECTATOR-MODE.md) for architecture, controls, supported scenarios and
native acceptance checks. Eight ordinary bot actors replace the normal seven bots plus human.
The watched role is a bot, not an automated native player. Roster-based targeting and mechanic
resolution therefore see eight participants. Native local movement and action inputs are locked;
pending human cast resolution is interrupted on entry. God Mode cannot exempt the watched bot.

The native camera actor-lookup hook and state restoration compile against the installed SDK.
Camera snapshot restoration is tested against the real client struct. **Actual following, visual
quality, native party HUD and restoration in a running client remain unverified.** The code and UI
say so. There is no claim that a genuine Ultimate POV has passed native acceptance yet.

Support is exposed for DSR P3 Easthogg, DSR Strength 1 and FRU Light Rampant. Other scenarios
refuse spectator mode until their AI and presentation have been validated.

## Why P3 repeated sides

Line membership and arrow recipients were already shuffled. `Line()` then sorted roles by enum
order, and the AI used that order for free claims. Without arrows, early roles systematically
occupied West/NW and later roles East/NE; middle roles had constrained South coverage. This was
position assignment bias after numbered assignments, rather than a failure to reroll numbers.

The fix rolls a separate seeded claim permutation in scenario state. It is drawn after the
existing fight rolls, preserving their RNG draw sequence. Reusing the numbered-assignment order
was insufficient: forced-line swaps correlated the human's claim index with their old group.
The independent claim roll removes that correlation too.

Conditional on a group having no arrows, an eligible role has equal West/South/East claim
probability for 1s/3s, or NW/NE for 2s. Arrow groups still override claims exactly: Up West,
Circle South, Down East; arrowed bots face East. Arrow-pair counts, role restrictions, line sizes,
forced number/arrow choices, tower placement and subsequent movement remain intact. No artificial
three-attempt cycle or waypoint swap is introduced. The same seed/settings reproduce the same
claims. Restart/Start seed behavior remains coherent with normal practice.

The state test uses 192 seeds for every role and forced line, verifies repeatability and valid
arrow pairs, and rejects a dominant claim slot. Combined validation uses 64 reproducibly sampled
seeds: D1 receives each unarrowed West/South/East position, arrowed divers face East, and every
full spectator run completes with no deaths through towers, Geirskoguls and the later mechanics.

## Validation

- Debug solution build: passed, no errors (existing NUnit analyzer warnings).
- Independent P3 suite: 135 passed.
- Independent spectator/camera/action suite: 55 passed, including 48 full eight-bot clears across
  three scenarios, eight roles, and both Easy/Hard settings; pause, restart, normal-mode return,
  hidden-body restoration, human-to-spectator affliction cleanup, camera refusal, failure participation and pending-action cleanup.
- Final bounded integrated suite: 2,097 passed. Includes P3, spectator/camera, scenario catalog,
  GCD/recast, Wrath, pause, native entry, offline and DSR/FRU lifecycle regressions. Extended and
  Integration categories excluded. The superseded broad run was stopped after its initial
  coverage-test failure; the final bounded run tests the corrected implementation.
- Relay Debug tests: 67 passed.
- Packet safety: ordinary Debug plus isolated optimized TRACE Debug; 314 passed and 3 skipped
  for each of the default and SafetyHook backend models. Skips are native Windows hook-patching
  contracts unavailable on Linux. Architecture and stale-artifact freshness checks passed.

Headless lifecycle tests model the isolated stay and restoration. Existing offline regressions
pass, but title-screen offline and inn-launched spectator camera/body/HUD behavior still need
native playtesting. No Extended stress suite, local Release build, tag, push, master advancement,
version bump, GitHub release or plogons modification was performed.
