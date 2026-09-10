using System;
using HarmonyLib;
using UnityEngine;

namespace Landoria.ServerInventory.Server
{
    internal sealed class AttackSoundScope : IDisposable
    {
        internal const string Key = "Landoria.ServerInventory.attackSound";
        [ThreadStatic] internal static string Current;
        private readonly string previous;
        internal AttackSoundScope(string request) { previous = Current; Current = request; }
        public void Dispose() => Current = previous;
    }

    [HarmonyPatch(typeof(EffectList), nameof(EffectList.Create))]
    internal static class AttackSoundTag
    {
        private static void Postfix(GameObject[] __result)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated() || AttackSoundScope.Current == null) return;
            foreach (var effect in __result)
            {
                var view = effect == null ? null : effect.GetComponent<ZNetView>();
                if (view != null && view.IsValid())
                    view.GetZDO().Set(AttackSoundScope.Key, AttackSoundScope.Current);
            }
        }
    }
}
