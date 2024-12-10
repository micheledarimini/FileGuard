using System;
using FileGuard.Core.Logging;

namespace FileGuard.Core.Tests
{
    public static class TestRunner
    {
        private static readonly ILogger _logger = LoggerFactory.GetDefaultLogger();

        public static void RunTests()
        {
            try
            {
                Console.WriteLine("\n=== INIZIO TEST FILEGUARD ===");
                _logger.LogInfo("Inizio esecuzione test", nameof(TestRunner));

                var treeViewTests = new TreeViewModelTests();

                // Test comportamenti specifici cartelle
                Console.WriteLine("\n=== Test Comportamenti Cartelle ===");
                Console.WriteLine("1. Test chiusura manuale sottocartella");
                Console.WriteLine("2. Test mantenimento stati dopo chiusura");
                Console.WriteLine("3. Test riapertura cartelle");
                Console.WriteLine("4. Test chiusura/riapertura rapida");
                
                Console.WriteLine("\nEsecuzione test comportamenti cartelle...");
                treeViewTests.TestManualFolderCollapse();
                Console.WriteLine("✓ Test comportamenti cartelle completati con successo");

                Console.WriteLine("\n=== RIEPILOGO TEST ===");
                Console.WriteLine("✓ Comportamenti cartelle: OK");
                Console.WriteLine("\n=== TEST COMPLETATI CON SUCCESSO ===\n");
                
                _logger.LogInfo("Test completati con successo", nameof(TestRunner));
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n=== ERRORE NEI TEST ===");
                Console.WriteLine($"Errore: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                _logger.LogError("Errore durante l'esecuzione dei test", ex, nameof(TestRunner));
                Environment.Exit(1);
            }
        }
    }
}
