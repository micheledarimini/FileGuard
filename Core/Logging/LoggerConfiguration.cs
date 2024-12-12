using System;

namespace FileGuard.Core.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class LoggerConfiguration
    {
        public string LogDirectory { get; set; } = string.Empty;
        public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
        public string FilePattern { get; set; } = "log_{0:yyyy-MM-dd}.log";
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB default
        public int MaxFiles { get; set; } = 5;

        public string GetCurrentLogPath()
        {
            return System.IO.Path.Combine(
                LogDirectory,
                string.Format(FilePattern, DateTime.Now)
            );
        }

        public static LoggerConfiguration CreateDefault()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return new LoggerConfiguration
            {
                LogDirectory = System.IO.Path.Combine(appDataPath, "FileGuard", "Logs"),
                MinimumLevel = LogLevel.Info,
                FilePattern = "fileguard_{0:yyyy-MM-dd}.log",
                MaxFileSize = 10 * 1024 * 1024,
                MaxFiles = 5
            };
        }

        public bool ShouldLog(LogLevel level)
        {
            return level >= MinimumLevel;
        }
    }
}
