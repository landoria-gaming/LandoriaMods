using System;
using System.Collections.Generic;
using Splatform;
using UnityEngine;

namespace Landoria.Socialize
{
    internal static class ChatCommands
    {
        internal static void Register()
        {
            Register("sh", SendShout);
            Register("shout", SendShout);
            Register("s", SendSay);
            Register("say", SendSay);
            Register("w", SendWhisper);
            Register("wping", SendTargetPing);
        }

        private static void Register(string name, Terminal.ConsoleEventFailable handler)
        {
            new Terminal.ConsoleCommand(name, GetDescription(name), handler);
        }

        private static string GetDescription(string name)
        {
            if (name == "w")
            {
                return "[player] [message] sends a private message";
            }
            if (name == "wping")
            {
                return "[player] [message] sends a private message with a ping";
            }
            return "[message] " + (name == "s" || name == "say"
                ? "says something to nearby players"
                : "shouts so everyone around you can hear you");
        }

        private static object SendShout(Terminal.ConsoleEventArgs args)
        {
            if (!TryGetMessage(args, out string message))
            {
                args.Context.AddString("Usage: /sh message");
                return true;
            }
            ChatChannelState.SetShout();
            SocialChatSender.SendShout(message);
            return true;
        }

        private static object SendSay(Terminal.ConsoleEventArgs args)
        {
            if (!TryGetMessage(args, out string message))
            {
                args.Context.AddString("Usage: /s message");
                return true;
            }
            ChatChannelState.SetNormal();
            Chat.instance.SendText(Talker.Type.Normal, message);
            return true;
        }

        private static object SendWhisper(Terminal.ConsoleEventArgs args)
        {
            if (!ChatCommandParser.TryParseTarget(args.FullLine, out string target, out string message))
            {
                args.Context.AddString("Usage: /w PlayerName message");
                return true;
            }
            if (!PrivateChat.Send(target, message, args.Context))
            {
                return true;
            }
            ChatChannelState.SetWhisper(target);
            return true;
        }

        private static object SendTargetPing(Terminal.ConsoleEventArgs args)
        {
            if (!ChatCommandParser.TryParseTarget(args.FullLine, out string target, out string message))
            {
                args.Context.AddString("Usage: /wping PlayerName message");
                return true;
            }
            TargetPingService.Send(target, message, args.Context);
            return true;
        }

        private static bool TryGetMessage(Terminal.ConsoleEventArgs args, out string message)
        {
            message = (args.ArgsAll ?? "").Trim();
            return Chat.instance != null && !string.IsNullOrEmpty(message);
        }

    }

    internal static class PrivateChat
    {
        private const string MessageRpc = "Landoria_Social_PrivateMessage";
        private const string ReceiptRpc = "Landoria_Social_PrivateReceipt";
        private const float ReceiptTimeoutSeconds = 15f;
        private static readonly Dictionary<string, PendingMessage> Pending =
            new Dictionary<string, PendingMessage>();
        private static ZRoutedRpc registeredRpc;

        private sealed class PendingMessage
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
            if (Pending.Count == 0)
            {
                return;
            }

            List<string> expired = null;
            foreach (KeyValuePair<string, PendingMessage> entry in Pending)
            {
                if (Time.realtimeSinceStartup - entry.Value.SentAt < ReceiptTimeoutSeconds)
                {
                    continue;
                }
                (expired ??= new List<string>()).Add(entry.Key);
            }

            if (expired == null)
            {
                return;
            }
            foreach (string requestId in expired)
            {
                PendingMessage pending = Pending[requestId];
                Pending.Remove(requestId);
                SocializePlugin.Log.LogWarning(
                    $"Private message [{requestId}] to '{pending.TargetName}' timed out waiting for a delivery receipt.");
                pending.Context?.AddString("Private message to " + pending.TargetName + " was not confirmed.");
            }
        }

        internal static void Reset()
        {
            Pending.Clear();
            registeredRpc = null;
        }

        internal static bool Send(string targetName, string message, Terminal context)
        {
            bool found = TryFindPlayer(targetName, out ZNet.PlayerInfo target);
            GroupDecision decision = PrivateChatPolicy.CanSend(
                found, found && IsLocalPlayer(target), targetName);
            if (!decision.Allowed)
            {
                context?.AddString(decision.Message);
                return false;
            }
            if (!PlatformManager.DistributionPlatform.PrivilegeProvider
                    .CheckPrivilege(Privilege.TextCommunication).IsGranted())
            {
                SocializePlugin.Log.LogWarning(
                    $"Private message to '{target.m_name}' blocked by the sender's text privilege.");
                context?.AddString("Text communication is not permitted for this account.");
                return false;
            }

            EnsureRpcs();
            SocializePlugin.Log.LogInfo(
                $"Checking sender permission for private message to '{target.m_name}' (peer={target.m_characterID.UserID}).");
            TextPermissionService.Check(
                target.m_userInfo.m_id, true,
                result => CompleteSendPermission(target, message, context, result));
            return true;
        }

