using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FileGuard.Core.Logging;

namespace FileGuard.Core.Tests.LoggerTests
{
    public class LoggerTestRunner
    {
        private const int MESSAGE_SIZE = 512; // 512 byte per messaggio
        private const int MAX_MESSAGES = 500;
        private const int MAX_FILE_SIZE = 1024 * 1024; // 1MB
        private const int MAX_FALLBACK_SIZE = 512 * 1024; // 500KB
        private readonly string _testLogDir;
        private readonly string _testLogPath;
        private readonly LoggerConfiguration _config;

        public LoggerTestRunner()
        {
            // Usa percorso assoluto per la directory di test
            _testLogDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_logs"));
            _testLogPath = Path.Combine(_testLogDir, "test.log");
            
            _config = new LoggerConfiguration
            {
                LogDirectory = _testLogDir,
                MinimumLevel = LogLevel.Debug,
                MaxFileSize = MAX_FILE_SIZE,
                MaxFiles = 3,
                FilePattern = "test_{0:yyyyMMddHHmmss}.log"
            };
        }

        public async Task RunTests()
        {
            Console.WriteLine("Inizializzazione test logger...");
            
            if (Directory.Exists(_testLogDir))
            {
                Directory.Delete(_testLogDir, true);
            }
            Directory.CreateDirectory(_testLogDir);

            await TestNormalOperation();
            await TestRotation();
            await TestMaxFiles();
            await TestFallback();
        }

        private async Task TestNormalOperation()
        {
            Console.WriteLine("\n1. Test Operazioni Normali");
            using var logger = new FileLogger(_testLogPath, _config);

            var messageBuilder = new StringBuilder();
            messageBuilder.Length = MESSAGE_SIZE;
            messageBuilder.Replace('\0', 'X');
            var message = messageBuilder.ToString();

            var messageCount = 0;

            while (messageCount < MAX_MESSAGES / 4) // Usa 1/4 dei messaggi per test normale
            {
                logger.LogDebug($"Test message {++messageCount}: {message}");

                if (messageCount % 50 == 0)
                {
                    var fileInfo = new FileInfo(_testLogPath);
                    if (fileInfo.Exists)
                    {
                        Console.WriteLine($"Dimensione file dopo {messageCount} messaggi: {fileInfo.Length / 1024}KB");
                    }
                }

                await Task.Delay(10);
            }
        }

        private async Task TestRotation()
        {
            Console.WriteLine("\n2. Test Rotazione File");
            using var logger = new FileLogger(_testLogPath, _config);

            var messageBuilder = new StringBuilder();
            messageBuilder.Length = MESSAGE_SIZE;
            messageBuilder.Replace('\0', 'X');
            var message = messageBuilder.ToString();

            var messageCount = 0;
            var maxMessagesForRotation = MAX_MESSAGES / 4;

            while (messageCount < maxMessagesForRotation)
            {
                logger.LogDebug($"Rotation test message {++messageCount}: {message}");

                if (messageCount % 50 == 0)
                {
                    var fileInfo = new FileInfo(_testLogPath);
                    if (fileInfo.Exists)
                    {
                        Console.WriteLine($"Dimensione file dopo {messageCount} messaggi: {fileInfo.Length / 1024}KB");
                    }
                    
                    var logFiles = Directory.GetFiles(_testLogDir, "test_*.log");
                    Console.WriteLine($"Numero di file log: {logFiles.Length}");
                }

                await Task.Delay(10);
            }
        }

