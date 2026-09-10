using Landoria.RavenWatch.Shared;
using System;
using System.IO;
using System.Linq;
using Landoria.RavenWatch.Server.Journal.Decoding;
using Landoria.RavenWatch.Server.Inventory;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.RavenWatch.Server.Journal
{
    internal static class RpcCapture
    {
        // Disable RPC inspection without disabling inventory collection or transport.
        internal const bool EnableRpcCapture = false;
        internal const bool LogRpcTraffic = false;

        private static RpcJournal journal;
        private static RpcJournal errorJournal;
        private static RpcJsonDecoder decoder;
        [ThreadStatic] private static JObject active;

        internal static bool CaptureEnabled => EnableRpcCapture && Enabled;

        internal static bool Enabled => ZNet.instance != null && ZNet.instance.IsDedicated();

        internal static JObject Begin(ZRpc rpc, ZPackage package, bool debug)
        {
            var previous = active;
            var entry = Entry();
            entry["request"] = Packet(rpc, package, true, debug);
            entry["parentCallId"] = previous?["callId"]?.DeepClone();
            active = entry;
            return previous;
        }

        internal static void End(JObject previous, Exception error)
        {
            var entry = active;
            active = previous;
            if (entry == null) return;
            entry["completedUtc"] = DateTime.UtcNow;
            if (error != null) entry["handlerError"] = error.ToString();
            Save(entry);
        }

        internal static JObject Outgoing(ZRpc rpc, ZPackage package, bool debug)
            => Packet(rpc, package, false, debug);

        internal static void Sent(JObject packet)
        {
            if (active != null) ((JArray)active["response"]).Add(packet);
            else
            {
                var entry = Entry();
                entry["association"] = "independent_outbound";
                ((JArray)entry["response"]).Add(packet);
                Save(entry);
            }
        }

        private static JObject Entry() => new JObject
        {
            ["callId"] = Guid.NewGuid().ToString("N"), ["receivedUtc"] = DateTime.UtcNow,
            ["request"] = null, ["response"] = new JArray(),
            ["association"] = "outbound_during_handler_not_guaranteed_replies"
        };

        private static JObject Packet(ZRpc rpc, ZPackage package, bool incoming, bool debug)
        {
            var result = new JObject { ["utc"] = DateTime.UtcNow,
                ["peer"] = rpc.GetSocket().GetHostName(), ["byteLength"] = package.Size(),
                ["direction"] = incoming ? "client_to_server" : "server_to_client" };
            byte[] bytes = package.GetArray();
            if (bytes.Length >= 4) result["hash"] = BitConverter.ToInt32(bytes, 0);
            try
            {
                if (decoder == null) decoder = new RpcJsonDecoder();
                result["data"] = InventoryRpcLog.Decode(bytes, incoming, debug)
                    ?? JObject.Parse(decoder.Decode(bytes, incoming,
                        global::Version.CurrentVersion.ToString(), debug, Components, ObjectComponents));
            }
            catch (Exception error)
            {
                RavenWatchLog.Log.LogError(error);
                result["decodeStatus"] = "error";
                result["error"] = error.ToString();
            }
            return result;
        }

        private static string[] Components(int hash)
        {
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(hash) : null;
            return prefab == null ? null : prefab.GetComponents<Component>()
                .Where(component => component != null).Select(component => component.GetType().Name).ToArray();
        }

        private static string[] ObjectComponents(long user, uint id)
        {
            var zdo = ZDOMan.instance?.GetZDO(new ZDOID(user, id));
            return zdo == null ? null : Components(zdo.GetPrefab());
        }

        private static void Save(JObject entry)
        {
            Start();
            SaveDecodeError(entry, entry["request"] as JObject, "request");
            foreach (JObject response in (JArray)entry["response"])
                SaveDecodeError(entry, response, "response");
            journal?.Append(entry);
        }

        internal static void Start()
        {
            if (!CaptureEnabled) return;
            string directory = Path.Combine(Utils.GetSaveDataPath(FileHelpers.FileSource.Local), "RavenWatch");
            if (journal == null) journal = LogRpcTraffic ? new RpcJournal(directory) : null;
        }

        private static void SaveDecodeError(JObject entry, JObject packet, string role)
        {
            if ((string)packet?["decodeStatus"] != "error") return;
            try
            {
                if (errorJournal == null)
                    errorJournal = new RpcJournal(Path.Combine(Utils.GetSaveDataPath(FileHelpers.FileSource.Local),
                        "RavenWatch"), "rpc-errors");
                errorJournal.Append(new JObject
                {
                    ["callId"] = entry["callId"].DeepClone(), ["role"] = role,
                    ["packet"] = packet.DeepClone()
                });
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }

        internal static void Close()
        {
            CloseJournal(journal);
            CloseJournal(errorJournal);
            InventoryCapture.Close();
            journal = null;
            errorJournal = null;
            decoder = null;
            active = null;
        }

        private static void CloseJournal(IDisposable value)
        {
            try { value?.Dispose(); }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }
}
