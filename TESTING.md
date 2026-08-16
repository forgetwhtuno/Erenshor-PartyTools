# Erenshor Party Tools — 0.1.x Test Matrix

The mod intentionally has only ready checks, social rolls, and a friend-availability view. Do not add new systems to solve a test failure; fix the existing behavior first.

## Retained Party Tools panel

1. Use the retained **Party Tools** launcher; confirm one draggable `PARTY TOOLS` panel opens. With Suite Hub installed, also confirm its **Open Panel** action opens the same single panel. `/tools` remains recovery access.
2. Click **Ready Check**, **Roll 1-100**, **Party Roll 1-100**, and **Friends Online** once each. Confirm each uses the same behavior as its command and updates the existing panel instead of stacking another instance.
3. Toggle **Launcher [ON/OFF]** and **Roll Summary [ON/OFF]** in the panel. Confirm the text always states the saved boolean explicitly. With Hub healthy, launcher OFF hides the launcher; with Hub absent/unusable or the module bridge unavailable, fallback visibility is forced on even though the saved preference remains OFF.
4. With a raid active, click Ready Check and Party Roll; confirm each fails with the existing short unavailable message and does not fabricate raid behavior.
5. Click the visible `X`, reopen, then zone while the panel is open. Confirm each close path is safe and exactly one retained panel exists after reopen.
6. Escape ownership matrix: (a) with Hub genuinely absent, an open standalone Party Tools panel may close on its local Escape fallback; (b) with a well-formed Hub present but `quickClose=0`, Escape must **not** close Party Tools—use X and confirm vanilla Escape behaves normally; (c) with a future Hub advertising `quickCloseContract=1&quickClose=1` and this provider registered, confirm Hub closes Party Tools through `ui.state` + `closePanel` with no double-close/flicker.
7. Verify the tall native Attack / Assist / Pull Target / Auto Pull / Guard party stack is unchanged by Party Tools. Party Tools owns only its own retained panel/launcher.
7. Confirm `/tools` still opens the same retained panel and `/tools nope` produces one short usage message.

## `/ptwho` — friend availability

1. Run `/ptwho` and confirm it shows only the current character's native Friends roster, with `AVAILABLE`, `BUSY - GROUPED`, or `OFFLINE` for each friend. Nearby Sims who are not friends must not appear.
2. Confirm verified local Sims are `L<level> <class> • LOCAL SIM` when the persistent tracking values are available. If COOP is installed and its component is observable on a party actor, confirm remote human/remote-owned Sim rows are clearly marked remote and are never treated as local readiness/roll participants. A remote human must not inherit class/level from the backing Sim tracking record. If no authoritative local avatar exists, `UNAVAILABLE` is acceptable and must not be guessed.
3. Remove or add a party member while the `/ptwho` view remains open. Within the bounded refresh interval, confirm the row disappears/appears without rerunning the command and without a stale-reference exception.
4. Zone while `/ptwho` is open; confirm the old panel closes. Reopen after the new world state is ready and confirm the native Friends roster is shown.
5. Legacy `FriendAvailability/*` config values may still exist in an old config file, but changing them must not alter `/ptwho` output.
6. Confirm `/ready`, `/roll`, and `/rollparty` retain their existing behavior after repeated `/ptwho` use.

## Build gate

Run `BUILD_AND_INSTALL.bat` against the currently installed Erenshor build.

Pass criteria:

- compilation succeeds against the installed `Assembly-CSharp.dll` and the Lunaris/Unity assemblies;
- `<Erenshor>\plugins\ErenshorPartyTools.dll` is produced;
- Lunaris reports the plugin loaded with no patch exception.

## `/ready`

### Solo / no party

1. Enter a normal playable zone with no Sims grouped.
2. Type `/ready`.
3. Confirm one small panel opens.
4. Confirm `You` is shown as `READY` when alive and out of combat.
5. Leave the ready check untouched for 10 seconds. Confirm the ready-check rows stop refreshing and the single retained panel returns to the neutral tools view without stacking or logging an error.

### Multiple party Sims

1. Group several local Sims.
2. Type `/ready`.
3. Confirm each current local party Sim appears once.
4. Confirm non-party Sims do not appear.
5. Repeat `/ready` before the 10-second timeout and confirm it restarts/replaces the current check rather than creating a second panel.

### Dead Sim

1. Have a current party Sim in a genuine dead state.
2. Type `/ready` or let an already-open ready panel refresh.
3. Confirm that Sim shows `DEAD`.
4. Confirm no revival/heal/state mutation is performed by Party Tools.

### Combat state

1. Enter genuine combat.
2. Type `/ready`.
3. Confirm the local player reports `IN COMBAT` while `GameData.InCombat` is true.
4. Confirm a local Sim reports `IN COMBAT` when its native group-combat signal or active aggro target establishes combat.
5. Leave combat and confirm the open panel refreshes back to `READY` when no other blocker remains.

