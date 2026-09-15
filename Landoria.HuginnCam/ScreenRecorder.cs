using System;
using System.Collections;
using System.IO;
using Landoria.UnityMediaRecorder;
using UnityEngine;
using MediaRecorder = Landoria.UnityMediaRecorder.UnityMediaRecorder;

namespace Landoria.HuginnCam
{
    // Connects HuginnCam input, camera behavior and status UI to the reusable recorder.
    internal sealed class ScreenRecorder : MonoBehaviour
    {
        private const int FrameRate = 60;
        private const float CameraWarmupSeconds = 3f;
        private const float ScreenshotWarmupSeconds = 3f;
        private readonly RecordingStatusDisplay _statusDisplay = new RecordingStatusDisplay();
        private CinematicCameraRig _cameraRig;
        private MediaRecorder _recorder;
        private CinematicCameraRig _screenshotRig;
        private Coroutine _screenshotRoutine;
        private string _archivePath;
        private string _outputPath;
        private string _temporaryContainerPath;
        private Coroutine _warmupRoutine;

        // Handles the recording hotkey without knowing the media pipeline internals.
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                StartScreenshot();
                return;
            }

            if (!Input.GetKeyDown(KeyCode.F8))
            {
                return;
            }

            if (_warmupRoutine != null)
            {
                CancelWarmup();
            }
            else if (_recorder?.IsFinalizing == true)
            {
                Notify("HuginnCam is still finalizing the previous recording");
            }
            else if (_recorder?.IsCapturing == true)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        // Stops active work and releases camera and display resources on destruction.
        private void OnDestroy()
        {
            if (_recorder != null)
            {
                UnsubscribeRecorder();
                _recorder.StopRecording();
                Destroy(_recorder);
                _recorder = null;
            }

            ReleaseCameraRig();
            ReleaseScreenshotRig();
            _statusDisplay.Dispose();
        }

        // Creates and warms an independent camera for one full-resolution PNG frame.
        private void StartScreenshot()
        {
            if (_screenshotRoutine != null)
            {
                Notify("HuginnCam is already preparing a screenshot");
                return;
            }

            try
            {
                Camera gameplayCamera = Camera.main;
                if (gameplayCamera == null)
                {
                    throw new InvalidOperationException("Valheim's gameplay camera was not found.");
                }

                _screenshotRig = gameObject.AddComponent<CinematicCameraRig>();
                _screenshotRig.Initialize(gameplayCamera, false, true, CinematicCameraAngle.Behind);
                int width = Math.Max(2, Screen.width & ~1);
                int height = Math.Max(2, Screen.height & ~1);
                _screenshotRig.BeginWarmup(width, height);
                _screenshotRoutine = StartCoroutine(CaptureScreenshotSet());
                Notify("HuginnCam is preparing comparison and cinematic screenshots");
            }
            catch (Exception exception)
            {
                HandleScreenshotFailed(exception);
            }
        }

        // Captures a matched comparison pair followed by three cinematic viewpoints.
        private IEnumerator CaptureScreenshotSet()
        {
            CinematicCameraAngle[] angles =
            {
                CinematicCameraAngle.Behind,
                CinematicCameraAngle.Front,
                CinematicCameraAngle.RightSide
            };

            int width = Math.Max(2, Screen.width & ~1);
            int height = Math.Max(2, Screen.height & ~1);
            string identifier = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            _screenshotRig.SetFollowPlayer(false);
            float referenceDeadline = Time.realtimeSinceStartup + ScreenshotWarmupSeconds;
            while (Time.realtimeSinceStartup < referenceDeadline)
            {
                yield return null;
            }

            yield return new WaitForEndOfFrame();
            if (!TrySaveScreenshot("Reference", identifier) || !TrySaveGameplayReference(identifier))
            {
                _screenshotRoutine = null;
                ReleaseScreenshotRig();
                yield break;
            }

            _screenshotRig.SetFollowPlayer(true);
            foreach (CinematicCameraAngle angle in angles)
            {
                _screenshotRig.SetAngle(angle);
                float deadline = Time.realtimeSinceStartup + ScreenshotWarmupSeconds;
                while (Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                yield return new WaitForEndOfFrame();
                if (!TrySaveScreenshot(angle.ToString(), identifier))
                {
                    _screenshotRoutine = null;
                    ReleaseScreenshotRig();
                    yield break;
                }
            }

            _screenshotRoutine = null;
            ReleaseScreenshotRig();
            Notify($"HuginnCam saved five {width}x{height} screenshots to Videos");
        }

        // Writes one rendered cinematic angle to the configured screenshot directory.
        private bool TrySaveScreenshot(string label, string identifier)
        {
            try
            {
                string screenshotDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    "NVIDIA",
                    "Valheim");
                Directory.CreateDirectory(screenshotDirectory);
                string path = Path.Combine(
                    screenshotDirectory,
                    $"HuginnCam_{identifier}_{label}.png");
                File.WriteAllBytes(path, _screenshotRig.EncodeStillFrame());
                HuginnCamPlugin.Log.LogInfo($"Cinematic screenshot saved: {path}");
                return true;
            }
            catch (Exception exception)
            {
                HuginnCamPlugin.Log.LogError(exception);
                Notify($"HuginnCam screenshot failed: {exception.Message}");
                return false;
            }
        }

