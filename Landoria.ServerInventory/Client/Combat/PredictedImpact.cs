using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Landoria.ServerInventory.Network;
using Landoria.ServerInventory.Server;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch]
    internal static class PredictedImpact
    {
        private static readonly HashSet<string> played = new HashSet<string>();
        private static readonly Queue<string> order = new Queue<string>();

        internal static void Play(string request, Attack attack, Humanoid actor, ItemDrop.ItemData weapon)
        {
            if (attack.m_attackType != Attack.AttackType.Horizontal && attack.m_attackType != Attack.AttackType.Vertical) return;
            try
            {
                if (!FindContact(attack, actor, out var contact)) return;
                PlayList(request, weapon.m_shared.m_hitEffect, contact.point);
                PlayList(request, attack.m_hitEffect, contact.point);
                var target = Projectile.FindHitObject(contact.collider);
                PlayList(request, target.GetComponent<Character>()?.m_hitEffects, contact.point);
                PlayList(request, target.GetComponent<TreeBase>()?.m_hitEffect, contact.point);
                PlayList(request, target.GetComponent<TreeLog>()?.m_hitEffect, contact.point);
                PlayList(request, target.GetComponent<Destructible>()?.m_hitEffect, contact.point);
                PlayList(request, target.GetComponent<WearNTear>()?.m_hitEffect, contact.point);
                PlayList(request, target.GetComponent<MineRock>()?.m_hitEffect, contact.point);
                PlayList(request, target.GetComponent<MineRock5>()?.m_hitEffect, contact.point);
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
        }

        private static bool FindContact(Attack attack, Humanoid actor, out RaycastHit contact)
        {
            Direction(attack, out var joint, out var direction);
            var origin = joint.position + Vector3.up * attack.m_attackHeight + actor.transform.right * attack.m_attackOffset;
            int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid",
                "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
            if (attack.m_hitTerrain) mask |= LayerMask.GetMask("terrain");
            var local = actor.transform.InverseTransformDirection(direction);
            for (float angle = -attack.m_attackAngle / 2f; angle <= attack.m_attackAngle / 2f; angle += 4f)
            {
                var rotation = attack.m_attackType == Attack.AttackType.Horizontal
                    ? Quaternion.Euler(0, -angle, 0) : Quaternion.Euler(angle, 0, 0);
                var ray = actor.transform.TransformDirection(rotation * local);
                var hits = attack.m_attackRayWidth > 0
                    ? Physics.SphereCastAll(origin, attack.m_attackRayWidth, ray,
                        Mathf.Max(0, attack.m_attackRange - attack.m_attackRayWidth), mask, QueryTriggerInteraction.Ignore)
                    : Physics.RaycastAll(origin, ray, attack.m_attackRange, mask, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits.OrderBy(value => value.distance))
                {
                    if (hit.collider.GetComponentInParent<Character>() == actor) continue;
                    contact = hit;
                    return true;
                }
            }
            contact = default;
            return false;
        }

        [HarmonyReversePatch, HarmonyPatch(typeof(Attack), "GetMeleeAttackDir")]
        private static void Direction(Attack instance, out Transform joint, out Vector3 direction)
            => throw new NotImplementedException("Native melee direction reverse patch.");

        private static void PlayList(string request, EffectList effects, Vector3 position)
        {
            if (effects == null) return;
            foreach (var effect in effects.m_effectPrefabs)
            {
                if (!effect.m_enabled || effect.m_prefab == null) continue;
                string key = request + "/" + effect.m_prefab.name;
                if (played.Contains(key)) continue;
                bool audible = false;
                foreach (var sound in effect.m_prefab.GetComponentsInChildren<ZSFX>(true))
                    audible |= PlaySound(sound, position);
                if (!audible) continue;
                played.Add(key);
                order.Enqueue(key);
                while (order.Count > 4096) played.Remove(order.Dequeue());
            }
        }

        private static bool PlaySound(ZSFX sound, Vector3 position)
        {
            var original = sound.GetComponent<AudioSource>();
            if (original == null || original.loop || sound.m_audioClips.Length == 0) return false;
            var clip = sound.m_audioClips[UnityEngine.Random.Range(0, sound.m_audioClips.Length)];
            if (clip == null) return false;
            var local = new GameObject("ServerInventory impact audio");
            local.transform.position = position;
            var source = local.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = original.outputAudioMixerGroup;
            source.spatialBlend = original.spatialBlend;
            source.rolloffMode = original.rolloffMode;
            source.minDistance = original.minDistance;
            source.maxDistance = original.maxDistance;
            source.volume = UnityEngine.Random.Range(sound.m_minVol, sound.m_maxVol);
            source.pitch = Mathf.Max(0.01f, UnityEngine.Random.Range(sound.m_minPitch, sound.m_maxPitch));
            source.clip = clip;
            source.Play();
            UnityEngine.Object.Destroy(local, clip.length / source.pitch + 0.1f);
            return true;
        }

        internal static bool WasPlayed(ZNetView view)
        {
            if (view == null || !view.IsValid()) return false;
            var zdo = view.GetZDO();
            string request = zdo.GetString(AttackSoundScope.Key, "");
            var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
            return prefab != null && played.Contains(request + "/" + prefab.name);
        }
    }

    [HarmonyPatch(typeof(ZSFX), nameof(ZSFX.Play))]
    internal static class PredictedImpactEcho
    {
        private static bool Prefix(ZSFX __instance)
            => !ClientDamageGuard.Active || !PredictedImpact.WasPlayed(__instance.GetComponentInParent<ZNetView>());
    }
}
