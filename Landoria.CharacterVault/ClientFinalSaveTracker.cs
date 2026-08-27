using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Landoria.CharacterVault
{
    internal sealed class ClientFinalSaveTracker
    {
        private const float ReceiptTimeoutSeconds = 10f;
        private readonly Dictionary<ZRpc, PendingFinalSave> _pending =
            new Dictionary<ZRpc, PendingFinalSave>();

        internal void RecordStarted(ZRpc rpc, string requestId, string playerName)
        {
            if (rpc == null ||
                requestId?.StartsWith("disconnect-", StringComparison.Ordinal) != true)
            {
                return;
            }
            _pending[rpc] = new PendingFinalSave(requestId, playerName);
            CharacterVaultPlugin.Instance.Run(WaitForReceipt(rpc, requestId));
        }

        internal void RecordReceived(ZRpc rpc, string requestId)
        {
            if (rpc != null && _pending.TryGetValue(rpc, out PendingFinalSave pending) &&
                string.Equals(pending.RequestId, requestId, StringComparison.Ordinal))
            {
                _pending.Remove(rpc);
            }
        }

        internal void Clear()
        {
            _pending.Clear();
        }

        private IEnumerator WaitForReceipt(ZRpc rpc, string requestId)
        {
            yield return new WaitForSecondsRealtime(ReceiptTimeoutSeconds);
            if (!_pending.TryGetValue(rpc, out PendingFinalSave pending) ||
                !string.Equals(pending.RequestId, requestId, StringComparison.Ordinal))
            {
                yield break;
            }
            _pending.Remove(rpc);
            CharacterVaultPlugin.Log.LogWarning(
                $"Final character save {requestId} from {pending.PlayerName} was not " +
                $"received within {ReceiptTimeoutSeconds:0} seconds.");
        }

        private sealed class PendingFinalSave
        {
            internal PendingFinalSave(string requestId, string playerName)
            {
                RequestId = requestId;
                PlayerName = playerName;
            }

            internal string PlayerName { get; }
            internal string RequestId { get; }
        }
    }
}
