using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace Landoria.ModSentry
{
    internal static class CharacterVaultGuestPatch
    {
        internal static void Apply(string harmonyId)
        {
            if (!Chainloader.PluginInfos.TryGetValue("Landoria.CharacterVault", out var plugin) ||
                plugin.Instance == null) return;

            // Optional integration: resolve the patch target without referencing CharacterVault.dll.
            Type service = plugin.Instance.GetType().Assembly.GetType(
                "Landoria.CharacterVault.ProfileTransferService");
            MethodInfo target = service == null ? null : AccessTools.DeclaredMethod(service,
                "ShouldStoreCharacterOnServer", new[] { typeof(ZRpc) });
            if (target == null || !target.IsStatic || target.ReturnType != typeof(bool))
            {
                ModSentryPlugin.Log.LogWarning(
                    "CharacterVault guest patch is unavailable: the installed version lacks the expected method.");
                return;
            }

            new Harmony(harmonyId).Patch(target, postfix: new HarmonyMethod(
                typeof(CharacterVaultGuestPatch), nameof(Postfix)));
            ModSentryPlugin.Log.LogInfo("CharacterVault guest validation patch is active.");
        }

        private static void Postfix(ZRpc __0, ref bool __result)
        {
            // Temporary guests must not be required to create a new character or import or save a server profile.
            // Disable CharacterVault storage for these sessions while preserving normal player handling.
            if (GuestAdmissions.IsGuest(__0)) __result = false;
        }
    }
}
