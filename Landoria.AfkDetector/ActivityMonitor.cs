using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landoria.AfkDetector
{
    // Tracks player activity and finds inactive players.
    internal sealed class ActivityMonitor
    {
        private readonly Dictionary<long, PlayerActivity> _players =
            new Dictionary<long, PlayerActivity>();
        private readonly Action<ZNetPeer> _disconnect;
        private float _timeoutSeconds;
        private float _movementToleranceSquared;

        // Creates a monitor with its timeout and movement settings.
        internal ActivityMonitor(float timeoutSeconds, float movementTolerance,
            Action<ZNetPeer> disconnect)
        {
            _disconnect = disconnect;
            Configure(timeoutSeconds, movementTolerance);
        }

        // Updates the timeout and movement settings.
        internal void Configure(float timeoutSeconds, float movementTolerance)
        {
            _timeoutSeconds = timeoutSeconds;
            _movementToleranceSquared = movementTolerance * movementTolerance;
        }

        // Checks the activity of all connected players.
        internal void Update(List<ZNetPeer> peers, float now)
        {
            HashSet<long> connected = new HashSet<long>();
            foreach (ZNetPeer peer in peers)
            {
                if (!peer.IsReady())
                {
                    continue;
                }
                connected.Add(peer.m_uid);
                UpdatePeer(peer, now);
            }
            RemoveDisconnected(connected);
        }

        // Records chat activity for a player.
        internal void RecordChat(long peerId, float now)
        {
            if (_players.TryGetValue(peerId, out PlayerActivity activity))
            {
                activity.LastActivityAt = now;
            }
        }

        // Updates the activity state of one player.
        private void UpdatePeer(ZNetPeer peer, float now)
        {
            if (!_players.TryGetValue(peer.m_uid, out PlayerActivity activity))
            {
                _players[peer.m_uid] = new PlayerActivity(peer.GetRefPos(), now);
                return;
            }
            if (HasMoved(activity.Position, peer.GetRefPos()))
            {
                activity.Position = peer.GetRefPos();
                activity.LastActivityAt = now;
                return;
            }
            if (!activity.DisconnectRequested && now - activity.LastActivityAt >= _timeoutSeconds)
            {
                activity.DisconnectRequested = true;
                _disconnect(peer);
            }
        }

        // Checks whether a player moved far enough.
        private bool HasMoved(Vector3 previous, Vector3 current)
        {
            return (current - previous).sqrMagnitude >= _movementToleranceSquared;
        }

        // Removes players who are no longer connected.
        private void RemoveDisconnected(HashSet<long> connected)
        {
            List<long> stale = new List<long>();
            foreach (long peerId in _players.Keys)
            {
                if (!connected.Contains(peerId))
                {
                    stale.Add(peerId);
                }
            }
            foreach (long peerId in stale)
            {
                _players.Remove(peerId);
            }
        }

        // Stores the latest activity of one player.
        private sealed class PlayerActivity
        {
            internal Vector3 Position;
            internal float LastActivityAt;
            internal bool DisconnectRequested;

            // Creates an activity record for a player.
            internal PlayerActivity(Vector3 position, float now)
            {
                Position = position;
                LastActivityAt = now;
            }
        }
    }
}
