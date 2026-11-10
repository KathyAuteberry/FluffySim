# CT Hallowed Wings and Wrath clockwise dodge — unreleased

This follow-up preserves the earlier Darklit presentation, Wrath/Death native castbar and local Provoke changes. It does not prepare or publish a release. Native retesting remains required, especially for Provoke's physical hotbar interception and the new Wrath movement.

## CT: demonstrated cause and reproduction

The old Wings effect explicitly passed non-tank front-four targets to `Damage(VulnerableTo(MagicVulnerabilityUp))`. The generic damage checker interpreted that vulnerability as unconditional death. It never compared incoming damage with HP or shields. Rear-four damage did not check the carried vulnerability at all. Splitting tanks and non-tanks before resolution also discarded the original eight-player rank for damage purposes.

The available native log confirms the reported MCH/D3 failure: first Wings from West, D3 fourth; second Wings from South, D3 third, MT/ST first and second. The first order was ST, D4, D2, D3, D1, H2, H1, MT; the second was MT, ST, D3, H1, H2, D1, D2, D4. The failure was the generic “had vuln up debuff” branch. The log shows Temperance and Desperate Measures on D3, but no ranged party reduction. Both tank party cooldowns were also absent from the old CT plan.

Earlier human-seat CT tests handed control over at 50s, after the 48.6s mitigation event, so the D3 provider was still treated as a bot when support was applied. They also used the exact bot recording offsets, excluding the valid fourth→third overlap. The old direct vulnerability test encoded the same unconditional-death assumption. The new overlap replay hands over control at 44.6s, before recording and mitigation, and checks damage and HP.

The original native log does not contain the seed, all recorded coordinates, HP or shield amounts. We therefore cannot reconstruct its exact original effective HP or claim the synthetic replay uses its original seed. Seed **9**, with a small human recording offset, deterministically recreates the exact **fourth→third** order in both North/South orientations. The replay uses a human MCH seat, actual Return/forced movement, real scenario casts, action-effect amounts and effect-result HP delivery. Both tanks are asserted to occupy second Wings ranks 1–2. No production movement, tank recording arms, transformations, snapshot geometry or tie-breaking were changed.

