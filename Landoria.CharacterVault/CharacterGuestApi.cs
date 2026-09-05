using System;

namespace Landoria.CharacterVault
{
    public interface ICharacterGuestProvider
    {
        bool IsGuest(ZRpc rpc);
    }

    public static class CharacterGuestApi
    {
        private static readonly object Sync = new object();
        private static ICharacterGuestProvider _provider;

        public static void Register(ICharacterGuestProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            lock (Sync)
            {
                if (_provider != null && !ReferenceEquals(_provider, provider))
                    throw new InvalidOperationException("A character guest provider is already registered.");
                _provider = provider;
            }
        }

        public static bool Unregister(ICharacterGuestProvider provider)
        {
            lock (Sync)
            {
                if (!ReferenceEquals(_provider, provider)) return false;
                _provider = null;
                return true;
            }
        }

        internal static bool IsGuest(ZRpc rpc)
        {
            if (rpc == null) return false;
            ICharacterGuestProvider provider;
            lock (Sync) provider = _provider;
            return provider?.IsGuest(rpc) == true;
        }
    }
}
