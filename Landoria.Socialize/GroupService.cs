using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landoria.Socialize
{
    internal static class GroupService
    {
        internal const string RequestRpc = "Landoria_Social_GroupRequest";
        internal const string ResponseRpc = "Landoria_Social_GroupResponse";
        internal const string PingRequestRpc = "Landoria_Social_GroupPingRequest";
        private const float PositionUpdateInterval = 5f;
        private static ZRoutedRpc registeredRpc;
        private static float nextPositionUpdate;

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
            }
        }

        internal static void Reset()
        {
            registeredRpc = null;
            nextPositionUpdate = 0f;
            GroupState.ClearAll();
            SocializePlugin.Settings?.ResetState();
            SocialChatSender.ApplyRangesToLoadedTalkers();
        }

        internal static bool IsLocalPlayerInGroup()
        {
            return Game.instance != null &&
                   GroupState.LocalMembers.Contains(Game.instance.GetPlayerProfile().GetPlayerID());
        }

        internal static void SendChat(string message)
        {
            if (!IsLocalPlayerInGroup())
            {
                Chat.instance?.AddString("You are not in a group.");
                return;
            }
            SendRequest("chat", message);
        }

        internal static void SendRequest(string action, string argument)
        {
            EnsureRpcs();
            if (ZRoutedRpc.instance == null || Game.instance == null || Player.m_localPlayer == null)
            {
                return;
            }
            ZPackage package = new ZPackage();
            package.Write(action);
            package.Write(Game.instance.GetPlayerProfile().GetPlayerID());
            package.Write(Game.instance.GetPlayerProfile().GetName());
            package.Write(argument ?? "");
            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc, package);
        }

        internal static void RequestInitialState()
        {
            SendRequest("state", "");
        }

        internal static void Dispatch(long sender, long playerId, string playerName, string action, string argument)
        {
            if (RegisterSession(sender, playerId))
            {
                BroadcastServerMessage(playerName + " joined the server.");
            }
            switch (action)
            {
                case "state": SendSnapshot(sender, playerId); break;
                case "invite": Invite(sender, playerId, playerName, argument); break;
                case "accept": Accept(sender, playerId, playerName, argument); break;
                case "reject": Reject(sender, playerId, argument); break;
                case "leave": Leave(sender, playerId); break;
                case "remove": Remove(sender, playerId, argument); break;
                case "promote": Promote(sender, playerId, argument); break;
                case "info": SendInfo(sender, playerId); break;
                case "chat": SendGroupChat(sender, playerId, argument); break;
            }
        }

        internal static void ReadResponse(ZPackage package)
        {
            string type = package.ReadString();
            if (type == "message")
            {
                Chat.instance?.AddString(package.ReadString());
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
            int recipients = 0;
            foreach (long member in group.Members.Keys)
            {
                long peer = FindPeer(member);
                if (peer == 0L)
                {
                    continue;
                }
                ZRoutedRpc.instance.InvokeRoutedRPC(peer, "ChatMessage", position,
                    (int)Talker.Type.Ping, user, "");
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
            ZRoutedRpc.instance.InvokeRoutedRPC(targetPeer, ResponseRpc, response);
            SendMessage(sender, "Group invitation sent.");
        }

        private static void Accept(long sender, long playerId, string playerName, string inviterText)
        {
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

        private static void SendGroupChat(long sender, long actor, string message)
        {
            SocialGroup group = GroupState.GetGroup(actor);
            GroupChatResult result = GroupChatPolicy.Prepare(group, actor, message,
                member => FindPeer(member) != 0L, ChatFormatting.FormatGroup);
            if (!result.Broadcast)
            {
                SendMessage(sender, result.Message);
                return;
            }
            Broadcast(group, result.Message);
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
            foreach (long target in new List<long>(GroupState.Invitations.Keys))
            {
                if (GroupState.Invitations[target] == playerId)
                {
                    GroupState.Invitations.Remove(target);
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

        private static void BroadcastServerMessage(string message)
        {
            foreach (long peerId in new List<long>(GroupState.PeerPlayers.Keys))
            {
                if (ZNet.instance.GetPeer(peerId) is ZNetPeer peer && peer.IsReady())
                {
                    SendMessage(peerId, message);
                }
            }
            SocializePlugin.Log.LogInfo(message);
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
