using Landoria.SharedLib;

namespace Landoria.AfkDetector
{
    // Shows the AFK disconnect reason to the client.
    internal static class ClientDisconnectReason
    {
        internal const string RpcName = "Landoria_AfkDisconnectReason";

        // Stores the disconnect message for the connection screen.
        internal static void Receive(ZRpc rpc, string message)
        {
            ConnectionFailureMessages.Push("Landoria.AfkDetector", message);
        }
    }
}
