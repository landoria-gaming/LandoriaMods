using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuestLobbyExample
{
    /// <summary>Protects the cube, brazier, rug, sign, and build boundary.</summary>
    internal static class GuestLobbyProtection
    {
        internal const string Marker = "example.guest_lobby.protected";
        private const string NoBuildMessage = "$msg_nobuildzone";
        private const float SignCheckInterval = 2f;
        private const float HalfWidth = 5.5f;
        private const float BelowSpawn = 2f;
        private const float AboveSpawn = 6f;
        private static readonly int SignKey =
            GuestLobbyUtility.StableHash("example_guest_lobby_sign");
        private static readonly int SignTextKey =
            GuestLobbyUtility.StableHash("example_guest_lobby_sign_text");
        private static float _nextSignCheckAt;

        /// <summary>Marks a network view as protected and applies protection.</summary>
        internal static void MarkAndApply(ZNetView view)
        {
            ZDO zdo = view?.GetZDO();
            zdo?.Set(Marker, true);
            if (zdo != null && ZNet.instance?.IsServer() == true)
            {
                zdo.SetOwner(ZDOMan.GetSessionID());
            }
            Apply(view);
        }

        /// <summary>Applies protection to a marked network view.</summary>
        internal static void Apply(ZNetView view)
        {
            if (!IsProtected(view) || ZNet.instance?.IsServer() != true)
            {
                return;
            }
            HitData.DamageModifiers immune = ImmuneDamage();
            foreach (WearNTear item in
                     view.GetComponentsInChildren<WearNTear>())
            {
                ProtectWear(item, immune);
            }
            foreach (Fireplace fireplace in
                     view.GetComponentsInChildren<Fireplace>())
            {
                ProtectFireplace(fireplace, view);
            }
            foreach (ItemDrop item in view.GetComponentsInChildren<ItemDrop>())
            {
                ProtectItem(item, view);
            }
        }

        /// <summary>Marks a sign for automatic text restoration.</summary>
        internal static void MarkSign(ZNetView view, string text)
        {
            ZDO zdo = view?.GetZDO();
            if (zdo == null)
            {
                return;
            }
            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.Set(SignKey, 1);
            zdo.Set(SignTextKey, text);
            RestoreSign(zdo);
        }

        /// <summary>Restores modified protected signs when guests are present.</summary>
        internal static void TickSign()
        {
            if (ZNet.instance?.IsServer() != true ||
                ZDOMan.instance == null ||
                !GuestLobbyController.HasGuestInside() ||
                Time.unscaledTime < _nextSignCheckAt)
            {
                return;
            }
            _nextSignCheckAt = Time.unscaledTime + SignCheckInterval;
            List<ZDOID> ids = ZDOExtraData.GetAllZDOIDsWithHash(
                ZDOExtraData.Type.Int, SignKey);
            foreach (ZDOID id in ids)
            {
                ZDO zdo = ZDOMan.instance.GetZDO(id);
                if (IsSignModified(zdo))
                {
                    RestoreSign(zdo);
                }
            }
        }

        /// <summary>Removes new pieces built by a guest inside the lobby.</summary>
        internal static void RemoveNewGuestPieces(ZRpc rpc,
            IEnumerable<ZDOID> newIds)
        {
            if (!GuestLobbyController.IsAdmitted(rpc) || newIds == null ||
                ZDOMan.instance == null || ZNetScene.instance == null ||
                !GuestLobbyGenerator.TryGetPosition(out Vector3 lobby))
            {
                return;
            }
            bool removed = false;
            foreach (ZDOID id in newIds)
            {
                removed |= RemoveIfBuiltInside(id, lobby);
            }
            if (removed)
            {
                ShowNoBuildMessage(GuestLobbyController.FindPeer(rpc));
            }
        }

        /// <summary>Checks whether a component belongs to a protected view.</summary>
        internal static bool IsProtected(Component component)
        {
            ZNetView view = component?.GetComponent<ZNetView>() ??
                component?.GetComponentInParent<ZNetView>();
            return IsProtected(view);
        }

        /// <summary>Checks whether a network view is protected.</summary>
        internal static bool IsProtected(ZNetView view)
        {
            return view?.GetZDO()?.GetBool(Marker) == true;
        }

        /// <summary>Checks whether world-object data is protected.</summary>
        internal static bool IsProtected(ZDO zdo)
        {
            return zdo?.GetBool(Marker) == true;
        }

        /// <summary>Checks whether a position is inside the lobby boundary.</summary>
        internal static bool IsInsideLobby(Vector3 position, Vector3 lobby)
        {
            return Mathf.Abs(position.x - lobby.x) <= HalfWidth &&
                Mathf.Abs(position.z - lobby.z) <= HalfWidth &&
                position.y >= lobby.y - BelowSpawn &&
                position.y <= lobby.y + AboveSpawn;
        }

        /// <summary>Finds new world-object identifiers reported by a guest.</summary>
        internal static List<ZDOID> FindNewIds(ZRpc rpc, ZPackage package)
        {
            List<ZDOID> ids = new List<ZDOID>();
            if (!GuestLobbyController.IsAdmitted(rpc))
            {
                return ids;
            }
            try
            {
                ReadZdoRecords(new ZPackage(package.GetArray()), ids);
            }
            catch (Exception exception)
            {
                ModSentryPlugin.Log.LogWarning(
                    "Could not read guest lobby ZDO records: " + exception);
                ids.Clear();
            }
            return ids;
        }

        /// <summary>Allows fireplace changes only outside protected objects.</summary>
        internal static bool AllowFireplaceMutation(Fireplace fireplace)
        {
            ZNetView view = fireplace?.GetComponent<ZNetView>();
            return ZNet.instance?.IsServer() != true || !IsProtected(view);
        }

        /// <summary>Allows ownership requests only outside protected objects.</summary>
        internal static bool AllowItemOwnershipRequest(ItemDrop item)
        {
            ZNetView view = item?.GetComponent<ZNetView>();
            return ZNet.instance?.IsServer() != true || !IsProtected(view);
        }

        private static void ProtectWear(WearNTear item,
            HitData.DamageModifiers immune)
        {
            item.m_damages = immune;
            item.m_noSupportWear = false;
            item.m_noRoofWear = false;
            item.m_burnable = false;
            item.m_ashDamageImmune = true;
            item.GetComponent<ZNetView>()?.GetZDO()?.Set(
                ZDOVars.s_health, item.m_health);
        }

        private static void ProtectFireplace(Fireplace fireplace,
            ZNetView view)
        {
            ZDO zdo = view?.GetZDO();
            if (fireplace == null || zdo == null)
            {
                return;
            }
            fireplace.m_infiniteFuel = true;
            fireplace.m_canRefill = false;
            fireplace.m_canTurnOff = false;
            zdo.Set(ZDOVars.s_fuel, fireplace.m_maxFuel);
            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.Set(ZDOVars.s_state, 1);
            ZDOMan.instance?.ForceSendZDO(zdo.m_uid);
        }

        private static void ProtectItem(ItemDrop item, ZNetView view)
        {
            if (item == null || view?.GetZDO() == null)
            {
                return;
            }
            item.m_autoPickup = false;
            item.m_autoDestroy = false;
            view.GetZDO().Persistent = true;
            Rigidbody body = item.GetComponent<Rigidbody>();
            if (body == null)
            {
                return;
            }
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        private static bool RemoveIfBuiltInside(ZDOID id, Vector3 lobby)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || !zdo.IsValid() || IsProtected(zdo) ||
                !IsInsideLobby(zdo.GetPosition(), lobby))
            {
                return false;
            }
            GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
            if (prefab?.GetComponent<Piece>() == null)
            {
                return false;
            }
            zdo.SetOwner(ZDOMan.GetSessionID());
            ZDOMan.instance.DestroyZDO(zdo);
            return true;
        }

        private static void ShowNoBuildMessage(ZNetPeer peer)
        {
            if (peer == null || ZRoutedRpc.instance == null)
            {
                return;
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ShowMessage",
                (int)MessageHud.MessageType.Center, NoBuildMessage);
        }

        private static bool IsSignModified(ZDO zdo)
        {
            return zdo != null && zdo.IsValid() &&
                (zdo.GetString(ZDOVars.s_text) !=
                    zdo.GetString(SignTextKey) ||
                 !string.IsNullOrEmpty(zdo.GetString(ZDOVars.s_author)) ||
                 !string.IsNullOrEmpty(
                     zdo.GetString(ZDOVars.s_authorDisplayName)));
        }

        private static void ReadZdoRecords(ZPackage package,
            ICollection<ZDOID> ids)
        {
            int invalidations = package.ReadInt();
            for (int index = 0; index < invalidations; index++)
            {
                package.ReadZDOID();
            }
            ZPackage data = new ZPackage();
            while (ReadZdoRecord(package, data, ids))
            {
            }
        }

        private static bool ReadZdoRecord(ZPackage package, ZPackage data,
            ICollection<ZDOID> ids)
        {
            ZDOID id = package.ReadZDOID();
            if (id.IsNone())
            {
                return false;
            }
            package.ReadUShort();
            package.ReadUInt();
            package.ReadLong();
            package.ReadVector3();
            package.ReadPackage(ref data);
            if (ZDOMan.instance?.GetZDO(id) == null)
            {
                ids.Add(id);
            }
            return true;
        }

        private static void RestoreSign(ZDO zdo)
        {
            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.Set(ZDOVars.s_text, zdo.GetString(SignTextKey));
            zdo.Set(ZDOVars.s_author, "");
            zdo.Set(ZDOVars.s_authorDisplayName, "");
            ZDOMan.instance?.ForceSendZDO(zdo.m_uid);
        }

        private static HitData.DamageModifiers ImmuneDamage()
        {
            HitData.DamageModifier immune = HitData.DamageModifier.Immune;
            return new HitData.DamageModifiers
            {
                m_blunt = immune, m_slash = immune, m_pierce = immune,
                m_chop = immune, m_pickaxe = immune, m_fire = immune,
                m_frost = immune, m_lightning = immune, m_poison = immune,
                m_spirit = immune
            };
        }
    }
}
