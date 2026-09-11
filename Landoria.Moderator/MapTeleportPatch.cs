using HarmonyLib;
using UnityEngine;

namespace Landoria.Moderator
{
    [HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
    internal static class MapTeleportPatch
    {
        private static bool Prefix(Minimap __instance)
        {
            if (!CanTeleport() || !TryGetMapPosition(__instance, out Vector3 destination))
            {
                return true;
            }

            destination.y = WorldGenerator.instance.GetHeight(destination.x, destination.z);
            Player player = Player.m_localPlayer;
            player.TeleportTo(destination, player.transform.rotation, distantTeleport: true);
            __instance.SetMapMode(Minimap.MapMode.Small);
            ModeratorPlugin.ModLogger.LogDebug(
                $"Map teleport moved the local player to {destination}.");
            return false;
        }

        private static bool CanTeleport()
        {
            return Player.m_localPlayer != null &&
                   WorldGenerator.instance != null &&
                   ModeratorState.IsActive &&
                   ZInput.GetKey(KeyCode.LeftShift);
        }

        private static bool TryGetMapPosition(Minimap map, out Vector3 position)
        {
            RectTransform rect = map.m_mapImageLarge.transform as RectTransform;
            if (rect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, ZInput.pointerPosition, null, out Vector2 localPoint))
            {
                position = Vector3.zero;
                return false;
            }

            Vector2 normalized = Rect.PointToNormalized(rect.rect, localPoint);
            Rect visibleMap = map.m_mapImageLarge.uvRect;
            float mapX = visibleMap.xMin + normalized.x * visibleMap.width;
            float mapY = visibleMap.yMin + normalized.y * visibleMap.height;
            float halfTexture = map.m_textureSize / 2f;
            position = new Vector3((mapX * map.m_textureSize - halfTexture) * map.m_pixelSize,
                0f, (mapY * map.m_textureSize - halfTexture) * map.m_pixelSize);
            return true;
        }
    }
}
