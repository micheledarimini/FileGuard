using System;
using System.IO;
using System.Threading.Tasks;

namespace FileGuard.Tests
{
    class SimpleMonitorTest
    {
        private static readonly string TestDir = "test_monitoring";
        private static FileSystemWatcher watcher;
        private static bool eventReceived;
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Test monitoraggio filesystem...\n");
            
            // Setup
            if (Directory.Exists(TestDir))
                Directory.Delete(TestDir, true);
            Directory.CreateDirectory(TestDir);

            // Configura FileSystemWatcher
            watcher = new FileSystemWatcher(TestDir)
            {
                NotifyFilter = NotifyFilters.FileName | 
                             NotifyFilters.DirectoryName |
                             NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += OnFileSystemEvent;
            watcher.Changed += OnFileSystemEvent;
            watcher.Deleted += OnFileSystemEvent;

            try
            {
                // Test creazione file
                Console.WriteLine("Test 1: Creazione file");
                eventReceived = false;
                File.WriteAllText(Path.Combine(TestDir, "test1.txt"), "Contenuto test");
                await Task.Delay(1000);
                Console.WriteLine(eventReceived ? "✓ Evento creazione rilevato" : "✗ Evento creazione non rilevato");
                
                // Test modifica file
                Console.WriteLine("\nTest 2: Modifica file");
                eventReceived = false;
                File.AppendAllText(Path.Combine(TestDir, "test1.txt"), "\nModifica test");
                await Task.Delay(1000);
                Console.WriteLine(eventReceived ? "✓ Evento modifica rilevato" : "✗ Evento modifica non rilevato");
                
                // Test eliminazione file
                Console.WriteLine("\nTest 3: Eliminazione file");
                eventReceived = false;
                File.Delete(Path.Combine(TestDir, "test1.txt"));
                await Task.Delay(1000);
                Console.WriteLine(eventReceived ? "✓ Evento eliminazione rilevato" : "✗ Evento eliminazione non rilevato");

                Console.WriteLine("\nTest completati!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nErrore durante i test: {ex.Message}");
            }
            finally
            {
                // Cleanup
                watcher.Dispose();
                if (Directory.Exists(TestDir))
                    Directory.Delete(TestDir, true);
            }

            Console.WriteLine("\nPremi un tasto per terminare...");
            Console.ReadKey();
        }

        private static void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            eventReceived = true;
            Console.WriteLine($"  Rilevato evento: {e.ChangeType} - File: {e.Name}");
        }
    }
}
