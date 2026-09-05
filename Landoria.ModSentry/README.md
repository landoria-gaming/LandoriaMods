# Landoria ModSentry

ModSentry verifies that the mods installed on each client match the mods expected
by the server. This lets every player use the same mods and play under the same
conditions.

## Features

- Uses the server's `BepInEx/config/ModSentry_Required` folder to define the mods
  and library DLLs required on every client.
- Uses the server's `BepInEx/config/ModSentry_Optional` folder to define mods and
  library DLLs that are allowed but not required on clients.
- Rejects client mods and library DLLs that are listed in neither folder.
- Keeps server-only mods in the server's `BepInEx/plugins` folder; they do not
  need to be listed in `ModSentry_Required` or `ModSentry_Optional`.
- Requires every file approved by the server to have the expected version and
  SHA-256 hash.
- Automatically rejects the player when a difference is detected and shows
  an error identifying any extra, missing, or outdated mod.
- Supports an optional guest lobby for clients without ModSentry.
- Detects known ValheimTooler, ValheimHack223, and valheim-hax managed
  assemblies and lets the server kick the reporting client.

## Installation

Most of Landoria mods need to be installed also on the dedicated server, we show you in this video how to do it on windows:

[Setup a Valheim Modded dedicated server on Windows](https://youtu.be/X5J1wSRr7Uo)

| Crossplay support | Steam network support |
|---|---
| Yes | No

| Client required | Dedicated server required | Player-hosted server |
|---|---|---|
| Yes | Yes | Not supported |

## Server configuration

ModSentry does not use command-line switches. Configure its client policy with
these folders on the server:

| Folder | Purpose |
|---|---|
| `BepInEx/config/ModSentry_Required` | Contains every mod and library DLL required on clients. Include `Landoria.ModSentry.dll` here. |
| `BepInEx/config/ModSentry_Optional` | Contains mods and library DLLs that clients may use but do not need. |

Copy the approved client DLLs into the appropriate folder. ModSentry compares
their plugin identifier, version, and SHA-256 hash with each connecting client.
Any client DLL listed in neither folder is rejected.

Server-only mods stay in `BepInEx/plugins` and are not copied into either policy
folder.

Known managed cheat detection is disabled by default. Enable it on the dedicated
server with:

`--modsentry-known-cheat-protection true`

The server asks verified ModSentry clients to activate the inspection for that
connection. Use `false` or omit the switch to keep it disabled.

To accept players who do not have ModSentry, see the
[guest lobby integration guide](https://github.com/landoria-gaming/LandoriaMods/blob/main/Landoria.ModSentry/GUEST_LOBBY.md).

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions and feedback, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
