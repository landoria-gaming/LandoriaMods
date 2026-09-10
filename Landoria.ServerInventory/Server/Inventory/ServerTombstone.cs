using System;
using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    internal static class ServerTombstone
    {
        [ThreadStatic] internal static bool Creating;

        internal static void Create(WorldActionTransaction action)
        {
            string death = action.Peer.m_characterID.ToString();
            if ((float)action.Document["profile"]["playerData"]["health"] > 0f ||
                (string)action.Document["lastTombstoneCharacter"] == death)
                throw new InvalidOperationException("No unprocessed server-confirmed death.");
            Creating = true;
            try { action.Player.CreateTombStone(); }
            finally { Creating = false; }
            action.Document["lastTombstoneCharacter"] = death;
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Setup))]
    internal static class ServerTombstoneOwnerPatch
    {
        private static void Prefix(ref string ownerName, ref long ownerUID)
        {
            if (!ServerTombstone.Creating) return;
            ownerName = WorldActionTransaction.Current.PlayerName;
            ownerUID = WorldActionTransaction.Current.PlayerId;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
    internal static class TombstoneUnequipPatch
    {
        private static void Postfix()
        {
            if (!ServerTombstone.Creating) return;
            foreach (var item in WorldActionTransaction.Current.Inventory.GetAllItems()) item.m_equipped = false;
        }
    }
}
