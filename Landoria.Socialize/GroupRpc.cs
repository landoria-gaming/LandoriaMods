namespace Landoria.Socialize
{
    internal static class GroupRpc
    {
        internal static void RPC_Request(long sender, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }
            SocializePlugin.Settings.InitializeServer(SocializePlugin.Log);
            string action = package.ReadString();
            long playerId = package.ReadLong();
            string playerName = package.ReadString();
            string argument = package.ReadString().Trim();
            UserInfo user = new UserInfo();
            user.Deserialize(ref package);
            GroupService.Dispatch(sender, playerId, playerName, action, argument, user);
        }

        internal static void RPC_Response(long sender, ZPackage package)
        {
            if (!GroupService.IsExpectedServer(sender))
            {
                SocializePlugin.Log.LogWarning(
                    $"Ignored group response from unexpected peer={sender}.");
                return;
            }
            GroupService.ReadResponse(package);
        }

        internal static void RPC_PingRequest(long sender, UnityEngine.Vector3 position, UserInfo user)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }
            GroupService.RelayPing(sender, position, user);
        }

        internal static void RPC_ChatReceipt(long sender, string requestId, int result)
        {
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                GroupService.ReceiveChatReceipt(sender, requestId,
                    (RelationsManagerPermissionResult)result);
            }
        }
    }
}
