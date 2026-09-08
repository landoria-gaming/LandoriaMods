using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Landoria.RavenWatch
{
    internal static class ContainerWitness
    {
        private sealed class State
        {
            internal bool initialized;
            internal bool inUse;
            internal long owner;
        }
        private static readonly ConditionalWeakTable<Container, State> states = new();

        internal static void Observe(Container container, ZNetView view, bool baseline)
        {
            if (!container || !view || !view.IsValid()) return;
            try
            {
                State state = states.GetValue(container, key => new State());
                bool inUse = view.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
                long owner = view.GetZDO().GetOwner();
                bool opened = state.initialized && !state.inUse && inUse;
                long previousOwner = state.owner;
                state.initialized = true;
                state.inUse = inUse;
                state.owner = owner;
                if (baseline || !opened || !Player.m_localPlayer || view.IsOwner()) return;
                ActivityJournal.Record(ContainerObservation.Capture(container, view, previousOwner));
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }

    [HarmonyPatch(typeof(Container), "Awake")]
    internal static class ContainerObservationBaselinePatch
    {
        private static void Postfix(Container __instance, ZNetView ___m_nview)
            => ContainerWitness.Observe(__instance, ___m_nview, true);
    }

    [HarmonyPatch(typeof(Container), "CheckForChanges")]
    internal static class ContainerObservationPatch
    {
        private static void Postfix(Container __instance, ZNetView ___m_nview)
            => ContainerWitness.Observe(__instance, ___m_nview, false);
    }
}
