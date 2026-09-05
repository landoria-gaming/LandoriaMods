extern alias CharacterVaultApi;

using ICharacterGuestProvider = CharacterVaultApi::Landoria.CharacterVault.ICharacterGuestProvider;

namespace Landoria.ModSentry
{
    internal sealed class CharacterGuestProvider : ICharacterGuestProvider
    {
        public bool IsGuest(ZRpc rpc) => GuestAdmissions.IsGuest(rpc);
    }
}
