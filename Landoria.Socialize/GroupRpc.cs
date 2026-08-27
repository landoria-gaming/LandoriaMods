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
            GroupService.Dispatch(sender, playerId, playerName, action, argument);
        }

        internal static void RPC_Response(long sender, ZPackage package)
        {
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
    }
}
