using Landoria.SharedLib;

namespace Landoria.ModSentry
{
    internal static class ClientMessage
    {
        internal static void Receive(ZRpc rpc, string message)
        {
            ConnectionFailureMessages.Push("Landoria.ModSentry", message);
            ModSentryPlugin.Log.LogWarning($"Server rejected the connection: {message}");
            rpc.Invoke(ModSentryPlugin.RejectionAckRpc);
            ModSentryPlugin.Log.LogDebug(
                "Acknowledged the rejection; waiting for the server disconnect.");
        }

        internal static void Clear()
        {
            ConnectionFailureMessages.Clear("Landoria.ModSentry");
        }
    }
}
