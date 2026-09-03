using System;
using System.Collections.Generic;
using Splatform;
using UnityEngine;

namespace Landoria.Socialize
{
    internal static class TextPermissionService
    {
        private const float TimeoutSeconds = 3f;
        private static readonly Dictionary<string, RelationsManagerPermissionResult> Cache =
            new Dictionary<string, RelationsManagerPermissionResult>();
        private static readonly Dictionary<string, PendingCheck> Pending =
            new Dictionary<string, PendingCheck>();

        private sealed class PendingCheck
        {
            internal PlatformUserID User;
            internal bool IsSender;
            internal float StartedAt;
            internal readonly List<Action<RelationsManagerPermissionResult>> Callbacks =
                new List<Action<RelationsManagerPermissionResult>>();
        }

        internal static void Check(PlatformUserID user, bool isSender,
            Action<RelationsManagerPermissionResult> completed)
        {
            string key = GetKey(user, isSender);
            if (Cache.TryGetValue(key, out RelationsManagerPermissionResult cached))
            {
                SocializePlugin.Log.LogInfo(
                    $"Using cached text permission for user={user}, isSender={isSender}: {cached}.");
                completed(cached);
                return;
            }
            if (Pending.TryGetValue(key, out PendingCheck existing))
            {
                existing.Callbacks.Add(completed);
                SocializePlugin.Log.LogDebug(
                    $"Joined pending text permission check for user={user}, isSender={isSender}.");
                return;
            }

            PendingCheck check = new PendingCheck
            {
                User = user,
                IsSender = isSender,
                StartedAt = Time.realtimeSinceStartup
            };
            check.Callbacks.Add(completed);
            Pending[key] = check;
            RelationsManager.CheckPermissionAsync(user, Permission.CommunicateWithUsingText,
                isSender, result => Complete(key, result, "provider"));
        }

        internal static void Update()
        {
            List<string> expired = null;
            foreach (KeyValuePair<string, PendingCheck> entry in Pending)
            {
                if (Time.realtimeSinceStartup - entry.Value.StartedAt >= TimeoutSeconds)
                {
                    (expired ??= new List<string>()).Add(entry.Key);
                }
            }
            if (expired == null) return;
            foreach (string key in expired)
            {
                if (!Pending.TryGetValue(key, out PendingCheck check)) continue;
                RelationsManagerPermissionResult fallback = GetFallback(check.User);
                SocializePlugin.Log.LogWarning(
                    $"Text permission check timed out for user={check.User}, isSender={check.IsSender}; using {fallback} fallback.");
                Complete(key, fallback, "timeout fallback");
            }
        }

        internal static void Reset()
        {
            Cache.Clear();
            Pending.Clear();
        }

        private static RelationsManagerPermissionResult GetFallback(PlatformUserID user)
        {
            PrivilegeResult privilege = PlatformManager.DistributionPlatform.PrivilegeProvider
                .CheckPrivilege(Privilege.TextCommunication);
            if (!privilege.IsGranted()) return RelationsManagerPermissionResult.Denied;
            return RelationsManager.FilterTextCommunicationSentToUser(user)
                ? RelationsManagerPermissionResult.GrantedRequiresFiltering
                : RelationsManagerPermissionResult.Granted;
        }

        private static void Complete(string key, RelationsManagerPermissionResult result, string source)
        {
            if (!Pending.TryGetValue(key, out PendingCheck check))
            {
                SocializePlugin.Log.LogDebug(
                    $"Ignoring late text permission result from {source}: {result}.");
                return;
            }
            Pending.Remove(key);
            if (result != RelationsManagerPermissionResult.Error)
            {
                Cache[key] = result;
            }
            SocializePlugin.Log.LogInfo(
                $"Text permission resolved by {source} for user={check.User}, isSender={check.IsSender}: {result}.");
            foreach (Action<RelationsManagerPermissionResult> callback in check.Callbacks)
            {
                callback(result);
            }
        }

        private static string GetKey(PlatformUserID user, bool isSender)
        {
            return (isSender ? "send:" : "receive:") + user;
        }
    }
}
