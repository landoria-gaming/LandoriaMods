using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Landoria.RavenWatch.Server
{
    internal sealed class EventReceiver
    {
        private readonly ZNetPeer peer;
        private string stream;
        private long lastBatch;
        internal EventReceiver(ZNetPeer peer) { this.peer = peer; }

        internal void Receive(ZRpc rpc, ZPackage package)
        {
            if (rpc != peer.m_rpc || !peer.IsReady() || !ZNet.instance.IsServer()) return;
            try
            {
                if (package.Size() > 1024 * 1024) throw new FormatException("Event batch too large.");
                if (package.ReadInt() != 1) throw new FormatException("Unsupported event protocol.");
                string incomingStream = package.ReadString();
                long batch = package.ReadLong();
                if (!Guid.TryParseExact(incomingStream, "N", out _) || batch <= 0)
                    throw new FormatException("Invalid stream or batch identifier.");
                if (stream != null && stream != incomingStream) throw new FormatException("Stream changed within connection.");
                if (batch <= lastBatch) { rpc.Invoke(RavenWatchProtocol.AckRpc, batch); return; }
                List<object> events = ReadEvents(package);
                if (!ReceivedJournal.Append(peer, incomingStream, batch, events)) return;
                stream = incomingStream;
                lastBatch = batch;
                rpc.Invoke(RavenWatchProtocol.AckRpc, batch);
            }
            catch (Exception exception) { RavenWatchPlugin.Log.LogError(exception); }
        }

        private static List<object> ReadEvents(ZPackage package)
        {
            int count = package.ReadInt();
            if (count < 1 || count > 128) throw new FormatException("Invalid event count.");
            var events = new List<object>(count);
            for (int index = 0; index < count; index++)
            {
                if (EventDeserializer.TryDeserialize(package.ReadString(), index, out object entry))
                    events.Add(entry);
            }
            return events;
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class EventRegistrationPatch
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            if (!__instance.IsServer()) return;
            try { peer.m_rpc.Register<ZPackage>(RavenWatchProtocol.EventsRpc, new EventReceiver(peer).Receive); }
            catch (Exception exception) { RavenWatchPlugin.Log.LogError(exception); }
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    internal static class EventReadyPatch
    {
        private static void Postfix(ZNet __instance, ZRpc rpc)
        {
            if (!__instance.IsServer()) return;
            try
            {
                ZNetPeer peer = __instance.GetPeers().FirstOrDefault(candidate => candidate.m_rpc == rpc);
                if (peer != null && peer.IsReady()) rpc.Invoke(RavenWatchProtocol.ReadyRpc, 1);
            }
            catch (Exception exception) { RavenWatchPlugin.Log.LogError(exception); }
        }
    }
}
