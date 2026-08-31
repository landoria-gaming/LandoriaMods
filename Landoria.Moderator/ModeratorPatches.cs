using HarmonyLib;

namespace Landoria.Moderator
{
    internal static class ModeratorState
    {
        internal const string ModeratorZdoKey = "Landoria.Moderator.Active";
        internal const string ModeratorLabel = "<color=#00A000>[Moderator]</color>";
        internal static bool IsActive { get; private set; }

        internal static void SetEnabled(bool active)
        {
            IsActive = active;
            if (!active)
            {
                ModeratorMapSharing.Disable();
            }
            AdminPlayerModes.Apply(active);
            Player player = Player.m_localPlayer;
            ZNetView view = player != null ? player.GetComponent<ZNetView>() : null;
            ZDO zdo = view?.GetZDO();
            if (zdo != null && zdo.IsOwner())
            {
                zdo.Set(ModeratorZdoKey, active);
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "GetOtherPublicPlayers")]
    internal static class ShowPlayersToModeratorOnMapPatch
    {
        private static void Postfix(System.Collections.Generic.List<ZNet.PlayerInfo> playerList)
        {
            ModeratorMapSharing.AddPlayers(playerList);
        }
    }

    [HarmonyPatch(typeof(Player), "OnSpawned")]
    internal static class LocalModeratorStatePatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }
            ModeratorState.SetEnabled(false);
            AdminAccess.RequestStatus();
        }
    }

    [HarmonyPatch(typeof(Game), "Start")]
    internal static class AdminRpcRegistrationPatch
    {
        private static void Postfix()
        {
            AdminAccess.RegisterRpcs();
            PlayerPositionRpc.RegisterResponseRpc();
            EventControlRpc.RegisterRpcs();
            WeatherControlRpc.RegisterRpcs();
        }
    }

    [HarmonyPatch(typeof(Player), "Awake")]
    internal static class PlayerPositionRpcRegistrationPatch
    {
        private static void Postfix(Player __instance, ZNetView ___m_nview)
        {
            PlayerPositionRpc.RegisterPlayerRpc(__instance, ___m_nview);
        }
    }

    [HarmonyPatch(typeof(Player), "GetHoverName")]
    internal static class ModeratorHoverNamePatch
    {
        private static void Postfix(Player __instance, ref string __result)
        {
            ZNetView view = __instance.GetComponent<ZNetView>();
            if (view?.GetZDO()?.GetBool(ModeratorState.ModeratorZdoKey) == true)
            {
                __result += "  " + ModeratorState.ModeratorLabel;
            }
        }
    }

    [HarmonyPatch(typeof(Terminal.ConsoleCommand), "IsValid")]
    internal static class ModeratorCommandValidationPatch
    {
        private static void Postfix(Terminal.ConsoleCommand __instance, ref bool __result)
        {
            if (!__instance.OnlyAdmin)
            {
                return;
            }

            bool isAdmin = AdminAccess.LocalPlayerIsAdminOrHost();
            bool inactiveModeratorCommand =
                ModeratorPlugin.RequiresEnabledModerator(__instance.Command) &&
                !ModeratorState.IsActive;
            if (!isAdmin || inactiveModeratorCommand)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    internal static class ModeratorDisconnectPatch
    {
        private static void Postfix()
        {
            ModeratorState.SetEnabled(false);
            AdminAccess.ResetSession();
            PlayerPositionRpc.ResetSession();
            EventControlRpc.ResetSession();
            WeatherControlRpc.ResetSession();
        }
    }
}
