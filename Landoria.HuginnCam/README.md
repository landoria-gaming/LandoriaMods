# HuginnCam

HuginnCam records Valheim gameplay and is planned to become an autonomous cinematic follow camera. The first version records the normal game view at the current screen resolution.

## Current features

- Press F8 to start recording and press F8 again to stop.
- Show `Recording...` in white below the minimap while recording is active.
- Capture the normal game view directly from Unity, including the HUD, at the active game resolution.
- Save recordings automatically to the Windows Desktop as H.264 MP4 files.
- Record at up to 60 frames per second using an available hardware encoder.
- Use a high-quality H.264 profile intended for editing and archival footage.
- Record the audio played through the current Windows output device.
- Run entirely on the client without a server installation.

FFmpeg must be installed separately. Set its full path in `Landoria.HuginnCam.cfg`, or leave the setting empty to resolve `ffmpeg.exe` from the Windows `PATH`.

HuginnCam records to the Desktop as a lossless `*.mkv.tmp` file for crash resilience. NVIDIA systems use lossless H.264 4:4:4 hardware encoding; other systems use lossless FFV1. Audio remains lossless 32-bit float PCM in this intermediate file. After recording stops, HuginnCam asynchronously compresses the video to a very-high-quality H.265 MP4 at CQ 16 and encodes the final audio once as 320-kbit/s AAC. The temporary file is deleted only after a non-empty MP4 is created successfully.

Audio is captured directly from Valheim's Unity audio mix as 32-bit float samples before FFmpeg encodes it to AAC.

Example:

```ini
[Recording]
FfmpegPath = C:\src\ffmpeg-9.0.1\bin\ffmpeg.exe
```

## Planned highlights

- Follow the local player from cinematic angles.
- Keep the player visible by detecting terrain, trees, and structures.
- Switch smoothly between wide shots, tracking shots, close-ups, and overhead views.
- Show the recorded view in a small in-game preview.
- Record the future cinematic camera without changing the gameplay view.

## Controls

| Control | Action |
|---|---|
| `F8` | Start or stop recording |

## BepInEx configuration

| Setting | Default | Description |
|---|---|---|
| `FfmpegPath` | empty | Full path to `ffmpeg.exe`; an empty value uses the Windows `PATH`. |

See [DESIGN.md](DESIGN.md) for the intended behavior and development stages.

## Valheim compatibility

Target: Valheim 1.0.x

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
