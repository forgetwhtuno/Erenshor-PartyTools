# Erenshor Party Tools — 0.1.x Test Matrix

The mod intentionally has only the ready check, social rolls, and friend-availability view in this phase. Do not add new systems to solve a test failure; fix the existing behavior first.

## Party Tools menu

1. Press `F7` or type `/tools`; confirm one draggable `PARTY TOOLS` menu opens.
2. Click each action once. Confirm it runs the same behavior as `/ready`,
   `/roll`, `/rollparty`, or `/ptwho`, and replaces the launcher rather than
   stacking a second panel.
3. With a raid active, click Ready Check and Party Roll; confirm each fails
   with the existing short unavailable message and does not open a result panel.
4. Press `Escape`, press F7 again, wait about 30 seconds, and zone while the
   menu is open; confirm each closes it safely.
5. Change `UI/OpenMenuKey` in the Lunaris config UI and confirm the new key
   toggles the menu. Confirm `/tools` still opens it after changing the key.
6. Confirm `/tools nope` shows one short usage message.

## `/ptwho`

1. Use native `/friend` on two Sims for the current character, then run `/ptwho`.
2. Confirm both appear without editing the Party Tools config.
3. Confirm an online ungrouped friend is `AVAILABLE`, an online friend already
   grouped is `BUSY - GROUPED`, and a native offline friend is `OFFLINE`.
4. Remove one with native `/friend` or the Group Builder friend toggle and
   confirm it disappears on the next `/ptwho` call.
5. Switch character slots and confirm only friends whose native `FriendedBy`
   matches the newly active slot appear.
6. Confirm GM characters and Sims friended by another character are excluded,
   matching Erenshor's own Group Builder Friends filter.
7. Leave `FriendAvailability/Friends` blank and confirm the native roster still
   works. Confirm fallback names are used only when the native manager/slot is
   unavailable, never when the available native roster is simply empty.
8. Change zones while the panel is open and confirm it closes. Repeat `/ptwho`
   and confirm it replaces rather than stacks.
9. Confirm `/ready`, `/roll`, and `/rollparty` retain their existing behavior.

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
5. Confirm the panel disappears after about nine seconds.

### Multiple party Sims

1. Group several local Sims.
2. Type `/ready`.
3. Confirm each current local party Sim appears once.
4. Confirm non-party Sims do not appear.

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

### Sim leaves while window is open

1. Open `/ready` with several party Sims.
2. Remove one Sim from the party while the panel remains visible.
3. Confirm the row disappears after the next refresh rather than throwing or retaining a stale object.

### Zone change while window is open

1. Open `/ready`.
2. Zone before the timeout expires.
3. Confirm the old panel closes immediately and does not survive into the new zone.

### COOP conservative behavior

If COOP is installed and a remote human / remote-owned Sim is represented in the party:

1. Run `/ready`.
2. Confirm Party Tools does not fabricate a local Sim readiness state for the remote member.
3. Confirm unresolved/remote state is `UNAVAILABLE` or absent only when the authoritative current-party list itself no longer contains that entry.

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
```

Pass criteria: one short usage line, no exception, no native command leakage from uncleared handled input.

## `/rollparty`

### Normal party

1. Group multiple local Sims.
2. Run `/rollparty`.
3. Confirm the panel contains `You` exactly once.
4. Confirm each eligible local party Sim appears exactly once.
5. Confirm each row has one result in 1-100.
6. With `Roll Chatter/Enabled = true`, confirm chat shows one player opening,
   one acknowledgment per eligible local Sim, one result per participant, and
   exactly one winner reaction when the highest roll is not tied.
7. Confirm friendly, competitive, blunt, and rival Sims use their corresponding
   short reaction tone when those verified personality values are present, and
   that Erenshor's native caps/lowercase/third-person/typo/emoticon quirks are
   applied by `PersonalizeString`.
8. Tie the highest value in a controlled/debug build and confirm no single
   winner is claimed. Set `Roll Chatter/Enabled = false` and confirm the panel
   still appears without the cosmetic chat sequence.
9. Run `/rollparty` with no eligible local Sims and confirm only the player's
   result is printed, with no fabricated acknowledgments or winner reaction.

### Alternate range

Run:

```text
/rollparty 1000
```

Confirm all values are in 1-1000 and the title shows the selected range.

### Party membership change

1. Open a party-roll panel.
2. Add or remove a party member before timeout.
3. Confirm the already-generated roll snapshot remains stable and no stale game object is dereferenced.
4. Run `/rollparty` again and confirm the new snapshot uses current membership.

### COOP exclusion

With remote COOP members present, confirm `/rollparty` rolls only for `You` plus verified eligible local Sims. It must not generate synthetic rolls for remote humans or remote-owned Sims.

## UI / spam

- Run `/ready` repeatedly and confirm only one panel remains.
- Run `/rollparty` repeatedly and confirm only one panel remains.
- Run `/ready`, then `/rollparty`, and confirm the latter replaces the former.
- Wait for timeout and confirm the panel is fully gone.
- Confirm normal movement/combat input is unaffected by the panel; Party Tools has no player-input interception patch.
- Confirm `/ready` emits no per-member chat. Confirm `/rollparty` emits exactly
  its configured bounded sequence and does not duplicate any result or reaction.
- Confirm unrelated slash commands continue to native Erenshor / other-mod handlers.
