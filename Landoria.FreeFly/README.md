# FreeFly

Gives players a controlled client-side free camera for exploring and filming Valheim without opening developer or debug modes.

## Video demo

[Watch FreeFly in action on YouTube](https://youtu.be/smoOkcAPKr0).

## Valheim compatibility

Current release: 1.0.x

## Features

- Toggles free fly with F7 by default.
- Starts in front of and slightly above the player, rotating around obstacles to find a clear position.
- Uses a continuous two-second exit that starts approaching as soon as the player enters view, then restores the previous camera orientation.
- Hides the interface during free fly and restores its previous state afterward.
- Sets free-camera smoothing to 0.25 on the first free-fly activation, then preserves changes made with `ffsmooth`.
- Sets free-camera speed to 4 m/s whenever free fly starts.
- Lets players adjust smoothing with `ffsmooth` and field of view with Valheim's `fov <degrees>` command.
- Limits free-camera movement to between 2 and 10 metres per second.
- Uses a one-metre-radius collision sphere to prevent the free camera from passing through terrain and solid objects.
- Limits the camera to 20 metres from the player.

## Controls and commands

| Control | Action |
|---|---|
| `F7` (configurable) | Enable or disable free fly |
| `Escape` | Exit free fly without opening the menu |
| `ffsmooth <0-1>` | Set free-camera smoothing |

## Installation

| Crossplay support | Steam network support |
|---|---
| Yes | Yes

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | No | Supported |


## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
