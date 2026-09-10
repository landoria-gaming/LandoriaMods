using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class ServerAttacks
    {
        private sealed class Session
        {
            internal readonly HashSet<Guid> Seen = new HashSet<Guid>();
            internal readonly Dictionary<ZDOID, float> Last = new Dictionary<ZDOID, float>();
        }
        private sealed class Running
        {
            internal Humanoid Character;
            internal Attack Attack;
            internal float Until;
            internal string Request;
        }
        private static readonly ConditionalWeakTable<ZRpc, Session> sessions = new ConditionalWeakTable<ZRpc, Session>();
        private static readonly List<Running> running = new List<Running>();

        internal static void Register(ZNetPeer peer)
        {
            sessions.Add(peer.m_rpc, new Session());
            peer.m_rpc.Register<ZPackage>(CharacterRpc.Attack, Receive);
        }

        private static void Receive(ZRpc rpc, ZPackage package)
        {
            if (!InventoryChangesServer.IsLoaded(rpc) || !sessions.TryGetValue(rpc, out var session)) return;
            try
            {
                if (!Guid.TryParse(package.ReadString(), out var request)) throw new InvalidDataException("Invalid attack request.");
                if (session.Seen.Contains(request)) return;
                var id = package.ReadZDOID();
                int hash = package.ReadInt(), quality = package.ReadInt();
                bool secondary = package.ReadBool();
                float draw = package.ReadSingle();
                if (float.IsNaN(draw) || draw < 0 || draw > 1) throw new InvalidDataException("Invalid attack draw.");
                var character = ResolveActor(rpc, id);
                if (session.Last.TryGetValue(id, out float last) && Time.realtimeSinceStartup - last < 0.15f) return;
                if (session.Seen.Count >= 100000) throw new InvalidDataException("Attack request limit reached.");
                session.Seen.Add(request);
                session.Last[id] = Time.realtimeSinceStartup;
                using (var sound = new AttackSoundScope(request.ToString("D")))
                    Execute(character, hash, quality, secondary, draw);
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
        }

        private static Humanoid ResolveActor(ZRpc rpc, ZDOID id)
        {
            var peer = ZNet.instance.GetPeers().FirstOrDefault(p => p.m_rpc == rpc && p.IsReady());
            if (peer == null) throw new InvalidDataException("Attack connection is not ready.");
            var zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null) throw new InvalidDataException("Attack actor ZDO has not reached the server.");
            if (zdo.GetOwner() != peer.m_uid) throw new InvalidDataException("Attack actor network owner does not match the connection.");
            var instance = ZNetScene.instance.FindInstance(id);
            if (instance == null) throw new InvalidDataException("Attack actor is not loaded in the server scene.");
            var character = instance.GetComponent<Humanoid>();
            if (character == null) throw new InvalidDataException("Attack actor is not a humanoid.");
            if (character.IsPlayer() && peer.m_characterID != id)
                throw new InvalidDataException("Attack player does not match the connection character.");
            return character;
        }

        private static void Execute(Humanoid character, int hash, int quality, bool secondary, float draw)
        {
            using (var scope = new CombatScope(character))
            {
                CombatCharacters.Prepare(character);
                if (character.IsDead() || character.GetHealth() <= 0f || character.IsStaggering()) return;
                var weapon = character.GetInventory().GetAllItems().FirstOrDefault(item =>
                    item.m_dropPrefab != null && item.m_dropPrefab.name.GetStableHashCode() == hash && item.m_quality == quality);
                if (weapon == null && character.m_unarmedWeapon != null && character.m_unarmedWeapon.name.GetStableHashCode() == hash)
                    weapon = character.m_unarmedWeapon.m_itemData.Clone();
                if (weapon == null) throw new InvalidDataException("Attack weapon is absent from the server inventory.");
                if (weapon.m_shared.m_useDurability && weapon.m_durability <= 0f) return;
                var template = secondary ? weapon.m_shared.m_secondaryAttack : weapon.m_shared.m_attack;
                if (template == null || !weapon.HavePrimaryAttack() || (secondary && !weapon.HaveSecondaryAttack())) return;
                CombatCharacters.Preparing = true;
                try { character.EquipItem(weapon, false); }
                finally { CombatCharacters.Preparing = false; }
                var attack = template.Clone();
                attack.StartWithoutAnimation(character, character.GetComponent<Rigidbody>(),
                    character.GetComponentInChildren<VisEquipment>(), weapon, draw);
                running.Add(new Running { Character = character, Attack = attack, Request = AttackSoundScope.Current,
                    Until = Time.realtimeSinceStartup + Math.Max(5f, template.m_burstInterval * template.m_projectileBursts + 2f) });
                CombatCharacters.SaveInventory(character);
            }
        }

        internal static void Tick()
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) { running.Clear(); return; }
            foreach (var job in running.ToArray())
            {
                if (job.Character == null || job.Attack.IsDone() || Time.realtimeSinceStartup > job.Until) { running.Remove(job); continue; }
                try
                {
                    using (var sound = new AttackSoundScope(job.Request))
                    using (var scope = new CombatScope(job.Character))
                    {
                        byte[] before = CombatCharacters.InventoryBytes(job.Character);
                        job.Attack.Update(Time.deltaTime);
                        if (!before.SequenceEqual(CombatCharacters.InventoryBytes(job.Character))) CombatCharacters.SaveInventory(job.Character);
                    }
                }
                catch (Exception error) { CharacterRpc.Log.LogError(error); running.Remove(job); }
            }
        }
    }
}
