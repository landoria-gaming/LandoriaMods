# GetMyTrophyBack

Returns a boss trophy after its Sacrificial Stone power is selected.

## Video demo

[Watch GetMyTrophyBack in action on YouTube](https://youtu.be/rX5TVIiGaNc).

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

## Features

- Starts a five-second return timer when a player selects a guardian power.
- Makes the trophy removable fifteen seconds after targeting it when its guardian power is already active.
- Shows the normal use prompt instead of the inactive current-power label.
- Drops the mounted trophy as a recoverable world item.
- Preserves the trophy's stored item data.
- Uses the stone's network owner for an authoritative synchronized drop.
- Prevents simultaneous requests from duplicating a trophy.
- Supports modded guardian stones built on vanilla `ItemStand` powers.

## Behavior

| Event | Result |
|---|---|
| Trophy mounted | Its guardian power remains available. |
| Power selected | The five-second return timer starts. |
| Power already active | Targeting the stone starts a fifteen-second delay, then the use prompt appears. |
| Active-power prompt visible | Interacting again drops the trophy immediately. |
| Timer completed | The trophy drops into the world. |
| Trophy already absent | No additional trophy is created. |

## Installation

Most of Landoria mods need to be installed also on the dedicated server, we show you in this video how to do it on windows:

[Setup a Valheim Modded dedicated server on Windows](https://youtu.be/X5J1wSRr7Uo)

| Crossplay support | Steam network support |
|---|---
| Yes | No

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
