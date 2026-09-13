using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Landoria.StructureProtection
{
    internal static class CharacterActivityPrefab
    {
        // Existing worlds already use this prefab name for persistent activity records.
        internal const string Name = "Landoria_CharacterActivity";
        private static GameObject template;

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        private static class ZNetSceneAwakePatch
        {
            private static void Prefix(ZNetScene __instance)
            {
                template = __instance.m_prefabs.FirstOrDefault(prefab =>
                    prefab != null && prefab.name == Name);
                if (template != null)
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
                template = prefab;
                StructureProtectionPlugin.Log.LogInfo(
                    $"Registered the {Name} world-record prefab.");
            }
        }

        // Marks data-only activity records as loaded without creating scene objects.
        [HarmonyPatch(typeof(ZNetScene), "CreateObject")]
        private static class ZNetSceneCreateObjectPatch
        {
            private static bool Prefix(ZDO zdo, ref GameObject __result)
            {
                if (zdo.GetPrefab() != Name.GetStableHashCode())
                {
                    return true;
                }

                zdo.Created = true;
                __result = template;
                return false;
            }
        }
    }
}
