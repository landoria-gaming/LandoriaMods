# AfkDetector

Disconnects players who remain motionless and silent beyond a configurable timeout.

## Video demo

[Watch AfkDetector in action on YouTube](https://youtu.be/VzsUbmJs5QA).

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

## Features

- Lets the server decide when a player is inactive.
- Resets the timer when the player moves or chats.
- Ignores tiny position changes so harmless character jitter does not count as activity.
- Clearly explains when a player was disconnected for inactivity.
- Defaults to a 30-minute timeout.
- Reads `--afktimeout <minutes>` from the server command line; `-1` disables detection.
- Works with CharacterVault to finish saving the character before an inactive player disconnects.

## Installation

Most of Landoria mods need to be installed also on the dedicated server, we show you in this video how to do it on windows:

[Setup a Valheim Modded dedicated server on Windows](https://youtu.be/X5J1wSRr7Uo)

| Crossplay support | Steam network support |
|---|---
| Yes | No

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |

The client component only displays the server-provided disconnect reason.

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
