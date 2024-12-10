using System;
using System.IO;

namespace FileGuard.Core.Logging
{
    /// <summary>
    /// Factory per la creazione di istanze del logger
    /// </summary>
    public static class LoggerFactory
    {
        private static ILogger? defaultLogger;
        private static readonly object lockObj = new();

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
                        var logPath = GetDefaultLogPath();
                        defaultLogger = new FileLogger(logPath);
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

        private static string GetDefaultLogPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDirectory = Path.Combine(appDataPath, "FileGuard", "Logs");
            return Path.Combine(logDirectory, "fileguard_debug.log");
        }
    }
}