### Party mutates while window is open

1. Open `/ready` with several party Sims.
2. Remove one Sim from the party while the panel remains visible; confirm the row disappears after the next refresh rather than throwing or retaining a stale object.
3. Add another current local Sim; confirm the new row appears on the next refresh.
4. Disband the party completely; confirm the next refresh contains only `You` and no stale rows.
5. Repeat `/ready` immediately after regrouping and confirm the same retained panel restarts the bounded check with the new roster.

### Zone change while window is open

1. Open `/ready`.
2. Zone while the retained panel is open.
3. Confirm the old panel closes immediately and does not survive into the new zone.

### COOP conservative behavior

If COOP is installed and a remote human / remote-owned Sim is represented in the party:

1. Run `/ready`.
2. Confirm Party Tools does not fabricate a local Sim readiness state for the remote member.
3. Confirm an explicitly detected remote human is `REMOTE`; remote-owned Sims or otherwise unresolved state remain `UNAVAILABLE`. A remote human is never shown as READY/COMBAT/DEAD by Party Tools.

### Raid boundary

1. Start a v0.7 raid, including one with members outside the player's normal subgroup.
2. Run `/ready` and `/rollparty`.
3. Confirm each command produces one short unavailable message and no panel.
4. Confirm `/roll` remains a local-only roll.

## `/roll`

Test each command independently:

```text
/roll
/roll 1
/roll 100
/roll 1000
/roll 1000000
```

Pass criteria:

- `/roll` produces exactly one social-log line in the form `Name rolls N (1-100).`;
- `/roll 1` always produces `1 (1-1)`;
- 100 and 1000 results remain inside their inclusive ranges;
- repeated rolls do not create UI panels.

Invalid input:

```text
/roll 0
/roll -1
/roll nope
/roll 1000001
/roll 999999999999999999999
/roll 10 20
/roll +1
```

Pass criteria: one short usage line, no exception, no native command leakage from uncleared handled input.

## `/rollparty`

### Normal party

1. Group multiple local Sims.
2. Run `/rollparty`.
3. Confirm the panel contains `You` exactly once.
4. Confirm each eligible local party Sim appears exactly once.
5. Confirm each row has one result in 1-100.
6. With `Roll Chatter/Enabled = true`, confirm exactly one concise local chat summary is emitted for the action, listing the generated results and either one untied winner or the tied high value. No Sim acknowledgment/dialogue is generated.
7. Tie the highest value in a controlled/debug build and confirm no single winner is claimed. Set `Roll Chatter/Enabled = false` and confirm the panel still appears with no roll-summary chat line.
8. Run `/rollparty` with no eligible local Sims and confirm the panel contains only the player's result and no fabricated Sim response.

### Alternate range

Run:

```text
/rollparty 1000
```

Confirm all values are in 1-1000 and the title shows the selected range.

### Party membership change

1. Open a party-roll panel.
2. Add or remove a party member while that snapshot remains visible.
3. Confirm the already-generated roll snapshot remains stable and no stale game object is dereferenced.
4. Run `/rollparty` again and confirm the new snapshot uses current membership.

### COOP exclusion

With remote COOP members present, confirm `/rollparty` rolls only for `You` plus verified eligible local Sims. It must not generate synthetic rolls for remote humans or remote-owned Sims.

## UI / spam

- Run `/ready` repeatedly and confirm only one panel remains.
- Run `/rollparty` repeatedly and confirm only one panel remains.
- Run `/ptwho` repeatedly and confirm only one panel remains and friend availability rows keep refreshing.
- Run `/ready`, then `/rollparty`, and confirm the latter replaces the former.
- Close with the visible `X` and confirm the panel hides cleanly; reopen and confirm exactly one retained instance.
- Confirm normal movement/combat input is unaffected by the panel; Party Tools has no player-input interception patch.
- Confirm `/ready` emits no per-member chat. Confirm `/rollparty` emits at most one configured summary line and never duplicates or impersonates a Sim response.
- Confirm unrelated slash commands continue to native Erenshor / other-mod handlers.


## Lunaris lifecycle / reload

- Disable/re-enable Party Tools repeatedly with and without COOP installed; confirm only one launcher/panel exists, optional COOP ownership detection still works, and no stale AssemblyLoad subscription is retained by an old plugin instance.
- After each reload, run `/roll 100`, `/ready`, and `/ptwho`; confirm the cryptographic RNG is available again, the retained panel rebuilds once, and command handling remains single-shot.
- Remove/reload COOP after Party Tools has loaded where the loader permits it; confirm the optional type cache invalidates on assembly load and unresolved actors fail closed rather than being reclassified from stale cached types.
- Shut down/unload while the panel is being dragged; confirm drag ownership, retained GameObjects, Aura handlers, Harmony patches, COOP reflection cache/subscription, and RNG provider are all released.
