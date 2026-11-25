<p align="center"><img src="../../images/icon.png" alt="FluffySim logo" width="128" height="128" /></p>

# Supported encounters

Every Ultimate, every major mechanic, and what FluffySim can practise for each Japanese strategy it follows: **Elemental** and **Mana**. The native scenarios are listed by Ultimate, newest first. [Upstream practice](#upstream-practice), inherited from AnoMech with other regions' strategies, is listed at the end.

| Status | Meaning |
| --- | --- |
| ✅ Verified | Timings, positions and assignments are checked against recordings of the real duty. |
| 🟢 Playable | Complete and playtested, but no recording of the real duty covers it yet. |
| 🧪 Alpha | Playable from start to end, but built from few retail pulls or from upstream; timings, bot spots or strategy details may still change. |
| ⬜ Not implemented | Not in FluffySim yet. The link opens a [fight data](../../CONTRIBUTING.md#contributing-fight-data) issue for it. |

Only recordings of the real duty count as verification; recordings made in FluffySim don't. **Menu entry** is where you find the mechanic in the FluffySim window. Several mechanics can share one entry, which then runs them in order.

## Overview

Mechanics with an Elemental or Mana scenario, out of the major mechanics listed for that Ultimate.

| Ultimate | Elemental | Mana |
| --- | --- | --- |
| [Dancing Mad (Ultimate)](#dancing-mad-ultimate) | ✅ 1 / 13 | ✅ 5 · 🧪 3 / 13 |
| [Futures Rewritten (Ultimate)](#futures-rewritten-ultimate) | — 0 / 12 | ✅ 12 / 12 |
| [The Omega Protocol (Ultimate)](#the-omega-protocol-ultimate) | ✅ 8 / 16 | — 0 / 16 |
| [Dragonsong's Reprise (Ultimate)](#dragonsongs-reprise-ultimate) | ✅ 7 · 🟢 16 / 23 | — 0 / 23 |
| [The Epic of Alexander (Ultimate)](#the-epic-of-alexander-ultimate) | — 0 / 13 | ✅ 13 / 13 |
| [The Weapon's Refrain (Ultimate)](#the-weapons-refrain-ultimate) | — 0 / 11 | — 0 / 11 |
| [The Unending Coil of Bahamut (Ultimate)](#the-unending-coil-of-bahamut-ultimate) | — 0 / 14 | — 0 / 14 |

Don't see your strategy? FluffySim accepts fight data from any Japanese strategy (Elemental, Gaia, Mana, Meteor); see [Contributing](../../CONTRIBUTING.md).

## Dancing Mad (Ultimate)

Mana follows Yan Flash's guide. P1 and P2 are native beta scenarios, built from retail combat logs and Duty Recorder captures. P3 is an alpha: its scenarios are playable, but not yet checked against enough retail pulls. The phases after P2 are listed as far as the upstream catalog covers them, so this list may still be missing mechanics.

| Phase | Mechanic | Elemental | Mana | Menu entry |
| --- | --- | --- | --- | --- |
| **P1** | Graven Image 1 | [⬜ Not implemented][dmu-p1-graven-image-1-e] | ✅ Verified | P1 › Graven Image 1 |
|  | Graven Image 2 | [⬜ Not implemented][dmu-p1-graven-image-2-e] | ✅ Verified | P1 › Graven Image 2 |
|  | Graven Image 3 | [⬜ Not implemented][dmu-p1-graven-image-3-e] | ✅ Verified | P1 › Graven Image 3 |
| **P2** | Forsaken | [⬜ Not implemented][dmu-p2-forsaken-e] | ✅ Verified | P2 › Forsaken |
|  | Trine | ✅ Verified | ✅ Verified | P2 › Trine |
| **P3** | Elements | [⬜ Not implemented][dmu-p3-elements-e] | 🧪 Alpha | P3 › Elements |
|  | Limit Cut | [⬜ Not implemented][dmu-p3-limit-cut-e] | 🧪 Alpha | P3 › Limit Cut |
|  | Black Hole | [⬜ Not implemented][dmu-p3-black-hole-e] | 🧪 Alpha | P3 › Black Hole |
| **P4** | Kefka Says | [⬜ Not implemented][dmu-p4-kefka-says-e] | [⬜ Not implemented][dmu-p4-kefka-says-m] | — |
| **P5** | Flood | [⬜ Not implemented][dmu-p5-flood-e] | [⬜ Not implemented][dmu-p5-flood-m] | — |
|  | Celestriad | [⬜ Not implemented][dmu-p5-celestriad-e] | [⬜ Not implemented][dmu-p5-celestriad-m] | — |
|  | Exaflares | [⬜ Not implemented][dmu-p5-exaflares-e] | [⬜ Not implemented][dmu-p5-exaflares-m] | — |
|  | Forsaken Null | [⬜ Not implemented][dmu-p5-forsaken-null-e] | [⬜ Not implemented][dmu-p5-forsaken-null-m] | — |

- **Graven Image 1–3** cover Revolting Ruin III tank swaps, Mystery Magic, Pulse Wave, Wave Cannon towers, Double Trouble Trap, Gravitas, Vitrophyre, the Tele-portent chase and the final gaze (upstream calls this part Tele-trouncing). At each tank swap the incoming tank must Provoke Kefka during the tankbuster cast. Graven Image 3 is solo-only.
- **Forsaken** covers all eight tower sets, the Future's and Past's End baits and the final-tower self-marks.
- **Trine**: since 2026-11-19 Yan Flash uses Elemental's Trine (the party takes the first safe triangle clockwise from north, the tanks the first counter-clockwise from north-west; the main tank steps out from Kefka square to the first side of the centre triangle counter-clockwise from north-west), so one scenario serves both.
- **Elements** (P3, Bowels of Agony) runs from Definition of Insanity to the second elements' Cyclones and follows Yan Flash's TLB3 method: everyone stands on fixed spots set by the wind and water crystals, so no wind is cleared before Vacuum Wave. The timeline comes from three retail pulls; the bot spots come from Yan's figures and still need an in-game check. Ultima Blaster and Vacuum Wave are not included yet.
- **Limit Cut** and **Black Hole** (P3) are native copies of the upstream scenarios with multiplayer and JESUS TAKE THE WHEEL. Limit Cut keeps upstream's solution. Black Hole uses the selfmark beam order: first in line press Attack (1–3), second Bind (1–3), third Ignore (1–2); the Thunder III sets default to ISSIS (ST invulns set 1, set 2 is shared MT first). Every P3 scenario has a Tanks setting for which tank holds Chaos (default: MT on Exdeath, ST on Chaos). Their timings are upstream's and still need our own retail check.
- Upstream practice for P1–P5 is listed under [Upstream practice](#upstream-practice).

[dmu-p1-graven-image-1-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P1%20Graven%20Image%201%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P1%20Graven%20Image%201&strategy=Elemental%3A%20
[dmu-p1-graven-image-2-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P1%20Graven%20Image%202%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P1%20Graven%20Image%202&strategy=Elemental%3A%20
[dmu-p1-graven-image-3-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P1%20Graven%20Image%203%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P1%20Graven%20Image%203&strategy=Elemental%3A%20
[dmu-p2-forsaken-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P2%20Forsaken%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P2%20Forsaken&strategy=Elemental%3A%20
[dmu-p3-elements-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P3%20Elements%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P3%20Elements&strategy=Elemental%3A%20
[dmu-p3-limit-cut-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P3%20Limit%20Cut%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P3%20Limit%20Cut&strategy=Elemental%3A%20
[dmu-p3-black-hole-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P3%20Black%20Hole%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P3%20Black%20Hole&strategy=Elemental%3A%20
[dmu-p4-kefka-says-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P4%20Kefka%20Says%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P4%20Kefka%20Says&strategy=Elemental%3A%20
[dmu-p4-kefka-says-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P4%20Kefka%20Says%20%28Mana%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P4%20Kefka%20Says&strategy=Mana%3A%20
[dmu-p5-flood-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P5%20Flood%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P5%20Flood&strategy=Elemental%3A%20
[dmu-p5-flood-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P5%20Flood%20%28Mana%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P5%20Flood&strategy=Mana%3A%20
[dmu-p5-celestriad-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P5%20Celestriad%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P5%20Celestriad&strategy=Elemental%3A%20
[dmu-p5-celestriad-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P5%20Celestriad%20%28Mana%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P5%20Celestriad&strategy=Mana%3A%20
[dmu-p5-exaflares-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P5%20Exaflares%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P5%20Exaflares&strategy=Elemental%3A%20
[dmu-p5-exaflares-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P5%20Exaflares%20%28Mana%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P5%20Exaflares&strategy=Mana%3A%20
[dmu-p5-forsaken-null-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P5%20Forsaken%20Null%20%28Elemental%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P5%20Forsaken%20Null&strategy=Elemental%3A%20
[dmu-p5-forsaken-null-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DMU%20P5%20Forsaken%20Null%20%28Mana%29&duty=Dancing%20Mad%20%28Ultimate%29&phases=P5%20Forsaken%20Null&strategy=Mana%3A%20

## Futures Rewritten (Ultimate)

Mana follows Tuufless's translation of the Mana guide. Every major mechanic is in, checked against recordings of the real duty. Simple mechanics that need no practice (Cyclonic Break, Utopian Sky, the P1 towers, Endless Ice Age and the Akh Morns) are left out on purpose.

| Phase | Mechanic | Elemental | Mana | Menu entry |
| --- | --- | --- | --- | --- |
| **P1 Fatebreaker** | Fall of Faith | [⬜ Not implemented][fru-p1-fall-of-faith-e] | ✅ Verified | P1 › Fall of Faith |
| **P2 Usurper of Frost** | Diamond Dust | [⬜ Not implemented][fru-p2-diamond-dust-e] | ✅ Verified | P2 › Diamond Dust |
|  | Mirror, Mirror | [⬜ Not implemented][fru-p2-mirror-mirror-e] | ✅ Verified | P2 › Mirror, Mirror |
|  | Light Rampant | [⬜ Not implemented][fru-p2-light-rampant-e] | ✅ Verified | P2 › Light Rampant |
| **P3 Oracle of Darkness** | Ultimate Relativity | [⬜ Not implemented][fru-p3-ultimate-relativity-e] | ✅ Verified | P3 › Ultimate Relativity |
|  | Apocalypse | [⬜ Not implemented][fru-p3-apocalypse-e] | ✅ Verified | P3 › Apocalypse |
| **P4 Gaia and Shiva** | Darklit Dragonsong | [⬜ Not implemented][fru-p4-darklit-dragonsong-e] | ✅ Verified | P4 › Darklit Dragonsong |
|  | Crystallize Time | [⬜ Not implemented][fru-p4-crystallize-time-e] | ✅ Verified | P4 › Crystallize Time |
| **P5 Pandora** | Fulgent Blade | [⬜ Not implemented][fru-p5-fulgent-blade-e] | ✅ Verified | P5 › Fulgent Blade |
|  | Paradise Regained | [⬜ Not implemented][fru-p5-paradise-regained-e] | ✅ Verified | P5 › Paradise Regained |
|  | Polarizing Strikes | [⬜ Not implemented][fru-p5-polarizing-strikes-e] | ✅ Verified | P5 › Polarizing Strikes |
|  | Pandora's Box (enrage) | [⬜ Not implemented][fru-p5-pandora-s-box-enrage-e] | ✅ Verified | P5 › Pandora's Box |

- **Crystallize Time** runs on through the second Akh Morn (7:1, ST solo) and Morn Afah. The same entry also offers an NA strategy.
- **P5 › Pandora sequence** chains the P5 entries into one run. P5 practises the mechanics, not the DPS race: there are no threat swaps or mitigation budgets, and the terminal enrage is optional.
- NA strategies for Light Rampant, Ultimate Relativity, Apocalypse and Darklit Dragonsong are listed under [Upstream practice](#upstream-practice).

[fru-p1-fall-of-faith-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P1%20Fall%20of%20Faith%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P1%20Fall%20of%20Faith&strategy=Elemental%3A%20
[fru-p2-diamond-dust-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P2%20Diamond%20Dust%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P2%20Diamond%20Dust&strategy=Elemental%3A%20
[fru-p2-mirror-mirror-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P2%20Mirror%2C%20Mirror%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P2%20Mirror%2C%20Mirror&strategy=Elemental%3A%20
[fru-p2-light-rampant-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P2%20Light%20Rampant%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P2%20Light%20Rampant&strategy=Elemental%3A%20
[fru-p3-ultimate-relativity-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P3%20Ultimate%20Relativity%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P3%20Ultimate%20Relativity&strategy=Elemental%3A%20
[fru-p3-apocalypse-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P3%20Apocalypse%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P3%20Apocalypse&strategy=Elemental%3A%20
[fru-p4-darklit-dragonsong-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P4%20Darklit%20Dragonsong%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P4%20Darklit%20Dragonsong&strategy=Elemental%3A%20
[fru-p4-crystallize-time-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P4%20Crystallize%20Time%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P4%20Crystallize%20Time&strategy=Elemental%3A%20
[fru-p5-fulgent-blade-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P5%20Fulgent%20Blade%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P5%20Fulgent%20Blade&strategy=Elemental%3A%20
[fru-p5-paradise-regained-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P5%20Paradise%20Regained%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P5%20Paradise%20Regained&strategy=Elemental%3A%20
[fru-p5-polarizing-strikes-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P5%20Polarizing%20Strikes%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P5%20Polarizing%20Strikes&strategy=Elemental%3A%20
[fru-p5-pandora-s-box-enrage-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20FRU%20P5%20Pandora%27s%20Box%20%28enrage%29%20%28Elemental%29&duty=Futures%20Rewritten%20%28Ultimate%29&phases=P5%20Pandora%27s%20Box%20%28enrage%29&strategy=Elemental%3A%20

## The Omega Protocol (Ultimate)

Elemental follows Tuufless's translation of the Elemental guide. P1–P4 are native beta scenarios, built from retail combat logs, and all of them support multiplayer.

| Phase | Mechanic | Elemental | Mana | Menu entry |
| --- | --- | --- | --- | --- |
| **P1 Omega** | Program Loop | ✅ Verified | [⬜ Not implemented][top-p1-program-loop-m] | P1 › Program Loop |
|  | Pantokrator | ✅ Verified | [⬜ Not implemented][top-p1-pantokrator-m] | P1 › Pantokrator |
| **P2 Omega-M/F** | Party Synergy | ✅ Verified | [⬜ Not implemented][top-p2-party-synergy-m] | P2 › Party Synergy |
|  | Limitless Synergy | ✅ Verified | [⬜ Not implemented][top-p2-limitless-synergy-m] | P2 › Limitless Synergy |
| **P3 Omega Reconfigured** | Transition | ✅ Verified | [⬜ Not implemented][top-p3-transition-m] | P3 › Transition |
|  | Hello, World | ✅ Verified | [⬜ Not implemented][top-p3-hello-world-m] | P3 › Hello World |
|  | Oversampled Wave Cannon | ✅ Verified | [⬜ Not implemented][top-p3-oversampled-wave-cannon-m] | P3 › Oversampled Wave Cannon |
| **P4 Blue Screen** | Blue Screen | ✅ Verified | [⬜ Not implemented][top-p4-blue-screen-m] | P4 › Blue Screen |
| **P5 Run: Dynamis** | Delta | [⬜ Not implemented][top-p5-delta-e] | [⬜ Not implemented][top-p5-delta-m] | — |
|  | Sigma | [⬜ Not implemented][top-p5-sigma-e] | [⬜ Not implemented][top-p5-sigma-m] | — |
|  | Omega | [⬜ Not implemented][top-p5-omega-e] | [⬜ Not implemented][top-p5-omega-m] | — |
| **P6 Alpha Omega** | Cosmo Memory | [⬜ Not implemented][top-p6-cosmo-memory-e] | [⬜ Not implemented][top-p6-cosmo-memory-m] | — |
|  | Cosmo Arrow and Cosmo Dive | [⬜ Not implemented][top-p6-cosmo-arrow-and-cosmo-dive-e] | [⬜ Not implemented][top-p6-cosmo-arrow-and-cosmo-dive-m] | — |
|  | Unlimited Wave Cannon and Wave Cannon | [⬜ Not implemented][top-p6-unlimited-wave-cannon-and-wave-cannon-e] | [⬜ Not implemented][top-p6-unlimited-wave-cannon-and-wave-cannon-m] | — |
|  | Cosmo Meteor | [⬜ Not implemented][top-p6-cosmo-meteor-e] | [⬜ Not implemented][top-p6-cosmo-meteor-m] | — |
|  | Magic Number | [⬜ Not implemented][top-p6-magic-number-e] | [⬜ Not implemented][top-p6-magic-number-m] | — |

- **Program Loop** and **Pantokrator** let you choose your number; Pantokrator can also force which pairs adjust sides.
- **Blue Screen** can force who gets each wave's stack marker. It ends when Omega casts Blue Screen; the DPS check itself is not simulated.
- Upstream practice for P2 Party Synergy, P5 Delta, Sigma and Omega, and P6 Wave Cannon 2 is listed under [Upstream practice](#upstream-practice).

[top-p1-program-loop-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P1%20Program%20Loop%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P1%20Program%20Loop&strategy=Mana%3A%20
[top-p1-pantokrator-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P1%20Pantokrator%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P1%20Pantokrator&strategy=Mana%3A%20
[top-p2-party-synergy-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P2%20Party%20Synergy%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P2%20Party%20Synergy&strategy=Mana%3A%20
[top-p2-limitless-synergy-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P2%20Limitless%20Synergy%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P2%20Limitless%20Synergy&strategy=Mana%3A%20
[top-p3-transition-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P3%20Transition%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P3%20Transition&strategy=Mana%3A%20
[top-p3-hello-world-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P3%20Hello%2C%20World%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P3%20Hello%2C%20World&strategy=Mana%3A%20
[top-p3-oversampled-wave-cannon-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P3%20Oversampled%20Wave%20Cannon%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P3%20Oversampled%20Wave%20Cannon&strategy=Mana%3A%20
[top-p4-blue-screen-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P4%20Blue%20Screen%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P4%20Blue%20Screen&strategy=Mana%3A%20
[top-p5-delta-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P5%20Delta%20%28Elemental%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P5%20Delta&strategy=Elemental%3A%20
[top-p5-delta-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P5%20Delta%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P5%20Delta&strategy=Mana%3A%20
[top-p5-sigma-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P5%20Sigma%20%28Elemental%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P5%20Sigma&strategy=Elemental%3A%20
[top-p5-sigma-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P5%20Sigma%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P5%20Sigma&strategy=Mana%3A%20
[top-p5-omega-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P5%20Omega%20%28Elemental%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P5%20Omega&strategy=Elemental%3A%20
[top-p5-omega-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P5%20Omega%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P5%20Omega&strategy=Mana%3A%20
[top-p6-cosmo-memory-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Cosmo%20Memory%20%28Elemental%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Cosmo%20Memory&strategy=Elemental%3A%20
[top-p6-cosmo-memory-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Cosmo%20Memory%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Cosmo%20Memory&strategy=Mana%3A%20
[top-p6-cosmo-arrow-and-cosmo-dive-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Cosmo%20Arrow%20and%20Cosmo%20Dive%20%28Elemental%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Cosmo%20Arrow%20and%20Cosmo%20Dive&strategy=Elemental%3A%20
[top-p6-cosmo-arrow-and-cosmo-dive-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Cosmo%20Arrow%20and%20Cosmo%20Dive%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Cosmo%20Arrow%20and%20Cosmo%20Dive&strategy=Mana%3A%20
[top-p6-unlimited-wave-cannon-and-wave-cannon-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Unlimited%20Wave%20Cannon%20and%20Wave%20Cannon%20%28Elemental%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Unlimited%20Wave%20Cannon%20and%20Wave%20Cannon&strategy=Elemental%3A%20
[top-p6-unlimited-wave-cannon-and-wave-cannon-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Unlimited%20Wave%20Cannon%20and%20Wave%20Cannon%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Unlimited%20Wave%20Cannon%20and%20Wave%20Cannon&strategy=Mana%3A%20
[top-p6-cosmo-meteor-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Cosmo%20Meteor%20%28Elemental%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Cosmo%20Meteor&strategy=Elemental%3A%20
[top-p6-cosmo-meteor-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Cosmo%20Meteor%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Cosmo%20Meteor&strategy=Mana%3A%20
[top-p6-magic-number-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Magic%20Number%20%28Elemental%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Magic%20Number&strategy=Elemental%3A%20
[top-p6-magic-number-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TOP%20P6%20Magic%20Number%20%28Mana%29&duty=The%20Omega%20Protocol%20%28Ultimate%29&phases=P6%20Magic%20Number&strategy=Mana%3A%20

## Dragonsong's Reprise (Ultimate)

Elemental follows Tuufless's translation of the Elemental guide ("Easthogg" in P3).

| Phase | Mechanic | Elemental | Mana | Menu entry |
| --- | --- | --- | --- | --- |
| **P1 The Holy See** | Hyperdimensional Slash | ✅ Verified | [⬜ Not implemented][dsr-p1-hyperdimensional-slash-m] | P1 › The Holy See |
|  | Playstation | ✅ Verified | [⬜ Not implemented][dsr-p1-playstation-m] | P1 › The Holy See |
|  | Pure of Heart | ✅ Verified | [⬜ Not implemented][dsr-p1-pure-of-heart-m] | P1 › The Holy See |
| **P2 King Thordan** | Strength of the Ward, part 1 | ✅ Verified | [⬜ Not implemented][dsr-p2-strength-of-the-ward-part-1-m] | P2 › Strength of the Ward 1 |
|  | Strength of the Ward, part 2 | ✅ Verified | [⬜ Not implemented][dsr-p2-strength-of-the-ward-part-2-m] | P2 › Strength of the Ward 2 |
|  | Sanctity of the Ward, part 1: Sacred Severs | ✅ Verified | [⬜ Not implemented][dsr-p2-sanctity-of-the-ward-part-1-sacred-severs-m] | P2 › Sanctity of the Ward 1 |
|  | Sanctity of the Ward, part 2: Meteors | ✅ Verified | [⬜ Not implemented][dsr-p2-sanctity-of-the-ward-part-2-meteors-m] | P2 › Sanctity of the Ward 2 |
| **P3 Nidhogg** | Dive from Grace | 🟢 Playable | [⬜ Not implemented][dsr-p3-dive-from-grace-m] | P3 › Dive from Grace |
|  | Geirskogul towers | 🟢 Playable | [⬜ Not implemented][dsr-p3-geirskogul-towers-m] | P3 › Dive from Grace |
| **P4 Eyes** | Eyes | 🟢 Playable | [⬜ Not implemented][dsr-p4-eyes-m] | P4 › Eyes |
|  | Saving Haurchefant | 🟢 Playable | [⬜ Not implemented][dsr-p4-saving-haurchefant-m] | P4 › Eyes |
| **P5 Alternate Thordan** | Wrath of the Heavens | 🟢 Playable | [⬜ Not implemented][dsr-p5-wrath-of-the-heavens-m] | P5 › Wrath of the Heavens |
|  | Death of the Heavens | 🟢 Playable | [⬜ Not implemented][dsr-p5-death-of-the-heavens-m] | P5 › Death of the Heavens |
| **P6 Double Dragons** | Wyrmsbreath 1 | 🟢 Playable | [⬜ Not implemented][dsr-p6-wyrmsbreath-1-m] | P6 › Wyrmsbreath 1 to Wroth Flames |
|  | Akh Afah and Hallowed Wings 1 | 🟢 Playable | [⬜ Not implemented][dsr-p6-akh-afah-and-hallowed-wings-1-m] | P6 › Wyrmsbreath 1 to Wroth Flames |
|  | Wroth Flames | 🟢 Playable | [⬜ Not implemented][dsr-p6-wroth-flames-m] | P6 › Wyrmsbreath 1 to Wroth Flames |
|  | Akh Afah and Hallowed Wings 2 | 🟢 Playable | [⬜ Not implemented][dsr-p6-akh-afah-and-hallowed-wings-2-m] | P6 › Hallowed Wings 2 to Cauterize |
|  | Wyrmsbreath 2 | 🟢 Playable | [⬜ Not implemented][dsr-p6-wyrmsbreath-2-m] | P6 › Hallowed Wings 2 to Cauterize |
|  | Cauterize | 🟢 Playable | [⬜ Not implemented][dsr-p6-cauterize-m] | P6 › Hallowed Wings 2 to Cauterize |
| **P7 Dragon-king Thordan** | Exaflare's Edge | 🟢 Playable | [⬜ Not implemented][dsr-p7-exaflare-s-edge-m] | P7 › Dragon-king Thordan |
|  | Akh Morn's Edge | 🟢 Playable | [⬜ Not implemented][dsr-p7-akh-morn-s-edge-m] | P7 › Dragon-king Thordan |
|  | Gigaflare's Edge | 🟢 Playable | [⬜ Not implemented][dsr-p7-gigaflare-s-edge-m] | P7 › Dragon-king Thordan |
|  | Morn Afah's Edge | 🟢 Playable | [⬜ Not implemented][dsr-p7-morn-afah-s-edge-m] | P7 › Dragon-king Thordan |

- **P3–P7** are complete and playtested, but no recording of the real duty covers them yet.
- **P4 › Eyes** continues into the Haurchefant rescue.
- **P5** uses the conventional MT → ST swap for Heavenly Heel and Ascalon's Might; a human ST must Provoke when prompted.

[dsr-p1-hyperdimensional-slash-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P1%20Hyperdimensional%20Slash%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P1%20Hyperdimensional%20Slash&strategy=Mana%3A%20
[dsr-p1-playstation-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P1%20Playstation%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P1%20Playstation&strategy=Mana%3A%20
[dsr-p1-pure-of-heart-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P1%20Pure%20of%20Heart%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P1%20Pure%20of%20Heart&strategy=Mana%3A%20
[dsr-p2-strength-of-the-ward-part-1-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P2%20Strength%20of%20the%20Ward%2C%20part%201%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P2%20Strength%20of%20the%20Ward%2C%20part%201&strategy=Mana%3A%20
[dsr-p2-strength-of-the-ward-part-2-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P2%20Strength%20of%20the%20Ward%2C%20part%202%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P2%20Strength%20of%20the%20Ward%2C%20part%202&strategy=Mana%3A%20
[dsr-p2-sanctity-of-the-ward-part-1-sacred-severs-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P2%20Sanctity%20of%20the%20Ward%2C%20part%201%3A%20Sacred%20Severs%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P2%20Sanctity%20of%20the%20Ward%2C%20part%201%3A%20Sacred%20Severs&strategy=Mana%3A%20
[dsr-p2-sanctity-of-the-ward-part-2-meteors-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P2%20Sanctity%20of%20the%20Ward%2C%20part%202%3A%20Meteors%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P2%20Sanctity%20of%20the%20Ward%2C%20part%202%3A%20Meteors&strategy=Mana%3A%20
[dsr-p3-dive-from-grace-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P3%20Dive%20from%20Grace%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P3%20Dive%20from%20Grace&strategy=Mana%3A%20
[dsr-p3-geirskogul-towers-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P3%20Geirskogul%20towers%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P3%20Geirskogul%20towers&strategy=Mana%3A%20
[dsr-p4-eyes-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P4%20Eyes%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P4%20Eyes&strategy=Mana%3A%20
[dsr-p4-saving-haurchefant-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P4%20Saving%20Haurchefant%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P4%20Saving%20Haurchefant&strategy=Mana%3A%20
[dsr-p5-wrath-of-the-heavens-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P5%20Wrath%20of%20the%20Heavens%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P5%20Wrath%20of%20the%20Heavens&strategy=Mana%3A%20
[dsr-p5-death-of-the-heavens-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P5%20Death%20of%20the%20Heavens%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P5%20Death%20of%20the%20Heavens&strategy=Mana%3A%20
[dsr-p6-wyrmsbreath-1-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P6%20Wyrmsbreath%201%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P6%20Wyrmsbreath%201&strategy=Mana%3A%20
[dsr-p6-akh-afah-and-hallowed-wings-1-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P6%20Akh%20Afah%20and%20Hallowed%20Wings%201%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P6%20Akh%20Afah%20and%20Hallowed%20Wings%201&strategy=Mana%3A%20
[dsr-p6-wroth-flames-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P6%20Wroth%20Flames%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P6%20Wroth%20Flames&strategy=Mana%3A%20
[dsr-p6-akh-afah-and-hallowed-wings-2-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P6%20Akh%20Afah%20and%20Hallowed%20Wings%202%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P6%20Akh%20Afah%20and%20Hallowed%20Wings%202&strategy=Mana%3A%20
[dsr-p6-wyrmsbreath-2-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P6%20Wyrmsbreath%202%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P6%20Wyrmsbreath%202&strategy=Mana%3A%20
[dsr-p6-cauterize-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P6%20Cauterize%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P6%20Cauterize&strategy=Mana%3A%20
[dsr-p7-exaflare-s-edge-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P7%20Exaflare%27s%20Edge%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P7%20Exaflare%27s%20Edge&strategy=Mana%3A%20
[dsr-p7-akh-morn-s-edge-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P7%20Akh%20Morn%27s%20Edge%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P7%20Akh%20Morn%27s%20Edge&strategy=Mana%3A%20
[dsr-p7-gigaflare-s-edge-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P7%20Gigaflare%27s%20Edge%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P7%20Gigaflare%27s%20Edge&strategy=Mana%3A%20
[dsr-p7-morn-afah-s-edge-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20DSR%20P7%20Morn%20Afah%27s%20Edge%20%28Mana%29&duty=Dragonsong%27s%20Reprise%20%28Ultimate%29&phases=P7%20Morn%20Afah%27s%20Edge&strategy=Mana%3A%20

## The Epic of Alexander (Ultimate)

Mana follows the Japanese Mana guide and macro (Fuccdayo, Nukemaru). Each entry runs a whole phase, built from retail combat logs and Duty Recorder captures of full clears.

| Phase | Mechanic | Elemental | Mana | Menu entry |
| --- | --- | --- | --- | --- |
| **P1 Living Liquid** | Jagd Dolls | [⬜ Not implemented][tea-p1-jagd-dolls-e] | ✅ Verified | P1 › Living Liquid |
|  | Protean Waves | [⬜ Not implemented][tea-p1-protean-waves-e] | ✅ Verified | P1 › Living Liquid |
| **Intermission 1** | Limit Cut | [⬜ Not implemented][tea-intermission-1-limit-cut-e] | ✅ Verified | I1 › Limit Cut |
| **P2 Brute Justice and Cruise Chaser** | Nisi, water and lightning | [⬜ Not implemented][tea-p2-nisi-water-and-lightning-e] | ✅ Verified | P2 › BJ/CC |
| **Intermission 2** | Temporal Stasis | [⬜ Not implemented][tea-intermission-2-temporal-stasis-e] | ✅ Verified | I2 › Temporal Stasis |
| **P3 Alexander Prime** | Inception Formation | [⬜ Not implemented][tea-p3-inception-formation-e] | ✅ Verified | P3 › Alexander Prime |
|  | Wormhole Formation | [⬜ Not implemented][tea-p3-wormhole-formation-e] | ✅ Verified | P3 › Alexander Prime |
|  | J Waves | [⬜ Not implemented][tea-p3-j-waves-e] | ✅ Verified | P3 › Alexander Prime |
| **P4 Perfect Alexander** | Forced March (Final Word) | [⬜ Not implemented][tea-p4-forced-march-final-word-e] | ✅ Verified | P4 › Perfect Alexander |
|  | Fate Calibration α | [⬜ Not implemented][tea-p4-fate-calibration-alpha-e] | ✅ Verified | P4 › Perfect Alexander |
|  | Fate Calibration β | [⬜ Not implemented][tea-p4-fate-calibration-beta-e] | ✅ Verified | P4 › Perfect Alexander |
|  | Exatrines | [⬜ Not implemented][tea-p4-exatrines-e] | ✅ Verified | P4 › Perfect Alexander |
|  | Temporal Prison | [⬜ Not implemented][tea-p4-temporal-prison-e] | ✅ Verified | P4 › Perfect Alexander |

- Strategies: Doll Skip (P1), 1211 (Limit Cut), healer-first Nisi (P2), fixed positions (Temporal Stasis), 3/4 fixed (P3) and Nukemaru (P4).
- **Jagd Dolls** are skipped, as Doll Skip does.
- **Temporal Prison** uses a fixed practice order; the real order depends on your party's damage.

[tea-p1-jagd-dolls-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P1%20Jagd%20Dolls%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P1%20Jagd%20Dolls&strategy=Elemental%3A%20
[tea-p1-protean-waves-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P1%20Protean%20Waves%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P1%20Protean%20Waves&strategy=Elemental%3A%20
[tea-intermission-1-limit-cut-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20Intermission%201%20Limit%20Cut%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=Intermission%201%20Limit%20Cut&strategy=Elemental%3A%20
[tea-p2-nisi-water-and-lightning-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P2%20Nisi%2C%20water%20and%20lightning%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P2%20Nisi%2C%20water%20and%20lightning&strategy=Elemental%3A%20
[tea-intermission-2-temporal-stasis-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20Intermission%202%20Temporal%20Stasis%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=Intermission%202%20Temporal%20Stasis&strategy=Elemental%3A%20
[tea-p3-inception-formation-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P3%20Inception%20Formation%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P3%20Inception%20Formation&strategy=Elemental%3A%20
[tea-p3-wormhole-formation-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P3%20Wormhole%20Formation%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P3%20Wormhole%20Formation&strategy=Elemental%3A%20
[tea-p3-j-waves-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P3%20J%20Waves%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P3%20J%20Waves&strategy=Elemental%3A%20
[tea-p4-forced-march-final-word-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P4%20Forced%20March%20%28Final%20Word%29%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P4%20Forced%20March%20%28Final%20Word%29&strategy=Elemental%3A%20
[tea-p4-fate-calibration-alpha-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P4%20Fate%20Calibration%20%CE%B1%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P4%20Fate%20Calibration%20%CE%B1&strategy=Elemental%3A%20
[tea-p4-fate-calibration-beta-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P4%20Fate%20Calibration%20%CE%B2%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P4%20Fate%20Calibration%20%CE%B2&strategy=Elemental%3A%20
[tea-p4-exatrines-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P4%20Exatrines%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P4%20Exatrines&strategy=Elemental%3A%20
[tea-p4-temporal-prison-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20TEA%20P4%20Temporal%20Prison%20%28Elemental%29&duty=The%20Epic%20of%20Alexander%20%28Ultimate%29&phases=P4%20Temporal%20Prison&strategy=Elemental%3A%20

## The Weapon's Refrain (Ultimate)

Nothing native yet. Upstream offers NAUR Ultimate Predation and Ultimate Suppression.

| Phase | Mechanic | Elemental | Mana | Menu entry |
| --- | --- | --- | --- | --- |
| **P1 Garuda** | Spiny Plume | [⬜ Not implemented][uwu-p1-spiny-plume-e] | [⬜ Not implemented][uwu-p1-spiny-plume-m] | — |
|  | Friction | [⬜ Not implemented][uwu-p1-friction-e] | [⬜ Not implemented][uwu-p1-friction-m] | — |
|  | Suparna and Chirada | [⬜ Not implemented][uwu-p1-suparna-and-chirada-e] | [⬜ Not implemented][uwu-p1-suparna-and-chirada-m] | — |
| **P2 Ifrit** | Infernal Nails | [⬜ Not implemented][uwu-p2-infernal-nails-e] | [⬜ Not implemented][uwu-p2-infernal-nails-m] | — |
|  | Crimson Cyclone | [⬜ Not implemented][uwu-p2-crimson-cyclone-e] | [⬜ Not implemented][uwu-p2-crimson-cyclone-m] | — |
| **P3 Titan** | Gaols | [⬜ Not implemented][uwu-p3-gaols-e] | [⬜ Not implemented][uwu-p3-gaols-m] | — |
|  | Landslides | [⬜ Not implemented][uwu-p3-landslides-e] | [⬜ Not implemented][uwu-p3-landslides-m] | — |
| **P4 The Ultima Weapon** | Ultimate Predation | [⬜ Not implemented][uwu-p4-ultimate-predation-e] | [⬜ Not implemented][uwu-p4-ultimate-predation-m] | — |
|  | Ultimate Annihilation | [⬜ Not implemented][uwu-p4-ultimate-annihilation-e] | [⬜ Not implemented][uwu-p4-ultimate-annihilation-m] | — |
|  | Ultimate Suppression | [⬜ Not implemented][uwu-p4-ultimate-suppression-e] | [⬜ Not implemented][uwu-p4-ultimate-suppression-m] | — |
|  | Primal Roulette | [⬜ Not implemented][uwu-p4-primal-roulette-e] | [⬜ Not implemented][uwu-p4-primal-roulette-m] | — |

[uwu-p1-spiny-plume-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P1%20Spiny%20Plume%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P1%20Spiny%20Plume&strategy=Elemental%3A%20
[uwu-p1-spiny-plume-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P1%20Spiny%20Plume%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P1%20Spiny%20Plume&strategy=Mana%3A%20
[uwu-p1-friction-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P1%20Friction%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P1%20Friction&strategy=Elemental%3A%20
[uwu-p1-friction-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P1%20Friction%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P1%20Friction&strategy=Mana%3A%20
[uwu-p1-suparna-and-chirada-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P1%20Suparna%20and%20Chirada%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P1%20Suparna%20and%20Chirada&strategy=Elemental%3A%20
[uwu-p1-suparna-and-chirada-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P1%20Suparna%20and%20Chirada%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P1%20Suparna%20and%20Chirada&strategy=Mana%3A%20
[uwu-p2-infernal-nails-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P2%20Infernal%20Nails%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P2%20Infernal%20Nails&strategy=Elemental%3A%20
[uwu-p2-infernal-nails-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P2%20Infernal%20Nails%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P2%20Infernal%20Nails&strategy=Mana%3A%20
[uwu-p2-crimson-cyclone-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P2%20Crimson%20Cyclone%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P2%20Crimson%20Cyclone&strategy=Elemental%3A%20
[uwu-p2-crimson-cyclone-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P2%20Crimson%20Cyclone%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P2%20Crimson%20Cyclone&strategy=Mana%3A%20
[uwu-p3-gaols-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P3%20Gaols%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P3%20Gaols&strategy=Elemental%3A%20
[uwu-p3-gaols-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P3%20Gaols%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P3%20Gaols&strategy=Mana%3A%20
[uwu-p3-landslides-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P3%20Landslides%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P3%20Landslides&strategy=Elemental%3A%20
[uwu-p3-landslides-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P3%20Landslides%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P3%20Landslides&strategy=Mana%3A%20
[uwu-p4-ultimate-predation-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P4%20Ultimate%20Predation%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P4%20Ultimate%20Predation&strategy=Elemental%3A%20
[uwu-p4-ultimate-predation-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P4%20Ultimate%20Predation%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P4%20Ultimate%20Predation&strategy=Mana%3A%20
[uwu-p4-ultimate-annihilation-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P4%20Ultimate%20Annihilation%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P4%20Ultimate%20Annihilation&strategy=Elemental%3A%20
[uwu-p4-ultimate-annihilation-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P4%20Ultimate%20Annihilation%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P4%20Ultimate%20Annihilation&strategy=Mana%3A%20
[uwu-p4-ultimate-suppression-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P4%20Ultimate%20Suppression%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P4%20Ultimate%20Suppression&strategy=Elemental%3A%20
[uwu-p4-ultimate-suppression-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P4%20Ultimate%20Suppression%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P4%20Ultimate%20Suppression&strategy=Mana%3A%20
[uwu-p4-primal-roulette-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P4%20Primal%20Roulette%20%28Elemental%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P4%20Primal%20Roulette&strategy=Elemental%3A%20
[uwu-p4-primal-roulette-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UWU%20P4%20Primal%20Roulette%20%28Mana%29&duty=The%20Weapon%27s%20Refrain%20%28Ultimate%29&phases=P4%20Primal%20Roulette&strategy=Mana%3A%20

## The Unending Coil of Bahamut (Ultimate)

Nothing native yet. Upstream offers Golden Bahamut's Exaflares.

| Phase | Mechanic | Elemental | Mana | Menu entry |
| --- | --- | --- | --- | --- |
| **P1 Twintania** | Twisters and Fireball | [⬜ Not implemented][ucob-p1-twisters-and-fireball-e] | [⬜ Not implemented][ucob-p1-twisters-and-fireball-m] | — |
|  | Liquid Hell and Hatch | [⬜ Not implemented][ucob-p1-liquid-hell-and-hatch-e] | [⬜ Not implemented][ucob-p1-liquid-hell-and-hatch-m] | — |
|  | Heavensfall | [⬜ Not implemented][ucob-p1-heavensfall-e] | [⬜ Not implemented][ucob-p1-heavensfall-m] | — |
| **P2 Nael deus Darnus** | Quotes | [⬜ Not implemented][ucob-p2-quotes-e] | [⬜ Not implemented][ucob-p2-quotes-m] | — |
|  | Bahamut's Favor | [⬜ Not implemented][ucob-p2-bahamut-s-favor-e] | [⬜ Not implemented][ucob-p2-bahamut-s-favor-m] | — |
|  | Seventh Umbral Era | [⬜ Not implemented][ucob-p2-seventh-umbral-era-e] | [⬜ Not implemented][ucob-p2-seventh-umbral-era-m] | — |
| **P3 Bahamut Prime** | Quickmarch Trio | [⬜ Not implemented][ucob-p3-quickmarch-trio-e] | [⬜ Not implemented][ucob-p3-quickmarch-trio-m] | — |
|  | Blackfire Trio | [⬜ Not implemented][ucob-p3-blackfire-trio-e] | [⬜ Not implemented][ucob-p3-blackfire-trio-m] | — |
|  | Fellruin Trio | [⬜ Not implemented][ucob-p3-fellruin-trio-e] | [⬜ Not implemented][ucob-p3-fellruin-trio-m] | — |
|  | Heavensfall Trio | [⬜ Not implemented][ucob-p3-heavensfall-trio-e] | [⬜ Not implemented][ucob-p3-heavensfall-trio-m] | — |
|  | Tenstrike Trio | [⬜ Not implemented][ucob-p3-tenstrike-trio-e] | [⬜ Not implemented][ucob-p3-tenstrike-trio-m] | — |
|  | Grand Octet | [⬜ Not implemented][ucob-p3-grand-octet-e] | [⬜ Not implemented][ucob-p3-grand-octet-m] | — |
| **P4 Adds** | Twintania and Nael | [⬜ Not implemented][ucob-p4-twintania-and-nael-e] | [⬜ Not implemented][ucob-p4-twintania-and-nael-m] | — |
| **P5 Golden Bahamut** | Exaflares, Akh Morn and Morn Afah | [⬜ Not implemented][ucob-p5-exaflares-akh-morn-and-morn-afah-e] | [⬜ Not implemented][ucob-p5-exaflares-akh-morn-and-morn-afah-m] | — |

[ucob-p1-twisters-and-fireball-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P1%20Twisters%20and%20Fireball%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P1%20Twisters%20and%20Fireball&strategy=Elemental%3A%20
[ucob-p1-twisters-and-fireball-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P1%20Twisters%20and%20Fireball%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P1%20Twisters%20and%20Fireball&strategy=Mana%3A%20
[ucob-p1-liquid-hell-and-hatch-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P1%20Liquid%20Hell%20and%20Hatch%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P1%20Liquid%20Hell%20and%20Hatch&strategy=Elemental%3A%20
[ucob-p1-liquid-hell-and-hatch-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P1%20Liquid%20Hell%20and%20Hatch%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P1%20Liquid%20Hell%20and%20Hatch&strategy=Mana%3A%20
[ucob-p1-heavensfall-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P1%20Heavensfall%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P1%20Heavensfall&strategy=Elemental%3A%20
[ucob-p1-heavensfall-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P1%20Heavensfall%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P1%20Heavensfall&strategy=Mana%3A%20
[ucob-p2-quotes-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P2%20Quotes%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P2%20Quotes&strategy=Elemental%3A%20
[ucob-p2-quotes-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P2%20Quotes%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P2%20Quotes&strategy=Mana%3A%20
[ucob-p2-bahamut-s-favor-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P2%20Bahamut%27s%20Favor%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P2%20Bahamut%27s%20Favor&strategy=Elemental%3A%20
[ucob-p2-bahamut-s-favor-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P2%20Bahamut%27s%20Favor%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P2%20Bahamut%27s%20Favor&strategy=Mana%3A%20
[ucob-p2-seventh-umbral-era-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P2%20Seventh%20Umbral%20Era%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P2%20Seventh%20Umbral%20Era&strategy=Elemental%3A%20
[ucob-p2-seventh-umbral-era-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P2%20Seventh%20Umbral%20Era%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P2%20Seventh%20Umbral%20Era&strategy=Mana%3A%20
[ucob-p3-quickmarch-trio-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Quickmarch%20Trio%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Quickmarch%20Trio&strategy=Elemental%3A%20
[ucob-p3-quickmarch-trio-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Quickmarch%20Trio%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Quickmarch%20Trio&strategy=Mana%3A%20
[ucob-p3-blackfire-trio-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Blackfire%20Trio%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Blackfire%20Trio&strategy=Elemental%3A%20
[ucob-p3-blackfire-trio-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Blackfire%20Trio%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Blackfire%20Trio&strategy=Mana%3A%20
[ucob-p3-fellruin-trio-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Fellruin%20Trio%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Fellruin%20Trio&strategy=Elemental%3A%20
[ucob-p3-fellruin-trio-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Fellruin%20Trio%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Fellruin%20Trio&strategy=Mana%3A%20
[ucob-p3-heavensfall-trio-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Heavensfall%20Trio%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Heavensfall%20Trio&strategy=Elemental%3A%20
[ucob-p3-heavensfall-trio-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Heavensfall%20Trio%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Heavensfall%20Trio&strategy=Mana%3A%20
[ucob-p3-tenstrike-trio-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Tenstrike%20Trio%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Tenstrike%20Trio&strategy=Elemental%3A%20
[ucob-p3-tenstrike-trio-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Tenstrike%20Trio%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Tenstrike%20Trio&strategy=Mana%3A%20
[ucob-p3-grand-octet-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Grand%20Octet%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Grand%20Octet&strategy=Elemental%3A%20
[ucob-p3-grand-octet-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P3%20Grand%20Octet%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P3%20Grand%20Octet&strategy=Mana%3A%20
[ucob-p4-twintania-and-nael-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P4%20Twintania%20and%20Nael%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P4%20Twintania%20and%20Nael&strategy=Elemental%3A%20
[ucob-p4-twintania-and-nael-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P4%20Twintania%20and%20Nael%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P4%20Twintania%20and%20Nael&strategy=Mana%3A%20
[ucob-p5-exaflares-akh-morn-and-morn-afah-e]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P5%20Exaflares%2C%20Akh%20Morn%20and%20Morn%20Afah%20%28Elemental%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P5%20Exaflares%2C%20Akh%20Morn%20and%20Morn%20Afah&strategy=Elemental%3A%20
[ucob-p5-exaflares-akh-morn-and-morn-afah-m]: https://github.com/KathyAuteberry/FluffySim/issues/new?template=fight-data.yml&title=%5BFight%20data%5D%20UCOB%20P5%20Exaflares%2C%20Akh%20Morn%20and%20Morn%20Afah%20%28Mana%29&duty=The%20Unending%20Coil%20of%20Bahamut%20%28Ultimate%29&phases=P5%20Exaflares%2C%20Akh%20Morn%20and%20Morn%20Afah&strategy=Mana%3A%20

## Upstream practice

These entries come from [AnoMech](https://github.com/anomek/AnoMech) and its contributors. They appear under **Upstream** in the FluffySim window and mostly follow non-Japanese strategies. FluffySim keeps them working but doesn't check them against recordings of the real duty.

| Ultimate | Phase | Entry | Strategies |
| --- | --- | --- | --- |
| Dancing Mad | P1 | Tele-trouncing | Standard |
|  | P2 | Forsaken | Kroxy-Rinon 341, South Flex 341, LPDU Buddies, Yan, two older variants |
|  | P3 | Limit Cut | Standard |
|  | P3 | Black Hole | Three tether orders |
|  | P4 | Kefka Says | Two gaze layouts |
|  | P5 | Flood | Rotate |
|  | P5 | Celestriad | Standard |
|  | P5 | Exaflares | Spread |
|  | P5 | Forsaken Null | Visual only: no bots, no damage |
| Futures Rewritten | P2 | Light Rampant | NA conga |
|  | P3 | Ultimate Relativity | NA |
|  | P3 | Apocalypse | NA priority, static spreads, OT bait |
|  | P4 | Darklit Dragonsong | NA, optionally with you taking Somber Dance |
| The Omega Protocol | P2 | Party Synergy | NA PF, LPDU |
|  | P5 | Delta | Standard |
|  | P5 | Sigma | Standard |
|  | P5 | Omega | Standard |
|  | P6 | Exasquares / Wave Cannon 2 | Standard |
| The Weapon's Refrain | P4 | Ultimate Predation | NAUR |
|  | P4 | Ultimate Suppression | NAUR |
| The Unending Coil of Bahamut | P5 | Exaflares | Dodge |
