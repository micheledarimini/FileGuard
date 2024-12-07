using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using FileGuard.Core.Models;

namespace FileGuard.Core.Interfaces
{
    public interface ITreeViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Collezione osservabile dei nodi monitorati
        /// </summary>
        ObservableCollection<FileSystemNode> MonitoredNodes { get; }

        /// <summary>
        /// Collezione osservabile dei cambiamenti nei file
        /// </summary>
        ObservableCollection<FileChangedEventArgs> FileChanges { get; }

        /// <summary>
        /// Nodo attualmente selezionato nell'albero
        /// </summary>
        FileSystemNode? SelectedNode { get; set; }

        /// <summary>
        /// Aggiunge una cartella al monitoraggio
        /// </summary>
        /// <param name="path">Percorso della cartella da monitorare</param>
        void AddFolder(string path);

        /// <summary>
        /// Rimuove una cartella dal monitoraggio
        /// </summary>
        /// <param name="node">Nodo da rimuovere</param>
        void RemoveFolder(FileSystemNode? node);

        /// <summary>
        /// Aggiorna lo stato di un nodo (espanso/collassato)
        /// </summary>
        /// <param name="path">Percorso del nodo</param>
        /// <param name="isExpanded">Stato di espansione</param>
        void UpdateNodeState(string path, bool isExpanded);

        /// <summary>
        /// Salva lo stato corrente dell'albero
        /// </summary>
        void SaveState();
    }

    public class FileChangedEventArgs : EventArgs
    {
        public string Path { get; }
        public string Type { get; }
        public string Description { get; }
        public DateTime Timestamp { get; }

        public FileChangedEventArgs(string path, string type, string description)
        {
            Path = path;
            Type = type;
            Description = description;
            Timestamp = DateTime.Now;
        }
    }
}
