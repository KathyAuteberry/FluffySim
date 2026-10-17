# Sanctity tower pillar height

Strength Part 2 (25567) and both Sanctity tower waves (29564, 28651) share Omen 90,
`d1025_sp012_bunsan01s`, circle cast type 2 and EffectRange 3 in the pinned Action sheet.
The height difference is not explained by a different sheet asset or radius.

Strength Part 2 relies on the client's cast rendering. Sanctity explicitly owns static
omen AVFX because helper casts alone did not establish persistent graphics. That path
scaled X/Z by EffectRange but kept Y at 1, producing `(3,1,3)` instances. Owner comparison
found the resulting pillars too short. Calling this Y value "native scale" in earlier
notes was not evidence of visual parity with the client's cast renderer.

The first adjustment requested height scale 3 for both waves. Owner comparison still
found those pillars too short; the live diagnostic log confirmed `(3,3,3)` was active.
Sanctity now requests height scale 6 for both waves, producing `(3,6,3)` instances.
This doubles the tested vertical height while preserving the footprint. It is a visual
tuning candidate, not a recovered measurement of Strength's native height.
The radius-three footprint, collision checks, asset and lifecycle remain unchanged.
Other explicit omens retain height scale 1 by default. Headless presentation checks verify
both waves' scale, ownership during Pause and release on Stop/Restart; exact visual parity
with Strength's client-rendered towers still requires an in-game comparison.
