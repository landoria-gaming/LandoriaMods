using System;
using System.Globalization;
using System.Linq;
using HarmonyLib;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal static class ServerPlayerDamageCollector
    {
        private static readonly int DamageTextRpc = "RPC_DamageText".GetStableHashCode();

        internal static void Observe(ZRpc rpc, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || package == null) return;
            try
            {
                ZNetPeer sender = ZNet.instance.GetPeers().FirstOrDefault(peer => peer.m_rpc == rpc);
                if (sender == null || sender.m_characterID.IsNone()) return;
                var data = new ZRoutedRpc.RoutedRPCData();
                data.Deserialize(new ZPackage(package.GetArray()));
                if (data.m_senderPeerID != sender.m_uid || data.m_methodHash != DamageTextRpc) return;
                PublishIfDamageAtOneHealth(sender, data.m_parameters);
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }

        private static void PublishIfDamageAtOneHealth(ZNetPeer sender, ZPackage parameters)
        {
            ZPackage damage = new ZPackage(parameters.GetArray()).ReadPackage();
            int damageTextType = damage.ReadInt();
            UnityEngine.Vector3 position = damage.ReadVector3();
            string text = damage.ReadString();
            bool player = damage.ReadBool();
            if (!player || !IsDamageType(damageTextType) || !float.TryParse(text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float amount) || amount <= 0f) return;
            ZDO zdo = ZDOMan.instance?.GetZDO(sender.m_characterID);
            if (zdo == null || !zdo.GetFloat(ZDOVars.s_health, out float health) ||
                !UnityEngine.Mathf.Approximately(health, 1f)) return;
            ServerEventPublisher.Publish(new PlayerDamagedAtOneHealthServer
            {
                playerName = sender.m_playerName,
                playerNetworkId = sender.m_characterID.ToString(),
                playerSessionId = sender.m_uid.ToString(),
                healthBeforeDamage = health,
                reportedDamage = amount,
                damagePosition = position,
                damageTextType = damageTextType
            });
        }

        private static bool IsDamageType(int type)
        {
            return type == (int)DamageText.TextType.Normal ||
                type == (int)DamageText.TextType.Resistant ||
                type == (int)DamageText.TextType.Weak;
        }
    }

    [HarmonyPatch(typeof(ZRoutedRpc), "RPC_RoutedRPC")]
    internal static class ServerPlayerDamageRoutedRpcPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ZRpc rpc, ZPackage pkg)
        {
            ServerPlayerDamageCollector.Observe(rpc, pkg);
        }
    }
}
