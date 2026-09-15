# HuginnCam design

## Vision

HuginnCam turns a second in-game camera into an autonomous cinematic companion. The player keeps full control of the normal gameplay camera while HuginnCam follows the local character, composes varied shots, avoids blocked views, and provides a live preview of the footage being captured.

The camera should feel like a deliberate operator, not a randomly moving drone. It should hold readable shots, anticipate movement, and transition only when a new angle improves the footage.

## Intended experience

- Toggle HuginnCam without interrupting gameplay.
- See the cinematic output in a small preview in the lower-left corner.
- Keep the preview independent from the captured image so it never appears recursively in the recording.
- Let an automatic director select and hold shots for meaningful durations.
- Keep the local player framed and visible whenever a valid camera position exists.
- Save a video from the cinematic camera rather than from the gameplay camera.

## Camera system

The mod will create a secondary Unity camera that renders to its own `RenderTexture`. The gameplay camera remains untouched. The same render texture will feed the preview and, later, the recording pipeline.

The camera controller will generate candidate positions around the player and score them using:

- visibility of the player and important nearby subjects;
- distance, height, and viewing angle;
- the player's velocity and facing direction;
- proximity to terrain, vegetation, structures, and other obstacles;
- continuity with the current shot;
- suitability for the current activity.

Sphere casts and line-of-sight tests will reject obstructed positions. If the current view becomes blocked, the camera will first adjust locally, then move closer, and finally select a different shot when necessary.

## Automatic director

The director will choose from a small set of shot profiles:

| Shot | Purpose |
|---|---|
| Wide follow | Show travel and the surrounding landscape. |
| Rear tracking | Follow movement from behind and above. |
| Side tracking | Add motion and variety during travel. |
| Overhead | Show navigation, construction, or groups of enemies. |
| Close-up | Emphasize combat, interactions, emotes, or quiet moments. |
| Orbit | Circle a mostly stationary player or point of interest. |

Each shot will define preferred distance, height, field of view, duration, and motion limits. Transitions will use damped position, rotation, and field-of-view changes. Cooldowns and minimum shot durations will prevent restless cutting.

## Preview

The first user interface will be a configurable picture-in-picture panel in the lower-left corner. It should support:

- show and hide controls;
- configurable size, margin, and screen corner;
- the exact cinematic camera framing;
- optional shot name, recording duration, and `REC` indicator;
- no Valheim HUD in the cinematic render.

The initial preview target is 320×180. Recording resolution must remain independent from preview resolution.

## Recording

The initial recorder captures the normal game view into a Unity render texture, reads it back asynchronously, and sends raw frames to FFmpeg. Unity's audio filter callback provides the Valheim audio mix. FFmpeg must be installed separately and configured with `FfmpegPath` or available through `PATH`.

Recordings use a lossless `*.mkv.tmp` file on the Desktop as a crash-resistant temporary Matroska container. NVIDIA systems use lossless H.264 4:4:4 hardware encoding, while other systems use FFV1. Audio is stored as lossless 32-bit float PCM. A normal stop asynchronously compresses the video to an H.265 MP4 with hardware acceleration, encodes the audio once as 320-kbit/s AAC, and removes the temporary file only after the MP4 is verified as non-empty.

When the secondary camera is implemented, recording will move to asynchronous GPU readback from its render target while keeping the same FFmpeg encoding process and Unity audio capture.

The recording implementation must:

- avoid blocking the main Unity thread;
- use a bounded frame queue;
- report dropped frames and encoder failures;
- finalize files safely after stopping;
- keep encoder binaries and licensing explicit;
- allow recording resolution and frame rate to be configured.

## Development stages

1. Record the normal gameplay view and Windows output audio through a separately installed FFmpeg executable.
2. Render a fixed secondary camera into a lower-left preview.
3. Follow the local player with smooth position and rotation.
4. Add collision avoidance and line-of-sight recovery.
5. Add shot profiles and automatic direction.
6. Add configuration, controls, and preview indicators.
7. Prototype video-only recording and measure performance.
8. Decide how game audio should be captured and synchronized.

## Initial non-goals

- Replacing or moving the gameplay camera.
- Server-authoritative camera behavior.
- Recording other players without the local player's camera active.
- Shipping an encoder before its distribution and licensing model is decided.
- Guaranteeing that every scene has an unobstructed cinematic angle.
