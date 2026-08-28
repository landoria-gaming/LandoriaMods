namespace Landoria.CharacterVault
{
    public static class GracefulShutdownApi
    {
        public static bool TryRequest() =>
            CharacterVaultPlugin.Coordinator?.TryRequestShutdown() == true;
    }
}
