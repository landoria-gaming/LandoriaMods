using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Landoria.ServerInventory.Network;
using Landoria.ServerInventory.Serialization;

namespace Landoria.ServerInventory.Server
{
    internal sealed class WorldActionTransaction : IDisposable
    {
        [ThreadStatic] internal static WorldActionTransaction Current;
        internal readonly Player Player;
        internal readonly ZNetPeer Peer;
        internal readonly JObject Document;
        internal readonly Inventory Inventory;
        internal readonly HashSet<GameObject> Created = new HashSet<GameObject>();
        internal readonly long PlayerId;
        internal readonly string PlayerName;
        private readonly string path;
        private bool committed;
        internal bool ReadOnly;
        private readonly Dictionary<ZDOID, ZDO> touched = new Dictionary<ZDOID, ZDO>();
        private readonly Dictionary<ZDOID, JObject> originals = new Dictionary<ZDOID, JObject>();
        private readonly HashSet<ZDOID> deleted = new HashSet<ZDOID>();
        private readonly List<Action> beforeCommit = new List<Action>();
        internal bool ApplyingDeletions;
        internal void Touch(ZNetView view)
        {
            var zdo = view.GetZDO();
            if (!touched.ContainsKey(zdo.m_uid)) originals[zdo.m_uid] = InventoryCommitLog.Snapshot(zdo);
            touched[zdo.m_uid] = zdo;
        }
        internal void Delete(ZNetView view) { Touch(view); deleted.Add(view.GetZDO().m_uid); }
        internal void BeforeCommit(Action action) => beforeCommit.Add(action);
        private readonly List<Action> completion = new List<Action>();
        internal void AfterCommit(Action action) => completion.Add(action);

        internal WorldActionTransaction(ZNetPeer peer, Player player)
        {
            Peer = peer;
            Player = player;
            path = CharacterStore.GetPath(peer);
            Document = JObject.Parse(File.ReadAllText(path));
            PlayerId = long.Parse((string)Document["profile"]["playerId"]);
            PlayerName = (string)Document["profile"]["playerName"];
            Inventory = new Inventory("Server action", null, 8, 4);
            using (var io = new BinaryJson())
            {
                CharacterSchema.Inventory(io, (JObject)Document["profile"]["playerData"]["inventory"]);
                Inventory.Load(new ZPackage(io.Finish()));
            }
            Current = this;
        }

        internal ZPackage Commit()
        {
            var package = new ZPackage();
            Inventory.Save(package);
            var data = new JObject();
            using (var io = new BinaryJson(package.GetArray())) { CharacterSchema.Inventory(io, data); io.Finish(); }
            Document["profile"]["playerData"]["inventory"] = data;
            if (ReadOnly) committed = true;
            else Persist();
            try { foreach (var complete in completion) complete(); }
            catch (Exception error) { InventoryCommitLog.Healthy = false; CharacterRpc.Log.LogError(error); throw; }
            Current = null;
            try { InventorySnapshot.Apply(Player, new ZPackage(package.GetArray())); }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
            finally { Current = this; }
            return package;
        }

        private void Persist()
        {
            foreach (var prepare in beforeCommit) prepare();
            var objects = new JArray();
            foreach (var instance in Created)
            {
                var view = instance == null ? null : instance.GetComponent<ZNetView>();
                if (view != null && view.IsValid() && view.GetZDO().Persistent) Touch(view);
            }
            foreach (var pair in touched) objects.Add(InventoryCommitLog.Snapshot(pair.Value, deleted.Contains(pair.Key)));
            string journal = InventoryCommitLog.WriteIntent(path, Document, objects);
            committed = true;
            try
            {
                CharacterStore.SaveExisting(path, Document);
                InventoryCommitLog.Complete(journal);
                ApplyDeletions();
            }
            catch (Exception error) { InventoryCommitLog.Healthy = false; CharacterRpc.Log.LogError(error); throw; }
        }

        private void ApplyDeletions()
        {
            ApplyingDeletions = true;
            try
            {
                foreach (var id in deleted)
                {
                    var instance = ZNetScene.instance.FindInstance(id);
                    if (instance != null) ZNetScene.instance.Destroy(instance);
                    else if (touched.TryGetValue(id, out var zdo)) ZDOMan.instance.DestroyZDO(zdo);
                }
            }
            finally { ApplyingDeletions = false; }
        }

        public void Dispose()
        {
            Current = null;
            if (committed) return;
            ApplyingDeletions = true;
            foreach (var state in originals.Values)
            {
                try { InventoryCommitLog.Restore(state); }
                catch (Exception error) { InventoryCommitLog.Healthy = false; CharacterRpc.Log.LogError(error); }
            }
            foreach (var instance in Created)
            {
                if (instance == null) continue;
                try { ZNetScene.instance.Destroy(instance); }
                catch (Exception error) { CharacterRpc.Log.LogError(error); }
            }
        }
    }

    [HarmonyPatch(typeof(ZNetView), "Awake")]
    internal static class WorldActionCreationCapture
    {
        private static void Postfix(ZNetView __instance)
        {
            if (WorldActionTransaction.Current != null && __instance.IsValid())
                WorldActionTransaction.Current.Created.Add(__instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Destroy))]
    internal static class WorldActionDeletionCapture
    {
        private static bool Prefix(GameObject go)
        {
            var action = WorldActionTransaction.Current;
            var view = go == null ? null : go.GetComponent<ZNetView>();
            if (action == null || action.ApplyingDeletions || view == null || !view.IsValid()) return true;
            action.Delete(view);
            return false;
        }
    }
}
