using System;
using System.Collections.Generic;
using Splatform;
using UnityEngine;

namespace Landoria.Socialize
{
    internal static class GroupService
    {
        internal const string RequestRpc = "Landoria_Social_GroupRequest";
        internal const string ResponseRpc = "Landoria_Social_GroupResponse";
        internal const string PingRequestRpc = "Landoria_Social_GroupPingRequest";
        internal const string ChatReceiptRpc = "Landoria_Social_GroupChatReceipt";
        private const float PositionUpdateInterval = 2f;
        private const float ChatReceiptTimeout = 15f;
        private static ZRoutedRpc registeredRpc;
        private static float nextPositionUpdate;
        private static bool awaitingInitialState;
        private static float nextInitialStateRequest;
        private static readonly Dictionary<string, PendingGroupChat> PendingChats =
            new Dictionary<string, PendingGroupChat>();
        private static readonly Dictionary<long, PendingInvite> PendingInvites =
            new Dictionary<long, PendingInvite>();

        private sealed class PendingGroupChat
        {
            internal long Sender;
            internal UserInfo User;
            internal string Message;
            internal readonly HashSet<long> Waiting = new HashSet<long>();
            internal int Rejected;
            internal float SentAt;
        }

        private sealed class PendingInvite
        {
            internal long InviterPeer;
            internal string TargetName;
            internal float SentAt;
        }

        internal static void Update()
        {
            EnsureRpcs();
            if (ZNet.instance == null || ZRoutedRpc.instance == null)
            {
                return;
            }
            if (ZNet.instance.IsServer())
            {
                SocializePlugin.Settings.InitializeServer(SocializePlugin.Log);
                if (Time.unscaledTime >= nextPositionUpdate)
                {
                    nextPositionUpdate = Time.unscaledTime + PositionUpdateInterval;
                    BroadcastPositionUpdates();
                }
                ExpireGroupChats();
                ExpireInvites();
            }
            else if (awaitingInitialState && Player.m_localPlayer != null &&
                     Time.unscaledTime >= nextInitialStateRequest)
            {
                SendInitialStateRequest();
            }
        }

        internal static void Reset()
        {
            registeredRpc = null;
            nextPositionUpdate = 0f;
            awaitingInitialState = false;
            nextInitialStateRequest = 0f;
            PendingChats.Clear();
            PendingInvites.Clear();
            GroupState.ClearAll();
            SocializePlugin.Settings?.ResetState();
            SocialChatSender.ApplyRangesToLoadedTalkers();
        }

        internal static bool IsLocalPlayerInGroup()
        {
            return Game.instance != null &&
                   GroupState.LocalMembers.Contains(Game.instance.GetPlayerProfile().GetPlayerID());
        }

        internal static bool IsExpectedServer(long sender)
        {
            if (ZNet.instance == null) return false;
            if (ZNet.instance.IsServer()) return true;
            ZNetPeer server = ZNet.instance.GetServerPeer();
            return server != null && server.m_uid == sender;
        }

        internal static void SendChat(string message)
        {
            if (!IsLocalPlayerInGroup())
            {
                Chat.instance?.AddString("You are not in a group.");
                return;
            }
            if (!PlatformManager.DistributionPlatform.PrivilegeProvider
                    .CheckPrivilege(Privilege.TextCommunication).IsGranted())
            {
                SocializePlugin.Log.LogWarning("Group chat blocked by the sender's text privilege.");
                Chat.instance?.AddString("Text communication is not permitted for this account.");
                return;
            }
            BeginGroupChatPermissionCheck(message);
        }

        private static void BeginGroupChatPermissionCheck(string message)
        {
            List<ZNet.PlayerInfo> recipients = new List<ZNet.PlayerInfo>();
            long local = ZNet.instance.LocalPlayerCharacterID.UserID;
            foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
            {
                if (player.m_characterID.UserID != local &&
                    GroupState.LocalMembers.Contains(player.m_characterID.UserID))
                {
                    recipients.Add(player);
                }
            }
            if (recipients.Count == 0)
            {
                SendChatRequest(message);
                return;
            }

            int remaining = recipients.Count;
            bool denied = false;
            SocializePlugin.Log.LogInfo(
                $"Checking sender permission for group chat against {recipients.Count} recipient(s).");
            foreach (ZNet.PlayerInfo recipient in recipients)
            {
                ZNet.PlayerInfo captured = recipient;
                TextPermissionService.Check(captured.m_userInfo.m_id, true, result =>
                    {
                        SocializePlugin.Log.LogInfo(
                            $"Sender permission result for group member '{captured.m_name}': {result}.");
                        if (!result.IsGranted()) denied = true;
                        remaining--;
                        if (remaining != 0) return;
                        if (denied)
                        {
                            Chat.instance?.AddString(
                                "Group message is not permitted for every connected member.");
                            SocializePlugin.Log.LogWarning(
                                "Group chat cancelled because at least one sender permission check failed.");
                        }
                        else
                        {
                            SendChatRequest(message);
                        }
                    });
            }
        }

