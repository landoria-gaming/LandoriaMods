# SealedTombstone

Keeps your recent tombstone safe from other players while letting you approve someone you trust to recover it.

## Video demo

[Watch SealedTombstone in action on YouTube](https://youtu.be/xWD9o9Mworg).

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

## Features

- Keeps recent tombstones locked to their owners.
- Uses a vanilla Yes/No popup for access requests.
- Permanently unlocks a tombstone after approval.
- Expires requests after 30 seconds and applies a two-minute cooldown.
- Makes tombstones public after ten in-game days.
- Blocks recent attackers from requesting access permanently.

## Access Rules

| Situation | Result |
|---|---|
| Owner approves | Tombstone permanently unlocks. |
| Owner is offline | The requester is informed immediately and no cooldown starts. |
| Owner denies or request expires | Tombstone remains locked. |
| Tombstone reaches ten days | Tombstone becomes public. |
| Requester attacked the owner before death | The request is blocked. |

## Installation

Most of Landoria mods need to be installed also on the dedicated server, we show you in this video how to do it on windows:

[Setup a Valheim Modded dedicated server on Windows](https://youtu.be/AxPDgOXEh8M)

| Crossplay support | Steam network support |
|---|---
| Yes | No

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |

Install matching versions on the server and every participating client.


## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
