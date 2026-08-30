using System.Collections;
using UnityEngine;

namespace Landoria.GetMyTrophyBack
{
    internal static class TrophyDropService
    {
        internal const string RpcName = "Landoria_DropGuardianTrophy";
        private const float DropDelaySeconds = 5f;

        internal static IEnumerator DropAfterDelay(ItemStand itemStand)
        {
            yield return new WaitForSeconds(DropDelaySeconds);
            RequestDrop(itemStand);
        }

        internal static void RequestDrop(ItemStand itemStand)
        {
            if (itemStand == null || !itemStand.HaveAttachment())
            {
                return;
            }

            ZNetView netView = GetNetView(itemStand);
            if (netView == null || !netView.IsValid())
            {
                GetMyTrophyBackPlugin.Log.LogWarning("Could not request the trophy drop because its network view is invalid.");
                return;
            }

            netView.InvokeRPC(RpcName);
        }

        internal static void HandleDropRequest(ItemStand itemStand, ZNetView netView, long sender)
        {
            if (!netView.IsOwner() || !itemStand.HaveAttachment())
            {
                return;
            }

            ZDO zdo = netView.GetZDO();
            string itemName = zdo.GetString(ZDOVars.s_item);
            GameObject prefab = ObjectDB.instance.GetItemPrefab(itemName);
            if (prefab == null)
            {
                GetMyTrophyBackPlugin.Log.LogWarning($"Could not drop missing trophy prefab {itemName}.");
                return;
            }

            SpawnTrophy(itemStand, prefab, zdo);
            ClearAttachment(netView, zdo);
            GetMyTrophyBackPlugin.Log.LogDebug($"Dropped trophy {itemName} after request by peer {sender}.");
        }

        private static ZNetView GetNetView(ItemStand itemStand)
        {
            return itemStand.m_netViewOverride
                ? itemStand.m_netViewOverride
                : itemStand.GetComponent<ZNetView>();
        }

        private static void SpawnTrophy(ItemStand itemStand, GameObject prefab, ZDO zdo)
        {
            GetAttachmentOffset(prefab, out Vector3 position, out Quaternion rotation);
            Transform spawn = itemStand.m_dropSpawnPoint;
            GameObject dropped = Object.Instantiate(
                prefab,
                spawn.position + position,
                spawn.rotation * rotation);
            dropped.GetComponent<ItemDrop>().LoadFromExternalZDO(zdo);
            Rigidbody body = dropped.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.up * 4f;
            }

            itemStand.m_effects.Create(spawn.position, Quaternion.identity);
        }

        private static void GetAttachmentOffset(
            GameObject prefab,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            Transform attach = prefab.transform.Find("attach");
            if (prefab.transform.Find("attachobj") != null && attach != null)
            {
                position = attach.localPosition;
                rotation = attach.localRotation;
            }
        }

        private static void ClearAttachment(ZNetView netView, ZDO zdo)
        {
            zdo.Set(ZDOVars.s_item, "");
            zdo.Set(ZDOVars.s_type, 0);
            netView.InvokeRPC(ZNetView.Everybody, "SetVisualItem", "", 0, 0, 0);
        }
    }
}