        private static void SendChatRequest(string message)
        {
            EnsureRpcs();
            if (ZRoutedRpc.instance == null || Game.instance == null || Player.m_localPlayer == null)
            {
                return;
            }
            Chat.GetChatMessageData(message, true, out UserInfo user, out string filtered);
            ZPackage package = NewRequest("chat", filtered);
            SocializePlugin.Log.LogInfo($"Sending group chat request (length={filtered.Length}).");
            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc, package);
        }

        internal static void SendRequest(string action, string argument)
        {
            EnsureRpcs();
            if (ZRoutedRpc.instance == null || Game.instance == null || Player.m_localPlayer == null)
            {
                return;
            }
            ZPackage package = NewRequest(action, argument);
            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc, package);
        }

        private static ZPackage NewRequest(string action, string argument)
        {
            ZPackage package = new ZPackage();
            package.Write(action);
            package.Write(Game.instance.GetPlayerProfile().GetPlayerID());
            package.Write(Game.instance.GetPlayerProfile().GetName());
            package.Write(argument ?? "");
            UserInfo.GetLocalUser().Serialize(ref package);
            return package;
        }

        internal static void RequestInitialState()
        {
            awaitingInitialState = true;
            SendInitialStateRequest();
        }

        private static void SendInitialStateRequest()
        {
            nextInitialStateRequest = Time.unscaledTime + PositionUpdateInterval;
            SocializePlugin.Log.LogDebug("Requesting initial group state from the server.");
            SendRequest("state", "");
        }

        internal static void Dispatch(long sender, long playerId, string playerName, string action,
            string argument, UserInfo user)
        {
            if (!TryValidateIdentity(sender, playerId, playerName, user,
                    out ZNet.PlayerInfo connected, out string reason))
            {
                SocializePlugin.Log.LogWarning(
                    $"Rejected group action '{action}' from peer={sender}: {reason}");
                if (action != "state")
                {
                    SendMessage(sender, "Group action rejected because the player identity could not be verified.");
                }
                return;
            }
            SocializePlugin.Log.LogInfo(
                $"Processing group action '{action}' from '{connected.m_name}' (peer={sender}).");
            if (RegisterSession(sender, playerId))
            {
                BroadcastArrival(playerName);
            }
            switch (action)
            {
                case "state": SendSnapshot(sender, playerId); break;
                case "invite": Invite(sender, playerId, playerName, argument); break;
                case "invite-received": ConfirmInviteReceipt(sender, playerId, argument); break;
                case "accept": Accept(sender, playerId, playerName, argument); break;
                case "reject": Reject(sender, playerId, argument); break;
                case "leave": Leave(sender, playerId); break;
                case "remove": Remove(sender, playerId, argument); break;
                case "promote": Promote(sender, playerId, argument); break;
                case "info": SendInfo(sender, playerId); break;
                case "chat": SendGroupChat(sender, playerId, argument, user); break;
                default:
                    SocializePlugin.Log.LogWarning(
                        $"Ignored unknown group action '{action}' from peer={sender}.");
                    break;
            }
        }

        internal static void ReadResponse(ZPackage package)
        {
            string type = package.ReadString();
            if (type == "message")
            {
                Chat.instance?.AddString(package.ReadString());
            }
            else if (type == "arrival")
            {
                ShowArrival(package.ReadString());
            }
            else if (type == "snapshot")
            {
                ReadSnapshot(package);
            }
            else if (type == "positions")
            {
                ReadPositionUpdate(package);
            }
            else if (type == "invite")
            {
                ShowInvite(package.ReadString(), package.ReadString());
            }
            else if (type == "groupChat")
            {
                ReadGroupChat(package);
            }
            else if (type == "groupChatResult")
            {
                ReadGroupChatResult(package);
            }
        }

        private static void EnsureRpcs()
        {
            if (!RpcRegistry.RegisterIfChanged(ref registeredRpc, RegisterRpcs))
            {
                return;
            }
            GroupState.ClearAll();
            SocializePlugin.Settings?.ResetState();
            SocialChatSender.ApplyRangesToLoadedTalkers();
        }

        private static void RegisterRpcs(ZRoutedRpc rpc)
        {
            rpc.Register<ZPackage>(RequestRpc, GroupRpc.RPC_Request);
            rpc.Register<ZPackage>(ResponseRpc, GroupRpc.RPC_Response);
            rpc.Register<Vector3, UserInfo>(PingRequestRpc, GroupRpc.RPC_PingRequest);
            rpc.Register<string, int>(ChatReceiptRpc, GroupRpc.RPC_ChatReceipt);
        }

        internal static void RelayPing(long sender, Vector3 position, UserInfo user)
        {
            if (!GroupState.PeerPlayers.TryGetValue(sender, out long playerId))
            {
                SocializePlugin.Log.LogDebug("Group ping ignored because the sender is not registered.");
                return;
            }
            SocialGroup group = GroupState.GetGroup(playerId);
            if (group == null)
            {
                SocializePlugin.Log.LogDebug("Group ping ignored because the sender is not in a group.");
                return;
            }
            if (!TryGetAuthoritativeUser(sender, user, out UserInfo authoritative))
            {
                SocializePlugin.Log.LogWarning(
                    $"Group ping ignored because peer={sender} supplied an invalid identity.");
                return;
            }
            int recipients = 0;
            foreach (long member in group.Members.Keys)
            {
                long peer = FindPeer(member);
                if (peer == 0L)
                {
                    continue;
                }
                ZRoutedRpc.instance.InvokeRoutedRPC(peer, "ChatMessage", position,
                    (int)Talker.Type.Ping, authoritative, "");
                recipients++;
            }
            SocializePlugin.Log.LogDebug(
                $"Relayed group ping from {GetPlayerName(playerId)} to {recipients} member(s).");
        }

        private static void Invite(long sender, long inviter, string inviterName, string targetName)
        {
            long targetPeer = FindPeerByName(targetName);
            bool targetReady = TryGetPlayerForPeer(targetPeer, out long target);
            GroupDecision targetDecision = GroupPolicy.CanInviteTarget(targetReady);
            if (!targetDecision.Allowed)
            {
                SendMessage(sender, targetDecision.Message);
                return;
            }
            SocialGroup inviterGroup = GroupState.GetGroup(inviter);
            GroupDecision decision = GroupInvitationPolicy.TryInvite(
                inviterGroup, inviter, target, GroupState.PlayerGroups.ContainsKey(target),
                GroupState.Invitations);
            if (!decision.Allowed)
            {
                if (decision.Message != null)
                {
                    SendMessage(sender, decision.Message);
                }
                return;
            }
            ZPackage response = NewResponse("invite");
            response.Write(inviter.ToString());
            response.Write(inviterName);
            PendingInvites[target] = new PendingInvite
            {
                InviterPeer = sender,
                TargetName = targetName,
                SentAt = Time.realtimeSinceStartup
            };
            ZRoutedRpc.instance.InvokeRoutedRPC(targetPeer, ResponseRpc, response);
            SocializePlugin.Log.LogInfo(
                $"Group invitation queued from peer={sender} for peer={targetPeer}; awaiting display receipt.");
        }

        private static void ConfirmInviteReceipt(long sender, long playerId, string inviterText)
        {
            if (!long.TryParse(inviterText, out long inviter) ||
                !GroupState.Invitations.TryGetValue(playerId, out long expectedInviter) ||
                inviter != expectedInviter)
            {
                SocializePlugin.Log.LogWarning(
                    $"Invalid group invitation receipt from peer={sender} for inviter={inviterText}.");
                return;
            }
            long inviterPeer = FindPeer(inviter);
            PendingInvites.Remove(playerId);
            SendMessage(inviterPeer, "Group invitation delivered.");
            SocializePlugin.Log.LogInfo(
                $"Group invitation from player={inviter} displayed for player={playerId}.");
        }

        private static void Accept(long sender, long playerId, string playerName, string inviterText)
        {
            PendingInvites.Remove(playerId);
            GroupAcceptanceResult result = GroupAcceptancePolicy.Accept(
                playerId, playerName, inviterText, GroupState.Invitations,
                GetOrCreateGroup, GroupState.PlayerGroups);
            if (!result.Accepted)
            {
                SendMessage(sender, result.Message);
                return;
            }
            BroadcastChange(result.Group, playerName + " joined the group.");
        }

        private static SocialGroup GetOrCreateGroup(long inviter)
        {
            SocialGroup group = GroupState.GetGroup(inviter);
            if (group != null)
            {
                return group.Leader == inviter ? group : null;
            }
            group = new SocialGroup { Id = GroupState.GetNextGroupId(), Leader = inviter };
            group.AddMember(inviter, GetPlayerName(inviter));
            GroupState.Groups[group.Id] = group;
            GroupState.PlayerGroups[inviter] = group.Id;
            return group;
        }

        private static void Reject(long sender, long playerId, string inviterText)
        {
            PendingInvites.Remove(playerId);
            GroupState.Invitations.Remove(playerId);
            SendMessage(sender, "Group invitation rejected.");
            if (long.TryParse(inviterText, out long inviter))
            {
                SendMessage(FindPeer(inviter), GetPlayerName(playerId) + " rejected the group invitation.");
            }
        }

        private static void Leave(long sender, long playerId)
        {
            SocialGroup group = GroupState.GetGroup(playerId);
            if (group == null)
            {
                SendMessage(sender, "You are not in a group.");
                return;
            }
            string name = group.Members[playerId];
            GroupState.PlayerGroups.Remove(playerId);
            GroupRemovalResult removal = GroupLifecyclePolicy.Remove(group, playerId);
            BroadcastMembers(removal.RemainingMembers, name + " left the group.");
            ApplyRemoval(group, removal);
            SendSnapshot(sender, playerId);
            SendMessage(sender, "You left the group.");
            BroadcastSnapshots(group);
        }

        private static void Remove(long sender, long actor, string targetName)
        {
            SocialGroup group = GroupState.GetGroup(actor);
            long target = FindMember(group, targetName);
            if (!ValidateLeaderAction(sender, actor, targetName, group, target)
                || !ValidateRemoveTarget(sender, actor, target))
            {
                return;
            }
            string name = group.Members[target];
            long targetPeer = FindPeer(target);
            GroupState.PlayerGroups.Remove(target);
            GroupRemovalResult removal = GroupLifecyclePolicy.Remove(group, target);
            BroadcastMembers(removal.RemainingMembers, name + " was removed from the group.");
            ApplyRemoval(group, removal);
            SendSnapshot(targetPeer, target);
            SendMessage(targetPeer, "You were removed from the group.");
            BroadcastSnapshots(group);
        }

        private static void Promote(long sender, long actor, string targetName)
        {
            SocialGroup group = GroupState.GetGroup(actor);
            long target = FindMember(group, targetName);
            GroupDecision decision = GroupPromotionPolicy.TryPromote(
                group, actor, target, targetName);
            if (!decision.Allowed)
            {
                SendMessage(sender, decision.Message);
                return;
            }
            Broadcast(group, group.Members[target] + " is now the group leader.");
            BroadcastSnapshots(group);
        }

        private static bool ValidateLeaderAction(
            long sender, long actor, string targetName, SocialGroup group, long target)
        {
            GroupDecision decision = GroupPolicy.CanTargetMember(
                group, actor, target, targetName);
            if (!decision.Allowed)
            {
                SendMessage(sender, decision.Message);
            }
            return decision.Allowed;
        }

        private static bool ValidateRemoveTarget(long sender, long actor, long target)
        {
            return ValidateTargetDecision(sender, GroupPolicy.CanRemove(actor, target));
        }

        private static bool ValidateTargetDecision(long sender, GroupDecision decision)
        {
            if (!decision.Allowed)
            {
                SendMessage(sender, decision.Message);
            }
            return decision.Allowed;
        }

        private static void ApplyRemoval(SocialGroup group, GroupRemovalResult result)
        {
            if (!result.Disbanded)
            {
                return;
            }
            foreach (long member in result.RemainingMembers)
            {
                GroupState.PlayerGroups.Remove(member);
                SendMessage(FindPeer(member), "The group was disbanded.");
                SendSnapshot(FindPeer(member), member);
            }
            GroupState.Groups.Remove(group.Id);
        }

        private static void SendGroupChat(long sender, long actor, string message, UserInfo user)
        {
            SocialGroup group = GroupState.GetGroup(actor);
            GroupChatResult result = GroupChatPolicy.Prepare(group, actor, message,
                member => FindPeer(member) != 0L, (name, text) => text);
            if (!result.Broadcast)
            {
                SendMessage(sender, result.Message);
                return;
            }
            if (!TryGetAuthoritativeUser(sender, user, out UserInfo authoritative))
            {
                SocializePlugin.Log.LogWarning($"Rejected group chat from peer={sender}: identity mismatch.");
                SendMessage(sender, "Group message rejected because the sender identity could not be verified.");
                return;
            }

            string requestId = System.Guid.NewGuid().ToString("N").Substring(0, 12);
            PendingGroupChat pending = new PendingGroupChat
            {
                Sender = sender,
                User = authoritative,
                Message = result.Message,
                SentAt = Time.realtimeSinceStartup
            };
            foreach (long member in group.Members.Keys)
            {
                long peer = FindPeer(member);
                if (peer == 0L || peer == sender) continue;
                pending.Waiting.Add(peer);
            }
            PendingChats[requestId] = pending;
            foreach (long peer in pending.Waiting)
            {
                SendGroupChatDelivery(peer, requestId, authoritative, result.Message);
            }
            SocializePlugin.Log.LogInfo(
                $"Group chat [{requestId}] accepted from peer={sender}; waiting for {pending.Waiting.Count} receipt(s).");
            if (pending.Waiting.Count == 0)
            {
                CompleteGroupChat(requestId, pending);
            }
        }

        private static void SendGroupChatDelivery(long peer, string requestId, UserInfo user, string message)
        {
            ZPackage response = NewResponse("groupChat");
            response.Write(requestId);
            user.Serialize(ref response);
            response.Write(message);
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, ResponseRpc, response);
        }

        private static void ReadGroupChat(ZPackage package)
        {
            string requestId = package.ReadString();
            UserInfo user = new UserInfo();
            user.Deserialize(ref package);
            string message = package.ReadString();
            SocializePlugin.Log.LogInfo(
                $"Group chat [{requestId}] received from '{user.GetDisplayName()}' (length={message.Length}); checking permission.");
            TextPermissionService.Check(user.UserId, false,
                result => CompleteGroupChatReceive(requestId, user, message, result));
        }

        private static void CompleteGroupChatReceive(string requestId, UserInfo user, string message,
            RelationsManagerPermissionResult result)
        {
            SocializePlugin.Log.LogInfo($"Group chat [{requestId}] permission result: {result}.");
            if (result.IsGranted() && Chat.instance != null)
            {
                string displayed = message.Replace('<', ' ').Replace('>', ' ');
                if (result == RelationsManagerPermissionResult.GrantedRequiresFiltering)
                {
                    CensorShittyWords.Filter(displayed, out displayed);
                }
                Chat.instance.AddString(ChatFormatting.FormatGroup(user.GetDisplayName(), displayed));
                SocializePlugin.Log.LogInfo($"Group chat [{requestId}] displayed; sending receipt.");
            }
            else if (result.IsGranted())
            {
                result = RelationsManagerPermissionResult.Error;
            }
            ZRoutedRpc.instance?.InvokeRoutedRPC(ChatReceiptRpc, requestId, (int)result);
        }

        internal static void ReceiveChatReceipt(long sender, string requestId,
            RelationsManagerPermissionResult result)
        {
            if (!PendingChats.TryGetValue(requestId, out PendingGroupChat pending) ||
                !pending.Waiting.Remove(sender))
            {
                SocializePlugin.Log.LogWarning(
                    $"Ignoring unknown group chat receipt [{requestId}] from peer={sender}.");
                return;
            }
            if (!result.IsGranted()) pending.Rejected++;
            SocializePlugin.Log.LogInfo(
                $"Group chat [{requestId}] receipt from peer={sender}: {result}; remaining={pending.Waiting.Count}.");
            if (pending.Waiting.Count == 0) CompleteGroupChat(requestId, pending);
        }

        private static void CompleteGroupChat(string requestId, PendingGroupChat pending)
        {
            PendingChats.Remove(requestId);
            ZPackage response = NewResponse("groupChatResult");
            response.Write(requestId);
            response.Write(pending.Rejected == 0);
            pending.User.Serialize(ref response);
            response.Write(pending.Message);
            response.Write(pending.Rejected);
            ZRoutedRpc.instance.InvokeRoutedRPC(pending.Sender, ResponseRpc, response);
        }

        private static void ReadGroupChatResult(ZPackage package)
        {
            string requestId = package.ReadString();
            bool delivered = package.ReadBool();
            UserInfo user = new UserInfo();
            user.Deserialize(ref package);
            string message = package.ReadString();
            int rejected = package.ReadInt();
            if (delivered)
            {
                string displayed = message.Replace('<', ' ').Replace('>', ' ');
                Chat.instance?.AddString(ChatFormatting.FormatGroup(user.GetDisplayName(), displayed));
                SocializePlugin.Log.LogInfo($"Group chat [{requestId}] confirmed and shown to the sender.");
            }
            else
            {
                Chat.instance?.AddString("Group message was not delivered to every member.");
                SocializePlugin.Log.LogWarning(
                    $"Group chat [{requestId}] completed with {rejected} rejected delivery attempt(s).");
            }
        }

        private static void ExpireGroupChats()
        {
            List<string> expired = null;
            foreach (KeyValuePair<string, PendingGroupChat> entry in PendingChats)
            {
                if (Time.realtimeSinceStartup - entry.Value.SentAt >= ChatReceiptTimeout)
                {
                    (expired ??= new List<string>()).Add(entry.Key);
                }
            }
            if (expired == null) return;
            foreach (string requestId in expired)
            {
                PendingGroupChat pending = PendingChats[requestId];
                PendingChats.Remove(requestId);
                SendMessage(pending.Sender, "Group message was not confirmed by every member.");
                SocializePlugin.Log.LogWarning(
                    $"Group chat [{requestId}] timed out with {pending.Waiting.Count} missing receipt(s).");
            }
        }

        private static void ExpireInvites()
        {
            List<long> expired = null;
            foreach (KeyValuePair<long, PendingInvite> entry in PendingInvites)
            {
                if (Time.realtimeSinceStartup - entry.Value.SentAt >= ChatReceiptTimeout)
                {
                    (expired ??= new List<long>()).Add(entry.Key);
                }
            }
            if (expired == null) return;
            foreach (long target in expired)
            {
                PendingInvite pending = PendingInvites[target];
                PendingInvites.Remove(target);
                GroupState.Invitations.Remove(target);
                SendMessage(pending.InviterPeer,
                    "Group invitation to " + pending.TargetName + " was not confirmed.");
                SocializePlugin.Log.LogWarning(
                    $"Group invitation to player={target} timed out waiting for display confirmation.");
            }
        }

        private static bool TryGetAuthoritativeUser(long sender, UserInfo claimed, out UserInfo user)
        {
            user = null;
            if (claimed == null || !TryGetPlayerInfo(sender, out ZNet.PlayerInfo player)) return false;
            if (player.m_name != claimed.Name ||
                player.m_userInfo.m_id.ToString() != claimed.UserId.ToString()) return false;
            user = new UserInfo { Name = player.m_name, UserId = player.m_userInfo.m_id };
            return true;
        }

        private static bool TryGetPlayerInfo(long peer, out ZNet.PlayerInfo player)
        {
            if (ZNet.instance != null)
            {
                foreach (ZNet.PlayerInfo candidate in ZNet.instance.GetPlayerList())
                {
                    if (candidate.m_characterID.UserID == peer)
                    {
                        player = candidate;
                        return true;
                    }
                }
            }
            player = default;
            return false;
        }

        private static bool IsPlayerIdClaimedByOtherPeer(long peer, long playerId)
        {
            foreach (KeyValuePair<long, long> mapping in GroupState.PeerPlayers)
            {
                if (mapping.Key != peer && mapping.Value == playerId) return true;
            }
            return false;
        }

        private static bool TryValidateIdentity(long peer, long playerId, string playerName,
            UserInfo claimed, out ZNet.PlayerInfo connected, out string reason)
        {
            if (!TryGetPlayerInfo(peer, out connected))
            {
                reason = "the peer is not present in the server player list yet";
                return false;
            }
            if (claimed == null)
            {
                reason = "the request did not contain a platform identity";
                return false;
            }
            if (connected.m_name != playerName || connected.m_name != claimed.Name)
            {
                reason = $"name mismatch (connected='{connected.m_name}', player='{playerName}', platform='{claimed.Name}')";
                return false;
            }
            string connectedUserId = connected.m_userInfo.m_id.ToString();
            string claimedUserId = claimed.UserId.ToString();
            if (connectedUserId != claimedUserId)
            {
                reason = $"platform user mismatch (connected='{connectedUserId}', claimed='{claimedUserId}')";
                return false;
            }
            if (IsPlayerIdClaimedByOtherPeer(peer, playerId))
            {
                reason = $"player id '{playerId}' is already associated with another peer";
                return false;
            }
            reason = null;
            return true;
        }

        private static void SendInfo(long sender, long playerId)
        {
            SocialGroup group = GroupState.GetGroup(playerId);
            SendMessage(sender, GroupInfoPolicy.Build(group, member => FindPeer(member) != 0L));
        }

        private static void BroadcastChange(SocialGroup group, string message)
        {
            Broadcast(group, message);
            BroadcastSnapshots(group);
            SocializePlugin.Log.LogInfo(message);
        }

        internal static void BeginPeerSession(long peer)
        {
            if (ZNet.instance != null && ZNet.instance.IsServer() &&
                GroupState.PeerPlayers.ContainsKey(peer))
            {
                DisconnectPeer(peer);
            }
        }

        internal static void DisconnectPeer(long peer)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() ||
                !GroupState.PeerPlayers.TryGetValue(peer, out long playerId))
            {
                return;
            }
            GroupState.PeerPlayers.Remove(peer);
            RemovePlayerSession(playerId);
        }

        private static bool RegisterSession(long peer, long playerId)
        {
            bool sameSession = GroupState.PeerPlayers.TryGetValue(peer, out long registered) &&
                               registered == playerId;
            if (!sameSession)
            {
                if (registered != 0L)
                {
                    RemovePlayerSession(registered);
                }
                RemovePlayerSession(playerId);
            }
            GroupState.PeerPlayers[peer] = playerId;
            return !sameSession;
        }

        private static void RemovePlayerSession(long playerId)
        {
            RemovePeerMappings(playerId);
            RemoveInvitations(playerId);
            SocialGroup group = GroupState.GetGroup(playerId);
            if (group == null)
            {
                return;
            }
            string name = group.Members[playerId];
            GroupState.PlayerGroups.Remove(playerId);
            GroupRemovalResult removal = GroupLifecyclePolicy.Remove(group, playerId);
            BroadcastMembers(removal.RemainingMembers, name + " left the group.");
            ApplyRemoval(group, removal);
            BroadcastSnapshots(group);
        }

        private static void RemovePeerMappings(long playerId)
        {
            foreach (long peer in new List<long>(GroupState.PeerPlayers.Keys))
            {
                if (GroupState.PeerPlayers[peer] == playerId)
                {
                    GroupState.PeerPlayers.Remove(peer);
                }
            }
        }

        private static void RemoveInvitations(long playerId)
        {
            GroupState.Invitations.Remove(playerId);
            PendingInvites.Remove(playerId);
            foreach (long target in new List<long>(GroupState.Invitations.Keys))
            {
                if (GroupState.Invitations[target] == playerId)
                {
                    GroupState.Invitations.Remove(target);
                    PendingInvites.Remove(target);
                }
            }
        }

        private static void BroadcastSnapshots(SocialGroup group)
        {
            if (group == null)
            {
                return;
            }
            foreach (long member in group.Members.Keys)
            {
                SendSnapshot(FindPeer(member), member);
            }
        }

        private static void BroadcastPositionUpdates()
        {
            foreach (SocialGroup group in GroupState.Groups.Values)
            {
                foreach (long member in group.Members.Keys)
                {
                    SendPositionUpdate(FindPeer(member), group);
                }
            }
        }

        private static void SendPositionUpdate(long peer, SocialGroup group)
        {
            if (peer == 0L || group == null)
            {
                return;
            }
            ZPackage response = NewResponse("positions");
            response.Write(group.Members.Count);
            foreach (KeyValuePair<long, string> member in group.Members)
            {
                response.Write(member.Key);
                response.Write(member.Value);
                GroupMapSharing.WritePosition(response, member.Key);
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, ResponseRpc, response);
        }

        private static void SendSnapshot(long peer, long playerId)
        {
            if (peer == 0L)
            {
                return;
            }
            SocialGroup group = GroupState.GetGroup(playerId);
            ZPackage response = NewResponse("snapshot");
            response.Write(group != null ? group.Leader : 0L);
            response.Write(group != null ? group.Members.Count : 0);
            if (group != null)
            {
                foreach (KeyValuePair<long, string> member in group.Members)
                {
                    response.Write(member.Key);
                    response.Write(member.Value);
                    GroupMapSharing.WritePosition(response, member.Key);
                }
            }
            SocializePlugin.Settings.WriteState(response);
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, ResponseRpc, response);
        }

        private static void ReadSnapshot(ZPackage package)
        {
            awaitingInitialState = false;
            package.ReadLong();
            GroupState.LocalMembers.Clear();
            GroupMapSharing.Clear();
            int count = package.ReadInt();
            for (int index = 0; index < count; index++)
            {
                long playerId = package.ReadLong();
                string playerName = package.ReadString();
                GroupState.LocalMembers.Add(playerId);
                GroupMapSharing.ReadPosition(package, playerId, playerName);
            }
            SocializePlugin.Settings.ReadState(package);
            SocialChatSender.ApplyRangesToLoadedTalkers();
        }

        private static void ReadPositionUpdate(ZPackage package)
        {
            int count = package.ReadInt();
            for (int index = 0; index < count; index++)
            {
                long playerId = package.ReadLong();
                string playerName = package.ReadString();
                GroupMapSharing.ReadPosition(package, playerId, playerName);
            }
        }

        private static void ShowInvite(string inviterId, string inviterName)
        {
            InvitationPresentation presentation = InvitationPresentationPolicy.Build(inviterName);
            UnifiedPopup.Push(new YesNoPopup(
                presentation.Title,
                presentation.Message,
                () => RespondToInvite(presentation.AcceptAction, inviterId),
                () => RespondToInvite(presentation.RejectAction, inviterId),
                localizeText: false));
            SendRequest("invite-received", inviterId);
        }

        private static void RespondToInvite(string action, string inviterId)
        {
            UnifiedPopup.Pop();
            SendRequest(action, inviterId);
        }

        private static void Broadcast(SocialGroup group, string message)
        {
            if (group == null)
            {
                return;
            }
            foreach (long member in group.Members.Keys)
            {
                SendMessage(FindPeer(member), message);
            }
        }

        private static void BroadcastMembers(IEnumerable<long> members, string message)
        {
            foreach (long member in members)
            {
                SendMessage(FindPeer(member), message);
            }
        }

        private static void BroadcastArrival(string playerName)
        {
            foreach (long peerId in new List<long>(GroupState.PeerPlayers.Keys))
            {
                if (ZNet.instance.GetPeer(peerId) is ZNetPeer peer && peer.IsReady())
                {
                    SendArrival(peerId, playerName);
                }
            }
            SocializePlugin.Log.LogInfo(playerName + " arrived on the server.");
        }

        private static void SendArrival(long peer, string playerName)
        {
            if (peer == 0L)
            {
                ShowArrival(playerName);
                return;
            }
            ZPackage response = NewResponse("arrival");
            response.Write(playerName ?? "");
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, ResponseRpc, response);
        }

        private static void ShowArrival(string playerName)
        {
            string message = Localization.instance.Localize(
                "$text_player_arrived");
            Chat.instance?.AddString(
                ChatFormattingPolicy.FormatArrival(playerName, message));
        }

        private static void SendMessage(long peer, string message)
        {
            if (peer == 0L)
            {
                Chat.instance?.AddString(message);
                return;
            }
            ZPackage response = NewResponse("message");
            response.Write(message ?? "");
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, ResponseRpc, response);
        }

        private static ZPackage NewResponse(string type)
        {
            ZPackage package = new ZPackage();
            package.Write(type);
            return package;
        }

        private static long FindMember(SocialGroup group, string name)
        {
            if (group == null)
            {
                return 0L;
            }
            foreach (KeyValuePair<long, string> member in group.Members)
            {
                if (string.Equals(member.Value, name, StringComparison.OrdinalIgnoreCase))
                {
                    return member.Key;
                }
            }
            return 0L;
        }

        private static long FindPeerByName(string name)
        {
            if (ZNet.instance == null)
            {
                return 0L;
            }
            foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
            {
                if (string.Equals(player.m_name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return player.m_characterID.UserID;
                }
            }
            return 0L;
        }

        private static bool TryGetPlayerForPeer(long peer, out long playerId)
        {
            playerId = 0L;
            return peer != 0L && GroupState.PeerPlayers.TryGetValue(peer, out playerId);
        }

        private static long FindPeer(long playerId)
        {
            foreach (KeyValuePair<long, long> mapping in GroupState.PeerPlayers)
            {
                if (mapping.Value == playerId && ZNet.instance != null &&
                    ZNet.instance.GetPeer(mapping.Key) is ZNetPeer peer && peer.IsReady())
                {
                    return mapping.Key;
                }
            }
            return 0L;
        }

        private static string GetPlayerName(long playerId)
        {
            SocialGroup group = GroupState.GetGroup(playerId);
            if (group != null && group.Members.TryGetValue(playerId, out string name))
            {
                return name;
            }
            foreach (KeyValuePair<long, long> mapping in GroupState.PeerPlayers)
            {
                if (mapping.Value == playerId && ZNet.instance.GetPeer(mapping.Key) is ZNetPeer peer)
                {
                    return peer.m_playerName;
                }
            }
            return playerId.ToString();
        }
    }
}
