using System;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch]
    internal static class InventoryCommitLog
    {
        internal static bool Healthy = true;
        private static string folder;
        private static string[] checkpoint = Array.Empty<string>();
        private static long sequence;

        internal static void Initialize()
        {
            Healthy = false;
            folder = Path.Combine(SaveSystem.GetCharacterFolderPath(FileHelpers.FileSource.Local),
                "transactions", ZNet.instance.GetWorldUID().ToString());
            Directory.CreateDirectory(folder);
            foreach (var path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.Ordinal)) Recover(path);
            sequence = DateTime.UtcNow.Ticks;
            Healthy = true;
        }

        internal static string Prepare(string characterPath, JObject character, JArray objects)
        {
            if (!Healthy || folder == null) throw new InvalidOperationException("Inventory recovery is required.");
            string id = (++sequence).ToString("D19") + "-" + Guid.NewGuid().ToString("N");
            character["lastInventoryTransaction"] = id;
            string path = Path.Combine(folder, id + ".json");
            Write(path, new JObject { ["character"] = Path.GetFileName(characterPath),
                ["data"] = character.DeepClone(), ["objects"] = objects, ["committed"] = false });
            return path;
        }

        internal static void Complete(string path)
        {
            var record = JObject.Parse(File.ReadAllText(path));
            record["committed"] = true;
            Write(path, record);
        }

        private static void Recover(string path)
        {
            var record = JObject.Parse(File.ReadAllText(path));
            if ((bool?)record["committed"] != true)
            {
                string name = (string)record["character"];
                if (Path.GetFileName(name) != name) throw new InvalidDataException("Invalid recovery character path.");
                var data = (JObject)record["data"];
                CharacterStore.SaveExisting(Path.Combine(SaveSystem.GetCharacterFolderPath(FileHelpers.FileSource.Local), name), data);
                record["committed"] = true; Write(path, record);
            }
            uint saved = ZNet.instance.GetWorld().SaveNumber();
            if (record["checkpoint"] != null && (uint)record["checkpoint"] <= saved)
            { File.Delete(path); return; }
            foreach (JObject state in (JArray)record["objects"]) Restore(state);
        }

        internal static JObject Snapshot(ZDO zdo, bool deleted = false)
        {
            var package = new ZPackage(); zdo.Serialize(package);
            var pos = zdo.GetPosition();
            return new JObject { ["user"] = zdo.m_uid.UserID.ToString(), ["object"] = zdo.m_uid.ID,
                ["deleted"] = deleted, ["x"] = pos.x, ["y"] = pos.y, ["z"] = pos.z,
                ["payload"] = Convert.ToBase64String(package.GetArray()) };
        }

        internal static void Restore(JObject state)
        {
            var id = new ZDOID(long.Parse((string)state["user"]), (uint)state["object"]);
            var zdo = ZDOMan.instance.GetZDO(id);
            if ((bool)state["deleted"])
            {
                if (zdo != null) Remove(ZDOMan.instance, id);
                return;
            }
            var pos = new Vector3((float)state["x"], (float)state["y"], (float)state["z"]);
            if (zdo == null) zdo = Create(ZDOMan.instance, id, pos, 0);
            ZDOExtraData.Release(zdo, id);
            zdo.SetRotation(Quaternion.identity);
            zdo.Deserialize(new ZPackage(Convert.FromBase64String((string)state["payload"])));
            zdo.SetPosition(pos); zdo.SetOwner(ZNet.GetUID());
            zdo.DataRevision++;
        }

        private static void Write(string path, JObject data)
        {
            string temporary = path + ".tmp";
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(data.ToString());
                file.Write(bytes, 0, bytes.Length); file.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        internal static void PrepareCheckpoint()
        {
            if (folder != null) checkpoint = Directory.GetFiles(folder, "*.json");
        }
        internal static void MarkCheckpoint()
        {
            foreach (var path in checkpoint)
            {
                var data = JObject.Parse(File.ReadAllText(path));
                data["checkpoint"] = SaveSystem.GetSaveNumber();
                Write(path, data);
            }
        }

        [HarmonyReversePatch, HarmonyPatch(typeof(ZDOMan), "CreateNewZDO", typeof(ZDOID), typeof(Vector3), typeof(int))]
        private static ZDO Create(ZDOMan instance, ZDOID id, Vector3 position, int prefab) => throw new NotImplementedException();
        [HarmonyReversePatch, HarmonyPatch(typeof(ZDOMan), "HandleDestroyedZDO")]
        private static void Remove(ZDOMan instance, ZDOID id) => throw new NotImplementedException();
    }

    [HarmonyPatch(typeof(ZNet), "ServerLoadWorld")]
    internal static class InventoryRecovery
    {
        private static void Postfix()
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) return;
            try { InventoryCommitLog.Initialize(); }
            catch (Exception error) { InventoryCommitLog.Healthy = false; CharacterRpc.Log.LogError(error); }
        }
    }
    [HarmonyPatch(typeof(ZDOMan), "PrepareSave")]
    internal static class InventoryCheckpointCapture
    {
        private static void Prefix()
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) return;
            InventoryCommitLog.PrepareCheckpoint();
        }
    }
    [HarmonyPatch(typeof(SaveSystem), nameof(SaveSystem.BeginSave))]
    internal static class InventoryCheckpointWrite
    {
        private static void Postfix()
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) return;
            InventoryCommitLog.MarkCheckpoint();
        }
    }
}
