# Contributing to FluffySim

**Would you like to see more fights in FluffySim?** You can help, and you don't need to write a
line of code.

Every FluffySim mechanic is rebuilt from recordings of real pulls: cast timings, positions,
hitboxes and what the boss puts on screen all come from combat logs and replays. The more real
recordings there are, the more fights FluffySim can offer and the closer each one is to the game.

## What I'm accepting

| ✅ Welcome | ❌ Not right now |
| --- | --- |
| [Bug reports](#reporting-a-bug) | Pull requests, patches and other code contributions |
| [Fight data](#contributing-fight-data) from any ultimate played with a Japanese strategy | |

I'm not open to code-level contributions at the moment, and pull requests will be closed without
review. Please don't take it personally; bug reports and fight data are what help the most.

## How to reach me

- **[GitHub issue tracker](https://github.com/KathyAuteberry/FluffySim/issues/new/choose)**: pick
  *Bug report* or *Fight data*. The form asks for everything listed below.
- **Discord: `kathy.auteberry`**: send a direct message if you'd rather not post publicly. Use this
  for fight data whenever privacy matters: GitHub issues and their attachments are public, and logs
  contain the names of everyone in your party (see [Privacy](#privacy)).

## Reporting a bug

Every bug report **must** include:

1. **Full scenario name**, exactly as the menu shows it: encounter, phase and mechanic, plus the
   strategy if the scenario offers more than one. For example *Futures Rewritten › P3 › Ultimate
   Relativity (Mana)*.
2. **Role** you played: MT, OT, H1, H2, M1, M2, R1 or R2. If the role selector was on *Auto*,
   give the role your job ended up in.
3. **Seed** of the run. Open **Advanced / RNG Seed** in the FluffySim window and use **Copy** next
   to *Last seed*. Do this right after the bugged run, before starting another one: every start
   rolls a new seed. With the seed I can replay exactly the run you saw.
4. **What went wrong**: what happened, and what you expected to happen instead.

Also helpful, if you have them: the FluffySim version, solo or multiplayer, inn or title-screen
offline mode, bot difficulty, God Mode, *JESUS TAKE THE WHEEL*, and a screenshot or short video.

## Contributing fight data

### Which strategies

I accept fight data from **any ultimate**, as long as your party played a **Japanese strategy**:
one used on the Japanese data centres (Elemental, Gaia, Mana, Meteor). Where you played doesn't
matter: a party on any data centre running [Tuufless's](https://ffxiv.tuufless.com/) translation
of a Japanese strategy counts.

The data doesn't have to match what FluffySim has today. Recordings of a Japanese strategy that
FluffySim doesn't offer yet, or of an ultimate or phase it doesn't cover yet, are especially
welcome (see the [supported encounters](docs/user-guide/README.md#supported-encounters)). Clears
and wipes are both useful, and several pulls are better than one.

Only recordings from the **real duty** help. Recordings of FluffySim itself (made in an inn, in The
Roost or offline) show the simulator, not the game, so I can't use them.

### What to send

For the same pulls, I need **all three** recordings plus the strategy:

| | What | File |
| --- | --- | --- |
| 1 | [IINACT combat log](#1-iinact-combat-log) | `Network_<version>_<YYYYMMDD>.log` |
| 2 | [Duty Recorder recording](#2-duty-recorder-recording) | `*.dat` |
| 3 | [RaidTrace trace](#3-raidtrace-trace) | `<YYYYMMDD-HHMMSS>-duty<id>.rtrace` |
| 4 | Strategy | see below |

**Strategy:** if you played Tuufless's version, just write **Tuufless**. For anything else, give the
**full URL** of every document, spreadsheet, video or macro the party used. Mention any changes
your party made to it ("Mana, but MT and OT swap for the first tower").

All three tools only record while they're installed and running, so set them up **before** your
pulls. Each of them deletes or rotates old files after a while, so send the files soon after.

### 1. IINACT combat log

1. In game, type `/xlsettings` and open **Experimental → Custom Plugin Repositories**.
2. Add `https://raw.githubusercontent.com/marzent/IINACT/main/repo.json`, enable it and save.
3. Type `/xlplugins`, then find and install **IINACT**.
4. In IINACT's settings, make sure writing the log file is enabled and note the log file folder.
5. After your pulls, send the `Network_*.log` file for that day from the log file folder. A pull
   that crossed midnight is split across two files; send both.

A network log from ACT with the FFXIV plugin works just as well.

### 2. Duty Recorder recording

1. Type `/xlplugins`, then find and install **A Realm Recorded**. It's in the main Dalamud
   repository, so no custom repository is needed. It starts the game's Duty Recorder automatically
   in most instances, including ultimates.
2. Play your pulls as usual.
3. Open the game's **Duty Recorder** window (from the *Duty* menu). A Realm Recorded adds a folder
   button there that opens the replay folder. Send the `.dat` files from its `autorenamed`
   subfolder; they're named after the duty and the date.

On Windows the folder is `Documents\My Games\FINAL FANTASY XIV - A Realm Reborn\replay\autorenamed\`.
A Realm Recorded keeps the latest 30 recordings by default, and older ones are moved to
`replay\deleted\`.

### 3. RaidTrace trace

1. Type `/xlsettings` and open **Experimental → Custom Plugin Repositories**.
2. Add `https://releases.raidtrace.com/dalamud/repo.json`, enable it and save.
3. Type `/xlplugins`, then find and install **RaidTrace**.
4. In RaidTrace's settings, make sure recording in duties is on and ultimates are among the
   recorded categories. Note the trace folder.
5. After your pulls, send the `.rtrace` files from the trace folder.

Uploading to raidtrace.com is optional and up to you; I need the `.rtrace` files either way.

### Sending large files

GitHub accepts attachments up to 25 MB. Zip the files first (`.dat` and `.rtrace` files can't be
attached directly anyway, and logs compress very well); one ZIP with everything is fine. If the zip is still too large, upload it
to a file host and paste the link, or send it to me on Discord.

## Privacy

Combat logs, Duty Recorder recordings and RaidTrace traces contain the character names and IDs of
everyone in your party, and IINACT logs can contain chat. Anything attached to a GitHub issue is
**public**. If you or your party would rather keep that private, send the files to
`kathy.auteberry` on Discord instead.

I never publish raw logs or recordings. What ends up in FluffySim are timings, positions and
shapes extracted from them, with no player names.

Thank you for helping FluffySim grow!
