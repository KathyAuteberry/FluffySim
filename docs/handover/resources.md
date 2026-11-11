# Resources

External sources used for FluffySim encounter work, what each informed, and how much to trust it.
Ordered by the evidence hierarchy in `CLAUDE.md`: in-game evidence first (see `workflow.md`),
then original sources, then cross-checks. Reconstructed from Codex's 2026-10-09 to 11-11
transcripts.

## 1. Strategy sources (assignments, positions, priorities)

| Source | Encounter / strategy | Informed |
|---|---|---|
| Tuufless Mana FRU: `https://ffxiv.tuufless.com/mana/fru/01_fatebreaker/` … `05_pandora/`, `/mana/fru/mitigation/`; mirror repo `~/Projekty/Tuufless/FFXIV-Elemental-raid-macros` (`_mana/fru/*.md`, images); `tuufless.github.io/FFXIV-Elemental-raid-macros/mana/fru/` | FRU, Mana | Every FRU scenario's assignments and positions. CT Wings mitigation plan. Paradise tower assignment (melee/ranged inversion fix). Fulgent Blade inner-corner dodges. Diagram images (`/images/ultimates/fru_mana/…`) checked visually |
| Tuufless Elemental DSR: `https://ffxiv.tuufless.com/elemental/dsr/01_the_holy_see/` … `07_dragonking_thordan/`, `/elemental/dsr/mitigation/` | DSR, Elemental ("Easthogg" P3) | Every DSR scenario, incl. Wroth L-dodge and automarkers (`/images/ultimates/dsr/06/wroth_flames_am_markers.jpg`), P7 |
| TEA Notion guide (Nukemaru/Mana, `ぬけまる/ヤ無視20/1211/ヒラ先/時間停止脳死/断絶34固定`), `https://app.notion.com/p/The-Epic-of-Alexander-Ultimate-TEA-226507c362a280e39a49c7406fcafe3c` | TEA, Mana | All TEA scenarios: Doll Skip, 1211 Limit Cut, healer-first third Nisi, fixed Temporal Stasis, 3/4 fixed Wormhole, P4. Programmatic retrieval failed; the browser-readable page was used |
| TEA macros: JP `https://jp.finalfantasyxiv.com/lodestone/character/34120564/blog/5274750/`, EN `https://na.finalfantasyxiv.com/lodestone/character/40730370/blog/5524788/` | TEA | Priority rules. The **JP version wins** when the translation is ambiguous |
| TEA videos: Fuccdayo P1 (`youtube.com/watch?v=mxy_ZYEYwlE`), Nukemaru (`AMdtMyvbPNs` P1/LC, `yuLjtpm2ti8` P2, `3wtfeXognIY` P3, `9YgDXyUWbiQ`); `fuucdayo.com/battle/3685/` | TEA | Positions from frames. JP auto-subtitles (yt-dlp) need corroboration. Re-downloadable |
| `https://note.com/s_kzw/n/n2783dab3f9cd` (JP) | DSR P7 | Gigaflare telegraph order (staggered markers) |
| Kitten's guide, `https://kitten-erp-guides.moe/ultimates/fru/p5/paradise_regained` | FRU P5 | Owner-supplied cross-check for the Paradise cleave ("half-moon cleave"; no angle stated) |
| icy-veins FRU P1/P5, naurffxiv FRU P1, materiaraiding FRU, consolegameswiki FRU/DSR | FRU/DSR | Early cross-checks only |
| **The Last Laugh** (owner's guide site) | — | **Cross-check only, never a source.** Codex did not use it |

## 2. Community encounter code (IDs, timings, geometry)

These are reference implementations, not retail measurements. When several of them agree, that
is still not independent evidence (they share lineage).

| Source | Informed |
|---|---|
| awgil/ffxiv_bossmod (`~/Projekty/awgil/ffxiv_bossmod`; `BossMod.Ultimate/Dawntrail/Ultimate/FRU/*`, `Endwalker/Ultimate/DSW1`, `DSW2/*`, `Components/Towers.cs`, `Framework/WorldStateGameSync.cs`, `Replay/*`, `Network/ServerIPC.cs`) | FRU/DSR action, status and OID IDs; state-machine timings; AoE shapes (e.g. Paradise 240° cone); BossMod replay format; ActorCast field names |
| FFXIV-CombatReborn/BossmodReborn (`~/Projekty/FFXIV-CombatReborn/BossmodReborn`; FRU modules, `DSW2/P7ExaflaresEdge.cs`, `Replay/Analysis/TEASpecific.cs`, `Replay/ReplayParserLog.cs`, `ReplayLogFormat.cs`) | Same, plus DSR P7 Exaflare, TEA analysis, reading the `Replaye` BMR files, Fulgent Blade targets (11 y/9 y inward) |
| quisquous/cactbot and OverlayPlugin/cactbot (`docs/LogGuide.md`, `dragonsongs_reprise_ultimate.ts`, `futures_rewritten_ultimate.ts`) | ACT log line semantics; head-marker and tether IDs |
| PunishXIV/Splatoon (`~/Projekty/PunishXIV/Splatoon`, presets and `SplatoonScripts/Duties/Dawntrail/The Futures Rewritten/*`) | Geometry and tower MapEffects for FRU P2/P5. The owner's local Fulgent Blade script edit lives there (jj change `unkuny`); FluffySim does not depend on it |
| WCGH/FRU-Sim (Godot; `~/Projekty/WCGH/FRU-Sim`, `scenes/p1–p4`, `light_ramp_player_pos_jp.gd`) | JP positions and sequences for FRU P1–P4. Its Ultimate Relativity assignment differs from the chosen strategy |
| WCGH/Dragonsong-Sim (Godot; local) | Licence only; not otherwise used |
| susy-bakaa/ffxiv-raid-sim (Unity, GPL; GitHub only, not cloned locally): `raidsim/Assets/FFXIV/Fights/DSR/Scenes/meteors.unity`, `FightTimeline.cs`, `BotTimeline.cs` | DSR Sanctity Meteors node geometry. Nothing copied (licence) |
| Asunnya/AsuMech (`~/Projekty/Asunnya/AsuMech`) | AnoMech-fork comparison (`docs/research/asumech-comparison.md`); DSR P1–P7 ports; pending-helper cast fix; hidden-DrawObject approach for DSR P3 dives. Its unverified values (P7 weather 45, Gnash/Lash 6 y) were not kept |
| anomek/AnoMech (`~/Projekty/anomek/AnoMech`, remote `upstream`) and Evernow/AnoMech (remote `evernow`) | Upstream base, offline mode, safety, multiplayer (default relay `sim.everscloset.com`), TEA foundation |
| paissaheavyindustries/Lemegeton (local) | Local soft-marker drawing approach (no runtime dependency) |
| kawaii/Hyperborea, MnFeN/Simulant, kotarou3/ffxiv-arena-images (GitHub) | Packet filter/firewall design and unload risk (Hyperborea issue #34); arena phase initialisation |

## 3. Client, Dalamud and data references

| Source | Informed |
|---|---|
| aers/FFXIVClientStructs | All native interop (layout, environment, weather, director, actions/recasts, draw objects, camera, HUD/party arrays, VFX, BGM, replay manager) |
| goatcorp/Dalamud, DalamudPackager | Plugin identity and config migration, Discord Bridge unload/reload via internals, packaging |
| UnknownX7/ARealmRecorded and Hypostasis (`FFXIVReplay.cs`, `ContentsReplayModule.cs`; ARR also local) | Duty Recorder `.dat` format → `tools/replay_inspector.py` |
| xivdev/file-formats; physis `envs.rs` (docs.rs) | ENVS/OBSB environment files (DSR P7 arena) |
| ff-meli and perchbirdd/OrchestrionPlugin (`xiv_bgm*.csv`) | BGM names: 230 Footsteps in the Snow, 802 The Extreme, 801 Promises to Keep, 800 The Legendary Beast, 921 Heroes (IDs confirmed in installed data) |
| xivapi/ffxiv-datamining @ `d9582a62…` | Sheets in headless tests |
| reiichi001/Dalamud.DiscordBridge, DelvUI, WigglyCorp/Questionable | Offline-mode compatibility (Bridge suspension, party list rendering) |
| NightmareXIV/ECommons | Angle conventions and ActionEffect parsing (Splatoon script) |
| Soreepeong/XivAlexander, UnknownX7/NoClippy | Latency simulation design (`docs/research/latency-simulation.md`) |
| FFLogs API v2 | Assessed and rejected as a log source: parsed events only, no raw ACT log, archived reports need subscriber auth |

## 4. In-game evidence used so far (owner's machine)

FRU: `Logy/Network_30301_20261007.log` (early FRU), `…_20261010/11`, `Network_30303_20261014` (P1),
`Network_30304_20261024` (native audit), `…_20261030/31` (clears) with the matching
`ffxivConfig/replay/autorenamed` `.dat`s; `Futures Rewritten (Ultimate) 2026.10.07 23.02.28.dat`
(P2/P3 arena inputs). DSR: `Network_30301_20261010`, `Network_30303_20261016` (Meteors),
`Network_30304_20261024`; the 2024 DSR replay zip in the old launcher folder (Spiral Thrust casts).
TEA: the owner's friend's package (`~/Stažené/Logs/Network_30304_20261108.log`, two `.dat`s in
`~/Stažené/Duty Recording/`). Snapshots in `pluginConfigs/{AnoMech,FluffySim}/arena-snapshots/`. The
exact files behind each committed value are cited in the encounter docs (`docs/encounters/*`,
`docs/research/*`, `docs/tea/EVIDENCE.md`).
