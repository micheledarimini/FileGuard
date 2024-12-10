using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using FileGuard.Core.Logging;

namespace FileGuard.Tests
{
    public class LoggerTestRunner
    {
        private readonly string testLogPath;
        private readonly ILogger logger;
        private readonly Dictionary<int, HashSet<string>> messagesPerThread;
        private DateTime? lastTimestamp;
        
        // Metriche test
        private int totalMessages;
        private int duplicateMessages;
        private bool hasOrderViolations;

        public LoggerTestRunner()
        {
            testLogPath = Path.Combine(Path.GetTempPath(), $"logger_test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            logger = new FileLogger(testLogPath);
            messagesPerThread = new Dictionary<int, HashSet<string>>();
        }

        public async Task RunBasicTests()
        {
            Console.WriteLine("Test Logger - Verifiche Base");
            Console.WriteLine("--------------------------");
            Console.WriteLine($"Log file: {testLogPath}\n");

            try
            {
                // Esegui test concorrente
                await RunConcurrencyTest();

                // Attendi un momento per assicurarsi che tutti i messaggi siano scritti
                await Task.Delay(1000);

                // Dispose del logger prima di leggere il file
                (logger as IDisposable)?.Dispose();

                // Verifica risultati con retry
                await VerifyResultsWithRetry();
            }
            finally
            {
                Cleanup();
            }
        }

        private async Task RunConcurrencyTest()
        {
            const int NumThreads = 10;
            const int MessagesPerThread = 100;

            logger.LogInfo("Inizio test accesso concorrente", "ConcurrentTest");

            var tasks = new List<Task>();
            for (int threadId = 0; threadId < NumThreads; threadId++)
            {
                var tid = threadId;
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

            await Task.WhenAll(tasks);
            logger.LogInfo("Test accesso concorrente completato", "ConcurrentTest");
        }

        private async Task VerifyResultsWithRetry()
        {
            const int maxRetries = 3;
            const int retryDelayMs = 500;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    await VerifyResults();
                    return;
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    Console.WriteLine($"Tentativo {attempt + 1} fallito, riprovo tra {retryDelayMs}ms...");
                    await Task.Delay(retryDelayMs);
                }
            }
        }

        private async Task VerifyResults()
        {
            Console.WriteLine("Verifica Risultati:");
            Console.WriteLine("------------------");

            var logLines = await File.ReadAllLinesAsync(testLogPath);
            totalMessages = logLines.Length;

            Console.WriteLine("\n[1/2] Verifica conteggio messaggi...");
            VerifyMessageCount(logLines);

            Console.WriteLine("\n[2/2] Verifica ordine messaggi...");
            VerifyMessageOrder(logLines);

            // Riepilogo finale
            Console.WriteLine("\nRiepilogo Test:");
            Console.WriteLine($"- Totale messaggi: {totalMessages}/1002 {(totalMessages == 1002 ? "✓" : "✗")}");
            Console.WriteLine($"- Messaggi duplicati: {duplicateMessages} {(duplicateMessages == 0 ? "✓" : "✗")}");
            Console.WriteLine($"- Violazioni ordine: {(hasOrderViolations ? "Sì ✗" : "No ✓")}");
            
            // Verifica successo complessivo
            if (totalMessages != 1002 || duplicateMessages > 0 || hasOrderViolations)
            {
                throw new Exception("Test fallito! Verificare il log per i dettagli.");
            }
            
            Console.WriteLine("\nTest completato con successo! ✓");
        }

        private void VerifyMessageCount(string[] logLines)
        {
            // Verifica numero totale messaggi (2 info + debug messages)
            var expectedTotal = 1002; // 2 info + (10 thread * 100 messages)
            Console.WriteLine($"Totale messaggi: {logLines.Length}/{expectedTotal}");

            if (logLines.Length != expectedTotal)
            {
                throw new Exception($"Numero messaggi non corretto. Atteso: {expectedTotal}, Trovato: {logLines.Length}");
            }

            // Analizza messaggi per thread
            foreach (var line in logLines)
            {
                if (line.Contains("Thread") && line.Contains("Messaggio"))
                {
                    // Estrai thread ID e messaggio ID
                    var threadMatch = System.Text.RegularExpressions.Regex.Match(line, @"Thread (\d+) - Messaggio (\d+)");
                    if (threadMatch.Success)
                    {
                        var threadId = int.Parse(threadMatch.Groups[1].Value);
                        var message = threadMatch.Groups[0].Value;

                        if (!messagesPerThread.ContainsKey(threadId))
                        {
                            messagesPerThread[threadId] = new HashSet<string>();
                        }

                        // Verifica duplicati
                        if (!messagesPerThread[threadId].Add(message))
                        {
                            duplicateMessages++;
                            Console.WriteLine($"WARN: Messaggio duplicato trovato: {message}");
                        }
                    }
                }
            }

            // Verifica completezza messaggi per thread
            Console.WriteLine("\nVerifica messaggi per thread:");
            foreach (var kvp in messagesPerThread)
            {
                var count = kvp.Value.Count;
                Console.WriteLine($"- Thread {kvp.Key}: {count}/100 messaggi {(count == 100 ? "✓" : "✗")}");
            }
        }

        private void VerifyMessageOrder(string[] logLines)
        {
            lastTimestamp = null;
            var lineNumber = 0;

            foreach (var line in logLines)
            {
                lineNumber++;
                if (DateTime.TryParse(line.Split(' ')[0], out DateTime timestamp))
                {
                    if (lastTimestamp.HasValue && timestamp < lastTimestamp.Value)
                    {
                        hasOrderViolations = true;
                        Console.WriteLine($"WARN: Violazione ordine timestamp alla riga {lineNumber}:");
                        Console.WriteLine($"- Precedente: {lastTimestamp.Value}");
                        Console.WriteLine($"- Corrente:   {timestamp}");
                    }
                    lastTimestamp = timestamp;
                }

                // Verifica formato base del messaggio
                if (!line.Contains("[") || !line.Contains("]"))
                {
                    Console.WriteLine($"WARN: Formato messaggio non valido alla riga {lineNumber}:");
                    Console.WriteLine($"- {line}");
                }
            }

            Console.WriteLine($"Verifica ordine timestamp: {(hasOrderViolations ? "Fallita ✗" : "OK ✓")}");
        }

        public void Cleanup()
        {
            try
            {
                // Assicurati che il logger sia disposto
                (logger as IDisposable)?.Dispose();

                // Attendi un momento per assicurarsi che il file sia rilasciato
                Thread.Sleep(100);

                if (File.Exists(testLogPath))
                {
                    File.Delete(testLogPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARN: Errore durante cleanup: {ex.Message}");
            }
        }
    }
}