        // Saves the gameplay screen from the same frame as the aligned HuginnCam reference.
        private static bool TrySaveGameplayReference(string identifier)
        {
            Texture2D gameplayImage = null;
            try
            {
                string screenshotDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    "NVIDIA",
                    "Valheim");
                string path = Path.Combine(
                    screenshotDirectory,
                    $"GameplayReference_{identifier}.png");
                gameplayImage = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(path, gameplayImage.EncodeToPNG());
                HuginnCamPlugin.Log.LogInfo($"Gameplay reference saved: {path}");
                return true;
            }
            catch (Exception exception)
            {
                HuginnCamPlugin.Log.LogError(exception);
                Notify($"HuginnCam gameplay reference failed: {exception.Message}");
                return false;
            }
            finally
            {
                if (gameplayImage != null)
                {
                    Destroy(gameplayImage);
                }
            }
        }

        // Reports a screenshot failure and releases its camera resources.
        private void HandleScreenshotFailed(Exception exception)
        {
            HuginnCamPlugin.Log.LogError(exception);
            Notify($"HuginnCam screenshot failed: {exception.Message}");
            ReleaseScreenshotRig();
        }

        // Stops pending screenshot work and destroys its independent camera.
        private void ReleaseScreenshotRig()
        {
            if (_screenshotRoutine != null)
            {
                StopCoroutine(_screenshotRoutine);
                _screenshotRoutine = null;
            }

            if (_screenshotRig == null)
            {
                return;
            }

            _screenshotRig.Dispose();
            Destroy(_screenshotRig);
            _screenshotRig = null;
        }

        // Creates the cinematic camera and starts the reusable Unity recorder.
        private void StartRecording()
        {
            try
            {
                Camera gameplayCamera = Camera.main;
                if (gameplayCamera == null)
                {
                    throw new InvalidOperationException("Valheim's gameplay camera was not found.");
                }

                CreateOutputPaths();
                _cameraRig = gameObject.AddComponent<CinematicCameraRig>();
                _cameraRig.Initialize(gameplayCamera);
                int width = Math.Max(2, Screen.width & ~1);
                int height = Math.Max(2, Screen.height & ~1);
                _cameraRig.BeginWarmup(width, height);
                _statusDisplay.Show("Preparing camera...");
                _warmupRoutine = StartCoroutine(StartRecorderAfterWarmup());
            }
            catch (Exception exception)
            {
                HandleRecordingFailed(exception);
            }
        }

        // Waits for independent auto-exposure to settle before opening the media streams.
        private IEnumerator StartRecorderAfterWarmup()
        {
            float deadline = Time.realtimeSinceStartup + CameraWarmupSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            _warmupRoutine = null;
            try
            {
                _recorder = gameObject.AddComponent<MediaRecorder>();
                SubscribeRecorder();
                _recorder.StartRecording(
                    _cameraRig.Camera,
                    _cameraRig.Listener,
                    CreateSettings(),
                    _cameraRig.PreparedTarget);
            }
            catch (Exception exception)
            {
                HandleRecordingFailed(exception);
            }
        }

        // Cancels camera preparation before any output process has been started.
        private void CancelWarmup()
        {
            StopCoroutine(_warmupRoutine);
            _warmupRoutine = null;
            _statusDisplay.Hide();
            ReleaseCameraRig();
            Notify("HuginnCam recording cancelled");
        }

        // Stops capture while allowing the recorder to create the final files.
        private void StopRecording()
        {
            _recorder?.StopRecording();
            ReleaseCameraRig();
        }

        // Builds generic recording settings from HuginnCam configuration and paths.
        private RecordingSettings CreateSettings()
        {
            return new RecordingSettings
            {
                FfmpegPath = HuginnCamPreference.FfmpegPath,
                TemporaryContainerPath = _temporaryContainerPath,
                ArchivePath = _archivePath,
                OutputPath = _outputPath,
                Width = Math.Max(2, Screen.width & ~1),
                Height = Math.Max(2, Screen.height & ~1),
                MaximumFrameRate = FrameRate,
                FlipVertically = true
            };
        }

        // Creates timestamped HuginnCam output paths on the Desktop.
        private void CreateOutputPaths()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string baseName = $"HuginnCam_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
            _temporaryContainerPath = Path.Combine(desktop, $"{baseName}.mkv.tmp");
            _archivePath = Path.Combine(desktop, $"{baseName}.mkv");
            _outputPath = Path.Combine(desktop, $"{baseName}.mp4");
        }

        // Connects recorder lifecycle events to HuginnCam presentation behavior.
        private void SubscribeRecorder()
        {
            _recorder.CaptureStarted += HandleCaptureStarted;
            _recorder.FinalizationStarted += HandleFinalizationStarted;
            _recorder.RecordingCompleted += HandleRecordingCompleted;
            _recorder.RecordingFailed += HandleRecordingFailed;
        }

        // Disconnects recorder lifecycle events before destroying the component.
        private void UnsubscribeRecorder()
        {
            _recorder.CaptureStarted -= HandleCaptureStarted;
            _recorder.FinalizationStarted -= HandleFinalizationStarted;
            _recorder.RecordingCompleted -= HandleRecordingCompleted;
            _recorder.RecordingFailed -= HandleRecordingFailed;
        }

        // Displays the active recording state after the media pipes connect.
        private void HandleCaptureStarted()
        {
            _statusDisplay.Show("Recording...");
            Notify($"HuginnCam recording started at {Screen.width}x{Screen.height}, {FrameRate} FPS");
            HuginnCamPlugin.Log.LogInfo($"Recording started: {_outputPath}");
        }

        // Displays the background MP4 finalization state after capture stops.
        private void HandleFinalizationStarted()
        {
            _statusDisplay.Show("Finalizing...");
            Notify("HuginnCam is finalizing the recording");
        }

        // Reports successful output creation and releases the recorder façade.
        private void HandleRecordingCompleted()
        {
            _statusDisplay.Hide();
            HuginnCamPlugin.Log.LogInfo($"Recording saved: {_outputPath}");
            HuginnCamPlugin.Log.LogInfo($"Archive recording saved: {_archivePath}");
            Notify("HuginnCam saved the MP4 and archive recording to Desktop");
            ReleaseRecorder();
        }

        // Reports a recorder failure while preserving any recoverable temporary file.
        private void HandleRecordingFailed(Exception exception)
        {
            _statusDisplay.Hide();
            HuginnCamPlugin.Log.LogError(exception);
            Notify($"HuginnCam recording failed: {exception.Message}");
            ReleaseCameraRig();
            ReleaseRecorder();
        }

        // Restores the gameplay audio listener and destroys the cinematic camera.
        private void ReleaseCameraRig()
        {
            if (_warmupRoutine != null)
            {
                StopCoroutine(_warmupRoutine);
                _warmupRoutine = null;
            }

            if (_cameraRig == null)
            {
                return;
            }

            _cameraRig.Dispose();
            Destroy(_cameraRig);
            _cameraRig = null;
        }

        // Disconnects and destroys the completed recorder component.
        private void ReleaseRecorder()
        {
            if (_recorder == null)
            {
                return;
            }

            UnsubscribeRecorder();
            Destroy(_recorder);
            _recorder = null;
        }

        // Displays a short HuginnCam notification through Valheim's message HUD.
        private static void Notify(string message)
        {
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, message);
        }
    }
}
