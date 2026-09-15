using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Landoria.HuginnCam.UnityCapture
{
    internal sealed class UnityVideoCapture : MonoBehaviour
    {
        private const int MaximumPendingReadbacks = 2;
        private MediaPipe _pipe;
        private RenderTexture _source;
        private RenderTexture _target;
        private Coroutine _captureRoutine;
        private int _pendingReadbacks;
        private bool _active;

        internal void StartCapture(MediaPipe pipe, int width, int height, int frameRate)
        {
            _pipe = pipe;
            if (Screen.width != width || Screen.height != height)
            {
                _source = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                _source.Create();
            }

            _target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _target.Create();
            _active = true;
            _captureRoutine = StartCoroutine(CaptureFrames(frameRate));
        }

        internal void StopCapture()
        {
            _active = false;
            if (_captureRoutine != null)
            {
                StopCoroutine(_captureRoutine);
                _captureRoutine = null;
            }

            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
                _target = null;
            }

            if (_source != null)
            {
                _source.Release();
                Destroy(_source);
                _source = null;
            }
        }

        private IEnumerator CaptureFrames(int frameRate)
        {
            var endOfFrame = new WaitForEndOfFrame();
            float interval = 1f / frameRate;
            float nextCapture = Time.realtimeSinceStartup;
            while (_active)
            {
                yield return endOfFrame;
                float now = Time.realtimeSinceStartup;
                if (now >= nextCapture && _pendingReadbacks < MaximumPendingReadbacks)
                {
                    CaptureFrame();
                    nextCapture = now + interval;
                }
            }
        }

        private void CaptureFrame()
        {
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_source ?? _target);
            if (_source != null)
            {
                Graphics.Blit(_source, _target);
            }
            _pendingReadbacks++;
            AsyncGPUReadback.Request(_target, 0, TextureFormat.RGBA32, CompleteReadback);
        }

        private void CompleteReadback(AsyncGPUReadbackRequest request)
        {
            _pendingReadbacks--;
            if (!_active || request.hasError)
            {
                return;
            }

            try
            {
                _pipe.Write(request.GetData<byte>().ToArray());
            }
            catch (Exception exception)
            {
                HuginnCamPlugin.Log.LogError(exception);
            }
        }
    }
}
