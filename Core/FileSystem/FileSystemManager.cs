using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using FileGuard.Core.Models;
using FileGuard.Core.Interfaces;

namespace FileGuard.Core.FileSystem
{
    public class FileSystemManager : IFileSystemManager, INotifyPropertyChanged, IDisposable
    {
        private readonly IFileSystemNodeFactory nodeFactory;
        private readonly IFileSystemMonitor fileSystemMonitor;
        private readonly ObservableCollection<FileSystemNode> monitoredNodes;
        private bool isDisposed;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<FileSystemEventArgs>? FileSystemChanged;
        public ObservableCollection<FileSystemNode> MonitoredNodes => monitoredNodes;

        public FileSystemManager(string settingsPath, IStateManager stateManager, Dispatcher dispatcher)
        {
            Debug.WriteLine("FileSystemManager: Inizializzazione");
            if (string.IsNullOrEmpty(settingsPath))
                throw new ArgumentException("Settings path cannot be null or empty", nameof(settingsPath));

            Debug.WriteLine($"FileSystemManager: Creazione NodeFactory con settingsPath = {settingsPath}");
            nodeFactory = new FileSystemNodeFactory(stateManager, dispatcher);
            fileSystemMonitor = new FileSystemMonitor();
            monitoredNodes = new ObservableCollection<FileSystemNode>();

            fileSystemMonitor.FileSystemChanged += OnFileSystemChanged;
            Debug.WriteLine("FileSystemManager: Inizializzazione completata");
        }

        public void StartMonitoring(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.WriteLine("FileSystemManager: StartMonitoring - path vuoto");
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            }

            if (!Directory.Exists(path))
            {
                Debug.WriteLine($"FileSystemManager: StartMonitoring - directory non trovata = {path}");
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }

            Debug.WriteLine($"FileSystemManager: StartMonitoring per path = {path}");
            var existingNode = FindNode(path);
            if (existingNode != null)
            {
                Debug.WriteLine($"FileSystemManager: Nodo già monitorato = {path}");
                return;
            }

            var node = nodeFactory.CreateNode(path);
            if (node != null)
            {
                Debug.WriteLine($"FileSystemManager: Aggiunto nuovo nodo = {path}");
                monitoredNodes.Add(node);
                fileSystemMonitor.StartMonitoring(path);
                OnPropertyChanged(nameof(MonitoredNodes));
            }
            else
            {
                Debug.WriteLine($"FileSystemManager: Creazione nodo fallita per = {path}");
            }
        }

        public void StopMonitoring(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.WriteLine("FileSystemManager: StopMonitoring - path vuoto");
                return;
            }

            Debug.WriteLine($"FileSystemManager: StopMonitoring per path = {path}");
            var node = FindNode(path);
            if (node != null)
            {
                Debug.WriteLine($"FileSystemManager: Rimozione nodo = {path}");
                monitoredNodes.Remove(node);
                fileSystemMonitor.StopMonitoring(path);
                OnPropertyChanged(nameof(MonitoredNodes));
            }
            else
            {
                Debug.WriteLine($"FileSystemManager: Nodo non trovato per rimozione = {path}");
            }
        }

        private FileSystemNode? FindNode(string path)
        {
            Debug.WriteLine($"FileSystemManager: Ricerca nodo per path = {path}");
            foreach (var node in monitoredNodes)
            {
                if (node.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"FileSystemManager: Nodo trovato = {path}");
                    return node;
                }
            }
            Debug.WriteLine($"FileSystemManager: Nodo non trovato = {path}");
            return null;
        }

        private void OnFileSystemChanged(object? sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"FileSystemManager: Evento filesystem = {e.ChangeType} per {e.FullPath}");
            FileSystemChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Debug.WriteLine($"FileSystemManager: PropertyChanged = {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (isDisposed) return;

            try
            {
                Debug.WriteLine("FileSystemManager: Inizio Dispose");
                if (fileSystemMonitor is IDisposable disposableMonitor)
                {
                    disposableMonitor.Dispose();
                }
            }
            finally
            {
                isDisposed = true;
                Debug.WriteLine("FileSystemManager: Dispose completato");
            }

            GC.SuppressFinalize(this);
        }
    }
}
