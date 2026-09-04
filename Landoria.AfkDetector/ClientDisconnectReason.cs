using Landoria.SharedLib;

namespace Landoria.AfkDetector
{
    internal static class ClientDisconnectReason
    {
        internal static void Receive(ZRpc rpc, string message)
        {
            ConnectionFailureMessages.Push("Landoria.AfkDetector", message);
        }
    }
}
