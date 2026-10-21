<p align="center"><img src="images/icon.png" alt="FluffySim logo" width="192" height="192" /></p>

# FluffySim — Final Fantasy XIV Ultimate Mechanics Simulator

Practice Ultimate raid mechanics inside FFXIV, without waiting for a party or progressing through the whole fight on every attempt. FluffySim is a Dalamud plugin that runs encounters locally, with seven simulated party members handling the other roles.

Choose a mechanic, pick your role and strategy, and retry as often as you like. FluffySim extends [AnoMech](https://github.com/anomek/AnoMech) with additional encounters, regional strategies and practice controls.

## What you can practice

- Individual mechanics with randomized assignments and seven bots.
- Easy or Hard bot movement, with the simulated party visible or hidden.
- Role selection and mechanic-specific assignment preferences where supported.
- God Mode, Pause/Resume and Restart for repeated practice.
- Local defensive actions and restored practice cooldowns between isolated attempts.
- Native bosses, casts, effects and arena environments.
- Entry from an inn, or title-screen offline practice with a configured local character.

## Installation

You need [XIVLauncher](https://goatcorp.github.io/) with Dalamud enabled. Install through the official FluffySim custom repository; **you do not need to download a ZIP or register a DLL**.

1. Launch FFXIV through XIVLauncher and wait for Dalamud to load.
2. Enter `/xlsettings` to open Dalamud Settings.
3. Open **Experimental → Custom Plugin Repositories**.
4. Add this repository URL:

   ```text
   https://raw.githubusercontent.com/KathyAuteberry/plogons/refs/heads/master/mostly-harmless.json
   ```

5. Ensure the repository is enabled, then save the settings.
6. Enter `/xlplugins` to open the Plugin Installer.
7. Find **FluffySim** and select **Install**. If it is disabled afterward, enable it in the installer's installed-plugin list.
8. Open the interface with `/fluffysim`.

Updates arrive through the same repository. **Do not load AnoMech and FluffySim simultaneously**, including old developer registrations. Migration and optional developer installation are covered in [INSTALLATION.md](INSTALLATION.md). Dalamud's [official help](https://goatcorp.github.io/faq/dalamud_troubleshooting.html) covers general installer problems.

## Quick start: from an inn

1. Enter an inn room with your character.
2. Open `/fluffysim`.
3. Choose an encounter and mechanic, your party role, strategy and practice options.
4. Start the simulation. The other seven party slots are controlled by bots.
5. Use **Pause/Resume** to stop and inspect the run, or **Restart** to try again.
6. When finished, use **Return to inn** (or **Leave**, where shown). Wait for the inn to return before resuming normal play or disabling the plugin.

Closing the interface alone does not leave the simulation.

## Supported encounters

The catalog contains **45 entries**, including **17 FRU entries** and **11 DSR entries**. Some FRU mechanics have separate regional implementations; those are separate catalog entries, rather than additional fight phases.

### Futures Rewritten (Ultimate)

The main FRU entries use **Mana** strategies. Additional registered implementations expose other supported strategies in their selectors.

| Phase | Playable entries |
| --- | --- |
| P1 | Fall of Faith |
| P2 | Diamond Dust; Mirror, Mirror; Light Rampant; additional upstream Light Rampant |
| P3 | Ultimate Relativity (Mana); Apocalypse; additional upstream Ultimate Relativity and Apocalypse |
| P4 | Darklit Dragonsong (Mana); additional upstream Darklit Dragonsong; Crystallize Time |
| P5 | Fulgent Blade; Paradise Regained; Polarizing Strikes; Pandora's Box; Pandora sequence |

The Pandora sequence connects its practice segments. It does not simulate a complete boss-HP/DPS race; terminal enrage resolution is optional.

### Dragonsong's Reprise (Ultimate)

DSR uses **Elemental** strategies, with these entries in encounter order:

1. P1 The Holy See
2. P2 Strength of the Ward 1
3. P2 Strength of the Ward 2
4. P2 Sanctity of the Ward 1
5. P2 Sanctity of the Ward 2
6. P3 Dive from Grace
7. P4 Eyes
8. P5 Wrath of the Heavens
9. P5 Death of the Heavens
10. P6 Wyrmsbreath 1 to Wroth Flames
11. P6 Hallowed Wings 2 to Cauterize

P4 includes the Haurchefant rescue continuation. P5 uses the conventional MT → ST swap for Heavenly Heel followed by Ascalon's Might; a human ST must use local Provoke when prompted. **P7 Dragonking Thordan is not implemented.**

### Other registered practice

- **The Omega Protocol:** Party Synergy, Delta, Sigma, Omega, Wave Cannon 2.
- **The Weapon's Refrain:** Ultimate Predation and Ultimate Suppression.
- **The Unending Coil of Bahamut:** Exaflares.
- **Dancing Mad:** Tele-trouncing, Forsaken, Limit Cut, Black Hole, Kefka Says, Flood, Celestriad, Exaflares and Forsaken Null. Forsaken Null is a limited visual entry without bot AI or damage resolution.

## Practice controls

Choose the role you want to learn; bots fill the remaining seats. Easy and Hard change bot behavior, rather than changing the encounter's strategy. Hiding the simulated party can help you focus on your own cues.

Assignments normally randomize each run. Some encounters offer preferences for a particular debuff, marker or responsibility. These constrain valid assignments; the available choices depend on the mechanic. Your preferences persist across retries.

**God Mode** helps you inspect mechanics without ordinary lethal failures ending your practice. **Pause/Resume** freezes simulation progression, statuses and movement/actions; native client animations may continue. **Restart** resets the attempt and restores supported local practice cooldowns. Cooldowns still matter within an attempt, and connected sequences can preserve cooldown planning between segments.

The local action model supports selected defensive and mechanic-relevant actions, including tank invulnerability, mitigation, anti-knockback and supported interrupts/LB practice. It does not reproduce every job action or interaction. Use the scenario's instructions and settings for its specific requirements.

## Title-screen offline practice

Offline mode loads a virtual inn without logging into a world. **Dalamud and FluffySim must already be loaded** for the title-screen **FluffySim offline mode** entry to appear. This is not a standalone application.

1. Load FluffySim in a normal session, then log out to the title screen. Alternatively, load it manually at title if your Dalamud setup offers that interface.
2. After logout, wait at least ten seconds before entering offline mode.
3. Select **FluffySim offline mode** and choose one of the character/gearset configurations saved locally on this computer.
4. Enter the character's name and **actual home world** if required. Local gearset files do not supply that identity information. Review the chosen loadout, appearance and warnings.
5. Select **Enter offline inn**, then choose and start a scenario as usual.
6. Leave the scenario first, then use offline mode's **Return to title** before logging in normally.

Offline mode temporarily uses local character settings and restores its session state on return. Back up character configuration before first use. Other plugins can react to the offline character or retain their own data; review the compatibility warning shown in the offline window. Discord Chat Bridge is temporarily unloaded and restored by the offline compatibility handling.

If something attempts to log in or connect while offline state is active, the safety response may close the game. Do not unload FluffySim or attempt normal login mid-session. See [offline setup and limitations](INSTALLATION.md#experimental-offline-mode) for more detail.

## Safety and limitations

FluffySim creates synthetic local game state. Its packet isolation prevents simulated encounter actions from being sent to the real FFXIV servers. While that isolation is active, some normal online events, including party changes and ready checks, may not appear until you leave.

Native actor, animation and effect behavior can differ from real encounter playback. Some damage and timing models remain approximations, and not every action or plugin combination is supported. Packet isolation is a protective mechanism, not a guarantee of risk-free use or universal compatibility.

Leave through the simulation controls before returning to normal gameplay. If the arena or actors fail to initialize, leave cleanly and retry from the inn or title screen; do not repeatedly reload the plugin inside a simulation.

## Troubleshooting

**FluffySim does not appear in the installer.** Check that the custom repository URL matches the installation block exactly, its enabled checkbox is checked, and settings were saved. Reopen `/xlplugins`, clear search filters and check installed plugins too. Game patches can temporarily affect Dalamud/plugin availability.

**The repository is missing or disabled.** Return to `/xlsettings` → Experimental → Custom Plugin Repositories, add or enable the URL and save. This is a custom repository, not the default Dalamud listing.

**The plugin will not start.** Confirm Dalamud has loaded and FluffySim is enabled in `/xlplugins`. Disable any other AnoMech/FluffySim copy or old developer DLL registration. Do not install multiple copies to work around an error.

**How do I open it?** Use `/fluffysim`. The legacy `/anomech` and `/ano` aliases are also supported.

**Why can't I enter here?** Online practice requires an inn room. Title-screen practice requires a loaded plugin and configured offline character. Follow the appropriate quick-start workflow instead of bypassing an entry refusal.

**Actors or scenery are missing.** Leave using the UI, wait for restoration, then retry. If it repeats, report the scenario, entry mode and exact steps. Avoid reloading while the simulated arena is active.

**How do I retry or stop?** Use Restart for another attempt, or Return to inn/Leave to finish. In offline mode, leave the scenario and then Return to title. Closing the window is not sufficient.

**Where do I report a bug?** Use [FluffySim GitHub Issues](https://github.com/KathyAuteberry/FluffySim/issues). Include the plugin version, scenario, role, strategy, bot difficulty, entry mode, error text and reproducible steps. Review logs/screenshots for personal information before sharing them. Discord membership is not required.

## Credits and license

FluffySim builds on **[AnoMech by Anomek and contributors](https://github.com/anomek/AnoMech)**. Thanks to WorstAquaPlayer, Wydox, RoarkGit, Evernow and the original contributors for the native simulation infrastructure, encounters and strategies; to [Evernow's fork](https://github.com/Evernow/AnoMech) for offline and consistency contributions; and to [AsuMech](https://github.com/Asunnya/AsuMech) for incorporated DSR work. FluffySim's contributors maintain the expanded practice suite and its own logo.

The inherited zone-loading work builds on Hyperborea. FFXIV-RaidsRewritten and BossMod inspired the original project. Incorporated source and dependencies retain their respective attribution and obligations: see [LICENSE.md](LICENSE.md), [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and [upstream integration notes](UPSTREAM-INTEGRATION.md).

FluffySim is licensed under **AGPL-3.0-or-later**. This is an independent community project, unaffiliated with Square Enix. FINAL FANTASY XIV and related game assets belong to their respective owners.

For contributors: [development instructions](AGENTS.md), [release workflow](RELEASING.md) and [changelog](CHANGELOG.md).
