using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class ServerPlayerCreation
    {
        internal sealed class Reservation { internal ZDOID Id; internal bool Used; }
        private static readonly ConditionalWeakTable<ZRpc, Reservation> reservations = new ConditionalWeakTable<ZRpc, Reservation>();

        internal static void Receive(ZRpc rpc, Vector3 position)
        {
            var peer = ZNet.instance.GetPeers().FirstOrDefault(value => value.m_rpc == rpc && value.IsReady());
            if (peer == null || !InventoryChangesServer.IsLoaded(rpc)) return;
            try
            {
                if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z))
                    throw new InvalidOperationException("Invalid player spawn position.");
                var slot = reservations.GetOrCreateValue(rpc);
                if (slot.Used)
                {
                    var previous = ZDOMan.instance.GetZDO(slot.Id);
                    if (previous != null && previous.GetFloat(ZDOVars.s_health, 1f) > 0f)
                        throw new InvalidOperationException("A living character already exists for this connection.");
                }
                var zdo = !slot.Used ? ZDOMan.instance.GetZDO(slot.Id) : null;
                if (zdo == null)
                {
                    var prefab = Game.instance.m_playerPrefab;
                    var view = prefab.GetComponent<ZNetView>();
                    zdo = ZDOMan.instance.CreateNewZDO(position, prefab.name.GetStableHashCode());
                    zdo.SetPrefab(prefab.name.GetStableHashCode());
                    zdo.Persistent = view.m_persistent;
                    zdo.SetType(view.m_type);
                    zdo.Distant = view.m_distant;
                    zdo.SetOwner(peer.m_uid);
                    InitializeCharacter(peer, zdo);
                    slot.Id = zdo.m_uid;
                    slot.Used = false;
                }
                Send(rpc, zdo);
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); ZNet.instance.Disconnect(peer); }
        }

        private static void InitializeCharacter(ZNetPeer peer, ZDO zdo)
        {
            var document = JObject.Parse(File.ReadAllText(CharacterStore.GetPath(peer)));
            var profile = document["profile"];
            var player = profile["playerData"];
            zdo.Set(ZDOVars.s_playerID, long.Parse((string)profile["playerId"]));
            zdo.Set(ZDOVars.s_playerName, (string)profile["playerName"]);
            zdo.Set(ZDOVars.s_health, (float)player["health"]);
            zdo.Set(ZDOVars.s_modelIndex, (int)player["modelIndex"]);
            zdo.Set(ZDOVars.s_hairItem, ((string)player["hairItem"]).GetStableHashCode());
            zdo.Set(ZDOVars.s_beardItem, ((string)player["beardItem"]).GetStableHashCode());
            zdo.Set(ZDOVars.s_skinColor, Color(player["skinColor"]));
            zdo.Set(ZDOVars.s_hairColor, Color(player["hairColor"]));
        }
        private static Vector3 Color(JToken color)
            => new Vector3((float)color["x"], (float)color["y"], (float)color["z"]);

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) < 100000f;

        private static void Send(ZRpc rpc, ZDO zdo)
        {
            var data = new ZPackage();
            zdo.Serialize(data);
            var package = new ZPackage();
            package.Write(zdo.m_uid);
            package.Write(zdo.GetPosition());
            package.Write(zdo.GetOwner());
            package.Write(data);
            rpc.Invoke(CharacterRpc.ReservedPlayer, package);
        }

        internal static bool Accept(ZRpc rpc, ZDOID id)
        {
            if (id.IsNone()) return true;
            if (!reservations.TryGetValue(rpc, out var slot) || id != slot.Id) return false;
            slot.Used = true;
            return true;
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_CharacterID")]
    internal static class ServerCharacterIdGuard
    {
        private static bool Prefix(ZNet __instance, ZRpc rpc, ZDOID characterID)
            => !__instance.IsDedicated() || ServerPlayerCreation.Accept(rpc, characterID);
    }
}
