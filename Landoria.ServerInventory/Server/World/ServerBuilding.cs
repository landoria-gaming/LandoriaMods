using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Splatform;
using UnityEngine;

namespace Landoria.ServerInventory.Server
{
    internal static class ServerBuilding
    {
        internal static void Create(WorldActionTransaction action, JObject request)
        {
            var prefab = ZNetScene.instance.GetPrefab((int)request["prefab"]);
            var piece = prefab == null ? null : prefab.GetComponent<Piece>();
            if (piece == null || !piece.m_enabled || piece.m_repairPiece || piece.m_removePiece ||
                prefab.GetComponent<ZNetView>() == null || prefab.GetComponent<TerrainModifier>() != null || prefab.GetComponent<TerrainOp>() != null)
                throw new InvalidOperationException("Unsupported construction.");
            var tool = action.Inventory.GetAllItems().FirstOrDefault(item => item.m_shared.m_buildPieces != null &&
                item.m_shared.m_buildPieces.m_pieces.Contains(prefab) && (!item.m_shared.m_useDurability || item.m_durability > 0f));
            if (tool == null || !action.Document["profile"]["playerData"]["knownRecipes"].Any(entry => (string)entry["value"] == piece.m_name))
                throw new InvalidOperationException("Build tool or known piece missing.");
            if (!action.Player.HaveRequirements(piece, Player.RequirementMode.CanBuild))
                throw new InvalidOperationException("Build requirements not met.");
            Vector3 position = new Vector3((float)request["x"], (float)request["y"], (float)request["z"]);
            float yaw = (float)request["yaw"];
            if (float.IsNaN(yaw) || float.IsInfinity(yaw)) throw new InvalidOperationException("Invalid build rotation.");
            var rotation = Quaternion.Euler(0, yaw, 0);
            BuildPlacement.Validate(action.Player, piece, position, rotation);
            var instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
            Setup(action, instance);
            if (!ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())) action.Player.ConsumeResources(piece.m_resources, 0);
        }

        private static void Setup(WorldActionTransaction action, GameObject instance)
        {
            string host = action.Peer.m_socket.GetHostName();
            if (!PlatformUserID.TryParse(host, out var platform)) platform = new PlatformUserID(new Platform("Steam"), host);
            instance.GetComponent<Piece>().SetCreator(action.PlayerId, platform);
            instance.GetComponent<PrivateArea>()?.Setup(action.PlayerName);
            instance.GetComponent<WearNTear>()?.OnPlaced();
            instance.GetComponent<ItemDrop>()?.MakePiece(true);
            foreach (var placed in instance.GetComponents<IPlaced>()) placed.OnPlaced();
            var station = instance.GetComponentInChildren<CraftingStation>();
            if (station != null) action.Player.AddKnownStation(station);
            bool cheated = action.Inventory.ItemCheated(instance.GetComponent<Piece>().m_resources) && !PlayerProfile.s_bypassCheatChecks;
            instance.GetComponent<ZNetView>().GetZDO().Set(ZDOVars.s_cheated, cheated);
        }
    }
}
