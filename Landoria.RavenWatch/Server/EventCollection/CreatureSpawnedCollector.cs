using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal static class ServerCreatureCollector
    {
        private sealed class State
        {
            internal bool createdLocally;
            internal bool recorded;
        }

        private static readonly ConditionalWeakTable<ZNetView, State> states = new();

        internal static void Initialize(ZNetView view, bool createdLocally)
        {
            if (!view || !view.IsValid()) return;
            Character creature = view.GetComponent<Character>();
            if (!creature || creature is Player) return;
            states.GetValue(view, key => new State()).createdLocally = createdLocally;
        }

        internal static void Observe(Character creature)
        {
            if (!creature || creature is Player || ZNet.instance == null || !ZNet.instance.IsServer())
                return;
            try
            {
                ZNetView view = creature.GetComponent<ZNetView>();
                if (!view || !view.IsValid()) return;
                bool known = states.TryGetValue(view, out State state);
                state = state ?? states.GetValue(view, key => new State());
                if (!known || !state.createdLocally || state.recorded) return;
                state.recorded = true;
                ServerEventPublisher.Publish(CreatureSpawned.Capture(creature, view.GetZDO()));
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }

    [HarmonyPatch(typeof(ZNetView), "Awake")]
    internal static class ServerCreatureNetworkViewPatch
    {
        private static void Prefix(ref bool __state) => __state = ZNetView.m_initZDO == null;

        private static void Postfix(ZNetView __instance, bool __state)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            try { ServerCreatureCollector.Initialize(__instance, __state); }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }

    [HarmonyPatch(typeof(Character), "Start")]
    internal static class ServerCreatureStartedPatch
    {
        private static void Postfix(Character __instance) => ServerCreatureCollector.Observe(__instance);
    }

    [HarmonyPatch(typeof(Humanoid), "Start")]
    internal static class ServerHumanoidCreatureStartedPatch
    {
        private static void Postfix(Humanoid __instance) => ServerCreatureCollector.Observe(__instance);
    }

    internal static class ServerZdoPacketCollector
    {
        private static readonly List<ZDOID> created = new List<ZDOID>();
        private static ZRpc currentRpc;

        internal static void Begin(ZRpc rpc)
        {
            currentRpc = ZNet.instance != null && ZNet.instance.IsServer() ? rpc : null;
            created.Clear();
        }

        internal static void NoteCreated(ZDOID uid)
        {
            if (currentRpc != null) created.Add(uid);
        }

        internal static void RecordCreated()
        {
            if (currentRpc == null || ZDOMan.instance == null || ZNetScene.instance == null) return;
            ZNetPeer sender = ZNet.instance.GetPeers()
                .FirstOrDefault(peer => peer.m_rpc == currentRpc);
            foreach (ZDOID uid in created)
            {
                ZDO zdo = ZDOMan.instance.GetZDO(uid);
                if (zdo == null) continue;
                GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
                if (!prefab) continue;
                ServerGroundItemCollector.Observe(zdo, prefab, sender);
                Character creature = prefab.GetComponent<Character>();
                if (!creature || creature is Player) continue;
                ServerEventPublisher.Publish(CreatureSpawned.Capture(zdo, prefab, sender));
            }
        }

        internal static void End()
        {
            currentRpc = null;
            created.Clear();
        }
    }

    [HarmonyPatch(typeof(ZDOMan), "RPC_ZDOData")]
    internal static class ServerZdoPacketPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ZRpc rpc) => ServerZdoPacketCollector.Begin(rpc);

        [HarmonyPostfix]
        private static void Postfix() => ServerZdoPacketCollector.RecordCreated();

        [HarmonyFinalizer]
        private static void Finalizer() => ServerZdoPacketCollector.End();
    }

    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateNewZDO),
        new Type[] { typeof(ZDOID), typeof(Vector3), typeof(int) })]
    internal static class ServerNewZdoPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ZDOID uid) => ServerZdoPacketCollector.NoteCreated(uid);
    }
}
