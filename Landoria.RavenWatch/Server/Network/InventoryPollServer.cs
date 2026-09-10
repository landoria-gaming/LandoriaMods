using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Landoria.RavenWatch.Shared;
using Landoria.RavenWatch.Server.Inventory;
using Landoria.RavenWatch.Server.Journal;

namespace Landoria.RavenWatch.Server.Network
{
    internal static class InventoryPollServer
    {
        private sealed class State
        {
            internal long Character;
            internal string Stream;
            internal long Sequence;
            internal string RequestId;
            internal float Sent;
        }
        private static readonly Dictionary<ZRpc, State> states = new Dictionary<ZRpc, State>();
        private static float next;

        internal static void Register(ZNetPeer peer)
        {
            states[peer.m_rpc] = new State();
            peer.m_rpc.Register<long, ZPackage>(InventoryProtocol.Event, InventoryEventReceiver.Receive);
            peer.m_rpc.Register<long, ZPackage>(InventoryProtocol.Response, Receive);
        }

        internal static bool Accept(ZRpc rpc, long id, JObject entry)
        {
            if (!states.TryGetValue(rpc, out var state)) return false;
            string stream = (string)entry["streamId"];
            long sequence = (long)entry["sequence"];
            if (state.Character != id) { state.Character = id; state.Stream = null; state.Sequence = 0; }
            if (state.Stream != null && state.Stream != stream)
                throw new InvalidDataException("Inventory stream changed within a character connection.");
            if (sequence <= state.Sequence) return false;
            state.Stream = stream;
            state.Sequence = sequence;
            return true;
        }

        internal static void Tick()
        {
            if (!RpcCapture.Enabled || ZNet.instance == null || !ZNet.instance.IsServer()) return;
            float now = Time.realtimeSinceStartup;
            if (now < next) return;
            next = now + 5f;
            foreach (var rpc in states.Keys.ToList())
            {
                try
                {
                    if (!rpc.IsConnected()) { states.Remove(rpc); continue; }
                    var peer = ZNet.instance.GetPeers().FirstOrDefault(p => p.m_rpc == rpc);
                    if (peer == null) { states.Remove(rpc); continue; }
                    if (!peer.IsReady() || ZDOMan.instance?.GetZDO(peer.m_characterID) == null) continue;
                    var state = states[rpc];
                    if (state.RequestId != null && now - state.Sent < 10f) continue;
                    state.RequestId = Guid.NewGuid().ToString("D");
                    state.Sent = now;
                    rpc.Invoke(InventoryProtocol.Request, state.RequestId);
                }
                catch (Exception error) { RavenWatchLog.Log.LogError(error); }
            }
        }

        private static void Receive(ZRpc rpc, long id, ZPackage package)
        {
            if (!RpcCapture.Enabled) return;
            try
            {
                var peer = InventoryEventReceiver.Peer(rpc, id);
                var snapshot = InventoryResponseWire.Read(package, id);
                if (!states.TryGetValue(rpc, out var state) || state.RequestId != (string)snapshot["requestId"])
                    throw new InvalidDataException("Unsolicited or expired inventory response.");
                if (state.Character != id) { state.Character = id; state.Stream = null; state.Sequence = 0; }
                if ((state.Stream != null && state.Stream != (string)snapshot["streamId"])
                    || state.Sequence != (long)snapshot["sequence"])
                    throw new InvalidDataException("Inventory response does not match the received event sequence.");
                state.Stream = (string)snapshot["streamId"];
                InventoryCapture.ClientInventory(peer, snapshot);
                state.RequestId = null;
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }

        internal static void Clear() { states.Clear(); next = 0; }

    }
}
