using System;
using System.Threading.Tasks;
using FileGuard.Core.Logging;

namespace FileGuard.Core.Utilities
{
    public static class OperationHandler
    {
        public static void ExecuteSafe(Action operation, ILogger logger, string context, string? operationName = null)
        {
            try
            {
                logger.LogDebug($"Inizio operazione: {operationName ?? "Non specificata"}", context);
                operation();
                logger.LogDebug($"Operazione completata: {operationName ?? "Non specificata"}", context);
            }
            catch (Exception ex)
            {
                logger.LogError($"Errore durante {operationName ?? "operazione"} in {context}", ex, context);
                throw;
            }
        }

        public static T ExecuteSafe<T>(Func<T> operation, ILogger logger, string context, string? operationName = null)
        {
            try
            {
                logger.LogDebug($"Inizio operazione: {operationName ?? "Non specificata"}", context);
                var result = operation();
                logger.LogDebug($"Operazione completata: {operationName ?? "Non specificata"}", context);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"Errore durante {operationName ?? "operazione"} in {context}", ex, context);
                throw;
            }
        }

        public static async Task ExecuteSafeAsync(Func<Task> operation, ILogger logger, string context, string? operationName = null)
        {
            try
            {
                logger.LogDebug($"Inizio operazione asincrona: {operationName ?? "Non specificata"}", context);
                await operation();
                logger.LogDebug($"Operazione asincrona completata: {operationName ?? "Non specificata"}", context);
            }
            catch (Exception ex)
            {
                logger.LogError($"Errore durante operazione asincrona {operationName ?? "Non specificata"} in {context}", ex, context);
                throw;
            }
        }

        public static async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> operation, ILogger logger, string context, string? operationName = null)
        {
            try
            {
                logger.LogDebug($"Inizio operazione asincrona: {operationName ?? "Non specificata"}", context);
                var result = await operation();
                logger.LogDebug($"Operazione asincrona completata: {operationName ?? "Non specificata"}", context);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"Errore durante operazione asincrona {operationName ?? "Non specificata"} in {context}", ex, context);
                throw;
            }
        }

        public static void ExecuteWithRetry(Action operation, ILogger logger, string context, int maxRetries = 3, TimeSpan? delay = null)
        {
            var retryDelay = delay ?? TimeSpan.FromSeconds(1);
            var attempt = 0;

            while (true)
            {
                try
                {
                    attempt++;
                    logger.LogDebug($"Tentativo {attempt} di {maxRetries}", context);
                    operation();
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt >= maxRetries)
                    {
                        logger.LogError($"Falliti tutti i tentativi ({maxRetries})", ex, context);
                        throw;
                    }

                    logger.LogWarning($"Tentativo {attempt} fallito, nuovo tentativo tra {retryDelay.TotalSeconds}s", context);
                    Task.Delay(retryDelay).Wait();
                }
            }
        }
    }
}
