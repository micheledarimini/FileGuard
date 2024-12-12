using System;

namespace FileGuard.Core.Logging
{
    public interface ILogger
    {
        void Log(string message);
        void LogDebug(string message, params object[] args);
        void LogInfo(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, Exception exception = null, params object[] args);
    }
}
