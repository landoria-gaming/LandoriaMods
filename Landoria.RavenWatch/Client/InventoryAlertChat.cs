using System;
using HarmonyLib;
using Landoria.RavenWatch.Shared;
using UnityEngine;

namespace Landoria.RavenWatch.Client
{
    internal static class InventoryAlertChat
    {
        private static float visibleUntil;
        internal static bool Visible => Time.realtimeSinceStartup < visibleUntil;
        internal static void Register(ZNetPeer peer)
            => peer.m_rpc.Register<string>(InventoryProtocol.Unverified, Show);

        private static void Show(ZRpc rpc, string message)
        {
            if (rpc != ZNet.instance?.GetServerPeer()?.m_rpc || string.IsNullOrWhiteSpace(message)) return;
            try
            {
                var chat = Chat.instance;
                if (chat == null) return;
                string safe = message.Replace('<', ' ').Replace('>', ' ').Replace('\n', ' ').Replace('\r', ' ');
                if (safe.Length > 1500) safe = safe.Substring(0, 1500) + "…";
                chat.AddString("<color=#FFD700>[RavenWatch]</color> " + safe);
                visibleUntil = Time.realtimeSinceStartup + 10f;
                chat.m_chatWindow.gameObject.SetActive(true);
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }

    [HarmonyPatch(typeof(Chat), nameof(Chat.Update))]
    internal static class InventoryAlertVisibilityPatch
    {
        private static void Prefix(ref float ___m_hideTimer)
        {
            if (InventoryAlertChat.Visible) ___m_hideTimer = 0f;
        }
    }
}
