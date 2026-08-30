# Structure Protection

Prevents creatures from deliberately targeting structures whose creator is offline and blocks player damage to structures inside active wards.

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

## Features

- Creatures cannot deliberately target player-built structures while their creator is offline.
- An active ward blocks damage to structures from players who are neither the ward creator nor on its permitted-player list.
- The ward creator and players on its permitted-player list can still damage protected structures.
- Protection against damage from unauthorized players remains active while the ward creator or a permitted player is online.
- Each character can have up to five wards in the world by default.
- Active wards are disabled after 30 real calendar days since their creator was last seen online.

Dedicated-server administrators can configure the protections with these command-line switches:

- `--structure-protection-offline-creature-targeting true|false`
- `--structure-protection-ward-player-damage true|false`
- `--structure-protection-max-wards-per-character <number>`: defaults to `5`; use `-1` to disable the ward limit.
- `--structure-protection-ward-inactivity-days <number>`: defaults to `30`; use `-1` to disable inactivity checks.
- `--structure-protection-ward-inactivity-check-hours <number>`: defaults to `6` and must be at least `1`.

## Installation

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |

Install matching versions of Structure Protection on the server and every participating client.


## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).