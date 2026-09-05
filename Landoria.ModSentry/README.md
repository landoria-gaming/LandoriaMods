# Landoria ModSentry

ModSentry verifies that the mods installed on each client match the mods expected
by the server. This lets every player use the same mods and play under the same
conditions.

## Features

- Checks that players have the required mods and only use approved files and versions.
- Rejects connections with extra, missing, or outdated mods and explains what needs fixing.
- Detects known cheat tools and lets the server kick or ban the player (kick by default).
- Prevents players from reusing an old mod check response when connecting again. (Nonce-based server challenge.)

> **ModSentry is not a complete anti-cheat solution.** It helps players use
> the correct mods and versions and includes client-side checks for known cheat tools.
> These checks can be bypassed by a determined cheater.
> If stronger anti-cheat protection is important for your server, use a complementary server-side mod.

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

Configure the client mod policy with these folders on the server:

| Folder | Purpose |
|---|---|
| `BepInEx/config/ModSentry_Required` | Contains every mod and library DLL required on clients. Include `Landoria.ModSentry.dll` here. |
| `BepInEx/config/ModSentry_Optional` | Contains mods and library DLLs that clients may use but do not need. |

Copy the approved client DLLs into the appropriate folder. ModSentry compares
their plugin identifier, version, and SHA-256 hash with each connecting client.
Any client DLL listed in neither folder is rejected.

Server-only mods stay in `BepInEx/plugins` and are not copied into either policy
folder.

Configure known cheat detection with these dedicated server arguments:

| Server argument | Action |
|---|---|
| `--modsentry-known-cheat-protection` | `true` or `false` (default: `true`). |
| `--modsentry-known-cheat-action` | `kick` or `ban` (default: `kick`). |

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions and feedback, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
