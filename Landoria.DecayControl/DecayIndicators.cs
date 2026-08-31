using HarmonyLib;
using UnityEngine;

namespace Landoria.DecayControl
{
    internal static class DecayIndicators
    {
        internal static bool Enabled { get; private set; }

        internal static void Toggle() { Enabled = !Enabled; }
        internal static void Reset() { Enabled = false; }

        internal static string FireplaceLabel(Fireplace fireplace)
        {
            if (!Enabled || fireplace == null) return null;
            ZNetView view = fireplace.GetComponent<ZNetView>();
            if (view == null || !view.IsValid()) return null;
            float fuel = view.GetZDO().GetFloat(ZDOVars.s_fuel);
            float displayedFuel = fuel * 1000f;
            float displayedMaxFuel = fireplace.m_maxFuel * 1000f;
            string label = Localization.instance.Localize("$piece_fire_fuel");
            string state = DecayProtection.IsFuelDecayOff(
                fireplace.GetComponent<Piece>()) ? " (off)" : "";
            return $"( {label} {displayedFuel:0}/{displayedMaxFuel:0} ) {state}";
        }

        internal static string BuildingLabel(Piece piece)
        {
            if (!Enabled || piece == null) return null;
            WearNTear wear = piece.GetComponent<WearNTear>();
            ZNetView view = piece.GetComponent<ZNetView>();
            if (wear == null || view == null || !view.IsValid()) return null;
            float health = view.GetZDO().GetFloat(ZDOVars.s_health, wear.m_health);
            float displayedHealth = health ;
            float displayedMaxHealth = wear.m_health;
            string label = Localization.instance.Localize("$se_health");
            string state = DecayProtection.IsEnvironmentalWearOff(piece)
                ? " (off)"
                : "";
            return $"\n{displayedHealth:0}/{displayedMaxHealth:0} {state}";
        }
    }

    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
    internal static class FireplaceDecayIndicatorPatch
    {
        private static void Postfix(Fireplace __instance, ref string __result)
        {
            string label = DecayIndicators.FireplaceLabel(__instance);
            if (!string.IsNullOrEmpty(label))
                __result = string.IsNullOrEmpty(__result) ? label : __result + "\n" + label;
        }
    }

    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    internal static class BuildingDecayIndicatorPatch
    {
        private static void Postfix(Hud __instance, Player player)
        {
            Piece piece = player == Player.m_localPlayer ? player.GetHoveringPiece() : null;
            string label = DecayIndicators.BuildingLabel(piece);
            if (!string.IsNullOrEmpty(label))
                __instance.m_hoverName.text = string.IsNullOrEmpty(__instance.m_hoverName.text)
                    ? label
                    : __instance.m_hoverName.text + "\n" + label;
        }
    }
}
