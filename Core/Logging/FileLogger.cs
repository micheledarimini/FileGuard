using System;
using System.IO;
using System.Text;
using System.Threading;

namespace FileGuard.Core.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;
        private readonly ReaderWriterLockSlim _lock;
        private readonly bool _appendToFile;

        public FileLogger(string logFilePath, bool appendToFile = true)
        {
            _logFilePath = logFilePath;
            _lock = new ReaderWriterLockSlim();
            _appendToFile = appendToFile;

            // Crea la directory dei log se non esiste
            var logDirectory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public void LogDebug(string message, string context = "")
        {
            WriteToFile("DEBUG", message, context);
        }

        public void LogError(string message, Exception ex, string context = "")
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

        public void LogWarning(string message, string context = "")
        {
            WriteToFile("WARNING", message, context);
        }

        public void LogInfo(string message, string context = "")
        {
            WriteToFile("INFO", message, context);
        }

        private void WriteToFile(string level, string message, string context)
        {
            try
            {
                _lock.EnterWriteLock();

                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{context}] {message}{Environment.NewLine}";

                File.AppendAllText(_logFilePath, logMessage);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
