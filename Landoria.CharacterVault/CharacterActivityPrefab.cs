using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Landoria.CharacterVault
{
    internal static class CharacterActivityPrefab
    {
        internal const string Name = "Landoria_CharacterActivity";

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        private static class ZNetSceneAwakePatch
        {
            private static void Prefix(ZNetScene __instance)
            {
                if (__instance.m_prefabs.Any(prefab => prefab != null && prefab.name == Name))
                {
                    return;
                }
                GameObject prefab = new GameObject(Name);
                prefab.SetActive(false);
                ZNetView view = prefab.AddComponent<ZNetView>();
                view.m_persistent = true;
                view.m_distant = false;
                view.m_type = ZDO.ObjectType.Default;
                __instance.m_prefabs.Add(prefab);
                CharacterVaultPlugin.Log.LogInfo(
                    $"Registered the {Name} world-record prefab.");
            }
        }
    }
}
