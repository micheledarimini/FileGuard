using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace FileGuard.Core.Logging
{
    public static class Logger
    {
        private static readonly object lockObj = new object();
        private static string? logPath;
        private static TextWriterTraceListener? logListener;

        public static void Initialize(string basePath)
        {
            try
            {
                logPath = Path.Combine(basePath, "fileguard_debug.log");
                
                // Crea il file di log con intestazione
                var header = new StringBuilder()
                    .AppendLine("=== Avvio Applicazione ===")
                    .AppendLine($"Data/Ora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                    .AppendLine($"Log Path: {logPath}")
                    .AppendLine("=== Applicazione avviata con successo ===")
                    .ToString();

                File.WriteAllText(logPath, header);

                // Configura il listener di Trace per scrivere sul file
                logListener = new TextWriterTraceListener(logPath);
                Trace.Listeners.Add(logListener);
                Trace.AutoFlush = true;

                Trace.WriteLine("Logger inizializzato");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Errore inizializzazione logger: {ex}");
            }
        }

        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(logPath)) return;

            try
            {
                lock (lockObj)
                {
                    File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - {message}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Errore scrittura log: {ex}");
            }
        }

        public static void Shutdown()
        {
            try
            {
                if (!string.IsNullOrEmpty(logPath))
                {
                    var footer = new StringBuilder()
                        .AppendLine("=== Chiusura Applicazione ===")
                        .AppendLine($"Data/Ora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                        .ToString();

                    File.AppendAllText(logPath, footer);
                }

                if (logListener != null)
                {
                    logListener.Flush();
                    logListener.Close();
                    Trace.Listeners.Remove(logListener);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Errore chiusura logger: {ex}");
            }
        }
    }
}
