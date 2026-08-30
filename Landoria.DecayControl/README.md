# DecayControl

Controls how rain affects player-built structures and how quickly their fires and torches consume fuel.

## Features

- Controls building wear and fuel use independently.
- Can keep the original Valheim behavior, pause each effect while the builder is offline,
  or disable it entirely for player-built structures.
- In `player-online` mode, only the original builder's connection matters; joining a group
  does not change the behavior.

## Dedicated server settings

| Switch | Values | Default |
|---|---|---|
| `--decay-control-fuel-consumption` | `default`, `player-online`, `disabled` | `default` |
| `--decay-control-environmental-building-wear` | `default`, `player-online`, `disabled` | `default` |

Settings are read once by the dedicated server and sent to clients after player spawn.
They are not BepInEx settings. `default` keeps the original Valheim behavior,
`player-online` requires the builder to be connected, and `disabled` stops the effect
for player-built structures.

## Installation

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |

Install matching versions of DecayControl on the server and every participating client.
DecayControl has no dependency on Socialize.


## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use
[GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).