namespace Landoria.FreeFly
{
    // Hides the interface during free fly and restores its previous state.
    internal static class FreeFlyInterfaceController
    {
        private static bool wasFreeFlyActive;
        private static bool previousHiddenState;
        private static bool stateCaptured;

        // Updates interface visibility for the current mode.
        internal static void Update()
        {
            bool active = GameCamera.InFreeFly();
            if (active)
            {
                CaptureState();
                if (Hud.instance)
                {
                    Hud.instance.m_userHidden = true;
                }
            }
            else if (wasFreeFlyActive)
            {
                Restore();
            }

            wasFreeFlyActive = active;
        }

        // Saves the current interface state once.
        private static void CaptureState()
        {
            if (stateCaptured || !Hud.instance)
            {
                return;
            }

            previousHiddenState = Hud.IsUserHidden();
            stateCaptured = true;
        }

        // Restores the saved interface state.
        internal static void Restore()
        {
            if (stateCaptured && Hud.instance)
            {
                Hud.instance.m_userHidden = previousHiddenState;
            }

            stateCaptured = false;
            wasFreeFlyActive = false;
        }
    }
}
