using System;
using HarmonyLib;

namespace Landoria.AfkDetector
{
    // Detects chat messages as player activity.
    [HarmonyPatch(typeof(ZRoutedRpc), "RPC_RoutedRPC")]
    internal static class ChatActivityPatch
    {
        private static readonly int ChatMessageHash = "ChatMessage".GetStableHashCode();
        private static readonly int SayHash = "Say".GetStableHashCode();

        // Records valid chat messages before Valheim handles them.
        private static void Prefix(ZPackage pkg)
        {
            if (!AfkDetectorServer.IsReady)
            {
                return;
            }
            try
            {
                ZRoutedRpc.RoutedRPCData data = ReadRoutedData(pkg);
                if (ContainsChatMessage(data))
                {
                    AfkDetectorServer.RecordChat(data.m_senderPeerID);
                }
            }
            catch (Exception exception)
            {
                AfkDetectorPlugin.Log.LogDebug($"Ignored unreadable chat activity: {exception}");
            }
        }

        // Reads routed data without changing the original package.
        private static ZRoutedRpc.RoutedRPCData ReadRoutedData(ZPackage source)
        {
            ZPackage copy = new ZPackage(source.GetArray());
            ZRoutedRpc.RoutedRPCData data = new ZRoutedRpc.RoutedRPCData();
            data.Deserialize(copy);
            return data;
        }

        // Checks whether routed data contains a chat message.
        private static bool ContainsChatMessage(ZRoutedRpc.RoutedRPCData data)
        {
            ZPackage parameters = new ZPackage(data.m_parameters.GetArray());
            if (data.m_methodHash == ChatMessageHash)
            {
                return ReadChatMessage(parameters, hasPosition: true);
            }
            if (data.m_methodHash == SayHash)
            {
                return ReadChatMessage(parameters, hasPosition: false);
            }

            return false;
        }

        // Reads a normal chat message and checks its text.
        private static bool ReadChatMessage(ZPackage package, bool hasPosition)
        {
            if (hasPosition)
            {
                package.ReadVector3();
            }
            package.ReadInt();
            package.ReadString();
            package.ReadString();
            return !string.IsNullOrWhiteSpace(package.ReadString());
        }

    }
}