        private static void CompleteSendPermission(ZNet.PlayerInfo target, string message,
            Terminal context, RelationsManagerPermissionResult result)
        {
            SocializePlugin.Log.LogInfo(
                $"Sender permission result for private message to '{target.m_name}': {result}.");
            if (!result.IsGranted())
            {
                context?.AddString("Private message to " + target.m_name + " is not permitted.");
                return;
            }
            Chat.GetChatMessageData(message,
                result == RelationsManagerPermissionResult.GrantedRequiresFiltering,
                out UserInfo user, out string filteredMessage);
            string requestId = Guid.NewGuid().ToString("N").Substring(0, 12);
            long targetPeer = target.m_characterID.UserID;
            Pending[requestId] = new PendingMessage
            {
                TargetPeer = targetPeer,
                TargetName = target.m_name,
                Message = filteredMessage,
                Context = context,
                SentAt = Time.realtimeSinceStartup
            };
            SocializePlugin.Log.LogInfo(
                $"Sending private message [{requestId}] to '{target.m_name}' (peer={targetPeer}, length={filteredMessage.Length}).");
            ZRoutedRpc.instance.InvokeRoutedRPC(
                targetPeer,
                MessageRpc,
                requestId,
                user,
                filteredMessage);
        }

        private static void EnsureRpcs()
        {
            RpcRegistry.RegisterIfChanged(ref registeredRpc, RegisterRpcs);
        }

        private static void RegisterRpcs(ZRoutedRpc rpc)
        {
            rpc.Register<string, UserInfo, string>(MessageRpc, RPC_PrivateMessage);
            rpc.Register<string, int>(ReceiptRpc, RPC_PrivateReceipt);
            SocializePlugin.Log.LogDebug("Private message RPCs registered.");
        }

        private static void RPC_PrivateMessage(long sender, string requestId, UserInfo user, string message)
        {
            if (!IsExpectedUser(sender, user))
            {
                SocializePlugin.Log.LogWarning(
                    $"Private message [{requestId}] rejected because peer={sender} supplied an invalid identity.");
                ZRoutedRpc.instance?.InvokeRoutedRPC(
                    sender, ReceiptRpc, requestId, (int)RelationsManagerPermissionResult.Error);
                return;
            }
            SocializePlugin.Log.LogInfo(
                $"Private message [{requestId}] received from peer={sender} (user='{user.GetDisplayName()}', length={message.Length}); checking text permission.");
            TextPermissionService.Check(
                user.UserId, false,
                result => CompleteReceive(sender, requestId, user, message, result));
        }

        private static void CompleteReceive(
            long sender,
            string requestId,
            UserInfo user,
            string message,
            RelationsManagerPermissionResult result)
        {
            SocializePlugin.Log.LogInfo(
                $"Private message [{requestId}] permission result for peer={sender}: {result}.");
            if (!result.IsGranted())
            {
                ZRoutedRpc.instance?.InvokeRoutedRPC(sender, ReceiptRpc, requestId, (int)result);
                return;
            }
            if (Chat.instance == null)
            {
                SocializePlugin.Log.LogWarning(
                    $"Private message [{requestId}] cannot be displayed because Chat is unavailable.");
                ZRoutedRpc.instance?.InvokeRoutedRPC(
                    sender, ReceiptRpc, requestId, (int)RelationsManagerPermissionResult.Error);
                return;
            }

            string displayedMessage = message.Replace('<', ' ').Replace('>', ' ');
            if (result == RelationsManagerPermissionResult.GrantedRequiresFiltering)
            {
                CensorShittyWords.Filter(displayedMessage, out displayedMessage);
            }
            ChatFormatting.AddPrivate(Chat.instance, user.GetDisplayName(), displayedMessage, false);
            SocializePlugin.Log.LogInfo(
                $"Private message [{requestId}] displayed; sending delivery receipt to peer={sender}.");
            ZRoutedRpc.instance?.InvokeRoutedRPC(
                sender, ReceiptRpc, requestId, (int)result);
        }

        private static void RPC_PrivateReceipt(long sender, string requestId, int resultValue)
        {
            RelationsManagerPermissionResult result = (RelationsManagerPermissionResult)resultValue;
            if (!Pending.TryGetValue(requestId, out PendingMessage pending))
            {
                SocializePlugin.Log.LogWarning(
                    $"Ignoring unknown private message receipt [{requestId}] from peer={sender}, result={result}.");
                return;
            }
            if (pending.TargetPeer != sender)
            {
                SocializePlugin.Log.LogWarning(
                    $"Ignoring private message receipt [{requestId}] from unexpected peer={sender}; expected={pending.TargetPeer}.");
                return;
            }

            Pending.Remove(requestId);
            if (!result.IsGranted())
            {
                SocializePlugin.Log.LogWarning(
                    $"Private message [{requestId}] to '{pending.TargetName}' was rejected: {result}.");
                pending.Context?.AddString("Private message to " + pending.TargetName + " was not delivered.");
                return;
            }

            string localName = Game.instance.GetPlayerProfile().GetName();
            ChatFormatting.AddPrivate(
                pending.Context, localName, "to " + pending.TargetName + ": " + pending.Message, false);
            SocializePlugin.Log.LogInfo(
                $"Private message [{requestId}] to '{pending.TargetName}' confirmed and shown to the sender.");
        }

        private static bool TryFindPlayer(string name, out ZNet.PlayerInfo player)
        {
            foreach (ZNet.PlayerInfo candidate in ZNet.instance.GetPlayerList())
            {
                if (string.Equals(candidate.m_name, name, StringComparison.OrdinalIgnoreCase))
                {
                    player = candidate;
                    return true;
                }
            }
            player = default;
            return false;
        }

        private static bool IsLocalPlayer(ZNet.PlayerInfo player)
        {
            return ZNet.instance != null
                   && player.m_characterID.UserID
                   == ZNet.instance.LocalPlayerCharacterID.UserID;
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
