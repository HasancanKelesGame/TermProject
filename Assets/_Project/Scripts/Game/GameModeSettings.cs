namespace TermProject.Game
{
    public static class GameModeSettings
    {
        public static bool HasSelection { get; private set; }
        public static bool CrazyModeEnabled { get; private set; }

        public static void SetCrazyMode(bool enabled)
        {
            CrazyModeEnabled = enabled;
            HasSelection = true;
        }

        public static void ClearSelection()
        {
            CrazyModeEnabled = false;
            HasSelection = false;
        }
    }
}
