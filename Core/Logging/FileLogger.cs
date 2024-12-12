using System;
using System.IO;

namespace FileGuard.Core.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _logPath;
        private readonly object _lock = new object();

        public FileLogger(string name)
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);
            _logPath = Path.Combine(logDir, $"{name}_{DateTime.Now:yyyyMMdd}.log");
        }

        private void WriteLog(string level, string message, Exception exception = null)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                if (exception != null)
                {
                    logMessage += $"{Environment.NewLine}Exception: {exception.Message}";
                    if (exception.StackTrace != null)
                    {
                        logMessage += $"{Environment.NewLine}StackTrace: {exception.StackTrace}";
                    }
                }

                lock (_lock)
                {
                    File.AppendAllText(_logPath, logMessage + Environment.NewLine);
                }
            }
            catch
            {
                // Ignora errori di scrittura log
            }
        }

        public void Log(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogDebug(string message, params object[] args)
        {
            WriteLog("DEBUG", string.Format(message, args));
        }

        public void LogInfo(string message, params object[] args)
        {
            WriteLog("INFO", string.Format(message, args));
        }

        public void LogWarning(string message, params object[] args)
        {
            WriteLog("WARN", string.Format(message, args));
        }

        public void LogError(string message, Exception exception = null, params object[] args)
        {
            WriteLog("ERROR", string.Format(message, args), exception);
        }
    }
}
