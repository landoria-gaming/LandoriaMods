using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Landoria.StructureProtection
{
    internal static class CharacterActivityRegistry
    {
        private const string RecordTypeKey = "Landoria.StructureProtection.Activity.Type";
        private const string PlatformIdKey = "Landoria.StructureProtection.Activity.PlatformId";
        private const string PlayerNameKey = "Landoria.StructureProtection.Activity.PlayerName";
        private const string CharacterKeyKey = "Landoria.StructureProtection.Activity.CharacterKey";
        private const string CharacterIdKey = "Landoria.StructureProtection.Activity.CharacterId";
        private const string FirstConnectedKey = "Landoria.StructureProtection.Activity.FirstConnectedUtc";
        private const string LastSeenOnlineKey =
            "Landoria.StructureProtection.Activity.LastSeenOnlineUtc";
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

        internal static void Record(string platformId, long characterId, string playerName, DateTime seenOnlineUtc)
        {
            ActivityObservation observation = new ActivityObservation(
                platformId, characterId, playerName, seenOnlineUtc);
            if (world == null || scanning)
            {
                Pending.Add(observation);
                return;
            }
            Record(observation);
        }

        internal static bool TryGetPlatformLastSeenOnlineUtc(
            string platformPlayerId, out DateTime lastSeenOnlineUtc)
        {
            lastSeenOnlineUtc = default;
            return platformPlayerId != null &&
                Platforms.TryGetValue(platformPlayerId, out ZDO zdo) &&
                TryReadLastSeenOnlineUtc(zdo, out lastSeenOnlineUtc);
        }

        internal static bool TryGetCharacterLastSeenOnlineUtc(
            long characterId, out DateTime lastSeenOnlineUtc)
        {
            lastSeenOnlineUtc = default;
            return Characters.TryGetValue(characterId, out ZDO zdo) &&
                TryReadLastSeenOnlineUtc(zdo, out lastSeenOnlineUtc);
        }

        private static void BeginScan(ZDOMan manager)
        {
            Platforms.Clear();
            Characters.Clear();
            Scanned.Clear();
            world = manager;
            scanIndex = 0;
            scanning = true;
            StructureProtectionPlugin.Log.LogInfo("Started character activity reconstruction.");
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
            StructureProtectionPlugin.Log.LogInfo(
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
            long ticks = observation.SeenOnlineUtc.ToUniversalTime().Ticks;
            ZDO platform = GetOrCreatePlatform(observation.PlatformId, ticks);
            platform.Set(LastSeenOnlineKey, ticks);
            ZDO character = GetOrCreateCharacter(observation, ticks);
            character.Set(PlatformIdKey, observation.PlatformId);
            character.Set(PlayerNameKey, observation.PlayerName);
            character.Set(CharacterKeyKey, observation.CharacterKey);
            character.Set(LastSeenOnlineKey, ticks);
            StructureProtectionPlugin.Log.LogInfo(
                $"Recorded last seen online for {observation.CharacterKey} at " +
                $"{observation.SeenOnlineUtc:O}.");
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

        private static bool TryReadLastSeenOnlineUtc(ZDO zdo, out DateTime value)
        {
            return TryReadUtc(zdo, LastSeenOnlineKey, out value);
        }

        private static string SafeSegment(string value)
        {
            const string invalid = "<>:\"/\\|?*";
            return new string(value
                .Select(character => char.IsControl(character) || invalid.Contains(character)
                    ? '_' : character)
                .ToArray());
        }

        private sealed class ActivityObservation
        {
            internal ActivityObservation(
                string platformId, long characterId, string playerName, DateTime seenOnlineUtc)
            {
                PlatformId = platformId;
                CharacterId = characterId;
                PlayerName = playerName;
                SeenOnlineUtc = seenOnlineUtc;
                CharacterKey = $"{SafeSegment(platformId)}_" +
                    SafeSegment(playerName);
            }

            internal string PlatformId { get; }
            internal long CharacterId { get; }
            internal string PlayerName { get; }
            internal DateTime SeenOnlineUtc { get; }
            internal string CharacterKey { get; }
        }
    }
}
