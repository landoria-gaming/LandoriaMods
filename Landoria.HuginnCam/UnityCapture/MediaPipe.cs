using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Threading;

namespace Landoria.HuginnCam.UnityCapture
{
    internal sealed class MediaPipe : IDisposable
    {
        private readonly BlockingCollection<byte[]> _queue;
        private readonly NamedPipeServerStream _stream;
        private Thread _writerThread;
        private bool _disposed;

        internal MediaPipe(string prefix, int capacity)
        {
            string name = $"{prefix}_{Guid.NewGuid():N}";
            Path = $@"\\.\pipe\{name}";
            _queue = new BlockingCollection<byte[]>(capacity);
            _stream = new NamedPipeServerStream(
                name,
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.WriteThrough,
                0,
                16 * 1024 * 1024);
        }

        internal string Path { get; }
        internal bool IsConnected => !_disposed && _stream.IsConnected;

        internal void BeginWaitForConnection()
        {
            _writerThread = new Thread(ConnectAndWrite) { IsBackground = true };
            _writerThread.Start();
        }

        internal bool Write(byte[] buffer)
        {
            return !_disposed && _stream.IsConnected && _queue.TryAdd(buffer);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _queue.CompleteAdding();
            if (_writerThread?.IsAlive == true && !_writerThread.Join(5_000))
            {
                HuginnCamPlugin.Log.LogWarning("A media pipe writer did not stop within five seconds.");
            }

            _stream.Dispose();
            _queue.Dispose();
        }

        private void ConnectAndWrite()
        {
            try
            {
                _stream.WaitForConnection();
                WriteQueuedData();
            }
            catch (Exception exception)
            {
                HuginnCamPlugin.Log.LogError(exception);
            }
        }

        private void WriteQueuedData()
        {
            try
            {
                foreach (byte[] buffer in _queue.GetConsumingEnumerable())
                {
                    _stream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception exception)
            {
                HuginnCamPlugin.Log.LogError(exception);
            }
        }
    }
}
