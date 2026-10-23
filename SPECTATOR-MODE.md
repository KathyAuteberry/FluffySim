# JESUS TAKE THE WHEEL

Development implementation; native camera presentation is **not yet verified in FFXIV**.

Choose Mode → JESUS TAKE THE WHEEL and a POV role (MT, ST, H1, H2, D1–D4), then Start.
The last role is saved in the plugin configuration. Pause/Resume, Restart, Stop and Leave use
normal local-run controls. Stop permits changing mode; Restart retains the current run parameters.
A blank seed still permits fresh rolls on a new Start; an explicit seed reproduces assignments.

## Simulation

`LocalPartyProvider` supplies eight `BotSlot`s instead of seven bots and a `PlayerSlot`.
Every participant is an ordinary `SimPartyNpc`, using the scenario's existing AI, movement,
actions, damage and death rules. The parked native player has no party seat, target-selection
membership, tower count or damage participation. The watched role supplies the perspective role
for practice overrides, without manufacturing a `SimPlayer`. Spectator runs force party visibility
and disable God Mode. No missing AI solution is replaced with immunity.

The initial supported scenarios are DSR P3 Nidhogg (Elemental Easthogg), DSR Strength 1, and
FRU Light Rampant. Other scenarios explicitly refuse this mode until validated. This is a
simulator-level roster/camera facility; scenario support only opts into that facility.

## Camera and isolation

`ISpectatorCamera` is the native boundary. The native implementation hooks the normal camera's
`GetCameraTargetObject` virtual function and returns the selected bot actor only for the owned
camera during an active isolated stay. Pending actor creation falls back to the native target;
the first observed entity ID prevents following a later occupant of that slot. Other camera
instances retain their original lookup. Mouse input, yaw, pitch, distance and collision are left
to the native camera rather than continuously forced toward mechanics.

The parked character is hidden and movement/action input is blocked independently of Pause.
Local user-action handlers do not tick for it. The camera implementation never steers or relocates
that character and never sends packets. Existing inn/offline entry gates, restoration and packet
firewalls remain responsible for the zone stay. Marks and ground presentation continue through
the native local recipient without making it a participant.

Teardown clears the target and disables the hook before actor cleanup, restores saved camera
angles/zoom and parked visibility, and releases the spectator input lock. Restart, Stop, Leave,
failed initialization and disposal use the same world cleanup callback.

## Native verification still required

The client structures expose an actor lookup but no documented actor-follow setter. Redirecting
that lookup is an implementation candidate, **not proof of authentic native POV following**.
Headless fakes cannot establish camera behavior, native party-list behavior or animation quality.
If the lookup is insufficient, further native investigation is needed; there is no silent static
spectator-view fallback. An unavailable normal camera refuses the start.

Before considering this feature native-accepted, check both an inn-launched stay and title-screen
offline mode:

- DSR P3 D1: follow throughout dives, tower placement/soaking, Geirskoguls and Soul Tethers.
- Mouse orbit, zoom, camera collision and visibility of nearby mechanics; check Standard and Legacy.
- All eight party entries, with no parked-player entry or duplicate seat; statuses, castbars,
  tethers, local markers and damage presentation.
- FRU Light Rampant and DSR Strength 1, including every role's movement and animations.
- Pause during movement and pending mechanics, Resume, Restart, wipe and completion.
- Stop → Normal Practice and Leave: camera, body visibility, controls, local actions, actor cleanup
  and the inn position restored; offline exit releases the virtual identity.
- Unexpected zone loss and plugin unload. Firewall remains armed until ordinary restoration
  proves client/server state agrees.

No native success claim should be inferred from passing headless tests.
