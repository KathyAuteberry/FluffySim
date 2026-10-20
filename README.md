
# <img src="images/icon.png" alt="FluffySim" width="64" height="64" /> FluffySim

*An FFXIV mechanic simulator extending AnoMech with playable DSR and FRU Ultimate encounters.*

---

Simulate FFXIV raid mechanics client-side for solo practice. Go to any Inn, open the plugin with `/fluffysim` and start practicing!


Thanks to improvements by [WorstAquaPlayer](https://github.com/WorstAquaPlayer) plugin is quite stable now! No more
crashes after training session.

**WARNING!!!**

**You are cut off from server traffic while in the sim zone.** To keep the
fake zone stable, the plugin firewalls incoming packets from the server.
While simulating:
  * Players joining or leaving your party will not appear in the party list
  until you leave the sim zone.
  * Ready checks will not pop.


### Beta: Advanced Simulation Resolution

The simulator now includes a beta feature that properly resolves most skills, triggers, and gauges during simulation.
As this feature is still in beta, some edge cases and less common interactions may not yet resolve correctly.


## Installation

Install through the [custom repository](https://raw.githubusercontent.com/KathyAuteberry/plogons/master/mostly-harmless.json), or manually register the release ZIP. Follow [installation and migration instructions](INSTALLATION.md). Do not load AnoMech and FluffySim together.

## Added FRU and DSR practice scenarios

- FRU: P1 Fall of Faith, P2 Light Rampant, P3 Ultimate Relativity, P3 Apocalypse, P4 Darklit Dragonsong — Mana.
- DSR: P2 Strength of the Ward Parts 1 and 2, Sanctity of the Ward Parts 1 and 2 — Elemental.
- Sanctity Meteors remains experimental; some timings and presentation need further recording validation.
- Shared Easy/Hard bots, invisible simulated party, God Mode, and local-only practice defensive actions.
- Native defensive hotbar practice includes scoped local cooldown resets and exit restoration, pending client validation of the new integration. Low-level Meteors players can use `/fluffysim antikb`; see the [packet-filter audit and manual checks](PACKET-FILTER-AUDIT.md). Leave and wait for the inn before reloading the plugin.
- Solo Pause/Resume freezes the ongoing run and status durations while restricting player movement/actions. Native animations may continue; see [Pause details](PAUSE-INVESTIGATION.md).

FluffySim extends [AnoMech by Anomek and contributors](https://github.com/anomek/AnoMech). Original native simulation infrastructure and the scenarios listed below retain their upstream attribution. The existing icon is inherited from AnoMech; no new logo is implied.

Experimental title-screen offline mode and Evernow's firewall/recast consistency improvements are integrated; read [usage, limitations and attribution](UPSTREAM-INTEGRATION.md) before testing. Offline config requires your character name and actual home world; local gearset files do not contain that identity metadata. Manually disable Discord Chat Bridge before offline entry until it handles invalid authenticated-world references. See the [offline repair notes and client checklist](OFFLINE-DSR-REPAIR.md) for the remaining DSR scenery and party-UI validation.

See [Hard movement audit](HARD-DIFFICULTY-AUDIT.md), [release/versioning instructions](RELEASING.md), and [release notes](CHANGELOG.md).

## Inherited AnoMech scenarios:
- Dancing Mad (Ultimate)
    - P1 Tele-trouncing _by [Evernow](https://github.com/Evernow)_
    - P2 Forsaken
      - NA
        - [Kroxy-Rinon 341 (Center/N Stacks) melee adjust](https://raidplan.io/plan/UATE__aDcw1-bgVv)
        - [South Adjust 341](https://raidplan.io/plan/uq7zdjvuu7uuw8fj)
        - diamond markers or week one positions
      - EU _by [Wydox](https://github.com/Wydox)_
        - [\[LPDU\] Buddies](https://raidplan.io/plan/142oXOZpPc_jh3dd)
        - [\[Old\] p3Z Buddy Meow](https://raidplan.io/plan/lZWqxfxvyhF9sp3Z)
        - [\[Old\] zP6 South adjust](https://raidplan.io/plan/rtc1FcuZFMuyBzP6)
    - P3 Limit Cut _by [Evernow](https://github.com/Evernow)_
    - P3 Black Hole _old bh (single tethers, n/s stomps)_
      - D>S>A
      - S>D>A _by [Evernow](https://github.com/Evernow)_
    - P4 Kefka Says
      - kefkabin
      - kefkabin, gazes: supports N / DPS S _by [Evernow](https://github.com/Evernow)_
    - P5 Flood _by [Evernow](https://github.com/Evernow)_
    - P5 Exaflares _by [Wydox](https://github.com/Wydox)_
    - P5 Celestriad _by [RoarkGit](https://github.com/RoarkGit)_
    - P5 Forsaken Null _no ai or damage_
- The Omega Protocol (Ultimate): _NA pf strats_
    - P2 Party Synergy
    - P5 Delta
    - P5 Sigma
    - P5 Omega
    - P6 Exasquares / Wave Cannon 2
- The Weapon's Refrain (Ultimate) _by [WorstAquaPlayer](https://github.com/WorstAquaPlayer)_
    - Ultimate Predaction
    - Ultimate Suppression
- The Unending Coil of Bahamut (Ultimate) _by [RoarkGit](https://github.com/RoarkGit)_
    - Exaflares

## Details

* Spawns fake party members and boss NPCs into the live game client
* Drives their positions, cast bars, tethers, and VFX so mechanics play out visually
* Your fake party members are full fledged bots that will do mechanics.
  Some scanarios also have solo mode where you can practice without disctractions.


## Development builds

Use `dotnet build -c Debug` and `dotnet test -c Debug` for development. Keep output
in Debug-specific `bin` and `obj` trees (the solution plugin uses `bin/x64/Debug`). Do not build, copy, or publish into Release output:
Dalamud watches the installed Release files and hot-reloads them, freezing the
running client. The user builds Release personally when ready to playtest.

## How to help

FRU implementation coverage, verified encounter data and local Linux build/test
instructions are tracked in [the FRU roadmap](FRU-ROADMAP.md).

1. Please provide feedback and report any issues in scenarios: bad timing, damage, config not working at it supposed
2. Bot AI currently only covers strategies from my region. Adding strategies for other regions requires little coding.
   Feel free to create pull request or contact me.
3. Adding new scenario is more involved. `tools/parser.py` generates a baseline scenario from a log, which still
   needs randomization, mechanic-failure logic and bot AI added by hand.
4. Plugin-development or reverse-engineering help, and improvement ideas, are also welcome.


## Known issues
* Visuals should closely match retail, but damage snapshot timings are best guesses
* In scenarios for Top Omega Protocol (Ultimate):
  * Tether distance threshold are very rough estimations
* Not all skills will resolve properly

#  Acknowledgments

Thanks for contributors:
* [WorstAquaPlayer](https://github.com/WorstAquaPlayer) - rewriting core & fixing crashes, scenarios for uwu
* [Wydox](https://github.com/Wydox) - core improvements, extra scenarios and strategies
* [RoarkGit](https://github.com/RoarkGit) - win streaks, extra scenarios
* [Evernow](https://github.com/Evernow) - multiplayer (WIP), extra sceanrions and strategies

AnoMech leans heavily on the work of other Dalamud plugins. Huge thanks to their authors!
Without them, the following would not be possible:

AnoMech's zone changes are built on **Hyperborea**, which served as the baseline for loading a solo duty arena.                                                                                                                                                                                   
[Evernow](https://github.com/Evernow) has since improved that code and made it safer to use.                                                                                                                                                                                                      

**FFXIV-RaidsRewritten** and **bossmod** were the initial inspiration for the project, though neither is part of the                                                                                                                                                                              
codebase anymore.

## Maintainer workflows

Use Jujutsu for development and integration; see [AGENTS.md](AGENTS.md). Local builds are
strictly Debug-only. Version bumps and reviewed `vX.Y.Z.0` tags drive isolated CI releases
and update the separate plogons manifest. Setup, publication and failure recovery are in
[RELEASING.md](RELEASING.md). Original AnoMech attribution and upstream ancestry are preserved.
