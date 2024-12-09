namespace FileGuard.Core.Cache
{
    public class StateManagerConfig
    {
        public string SettingsPath { get; }

        public StateManagerConfig(string settingsPath)
        {
            SettingsPath = settingsPath;
        }
    }
}
