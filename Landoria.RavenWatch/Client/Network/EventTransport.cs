using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal static class EventTransport
    {
        private const int MaximumBytes = 1024 * 1024;
        private sealed class Batch
        {
            internal long id;
            internal ZPackage package;
        }
        private static readonly Queue<Batch> pending = new();
        private static ZRpc connection;
        private static bool ready;
        private static string stream;
        private static long nextBatch;
        private static int queuedBytes;
        private static float nextSend;

        internal static void Register(ZNetPeer peer)
        {
            if (!peer.m_server) return;
            ActivityJournal.Flush();
            Reset();
            connection = peer.m_rpc;
            stream = Guid.NewGuid().ToString("N");
            connection.Register<int>(RavenWatchProtocol.ReadyRpc, (rpc, version) =>
            {
                if (rpc == connection && version == 1) ready = true;
            });
            connection.Register<long>(RavenWatchProtocol.AckRpc, Acknowledge);
            connection.Register<string, string>(RavenWatchProtocol.AnomalyRpc, ShowAnomaly);
        }

        internal static void Reset()
        {
            ClearPending();
            connection = null;
            ready = false;
            nextBatch = 0;
            nextSend = 0;
        }

        private static void ClearPending()
        {
            pending.Clear();
            queuedBytes = 0;
        }

        internal static void Queue(IReadOnlyList<string> events)
        {
            if (connection == null || !connection.IsConnected()) return;
            var lines = new List<string>();
            int bytes = 128;
            foreach (string line in events)
            {
                int size = Encoding.UTF8.GetByteCount(line) + 5;
                if (size > MaximumBytes - 128)
                {
                    RavenWatchPlugin.Log.LogWarning("Event exceeds RPC limit and was omitted.");
                    continue;
                }
                if (lines.Count == 128 || bytes + size > MaximumBytes)
                {
                    AddBatch(lines);
                    lines.Clear();
                    bytes = 128;
                }
                lines.Add(line);
                bytes += size;
            }
            if (lines.Count > 0) AddBatch(lines);
        }

        private static void AddBatch(List<string> lines)
        {
            var package = new ZPackage();
            package.Write(1);
            package.Write(stream);
            package.Write(++nextBatch);
            package.Write(lines.Count);
            foreach (string line in lines) package.Write(line);
            if (queuedBytes + package.Size() > 8 * MaximumBytes)
            {
                RavenWatchPlugin.Log.LogWarning("RPC queue full; new batch omitted.");
                return;
            }
            pending.Enqueue(new Batch { id = nextBatch, package = package });
            queuedBytes += package.Size();
        }

        internal static void Send()
        {
            if (!ready || connection == null || !connection.IsConnected()
                || pending.Count == 0 || Time.realtimeSinceStartup < nextSend) return;
            nextSend = Time.realtimeSinceStartup + 5f;
            try { connection.Invoke(RavenWatchProtocol.EventsRpc, pending.Peek().package); }
            catch (Exception exception) { RavenWatchPlugin.Log.LogError(exception); }
        }

        private static void Acknowledge(ZRpc rpc, long batch)
        {
            if (rpc != connection || pending.Count == 0 || pending.Peek().id != batch) return;
            queuedBytes -= pending.Dequeue().package.Size();
            nextSend = 0;
        }

        private static void ShowAnomaly(ZRpc rpc, string playerName, string anomaly)
        {
            if (rpc != connection) return;
            try
            {
                AnomalyChatDisplay.Show(playerName, anomaly);
                RavenWatchPlugin.Log.LogInfo("Displayed a server anomaly in the chat window.");
            }
            catch (Exception exception) { RavenWatchPlugin.Log.LogError(exception); }
        }

        internal static void Disconnect(ZNetPeer peer)
        {
            if (peer.m_rpc != connection) return;
            ActivityJournal.Flush();
            if (pending.Count > 0)
                RavenWatchPlugin.Log.LogWarning("Disconnected with unacknowledged batches; pending events discarded.");
            Reset();
        }
    }
}
