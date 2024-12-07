using System;
using System.IO;

namespace FileGuard.Core.Interfaces
{
    public interface IFileSystemMonitor : IDisposable
    {
        /// <summary>
        /// Evento scatenato quando viene rilevato un cambiamento nel filesystem
        /// </summary>
        event EventHandler<FileSystemEventArgs> FileSystemChanged;

        /// <summary>
        /// Avvia il monitoraggio di una directory
        /// </summary>
        /// <param name="path">Percorso della directory da monitorare</param>
        /// <returns>true se il monitoraggio è stato avviato con successo, false altrimenti</returns>
        bool StartMonitoring(string path);

        /// <summary>
        /// Interrompe il monitoraggio di una directory
        /// </summary>
        /// <param name="path">Percorso della directory da non monitorare più</param>
        void StopMonitoring(string path);
    }
}
