# Spectator party-slot displacement repair

## Source of displacement

The simulator has eight bot participants and sends eight synthetic party records. None is the
native local player. The HUD agent constructs its self-first view separately. In the original
spectator path only offline mode repaired that view; online spectator mode left the parked
player in self's slot and displaced the final bot from the eight displayed positions.

Dalamud's IPartyList enumerates MainGroup.PartyMembers and its MemberCount directly. Installed
DelvUI 2.8.0.0 additionally reads the HUD agent's raw identity/name/index table and then resolves
matching Dalamud party records. Thus a correct simulation roster alone, or hiding a UI node,
cannot repair all consumers. Installed-assembly contract tests verify those data paths and the
HUD layout offset without loading or modifying DelvUI.

References: [Dalamud party enumeration](https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Game/ClientState/Party/PartyList.cs),
[DelvUI party data](https://github.com/DelvUI/DelvUI/blob/develop/DelvUI/Interface/Party/PartyManager.cs),
[client HUD structure](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Agent/AgentHUD.cs).

## Contained projection

The watched bot is first in presentation order; the other seven roles follow in role order.
All eight simulation actors retain their own identities. Party.Player remains absent in
spectator mode; the parked native player remains intact and the approved camera is unchanged.

PartyHud projects the same eight actors into three existing native presentation surfaces:

1. MainGroup's eight member records, including actor IDs, names, jobs, HP/MP and status data.
2. AgentHUD's self-first member table, including matching actor pointers, IDs and row indices.
3. The existing native party-list number/string arrays, including job icons, HP/MP, shields,
   status icons and casting information. No separate party HUD or ninth row is created.

A scoped post-AgentHUD.Update hook reapplies the projection after native self insertion, even
when DelvUI hides the native addon. PreRequestedUpdate also reapplies it immediately before the
native party renderer consumes its arrays. Numeric array updates explicitly notify subscribers.
Both mechanisms run only for an owned spectator roster during an active isolated stay.

The real actor's identity, content ID, ownership, job, HP/MP and status manager are never changed
by this projection. No bot receives the real player's identity. Row targeting metadata refers
to the actor shown. The simulation roster, damage, camera and input routing are unchanged.
No outgoing packet or server membership change is produced.

Native buffer sizes are checked before projecting rows. Status display is capped at ten icons
while the shared party record retains the full status list. Synthetic enmity values are not
invented; spectator enmity indicators are cleared rather than showing the parked player's hate.
Native social/ready-check self behavior is not emulated.

## Restoration and validation

The existing authentic party packet snapshot remains the restoration source. Clear removes
owned virtual HUD entries and projected strings/numbers, then replays that snapshot locally.
The next native HUD update rebuilds authentic rows, so a transient empty HUD is possible during
teardown rather than retaining pointers to despawned bots. Normal-practice projection remains
unchanged. The update hook is disposed on unload and cannot reapply a cleared projection.

Native-memory tests check all eight watched roles, exact group/HUD/number-row identity, the
2 tank / 2 healer / 4 DPS job set, D4 retention, HP/MP/status mapping, bounded status counts,
death refresh and selective cleanup. Headless lifecycle tests exercise FRU and DSR watched roles,
pause/resume, role and scenario changes, spectator→normal→spectator transitions, intentional
failure, actor cleanup and restoration. Full coverage/combined results are in the batch report.

## Native verification still required

The repair has not yet been observed in the running native or DelvUI UI. Verify both displays
with MT through D4 selected, including a tank native player and D4 watched: eight bot identities,
2/2/4 composition, no parked-player row, correct HP/MP/jobs/statuses, row clicks and status hover.
Repeat with the native addon hidden by DelvUI, and across Pause, Restart, wipe/completion, mode
and scenario changes, Leave and unload, in inn and title-screen offline sessions.

Party cast progress falls back to a provisional 0–100 projection and is marked UNVERIFIED in
source; verify its visual scale and class/status icon styling. Third-party compatibility is
proven only at the installed data-contract level until native retesting. No global local-player
substitution or unsafe identity mutation is used to force compatibility.
