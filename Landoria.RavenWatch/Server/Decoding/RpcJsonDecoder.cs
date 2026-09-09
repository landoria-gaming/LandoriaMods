using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Decoding
{
    public sealed class RpcJsonDecoder
    {
        private readonly RpcDefinitions definitions = new RpcDefinitions();
        public string GameVersion => definitions.GameVersion;

        internal JObject DecodeItemData(byte[] bytes)
        {
            var context = new DecodeContext("server_snapshot", null, null);
            using (var input = new WireInput(bytes))
            {
                var state = (JObject)new SchemaReader(definitions, context).Read(input, definitions.BinaryValues[ZDOVars.s_itemData.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
                input.RequireEnd();
                return (JObject)state["item"];
            }
        }

        public string Decode(byte[] packet, bool receivedByServer, string gameVersion, bool debugRpc = false,
            Func<int, string[]> prefabComponents = null, Func<long, uint, string[]> objectComponents = null)
        {
            if (gameVersion != GameVersion) throw new InvalidDataException("RPC definitions do not match game version " + gameVersion);
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            var context = new DecodeContext(receivedByServer ? "client_to_server" : "server_to_client", prefabComponents, objectComponents);
            using (var input = new WireInput(packet))
            {
                int hash = (int)input.Primitive("int32");
                JObject decoded;
                if (hash == 0) decoded = new JObject { ["name"] = "transport_ping", ["requestReply"] = (bool)input.Primitive("boolean") };
                else
                {
                    string wireName = debugRpc ? (string)input.Primitive("string") : null;
                    decoded = new SchemaReader(definitions, context).DecodeMethod(input, "direct", hash);
                    if (wireName != null) decoded["wireName"] = wireName;
                }
                input.RequireEnd();
                decoded["direction"] = context.Direction;
                decoded["gameVersion"] = GameVersion;
                decoded["decodeStatus"] = "decoded";
                return decoded.ToString(Formatting.None);
            }
        }
    }
}
