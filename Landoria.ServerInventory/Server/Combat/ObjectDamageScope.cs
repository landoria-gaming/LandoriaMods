using System;
using UnityEngine;

namespace Landoria.ServerInventory.Server
{
    internal sealed class ObjectDamageScope : IDisposable
    {
        [ThreadStatic] private static ZNetView current;
        private readonly ZNetView previous;
        private readonly ZNetView view;
        internal static bool Dedicated => ZNet.instance != null && ZNet.instance.IsDedicated();

        internal ObjectDamageScope(ZNetView target)
        {
            previous = current;
            view = target;
            // Native Damage routes to the actual owner, so claim before calling it.
            view.GetZDO().SetOwner(ZNet.GetUID());
            current = view;
        }

        internal static bool Begin(Component target, out ObjectDamageScope scope)
        {
            scope = null;
            var view = target.GetComponent<ZNetView>();
            if (view == null || !view.IsValid()) return false;
            scope = new ObjectDamageScope(view);
            return true;
        }

        internal static bool AcceptRpc(Component target, long sender)
            => !Dedicated || (sender == ZNet.GetUID() && current != null &&
                target.GetComponent<ZNetView>() == current);

        public void Dispose()
        {
            current = previous;
            if (view != null && view.IsValid())
                ZDOMan.instance.ForceSendZDO(view.GetZDO().m_uid);
        }
    }
}
