using System;
using HarmonyLib;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch]
    internal static class PlayerCreation
    {
        internal static ZDO Reserved;
        private static ZRpc connection;
        private static bool waiting;
        private static float requestedAt;
        internal static bool HasSpawnPoint;
        internal static Vector3 SpawnPoint;
        internal static bool UsedLogoutPoint;

        internal static void Reset()
        {
            Reserved = null;
            connection = null;
            waiting = false;
            HasSpawnPoint = false;
        }

        internal static bool Ready(Vector3 position)
        {
            var rpc = ZNet.instance.GetServerRPC();
            if (rpc != connection) { connection = rpc; Reserved = null; waiting = false; }
            if (Reserved != null) return true;
            if (!waiting)
            {
                waiting = true;
                requestedAt = Time.realtimeSinceStartup;
                rpc.Invoke(CharacterRpc.ReservePlayer, position);
                CharacterRpc.Log.LogInfo("Spawn position validated; requesting the server-created player.");
            }
            else if (Time.realtimeSinceStartup - requestedAt > 30f)
            {
                CharacterRpc.Log.LogError("Server player creation timed out.");
                Game.instance.Logout(false);
            }
            return false;
        }

        internal static void Receive(ZRpc rpc, ZPackage package)
        {
            if (ZNet.instance == null || rpc != connection || rpc != ZNet.instance.GetServerRPC() || !waiting) return;
            try
            {
                var id = package.ReadZDOID();
                var position = package.ReadVector3();
                long owner = package.ReadLong();
                var data = package.ReadPackage();
                if (id.IsNone() || owner != ZNet.GetUID()) throw new InvalidOperationException("Invalid player reservation.");
                var zdo = ZDOMan.instance.GetZDO(id) ?? CreateExisting(ZDOMan.instance, id, position, 0);
                zdo.Deserialize(data);
                if (zdo.GetPrefab() != Game.instance.m_playerPrefab.name.GetStableHashCode())
                    throw new InvalidOperationException("Unexpected reserved player prefab.");
                zdo.SetOwner(owner);
                Reserved = zdo;
                waiting = false;
                CharacterRpc.Log.LogInfo("Server-created player received; completing spawn at the validated position.");
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); Game.instance.Logout(false); }
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZDOMan), "CreateNewZDO", typeof(ZDOID), typeof(Vector3), typeof(int))]
        private static ZDO CreateExisting(ZDOMan instance, ZDOID id, Vector3 position, int prefabHash)
            => throw new NotImplementedException("Native existing-ZDO creation was not patched.");
    }

    [HarmonyPatch(typeof(Game), "FindSpawnPoint")]
    internal static class WaitForServerPlayerPatch
    {
        private static bool Prefix(ref Vector3 point, ref bool usedLogoutPoint, ref bool __result)
        {
            if (!ClientDamageGuard.Active || !PlayerCreation.HasSpawnPoint) return true;
            point = PlayerCreation.SpawnPoint;
            usedLogoutPoint = PlayerCreation.UsedLogoutPoint;
            __result = PlayerCreation.Ready(point);
            return false;
        }

        private static void Postfix(Vector3 point, bool usedLogoutPoint, ref bool __result)
        {
            if (!__result || !ClientDamageGuard.Active || PlayerCreation.HasSpawnPoint) return;
            // Native validation may clear the logout point. Do not repeat it while awaiting the reserved ZDO.
            PlayerCreation.SpawnPoint = point;
            PlayerCreation.UsedLogoutPoint = usedLogoutPoint;
            PlayerCreation.HasSpawnPoint = true;
            __result = PlayerCreation.Ready(point);
        }
    }

    [HarmonyPatch(typeof(Game), "SpawnPlayer")]
    internal static class UseServerPlayerPatch
    {
        private static void Prefix(Vector3 spawnPoint, ref bool spawnValkyrie)
        {
            if (!ClientDamageGuard.Active) return;
            if (PlayerCreation.Reserved == null) throw new InvalidOperationException("No server-created player is available.");
            PlayerCreation.Reserved.SetPosition(spawnPoint);
            ZNetView.m_initZDO = PlayerCreation.Reserved;
            ZNetView.m_useInitZDO = true;
            // The client may not create the networked intro Valkyrie.
            spawnValkyrie = false;
        }

        private static Exception Finalizer(Exception __exception)
        {
            if (ClientDamageGuard.Active)
            {
                ZNetView.m_initZDO = null;
                ZNetView.m_useInitZDO = false;
                PlayerCreation.Reserved = null;
                PlayerCreation.HasSpawnPoint = false;
            }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "CreateObject")]
    internal static class LocalPlayerSceneCreationGuard
    {
        private static bool Prefix(ZDO zdo, ref GameObject __result)
        {
            if (!ClientDamageGuard.Active || zdo.GetOwner() != ZNet.GetUID() ||
                zdo.GetPrefab() != Game.instance.m_playerPrefab.name.GetStableHashCode()) return true;
            __result = null;
            return false;
        }
    }
}
