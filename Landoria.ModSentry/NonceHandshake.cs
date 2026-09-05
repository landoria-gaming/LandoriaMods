using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HarmonyLib;
using UnityEngine;

namespace Landoria.ModSentry
{
    internal static class NonceHandshake
    {
        private const string RequestRpc = "Landoria_ModSentry_ChallengeRequest_v2";
        private const string ChallengeRpc = "Landoria_ModSentry_Challenge_v2";
        private const float TimeoutSeconds = 30f;
        private static readonly Dictionary<ZRpc, Challenge> Challenges = new Dictionary<ZRpc, Challenge>();
        private static ZRpc serverRpc;
        private static ZNet clientNetwork;
        private static string pendingPassword;
        private static bool requested;
        private static bool sent;
        private static float clientDeadline;

        internal static void Register(ZNet network, ZRpc rpc)
        {
            if (network.IsServer())
            {
                byte[] bytes = new byte[32];
                using (RandomNumberGenerator random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
                Challenges[rpc] = new Challenge { Nonce = Convert.ToBase64String(bytes) };
                rpc.Register<int>(RequestRpc, Request);
                return;
            }
            serverRpc = rpc;
            clientNetwork = network;
            pendingPassword = null;
            requested = sent = false;
            rpc.Register<int, string>(ChallengeRpc, Receive);
        }

        internal static bool AllowPeerInfo(ZNet network, ZRpc rpc, string password)
        {
            if (!ReferenceEquals(serverRpc, rpc)) return false;
            if (sent) return true;
            if (!requested)
            {
                requested = true;
                clientNetwork = network;
                pendingPassword = password;
                clientDeadline = Time.unscaledTime + TimeoutSeconds;
                rpc.Invoke(RequestRpc, ModSentryPlugin.ProtocolVersion);
            }
            return false;
        }

        private static void Request(ZRpc rpc, int protocol)
        {
            if (!Challenges.TryGetValue(rpc, out Challenge challenge) || IsFinal(rpc)) return;
            if (challenge.Started) return;
            challenge.Started = true;
            challenge.Deadline = Time.unscaledTime + TimeoutSeconds;
            if (protocol != ModSentryPlugin.ProtocolVersion)
            {
                Reject(rpc, "Incompatible or late ModSentry challenge request.");
                return;
            }
            rpc.Invoke(ChallengeRpc, ModSentryPlugin.ProtocolVersion, challenge.Nonce);
        }

        private static void Receive(ZRpc rpc, int protocol, string nonce)
        {
            if (!ReferenceEquals(serverRpc, rpc) || !requested || sent) return;
            try
            {
                if (protocol != ModSentryPlugin.ProtocolVersion || nonce == null || nonce.Length != 44 ||
                    Convert.FromBase64String(nonce).Length != 32)
                    throw new InvalidDataException("Invalid ModSentry challenge.");
                rpc.Invoke(ModSentryPlugin.InventoryRpc, PluginInventory.Serialize(nonce));
                sent = true;
                // Resume the normal patched call so other plugins retain their handshake ordering.
                string password = pendingPassword;
                pendingPassword = null;
                AccessTools.Method(typeof(ZNet), "SendPeerInfo", new[] { typeof(ZRpc), typeof(string) })
                    .Invoke(clientNetwork, new object[] { rpc, password });
            }
            catch (Exception exception)
            {
                ModSentryPlugin.Log.LogError(exception);
                FailClient("Mod verification failed. Please update ModSentry and reconnect.");
            }
        }

        internal static bool Consume(ZRpc rpc, ZPackage package)
        {
            if (!Challenges.TryGetValue(rpc, out Challenge challenge) || !challenge.Started ||
                challenge.Consumed)
            {
                Reject(rpc, "Unexpected or replayed ModSentry inventory.");
                return false;
            }
            challenge.Consumed = true;
            string expected = challenge.Nonce;
            challenge.Nonce = null;
            if (Time.unscaledTime >= challenge.Deadline || package.ReadInt() != ModSentryPlugin.ProtocolVersion ||
                !string.Equals(package.ReadString(), expected, StringComparison.Ordinal))
            {
                Reject(rpc, "Invalid or expired ModSentry inventory nonce.");
                return false;
            }
            return true;
        }

        internal static bool IsFinal(ZRpc rpc) =>
            HandshakeState.IsAccepted(rpc) || HandshakeState.RejectionFor(rpc) != null;

        private static void Reject(ZRpc rpc, string reason)
        {
            if (IsFinal(rpc)) return;
            if (Challenges.TryGetValue(rpc, out Challenge challenge))
            {
                challenge.Consumed = true;
                challenge.Nonce = null;
            }
            ModSentryHandshake.Record(rpc, ValidationResult.Reject(
                "Mod verification failed. Please update ModSentry and reconnect.", reason));
        }

        internal static void Tick()
        {
            foreach (var pair in Challenges.ToArray())
            {
                if (pair.Value.Started && !pair.Value.Consumed && Time.unscaledTime >= pair.Value.Deadline)
                    Reject(pair.Key, "ModSentry inventory challenge timed out.");
            }
            if (requested && !sent && Time.unscaledTime >= clientDeadline)
                FailClient("The server did not provide a compatible ModSentry challenge. Please update and reconnect.");
        }

        private static void FailClient(string message)
        {
            pendingPassword = null;
            requested = false;
            var rpc = serverRpc;
            serverRpc = null;
            Landoria.SharedLib.ConnectionFailureMessages.Push("Landoria.ModSentry", message, true);
            if (rpc != null) ModSentryHandshake.ForceDisconnect(rpc);
        }

        internal static void Remove(ZRpc rpc)
        {
            Challenges.Remove(rpc);
            if (!ReferenceEquals(serverRpc, rpc)) return;
            serverRpc = null;
            clientNetwork = null;
            pendingPassword = null;
            requested = sent = false;
        }

        internal static void Clear()
        {
            pendingPassword = null;
            Challenges.Clear();
            serverRpc = null;
            clientNetwork = null;
            requested = sent = false;
        }

        private sealed class Challenge
        {
            internal string Nonce;
            internal bool Started;
            internal bool Consumed;
            internal float Deadline;
        }
    }
}
