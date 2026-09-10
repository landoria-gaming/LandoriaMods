using System;
using System.Linq;
using HarmonyLib;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(ZRoutedRpc), "RPC_RoutedRPC")]
    internal static class WorldRpcGuard
    {
        private static bool Prefix(ZRpc rpc, ZPackage pkg)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) return true;
            var peer = ZNet.instance.GetPeers().FirstOrDefault(p => p.m_rpc == rpc && p.IsReady());
            if (peer == null) return false;
            try
            {
                if (pkg.Size() > 65536) return false;
                var data = new ZRoutedRpc.RoutedRPCData();
                data.Deserialize(new ZPackage(pkg.GetArray()));
                if (data.m_senderPeerID != peer.m_uid) return false;
                if (data.m_targetZDO.IsNone())
                    return data.m_methodHash == "RequestZDO".GetStableHashCode() || data.m_methodHash == "ChatMessage".GetStableHashCode();
                var target = ZDOMan.instance.GetZDO(data.m_targetZDO);
                if (target == null || target.GetOwner() != peer.m_uid) return false;
                var prefab = ZNetScene.instance.GetPrefab(target.GetPrefab());
                // All gameplay RPCs must use a validated action handler, including destruction and ownership requests.
                return prefab != null && prefab.GetComponent<Character>() != null &&
                    (data.m_methodHash == "SetTrigger".GetStableHashCode() ||
                    (data.m_targetZDO == peer.m_characterID && data.m_methodHash == "Say".GetStableHashCode()));
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); return false; }
        }
    }

    [HarmonyPatch(typeof(ZDO), nameof(ZDO.SetOwner))]
    internal static class WorldObjectOwnership
    {
        private static void Prefix(ZDO __instance, ref long uid)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated() || ZNetScene.instance == null || uid == 0) return;
            var prefab = ZNetScene.instance.GetPrefab(__instance.GetPrefab());
            if (prefab != null && prefab.GetComponent<Character>() == null) uid = ZNet.GetUID();
        }
    }
}
