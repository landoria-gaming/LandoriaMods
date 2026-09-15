# Changelog

## Unreleased

- Add continuous figure-eight first-person camera movement matched to vanilla movement paces.
- Stop first-person camera rotation from accelerating while the game menu is open.
- Add F6 to enable automatic first-person view and restore the saved third-person distance when disabled.
- Show First Person or Third Person when F6 changes the camera mode.
- Let any configured camera zoom choose the temporary third-person combat distance.
- Temporarily switch to third person after any configured camera zoom.
- Return to first person three seconds after the last configured camera zoom.
- Prevent zoom-in controls from entering first person.
- Limit temporary zoom-in to Valheim's native third-person minimum distance.
- Apply temporary zoom limits before camera positioning to prevent a close-view flash.
- Use a 0.09-metre near clip plane in first person.
- Return to first person 0.5 seconds after the actual vanilla attack or block state ends.
- Smooth transitions between first- and third-person camera distances.
- Include the final backward first-person offset in camera transitions.
- Make camera transition acceleration and deceleration more progressive.
- Blend Valheim's native camera offsets to remove the final first-person transition stutter.

## 1.0.9

 - Bug fix: Keep Valheim’s native camera position smoothing to prevent stuttering during sideways movement.

## 1.0.8

- Migration to Valheim 1.0.x.

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
