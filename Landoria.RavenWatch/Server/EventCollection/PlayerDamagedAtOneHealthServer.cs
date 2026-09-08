using System;
using UnityEngine;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal sealed class PlayerDamagedAtOneHealthServer
    {
        public int schemaVersion = 1;
        public string kind = "player_damaged_at_one_health_server";
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string observation = "positive_damage_reported_while_health_was_one";
        public string playerName;
        public string playerNetworkId;
        public string playerSessionId;
        public float healthBeforeDamage;
        public float reportedDamage;
        public Vector3 damagePosition;
        public int damageTextType;
        public string evidence = "server_routed_positive_damage_text_from_player_at_one_health";
    }
}
