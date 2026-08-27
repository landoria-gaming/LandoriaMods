using Landoria.SharedLib;

namespace Landoria.HammerFreedom
{
    internal sealed class HammerFreedomSettings
    {
        private const string FlightArgument = "--hammerfreedom-fly";
        private const string FallDamageArgument = "--hammerfreedom-fall-damage-immunity";
        private const string StaminaArgument = "--hammerfreedom-unlimited-stamina";
        private const string DurabilityArgument = "--hammerfreedom-no-durability-loss";

        internal bool Flight { get; private set; }
        internal bool FallDamageImmunity { get; private set; }
        internal bool UnlimitedStamina { get; private set; }
        internal bool NoDurabilityLoss { get; private set; }

        internal static HammerFreedomSettings FromArguments(string[] arguments, ModLog logger)
        {
            return new HammerFreedomSettings
            {
                Flight = Read(arguments, FlightArgument, logger),
                FallDamageImmunity = Read(arguments, FallDamageArgument, logger),
                UnlimitedStamina = Read(arguments, StaminaArgument, logger),
                NoDurabilityLoss = Read(arguments, DurabilityArgument, logger)
            };
        }

        private static bool Read(string[] arguments, string name, ModLog logger)
        {
            bool enabled = HammerFreedomArgumentPolicy.Resolve(arguments, name, out bool valid);
            if (!valid)
            {
                logger.LogWarning($"Invalid {name} value; using false.");
            }
            return enabled;
        }
    }
}
