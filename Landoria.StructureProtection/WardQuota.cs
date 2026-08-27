using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Landoria.StructureProtection
{
    internal static class WardQuota
    {
        internal const string OwnerKey = "Landoria.StructureProtection.WardOwner";
        private const string QuotaRpc = "Landoria_StructureProtection_WardQuota";
        private const string WardPrefab = "guard_stone";
        private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
        private static readonly Dictionary<ZDOID, string> Tracked = new Dictionary<ZDOID, string>();
        private static readonly Dictionary<ZDOID, long> LegacyCreators =
            new Dictionary<ZDOID, long>();
        private static readonly Dictionary<long, string> Identities = new Dictionary<long, string>();
        private static readonly HashSet<PrivateArea> Pending = new HashSet<PrivateArea>();
        private static readonly HashSet<ZDOID> AllWards = new HashSet<ZDOID>();
        private static readonly List<ZDO> ScannedWards = new List<ZDO>();
        private static ZDOMan world;
        private static ZRoutedRpc registeredRpc;
        private static float nextReconciliation;
        private static int scanIndex;
        private static bool rebuilding;
        private static string localOwner;
        private static int localCount;
        private static bool localReady;

        internal static void Update()
        {
            RegisterRpc();
            if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null)
            {
                return;
            }
            if (!ReferenceEquals(world, ZDOMan.instance))
            {
                BeginRebuild(ZDOMan.instance);
            }
            if (rebuilding)
            {
                ContinueRebuild();
                return;
            }
            ProcessPending();
            if (Time.unscaledTime >= nextReconciliation)
            {
                ReconcileDestroyed();
                nextReconciliation = Time.unscaledTime + 5f;
            }
        }

        internal static void Reset()
        {
            Counts.Clear();
            Tracked.Clear();
            LegacyCreators.Clear();
            Identities.Clear();
            Pending.Clear();
            AllWards.Clear();
            ScannedWards.Clear();
            world = null;
            registeredRpc = null;
            scanIndex = 0;
            rebuilding = false;
            localOwner = null;
            localCount = 0;
            localReady = false;
        }

        internal static void RegisterIdentity(long peerId, long characterId)
        {
            RegisterRpc();
            ZNetPeer peer = ZNet.instance?.GetPeer(peerId);
            if (peer?.m_socket == null || string.IsNullOrWhiteSpace(peer.m_playerName))
            {
                StructureProtectionPlugin.Log.LogWarning(
                    $"Could not register ward quota identity for peer {peerId}.");
                return;
            }
            string owner = MakeOwner(peer.m_socket.GetHostName(), peer.m_playerName);
            Identities[characterId] = owner;
            StructureProtectionPlugin.Log.LogInfo(
                $"Registered ward quota identity {owner} for character {characterId}.");
            if (!rebuilding && world != null)
            {
                AttributeLegacyWards(characterId, owner);
                SendQuota(peerId, owner);
            }
        }

        internal static void RemoveIdentity(long characterId)
        {
            Identities.Remove(characterId);
        }

        internal static void ResetClientState()
        {
            localOwner = null;
            localCount = 0;
            localReady = false;
        }

        internal static bool TryGetWardSnapshot(out List<ZDO> wards)
        {
            wards = new List<ZDO>();
            if (world == null || rebuilding)
            {
                return false;
            }
            foreach (ZDOID id in AllWards)
            {
                ZDO zdo = world.GetZDO(id);
                if (zdo != null)
                {
                    wards.Add(zdo);
                }
            }
            return true;
        }

        internal static void Observe(PrivateArea ward)
        {
            if (ward != null && ZNet.instance != null && ZNet.instance.IsServer())
            {
                Pending.Add(ward);
            }
        }

        internal static void TagLocalWard(PrivateArea ward)
        {
            ZDO zdo = ward?.GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null || string.IsNullOrWhiteSpace(localOwner))
            {
                return;
            }
            zdo.Set(OwnerKey, localOwner);
            localCount++;
            StructureProtectionPlugin.Log.LogInfo(
                $"Tagged newly placed ward for {localOwner}; local count is {localCount}.");
        }

        internal static bool CanPlaceLocalWard(Player player, Piece piece)
        {
            int maximum = StructureProtectionPlugin.Settings.MaximumWardsPerCharacter;
            if (player != Player.m_localPlayer || piece?.GetComponent<PrivateArea>() == null)
            {
                return true;
            }
            if (!localReady)
            {
                player.Message(MessageHud.MessageType.Center, "Ward limits are still loading.");
                return false;
            }
            if (maximum < 0 || localCount < maximum)
            {
                return true;
            }
            player.Message(MessageHud.MessageType.Center,
                $"Ward limit reached ({localCount}/{maximum}).");
            StructureProtectionPlugin.Log.LogInfo(
                $"Blocked ward placement for {localOwner}: {localCount}/{maximum} wards.");
            return false;
        }

        private static void RegisterRpc()
        {
            if (ZRoutedRpc.instance == null || ReferenceEquals(registeredRpc, ZRoutedRpc.instance))
            {
                return;
            }
            registeredRpc = ZRoutedRpc.instance;
            registeredRpc.Register<ZPackage>(QuotaRpc, ReceiveQuota);
        }

        private static void BeginRebuild(ZDOMan manager)
        {
            Counts.Clear();
            Tracked.Clear();
            LegacyCreators.Clear();
            AllWards.Clear();
            ScannedWards.Clear();
            world = manager;
            scanIndex = 0;
            rebuilding = true;
            StructureProtectionPlugin.Log.LogInfo("Started iterative ward quota reconstruction.");
        }

        private static void ContinueRebuild()
        {
            if (!world.GetAllZDOsWithPrefabIterative(WardPrefab, ScannedWards, ref scanIndex))
            {
                return;
            }
            int legacy = ReadScannedWards();
            rebuilding = false;
            StructureProtectionPlugin.Log.LogInfo(
                $"Rebuilt ward quotas from the world: {Tracked.Count} tracked wards, " +
                $"{Counts.Count} characters, {legacy} legacy wards awaiting attribution.");
            foreach (KeyValuePair<long, string> identity in Identities.ToArray())
            {
                AttributeLegacyWards(identity.Key, identity.Value);
            }
            foreach (string owner in Identities.Values.Distinct())
            {
                SendQuotaForOwner(owner);
            }
        }

        private static int ReadScannedWards()
        {
            int legacy = 0;
            foreach (ZDO zdo in ScannedWards)
            {
                if (zdo == null)
                {
                    continue;
                }
                AllWards.Add(zdo.m_uid);
                string owner = zdo.GetString(OwnerKey);
                if (string.IsNullOrWhiteSpace(owner))
                {
                    LegacyCreators[zdo.m_uid] = zdo.GetLong(ZDOVars.s_creator);
                    legacy++;
                    continue;
                }
                Track(zdo.m_uid, owner);
            }
            ScannedWards.Clear();
            return legacy;
        }

        private static void ProcessPending()
        {
            foreach (PrivateArea ward in Pending.ToArray())
            {
                ProcessPendingWard(ward);
            }
        }

        private static void ProcessPendingWard(PrivateArea ward)
        {
            ZDO zdo = ward?.GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null || Tracked.ContainsKey(zdo.m_uid))
            {
                Pending.Remove(ward);
                return;
            }
            string owner = ResolveOwner(ward, zdo);
            if (owner == null)
            {
                return;
            }
            int maximum = StructureProtectionPlugin.Settings.MaximumWardsPerCharacter;
            if (!string.IsNullOrWhiteSpace(zdo.GetString(OwnerKey)) &&
                maximum >= 0 && GetCount(owner) >= maximum)
            {
                RejectExcessWard(ward, zdo, owner, maximum);
                Pending.Remove(ward);
                return;
            }
            RegisterWard(zdo, owner);
            Pending.Remove(ward);
        }

        private static void RegisterWard(ZDO zdo, string owner)
        {
            AllWards.Add(zdo.m_uid);
            zdo.Set(OwnerKey, owner);
            LegacyCreators.Remove(zdo.m_uid);
            Track(zdo.m_uid, owner);
            SendQuotaForOwner(owner);
            StructureProtectionPlugin.Log.LogInfo(
                $"Registered ward {zdo.m_uid} for {owner}; count is {Counts[owner]}.");
        }

        private static void AttributeLegacyWards(long characterId, string owner)
        {
            if (world == null)
            {
                return;
            }
            int attributed = 0;
            foreach (KeyValuePair<ZDOID, long> legacy in LegacyCreators.ToArray())
            {
                if (legacy.Value != characterId)
                {
                    continue;
                }
                ZDO zdo = world.GetZDO(legacy.Key);
                LegacyCreators.Remove(legacy.Key);
                if (zdo == null)
                {
                    continue;
                }
                zdo.Set(OwnerKey, owner);
                Track(legacy.Key, owner);
                attributed++;
            }
            if (attributed > 0)
            {
                StructureProtectionPlugin.Log.LogInfo(
                    $"Attributed {attributed} legacy wards to {owner}; count is {GetCount(owner)}.");
            }
        }

        private static void RejectExcessWard(
            PrivateArea ward, ZDO zdo, string owner, int maximum)
        {
            StructureProtectionPlugin.Log.LogWarning(
                $"Rejected excess ward {zdo.m_uid} for {owner}: " +
                $"{GetCount(owner)}/{maximum} wards already exist.");
            SendQuotaForOwner(owner);
            ZNetView view = ward.GetComponent<ZNetView>();
            view.ClaimOwnership();
            ZNetScene.instance.Destroy(ward.gameObject);
        }

        private static string ResolveOwner(PrivateArea ward, ZDO zdo)
        {
            string owner = zdo.GetString(OwnerKey);
            if (!string.IsNullOrWhiteSpace(owner))
            {
                return owner;
            }
            long creator = ward.GetComponent<Piece>()?.GetCreator() ?? 0L;
            return Identities.TryGetValue(creator, out owner) ? owner : null;
        }

        private static void ReconcileDestroyed()
        {
            foreach (KeyValuePair<ZDOID, string> entry in Tracked.ToArray())
            {
                if (world.GetZDO(entry.Key) != null)
                {
                    continue;
                }
                Tracked.Remove(entry.Key);
                AllWards.Remove(entry.Key);
                Counts[entry.Value]--;
                if (Counts[entry.Value] == 0)
                {
                    Counts.Remove(entry.Value);
                }
                SendQuotaForOwner(entry.Value);
                StructureProtectionPlugin.Log.LogInfo(
                    $"Removed destroyed ward {entry.Key} from {entry.Value}; " +
                    $"count is {GetCount(entry.Value)}.");
            }
        }

        private static void Track(ZDOID id, string owner)
        {
            Tracked[id] = owner;
            Counts[owner] = GetCount(owner) + 1;
        }

        private static int GetCount(string owner) =>
            Counts.TryGetValue(owner, out int count) ? count : 0;

        private static void SendQuotaForOwner(string owner)
        {
            foreach (KeyValuePair<long, string> identity in Identities)
            {
                if (identity.Value == owner)
                {
                    StructureProtectionSession.TryGetPeer(identity.Key, out long peerId);
                    SendQuota(peerId, owner);
                }
            }
        }

        private static void SendQuota(long peerId, string owner)
        {
            if (registeredRpc == null || peerId == 0L)
            {
                return;
            }
            ZPackage package = new ZPackage();
            package.Write(owner);
            package.Write(GetCount(owner));
            registeredRpc.InvokeRoutedRPC(peerId, QuotaRpc, package);
        }

        private static void ReceiveQuota(long sender, ZPackage package)
        {
            if (!StructureProtectionSession.IsTrustedServer(sender))
            {
                return;
            }
            localOwner = package.ReadString();
            localCount = package.ReadInt();
            localReady = true;
            StructureProtectionPlugin.Log.LogInfo(
                $"Received ward quota for {localOwner}: {localCount}/" +
                $"{StructureProtectionPlugin.Settings.MaximumWardsPerCharacter}.");
        }

        private static string MakeOwner(string platformId, string playerName) =>
            $"{SafeSegment(platformId)}_{SafeSegment(playerName)}";

        private static string SafeSegment(string value)
        {
            const string invalid = "<>:\"/\\|?*";
            return new string(value.Select(character =>
                char.IsControl(character) || invalid.Contains(character) ? '_' : character)
                .ToArray());
        }

        private static void RemoveLocalWard(WearNTear wear)
        {
            ZDO zdo = wear?.GetComponent<PrivateArea>()?.GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null || string.IsNullOrWhiteSpace(localOwner) ||
                zdo.GetString(OwnerKey) != localOwner)
            {
                return;
            }
            localCount = Math.Max(0, localCount - 1);
            StructureProtectionPlugin.Log.LogInfo(
                $"Removed local ward for {localOwner}; local count is {localCount}.");
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        private static class PlacementPatch
        {
            private static bool Prefix(Player __instance, Piece piece, ref bool __result)
            {
                if (CanPlaceLocalWard(__instance, piece))
                {
                    return true;
                }
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.Setup))]
        private static class WardSetupPatch
        {
            private static void Postfix(PrivateArea __instance) => TagLocalWard(__instance);
        }


        [HarmonyPatch(typeof(WearNTear), "Destroy")]
        private static class WardDestroyPatch
        {
            private static void Prefix(WearNTear __instance) => RemoveLocalWard(__instance);
        }
    }
}
