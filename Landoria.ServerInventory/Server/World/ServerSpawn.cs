using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class ServerSpawn
    {
        internal static void Receive(ZRpc rpc, string command)
        {
            var peer = ZNet.instance.GetPeers().FirstOrDefault(value => value.m_rpc == rpc && value.IsReady());
            if (peer == null || !InventoryChangesServer.IsLoaded(rpc)) return;
            try
            {
                if (!ZNet.instance.IsAdmin(peer.m_socket.GetHostName()))
                { Reply(rpc, "Only server administrators can spawn objects."); return; }
                if (command == null || command.Length > 256) { Reply(rpc, "Invalid spawn request."); return; }
                var args = command.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length < 2 || args.Length > 5 || !string.Equals(args[0], "spawn", StringComparison.OrdinalIgnoreCase))
                { Reply(rpc, "Usage: spawn <prefab> [amount] [level] [radius]."); return; }
                int amount = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 1;
                int level = args.Length > 3 ? int.Parse(args[3], CultureInfo.InvariantCulture) : 1;
                float radius = args.Length > 4 ? float.Parse(args[4], CultureInfo.InvariantCulture) : 0.5f;
                if (amount < 1 || amount > 100 || level < 1 || level > 9 || float.IsNaN(radius) || radius < 0 || radius > 20)
                { Reply(rpc, "Spawn limits: amount 1-100, level 1-9, radius 0-20."); return; }
                var prefab = ZNetScene.instance.GetPrefab(args[1]);
                var player = ZDOMan.instance.GetZDO(peer.m_characterID);
                if (prefab == null || prefab.GetComponent<ZNetView>() == null || prefab.GetComponent<Player>() != null || player == null)
                { Reply(rpc, "Unknown or unsupported network prefab, or player unavailable."); return; }
                Vector3 position = player.GetPosition() + player.GetRotation() * Vector3.forward * 2f + Vector3.up;
                for (int index = 0; index < amount; index++)
                    Create(prefab, position + UnityEngine.Random.insideUnitSphere * (amount == 1 ? 0f : radius), level);
                Reply(rpc, "Server spawned " + amount + " x " + prefab.name + ".");
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); Reply(rpc, "Spawn failed; check the server log."); }
        }

        private static void Create(GameObject prefab, Vector3 position, int level)
        {
            var instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            var view = instance.GetComponent<ZNetView>();
            bool cheated = !PlayerProfile.s_bypassCheatChecks;
            if (view != null && view.IsValid()) view.GetZDO().Set(ZDOVars.s_cheated, cheated);
            ItemDrop.OnCreateNew(instance, cheated);
            var item = instance.GetComponent<ItemDrop>();
            if (item != null)
            {
                item.m_itemData.m_durability = item.m_itemData.GetMaxDurability();
                if (level > 1) item.SetQuality(Math.Min(level, 4));
            }
            else if (level > 1) instance.GetComponent<Character>()?.SetLevel(level);
            if (view != null && view.IsValid()) ZDOMan.instance.ForceSendZDO(view.GetZDO().m_uid);
        }

        private static void Reply(ZRpc rpc, string message) => rpc.Invoke(CharacterRpc.SpawnResult, message);
    }
}
