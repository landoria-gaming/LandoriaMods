using System;
using System.IO;
using HarmonyLib;
using UnityEngine;
using Landoria.ServerInventory.Network;
using Landoria.ServerInventory.Serialization;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch(typeof(Game), "UpdateRespawn")]
    internal static class CharacterLoad
    {
        internal static PlayerProfile Profile;
        internal static byte[] LoadingPayload;
        private static PlayerProfile loadingProfile;
        private static string requestId;
        private static ZRpc connection;
        private static float requestedAt;

        private static bool Prefix(ref PlayerProfile ___m_playerProfile, ref bool ___m_queuedIntro, ref bool ___m_inIntro)
        {
            var net = ZNet.instance;
            if (net == null || net.IsServer()) return true;
            var peer = net.GetServerPeer();
            if (peer == null) return false;
            if (!net.IsCurrentServerDedicated()) return true;
            if (connection != peer.m_rpc) Reset(peer.m_rpc);
            if (Profile != null)
            {
                if (!ReferenceEquals(___m_playerProfile, Profile))
                {
                    ___m_playerProfile = Profile;
                    ___m_queuedIntro = Profile.m_firstSpawn;
                    ___m_inIntro = Profile.m_firstSpawn;
                }
                return true;
            }
            if (requestId == null)
            {
                requestId = Guid.NewGuid().ToString("D");
                requestedAt = Time.realtimeSinceStartup;
                try { connection.Invoke(CharacterRpc.Request, requestId, InitialAppearance.Read(Game.instance.GetPlayerProfile())); }
                catch (Exception error)
                {
                    CharacterRpc.Log.LogError(error);
                    Game.instance.Logout(save: false);
                }
            }
            else if (Time.realtimeSinceStartup - requestedAt > 30f)
            {
                CharacterRpc.Log.LogError("Server character load timed out; no local character was used.");
                Game.instance.Logout(save: false);
            }
            return false;
        }

        internal static void Receive(ZRpc rpc, string id, ZPackage package)
        {
            if (rpc != connection || id != requestId || Profile != null ||
                rpc != ZNet.instance.GetServerRPC()) return;
            try
            {
                byte[] fch = package.ReadByteArray();
                FchJsonConverter.ToJson(fch);
                var previous = Game.instance.GetPlayerProfile();
                loadingProfile = new PlayerProfile(previous.m_filename, previous.m_fileSource);
                using (var reader = new BinaryReader(new MemoryStream(fch)))
                    LoadingPayload = reader.ReadBytes(reader.ReadInt32());
                if (!loadingProfile.Load()) throw new InvalidDataException("Server character could not be loaded.");
                Profile = loadingProfile;
                CharacterRpc.Log.LogInfo("Server character received; spawn can proceed.");
            }
            catch (Exception error)
            {
                CharacterRpc.Log.LogError(error);
                Game.instance.Logout(save: false);
            }
            finally { loadingProfile = null; LoadingPayload = null; }
        }

        internal static bool IsLoading(PlayerProfile profile) => ReferenceEquals(profile, loadingProfile);

        internal static void Reset(ZRpc rpc = null)
        {
            PlayerCreation.Reset();
            Profile = null;
            loadingProfile = null;
            LoadingPayload = null;
            requestId = null;
            connection = rpc;
        }
    }

    [HarmonyPatch(typeof(PlayerProfile), "LoadPlayerDataFromDisk")]
    internal static class ServerProfileDataPatch
    {
        private static bool Prefix(PlayerProfile __instance, ref ZPackage __result)
        {
            if (!CharacterLoad.IsLoading(__instance)) return true;
            __result = new ZPackage(CharacterLoad.LoadingPayload);
            return false;
        }
    }

    [HarmonyPatch(typeof(Game), "Update")]
    internal static class CharacterIntroPatch
    {
        private static void Prefix(ref bool ___m_queuedIntro)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer() && ZNet.instance.IsCurrentServerDedicated() && CharacterLoad.Profile == null)
                ___m_queuedIntro = false;
        }
    }

    [HarmonyPatch(typeof(Game), "Awake")]
    internal static class CharacterSessionPatch
    {
        private static void Prefix() => CharacterLoad.Reset();
    }
}
