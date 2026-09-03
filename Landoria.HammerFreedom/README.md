# HammerFreedom

Lets players build freely in authorized Hammer worlds with flight, unlimited stamina,
fall protection, and lasting equipment—without granting administrator access.

## Video demo

[Watch HammerFreedom's Fly mode in action on YouTube](https://www.youtube.com/watch?v=PnJOJYb4LwA).

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

## Features

- Makes every creative ability available only when the connected server authorizes it.
- Provides separate `--hammerfreedom-fly`, `--hammerfreedom-fall-damage-immunity`, and
  `--hammerfreedom-unlimited-stamina` server switches, plus
  `--hammerfreedom-no-durability-loss`; each is disabled by default.
- Works only in worlds using the required Hammer modifiers.
- Prevents all fall damage, regardless of fall height, when authorized.
- Prevents all stamina use when authorized, regardless of the action.
- Prevents durability loss for tools, weapons, shields, armor, and other durable equipment.
- Removes these creative abilities when moving to a server that does not authorize them.
- Shows the `fly` command only when it is available.
- Supports `fly`, `fly on`, `fly off`, and the fixed native `Z` toggle shortcut.
- Limits flight to 4 metres per second normally and 7 metres per second while sprinting.
- Keeps vanilla movement: Space ascends, Left Control descends, and Shift increases speed.
- Prevents Space from jumping and Left Control from crouching while flying.

## Installation

Most of Landoria mods need to be installed also on the dedicated server, we show you in this video how to do it on windows:

[Setup a Valheim Modded dedicated server on Windows](https://youtu.be/AxPDgOXEh8M)

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
