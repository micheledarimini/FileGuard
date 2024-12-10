using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace FileGuard.Core.Logging
{
    /// <summary>
    /// Test accesso concorrente al logger
    /// </summary>
    public class ConcurrentLoggerTest
    {
        private readonly string testDir;
        private readonly string logPath;
        private ILogger? logger;
        private const int NumThreads = 10;
        private const int MessagesPerThread = 100;

        public ConcurrentLoggerTest()
        {
            testDir = Path.Combine(Path.GetTempPath(), "LoggerTest_" + DateTime.Now.Ticks);
            logPath = Path.Combine(testDir, "test.log");
            Directory.CreateDirectory(testDir);
        }

        public async Task RunTest()
        {
            try
            {
                // Setup
                logger = new FileLogger(logPath);
                logger.LogInfo("Inizio test accesso concorrente", "ConcurrentTest");

                // Crea task per simulare accessi concorrenti
                var tasks = new List<Task>();
                for (int threadId = 0; threadId < NumThreads; threadId++)
                {
                    var tid = threadId; // Cattura per lambda
                    tasks.Add(Task.Run(async () =>
                    {
                        for (int msgId = 0; msgId < MessagesPerThread; msgId++)
                        {
                            logger.LogDebug(
                                $"Thread {tid} - Messaggio {msgId}",
                                $"Thread{tid}"
                            );
                            await Task.Delay(Random.Shared.Next(1, 10)); // Simula carico random
                        }
                    }));
                }

                // Attendi completamento di tutti i task
                await Task.WhenAll(tasks);
                logger.LogInfo("Test accesso concorrente completato", "ConcurrentTest");

                // Verifica risultati
                await VerifyResults();
            }
            finally
            {
                Cleanup();
            }
        }

        private async Task VerifyResults()
        {
            // Attendi un momento per assicurarsi che tutti i messaggi siano scritti
            await Task.Delay(1000);

            // Leggi il file di log
            var logContent = await File.ReadAllLinesAsync(logPath);

            // Verifica numero totale messaggi (2 info + debug messages)
            var expectedTotal = 2 + (NumThreads * MessagesPerThread);
            if (logContent.Length != expectedTotal)
            {
                throw new Exception($"Numero messaggi non corretto. Atteso: {expectedTotal}, Trovato: {logContent.Length}");
            }

            // Verifica presenza messaggi da ogni thread
            for (int threadId = 0; threadId < NumThreads; threadId++)
            {
                var threadMessages = logContent.Count(line => 
                    line.Contains($"Thread {threadId} - Messaggio"));
                
                if (threadMessages != MessagesPerThread)
                {
                    throw new Exception(
                        $"Messaggi mancanti per Thread {threadId}. " +
                        $"Attesi: {MessagesPerThread}, Trovati: {threadMessages}"
                    );
                }
            }

            // Verifica ordine temporale
            DateTime? lastTimestamp = null;
            foreach (var line in logContent)
            {
                if (DateTime.TryParse(line.Split(' ')[0], out DateTime timestamp))
                {
                    if (lastTimestamp.HasValue && timestamp < lastTimestamp.Value)
                    {
                        throw new Exception("Rilevato timestamp non ordinato");
                    }
                    lastTimestamp = timestamp;
                }
            }

            Console.WriteLine("Verifica completata con successo:");
            Console.WriteLine($"- Totale messaggi: {logContent.Length}");
            Console.WriteLine($"- Thread utilizzati: {NumThreads}");
            Console.WriteLine($"- Messaggi per thread: {MessagesPerThread}");
        }

        private void Cleanup()
        {
            try
            {
                (logger as IDisposable)?.Dispose();
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante cleanup: {ex.Message}");
            }
        }
    }
}
