# Changelog

## 1.0.4

- Updated README.

## 1.0.3

- Add the First Person video demo to the package documentation.

## 1.0.2

- Keep items held in either hand visible while hiding the local player's body in first person.
- Reject configured FOV values above 85 without changing the current or saved FOV.
- Add 15 degrees to the effective FOV only while first person is active, up to 100.

## 1.0.1

- Lock the camera to the player's animated head position and turn the body with horizontal camera movement.
- Keep the first-person camera exactly at the animated eye point along the full look direction.
- Hide the local player body and equipped items in first person.
- Keep animated equipment transforms active so attached lights follow vertical look movement.
- Stabilize helmet lights at the first-person camera and suppress their local flicker and movement.
- Reapply local renderer hiding after complete character visual updates.
- Restore the complete local character outside first person.
- Keep the vanilla `fov` command active in first-person and third-person gameplay, save the FOV and first-person toggle in the local mod configuration, add `fov reset`, and cap the FOV at 90.

## 1.0.0

- Add toggleable first-person view at minimum camera zoom.
