using System;
using System.IO;
using System.Windows;
using FileGuard.Core.Tests;
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

            if (e.Args.Length > 0)
            {
                Console.WriteLine($"Argomento ricevuto: {e.Args[0]}");
                _logger.LogInfo($"Argomento ricevuto: {e.Args[0]}", nameof(App));

                if (e.Args[0] == "--test")
                {
                    Console.WriteLine("Modalità test rilevata");
                    _logger.LogInfo("Modalità test rilevata", nameof(App));
                    RunTests();
                    Console.WriteLine("Chiusura applicazione dopo i test");
                    _logger.LogInfo("Chiusura applicazione dopo i test", nameof(App));
                    Shutdown();
                    return;
                }
            }

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

        private void RunTests()
        {
            try
            {
                Console.WriteLine("\n=== INIZIO TEST FILEGUARD ===");
                _logger.LogInfo("Inizio esecuzione test", nameof(App));

                var treeViewTests = new TreeViewModelTests();
                Console.WriteLine("\nEsecuzione test TreeViewModel...");
                treeViewTests.RunAllTests();

                Console.WriteLine("\n=== TEST COMPLETATI CON SUCCESSO ===\n");
                _logger.LogInfo("Test completati con successo", nameof(App));
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n=== ERRORE NEI TEST ===");
                Console.WriteLine($"Errore: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                _logger.LogError("Errore durante l'esecuzione dei test", ex, nameof(App));
                Environment.Exit(1);
            }
        }
    }
}