The damage reference is [the Hallowed Wings FAQ](https://ffxiv.tuufless.com/mana/fru/04_gaia_and_shiva/). The five party contributions, reference gear and basic shield approximation come from [the Mana mitigation plan](https://ffxiv.tuufless.com/mana/fru/mitigation/).

## Complete Mana support and actual calculation

The plan now supplies both tank 90s party cooldowns, H1 120s, H2 120s and D3's party reduction at 48.6, shortly after Spirit Taker. Their application checks living providers and recipients within 30 yalms plus the existing hitbox allowance. A human occupying one of these seats no longer removes its expected party support. Ranged presentation selects Troubadour, Tactician or Shield Samba for the recipient job. Tank contributions select the actual tank job's party shield/reduction. Their durations cover both Wings snapshots.

Living healers in range provide expected healing and a prepared basic 20k shield before each Wings, at 53.3 and 57.3. This is explicit simulated raid support, not a required healer/DPS personal defensive button. Shields are finite: remaining tank shields carry between hits; only the planned healer shield is refreshed. New shield applications are distinguished from partially spent old ones, including a refresh between snapshot and impact. Partial shield HUD values update at impact.

For the default WAR/PLD party and the guide's normal 157,000 physical-DPS HP, the deterministic fourth→third replay calculates:

| Snapshot | Base | With vulnerability | After three reductions | Shield before hit | HP after hit |
|---|---:|---:|---:|---:|---:|
| First, fourth | 55,000 | 55,000 | 37,868 | 59,250 | 157,000 |
| Second, third | 55,000 | 220,000 | 151,470 | 21,382 | **26,912** |

The reductions are `0.9 × 0.9 × 0.85`. Tank shields initially contribute the existing modeled 15% + 10% of recipient maximum HP; the prepared healer barrier adds 20k. The first hit consumes 37,868 of the tank shields. After the scheduled heal and healer-shield preparation, 1,382 of tank shielding plus the 20k barrier remain for the second hit. Rounding is upward to whole damage.

Removing that support before the second snapshot produces a real 220,000-damage death against 157,000 HP. Vulnerability is still 4×; inadequate mitigation, absent shields, bad tank-first positioning, missed anti-knockback and failed cleanses remain punishable.

## Model boundaries and diagnostics

Wings now resolves all eight ranks before applying the new front-four vulnerability. It uses the guide's rank damage, 0.64 tank / 0.94 magical-job role factors, actual active mitigation, current HP, max-HP boosts and finite shields. Surviving native actors receive calculated remaining HP through the existing local effect-result path. Other encounters retain their existing qualitative damage resolution.

The native doppels have a fixed placeholder 100k maximum HP; numeric Wings evaluates bots using reference pools of 225k tanks, 157k physical DPS and 142k magical jobs, preserving the corresponding percentage on their native bars. Human HP comes from the native actor. Native God Mode still preserves human and bot HP and suppresses death; a delayed damage confirmation cannot restore HP to an actor killed meanwhile by another mechanic. The per-run shield budget is replaced on Restart.

Normal tank practice supplies the expected 40% cooldown only when an actual 40% cooldown is absent. It never duplicates that personal cooldown. Strict tank practice uses actual personal actions and retains the independently labelled, UNVERIFIED approximate 56% vulnerable-front check. This option does not impose personal mitigation on DPS or healers.

Remaining approximations: guide base damage and role factors, reference bot gear, existing recipient-HP tank-shield fractions rather than exact caster-HP/potency scaling, a prepared 20k healer barrier, planned full healing between snapshots, and the opt-in tank threshold. This is a numeric Wings practice model, not a general combat/gear/healing simulator.

New `[CT Return]` events record every saved/restored coordinate. Each `[CT Wings]` snapshot logs source, rank, role, position, carried vulnerability, anti-knockback, base/role-adjusted/vulnerable damage, active status IDs and expiration times, reduction product, shields before/after/absorbed, maximum/current/remaining HP and failure explanation. These are snapshot events, not per-frame logging. Original raw native logs/captures were not added to source control.

## Wrath: clockwise route correction

The [Elemental Step 4 diagram](https://ffxiv.tuufless.com/elemental/dsr/05_alternate_thordan/) places the former SW tether target on the West side and the former SE tether target farther clockwise on the SW side. Assignment identities were retained after tether cleanup; they were not lost.

The old AI paired the SW start (210°) with 220° and the SE start (150°) with 270°. One barely moved clockwise while the other took a long straight chord across the arena. A single final destination did not describe the required outer clockwise route. The old survival-only tests could pass after a short inward dodge without verifying clockwise choreography. The fake client does not establish that either native actor literally remained stationary; native animation/movement confirmation is still needed.

Corrected destinations, all relative to the white dragon: leap 300°→330°, SW tether 210°→270°, SE tether 150°→220°. Three sprint waypoints at **45.15, 46.05 and 46.95** describe clockwise outer arcs, then the radius-15 spreads. Initial Spiral Pierce (45.06) and Skyward Leap (45.10) snapshots remain ahead of movement. Twister position snapshots (44.97), spawn/arming, Mercy, baited AoEs, Altar Pyre, Liquid Heaven and Thunderstruck are unchanged. The divebomb player still goes behind Charibert and the remaining four spread East.

Human-controlled marked seats are not steered by these bot routes. The tests separately prove that a stationary human receives no automatic dodge and dies to Twister, while explicit human pose input replay can complete the route with the other bots. Pose playback is a headless input model, not proof of native human input.

## Validation

All builds/tests used Debug. Final bounded validation: **471 passed**, no failures or skips, **68.543s wall time** (61s NUnit time), including ordinary Debug compilation. The optimized TRACE Debug artifact built in **5.499s** to `/tmp/fluffysim-ct-wrath-safety/`, using separate `obj/ct-wrath-safety-Debug` intermediates. The ordinary artifact is `AnoMech/bin/Debug/FluffySim.dll`.

| Final packet-safety run | Result | Wall time |
|---|---|---:|
| Default backend | 313 passed, 3 skipped | 20.385s |
| SafetyHook failure model | 313 passed, 3 skipped | 20.263s |

Each safety backend inspected both fresh ordinary and optimized TRACE Debug artifacts with stale-artifact protection enabled. The three skips are native Windows hook-patching cases on Linux. Static boundary checks and modeled client/server transport/filter failure cases passed; no fabricated server-bound operations were introduced. NuGet advisory lookup emitted the existing NU1900 connectivity warning; builds had zero errors.

The complete headless run is recorded in `/tmp/ct-wrath-complete.trx`; both safety logs are `/tmp/ct-safety-complete-default.log` and `/tmp/ct-safety-complete-safetyhook.log`. These local diagnostic outputs and binaries are not committed. No Release build, version edit, push, tag, publication or plogons operation occurred.

Scoped anonymous changes: `ruwtky` (`d27683`) contains CT damage/support and its core regressions; `knuuox` (`60176e`) contains Wrath clockwise routing and its regressions; `mvooqr` records this report. They continue the existing `xlkrwp` → `mozkkp` → `yowxqp` → `zxqmxx` → `kxkywx` ancestry above published `master@origin` (read newest to oldest). Both local and origin master remain `xorxqy` / `f0f74b`, published 3.0.1.0. The published `qolpkm` integration, Paradise/Wroth parents and historical `plkqvn` remain ancestors. Only one workspace exists; no unpublished shared integration generation was found. No bookmark was moved and no integration or released change was rewritten. This feature chain is ready for owner review/native testing; it is not a prepared 3.0.2.0 release.

Coverage includes exact D3 fourth→third and front→rear orders, both wave orientations and bot difficulties, full/absent support, HP and damage amounts, 30-yalm coverage, active/expired mitigation/shields, partial consumption/refresh, restart cleanup, existing anti-knockback/cleanse/tank crossings, strict tank practice, four rotated Wrath layouts, attack-before-movement snapshots, clockwise displacement before Twister danger, separated final spreads, human leap/either tether responsibility and survival through Wrath's 88.1 completion. Prior native castbar, Provoke and lifecycle regressions are retained. Large randomized suites remain opt-in.

## Native retest

1. In both inn and offline entry, play MCH/D3 with Mana and normal mitigation settings. Record centrally for the first hit's fourth rank and the second hit's third rank, keep both tanks ahead, and press Arm's Length before stun. Repeat with North/South second-wave origins. Inspect the two `[CT Wings]` rows: expected support, finite shields, correct 4× vulnerability and positive final HP. Record actual HP/gear if it differs from the reference replay. Check another ranged job and H1/H2 for automatic party support.
2. Repeat after Pause/Resume and Restart. Omit anti-knockback, deliberately mis-record or remove support only in a diagnostic run: the appropriate movement, geometry or damage failure must remain. Tank strict mode should continue requiring actual personal mitigation; normal mode supplies the expected tank cooldown.
3. In Wrath, watch all three early marked bots after their hits. They should visibly arc clockwise relative to the white dragon, leave their Twister locations, then spread without crossing the central knights. Rotate the dragon and select blue/either tether human preferences. The human must move; other bots must continue safely through the full sequence and connecting tankbuster.
4. Recheck Wrath/Death Twisting Dive in the normal Enemy List and the physical ST Provoke button using the existing structured action diagnostics. These previous candidates are preserved, but headless action routing cannot certify native hotbar interception. Do not call that interaction fixed in-game without owner confirmation.

Discord wording, after native confirmation: “CT Wings now calculates real ranked damage instead of treating vulnerability as instant death, and the bots provide the complete Mana party mitigation plan. The valid D3 fourth→third overlap survives without an extra personal defensive.” / “Wrath's blue and tether bots now dodge clockwise around the white dragon before spreading, while human targets still perform their own dodge.” These changes are **not released**.
