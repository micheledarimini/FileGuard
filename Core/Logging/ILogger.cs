using System;

namespace FileGuard.Core.Logging
{
    public interface ILogger : IDisposable
    {
        void LogDebug(string message, string context = "");
        void LogError(string message, Exception ex, string context = "");
        void LogWarning(string message, string context = "");
        void LogInfo(string message, string context = "");
    }
}
