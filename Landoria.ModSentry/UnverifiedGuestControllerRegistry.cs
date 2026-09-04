using System;

namespace Landoria.ModSentry
{
    internal static class UnverifiedGuestControllerRegistry
    {
        internal const int ProtocolVersion = 1;
        private static IUnverifiedGuestController _controller;

        internal static bool IsRegistered => _controller != null;

        internal static bool IsReady
        {
            get
            {
                try
                {
                    return _controller?.IsReady == true;
                }
                catch (Exception exception)
                {
                    ModSentryPlugin.Log.LogWarning(
                        "Could not read the unverified guest controller state: " + exception);
                    return false;
                }
            }
        }

        internal static void Register(IUnverifiedGuestController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }
            if (controller.ProtocolVersion != ProtocolVersion)
            {
                throw new InvalidOperationException(
                    "The unverified guest controller protocol is incompatible.");
            }
            if (_controller != null && !ReferenceEquals(_controller, controller))
            {
                throw new InvalidOperationException(
                    "An unverified guest controller is already registered.");
            }
            _controller = controller;
        }

        internal static void Unregister(IUnverifiedGuestController controller)
        {
            if (ReferenceEquals(_controller, controller))
            {
                _controller = null;
            }
        }

        internal static bool NotifyAdmitted(ZRpc rpc, out string failure)
        {
            failure = null;
            try
            {
                _controller.OnGuestAdmitted(rpc);
                return true;
            }
            catch (Exception exception)
            {
                ModSentryPlugin.Log.LogWarning(
                    "Could not notify the unverified guest controller: " + exception);
                failure = $"{exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        internal static void NotifyDisconnected(ZRpc rpc)
        {
            _controller?.OnGuestDisconnected(rpc);
        }

        internal static void Clear()
        {
            _controller?.ClearGuests();
            _controller = null;
        }
    }
}
