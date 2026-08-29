# GetMyTrophyBack

Returns a boss trophy after its Sacrificial Stone power is selected.

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

## Features

- Starts a five-second return timer when a player selects a guardian power.
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
| Timer completed | The trophy drops into the world. |
| Trophy already absent | No additional trophy is created. |

## Installation

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).
All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).