# Erenshor Party Tools 0.1.2

Party Tools is a lightweight social/diagnostic utility for local Erenshor parties. It does not control combat, movement, healing, loot, rolls in the game engine, or rewards.

## Commands and panel

```text
/tools                    open the retained Party Tools panel
/ready                    show a local-party ready check
/roll [maximum]           roll for the player, 1..1,000,000
/rollparty [maximum]      display cosmetic rolls for the current local party
/ptwho                    show configured/native friend availability
```

Normal mouse access is through the retained-uGUI **Party Tools** launcher or Suite Hub's **Open Panel** action. There is no polled global hotkey for normal access; the old F7 config value is retained only as legacy config compatibility. The panel has a visible X, a dedicated non-button drag surface, normalized position persistence, resolution reclamping, and a scrollable result area.

The same four deterministic tools are available as panel buttons: **Ready Check**, **Roll 1-100**, **Party Roll 1-100**, and **Friends Online**. Commands retain their exact syntax/bounds and remain useful as compatibility/debug recovery paths.

## Ready checks

`/ready` reads the currently observable local party and displays readiness rows. It is unavailable during raids. It is a readiness display, not a command that changes party state or forces a player/Sim response.

## Cosmetic rolls

`/roll` uses a local bounded random roll. `/rollparty` creates a display row for each eligible local party participant, announces the roll in party-style chat, lets local Sims acknowledge it with bounded personality/typing flavor, and displays one winner or tie result. The winner is not awarded an item, currency, XP, loot rights, or any other gameplay state. Roll chatter can be disabled in configuration.

## Friend availability

`/ptwho` prefers Erenshor's native current-character friend roster. If native data is temporarily unavailable, a configured comma-separated fallback list can be used. Availability is a local simulated display with a bounded real-time session block; it never sends invites, changes the friend list, or claims remote-online status.

## Compatibility and safety

Party Tools uses narrow Harmony command interception and read-only state inspection. Production UI is retained uGUI; it does not patch player clicks or camera orbit and never forces `EditUIMode`. It preserves commands it does not own, stays deliberately limited in raids, keeps panel refresh bounded, and fails closed when native party/friend data is unavailable. It has no Deep Sims, Ollama, network, or gameplay dependency; COOP compatibility is runtime-detected so remote-human readiness is never invented.

## Build

This version requires **native Lunaris** — BepInEx is no longer required. `BUILD_AND_INSTALL.ps1` compiles the plugin and installs it to `<Erenshor>\plugins\ErenshorPartyTools.dll`; Lunaris manages enable/disable and config. The plugin identifier is `forgetwhtuno.erenshor.partytools`, version `0.1.2`.

**Status:** the native-Lunaris baseline previously compiled against the installed Lunaris/Assembly-CSharp. The retained-uGUI workstream changes the UI/source surface and therefore still requires a fresh current-assembly compile, deterministic test run, and live enable/disable/reload validation before release. A legacy BepInEx release remains available in this repository's Git history.

## Credits and Inspiration

### Compatibility / related projects

- **[Erenshor COOP](https://github.com/MizukiBelhi/ErenshorCoop) by MizukiBelhi** — important technical reference and compatibility target for remote-human/networked-Sim detection. I have also tested against a locally updated copy for recent Erenshor and Deep Sims compatibility.

## Development note

This project has been developed heavily with AI-assisted coding tools. The goal has been to build features I wanted to use in Erenshor, with development guided through design, testing, playtesting, audits, and iteration against the game. Bug reports, code review, corrections, and contributions from experienced Erenshor modders are welcome.

This is an unofficial, community-made mod for Erenshor and is not affiliated with or endorsed by the game's developer.


## Optional Suite Hub integration

Erenshor Suite Hub is **optional**. Party Tools exposes its versioned `PartyToolsControlApi` through Aura without referencing Hub types or assuming Hub load order. Hub can show concise state, `Show Party Tools launcher`, roll-chatter/friend-fallback settings, and the conventional `openPanel` action. Additional action IDs remain available over the Aura transport and independently revalidate through the ControlApi.

The retained panel and commands stay independently usable. Launcher fallback is mandatory: with Hub absent/unusable or this module's bridge unregistered, the on-screen Party Tools launcher is forced visible even when the saved Hub-era preference is off. With Hub and the bridge usable, the preference is obeyed.

Ready-check inference, roll bounds/output behavior, raid limitations, friend availability, and COOP remote-human handling remain deterministic and authoritative in the existing Party Tools logic.

The retained-uGUI migration still requires a current-assembly compile and live Lunaris hot-reload pass before release.
