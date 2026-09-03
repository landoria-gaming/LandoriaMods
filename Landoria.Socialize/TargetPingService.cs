using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Splatform;
using UnityEngine;

namespace Landoria.Socialize
{
    internal static class TargetPingService
    {
        private const string MessageRpc = "Landoria_Social_TargetPingMessage";
        private const string ReceiptRpc = "Landoria_Social_TargetPingReceipt";
        private const float ReceiptTimeoutSeconds = 15f;
        private static readonly MethodInfo AddInworldText = AccessTools.Method(typeof(Chat), "AddInworldText");
        private static readonly Dictionary<string, PendingPing> Pending = new Dictionary<string, PendingPing>();
        private static ZRoutedRpc registeredRpc;

        private sealed class PendingPing
        {
            internal long TargetPeer;
            internal string TargetName;
            internal string Message;
            internal Terminal Context;
            internal float SentAt;
        }

        internal static void Update()
        {
            EnsureRpcs();
            List<string> expired = null;
            foreach (KeyValuePair<string, PendingPing> entry in Pending)
            {
                if (Time.realtimeSinceStartup - entry.Value.SentAt >= ReceiptTimeoutSeconds)
                {
                    (expired ??= new List<string>()).Add(entry.Key);
                }
            }
            if (expired == null) return;
            foreach (string requestId in expired)
            {
                PendingPing pending = Pending[requestId];
                Pending.Remove(requestId);
                SocializePlugin.Log.LogWarning($"Target ping [{requestId}] to '{pending.TargetName}' timed out.");
                pending.Context?.AddString("Ping to " + pending.TargetName + " was not confirmed.");
            }
        }

        internal static void Reset()
        {
            Pending.Clear();
            registeredRpc = null;
        }

        internal static bool Send(string targetName, string message, Terminal context)
        {
            bool found = TryFindTarget(targetName, out ZNet.PlayerInfo target);
            bool ready = Player.m_localPlayer != null && ZRoutedRpc.instance != null;
            bool isLocal = found && ZNet.instance != null &&
                           target.m_characterID.UserID == ZNet.instance.LocalPlayerCharacterID.UserID;
            GroupDecision decision = PrivateChatPolicy.CanSend(found && ready, isLocal, targetName);
            if (!decision.Allowed)
            {
                SocializePlugin.Log.LogWarning($"Target ping to '{targetName}' rejected locally: {decision.Message}");
                context?.AddString(decision.Message);
                return false;
            }
            if (!PlatformManager.DistributionPlatform.PrivilegeProvider
                    .CheckPrivilege(Privilege.TextCommunication).IsGranted())
            {
                SocializePlugin.Log.LogWarning(
                    $"Target ping to '{target.m_name}' blocked by the sender's text privilege.");
                context?.AddString("Text communication is not permitted for this account.");
                return false;
            }

            EnsureRpcs();
            SocializePlugin.Log.LogInfo(
                $"Checking sender permission for target ping to '{target.m_name}' (peer={target.m_characterID.UserID}).");
            TextPermissionService.Check(target.m_userInfo.m_id, true,
                result => CompleteSendPermission(target, message, context, result));
            return true;
        }

        private static void CompleteSendPermission(ZNet.PlayerInfo target, string message,
            Terminal context, RelationsManagerPermissionResult result)
        {
            SocializePlugin.Log.LogInfo(
                $"Sender permission result for target ping to '{target.m_name}': {result}.");
            if (!result.IsGranted())
            {
                context?.AddString("Ping to " + target.m_name + " is not permitted.");
                return;
            }
            Chat.GetChatMessageData(message,
                result == RelationsManagerPermissionResult.GrantedRequiresFiltering,
                out UserInfo user, out string filtered);
            string requestId = Guid.NewGuid().ToString("N").Substring(0, 12);
            long targetPeer = target.m_characterID.UserID;
            Pending[requestId] = new PendingPing
            {
                TargetPeer = targetPeer,
                TargetName = target.m_name,
                Message = filtered,
                Context = context,
                SentAt = Time.realtimeSinceStartup
            };
            SocializePlugin.Log.LogInfo(
                $"Sending target ping [{requestId}] to '{target.m_name}' (peer={targetPeer}, length={filtered.Length}).");
            ZRoutedRpc.instance.InvokeRoutedRPC(
                targetPeer, MessageRpc, requestId, Player.m_localPlayer.GetHeadPoint(), user, filtered);
        }

        private static void RegisterRpcs(ZRoutedRpc rpc)
        {
            rpc.Register<string, Vector3, UserInfo, string>(MessageRpc, RPC_TargetPingMessage);
            rpc.Register<string, int>(ReceiptRpc, RPC_TargetPingReceipt);
            SocializePlugin.Log.LogDebug("Target ping RPCs registered.");
        }

