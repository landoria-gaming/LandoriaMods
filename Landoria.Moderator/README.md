# Moderator

Gives trusted Valheim moderators server-authorized tools for helping players and managing the world.

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

## Features

- Adds a protected moderator mode that starts disabled until an administrator activates it.
- Enables god and ghost modes for active moderators.
- Hides and blocks vanilla cheats and `devcommands`.
- Tracks connected players after revealing the map.
- Supports Shift-click map teleportation.
- Shows a green `[Moderator]` label beside active moderators' names.
- Records every moderator command for accountability.

## Commands

| Command | Purpose |
|---|---|
| `moderator` | Toggles moderator mode. |
| `exploremap` | Reveals the map and tracks players. |
| `goto <player>` | Teleports to a player. |
| `itemset <biome>` | Applies a vanilla biome item set. |
| `playerlist` | Lists players and administrators. |
| `summon <player>` | Teleports a player to you. |
| `resetmap` | Clears exploration and tracking. |
| `spawn <prefab> [amount] [level] [radius]` | Spawns a Valheim prefab. |

All commands except `moderator` require active moderator mode. The server validates access against `adminlist.txt`.

## Installation

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |

Matching versions must be installed on the server and every client.


## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).