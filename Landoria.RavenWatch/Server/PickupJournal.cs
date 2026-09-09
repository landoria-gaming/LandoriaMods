using System;
using Landoria.RavenWatch.Server.Decoding;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal sealed class PickupJournal : IDisposable
    {
        private readonly RpcJournal journal;
        private readonly FirstConnectionCapture firstConnections = new FirstConnectionCapture();
        internal PickupJournal(string directory) { journal = new RpcJournal(directory, "pickups"); }

        internal void CaptureDrops(JObject call, ZNetPeer peer)
            => DroppedItemCapture.Capture(journal, call, peer);

        internal void CaptureFirstConnections(JObject call, ZNetPeer peer)
            => firstConnections.Capture(journal, call, peer);

        internal void Capture(JObject call, RpcJsonDecoder decoder, Func<int, string[]> components, ZNetPeer peer)
        {
            var packet = call["request"];
            var data = packet?["data"];
            if ((string)packet?["direction"] != "client_to_server" || (string)data?["name"] != "RoutedRPC") return;
            var routed = data["arguments"]?["pkg"];
            var method = routed?["parameters"];
            if ((string)method?["name"] != "RPC_SetPicked" || (bool?)method?["arguments"]?["picked"] != true) return;
            var target = routed["targetZdo"];
            var zdo = ZDOMan.instance?.GetZDO(new ZDOID((long)target["userId"], (uint)target["id"]));
            journal.Append(CharacterIdentity.Add(new JObject
            {
                ["event"] = "pickup_reported", ["callId"] = call["callId"].DeepClone(),
                ["utc"] = packet["utc"].DeepClone(), ["direction"] = "client_to_server",
                ["peer"] = packet["peer"].DeepClone(), ["reportedSenderPeerId"] = routed["senderPeerId"].DeepClone(),
                ["rpc"] = "RPC_SetPicked", ["arguments"] = method["arguments"].DeepClone(),
                ["targetZdo"] = target.DeepClone(), ["zdo"] = Snapshot(zdo, decoder, components),
                ["zdoStatus"] = zdo == null ? "not_found" : "found",
                ["snapshotTiming"] = "server_state_before_rpc_handler"
            }, peer));
        }

        private static JObject Snapshot(ZDO zdo, RpcJsonDecoder decoder, Func<int, string[]> components)
        {
            if (zdo == null) return null;
            var position = zdo.GetPosition();
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(zdo.GetPrefab()) : null;
            var snapshot = new JObject
            {
                ["prefabHash"] = zdo.GetPrefab(), ["prefabName"] = prefab != null ? prefab.name : null,
                ["owner"] = zdo.GetOwner().ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["ownerRevision"] = zdo.OwnerRevision, ["dataRevision"] = zdo.DataRevision,
                ["position"] = new JObject { ["x"] = position.x, ["y"] = position.y, ["z"] = position.z }
            };
            try
            {
                // Serialize the existing server ZDO; this does not change its revisions or ownership.
                var package = new ZPackage();
                zdo.Serialize(package);
                snapshot["state"] = decoder.DecodeZdoState(package.GetArray(), components);
            }
            catch (Exception error)
            {
                RpcCapture.Log.LogError(error);
                snapshot["stateDecodeError"] = error.ToString();
            }
            return snapshot;
        }

        public void Dispose() { journal.Dispose(); }
    }
}
