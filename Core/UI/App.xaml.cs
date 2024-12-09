using System;
using System.IO;
using System.Windows;
using FileGuard.Core.Logging;

namespace FileGuard.Core.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inizializza il logger nella stessa cartella dell'eseguibile
            var exePath = AppDomain.CurrentDomain.BaseDirectory;
            Logger.Initialize(exePath);

            // Gestisci chiusura applicazione
            Current.Exit += (s, args) => Logger.Shutdown();
        }
    }
}
