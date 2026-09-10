using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Landoria.ServerInventory.Serialization;

namespace Landoria.ServerInventory.Server
{
    internal static class CombatCharacters
    {
        [ThreadStatic] internal static bool Preparing;
        internal static ZNetPeer Peer(Character character)
            => ZNet.instance.GetPeers().FirstOrDefault(peer => peer.IsReady() && peer.m_characterID == character.GetZDOID());

        internal static JObject Load(Player player)
        {
            var peer = Peer(player) ?? throw new InvalidDataException("Player has no server character session.");
            return JObject.Parse(File.ReadAllText(CharacterStore.GetPath(peer)));
        }

        internal static void Prepare(Character character)
        {
            bool previous = Preparing;
            Preparing = true;
            try { LoadPlayer(character); }
            finally { Preparing = previous; }
        }

        private static void LoadPlayer(Character character)
        {
            if (!(character is Player player)) return;
            var document = Load(player);
            var data = (JObject)document["profile"]["playerData"];
            using (var io = new BinaryJson())
            {
                CharacterSchema.Inventory(io, (JObject)data["inventory"]);
                player.UnequipAllItems();
                player.GetInventory().Load(new ZPackage(io.Finish()));
            }
            foreach (var item in player.GetInventory().GetAllItems().ToList())
                if (item.m_equipped) player.EquipItem(item, false);
            using (var io = new BinaryJson())
            {
                CharacterSchema.Skills(io, (JObject)data["skills"]);
                player.GetSkills().Load(new ZPackage(io.Finish()));
            }
            player.SetMaxHealth((float)data["maxHealth"]);
            player.SetHealth((float)data["health"]);
        }

        internal static void SaveHealth(Character character)
        {
            if (!(character is Player player)) return;
            var document = Load(player);
            document["profile"]["playerData"]["health"] = (double)player.GetHealth();
            document["profile"]["playerData"]["maxHealth"] = (double)player.GetMaxHealth();
            CharacterStore.SaveExisting(CharacterStore.GetPath(Peer(player)), document);
        }

        internal static byte[] InventoryBytes(Humanoid character)
        {
            var package = new ZPackage();
            character.GetInventory().Save(package);
            return package.GetArray();
        }

        internal static void SaveInventory(Humanoid character)
        {
            if (!(character is Player player)) return;
            var document = Load(player);
            var package = new ZPackage();
            player.GetInventory().Save(package);
            var inventory = new JObject();
            using (var io = new BinaryJson(package.GetArray())) { CharacterSchema.Inventory(io, inventory); io.Finish(); }
            if (JToken.DeepEquals(document["profile"]["playerData"]["inventory"], inventory)) return;
            document["profile"]["playerData"]["inventory"] = inventory;
            CharacterStore.SaveExisting(CharacterStore.GetPath(Peer(player)), document);
            Peer(player).m_rpc.Invoke(Network.CharacterRpc.CombatInventory, package);
        }
    }
}
