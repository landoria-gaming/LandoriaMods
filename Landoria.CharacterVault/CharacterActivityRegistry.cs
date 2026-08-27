using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landoria.CharacterVault
{
    internal static class CharacterActivityRegistry
    {
        private const string RecordTypeKey = "Landoria.CharacterVault.Activity.Type";
        private const string PlatformIdKey = "Landoria.CharacterVault.Activity.PlatformId";
        private const string PlayerNameKey = "Landoria.CharacterVault.Activity.PlayerName";
        private const string CharacterKeyKey = "Landoria.CharacterVault.Activity.CharacterKey";
        private const string CharacterIdKey = "Landoria.CharacterVault.Activity.CharacterId";
        private const string FirstConnectedKey = "Landoria.CharacterVault.Activity.FirstConnectedUtc";
        private const string LastConnectedKey = "Landoria.CharacterVault.Activity.LastConnectedUtc";
        private const int PlatformRecord = 1;
        private const int CharacterRecord = 2;
        private static readonly Vector3 StoragePosition = new Vector3(100000f, -10000f, 100000f);
        private static readonly Dictionary<string, ZDO> Platforms =
            new Dictionary<string, ZDO>(StringComparer.Ordinal);
        private static readonly Dictionary<long, ZDO> Characters = new Dictionary<long, ZDO>();
        private static readonly List<ZDO> Scanned = new List<ZDO>();
        private static readonly List<ActivityObservation> Pending =
            new List<ActivityObservation>();
        private static ZDOMan world;
        private static int scanIndex;
        private static bool scanning;

        internal static bool IsReady => world != null && !scanning;

        internal static void Update()
        {
            if (ZNet.instance?.IsServer() != true || ZDOMan.instance == null)
            {
                return;
            }
            if (!ReferenceEquals(world, ZDOMan.instance))
            {
                BeginScan(ZDOMan.instance);
            }
            if (scanning)
            {
                ContinueScan();
            }
        }

        internal static void Reset()
        {
            Platforms.Clear();
            Characters.Clear();
            Scanned.Clear();
            Pending.Clear();
            world = null;
            scanIndex = 0;
            scanning = false;
        }

        internal static void Record(VaultSession session, DateTime connectedUtc)
        {
            ActivityObservation observation = new ActivityObservation(
                session.AccountId, session.CharacterId, session.Name, connectedUtc);
            if (world == null || scanning)
            {
                Pending.Add(observation);
                return;
            }
            Record(observation);
        }

        internal static bool TryGetPlatformLastConnectedUtc(
            string platformPlayerId, out DateTime lastConnectedUtc)
        {
            lastConnectedUtc = default;
            return platformPlayerId != null &&
                Platforms.TryGetValue(platformPlayerId, out ZDO zdo) &&
                TryReadUtc(zdo, LastConnectedKey, out lastConnectedUtc);
        }

        internal static bool TryGetCharacterLastConnectedUtc(
            long characterId, out DateTime lastConnectedUtc)
        {
            lastConnectedUtc = default;
            return Characters.TryGetValue(characterId, out ZDO zdo) &&
                TryReadUtc(zdo, LastConnectedKey, out lastConnectedUtc);
        }

        private static void BeginScan(ZDOMan manager)
        {
            Platforms.Clear();
            Characters.Clear();
            Scanned.Clear();
            world = manager;
            scanIndex = 0;
            scanning = true;
            CharacterVaultPlugin.Log.LogInfo("Started character activity reconstruction.");
        }

        private static void ContinueScan()
        {
            if (!world.GetAllZDOsWithPrefabIterative(
                CharacterActivityPrefab.Name, Scanned, ref scanIndex))
            {
                return;
            }
            foreach (ZDO zdo in Scanned)
            {
                ReadRecord(zdo);
            }
            scanning = false;
            CharacterVaultPlugin.Log.LogInfo(
                $"Rebuilt character activity: {Platforms.Count} platform accounts, " +
                $"{Characters.Count} characters.");
            FlushPending();
        }

        private static void ReadRecord(ZDO zdo)
        {
            int type = zdo.GetInt(RecordTypeKey);
            if (type == PlatformRecord)
            {
                AddPlatform(zdo);
            }
            else if (type == CharacterRecord)
            {
                AddCharacter(zdo);
            }
        }

        private static void AddPlatform(ZDO zdo)
        {
            string platformId = zdo.GetString(PlatformIdKey);
            if (!string.IsNullOrWhiteSpace(platformId))
            {
                Platforms[platformId] = zdo;
            }
        }

        private static void AddCharacter(ZDO zdo)
        {
            long characterId = zdo.GetLong(CharacterIdKey);
            if (characterId != 0L)
            {
                Characters[characterId] = zdo;
            }
        }

        private static void FlushPending()
        {
            foreach (ActivityObservation observation in Pending)
            {
                Record(observation);
            }
            Pending.Clear();
        }

        private static void Record(ActivityObservation observation)
        {
            long ticks = observation.ConnectedUtc.ToUniversalTime().Ticks;
            ZDO platform = GetOrCreatePlatform(observation.PlatformId, ticks);
            platform.Set(LastConnectedKey, ticks);
            ZDO character = GetOrCreateCharacter(observation, ticks);
            character.Set(PlatformIdKey, observation.PlatformId);
            character.Set(PlayerNameKey, observation.PlayerName);
            character.Set(CharacterKeyKey, observation.CharacterKey);
            character.Set(LastConnectedKey, ticks);
            CharacterVaultPlugin.Log.LogInfo(
                $"Recorded world activity for {observation.CharacterKey} at " +
                $"{observation.ConnectedUtc:O}.");
        }

        private static ZDO GetOrCreatePlatform(string platformId, long ticks)
        {
            if (Platforms.TryGetValue(platformId, out ZDO zdo))
            {
                return zdo;
            }
            zdo = CreateRecord(PlatformRecord, ticks);
            zdo.Set(PlatformIdKey, platformId);
            Platforms[platformId] = zdo;
            return zdo;
        }

        private static ZDO GetOrCreateCharacter(ActivityObservation observation, long ticks)
        {
            if (Characters.TryGetValue(observation.CharacterId, out ZDO zdo))
            {
                return zdo;
            }
            zdo = CreateRecord(CharacterRecord, ticks);
            zdo.Set(CharacterIdKey, observation.CharacterId);
            Characters[observation.CharacterId] = zdo;
            return zdo;
        }

        private static ZDO CreateRecord(int type, long ticks)
        {
            int prefab = CharacterActivityPrefab.Name.GetStableHashCode();
            ZDO zdo = world.CreateNewZDO(StoragePosition, prefab);
            zdo.Persistent = true;
            zdo.SetPrefab(prefab);
            zdo.Set(RecordTypeKey, type);
            zdo.Set(FirstConnectedKey, ticks);
            return zdo;
        }

        private static bool TryReadUtc(ZDO zdo, string key, out DateTime value)
        {
            long ticks = zdo.GetLong(key);
            if (ticks <= 0L || ticks > DateTime.MaxValue.Ticks)
            {
                value = default;
                return false;
            }
            value = new DateTime(ticks, DateTimeKind.Utc);
            return true;
        }

        private sealed class ActivityObservation
        {
            internal ActivityObservation(
                string platformId, long characterId, string playerName, DateTime connectedUtc)
            {
                PlatformId = platformId;
                CharacterId = characterId;
                PlayerName = playerName;
                ConnectedUtc = connectedUtc;
                CharacterKey = $"{VaultStorage.SafeSegment(platformId)}_" +
                    VaultStorage.SafeSegment(playerName);
            }

            internal string PlatformId { get; }
            internal long CharacterId { get; }
            internal string PlayerName { get; }
            internal DateTime ConnectedUtc { get; }
            internal string CharacterKey { get; }
        }
    }
}
