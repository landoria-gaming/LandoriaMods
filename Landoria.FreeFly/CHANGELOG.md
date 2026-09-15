# Changelog

## 1.0.0

- Add a configurable F7 shortcut to toggle free fly.
- Hide the interface during free fly and restore it afterward.
- Set free-camera smoothing to 0.25 only on the first free-fly activation.
- Replace mouse-wheel speed changes with a smooth Shift boost from 4 to 10 m/s.
- Exit free fly with Escape without showing toggle messages.
- Prevent Escape from opening the menu when it exits free fly.
- Rotate around the player to find a clear starting position without reducing camera distance.
- Keep the initial free-fly camera rotation pointed at the player.
- Smooth camera movement when entering and leaving free fly.
- Restore the previous camera mode only after the exit transition finishes.
- Use a smooth three-second exit that moves continuously while blending its initial and final rotations.
- Limit free fly to 50 metres from the player.
