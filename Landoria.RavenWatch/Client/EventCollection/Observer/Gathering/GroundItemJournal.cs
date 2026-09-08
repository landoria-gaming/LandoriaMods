using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Landoria.RavenWatch
{
    internal static class GroundItemJournal
    {
        private sealed class Observation
        {
            internal bool existing;
            internal bool recorded;
            internal long ownerAtInitialization;
        }
        private static readonly ConditionalWeakTable<ZNetView, Observation> observations = new();

        internal static void Initialize(ZNetView view, bool existing)
        {
            if (!view || !view.IsValid() || !view.GetComponent<ItemDrop>()) return;
            Observation state = observations.GetValue(view, key => new Observation());
            state.existing = existing;
            state.ownerAtInitialization = view.GetZDO().GetOwner();
        }

        internal static void Observe(ItemDrop drop)
        {
            if (!drop) return;
            try
            {
                ZNetView view = drop.GetComponent<ZNetView>();
                if (!view || !view.IsValid()) return;
                bool known = observations.TryGetValue(view, out Observation state);
                if (!known || !state.existing || state.recorded) return;
                ItemObservation entry = ItemObservation.Capture("ground_item_observed", drop.m_itemData);
                entry.observation = "existing_network_object_loaded";
                entry.sourceNetworkId = view.GetZDO().m_uid.ToString();
                entry.sourcePosition = drop.transform.position;
                entry.groundEvidence = GroundItemEvidence.Capture(drop, view,
                    state.ownerAtInitialization);
                ActivityJournal.Record(entry);
                observations.GetValue(view, key => new Observation()).recorded = true;
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }

    [HarmonyPatch(typeof(ZNetView), "Awake")]
    internal static class GroundNetworkOriginPatch
    {
        private static void Prefix(out bool __state) => __state = ZNetView.m_initZDO != null;
        private static void Postfix(ZNetView __instance, bool __state)
        {
            try
            {
                GroundItemJournal.Initialize(__instance, __state);
                CreatureObservationJournal.Initialize(__instance, __state);
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }

    [HarmonyPatch(typeof(ItemDrop), "Start")]
    internal static class GroundItemStartedPatch
    {
        private static void Postfix(ItemDrop __instance) => GroundItemJournal.Observe(__instance);
    }
}
