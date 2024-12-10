using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using FileGuard.Core.Models;

namespace FileGuard.Core.Interfaces
{
    public interface IFileSystemManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Collezione osservabile dei nodi monitorati nel filesystem
        /// </summary>
        ObservableCollection<FileSystemNode> MonitoredNodes { get; }

        /// <summary>
        /// Evento scatenato quando avviene un cambiamento nel filesystem
        /// </summary>
        event EventHandler<FileSystemEventArgs> FileSystemChanged;

        /// <summary>
        /// Avvia il monitoraggio di un percorso
        /// </summary>
        /// <param name="path">Percorso da monitorare</param>
        void StartMonitoring(string path);

        /// <summary>
        /// Interrompe il monitoraggio di un percorso
        /// </summary>
        /// <param name="path">Percorso da non monitorare più</param>
        void StopMonitoring(string path);
    }
}
