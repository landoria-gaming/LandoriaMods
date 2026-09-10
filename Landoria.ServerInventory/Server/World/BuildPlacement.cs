using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch]
    internal static class BuildPlacement
    {
        internal static void Validate(Player player, Piece piece, Vector3 position, Quaternion rotation)
        {
            float distance = Vector3.Distance(player.GetEyePoint(), position);
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance > player.m_maxPlaceDistance + piece.m_extraPlacementDistance)
                Deny("Construction out of range.");
            if (!ZNetScene.instance.IsAreaReady(position)) Deny("Construction area is not loaded.");
            if (Location.IsInsideNoBuildLocation(position)) Deny("Building is forbidden here.");
            var ward = piece.GetComponent<PrivateArea>();
            if (!PrivateArea.CheckAccess(position, ward == null ? 0 : ward.m_radius, false, ward != null)) Deny("Protected area.");
            var direction = position - player.GetEyePoint();
            int mask = LayerMask.GetMask("Default", "static_solid", "piece", "terrain", "vehicle");
            if (!Physics.Raycast(player.GetEyePoint(), direction.normalized, out var hit, distance + 1f, mask))
                Deny("No placement surface.");
            if (Vector3.Distance(hit.point, position) > 1.5f) Deny("Placement surface does not match the request.");
            ValidateSurface(player, piece, position, hit);
            var parent = new GameObject("Server placement validation");
            parent.SetActive(false);
            try
            {
                var ghost = UnityEngine.Object.Instantiate(piece.gameObject, position, rotation, parent.transform);
                ValidatePreview(player, ghost, piece, position);
            }
            finally { UnityEngine.Object.Destroy(parent); }
        }

        private static void ValidateSurface(Player player, Piece piece, Vector3 position, RaycastHit hit)
        {
            var ground = hit.collider.GetComponent<Heightmap>();
            var support = hit.collider.GetComponentInParent<WearNTear>();
            if (support != null && !support.m_supports) Deny("Surface does not support building.");
            if ((piece.m_groundOnly || piece.m_groundPiece) && ground == null) Deny("Ground required.");
            if (piece.m_cultivatedGroundOnly && (ground == null || !ground.IsCultivated(hit.point))) Deny("Cultivated ground required.");
            if (piece.m_vegetationGroundOnly && (ground == null || (ground.GetBiome(hit.point) == Heightmap.Biome.AshLands
                ? ground.GetVegetationMask(hit.point) > 0.1f : ground.GetVegetationMask(hit.point) < 0.25f))) Deny("Vegetated ground required.");
            if (piece.m_notOnWood && support != null && (support.m_materialType == WearNTear.MaterialType.Wood ||
                support.m_materialType == WearNTear.MaterialType.HardWood)) Deny("Wooden surface is not allowed.");
            if (piece.m_notOnTiltingSurface && hit.normal.y < 0.8f || piece.m_inCeilingOnly && hit.normal.y > -0.5f ||
                piece.m_notOnFloor && hit.normal.y > 0.1f) Deny("Invalid surface orientation.");
            bool submerged = position.y < Floating.GetLiquidLevel(position);
            if (piece.m_waterPiece && !submerged || piece.m_noInWater && submerged) Deny("Invalid water placement.");
            if (piece.m_onlyInTeleportArea && !EffectArea.IsPointInsideArea(position, EffectArea.Type.Teleport)) Deny("Teleport area required.");
            if (!piece.m_allowedInDungeons && Character.InInterior(position) && !EnvMan.instance.CheckInteriorBuildingOverride() &&
                !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DungeonBuild)) Deny("Cannot build in a dungeon.");
            var biome = Heightmap.FindBiome(position);
            if (piece.m_onlyInBiome != Heightmap.Biome.None && (biome & piece.m_onlyInBiome) == 0) Deny("Invalid biome.");
            if (!piece.m_allowedInDeepSnow && biome == Heightmap.Biome.DeepNorth && ground != null &&
                ground.GetCultivationMask(hit.point) > player.m_deepSnowBuildHeight) Deny("Snow is too deep.");
            if (piece.m_requireDeepSnow && (biome != Heightmap.Biome.DeepNorth || ground == null ||
                ground.GetCultivationMask(hit.point) <= 0)) Deny("Deep snow required.");
        }

        private static void ValidatePreview(Player player, GameObject ghost, Piece piece, Vector3 position)
        {
            if (piece.m_noClipping)
                foreach (var child in ghost.GetComponentsInChildren<Collider>(true))
                    if (TestClipping(player, child.gameObject, 0.2f)) Deny("Construction overlaps the world.");
            foreach (var collider in ghost.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger || !collider.enabled) continue;
                foreach (var character in Character.GetAllCharacters())
                {
                    var other = character.GetCollider();
                    if (other != null && Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                        other, other.transform.position, other.transform.rotation, out _, out _)) Deny("Construction overlaps a character.");
                }
            }
            var extension = ghost.GetComponent<StationExtension>();
            if (extension != null && (extension.FindClosestStationInRange(position) == null || extension.OtherExtensionInRange(piece.m_spaceRequirement)))
                Deny("Station extension requirements not met.");
            if (piece.m_mustConnectTo != null) Deny("This connection-specific construction is not supported yet.");
            if (piece.m_blockRadius <= 0 || piece.m_blockingPieces.Count == 0) return;
            foreach (var collider in Physics.OverlapSphere(position, piece.m_blockRadius, LayerMask.GetMask("piece")))
            {
                var other = collider.GetComponentInParent<Piece>();
                if (other != null && piece.m_blockingPieces.Any(value => value.m_name == other.m_name)) Deny("More space required.");
            }
        }

        private static void Deny(string reason) => throw new InvalidOperationException(reason);

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Player), "TestGhostClipping")]
        private static bool TestClipping(Player instance, GameObject ghost, float maxPenetration)
            => throw new NotImplementedException("Native build clipping check was not patched.");
    }
}
