using System;
using System.Collections.Generic;
using System.Linq;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class ServerRespawn
    {
        private static readonly Dictionary<ZRpc, ZDOID> pending = new Dictionary<ZRpc, ZDOID>();

        internal static void Queue(ZRpc rpc, ZDOID id) => pending[rpc] = id;

        internal static void Tick()
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) { pending.Clear(); return; }
            foreach (var entry in pending.ToArray())
            {
                var peer = ZNet.instance.GetPeers().FirstOrDefault(value => value.m_rpc == entry.Key && value.IsReady());
                if (peer == null || peer.m_characterID != entry.Value) { pending.Remove(entry.Key); continue; }
                if (!InventoryChangesServer.IsLoaded(entry.Key) || ZNetScene.instance == null) continue;
                var player = ZNetScene.instance.FindInstance(entry.Value)?.GetComponent<Player>();
                if (player == null) continue;
                pending.Remove(entry.Key);
                try { Restore(player); }
                catch (Exception error)
                {
                    CharacterRpc.Log.LogError(error);
                    ZNet.instance.Disconnect(peer);
                }
            }
        }

        private static void Restore(Player player)
        {
            var document = CombatCharacters.Load(player);
            var data = document["profile"]["playerData"];
            if ((float)data["health"] > 0f) return;
            bool previous = CombatCharacters.Preparing;
            try
            {
                CombatCharacters.Preparing = true;
                using (var scope = new CombatScope(player))
                {
                    player.SetMaxHealth((float)data["maxHealth"]);
                    player.OnRespawn();
                    NativeDamage.Publish(player, new HitData(), 0f);
                }
                CharacterRpc.Log.LogInfo("Server respawn completed; health saved and synchronized.");
            }
            finally { CombatCharacters.Preparing = previous; }
        }
    }
}
