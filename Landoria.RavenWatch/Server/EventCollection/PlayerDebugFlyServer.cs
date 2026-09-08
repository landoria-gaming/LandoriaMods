using System;
using UnityEngine;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal sealed class PlayerDebugFlyServer
    {
        public int schemaVersion = 1;
        public string kind = "player_debug_fly_server";
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string observation = "player_zdo_debug_fly_enabled";
        public string playerName;
        public string playerNetworkId;
        public string playerSessionId;
        public string ownerSessionId;
        public string networkSenderSessionId;
        public bool networkSenderWasOwner;
        public Vector3 position;
        public bool debugFly = true;
        public string evidence = "player_sent_debug_fly_flag_enabled";

        internal static PlayerDebugFlyServer Capture(ZDO zdo, ZNetPeer sender)
        {
            return new PlayerDebugFlyServer
            {
                playerName = sender.m_playerName,
                playerNetworkId = zdo.m_uid.ToString(),
                playerSessionId = sender.m_uid.ToString(),
                ownerSessionId = zdo.GetOwner().ToString(),
                networkSenderSessionId = sender.m_uid.ToString(),
                networkSenderWasOwner = zdo.GetOwner() == sender.m_uid,
                position = zdo.GetPosition()
            };
        }
    }
}
