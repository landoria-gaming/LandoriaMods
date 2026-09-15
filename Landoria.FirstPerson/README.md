# First Person

Enjoy a smooth first-person view that follows where you look.

Press F6 to enable or disable first person. The shortcut is configurable in the
BepInEx configuration and defaults to F6. First person is disabled by default,
and your choice persists across characters, worlds, servers, and game restarts.
Your body is hidden only from your own first-person view, while held items remain
visible without changing how other players see you.

While enabled, attacks and blocks temporarily use third person, then return to
first person after the configured delay. Any configured camera zoom also uses
third person for a configurable delay and sets the distance used by temporary
third-person views. Zoom controls never enter first person.

## Video demo

[Watch First Person in action on YouTube](https://youtu.be/eExAEyoNsSs).

## Highlights

- Smooth first-person movement in every direction.
- Smooth camera transitions between first and third person.
- Uses third person during primary attacks, secondary attacks, blocks, and camera zooms.
- Temporary third-person zoom stops at Valheim's native minimum camera distance.
- Nearby vegetation and helmet lighting remain stable.
- Adjustable FOV up to 120 degrees.
- Client-side only: no server installation or configuration required.

## Controls and commands

| Control | Action |
|---|---|
| `F6` (configurable) | Enable or disable automatic first-person view |
| `fov <degrees>` | Set the saved FOV, up to 120 |
| `fov` | Show the current FOV |
| `fov reset` | Restore the default FOV of 65 |

## Configuration

| Section | Setting | Default | Range | Description |
|---|---|---|---|---|
| `Camera` | `FirstPersonEnabled` | `false` | `true` or `false` | Saved first-person state |
| `Camera` | `FieldOfView` | `65` | `65` to `120` | Saved camera FOV |
| `Camera` | `HeadBobMultiplier` | `2` | `0` to `3` | First-person head bob strength; `0` disables it |
| `Controls` | `ToggleShortcut` | `F6` | Unity `KeyCode` | First-person toggle shortcut |
| `Transitions` | `CombatReturnDelay` | `1` | `0` or higher | Delay after combat ends; `0` disables temporary third person for combat |
| `Transitions` | `ZoomReturnDelay` | `3` | `0` or higher | Delay after camera zoom; `0` disables temporary third person for zoom |

## Valheim compatibility

Current release: 1.0.x

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
