using System;
using Splatform;
using HarmonyLib;
using Landoria.SharedLib;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch(typeof(PlayerProfile), "SavePlayerToDisk")]
    internal static class CharacterSavePatch
    {
        internal static ModLog Log;

        private static bool Prefix(PlayerProfile __instance, ref bool __result)
        {
            if (ZNet.instance != null && ZNet.instance.IsDedicated()) return true;
            try
            {
                if (!ReferenceEquals(__instance, CharacterLoad.Profile) && !Exists(__instance)) return true;
                // Keep the vanilla save notifications balanced even when no write is needed.
                PlayerProfile.SavingStarted?.Invoke();
                PlayerProfile.SavingFinished?.Invoke();
                __result = true;
            }
            catch (Exception error)
            {
                Log.LogError(error);
                // Never overwrite a character when its storage cannot be checked.
                __result = false;
            }
            return false;
        }

        private static bool Exists(PlayerProfile profile)
        {
            bool mounted = false;
            try
            {
                if (FileHelpers.CloudStorageSupported)
                {
                    mounted = FileHelpers.Mount(SaveDataAccess.Read);
                    if (!mounted) throw new InvalidOperationException("Could not check character cloud storage.");
                }
                if (HasFile(profile, profile.m_fileSource)) return true;
                if (FileHelpers.LocalStorageSupportedAndAllowed &&
                    (HasFile(profile, FileHelpers.FileSource.Local) || HasFile(profile, FileHelpers.FileSource.Legacy)))
                    return true;
                return FileHelpers.CloudStorageSupported && HasFile(profile, FileHelpers.FileSource.Cloud);
            }
            finally
            {
                if (mounted) FileHelpers.Unmount();
            }
        }

        private static bool HasFile(PlayerProfile profile, FileHelpers.FileSource source)
            => FileHelpers.Exists(SaveSystem.GetCharacterPath(source, profile.m_filename), source);
    }
}
