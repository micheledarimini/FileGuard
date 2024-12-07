using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FileGuard.Core.UI
{
    public partial class App : Application
    {
        private TextWriterTraceListener? traceListener;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                SetupLogging();
                base.OnStartup(e);
                Debug.WriteLine("=== Applicazione avviata con successo ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante l'avvio: {ex}");
                MessageBox.Show($"Errore durante l'avvio dell'applicazione: {ex.Message}", "Errore Critico", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void SetupLogging()
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fileguard_debug.log");
                traceListener = new TextWriterTraceListener(logPath, "FileGuardTraceListener");
                Trace.Listeners.Add(traceListener);
                Trace.AutoFlush = true;

                Debug.WriteLine("=== Avvio Applicazione ===");
                Debug.WriteLine($"Data/Ora: {DateTime.Now}");
                Debug.WriteLine($"Log Path: {logPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'inizializzazione del logging: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (traceListener != null)
                {
                    Debug.WriteLine("=== Chiusura Applicazione ===");
                    Debug.WriteLine($"Data/Ora: {DateTime.Now}");
                    
                    traceListener.Flush();
                    traceListener.Close();
                    Trace.Listeners.Remove(traceListener);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nella chiusura del logging: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            base.OnExit(e);
        }
    }
}
