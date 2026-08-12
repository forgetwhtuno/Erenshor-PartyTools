# Erenshor Party Tools 0.1.2

Party Tools is a lightweight social/diagnostic utility for local Erenshor parties. It does not control combat, movement, healing, loot, rolls in the game engine, or rewards.

## Commands and menu

```text
/tools                    open the command menu
/ready                    show a local-party ready check
/roll [maximum]           roll for the player, 1..1,000,000
/rollparty [maximum]      display cosmetic rolls for the current local party
/ptwho                    show configured/native friend availability
```

`F7` opens the menu by default. The key and panel offsets are configurable. Panels use the shared upper-right/below-minimap layout, persist a clamped drag position, suppress world clicks/camera movement under the panel, and close on timeout or relevant scene changes.

## Ready checks

`/ready` reads the currently observable local party and displays readiness rows. It is unavailable during raids. It is a readiness display, not a command that changes party state or forces a player/Sim response.

## Cosmetic rolls

`/roll` uses a local bounded random roll. `/rollparty` creates a display row for each eligible local party participant, announces the roll in party-style chat, lets local Sims acknowledge it with bounded personality/typing flavor, and displays one winner or tie result. The winner is not awarded an item, currency, XP, loot rights, or any other gameplay state. Roll chatter can be disabled in configuration.

## Friend availability

`/ptwho` prefers Erenshor's native current-character friend roster. If native data is temporarily unavailable, a configured comma-separated fallback list can be used. Availability is a local simulated display with a bounded real-time session block; it never sends invites, changes the friend list, or claims remote-online status.

## Compatibility and safety

Party Tools uses narrow Harmony command interception and read-only state inspection. It preserves commands it does not own, is raid-aware, keeps panel work off high-frequency scans, and fails closed when native party/friend data is unavailable. It has no Deep Sims, COOP, Ollama, network, or gameplay dependency.

## Build

`BUILD_AND_INSTALL.ps1` compiles and installs the plugin to a BepInEx profile. The plugin identifier is `forgetwhtuno.erenshor.partytools`, version `0.1.2`.

## Credits and Inspiration

### Compatibility / related projects

- **[Erenshor COOP](https://github.com/MizukiBelhi/ErenshorCoop) by MizukiBelhi** — important technical reference and compatibility target for remote-human/networked-Sim detection. I have also tested against a locally updated copy for recent Erenshor and Deep Sims compatibility.

## Development note

This project has been developed heavily with AI-assisted coding tools. The goal has been to build features I wanted to use in Erenshor, with development guided through design, testing, playtesting, audits, and iteration against the game. Bug reports, code review, corrections, and contributions from experienced Erenshor modders are welcome.

This is an unofficial, community-made mod for Erenshor and is not affiliated with or endorsed by the game's developer.
