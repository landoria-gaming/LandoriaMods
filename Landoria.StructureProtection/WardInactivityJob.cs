extern alias CharacterVaultApi;

using System;
using System.Collections.Generic;
using CharacterActivityApi =
    CharacterVaultApi::Landoria.CharacterVault.CharacterActivityApi;
using UnityEngine;

namespace Landoria.StructureProtection
{
    internal static class WardInactivityJob
    {
        private const int BatchSize = 50;
        private static readonly List<ZDO> Wards = new List<ZDO>();
        private static JobState state;
        private static HashSet<long> onlineCreators;
        private static DateTime cutoffUtc;
        private static DateTime startedUtc;
        private static float nextRun;
        private static float nextDependencyLog;
        private static int index;
        private static JobMetrics metrics;

        internal static void Update()
        {
            if (ZNet.instance?.IsServer() != true ||
                StructureProtectionPlugin.Settings.WardInactivityDays < 0)
            {
                return;
            }
            if (state == JobState.Running)
            {
                ProcessBatchSafely();
                return;
            }
            if (Time.unscaledTime < nextRun)
            {
                return;
            }
            TryStart();
        }

        internal static void Reset()
        {
            Wards.Clear();
            onlineCreators = null;
            state = JobState.Idle;
            nextRun = 0f;
            nextDependencyLog = 0f;
            index = 0;
            metrics = null;
        }

        private static void TryStart()
        {
            if (state == JobState.Running)
            {
                StructureProtectionPlugin.Log.LogWarning(
                    "Skipped a concurrent ward inactivity job launch.");
                return;
            }
            if (!CharacterActivityApi.IsReady || !WardQuota.TryGetWardSnapshot(out List<ZDO> wards))
            {
                WaitForDependencies();
                return;
            }
            Begin(wards);
        }

        private static void WaitForDependencies()
        {
            state = JobState.WaitingForDependencies;
            nextRun = Time.unscaledTime + 10f;
            if (Time.unscaledTime < nextDependencyLog)
            {
                return;
            }
            nextDependencyLog = Time.unscaledTime + 60f;
            StructureProtectionPlugin.Log.LogInfo(
                "Deferred the ward inactivity job until activity and ward data are ready.");
        }

        private static void Begin(List<ZDO> wards)
        {
            Wards.Clear();
            Wards.AddRange(wards);
            onlineCreators = StructureProtectionSession.GetOnlinePlayers();
            startedUtc = DateTime.UtcNow;
            cutoffUtc = startedUtc.AddDays(
                -StructureProtectionPlugin.Settings.WardInactivityDays);
            index = 0;
            metrics = new JobMetrics();
            state = JobState.Running;
            StructureProtectionPlugin.Log.LogInfo(
                $"Started ward inactivity job for {Wards.Count} wards; " +
                $"cutoff={cutoffUtc:O}.");
        }

        private static void ProcessBatchSafely()
        {
            try
            {
                int end = Math.Min(index + BatchSize, Wards.Count);
                while (index < end)
                {
                    ProcessWard(Wards[index++]);
                }
                if (index >= Wards.Count)
                {
                    Complete();
                }
            }
            catch (Exception exception)
            {
                StructureProtectionPlugin.Log.LogError(
                    $"Ward inactivity job failed: {exception}");
                Finish();
            }
        }

        private static void ProcessWard(ZDO ward)
        {
            metrics.Examined++;
            if (ward == null || !ward.GetBool(ZDOVars.s_enabled))
            {
                metrics.AlreadyInactive++;
                return;
            }
            metrics.Active++;
            long creator = ward.GetLong(ZDOVars.s_creator);
            if (creator == 0L)
            {
                metrics.InvalidCreator++;
                Disable(ward, creator, "creator ID is missing");
                return;
            }
            if (onlineCreators.Contains(creator))
            {
                metrics.CreatorOnline++;
                return;
            }
            EvaluateLastConnection(ward, creator);
        }

        private static void EvaluateLastConnection(ZDO ward, long creator)
        {
            if (!CharacterActivityApi.TryGetCharacterLastSeenOnlineUtc(
                creator, out DateTime lastSeenOnlineUtc))
            {
                metrics.MissingActivity++;
                Disable(ward, creator, "last seen online is missing");
                return;
            }
            if (lastSeenOnlineUtc <= cutoffUtc)
            {
                metrics.Expired++;
                Disable(ward, creator, $"last seen online at {lastSeenOnlineUtc:O}");
                return;
            }
            metrics.KeptActive++;
        }

        private static void Disable(ZDO ward, long creator, string reason)
        {
            ward.SetOwner(ZDOMan.GetSessionID());
            ward.Set(ZDOVars.s_enabled, false);
            metrics.Disabled++;
            string owner = ward.GetString(WardQuota.OwnerKey);
            StructureProtectionPlugin.Log.LogInfo(
                $"Disabled inactive ward {ward.m_uid}; creator={creator}, " +
                $"owner={owner}, reason={reason}.");
        }

        private static void Complete()
        {
            double durationMs = (DateTime.UtcNow - startedUtc).TotalMilliseconds;
            StructureProtectionPlugin.Log.LogInfo(
                $"Completed ward inactivity job in {durationMs:F0} ms: " +
                $"examined={metrics.Examined}, active={metrics.Active}, " +
                $"alreadyInactive={metrics.AlreadyInactive}, disabled={metrics.Disabled}, " +
                $"keptActive={metrics.KeptActive}, missingActivity={metrics.MissingActivity}, " +
                $"invalidCreator={metrics.InvalidCreator}, expired={metrics.Expired}, " +
                $"creatorOnline={metrics.CreatorOnline}.");
            Finish();
        }

        private static void Finish()
        {
            Wards.Clear();
            onlineCreators = null;
            metrics = null;
            state = JobState.Idle;
            nextRun = Time.unscaledTime +
                StructureProtectionPlugin.Settings.WardInactivityCheckHours * 3600f;
        }

        private enum JobState
        {
            Idle,
            WaitingForDependencies,
            Running
        }

        private sealed class JobMetrics
        {
            internal int Examined;
            internal int Active;
            internal int AlreadyInactive;
            internal int Disabled;
            internal int KeptActive;
            internal int MissingActivity;
            internal int InvalidCreator;
            internal int Expired;
            internal int CreatorOnline;
        }
    }
}
