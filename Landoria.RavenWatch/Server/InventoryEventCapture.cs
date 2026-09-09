using System;
using Landoria.RavenWatch.Server.Decoding;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal sealed class InventoryEventCapture : IDisposable
    {
        private readonly InventoryJournal journal;
        internal InventoryEventCapture(string directory) { journal = new InventoryJournal(directory); }

        internal void Capture(JObject call, RpcJsonDecoder decoder, ZNetPeer peer)
        {
            var data = call["request"]?["data"];
            switch ((string)data?["name"])
            {
                case "ZDOData":
                    CharacterJournalCapture.Capture(journal, call, peer);
                    DroppedItemCapture.Capture(journal, call, peer);
                    break;
                case "RoutedRPC":
                    if ((string)data["arguments"]?["pkg"]?["parameters"]?["name"] == "DestroyZDO")
                        ItemPickupCapture.Capture(journal, call, peer, decoder);
                    break;
            }
        }

        public void Dispose() => journal.Dispose();
    }
}
