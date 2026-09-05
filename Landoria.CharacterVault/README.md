# CharacterVault

CharacterVault keeps your Valheim character on the server. When you join, the
server loads its trusted copy, preventing items from being imported or duplicated
with another save or a restored backup.

## Features

- The server loads its latest trusted copy before your character enters the world; local saves still work normally.
- A status below the minimap shows when the character is saving and confirms when the save is complete.
- The server keeps up to 15 automatic backups per character.
- CharacterVault requires a new character when joining a server for the first time.
- A server integration can restore the latest compatible backup when no local server profile exists.
- CharacterVault supports crossplay servers.

Server-side mods can provide a verified character when it is not yet present on the current world. See the
[character restore provider integration guide](https://github.com/landoria-gaming/LandoriaMods/blob/main/Landoria.CharacterVault/CHARACTER_RESTORE_PROVIDER.md).

## When characters are saved

Characters are saved:

- after their first enrollment;
- during automatic and manual saves;
- when the player logs out, quits, or is disconnected by the server;
- before a graceful server stop or restart (when configured on the server).

Characters are not saved after a client crash or network loss because the
connection is already unavailable. Other server mods can disable character
validation and storage for specific sessions.

## Installation

Most of Landoria mods need to be installed also on the dedicated server, we show you in this video how to do it on windows:

[Setup a Valheim Modded dedicated server on Windows](https://youtu.be/X5J1wSRr7Uo)

| Crossplay support | Steam network support |
|---|---
| Yes | No

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |

## Server configuration

Add these optional switches to the dedicated-server command line:

| Switch | Purpose | Default |
|---|---|---|
| `--charactervault-allow-multiple-characters true\|false` | Allows or prevents one account from enrolling multiple characters. | `true` |
| `--charactervault-starting-items Prefab:Quantity,...` | Gives the listed items once when a new character is enrolled. | No items |

Example:

```text
--charactervault-allow-multiple-characters false
--charactervault-starting-items Hammer:1,PickaxeAntler:1
```

Graceful server stop and restart support is optional on dedicated servers and
requires integration with the service that stops Valheim. See the
[graceful server stop and restart guide](https://github.com/landoria-gaming/LandoriaMods/blob/main/Landoria.CharacterVault/GRACEFUL_SERVER_STOP.md).

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions and feedback, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
