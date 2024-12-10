using System;

namespace FileGuard.Core.Logging
{
    /// <summary>
    /// Interfaccia per il sistema di logging centralizzato
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logga un messaggio informativo
        /// </summary>
        /// <param name="message">Il messaggio da loggare</param>
        /// <param name="context">Il contesto del messaggio (es. nome classe)</param>
        void LogInfo(string message, string? context = null);

        /// <summary>
        /// Logga un errore con eventuale eccezione
        /// </summary>
        /// <param name="message">Il messaggio di errore</param>
        /// <param name="ex">L'eccezione associata (opzionale)</param>
        /// <param name="context">Il contesto dell'errore (es. nome classe)</param>
        void LogError(string message, Exception? ex = null, string? context = null);

        /// <summary>
        /// Logga un messaggio di debug
        /// </summary>
        /// <param name="message">Il messaggio di debug</param>
        /// <param name="context">Il contesto del messaggio (es. nome classe)</param>
        void LogDebug(string message, string? context = null);
    }
}
