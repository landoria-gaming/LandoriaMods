using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal static class ActivityJournal
    {
        private const int MaximumPendingEntries = 4096;
        private static readonly List<string> pending = new List<string>();
        private static bool active;
        private static long sequence;
        private static int dropped;

        internal static void Open()
        {
            pending.Clear();
            sequence = 0;
            dropped = 0;
            active = true;
        }

        internal static void Record(ItemObservation entry)
        {
            Enqueue(number => { entry.sequence = number; return entry; });
        }

        internal static void Record(ContainerObservation entry)
        {
            Enqueue(number => { entry.sequence = number; return entry; });
        }

        internal static void Record(PlayerAppearanceObserved entry)
        {
            Enqueue(number => { entry.sequence = number; return entry; });
        }

        internal static void Record(PlayerDebugFlyObserved entry)
        {
            Enqueue(number => { entry.sequence = number; return entry; });
        }

        internal static void Record(CreatureAppearanceObserved entry)
        {
            Enqueue(number => { entry.sequence = number; return entry; });
        }

        private static void Enqueue(Func<long, object> capture)
        {
            if (!active) return;
            try
            {
                if (pending.Count >= MaximumPendingEntries)
                {
                    dropped++;
                    return;
                }
                object entry = capture(sequence + 1);
                if (!ObserverEventGuard.IsObservationOfOtherPlayer(entry)) return;
                sequence++;
                pending.Add(JournalJson.SerializeEvent(entry));
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log.LogError(exception);
            }
        }

        internal static void Flush()
        {
            if (!active || (pending.Count == 0 && dropped == 0)) return;
            try
            {
                if (dropped != 0)
                {
                    pending.Add(JournalJson.SerializeEvent(new JournalOverflow { droppedEntries = dropped }));
                    RavenWatchPlugin.Log.LogWarning($"Activity buffer dropped {dropped} entries: buffer full.");
                }
                EventTransport.Queue(pending);
                pending.Clear();
                dropped = 0;
            }
            catch (Exception exception) { RavenWatchPlugin.Log.LogError(exception); }
        }

        internal static void Close()
        {
            Flush();
            active = false;
        }
    }
}
