using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Client
{
    internal static class WorldActionRequest
    {
        private static ZRpc connection;
        private static readonly Dictionary<string, string> pending = new Dictionary<string, string>();
        private static readonly Dictionary<string, Action> completions = new Dictionary<string, Action>();

        internal static void Send(JObject request, Action onSuccess = null)
        {
            try
            {
                var rpc = ZNet.instance.GetServerRPC();
                if (rpc == null || !rpc.IsConnected()) return;
                if (connection != rpc) { connection = rpc; pending.Clear(); completions.Clear(); }
                string key = (string)request["kind"] == "pickup"
                    ? "pickup/" + (string)request["user"] + "/" + (uint)request["object"]
                    : request.ToString(Formatting.None);
                if (pending.Values.Contains(key)) return;
                string id = Guid.NewGuid().ToString("D");
                request["id"] = id;
                pending.Add(id, key);
                if (onSuccess != null) completions.Add(id, onSuccess);
                rpc.Invoke(CharacterRpc.WorldAction, request.ToString(Formatting.None));
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
        }

        internal static void Receive(ZRpc rpc, string id, bool success, string reason, ZPackage inventory)
        {
            if (ZNet.instance == null || rpc != ZNet.instance.GetServerRPC() || rpc != connection || !pending.Remove(id)) return;
            completions.TryGetValue(id, out var complete);
            completions.Remove(id);
            if (success)
            {
                CombatResult.ReceiveInventory(rpc, inventory);
                complete?.Invoke();
            }
            else if (!string.IsNullOrEmpty(reason)) Player.m_localPlayer?.Message(MessageHud.MessageType.Center, reason);
        }

        internal static void Build(Piece piece, Vector3 position, Quaternion rotation)
            => Send(new JObject { ["kind"] = "build", ["prefab"] = piece.gameObject.name.GetStableHashCode(),
                ["x"] = position.x, ["y"] = position.y, ["z"] = position.z, ["yaw"] = rotation.eulerAngles.y });
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropItem))]
    internal static class RequestPlayerDropPatch
    {
        private static bool Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, int amount, ref bool __result)
        {
            if (!ClientDamageGuard.Active) return true;
            __result = false;
            if (__instance != Player.m_localPlayer || item == null) return false;
            if (inventory != null && inventory != __instance.GetInventory())
            { __instance.Message(MessageHud.MessageType.Center, "Move the item into your inventory before dropping it."); return false; }
            WorldActionRequest.Send(new JObject { ["kind"] = "drop", ["prefab"] = item.m_dropPrefab.name.GetStableHashCode(),
                ["amount"] = amount, ["quality"] = item.m_quality, ["variant"] = item.m_variant,
                ["x"] = item.m_gridPos.x, ["y"] = item.m_gridPos.y },
                DropCursor.Completion(item, inventory ?? __instance.GetInventory(), amount));
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    internal static class RequestBuildPatch
    {
        private static bool Prefix(Player __instance, Piece piece, GameObject ___m_placementGhost, ref bool __result)
        {
            if (!ClientDamageGuard.Active) return true;
            __result = false;
            if (__instance == Player.m_localPlayer && piece != null && ___m_placementGhost != null &&
                __instance.GetPlacementStatus() == Player.PlacementStatus.Valid)
                WorldActionRequest.Build(piece, ___m_placementGhost.transform.position, ___m_placementGhost.transform.rotation);
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    internal static class RequestDirectBuildPatch
    {
        private static bool Prefix(Player __instance, Piece piece, Vector3 pos, Quaternion rot)
        {
            if (!ClientDamageGuard.Active) return true;
            if (__instance == Player.m_localPlayer && piece != null) WorldActionRequest.Build(piece, pos, rot);
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
    internal static class RequestTombstonePatch
    {
        private static bool Prefix(Player __instance)
        {
            if (!ClientDamageGuard.Active) return true;
            if (__instance == Player.m_localPlayer) WorldActionRequest.Send(new JObject { ["kind"] = "tombstone" });
            return false;
        }
    }
}
