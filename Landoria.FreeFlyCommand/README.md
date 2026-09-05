# FreeFlyCommand

Gives authorized players a controlled free camera for exploring and filming Valheim without opening developer or debug modes.

## Video demo

[Watch FreeFlyCommand in action on YouTube](https://youtu.be/smoOkcAPKr0).

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

## Features

- Keeps the free camera unavailable until the connected server authorizes it.
- Smooths camera movement automatically when free camera mode starts.
- Lets authorized players adjust smoothing with `ffsmooth` and field of view with Valheim's `fov <degrees>` command.
- Limits free-camera movement to 20 metres per second.
- Uses a one-metre-radius collision sphere to prevent the free camera from passing through terrain and solid objects.
- Limits the camera to 50 metres from the player.

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
