# DSR native playtest repairs — 2026-10-20

Owner playtest reported invisible P1/P5 bosses, naked P4 NPCs, absent P1 knockback and near-instant P6 puddle deaths. Repairs are saved in `qvoykv`, integrated into `xwtnow`. Native rendering is **not yet owner-confirmed**.

## Menu and identity

DisplayNameOverride only; Name/Phase/FullName saved keys unchanged. Catalog and UI phase traversal now agree:

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

Regions and strategies remain in the strategy selectors. No configuration migration or new scenarios.

## Shared appearance defect

New P1/P5 spawners bypassed the working P2 `DsrActors.Config` appearance data. ModelChara Type2 demihuman bodies/weapons come from NpcEquip, not ModelChara alone. Native actor allocation could succeed with no usable body. Both P5 scenarios now share Dark Thordan12611/model3269/NpcEquip2159 body131073 (variant2), weapon4297590237, matching installed sheets; P2 Thordan12604 uses variant1. P1 Adelphel12601 and Grinnaux12602 use the existing working P2 knight equipment/model configuration. Spawn position, targetability, faction, visibility schedule and timelines are preserved.

P4 had a related missing-data problem on Type1 humanoids: all story NPC spawns omitted both BNpcCustomize and NpcEquip. Owner identified the center naked actor (Estinien) and multiple NPCs near the party (Ysayle/Haurchefant). Alphinaud was also affected. Naming an actor never supplied its appearance. Exact installed sheet payloads now initialize all four:

| Actor | BNpcBase | ModelChara | BNpcCustomize | NpcEquip |
| --- | --- | --- | --- | --- |
| Estinien |13119|3291|1046|2244|
| Alphinaud |13769|3499|516|964|
| Haurchefant |13117|3289|327|2165|
| Ysayle |13118|3290|1045|2166|

Each has all26 customization bytes and ten equipment slots, plus weapons; Alphinaud retains the native child model/body type. Installed skeletons c0501/c0601/c0504 exist. Exact body meshes exist for Estinien/Ysayle/Alphinaud. Haurchefant's e0015 body has c0101/c0301 shared meshes rather than a c0501-specific mesh; no invented model substitution was made. Native racial fallback/rendering still needs playtesting. No game binaries or raw sheets are committed.

`DsrActors.WithNativeAppearance` applies only appearance/model fields, preserving the caller's phase placement and behavior. It is used by P1, both P5 scenarios, Eyes story NPCs and the P4 rescue actors. The rescue's Charibert/Zephirin/Haurchefant receive the same correct data. Appearance is written into the spawn packet before the client allocates/draws the actor; no later equipment mutation, arbitrary delay or force-visible workaround.

The shared readiness contract now distinguishes registered object, DrawObject, loaded model-slot mask and visible drawing. Meshless helpers remain exempt. Model readiness is forwarded through recording/peer proxies. Fake tests explicitly toggle these states; this is an initialization contract, **not native rendering proof**.

## P1 physical knockback and chains

Old P1 used the row169 ActionEffect path. For the human, PlayerMotion relied entirely on the client applying that synthetic effect; no explicit local shove was emitted. The exact reason the live client ignored that entry has not been captured. P1 now uses the existing local ActorControlSelf0xE1 push path, with equivalent bot forced movement, from Grinnaux:16y at20y/s. The intended55.83/81.98 snapshots remain unchanged. Arm's Length1209 and Surecast160 suppress the shove; first-knockback bot resistance stays intact. No global knockback implementation or FRU mechanic changed.

Chains previously compared absolute separation to30y. Each pair now snapshots its actual creation distance and breaks only beyond that baseline+30y. Existing outward bot movement is retained. No automatic chain break on cast. Tests demonstrate actual16y human displacement, bot displacement, unbroken chains with anti-knockback and with incorrectly aligned pairs despite a real shove. The30y additional distance remains an inherited practice value; native calibration is pending.

## P6 puddle exposure

The defect was a per-frame Die call inside each6y puddle after1.1s, with no bleed cadence/exposure. Replaced with one exposure tracker per party member over the union of active pools. Placement/native EObj, four Akh Morn stack snapshots, Cauterize, eye/cross clocks and Elemental routes unchanged.

Current provisional model:1.1s placement-to-activation, then1s continuous exposure before first bleed pulse;1s intervals; third consecutive pulse is lethal. Exit removes the status and resets exposure. Already-active puddles use the same per-player entry grace. Overlapping pools cannot multiply pulses; elapsed simulation time drives ticks, so pause/frame rate cannot accelerate them. Run start and teardown clear exposure. This remains a qualitative practice failure model, not a complete HP/healing simulator.

AsuMech's reference lacks persistent Akh Morn puddle hazard handling. BossMod corroborates6y voidzones, not activation/bleed cadence. No real P6 action records were found in the available IINACT logs. Thus the new timing values are explicitly **UNVERIFIED**, chosen to allow movement while sustained exposure remains lethal. No claim of measured native DoT cadence.

## Validation and remaining native checks

Focused repair checks25 passed2.75s; P1+repair53 passed13.03s; incorrect-pair direction regression1 passed505ms. Main affected-phase/native-startup/FRU-presentation regression622 passed50.04s. Overlapping sets are not additive. Final integrated/safety results are in dsr-progress.md.

Owner playtest checklist:

- All11 menu labels/order; saved scenario/strategy selections survive.
- P1 and both P5 entries: cold inn/title-screen entry, body/weapon meshes, facing and animations; repeat Restart/Leave. Confirm loaded model, not just object-table presence.
- P4: center Estinien, nearby Ysayle/Haurchefant and Alphinaud with correct face/body/clothes/weapons; rescue Charibert/Zephirin/friendly Haurchefant; phase positions, no duplicates.
- P1: human and bots physically move16y from Grinnaux; anti-knockback meaningfully fails chains; correct pair positions/outward movement break them before Heavensflame. Test multiple roles and both entry modes.
- P6: four stacks survive together; each persistent puddle is visible before damaging; immediate escape succeeds, standing/late reentry eventually causes bleed death; no overlapping/frame-rate multiplier. Capture real tick/activation timing if available.
- Pause/Resume, God Mode, Restart during active bleed/chains and Leave→other DSR/FRU entries; no orphaned actors/statuses/EObjs.
