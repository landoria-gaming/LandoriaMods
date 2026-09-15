using System;
using System.IO;
using Landoria.HuginnCam.UnityCapture;
using UnityEngine;

namespace Landoria.HuginnCam
{
    internal sealed class ScreenRecorder : MonoBehaviour
    {
        private const int FrameRate = 60;
        private readonly RecordingStatusDisplay _statusDisplay = new RecordingStatusDisplay();
        private FfmpegProcess _ffmpeg;
        private FfmpegProcess _compression;
        private UnityAudioCapture _audio;
        private MediaPipe _audioPipe;
        private UnityVideoCapture _video;
        private MediaPipe _videoPipe;
        private string _outputPath;
        private string _temporaryPath;
        private bool _waitingForAudio;
        private bool _waitingForPipe;
        private int _captureWidth;
        private int _captureHeight;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ToggleRecording();
            }

            if (_waitingForAudio && _audio.IsReady)
            {
                StartFfmpeg();
            }
            else if (_waitingForPipe)
            {
                AdvancePipeStartup();
            }

            if (_compression?.HasExited == true)
            {
                CompleteCompression();
            }
        }

        private void OnDestroy()
        {
            StopRecording();
            _statusDisplay.Dispose();
        }

        private void ToggleRecording()
        {
            if (_compression != null)
            {
                Notify("HuginnCam is still compressing the previous recording");
            }
            else if (_ffmpeg == null && !_waitingForAudio && !_waitingForPipe)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        private void StartRecording()
        {
            try
            {
                AudioListener listener = FindAnyObjectByType<AudioListener>();
                if (listener == null)
                {
                    throw new InvalidOperationException("Valheim's audio listener was not found.");
                }

                _audioPipe = new MediaPipe("HuginnCamAudio", 256);
                _videoPipe = new MediaPipe("HuginnCamVideo", 2);
                _audio = listener.gameObject.AddComponent<UnityAudioCapture>();
                _audio.Initialize(_audioPipe);
                ResolveCaptureSize();
                CreateOutputPaths();
                _waitingForAudio = true;
            }
            catch (Exception exception)
            {
                HandleStartFailure(exception);
            }
        }

        private void StartFfmpeg()
        {
            _waitingForAudio = false;
            try
            {
                _audioPipe.BeginWaitForConnection();
                _videoPipe.BeginWaitForConnection();
                _ffmpeg = FfmpegProcess.Start(
                    _videoPipe.Path,
                    _audioPipe.Path,
                    _audio.SampleRate,
                    _audio.Channels,
                    _temporaryPath,
                    _captureWidth,
                    _captureHeight,
                    FrameRate,
                    SystemInfo.graphicsDeviceVendor);
                _waitingForPipe = true;
            }
            catch (Exception exception)
            {
                HandleStartFailure(exception);
            }
        }

        private void CompleteStartup()
        {
            _waitingForPipe = false;
            _statusDisplay.Show("Recording...");
            Notify($"HuginnCam recording started at {_captureWidth}x{_captureHeight}, {FrameRate} FPS");
            HuginnCamPlugin.Log.LogInfo($"Recording started: {_outputPath}");
        }

        private void AdvancePipeStartup()
        {
            if (_videoPipe.IsConnected && _video == null)
            {
                _video = gameObject.AddComponent<UnityVideoCapture>();
                _video.StartCapture(_videoPipe, _captureWidth, _captureHeight, FrameRate);
            }

            if (_videoPipe.IsConnected && _audioPipe.IsConnected)
            {
                CompleteStartup();
            }
        }

        private void HandleStartFailure(Exception exception)
        {
            HuginnCamPlugin.Log.LogError(exception);
            StopRecording();
            Notify($"HuginnCam could not start: {exception.Message}");
        }

        private void StopRecording()
        {
            bool wasRecording = _ffmpeg != null;
            _waitingForAudio = false;
            _waitingForPipe = false;
            _statusDisplay.Hide();
            if (_video != null)
            {
                _video.StopCapture();
                Destroy(_video);
                _video = null;
            }

            if (_audio != null)
            {
                Destroy(_audio);
                _audio = null;
            }

            _audioPipe?.Dispose();
            _audioPipe = null;
            _videoPipe?.Dispose();
            _videoPipe = null;
            _ffmpeg?.Stop();
            _ffmpeg = null;
            if (wasRecording)
            {
                StartCompression();
            }
        }

        private void StartCompression()
        {
            try
            {
                _compression = FfmpegProcess.StartCompression(
                    _temporaryPath,
                    _outputPath,
                    SystemInfo.graphicsDeviceVendor);
                _statusDisplay.Show("Compressing...");
                Notify("HuginnCam is compressing the recording");
            }
            catch (Exception exception)
            {
                HuginnCamPlugin.Log.LogError(exception);
                Notify($"Compression failed; {Path.GetFileName(_temporaryPath)} was kept");
            }
        }

        private void CompleteCompression()
        {
            bool succeeded = _compression.Succeeded &&
                             File.Exists(_outputPath) &&
                             new FileInfo(_outputPath).Length > 0;
            _compression.Dispose();
            _compression = null;
            _statusDisplay.Hide();
            if (succeeded)
            {
                DeleteTemporaryRecording();
                HuginnCamPlugin.Log.LogInfo($"Recording saved: {_outputPath}");
                Notify($"HuginnCam saved {Path.GetFileName(_outputPath)} to Desktop");
                return;
            }

            HuginnCamPlugin.Log.LogWarning($"Compression failed; temporary recording kept: {_temporaryPath}");
            Notify($"Compression failed; {Path.GetFileName(_temporaryPath)} was kept");
        }

        private void DeleteTemporaryRecording()
        {
            try
            {
                File.Delete(_temporaryPath);
            }
            catch (Exception exception)
            {
                HuginnCamPlugin.Log.LogError(exception);
                Notify("MP4 saved; temporary file could not be deleted");
            }
        }

        private void CreateOutputPaths()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string baseName = $"HuginnCam_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
            _temporaryPath = Path.Combine(desktop, $"{baseName}.mkv.tmp");
            _outputPath = Path.Combine(desktop, $"{baseName}.mp4");
        }

        private void ResolveCaptureSize()
        {
            _captureWidth = Math.Max(2, Screen.width & ~1);
            _captureHeight = Math.Max(2, Screen.height & ~1);
        }

        private static void Notify(string message)
        {
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, message);
        }
    }
}
