using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FileGuard.Core.Logging
{
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string logPath;
        private readonly object writerLock = new();
        private readonly int maxFileSizeBytes;
        private readonly int maxRetries = 3;
        private StreamWriter? logWriter;
        private bool isDisposed;

        public FileLogger(string logPath, int maxFileSizeBytes = 10 * 1024 * 1024)
        {
            this.logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
            this.maxFileSizeBytes = maxFileSizeBytes;

            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            InitializeWriter();
        }

        private void InitializeWriter()
        {
            lock (writerLock)
            {
                try
                {
                    var fileStream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                    logWriter = new StreamWriter(fileStream, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Errore inizializzazione logger: {ex.Message}");
                    throw;
                }
            }
        }

        public void LogInfo(string message, string? context = null)
        {
            WriteToLogWithRetry("[INFO]", message, context).Wait();
            Trace.WriteLine(FormatMessage("[INFO]", message, context));
        }

        public void LogError(string message, Exception? ex = null, string? context = null)
        {
            var sb = new StringBuilder(message);
            if (ex != null)
            {
                sb.AppendLine()
                  .Append("Exception: ").AppendLine(ex.GetType().Name)
                  .Append("Message: ").AppendLine(ex.Message)
                  .Append("StackTrace: ").AppendLine(ex.StackTrace);
            }

            WriteToLogWithRetry("[ERROR]", sb.ToString(), context).Wait();
            Trace.WriteLine(FormatMessage("[ERROR]", sb.ToString(), context));
        }

        public void LogDebug(string message, string? context = null)
        {
            WriteToLogWithRetry("[DEBUG]", message, context).Wait();
            Trace.WriteLine(FormatMessage("[DEBUG]", message, context));
        }

        private string FormatMessage(string level, string message, string? context)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var contextInfo = context != null ? $"[{context}] " : "";
            
            return $"{timestamp} {level} [{threadId}] {contextInfo}{message}";
        }

        private async Task WriteToLogWithRetry(string level, string message, string? context)
        {
            if (isDisposed) return;

            var formattedMessage = FormatMessage(level, message, context);
            var retryCount = 0;
            var baseDelay = TimeSpan.FromMilliseconds(100);

            while (retryCount < maxRetries)
            {
                try
                {
                    await WriteToLogAsync(formattedMessage);
                    return;
                }
                catch (IOException) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * baseDelay.TotalMilliseconds);
                    await Task.Delay(delay);

                    try
                    {
                        lock (writerLock)
                        {
                            logWriter?.Dispose();
                            InitializeWriter();
                        }
                    }
                    catch
                    {
                        // Ignora errori di reinizializzazione
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Errore fatale nella scrittura del log: {ex.Message}");
                    Trace.WriteLine(formattedMessage);
                    return;
                }
            }

            Trace.WriteLine($"Impossibile scrivere sul file di log dopo {maxRetries} tentativi");
            Trace.WriteLine(formattedMessage);
        }

        private async Task WriteToLogAsync(string formattedMessage)
        {
            if (isDisposed || logWriter == null) return;

            lock (writerLock)
            {
                try
                {
                    var fileInfo = new FileInfo(logPath);
                    if (fileInfo.Exists && fileInfo.Length >= maxFileSizeBytes)
                    {
                        RotateLogFile();
                    }

                    logWriter.WriteLine(formattedMessage);
                    logWriter.Flush();
                }
                catch (Exception ex)
                {
                    throw new IOException($"Errore nella scrittura del log: {ex.Message}", ex);
                }
            }

            await Task.CompletedTask; // Per rendere il metodo effettivamente asincrono
        }

        private void RotateLogFile()
        {
            try
            {
                lock (writerLock)
                {
                    logWriter?.Dispose();
                    logWriter = null;

                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var backupPath = Path.Combine(
                        Path.GetDirectoryName(logPath) ?? "",
                        $"{Path.GetFileNameWithoutExtension(logPath)}_{timestamp}{Path.GetExtension(logPath)}"
                    );

                    if (File.Exists(logPath))
                    {
                        File.Move(logPath, backupPath);
                    }

                    InitializeWriter();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Errore nella rotazione del file di log: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                lock (writerLock)
                {
                    try
                    {
                        if (logWriter != null)
                        {
                            logWriter.Flush();
                            logWriter.Dispose();
                            logWriter = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Errore nella chiusura del logger: {ex.Message}");
                    }
                    finally
                    {
                        isDisposed = true;
                    }
                }
            }
        }
    }
}
