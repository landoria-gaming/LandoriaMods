# CharacterVault

CharacterVault keeps your Valheim character on the server. When you join, the
server loads its trusted copy, preventing items from being imported or duplicated
with another save or a restored backup.

## Features

- The server loads its latest trusted copy before your character enters the world; local saves still work normally.
- A status below the minimap shows when the character is saving and confirms when the save is complete.
- The server keeps up to 15 automatic backups per character.
- CharacterVault requires a new character when joining a server for the first time.
- CharacterVault supports crossplay servers.
- The server records when each platform account and character was first connected and last seen online.
- Last-seen timestamps are updated when a character is admitted and before each world save, entirely on the server.

## When characters are saved

Characters are saved:

- after their first enrollment;
- during automatic and manual saves;
- when the player logs out, quits, or is disconnected by the server;
- before a graceful server stop or restart (when configured on the server).

Characters are not saved after a client crash or network loss because the
connection is already unavailable. Guests are not saved (see ModSentry).

## Installation

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