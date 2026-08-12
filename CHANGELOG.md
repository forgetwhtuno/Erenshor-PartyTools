# Changelog

## Unreleased

- Aligned the panel interaction contract with PvP: upper-right/below-minimap placement, persisted clamped dragging with hot-control ownership, and suppression of world clicks and camera rotation while the pointer is over or dragging Party Tools.
- `/ptwho` now reads the current character's real Erenshor friend roster using
  the same `FriendedBy == CurrentCharacterSlot.index` predicate as Group
  Builder's native Friends filter. Availability uses native `online` and
  `Grouped` tracking; manual names remain compatibility fallback only.
- Added the required `UnityEngine.InputLegacyModule` build reference for the
  configurable menu key and Escape-to-close handling.
- Added configurable `/rollparty` social chatter: player announcement, one
  acknowledgment per eligible local Sim, chat-visible results, and an untied
  winner reaction. Sim wording uses verified personality/rival signals and
  Erenshor's native `PersonalizeString` typing quirks, following Deep Sims'
  established reader pattern without creating a hard Deep Sims dependency.
- Added a temporary Party Tools command menu. Press configurable `F7`, type
  `/tools`, then choose Ready Check, Roll 1-100, Party Roll
  1-100, or Friends Online. Menu actions share the existing command handlers.

- Added `/ptwho`, an advisory-only friend availability panel.
- Retained the earlier persistent-seed availability simulation only as a
  compatibility fallback; no game save, AI, grouping, invitation, or COOP sync
  is changed.
- Fail `/ready` and `/rollparty` closed while the game reports an active raid;
  the mod does not interpret raid rosters or subgroups.
- Treat inactive player and Sim objects as `UNAVAILABLE` before reading their
  liveness, preventing stale readiness rows during scene transitions.

## 0.1.0 - Initial development build

- Added `/ready` temporary party readiness panel.
- Added verified `READY`, `DEAD`, `IN COMBAT`, and `UNAVAILABLE` states.
- Added `/roll` with a default 1-100 range and optional maximum.
- Added `/rollparty` temporary local-party roll panel.
- Added reflection-only COOP exclusion for remote humans and remote-owned Sims.
- Added automatic panel timeout and zone-change cleanup.
- Added build/install and uninstall helpers.
