using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(ZDOMan), "RPC_ZDOData")]
    internal static class ClientObjectFilter
    {
        private static bool Prefix(ZDOMan __instance, ZRpc rpc, ZPackage pkg)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) return true;
            var peer = ZNet.instance.GetPeers().FirstOrDefault(value => value.m_rpc == rpc && value.IsReady());
            if (peer == null) return false;
            try
            {
                if (pkg.Size() > 2097152) throw new InvalidDataException("Oversized world update.");
                var input = new ZPackage(pkg.GetArray());
                input.SetPos(pkg.GetPos());
                var output = new ZPackage();
                int invalidations = input.ReadInt();
                if (invalidations < 0 || invalidations > 100000) throw new InvalidDataException("Invalid world update header.");
                // The server maintains sector membership from accepted object updates.
                for (int index = 0; index < invalidations; index++) input.ReadZDOID();
                output.Write(0);
                while (CopyExisting(__instance, peer, input, output)) { }
                if (input.GetPos() != input.Size()) throw new InvalidDataException("Trailing world update bytes.");
                output.Write(ZDOID.None);
                pkg.Load(output.GetArray());
                return true;
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); return false; }
        }

        private static bool Finite(UnityEngine.Vector3 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x) && !float.IsNaN(value.y) &&
                !float.IsInfinity(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private static bool CopyExisting(ZDOMan manager, ZNetPeer peer, ZPackage input, ZPackage output)
        {
            var id = input.ReadZDOID();
            if (id.IsNone()) return false;
            input.ReadUShort();
            input.ReadUInt();
            input.ReadLong();
            var position = input.ReadVector3();
            var data = input.ReadPackage();
            var existing = manager.GetZDO(id);
            // Never create an unknown ID or accept a prefab/ownership change from the transport peer.
            if (existing == null || existing.GetOwner() != peer.m_uid) return true;
            ushort flags = data.ReadUShort();
            if (data.ReadInt() != existing.GetPrefab()) return true;
            var rotation = (flags & 0x1000) != 0 ? data.ReadVector3() : UnityEngine.Vector3.zero;
            var prefab = ZNetScene.instance.GetPrefab(existing.GetPrefab());
            // Only character transforms remain client simulated. Never ingest client gameplay fields.
            if (prefab == null || prefab.GetComponent<Character>() == null) return true;
            if (!Finite(position) || !Finite(rotation)) return true;
            var safe = WithRotation(existing, rotation);
            output.Write(id);
            output.Write(existing.OwnerRevision);
            output.Write(existing.DataRevision + 1);
            output.Write(existing.GetOwner());
            output.Write(position);
            output.Write(safe);
            return true;
        }
        private static ZPackage WithRotation(ZDO existing, UnityEngine.Vector3 rotation)
        {
            var canonical = new ZPackage();
            existing.Serialize(canonical);
            var canonicalBytes = canonical.GetArray();
            var header = new ZPackage(canonicalBytes);
            ushort savedFlags = header.ReadUShort();
            header.ReadInt();
            if ((savedFlags & 0x1000) != 0) header.ReadVector3();
            var safe = new ZPackage();
            safe.Write((ushort)(savedFlags | 0x1000));
            safe.Write(existing.GetPrefab());
            safe.Write(rotation);
            for (int i = header.GetPos(); i < canonicalBytes.Length; i++) safe.Write(canonicalBytes[i]);
            return safe;
        }
    }
}
