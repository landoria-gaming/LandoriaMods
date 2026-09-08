using System;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal sealed class CreatureAppearanceObserved
    {
        public int schemaVersion = 1;
        public string kind = "creature_appearance_observed";
        public long sequence;
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string observation;
        public string worldId;
        public string observerName;
        public string observerCharacterId;
        public string observerSessionId;
        public Vector3? observerPosition;
        public string creatureName;
        public string creatureNetworkId;
        public string networkIdUserComponent;
        public string ownerSessionId;
        public string ownerSessionIdAtInitialization;
        public Vector3 position;
        public Vector3 velocity;
        public int level;
        public float health;
        public bool tamed;
        public float nearbyRadius = 30f;
        public NearbyPlayerEvidence[] nearbyPlayers;

        internal static CreatureAppearanceObserved Capture(Character creature, ZNetView view)
        {
            Player observer = Player.m_localPlayer;
            long owner = view.GetZDO().GetOwner();
            return new CreatureAppearanceObserved
            {
                worldId = ZNet.instance ? ZNet.instance.GetWorldUID().ToString() : null,
                observerName = observer ? observer.GetPlayerName() : null,
                observerCharacterId = observer ? observer.GetPlayerID().ToString() : null,
                observerPosition = observer ? (Vector3?)observer.transform.position : null,
                observerSessionId = ZNet.instance ? ZNet.GetUID().ToString() : null,
                creatureName = creature.gameObject.name, creatureNetworkId = view.GetZDO().m_uid.ToString(),
                networkIdUserComponent = view.GetZDO().m_uid.UserID.ToString(),
                ownerSessionId = owner.ToString(), position = creature.transform.position,
                velocity = creature.GetVelocity(), level = creature.GetLevel(),
                health = creature.GetHealth(), tamed = creature.IsTamed(),
                nearbyPlayers = Player.GetAllPlayers().Where(player => player)
                    .Select(player => NearbyPlayerEvidence.Capture(player, creature.transform.position, owner))
                    .Where(player => player.distance <= 30f).OrderBy(player => player.distance).ToArray()
            };
        }
    }

    internal static class CreatureObservationJournal
    {
        private sealed class State
        {
            internal bool existing;
            internal long initialOwner;
            internal bool recorded;
        }
        private static readonly ConditionalWeakTable<ZNetView, State> states = new();

        internal static void Initialize(ZNetView view, bool existing)
        {
            if (!view || !view.IsValid()) return;
            Character creature = view.GetComponent<Character>();
            if (!creature || creature is Player) return;
            State state = states.GetValue(view, key => new State());
            state.existing = existing;
            state.initialOwner = view.GetZDO().GetOwner();
        }

        internal static void Observe(Character creature)
        {
            if (!creature || creature is Player) return;
            try
            {
                ZNetView view = creature.GetComponent<ZNetView>();
                if (!view || !view.IsValid()) return;
                bool known = states.TryGetValue(view, out State state);
                if (!known || !state.existing || state.recorded) return;
                CreatureAppearanceObserved entry = CreatureAppearanceObserved.Capture(creature, view);
                states.GetValue(view, key => new State()).recorded = true;
                if (entry.nearbyPlayers.Length == 0) return;
                entry.observation = "existing_network_object_loaded";
                entry.ownerSessionIdAtInitialization = state.initialOwner.ToString();
                ActivityJournal.Record(entry);
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }

    [HarmonyPatch(typeof(Character), "Start")]
    internal static class CreatureStartedPatch
    {
        private static void Postfix(Character __instance) => CreatureObservationJournal.Observe(__instance);
    }

    [HarmonyPatch(typeof(Humanoid), "Start")]
    internal static class HumanoidCreatureStartedPatch
    {
        private static void Postfix(Humanoid __instance) => CreatureObservationJournal.Observe(__instance);
    }
}
