using System;
using UnityEngine;

namespace Landoria.HuginnCam.UnityCapture
{
    internal sealed class UnityAudioCapture : MonoBehaviour
    {
        private MediaPipe _pipe;
        private volatile int _channels;

        internal int Channels => _channels;
        internal int SampleRate => AudioSettings.outputSampleRate;
        internal bool IsReady => _channels > 0;

        internal void Initialize(MediaPipe pipe)
        {
            _pipe = pipe;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            _channels = channels;
            if (_pipe == null || !_pipe.IsConnected)
            {
                return;
            }

            try
            {
                byte[] bytes = new byte[data.Length * sizeof(float)];
                Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
                _pipe.Write(bytes);
            }
            catch (Exception exception)
            {
                HuginnCamPlugin.Log.LogError(exception);
            }
        }
    }
}
