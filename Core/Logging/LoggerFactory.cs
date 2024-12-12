using System.Collections.Concurrent;

namespace FileGuard.Core.Logging
{
    public static class LoggerFactory
    {
        private static readonly ConcurrentDictionary<string, ILogger> _loggers = new();
        private static readonly ILogger _defaultLogger;

        static LoggerFactory()
        {
            _defaultLogger = CreateLogger("FileGuard");
        }

        public static ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, loggerName => new FileLogger(loggerName));
        }

        public static ILogger GetDefaultLogger()
        {
            return _defaultLogger;
        }
    }
}
