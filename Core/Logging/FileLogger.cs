using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;

namespace FileGuard.Core.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;
        private readonly ReaderWriterLockSlim _lock;
        private readonly bool _appendToFile;
        private readonly LoggerConfiguration _configuration;
        private StreamWriter? _writer;
        private readonly object _writerLock = new();
        private bool _disposed;

        public FileLogger(string logFilePath, bool appendToFile = true)
            : this(logFilePath, LoggerFactory.GetConfiguration(), appendToFile)
        {
        }

        public FileLogger(string logFilePath, LoggerConfiguration configuration, bool appendToFile = true)
        {
            _logFilePath = logFilePath;
            _configuration = configuration;
            _lock = new ReaderWriterLockSlim();
            _appendToFile = appendToFile;

            // Crea la directory dei log se non esiste
            var logDirectory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            InitializeWriter();
        }

        private void InitializeWriter()
        {
            try
            {
                if (_writer == null)
                {
                    lock (_writerLock)
                    {
                        if (_writer == null)
                        {
                            _writer = new StreamWriter(_logFilePath, _appendToFile, Encoding.UTF8)
                            {
                                AutoFlush = true
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToFallback($"Errore inizializzazione writer: {ex.Message}");
            }
        }

        public void LogDebug(string message, string context = "")
        {
            if (_configuration.ShouldLog(LogLevel.Debug))
            {
                WriteToFile("DEBUG", message, context);
            }
        }

        public void LogError(string message, Exception ex, string context = "")
        {
            if (_configuration.ShouldLog(LogLevel.Error))
            {
                var sb = new StringBuilder();
                sb.AppendLine(message);
                sb.AppendLine($"Exception: {ex.Message}");
                sb.AppendLine($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    sb.AppendLine($"Inner Exception: {ex.InnerException.Message}");
                    sb.AppendLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
                }

                WriteToFile("ERROR", sb.ToString(), context);
            }
        }

        public void LogWarning(string message, string context = "")
        {
            if (_configuration.ShouldLog(LogLevel.Warning))
            {
                WriteToFile("WARNING", message, context);
            }
        }

        public void LogInfo(string message, string context = "")
        {
            if (_configuration.ShouldLog(LogLevel.Info))
            {
                WriteToFile("INFO", message, context);
            }
        }

        private void WriteToFile(string level, string message, string context)
        {
            if (_disposed) return;

            try
            {
                _lock.EnterWriteLock();

                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{context}] {message}{Environment.NewLine}";

                // Verifica se è necessario ruotare il file
                CheckRotation();

                // Assicurati che il writer sia inizializzato
                InitializeWriter();

                // Scrivi il messaggio
                if (_writer != null)
                {
                    _writer.Write(logMessage);
                    _writer.Flush();
                }
                else
                {
                    WriteToFallback($"Writer non disponibile per il messaggio: {logMessage}");
                }
            }
            catch (Exception ex)
            {
                WriteToFallback($"Errore scrittura log: {ex.Message}");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void WriteToFallback(string errorMessage)
        {
            try
            {
                var fallbackPath = Path.Combine(
                    Path.GetDirectoryName(_logFilePath) ?? string.Empty,
                    "fileguard_error.log"
                );
                File.AppendAllText(fallbackPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] LOGGING ERROR: {errorMessage}{Environment.NewLine}"
                );
            }
            catch
            {
                // Se anche il fallback fallisce, non possiamo fare molto
            }
        }

        private void CheckRotation()
        {
            try
            {
                var fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Exists && fileInfo.Length >= _configuration.MaxFileSize)
                {
                    lock (_writerLock)
                    {
                        // Chiudi il writer corrente
                        if (_writer != null)
                        {
                            _writer.Dispose();
                            _writer = null;
                        }

                        // Crea il nuovo nome file con timestamp
                        var directory = Path.GetDirectoryName(_logFilePath);
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(_logFilePath);
                        var extension = Path.GetExtension(_logFilePath);
                        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        var newPath = Path.Combine(
                            directory ?? string.Empty,
                            $"{fileNameWithoutExt}_{timestamp}{extension}"
                        );

                        // Sposta il file corrente
                        if (File.Exists(_logFilePath))
                        {
                            File.Move(_logFilePath, newPath);
                        }

                        // Elimina i file più vecchi se necessario
                        CleanupOldLogs();

                        // Reinizializza il writer
                        InitializeWriter();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToFallback($"Errore durante la rotazione: {ex.Message}");
            }
        }

        private void CleanupOldLogs()
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath);
                var filePattern = Path.GetFileNameWithoutExtension(_logFilePath) + "_*" + Path.GetExtension(_logFilePath);
                var files = Directory.GetFiles(directory ?? string.Empty, filePattern)
                    .OrderByDescending(f => f)
                    .Skip(_configuration.MaxFiles - 1);

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        WriteToFallback($"Errore eliminazione file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToFallback($"Errore pulizia log: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _writer?.Dispose();
                _lock.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
