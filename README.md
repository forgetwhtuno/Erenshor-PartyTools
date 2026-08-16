# Erenshor Party Tools 0.1.5

Party Tools is a lightweight social/diagnostic utility for local Erenshor parties. It does not control combat, movement, healing, loot, rolls in the game engine, or rewards.

## Commands and panel

```text
/tools                    open the retained Party Tools panel
/ready                    show a local-party ready check
/roll [maximum]           roll for the player, 1..1,000,000
/rollparty [maximum]      display cosmetic rolls for the current local party
/ptwho                    show the current character's friend availability
```

Normal mouse access is through the retained-uGUI **Party Tools** launcher or Suite Hub's **Open Panel** action. There is no polled global hotkey for normal access; the old F7 config value is retained only as legacy config compatibility. The panel has a visible X, a dedicated non-button drag surface, normalized position persistence, resolution reclamping, and a scrollable result area.

The same four deterministic tools are available as panel buttons: **Ready Check**, **Roll 1-100**, **Party Roll 1-100**, and **Friends Online**. The panel also shows explicit **Launcher [ON/OFF]** and **Roll Summary [ON/OFF]** state buttons rather than ambiguous tiny toggles. Commands retain their exact syntax/bounds and remain useful as compatibility/debug recovery paths.

## Ready checks

`/ready` reads the currently observable local party and displays readiness rows. It is unavailable during raids. The check refreshes only verified local state and times out after a bounded 10 seconds, returning the panel to its neutral tools view. It is a readiness display, not a command that changes party state or forces a player/Sim response. No AI readiness or speculative "recovering" state is inferred.

## Cosmetic rolls

`/roll` uses rejection-sampled cryptographic randomness and makes exactly one native chat append attempt for its local result. `/rollparty` generates one result for the player plus each verified eligible local Sim, displays those results in the retained panel, and can make one native append attempt for one concise local summary line. It never impersonates Sim responses. The winner is not awarded an item, currency, XP, loot rights, or any other gameplay state. The summary line can be disabled in configuration.

## Friend availability

`/ptwho` reads the current character's native friend roster using the same character binding as Erenshor's Friends filter. It shows each friend's native state: **AVAILABLE**, **BUSY - GROUPED**, or **OFFLINE**. While the view remains open it refreshes on a bounded cadence so the list does not go stale. The tool is read-only: it never sends invites or changes the friend list.

## Compatibility and safety

Party Tools uses narrow Harmony command interception, read-only state inspection, and a fail-closed monotonic `CameraController.UsingUI()` postfix only while Party Tools owns a left-button retained-UI drag. It does not patch player clicks, poll camera axes, or force `EditUIMode`. It preserves commands it does not own, stays deliberately limited in raids, keeps panel refresh bounded, and fails closed when native current-party state is unavailable. It has no Deep Sims, Ollama, network, or gameplay dependency; COOP compatibility is runtime-detected so remote-human readiness is never invented.

## UI ownership boundary

Party Tools owns only its retained **PARTY TOOLS** launcher/panel and the Ready Check, Roll, Party Roll, Friends Online, launcher preference, and roll-summary controls described above. The tall in-game party command stack containing controls such as **Attack, Assist, Pull Target, Auto Pull, Guard** is Erenshor's native party/group UI, not a Party Tools panel. This mod does not patch or restyle that stack in the current source, so changes to it are outside Party Tools' safe surface.

## Build

This version requires **native Lunaris** — BepInEx is no longer required. `BUILD_AND_INSTALL.ps1` compiles the plugin and installs it to `<Erenshor>\plugins\ErenshorPartyTools.dll`; Lunaris manages enable/disable and config. The plugin identifier is `forgetwhtuno.erenshor.partytools`, version `0.1.5`.

**Status:** the native-Lunaris baseline previously compiled against the installed Lunaris/Assembly-CSharp. The retained-uGUI workstream changes the UI/source surface and therefore still requires a fresh current-assembly compile, deterministic test run, and live enable/disable/reload validation before release. A legacy BepInEx release remains available in this repository's Git history.

## Credits and Inspiration

### Compatibility / related projects

- **[Erenshor COOP](https://github.com/MizukiBelhi/ErenshorCoop) by MizukiBelhi** — important technical reference and compatibility target for remote-human/networked-Sim detection. I have also tested against a locally updated copy for recent Erenshor and Deep Sims compatibility.

## Development note

This project has been developed heavily with AI-assisted coding tools. The goal has been to build features I wanted to use in Erenshor, with development guided through design, testing, playtesting, audits, and iteration against the game. Bug reports, code review, corrections, and contributions from experienced Erenshor modders are welcome.

This is an unofficial, community-made mod for Erenshor and is not affiliated with or endorsed by the game's developer.


## Optional Suite Hub integration

Erenshor Suite Hub is **optional**. Party Tools exposes its versioned `PartyToolsControlApi` through Aura without referencing Hub types or assuming Hub load order. Hub can show concise state, `Show Party Tools Launcher`, `Party roll chat summary`, and the conventional panel/actions. The module advertises `ui.state` + `closePanel`. Escape ownership is deterministic: if a well-formed Hub endpoint is present, Party Tools does **not** poll Escape—even when Hub reports `quickClose=0`; the player uses explicit X/close controls until Hub has a verified native consume path. A local Escape fallback exists only when the Hub endpoint is genuinely unavailable, preserving standalone usability without competing with a healthy Hub.

The retained panel and commands stay independently usable. Launcher fallback is mandatory: with Hub absent/unusable or this module's bridge unregistered, the on-screen Party Tools launcher is forced visible even when the saved Hub-era preference is off. With Hub and the bridge usable, the preference is obeyed.

Ready-check inference, roll bounds/output behavior, raid limitations, friend availability, and COOP remote-human handling remain deterministic and authoritative in Party Tools.

The retained-uGUI migration still requires a current-assembly compile and live Lunaris hot-reload pass before release.
