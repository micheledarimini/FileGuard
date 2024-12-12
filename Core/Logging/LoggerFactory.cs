using System;
using System.IO;
using System.Reflection;

namespace FileGuard.Core.Logging
{
    /// <summary>
    /// Factory per la creazione di istanze del logger
    /// </summary>
    public static class LoggerFactory
    {
        private static ILogger? defaultLogger;
        private static readonly object lockObj = new();
        private static LoggerConfiguration? configuration;

        /// <summary>
        /// Crea o restituisce l'istanza predefinita del logger
        /// </summary>
        public static ILogger GetDefaultLogger()
        {
            if (defaultLogger == null)
            {
                lock (lockObj)
                {
                    if (defaultLogger == null)
                    {
                        var config = GetConfiguration();
                        defaultLogger = new FileLogger(config.GetCurrentLogPath());
                    }
                }
            }
            return defaultLogger;
        }

        /// <summary>
        /// Crea una nuova istanza del logger con il percorso specificato
        /// </summary>
        public static ILogger CreateLogger(string logPath)
        {
            return new FileLogger(logPath);
        }

        /// <summary>
        /// Imposta il logger predefinito
        /// </summary>
        public static void SetDefaultLogger(ILogger logger)
        {
            defaultLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ottiene la configurazione corrente del logger
        /// </summary>
        public static LoggerConfiguration GetConfiguration()
        {
            if (configuration == null)
            {
                lock (lockObj)
                {
                    if (configuration == null)
                    {
                        configuration = LoadConfiguration();
                    }
                }
            }
            return configuration;
        }

        /// <summary>
        /// Carica la configurazione dal file di progetto
        /// </summary>
        private static LoggerConfiguration LoadConfiguration()
        {
            var config = new LoggerConfiguration();

            try
            {
                // Determina se siamo in Debug o Release
                #if DEBUG
                    config.MinimumLevel = LogLevel.Debug;
                    config.FilePattern = "debug_{0:yyyy-MM-dd}.log";
                #else
                    config.MinimumLevel = LogLevel.Info;
                    config.FilePattern = "release_{0:yyyy-MM-dd}.log";
                #endif

                // Imposta la directory dei log
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                config.LogDirectory = Path.Combine(appDataPath, "FileGuard", "Logs");

                // Crea la directory se non esiste
                if (!Directory.Exists(config.LogDirectory))
                {
                    Directory.CreateDirectory(config.LogDirectory);
                }

                // Imposta i limiti predefiniti
                config.MaxFileSize = 10 * 1024 * 1024; // 10MB
                config.MaxFiles = 5;

                return config;
            }
            catch (Exception)
            {
                // In caso di errore, ritorna una configurazione predefinita
                return LoggerConfiguration.CreateDefault();
            }
        }

        /// <summary>
        /// Percorso predefinito per retrocompatibilità
        /// </summary>
        private static string GetDefaultLogPath()
        {
            var config = GetConfiguration();
            return config.GetCurrentLogPath();
        }
    }
}