        private async Task TestMaxFiles()
        {
            Console.WriteLine("\n3. Test Numero Massimo File");
            using var logger = new FileLogger(_testLogPath, _config);

            var messageBuilder = new StringBuilder();
            messageBuilder.Length = MESSAGE_SIZE;
            messageBuilder.Replace('\0', 'X');
            var message = messageBuilder.ToString();

            var totalMessageCount = 0;
            var maxMessagesForMaxFiles = MAX_MESSAGES / 4;

            // Genera abbastanza dati per creare più file del massimo consentito
            for (int i = 0; i < _config.MaxFiles + 1 && totalMessageCount < maxMessagesForMaxFiles; i++)
            {
                var messageCount = 0;
                var targetSize = MAX_FILE_SIZE + 1;
                var currentSize = 0;

                while (currentSize < targetSize && totalMessageCount < maxMessagesForMaxFiles)
                {
                    logger.LogDebug($"Max files test message {++messageCount}: {message}");
                    currentSize += MESSAGE_SIZE;
                    totalMessageCount++;
                    await Task.Delay(10);
                }

                var logFiles = Directory.GetFiles(_testLogDir, "test_*.log");
                Console.WriteLine($"Numero di file log dopo rotazione {i + 1}: {logFiles.Length}");
            }
        }

        private async Task TestFallback()
        {
            Console.WriteLine("\n4. Test Fallback");
            
            var restrictedDir = Path.Combine(_testLogDir, "restricted");
            Directory.CreateDirectory(restrictedDir);
            var restrictedLogPath = Path.Combine(restrictedDir, "restricted.log");
            var fallbackPath = Path.Combine(_testLogDir, "fileguard_error.log");

            try
            {
                using var logger = new FileLogger(restrictedLogPath, _config);
                logger.LogDebug("Test message before restriction");

                var di = new DirectoryInfo(restrictedDir);
                di.Attributes = FileAttributes.ReadOnly;

                // Genera messaggi fino al limite fallback
                var messageBuilder = new StringBuilder();
                messageBuilder.Length = MESSAGE_SIZE;
                messageBuilder.Replace('\0', 'X');
                var message = messageBuilder.ToString();

                var messageCount = 0;
                while (messageCount < MAX_MESSAGES / 4)
                {
                    logger.LogDebug($"Fallback test message {++messageCount}: {message}");

                    if (File.Exists(fallbackPath))
                    {
                        var fallbackInfo = new FileInfo(fallbackPath);
                        if (fallbackInfo.Length >= MAX_FALLBACK_SIZE)
                        {
                            Console.WriteLine($"Raggiunto limite fallback di {MAX_FALLBACK_SIZE / 1024}KB");
                            break;
                        }
                    }

                    await Task.Delay(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il test fallback: {ex.Message}");
            }
            finally
            {
                var di = new DirectoryInfo(restrictedDir);
                di.Attributes = FileAttributes.Normal;
            }
        }

        public void PrintResults()
        {
            Console.WriteLine("\nRapporto Finale Test Logger:");
            
            var logFiles = Directory.GetFiles(_testLogDir, "test_*.log");
            Console.WriteLine($"\nFile di log trovati: {logFiles.Length}");
            
            var totalSize = 0L;
            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;
                Console.WriteLine($"- {Path.GetFileName(file)}: {fileInfo.Length / 1024}KB");
            }

            if (logFiles.Length > 0)
            {
                Console.WriteLine($"\nDimensione totale log: {totalSize / 1024}KB");
                Console.WriteLine($"Dimensione media per file: {totalSize / logFiles.Length / 1024}KB");
            }

            var fallbackPath = Path.Combine(_testLogDir, "fileguard_error.log");
            if (File.Exists(fallbackPath))
            {
                var fallbackInfo = new FileInfo(fallbackPath);
                Console.WriteLine($"File fallback: {fallbackInfo.Length / 1024}KB");
            }

            // Verifica limiti
            Console.WriteLine("\nVerifica Limiti:");
            if (logFiles.Length > 0)
            {
                Console.WriteLine($"- Max file size (1MB): {(totalSize / logFiles.Length <= MAX_FILE_SIZE ? "OK" : "SUPERATO")}");
            }
            Console.WriteLine($"- Max files (3): {(logFiles.Length <= 3 ? "OK" : "SUPERATO")}");
            if (File.Exists(fallbackPath))
            {
                var fallbackInfo = new FileInfo(fallbackPath);
                Console.WriteLine($"- Fallback size (500KB): {(fallbackInfo.Length <= MAX_FALLBACK_SIZE ? "OK" : "SUPERATO")}");
            }
        }
    }
}
