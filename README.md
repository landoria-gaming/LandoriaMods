# Landoria Mods

This repository contains the public source code for Landoria's Valheim mods.
Each mod directory includes one player-focused `README.md` used on GitHub and
Thunderstore. A mod may also include a changelog.

## Server-driven configuration

- The server controls multiplayer rules and sends effective settings to clients.
- Players do not need to copy server configuration files.
- Personal settings, such as camera preferences, remain local.

| Mod | Installation side | Description |
|---|---|---|
| [AfkDetector](Landoria.AfkDetector/) | Both | Disconnects players who remain motionless and silent, with a clear inactivity message. |
| [CharacterVault](Landoria.CharacterVault/) | Both | Stores authoritative server profiles and checkpoints characters with world saves. |
| [DecayControl](Landoria.DecayControl/) | Both | Controls rain damage and fuel use for player-built structures. |
| [GentleDeath](Landoria.GentleDeath/) | Client; server when required by a game mode | Keeps equipable gear on the player after death and moves other items to the tombstone. |
| [GetMyTrophyBack](Landoria.GetMyTrophyBack/) | Both | Drops a mounted boss trophy five seconds after its guardian power is selected. |
| [HammerFreedom](Landoria.HammerFreedom/) | Both | Adds server-authorized creative freedoms to Hammer worlds. |
| [FreeFlyCommand](Landoria.FreeFlyCommand/) | Both | Allows server-authorized native free-camera commands within 50 metres of the player. |
| [First Person](Landoria.FirstPerson/) | Client-only | Adds first-person view at the closest camera zoom level. |
| [ModSentry](Landoria.ModSentry/) | Both | Validates the exact client mod inventory before a server accepts a connection. |
| [Moderator](Landoria.Moderator/) | Both | Adds multiplayer moderation commands gated by server-validated administrator access. |
| [QuickLaunch](Landoria.QuickLaunch/) | Client-only | Automatically resumes the last local or multiplayer session by default. |
| [ExpandedServer](Landoria.ExpandedServer/) | Both | Raises the server player limit. |
| [Structure Protection](Landoria.StructureProtection/) | Both | Protects structures while their authorized players are offline. |
| [SealedTombstone](Landoria.SealedTombstone/) | Both | Protects tombstones and lets their owners approve access. |
| [Socialize](Landoria.Socialize/) | Both | Adds temporary groups for missions and expeditions, private messaging, map sharing, and expanded chat channels. |

## Shared library

`Landoria.SharedLib` provides common plugin infrastructure, including the
plugin base, Harmony registration, and logging. It is an internal component and
is never installed as a standalone mod.

## Discover the mods

- [Watch Landoria mod demos on YouTube](https://www.youtube.com/channel/UC7JKJ6QyDyFbWrgQGg8k5jQ).
- [Browse all Landoria mods on Thunderstore](https://thunderstore.io/c/valheim/p/Landoria/).

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).