        private static void RPC_TargetPingMessage(
            long sender, string requestId, Vector3 position, UserInfo user, string message)
        {
            if (!IsExpectedUser(sender, user))
            {
                SocializePlugin.Log.LogWarning(
                    $"Target ping [{requestId}] rejected because peer={sender} supplied an invalid identity.");
                ZRoutedRpc.instance?.InvokeRoutedRPC(
                    sender, ReceiptRpc, requestId, (int)RelationsManagerPermissionResult.Error);
                return;
            }
            SocializePlugin.Log.LogInfo(
                $"Target ping [{requestId}] received from peer={sender} (user='{user.GetDisplayName()}', length={message.Length}); checking text permission.");
            TextPermissionService.Check(user.UserId, false,
                result => CompleteReceive(sender, requestId, position, user, message, result));
        }

        private static void CompleteReceive(long sender, string requestId, Vector3 position,
            UserInfo user, string message, RelationsManagerPermissionResult result)
        {
            SocializePlugin.Log.LogInfo($"Target ping [{requestId}] permission result for peer={sender}: {result}.");
            if (!result.IsGranted() || Chat.instance == null || AddInworldText == null)
            {
                RelationsManagerPermissionResult failure = result.IsGranted()
                    ? RelationsManagerPermissionResult.Error : result;
                SocializePlugin.Log.LogWarning($"Target ping [{requestId}] was not displayed: {failure}.");
                ZRoutedRpc.instance?.InvokeRoutedRPC(sender, ReceiptRpc, requestId, (int)failure);
                return;
            }

            string displayed = message.Replace('<', ' ').Replace('>', ' ');
            if (result == RelationsManagerPermissionResult.GrantedRequiresFiltering)
            {
                CensorShittyWords.Filter(displayed, out displayed);
            }
            try
            {
                ChatFormatting.AddPing(Chat.instance, user.GetDisplayName(), "", displayed);
                AddInworldText.Invoke(Chat.instance,
                    new object[] { null, sender, position, Talker.Type.Ping, user, "" });
            }
            catch (Exception exception)
            {
                SocializePlugin.Log.LogError(
                    $"Target ping [{requestId}] display failed: {exception.GetBaseException().Message}");
                ZRoutedRpc.instance?.InvokeRoutedRPC(
                    sender, ReceiptRpc, requestId, (int)RelationsManagerPermissionResult.Error);
                return;
            }
            SocializePlugin.Log.LogInfo($"Target ping [{requestId}] displayed; sending receipt to peer={sender}.");
            ZRoutedRpc.instance?.InvokeRoutedRPC(sender, ReceiptRpc, requestId, (int)result);
        }

        private static void RPC_TargetPingReceipt(long sender, string requestId, int resultValue)
        {
            RelationsManagerPermissionResult result = (RelationsManagerPermissionResult)resultValue;
            if (!Pending.TryGetValue(requestId, out PendingPing pending))
            {
                SocializePlugin.Log.LogWarning(
                    $"Ignoring unknown target ping receipt [{requestId}] from peer={sender}, result={result}.");
                return;
            }
            if (pending.TargetPeer != sender)
            {
                SocializePlugin.Log.LogWarning(
                    $"Ignoring target ping receipt [{requestId}] from peer={sender}; expected={pending.TargetPeer}.");
                return;
            }
            Pending.Remove(requestId);
            if (!result.IsGranted())
            {
                SocializePlugin.Log.LogWarning(
                    $"Target ping [{requestId}] to '{pending.TargetName}' was rejected: {result}.");
                pending.Context?.AddString("Ping to " + pending.TargetName + " was not delivered.");
                return;
            }
            ChatFormatting.AddPing(pending.Context, Game.instance.GetPlayerProfile().GetName(),
                pending.TargetName, pending.Message);
            SocializePlugin.Log.LogInfo(
                $"Target ping [{requestId}] to '{pending.TargetName}' confirmed and shown to the sender.");
        }

        private static void EnsureRpcs()
        {
            RpcRegistry.RegisterIfChanged(ref registeredRpc, RegisterRpcs);
        }

        private static bool TryFindTarget(string targetName, out ZNet.PlayerInfo target)
        {
            if (ZNet.instance != null)
            {
                foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
                {
                    if (string.Equals(player.m_name, targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        target = player;
                        return true;
                    }
                }
            }
            target = default;
            return false;
        }

        private static bool IsExpectedUser(long sender, UserInfo user)
        {
            if (user == null || ZNet.instance == null) return false;
            foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
            {
                if (player.m_characterID.UserID == sender)
                {
                    return player.m_name == user.Name &&
                           player.m_userInfo.m_id.ToString() == user.UserId.ToString();
                }
            }
            return false;
        }
    }
}
