using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Landoria.Socialize
{
    [HarmonyPatch(typeof(Minimap), "Update")]
    internal static class UpdateMapPingVisibilityPatch
    {
        private static void Postfix(Minimap __instance)
        {
            HidePublicPositionTogglePatch.UpdateVisibility(__instance);
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.SetPublicReferencePosition))]
    internal static class DisablePublicPositionPatch
    {
        private static void Prefix(ref bool pub)
        {
            pub = MapSharingPolicy.GetPublicPosition(
                SocializePlugin.Settings.RestrictPublicPositions, pub);
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.GetOtherPublicPlayers))]
    internal static class ShowGroupMembersOnMapPatch
    {
        private static void Postfix(List<ZNet.PlayerInfo> playerList)
        {
            GroupMapSharing.AddGroupMembers(playerList);
        }
    }

    [HarmonyPatch(typeof(Minimap), "Start")]
    internal static class HidePublicPositionTogglePatch
    {
        private static void Postfix(Minimap __instance)
        {
            UpdateVisibility(__instance);
        }

        internal static void UpdateVisibility(Minimap minimap)
        {
            UpdatePositionVisibility(minimap);
            if (minimap.m_pingImageObject == null) return;
            GetPingButton(minimap).SetActive(
                MapSharingPolicy.CanSendPublicPing(
                    SocializePlugin.Settings.RestrictPublicPings,
                    GroupService.IsLocalPlayerInGroup()));
        }

        private static void UpdatePositionVisibility(Minimap minimap)
        {
            Toggle toggle = minimap.m_publicPosition;
            if (toggle == null) return;
            bool visible = !SocializePlugin.Settings.RestrictPublicPositions;
            if (!visible) toggle.isOn = false;
            toggle.gameObject.SetActive(visible);
            SetDedicatedContainerVisibility(minimap, toggle, visible);
        }

        private static GameObject GetPingButton(Minimap minimap)
        {
            Button button = minimap.m_pingImageObject.GetComponentInParent<Button>(true);
            if (button != null && !IsMapRoot(minimap, button.gameObject))
            {
                return button.gameObject;
            }
            Transform parent = minimap.m_pingImageObject.transform.parent;
            if (parent != null && !IsMapRoot(minimap, parent.gameObject)
                && parent.GetComponentsInChildren<RawImage>(true).Length == 0)
            {
                return parent.gameObject;
            }
            return minimap.m_pingImageObject.gameObject;
        }

        private static void SetDedicatedContainerVisibility(
            Minimap minimap, Toggle toggle, bool visible)
        {
            Transform parent = toggle.transform.parent;
            if (parent == null || IsMapRoot(minimap, parent.gameObject)) return;
            bool onlyToggle = parent.GetComponentsInChildren<Toggle>(true).Length == 1;
            bool containsMap = parent.GetComponentsInChildren<RawImage>(true).Length > 0;
            if (onlyToggle && !containsMap) parent.gameObject.SetActive(visible);
        }

        private static bool IsMapRoot(Minimap minimap, GameObject target)
        {
            return target == minimap.m_largeRoot || target == minimap.m_smallRoot
                   || target == minimap.m_mapLarge || target == minimap.m_mapSmall;
        }
    }
}
