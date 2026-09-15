# First Person

Enjoy a smooth first-person view that follows where you look.

## Video demo

[Watch First Person in action on YouTube](https://youtu.be/eExAEyoNsSs).

## Highlights

- Toggle between first and third person camera with the default F6 key.
- F6 can be changed to another key or mouse button in the BepInEx config file.
- Instantly switch between first and third person during combat.
- Customize your field of view (FOV) in-game from 65 to 120.
- Client-side mod; no server installation needed.

## Controls

| Control | Action |
|---|---|
| `F6` | Enable or disable first-person view |

## Commands

| Command | Action |
|---|---|
| `fov <degrees>` | Set the saved FOV, up to 120 (default 65) |
| `fov` | Show the current FOV |
| `fov reset` | Restore the default FOV of 65 |

## BepInEx Configuration

The file `Landoria.FirstPerson.cfg` is created automatically in the config folder of the bepinex current profile containing the following settings:

| Setting | Default | Description |
|---|---|---|
| `ToggleShortcut` | `F6` | First-person toggle shortcut. May be changed to `Mouse2` or `Mouse3` for example |
| `CombatReturnDelay` | `1` | Return to first person delay in seconds after combat ends; `0` disables temporary third person for combat |
| `ZoomReturnDelay` | `3` | Return to first person delay in seconds after camera zoom; `0` disables temporary third person for zoom |
| `HeadBobStrength` | `2` | First-person head bob strength; `0` disables it |
| `FirstPersonEnabled` | `false` | Saved first-person state. Normally changed using F6. |
| `FieldOfView` | `65` | Saved camera FOV. Normally changed using /fov command in-game. |

## Valheim compatibility

Current release: 1.0.x

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
