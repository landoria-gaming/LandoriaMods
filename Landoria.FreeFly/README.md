# FreeFly

Gives players a controlled client-side free camera for exploring and filming Valheim without opening developer or debug modes.

## Video demo

[Watch FreeFly in action on YouTube](https://youtu.be/smoOkcAPKr0).

## Valheim compatibility

Current release: 1.0.x

## Features

- Toggles free fly with F7 by default.
- Starts in front of and slightly above the player, rotating around obstacles to find a clear position.
- Uses one smooth three-second exit that moves continuously toward the saved camera position, faces the player during the first 1.5 seconds, and restores the saved rotation during the last second.
- Hides the interface during free fly and restores its previous state afterward.
- Sets free-camera smoothing to 0.25 on the first free-fly activation, then preserves changes made with `ffsmooth`.
- Smoothly boosts free-camera speed from 4 to 10 m/s while either Shift key is held, then returns it to 4 m/s when released.
- Lets players adjust smoothing with `ffsmooth` and field of view with Valheim's `fov <degrees>` command.
- Limits free-camera movement to between 2 and 10 metres per second.
- Uses a one-metre-radius collision sphere to prevent the free camera from passing through terrain and solid objects.
- Limits the camera to 50 metres from the player.

## Controls and commands

| Control | Action |
|---|---|
| `F7` (configurable) | Enable or disable free fly |
| `Escape` | Exit free fly without opening the menu |
| `Left Shift` or `Right Shift` | Smoothly boost speed while held |
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
