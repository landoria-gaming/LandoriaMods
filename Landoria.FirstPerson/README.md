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

| Section | Setting | Default | Range | Description |
|---|---|---|---|---|
| `Controls` | `ToggleShortcut` | `F6` | Unity `KeyCode` | First-person toggle shortcut. May be changed to `Mouse2` or `Mouse3` for example |
| `Transitions` | `CombatReturnDelay` | `1` | `0` or higher | Delay after combat ends; `0` disables temporary third person for combat |
| `Transitions` | `ZoomReturnDelay` | `3` | `0` or higher | Delay after camera zoom; `0` disables temporary third person for zoom |
| `Camera` | `HeadBobMultiplier` | `2` | `0` to `3` | First-person head bob strength; `0` disables it |
| `Camera` | `FirstPersonEnabled` | `false` | `true` or `false` | Saved first-person state. Normally changed using F6. |
| `Camera` | `FieldOfView` | `65` | `65` to `120` | Saved camera FOV. Normally changed using /fov command in-game. |
## Valheim compatibility

Current release: 1.0.x

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
