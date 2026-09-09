using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal static class CharacterIdentity
    {
        internal static JObject Add(JObject entry, ZNetPeer peer)
        {
            var zdo = peer == null ? null : ZDOMan.instance?.GetZDO(peer.m_characterID);
            long id = zdo?.GetLong(ZDOVars.s_playerID, 0L) ?? 0L;
            if (id == 0L && peer != null) id = peer.m_playerID;
            // PlayerProfile saves this ID; network session IDs and character ZDOIDs can change.
            entry["characterId"] = id == 0L ? null : id.ToString(CultureInfo.InvariantCulture);
            entry["playerName"] = peer?.m_playerName;
            return entry;
        }
    }
}
