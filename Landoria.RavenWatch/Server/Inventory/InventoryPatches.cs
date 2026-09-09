using Landoria.RavenWatch.Server.Journal;
using System;
using HarmonyLib;
using UnityEngine;

namespace Landoria.RavenWatch.Server.Inventory
{
    [HarmonyPatch(typeof(ZDOMan), "RPC_ZDOData")]
    internal static class InventoryReceivePatch
    {
        private static void Prefix(ZRpc rpc, out InventoryCapture.ReceiveScope __state)
        {
            __state = InventoryCapture.Current;
            if (!RpcCapture.Enabled) return;
            try { __state = InventoryCapture.Begin(rpc); }
            catch (Exception error) { InventoryCapture.Current = null; RpcCapture.Log.LogError(error); }
        }

        private static Exception Finalizer(Exception __exception, InventoryCapture.ReceiveScope __state)
        {
            InventoryCapture.Current = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ZDOMan), "CreateNewZDO", new Type[] { typeof(ZDOID), typeof(Vector3), typeof(int) })]
    internal static class InventoryCreatedZdoPatch
    {
        private static void Postfix(ZDO __result)
        {
            if (InventoryCapture.Current == null || __result == null) return;
            try { InventoryCapture.Current.Created.Add(__result.m_uid); }
            catch (Exception error) { RpcCapture.Log.LogError(error); }
        }
    }

    [HarmonyPatch(typeof(ZDO), "Deserialize", new Type[] { typeof(ZPackage) })]
    internal static class InventoryDeserializedPatch
    {
        private static void Prefix(ZDO __instance, out byte[] __state)
        {
            __state = null;
            try { __state = TombstoneCapture.Before(__instance); }
            catch (Exception error) { RpcCapture.Log.LogError(error); }
        }

        private static void Postfix(ZDO __instance, byte[] __state)
        {
            if (InventoryCapture.Current == null) return;
            try { InventoryCapture.Received(__instance); }
            catch (Exception error) { RpcCapture.Log.LogError(error); }
            try { TombstoneCapture.After(__instance, __state); }
            catch (Exception error) { RpcCapture.Log.LogError(error); }
        }
    }

    [HarmonyPatch(typeof(ZDOMan), "RPC_DestroyZDO")]
    internal static class InventoryDestroyPatch
    {
        private static void Prefix(long sender, ZPackage pkg)
        {
            if (!RpcCapture.Enabled) return;
            try { InventoryCapture.Destroyed(sender, pkg); }
            catch (Exception error) { RpcCapture.Log.LogError(error); }
        }
    }
}
