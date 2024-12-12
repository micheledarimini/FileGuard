using System;
using System.IO;
using System.Windows;
using FileGuard.Core.Logging;

namespace FileGuard.Core.UI
{
    public partial class App : Application
    {
        private readonly ILogger _logger;

        public App()
        {
            _logger = LoggerFactory.GetDefaultLogger();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Console.WriteLine("Avvio applicazione...");
            _logger.LogInfo("Avvio applicazione", nameof(App));

            // Inizializza il logger nella directory dell'eseguibile
            var exePath = AppDomain.CurrentDomain.BaseDirectory;
            Logger.Initialize(exePath);

            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore durante l'avvio dell'applicazione", ex, nameof(App));
                Console.WriteLine($"Errore durante l'avvio: {ex.Message}");
                Shutdown(1);
            }

            // Gestisci chiusura applicazione
            Current.Exit += (s, args) => Logger.Shutdown();
        }
    }
}
