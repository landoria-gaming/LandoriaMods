using Landoria.RavenWatch.Shared;
using System;
using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Journal
{
    [HarmonyPatch(typeof(ZRpc), "HandlePackage")]
    internal static class ReceivedRpcPatch
    {
        internal sealed class State { internal JObject Previous; }

        private static void Prefix(ZRpc __instance, ZPackage package, bool ___m_DEBUG, out State __state)
        {
            __state = null;
            if (!RpcCapture.CaptureEnabled) return;
            try { __state = new State { Previous = RpcCapture.Begin(__instance, package, ___m_DEBUG) }; }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }

        private static Exception Finalizer(Exception __exception, State __state)
        {
            if (__state == null) return __exception;
            try { RpcCapture.End(__state.Previous, __exception); }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ZRpc), "SendPackage")]
    internal static class SentRpcPatch
    {
        private static void Prefix(ZRpc __instance, ZPackage pkg, bool ___m_DEBUG, out JObject __state)
        {
            __state = null;
            if (!RpcCapture.CaptureEnabled) return;
            // PlayFab appends a four-byte sequence and a one-byte message type during Send.
            // Decode the RPC before that mutation; record it only after SendPackage returns.
            try { __state = RpcCapture.Outgoing(__instance, pkg, ___m_DEBUG); }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }

        private static void Postfix(JObject __state)
        {
            if (__state == null) return;
            try { RpcCapture.Sent(__state); }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }

    [HarmonyPatch(typeof(ZNet), "Shutdown")]
    internal static class RpcShutdownPatch
    {
        private static void Postfix()
        {
            try { Network.InventoryPollServer.Clear(); RpcCapture.Close(); }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }
}
