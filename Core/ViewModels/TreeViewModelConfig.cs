namespace FileGuard.Core.ViewModels
{
    public class TreeViewModelConfig
    {
        /// <summary>
        /// Percorso del file di configurazione per il salvataggio dello stato
        /// </summary>
        public string SettingsPath { get; }

        /// <summary>
        /// Percorso del file per il salvataggio dello stato dell'albero
        /// </summary>
        public string StatePath { get; }

        /// <summary>
        /// Percorso predefinito per il monitoraggio dei file
        /// </summary>
        public string DefaultMonitorPath { get; }

        /// <summary>
        /// Numero massimo di eventi di cambiamento da mantenere nella cronologia
        /// </summary>
        public int MaxChangeHistoryItems { get; }

        /// <summary>
        /// Indica se caricare automaticamente le cartelle monitorate all'avvio
        /// </summary>
        public bool AutoLoadMonitoredFolders { get; }

        public TreeViewModelConfig(
            string settingsPath,
            string statePath,
            string defaultMonitorPath,
            int maxChangeHistoryItems = 100,
            bool autoLoadMonitoredFolders = true)
        {
            SettingsPath = settingsPath;
            StatePath = statePath;
            DefaultMonitorPath = defaultMonitorPath;
            MaxChangeHistoryItems = maxChangeHistoryItems;
            AutoLoadMonitoredFolders = autoLoadMonitoredFolders;
        }
    }
}
